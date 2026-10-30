namespace BaseStorage.Core.Tests.Organize.Deposit;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>
/// The invariants the S-E004-013 fuzzer asserts on every random Deposit (AC-4.9, AC-4.5, AC-4.6, principle 1): an independent exclusion
/// oracle, the inventory as the only source, no excluded item in any move, conservation of the plan, the replay, and a run through
/// <see cref="OrganizeExecutor"/> over a <see cref="FakeContainerLayer"/> with conservation and untouched excluded stacks afterwards.
/// </summary>
internal static class DepositFuzzInvariants
{
    /// <summary>What one case exercised, summed by the test for its log line and its non-vacuity checks.</summary>
    /// <param name="Stacks">The inventory stacks.</param>
    /// <param name="Excluded">The excluded stacks.</param>
    /// <param name="Reasons">The union of the exclusion reasons seen.</param>
    /// <param name="Moves">The planned moves.</param>
    /// <param name="MovedItems">The items the moves carry.</param>
    /// <param name="NoRoomItems">The items left in place for lack of room.</param>
    /// <param name="Claimed">The claimed overflow chests.</param>
    /// <param name="Executed">The cases run through the executor.</param>
    internal sealed record Stats(int Stacks, int Excluded, DepositExclusion Reasons, int Moves, long MovedItems, long NoRoomItems, int Claimed, int Executed)
    {
        /// <summary>The empty total.</summary>
        internal static Stats Zero => new Stats(0, 0, DepositExclusion.None, 0, 0L, 0L, 0, 0);

        /// <summary>Adds another case's stats.</summary>
        /// <param name="other">The other stats.</param>
        internal Stats Add(Stats other) => new Stats(
            Stacks + other.Stacks,
            Excluded + other.Excluded,
            Reasons | other.Reasons,
            Moves + other.Moves,
            MovedItems + other.MovedItems,
            NoRoomItems + other.NoRoomItems,
            Claimed + other.Claimed,
            Executed + other.Executed);
    }

    /// <summary>Plans one case, asserts every planning invariant, and runs it through the executor when the fake can model it exactly.</summary>
    /// <param name="generated">The case.</param>
    /// <returns>What the case exercised.</returns>
    internal static Stats Check(RandomInventoryGenerator.GeneratedDeposit generated)
    {
        PlayerInventorySnapshot player = generated.Player;
        PlanInput input = generated.Input;
        DepositPlan deposit = DepositFixtures.Run(player, generated.Exclusions, input);
        DepositPlan again = DepositFixtures.Run(player, generated.Exclusions, input);
        AssertSamePlan(deposit.Plan, again.Plan);
        Assert.Equal(DepositFixtures.ExcludedOf(deposit), DepositFixtures.ExcludedOf(again));

        OrganizePlan plan = deposit.Plan;
        DepositExclusion[] oracle = Oracle(player, generated.Exclusions, input.Kinds);
        AssertExclusions(deposit, oracle);
        HashSet<string> excludedKeys = ExcludedKeys(player, oracle);
        AssertMoves(deposit, oracle, excludedKeys, input);
        AssertPerStackTotals(deposit, oracle);
        AssertLayouts(deposit, input);
        AssertConservation(deposit, input);

        bool invalid = player.Stacks.Any(stack => stack.Stack.Count > stack.Stack.MaxStack);
        if (!invalid)
        {
            var containers = new List<ContainerSnapshot> { player.Container };
            containers.AddRange(input.Containers);
            PlanReplay.ReplayResult replay = PlanReplay.Replay(containers, plan);
            Assert.Equal(plan.Moves.Count, replay.Steps.Count);
            Assert.Equal(Join(plan.Layouts[0].Stacks), Join(replay.Final[player.Id]));
            foreach (ChestLayout layout in plan.Layouts)
            {
                Assert.Equal(ItemTotals(layout.Stacks), ItemTotals(replay.Final[layout.Id]));
            }
        }

        int executed = 0;
        if (!invalid && FakeCanModel(deposit, input))
        {
            Execute(deposit, input, excludedKeys);
            executed = 1;
        }

        DepositExclusion reasons = DepositExclusion.None;
        foreach (ExcludedStack excluded in deposit.Excluded)
        {
            reasons |= excluded.Reasons;
        }

        return new Stats(
            player.Stacks.Count,
            deposit.Excluded.Count,
            reasons,
            plan.Moves.Count,
            plan.Moves.Sum(move => (long)move.Count),
            plan.NoRoom.Sum(entry => (long)entry.Count),
            plan.ClaimedOverflow.Count,
            executed);
    }

    /// <summary>The independent exclusion oracle: deposit contract rules D1 to D6 written again without the planner's code.</summary>
    internal static DepositExclusion[] Oracle(PlayerInventorySnapshot player, DepositExclusions exclusions, IReadOnlyDictionary<string, ItemKind> kinds)
    {
        var reasons = new DepositExclusion[player.Stacks.Count];
        for (int i = 0; i < reasons.Length; i++)
        {
            PlayerInventoryStack stack = player.Stacks[i];
            ItemKind kind = kinds.TryGetValue(stack.Stack.PrefabName, out ItemKind known) ? known : ItemKind.Other;
            DepositExclusion reason = DepositExclusion.None;
            if (stack.Slot.Y == 0)
            {
                reason |= DepositExclusion.Hotbar;
            }

            if ((stack.Traits & InventoryItemTraits.Equipped) != 0)
            {
                reason |= DepositExclusion.Equipped;
            }

            if (exclusions.Favourites.Slots.Contains(stack.Slot))
            {
                reason |= DepositExclusion.Favourite;
            }

            if (exclusions.NeverItems.Any(entry => OracleNeverMatch(entry, stack, kind)))
            {
                reason |= DepositExclusion.NeverList;
            }

            if (stack.Stack.Count > stack.Stack.MaxStack)
            {
                reason |= DepositExclusion.InvalidStack;
            }

            reasons[i] = reason;
        }

        HashSet<string> excludedKeys = ExcludedKeys(player, reasons);
        for (int i = 0; i < reasons.Length; i++)
        {
            if (reasons[i] == DepositExclusion.None && excludedKeys.Contains(KeyOf(player.Stacks[i].Stack)))
            {
                reasons[i] = DepositExclusion.SameItemAsExcluded;
            }
        }

        return reasons;
    }

    private static bool OracleNeverMatch(string entry, PlayerInventoryStack stack, ItemKind kind)
    {
        if (!entry.StartsWith("@", StringComparison.Ordinal))
        {
            return string.Equals(entry, stack.Stack.PrefabName, StringComparison.Ordinal);
        }

        InventoryItemTraits traits = stack.Traits;
        switch (entry.ToUpperInvariant())
        {
            case "@FOOD":
                return kind == ItemKind.Food || (traits & InventoryItemTraits.Food) != 0;
            case "@MEAD":
                return (traits & InventoryItemTraits.Mead) != 0;
            case "@AMMO":
                return kind == ItemKind.Ammo || (traits & InventoryItemTraits.Ammo) != 0;
            case "@BAIT":
                return (traits & InventoryItemTraits.Bait) != 0;
            default:
                return false;
        }
    }

    private static string KeyOf(ItemStackSnapshot stack) => stack.PrefabName + "|" + stack.Quality + "|" + stack.MaxStack;

    private static string KeyOf(PlannedMove move) => move.PrefabName + "|" + move.Quality + "|" + move.MaxStack;

    private static HashSet<string> ExcludedKeys(PlayerInventorySnapshot player, DepositExclusion[] reasons)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < reasons.Length; i++)
        {
            if (reasons[i] != DepositExclusion.None)
            {
                keys.Add(KeyOf(player.Stacks[i].Stack));
            }
        }

        return keys;
    }

    private static void AssertExclusions(DepositPlan deposit, DepositExclusion[] oracle)
    {
        var expected = new List<(int, DepositExclusion)>();
        for (int i = 0; i < oracle.Length; i++)
        {
            if (oracle[i] != DepositExclusion.None)
            {
                expected.Add((i, oracle[i]));
            }
        }

        Assert.Equal(expected, DepositFixtures.ExcludedOf(deposit));
        Assert.Equal(oracle.Length - expected.Count, deposit.DepositableStacks);
        for (int i = 0; i < oracle.Length; i++)
        {
            ExcludedStack? entry = deposit.FindExcluded(i);
            Assert.Equal(oracle[i] != DepositExclusion.None, entry is not null);
            if (entry is not null)
            {
                Assert.Same(deposit.Player.Stacks[i], entry.Source);
                Assert.NotEqual(DepositExclusion.None, entry.Reasons);
            }
        }
    }

    private static void AssertMoves(DepositPlan deposit, DepositExclusion[] oracle, HashSet<string> excludedKeys, PlanInput input)
    {
        OrganizePlan plan = deposit.Plan;
        PlayerInventorySnapshot player = deposit.Player;
        foreach (PlannedMove move in plan.Moves)
        {
            string where = "move " + move.SourceStackIndex + " -> " + move.Destination.Id;
            Assert.True(move.Source == player.Id, where + ": the source is not the player inventory");
            Assert.True(move.SourceStackIndex >= 0 && move.SourceStackIndex < oracle.Length, where + ": index out of range");
            Assert.True(oracle[move.SourceStackIndex] == DepositExclusion.None, where + ": an excluded stack is a source");
            ItemStackSnapshot stack = player.Stacks[move.SourceStackIndex].Stack;
            Assert.True(KeyOf(stack) == KeyOf(move), where + ": the move carries another item than the source stack");
            Assert.True(!excludedKeys.Contains(KeyOf(move)), where + ": the move carries the same item as an excluded stack");
            Assert.True(move.Count >= 1, where + ": count below 1");

            ChestLayout? layout = plan.FindLayout(move.Destination);
            Assert.True(layout is not null, where + ": the destination has no layout");
            Assert.True(layout!.Role == ChestRole.Category || layout.Role == ChestRole.ClaimedOverflow, where + ": the destination is not a category chest");
            Assert.True(layout.CategoryName == move.CategoryName, where + ": the destination category differs");
            ItemKind kind = input.Kinds.TryGetValue(move.PrefabName, out ItemKind known) ? known : ItemKind.Other;
            Assert.True(input.Catalog.Resolve(move.PrefabName, kind) == move.CategoryName, where + ": the category is not the item's");
            Assert.True(plan.FindSkipped(move.Destination) is null, where + ": the destination is a skipped chest");
        }

        foreach (NoRoomEntry entry in plan.NoRoom)
        {
            Assert.True(entry.Source == player.Id, "no-room " + entry.SourceStackIndex + ": the source is not the player inventory");
            Assert.True(oracle[entry.SourceStackIndex] == DepositExclusion.None, "no-room " + entry.SourceStackIndex + ": an excluded stack");
            Assert.True(entry.Count >= 1, "no-room " + entry.SourceStackIndex + ": count below 1");
        }
    }

    private static void AssertPerStackTotals(DepositPlan deposit, DepositExclusion[] oracle)
    {
        OrganizePlan plan = deposit.Plan;
        var expectedRemaining = new List<ItemStackSnapshot>();
        for (int i = 0; i < oracle.Length; i++)
        {
            ItemStackSnapshot stack = deposit.Player.Stacks[i].Stack;
            long moved = plan.Moves.Where(move => move.SourceStackIndex == i).Sum(move => (long)move.Count);
            long noRoom = plan.NoRoom.Where(entry => entry.SourceStackIndex == i).Sum(entry => (long)entry.Count);
            if (oracle[i] != DepositExclusion.None)
            {
                Assert.True(moved == 0 && noRoom == 0, "stack " + i + " is excluded but appears in a move or a no-room entry");
                expectedRemaining.Add(stack);
                continue;
            }

            Assert.True(moved + noRoom == stack.Count, "stack " + i + ": moved " + moved + " + no room " + noRoom + " != " + stack.Count);
            Assert.True(plan.NoRoom.Count(entry => entry.SourceStackIndex == i) <= 1, "stack " + i + " has two no-room entries");
            if (noRoom > 0)
            {
                expectedRemaining.Add(new ItemStackSnapshot(stack.PrefabName, (int)noRoom, stack.Quality, stack.MaxStack));
            }
        }

        ChestLayout inventory = plan.Layouts[0];
        Assert.Equal(deposit.Player.Id, inventory.Id);
        Assert.Equal(ChestRole.Source, inventory.Role);
        Assert.Null(inventory.CategoryName);
        Assert.Same(deposit.Player.Container, inventory.Container);
        Assert.Equal(Join(expectedRemaining), Join(inventory.Stacks));
    }

    private static void AssertLayouts(DepositPlan deposit, PlanInput input)
    {
        OrganizePlan plan = deposit.Plan;
        Assert.Equal(plan.AutoAssign.Chests.Select(chest => chest.Id), plan.Layouts.Skip(1).Select(layout => layout.Id));
        foreach (ContainerSnapshot container in input.Containers)
        {
            ChestLayout? layout = plan.FindLayout(container.Id);
            SkippedChest? skipped = plan.FindSkipped(container.Id);
            Assert.True((layout is null) != (skipped is null), "chest " + container.Id.Id + " must be exactly one of usable and skipped");
            if (input.Assignments.TryGetValue(container.Id, out CategoryAssignment? assignment) && assignment!.IsKeep)
            {
                // A Keep chest is always skipped; an ineligibility of higher precedence (tombstone, vehicle) may name the reason instead.
                Assert.True(skipped is not null, "Keep chest " + container.Id.Id + " is not skipped");
            }

            if (layout is null)
            {
                continue;
            }

            string where = "chest " + container.Id.Id;
            Dictionary<string, long> received = ItemTotals(plan.Moves.Where(move => move.Destination == container.Id)
                .Select(move => new ItemStackSnapshot(move.PrefabName, move.Count, move.Quality, move.MaxStack)));
            Dictionary<string, long> expected = ItemTotals(container.Contents);
            foreach (KeyValuePair<string, long> pair in received)
            {
                expected.TryGetValue(pair.Key, out long current);
                expected[pair.Key] = current + pair.Value;
            }

            Assert.Equal(expected, ItemTotals(layout.Stacks));
            Assert.True(layout.Stacks.Count <= container.SlotCount, where + ": more stacks than slots");
            foreach (ItemStackSnapshot stack in layout.Stacks)
            {
                Assert.True(stack.Count >= 1 && stack.Count <= stack.MaxStack, where + ": a layout stack is outside 1..MaxStack");
            }

            switch (layout.Role)
            {
                case ChestRole.Source:
                    Assert.Equal(Join(container.Contents), Join(layout.Stacks));
                    Assert.Empty(received);
                    break;
                case ChestRole.UnusedOverflow:
                    Assert.Empty(layout.Stacks);
                    Assert.Empty(received);
                    break;
                default:
                    Assert.NotNull(layout.CategoryName);
                    break;
            }
        }

        foreach (ChestLayout claimed in plan.ClaimedOverflow)
        {
            Assert.Equal(ChestRole.ClaimedOverflow, claimed.Role);
            Assert.Empty(claimed.Container.Contents);
            Assert.NotEmpty(claimed.Stacks);
            Assert.Equal(1, plan.ClaimedOverflow.Count(other => other.Id == claimed.Id));
        }
    }

    private static void AssertConservation(DepositPlan deposit, PlanInput input)
    {
        OrganizePlan plan = deposit.Plan;
        IEnumerable<ItemStackSnapshot> before = deposit.Player.Container.Contents
            .Concat(input.Containers.Where(c => plan.FindLayout(c.Id) is not null).SelectMany(c => c.Contents));
        IEnumerable<ItemStackSnapshot> after = plan.Layouts.SelectMany(layout => layout.Stacks);
        Assert.Equal(ItemTotals(before), ItemTotals(after));

        if (deposit.Player.Stacks.All(stack => stack.Stack.Count <= stack.Stack.MaxStack))
        {
            IEnumerable<ContainerSnapshot> beforeSnapshots = new[] { deposit.Player.Container }.Concat(input.Containers.Where(c => plan.FindLayout(c.Id) is not null));
            IEnumerable<ContainerSnapshot> afterSnapshots = plan.Layouts.Select(layout =>
                new ContainerSnapshot(layout.Id, layout.Container.Position, layout.Container.Width, layout.Container.Height, layout.Stacks, layout.Container.Flags));
            ConservationAssert.Holds(beforeSnapshots, afterSnapshots);
        }
    }

    /// <summary>
    /// True when the fake layer models the plan exactly: its AddItems merges by prefab and quality only, so every destination chest must
    /// hold one max stack per (prefab, quality) across its input contents and the moves into it. Sources are located by full value, so
    /// the inventory needs no such condition.
    /// </summary>
    private static bool FakeCanModel(DepositPlan deposit, PlanInput input)
    {
        foreach (ContainerId destination in deposit.Plan.Moves.Select(move => move.Destination).Distinct())
        {
            var maxStacks = new Dictionary<string, int>(StringComparer.Ordinal);
            IEnumerable<ItemStackSnapshot> stacks = input.Containers.Single(c => c.Id == destination).Contents
                .Concat(deposit.Plan.Moves.Where(move => move.Destination == destination)
                    .Select(move => new ItemStackSnapshot(move.PrefabName, move.Count, move.Quality, move.MaxStack)));
            foreach (ItemStackSnapshot stack in stacks)
            {
                string key = stack.PrefabName + "|" + stack.Quality;
                if (maxStacks.TryGetValue(key, out int known) && known != stack.MaxStack)
                {
                    return false;
                }

                maxStacks[key] = stack.MaxStack;
            }
        }

        return true;
    }

    /// <summary>Runs the plan through the executor over a fake seeded with the inventory and every base chest, then checks the world.</summary>
    private static void Execute(DepositPlan deposit, PlanInput input, HashSet<string> excludedKeys)
    {
        OrganizePlan plan = deposit.Plan;
        var fake = new FakeContainerLayer(deposit.Player.Container);
        foreach (ContainerSnapshot container in input.Containers)
        {
            fake.AddChest(container);
        }

        IReadOnlyList<ContainerSnapshot> before = fake.Capture();
        FakeSettingsSource source = new FakeSettingsSource().WithInt(SettingKeys.MovesPerFrame, 20).WithInt(SettingKeys.ReserveTimeoutMs, 1500);
        var executor = new OrganizeExecutor(plan, fake, new ManualClock(TimeSpan.Zero), new BaseStorageSettings(source));
        int steps = 0;
        while (!executor.IsComplete)
        {
            Assert.True(steps++ < 10000, "the executor did not complete");
            Assert.True(executor.Step() <= 20, "a step completed more than 20 moves");
        }

        OrganizeExecutionResult result = executor.Result;
        Assert.True(result.Status == ExecutionStatus.Completed, "the run stopped: " + result.StopReason + " at chest " + result.FailedChest.Id);
        Assert.Equal(plan.Moves.Count, result.MovesCompleted);
        Assert.Equal(plan.Moves.Sum(move => (long)move.Count), result.ItemsMoved);
        Assert.Equal(plan.NoRoom.Count, result.NoRoomStacks);

        IReadOnlyList<ContainerSnapshot> after = fake.Capture();
        Assert.Equal(ItemTotals(before.SelectMany(c => c.Contents)), ItemTotals(after.SelectMany(c => c.Contents)));
        if (before.All(c => c.Contents.Count <= c.SlotCount && c.Contents.All(stack => stack.Count <= stack.MaxStack)))
        {
            // The limit checks of ConservationAssert apply only when no skipped chest already breaks a limit in the seed.
            ConservationAssert.Holds(before, after);
        }

        ContainerSnapshot inventory = FakeFixtures.Get(fake, deposit.Player.Id);
        Assert.Equal(ItemTotals(plan.Layouts[0].Stacks), ItemTotals(inventory.Contents));
        Dictionary<string, long> excludedBefore = ItemTotals(deposit.Player.Container.Contents.Where(stack => excludedKeys.Contains(KeyOf(stack))));
        Dictionary<string, long> excludedAfter = ItemTotals(inventory.Contents.Where(stack => excludedKeys.Contains(KeyOf(stack))));
        Assert.Equal(excludedBefore, excludedAfter);
        foreach (ExcludedStack excluded in deposit.Excluded)
        {
            int seeded = deposit.Player.Container.Contents.Count(stack => stack == excluded.Source.Stack);
            int live = inventory.Contents.Count(stack => stack == excluded.Source.Stack);
            Assert.True(live >= seeded, "excluded stack " + excluded.StackIndex + " (" + Describe(excluded.Source.Stack) + ") is no longer in the inventory");
        }

        foreach (ChestLayout layout in plan.Layouts.Skip(1))
        {
            Assert.Equal(ItemTotals(layout.Stacks), ItemTotals(FakeFixtures.Get(fake, layout.Id).Contents));
        }

        foreach (SkippedChest skipped in plan.Skipped)
        {
            Assert.Equal(skipped.Container, FakeFixtures.Get(fake, skipped.Id));
        }

        foreach (ContainerSnapshot chest in fake.GetContainers())
        {
            Assert.Equal(BaseStorage.Core.Abstractions.ReservationStatus.None, fake.GetReservationStatus(chest.Id));
        }
    }
}
