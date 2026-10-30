namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>
/// The invariants the S-E004-007 fuzzer asserts on every random base (AC-4.5, AC-4.6, principle 1). It reuses
/// <see cref="PlanReplay"/> for step-by-step feasibility and <see cref="ConservationAssert"/> for the per-item totals, and adds the
/// partition, Keep, skipped, misplaced-oracle and determinism checks. Every assertion message names the chest it is about.
/// </summary>
internal static class OrganizeFuzzInvariants
{
    /// <summary>What one base exercised, summed by the test for its log line and its non-vacuity checks.</summary>
    /// <param name="Usable">The usable chests (one layout each).</param>
    /// <param name="Skipped">The skipped chests.</param>
    /// <param name="KeepSkipped">The skipped chests whose assignment is Keep.</param>
    /// <param name="InvalidSkipped">The chests skipped for invalid contents.</param>
    /// <param name="Moves">The planned moves.</param>
    /// <param name="MovedItems">The items the moves carry.</param>
    /// <param name="NoRoomItems">The items left in place for lack of room.</param>
    /// <param name="Claimed">The claimed overflow chests.</param>
    internal sealed record Stats(int Usable, int Skipped, int KeepSkipped, int InvalidSkipped, int Moves, long MovedItems, long NoRoomItems, int Claimed)
    {
        /// <summary>The empty total.</summary>
        internal static Stats Zero => new Stats(0, 0, 0, 0, 0, 0L, 0L, 0);

        /// <summary>Adds another base's stats.</summary>
        /// <param name="other">The other stats.</param>
        internal Stats Add(Stats other) => new Stats(
            Usable + other.Usable,
            Skipped + other.Skipped,
            KeepSkipped + other.KeepSkipped,
            InvalidSkipped + other.InvalidSkipped,
            Moves + other.Moves,
            MovedItems + other.MovedItems,
            NoRoomItems + other.NoRoomItems,
            Claimed + other.Claimed);
    }

    /// <summary>Plans one base and asserts every invariant. Throws an xunit assertion exception on the first broken one.</summary>
    /// <param name="input">The planner arguments.</param>
    /// <param name="autoAssignMixedChests">The AutoAssignMixedChests value of the plan (S-A-001); false by default, which is the v0.1 fuzz.</param>
    /// <returns>What the base exercised.</returns>
    internal static Stats Check(PlanInput input, bool autoAssignMixedChests = false)
    {
        OrganizePlan plan = Run(input, autoAssignMixedChests);

        AssertPartition(input, plan);
        AssertKeepAndSkippedUntouched(input, plan);
        AssertMoveShape(input, plan);
        AssertLayoutLimits(plan);

        PlanReplay.ReplayResult replay = PlanReplay.Replay(input.Containers, plan);
        Assert.Equal(plan.Moves.Count, replay.Steps.Count);
        foreach (ChestLayout layout in plan.Layouts)
        {
            Assert.True(
                replay.Peak[layout.Id] <= layout.Container.SlotCount,
                "chest " + layout.Id.Id + " exceeded its slot count during the replay");
        }

        AssertConservation(input, plan, replay);
        AssertMisplacedOracle(input, plan);
        AssertSamePlan(plan, Run(input, autoAssignMixedChests));

        int keepSkipped = plan.Skipped.Count(s => input.Assignments.TryGetValue(s.Id, out var a) && a.IsKeep);
        int invalidSkipped = plan.Skipped.Count(s => s.Kind == SkipReason.InvalidContents);
        return new Stats(
            plan.Layouts.Count,
            plan.Skipped.Count,
            keepSkipped,
            invalidSkipped,
            plan.Moves.Count,
            plan.Moves.Sum(m => (long)m.Count),
            plan.NoRoom.Sum(e => (long)e.Count),
            plan.ClaimedOverflow.Count);
    }

    /// <summary>Every input chest is exactly one of: a layout or a skipped entry, with the same snapshot instance (contract 4.5, 4.6).</summary>
    private static void AssertPartition(PlanInput input, OrganizePlan plan)
    {
        Assert.Equal(input.Containers.Length, plan.Layouts.Count + plan.Skipped.Count);
        var seen = new HashSet<ContainerId>();
        foreach (ContainerSnapshot container in input.Containers)
        {
            Assert.True(seen.Add(container.Id), "duplicate input id " + container.Id.Id);
            ChestLayout? layout = plan.FindLayout(container.Id);
            SkippedChest? skipped = plan.FindSkipped(container.Id);
            Assert.True((layout is null) != (skipped is null), "chest " + container.Id.Id + " must be exactly one of usable and skipped");
            Assert.Same(container, layout is not null ? layout.Container : skipped!.Container);
        }

        Assert.Equal(input.Containers.Where(c => plan.FindLayout(c.Id) is not null).Select(c => c.Id), plan.Layouts.Select(l => l.Id));
        Assert.Equal(input.Containers.Where(c => plan.FindSkipped(c.Id) is not null).Select(c => c.Id), plan.Skipped.Select(s => s.Id));
        Assert.Equal(plan.Layouts.Select(l => l.Id), plan.AutoAssign.Chests.Select(c => c.Id));
    }

    /// <summary>A Keep chest is always skipped, and no skipped chest is a source, a destination or a no-room source (AC-4.6, contract 4.5).</summary>
    private static void AssertKeepAndSkippedUntouched(PlanInput input, OrganizePlan plan)
    {
        var skippedIds = new HashSet<ContainerId>(plan.Skipped.Select(s => s.Id));
        foreach (KeyValuePair<ContainerId, CategoryAssignment> pair in input.Assignments)
        {
            if (pair.Value.IsKeep && input.Containers.Any(c => c.Id == pair.Key))
            {
                Assert.True(skippedIds.Contains(pair.Key), "Keep chest " + pair.Key.Id + " is not in Skipped");
                Assert.Null(plan.FindLayout(pair.Key));
            }
        }

        foreach (PlannedMove move in plan.Moves)
        {
            Assert.False(skippedIds.Contains(move.Source), "move reads from skipped chest " + move.Source.Id);
            Assert.False(skippedIds.Contains(move.Destination), "move writes to skipped chest " + move.Destination.Id);
        }

        foreach (NoRoomEntry entry in plan.NoRoom)
        {
            Assert.False(skippedIds.Contains(entry.Source), "no-room entry names skipped chest " + entry.Source.Id);
        }

        foreach (SkippedChest skipped in plan.Skipped)
        {
            Assert.Null(plan.AutoAssign.Find(skipped.Id));
            Assert.False(string.IsNullOrWhiteSpace(skipped.Reason));
        }
    }

    /// <summary>Every move has distinct ends, a positive count, a category and claimed-overflow destination, and the layout's category (4.3, 4.4).</summary>
    private static void AssertMoveShape(PlanInput input, OrganizePlan plan)
    {
        foreach (PlannedMove move in plan.Moves)
        {
            Assert.NotEqual(move.Source, move.Destination);
            Assert.True(move.Count >= 1, "move count must be at least 1");
            ChestLayout? destination = plan.FindLayout(move.Destination);
            Assert.NotNull(destination);
            Assert.Equal(destination!.CategoryName, move.CategoryName);
            Assert.True(
                destination.Role == ChestRole.Category || destination.Role == ChestRole.ClaimedOverflow,
                "move into chest " + move.Destination.Id + " with role " + destination.Role);
            Assert.NotNull(plan.FindLayout(move.Source));
        }

        var claimedIds = new HashSet<ContainerId>();
        foreach (ChestLayout claimed in plan.ClaimedOverflow)
        {
            Assert.True(claimedIds.Add(claimed.Id), "chest " + claimed.Id.Id + " is claimed twice");
            Assert.Equal(ChestRole.ClaimedOverflow, claimed.Role);
            Assert.NotEmpty(claimed.Stacks);
            Assert.Empty(claimed.Container.Contents);
            Assert.False(input.Assignments.ContainsKey(claimed.Id), "claimed chest " + claimed.Id.Id + " had a persisted assignment");
            Assert.Same(claimed, plan.FindLayout(claimed.Id));
        }

        Assert.Equal(plan.Layouts.Count(l => l.Role == ChestRole.ClaimedOverflow), plan.ClaimedOverflow.Count);
        AssertClaimedMovesMatchLayouts(plan);
    }

    /// <summary>No layout exceeds its slot count, and every layout stack is within 1..MaxStack (AC-4.5).</summary>
    private static void AssertLayoutLimits(OrganizePlan plan)
    {
        foreach (ChestLayout layout in plan.Layouts)
        {
            Assert.True(
                layout.Stacks.Count <= layout.Container.SlotCount,
                "chest " + layout.Id.Id + " layout holds " + layout.Stacks.Count + " stacks in " + layout.Container.SlotCount + " slots");
            foreach (ItemStackSnapshot stack in layout.Stacks)
            {
                Assert.True(
                    stack.Count >= 1 && stack.Count <= stack.MaxStack,
                    "chest " + layout.Id.Id + " has stack " + Describe(stack) + " outside 1..MaxStack");
            }

            if (layout.Role == ChestRole.UnusedOverflow)
            {
                Assert.Empty(layout.Stacks);
            }
        }
    }

    /// <summary>Exact per-item conservation: input, layouts and replayed end state agree per item key, per chest and through <see cref="ConservationAssert"/>.</summary>
    private static void AssertConservation(PlanInput input, OrganizePlan plan, PlanReplay.ReplayResult replay)
    {
        List<ContainerSnapshot> usableBefore = input.Containers.Where(c => plan.FindLayout(c.Id) is not null).ToList();
        List<ContainerSnapshot> usableAfter = plan.Layouts.Select(WithLayoutContents).ToList();

        AssertTotals(ItemTotals(usableBefore.SelectMany(c => c.Contents)), ItemTotals(plan.Layouts.SelectMany(l => l.Stacks)));
        AssertTotals(ItemTotals(usableBefore.SelectMany(c => c.Contents)), ItemTotals(replay.Final.Values.SelectMany(s => s)));

        foreach (ChestLayout layout in plan.Layouts)
        {
            if (layout.Role == ChestRole.Source)
            {
                Assert.Equal(Join(layout.Stacks), Join(replay.Final[layout.Id]));
            }
            else
            {
                AssertTotals(ItemTotals(layout.Stacks), ItemTotals(replay.Final[layout.Id]));
            }
        }

        ConservationAssert.Holds(usableBefore, usableAfter);

        // The whole base, with the skipped chests carried over unchanged. A skipped chest may already break a limit in the input
        // (an invalid-contents skip, or a Keep, in-use or ineligible chest classified before that check), so the whole-base limit
        // check only applies when no skipped chest does.
        if (plan.Skipped.All(s => !HasInvalidContents(s.Container)))
        {
            IEnumerable<ContainerSnapshot> after = input.Containers.Select(c => plan.FindLayout(c.Id) is ChestLayout layout ? WithLayoutContents(layout) : c);
            ConservationAssert.Holds(input.Containers, after);
        }
    }

    /// <summary>
    /// The misplaced oracle (AC-4.5 rule 1 and 4): a stack is misplaced when its resolved category differs from its chest's category
    /// (as <c>AutoAssign</c> reports it). For every misplaced stack, moved plus no-room equals its count; a stack in place is in no move
    /// and no entry. Every move and entry carries the item's resolved category.
    /// </summary>
    private static void AssertMisplacedOracle(PlanInput input, OrganizePlan plan)
    {
        var transferred = new Dictionary<(ContainerId Source, int Index), long>();
        foreach (PlannedMove move in plan.Moves)
        {
            (ContainerId, int) key = (move.Source, move.SourceStackIndex);
            transferred[key] = transferred.GetValueOrDefault(key) + move.Count;
            Assert.Equal(ResolveCategory(input, move.PrefabName), move.CategoryName);
        }

        foreach (NoRoomEntry entry in plan.NoRoom)
        {
            (ContainerId, int) key = (entry.Source, entry.SourceStackIndex);
            transferred[key] = transferred.GetValueOrDefault(key) + entry.Count;
            Assert.Equal(ResolveCategory(input, entry.PrefabName), entry.CategoryName);
        }

        var expected = new HashSet<(ContainerId Source, int Index)>();
        foreach (ChestLayout layout in plan.Layouts)
        {
            ChestAutoAssignment? auto = plan.AutoAssign.Find(layout.Id);
            Assert.NotNull(auto);
            string? chestCategory = auto!.CategoryName;
            if (layout.Role == ChestRole.Category)
            {
                Assert.Equal(chestCategory, layout.CategoryName);
            }
            else if (layout.Role == ChestRole.ClaimedOverflow)
            {
                Assert.Null(chestCategory);
                Assert.NotNull(layout.CategoryName);
            }
            else
            {
                Assert.Null(layout.CategoryName);
            }

            IReadOnlyList<ItemStackSnapshot> contents = layout.Container.Contents;
            for (int i = 0; i < contents.Count; i++)
            {
                bool misplaced = !string.Equals(ResolveCategory(input, contents[i].PrefabName), chestCategory, StringComparison.Ordinal);
                long moved = transferred.GetValueOrDefault((layout.Id, i));
                if (misplaced)
                {
                    expected.Add((layout.Id, i));
                    Assert.True(
                        moved == contents[i].Count,
                        "chest " + layout.Id.Id + " stack " + i + " (" + Describe(contents[i]) + ") is misplaced but moved+noRoom is " + moved);
                }
                else
                {
                    Assert.True(moved == 0, "chest " + layout.Id.Id + " stack " + i + " (" + Describe(contents[i]) + ") is in place but was moved");
                }
            }
        }

        Assert.True(transferred.Keys.All(expected.Contains), "a move or no-room entry names a stack that is not misplaced or does not exist");
    }

    /// <summary>True when the input snapshot itself breaks a limit: more stacks than slots, or a stack above its max stack.</summary>
    private static bool HasInvalidContents(ContainerSnapshot container) =>
        container.Contents.Count > container.SlotCount || container.Contents.Any(s => s.Count > s.MaxStack);

    private static string ResolveCategory(PlanInput input, string prefabName)
    {
        ItemKind kind = input.Kinds.TryGetValue(prefabName, out ItemKind known) ? known : ItemKind.Other;
        return input.Catalog.Resolve(prefabName, kind);
    }

    /// <summary>The input snapshot of a usable chest with the layout's stacks as its contents.</summary>
    private static ContainerSnapshot WithLayoutContents(ChestLayout layout) =>
        new ContainerSnapshot(
            layout.Container.Id,
            layout.Container.Position,
            layout.Container.Width,
            layout.Container.Height,
            layout.Stacks,
            layout.Container.Flags);
}
