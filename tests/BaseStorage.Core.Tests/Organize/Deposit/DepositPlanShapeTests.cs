namespace BaseStorage.Core.Tests.Organize.Deposit;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.Deposit.DepositFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>
/// AC-4.9, AC-4.5, AC-4.6: the shape of a Deposit plan (deposit contract 6.3, 6.6 to 6.8): the inventory is the only source, base chests
/// are destinations only, Keep chests stay untouched, overflow is claimed as in Organize, and the input validation order.
/// </summary>
public sealed class DepositPlanShapeTests
{
    private const uint Player = 1u;

    // GW-U-1060 (AC-4.9, 6.3): the golden plan of inventory I into base D, every list as literals.
    [Fact]
    public void InventoryIIntoBaseDGivesTheContractPlan()
    {
        DepositPlan deposit = RunI();
        OrganizePlan plan = deposit.Plan;

        Assert.Equal(
            new[]
            {
                Move(Player, 2, 301, "Wood", 1, 50, 50, "Wood"),
                Move(Player, 3, 301, "Wood", 1, 50, 20, "Wood"),
                Move(Player, 3, 306, "Wood", 1, 50, 20, "Wood"),
                Move(Player, 4, 304, "Stone", 1, 50, 30, "Stone"),
                Move(Player, 11, 306, "Wood", 1, 50, 10, "Wood"),
            },
            MovesOf(plan));
        Assert.Equal(
            new[]
            {
                NoRoom(Player, 5, "Flint", 1, 50, 20, "Stone"),
                NoRoom(Player, 10, "TrophyDeer", 1, 20, 1, "Trophies"),
            },
            NoRoomOf(plan));
        Assert.Equal(new[] { Skip(305, SkipReason.Keep, "Keep", null, ExclusionReason.Keep) }, SkipsOf(plan));
        Assert.Equal(
            new[]
            {
                Layout(Player, ChestRole.Source, null, S("SwordIron", 1, 1, 2), S("CookedMeat", 5), S("Flint", 20), S("HelmetBronze", 1, 1), S("Resin", 12), S("ArrowWood", 20), S("Hammer", 1, 1), S("TrophyDeer", 1, 20)),
                Layout(301, ChestRole.Category, "Wood", S("Wood", 50), S("Wood", 50)),
                Layout(302, ChestRole.Category, "Stone", S("Stone", 50)),
                Layout(303, ChestRole.Source, null, S("Flint", 5), S("Wood", 5)),
                Layout(304, ChestRole.ClaimedOverflow, "Stone", S("Stone", 30)),
                Layout(306, ChestRole.ClaimedOverflow, "Wood", S("Wood", 30)),
            },
            LayoutsOf(plan));
        Assert.Equal(new[] { Layout(306, ChestRole.ClaimedOverflow, "Wood", S("Wood", 30)), Layout(304, ChestRole.ClaimedOverflow, "Stone", S("Stone", 30)) }, ClaimedOf(plan));
        Assert.Equal(
            new[]
            {
                (0, DepositExclusion.Hotbar | DepositExclusion.Equipped),
                (1, DepositExclusion.Hotbar | DepositExclusion.NeverList),
                (6, DepositExclusion.Equipped),
                (7, DepositExclusion.Favourite),
                (8, DepositExclusion.NeverList),
                (9, DepositExclusion.NeverList),
            },
            ExcludedOf(deposit));
        Assert.Equal(6, deposit.DepositableStacks);
        Assert.Same(plan.Layouts[0], plan.FindLayout(PlayerId));
        Assert.Same(deposit.Player.Container, plan.Layouts[0].Container);
        Assert.Equal(new uint[] { 301, 302, 303, 304, 306 }, plan.AutoAssign.Chests.Select(chest => chest.Id.Id));
    }

    // GW-U-1061 (AC-4.9, 6.6): the inventory is the only source; a misplaced stack in a base chest stays, even with room elsewhere.
    [Fact]
    public void BaseChestsAreDestinationsOnly()
    {
        // Base E (Organize contract 6.2): chest 201 (Wood) holds a misplaced Stone 10 that an Organize would move to 202.
        DepositPlan deposit = Run(Inventory(At(0, 1, S("Wood", 10))), NoExclusions(), E());
        OrganizePlan plan = deposit.Plan;

        Assert.All(plan.Moves, move => Assert.Equal(PlayerId, move.Source));
        Assert.All(plan.NoRoom, entry => Assert.Equal(PlayerId, entry.Source));
        Assert.Equal(new[] { Move(Player, 0, 201, "Wood", 1, 50, 10, "Wood") }, MovesOf(plan));
        Assert.Equal(Layout(201, ChestRole.Category, "Wood", S("Stone", 10), S("Wood", 40)), LayoutRowOf(plan.FindLayout(Id(201))!));
        Assert.Equal(Layout(202, ChestRole.Category, "Stone", S("Flint", 5), S("Stone", 45)), LayoutRowOf(plan.FindLayout(Id(202))!));
        // 203 auto-assigns to Stone (Flint and Stone are 2 of 3 stacks); its misplaced Wood 20 stays, because a Deposit moves nothing out of a chest.
        Assert.Equal(Layout(203, ChestRole.Category, "Stone", S("Flint", 7), S("Stone", 40), S("Wood", 20)), LayoutRowOf(plan.FindLayout(Id(203))!));
        Assert.Equal(Layout(209, ChestRole.Source, null, S("Wood", 40), S("Wood", 45), S("Resin", 12), S("CookedMeat", 5), S("TrophyDeer", 1, 20)), LayoutRowOf(plan.FindLayout(Id(209))!));
        Assert.Equal(new uint[] { 205, 206, 207 }, plan.Skipped.Select(skip => skip.Id.Id));
        Assert.Equal(PlayerId, plan.Layouts[0].Id);
        Assert.Equal(ChestRole.Source, plan.Layouts[0].Role);
        Assert.Empty(plan.Layouts[0].Stacks);
    }

    // GW-U-1062 (AC-4.6, 6.7): a Keep chest is never a destination, even when it is the only chest of the category and has room.
    [Fact]
    public void KeepChestsAreNeverDestinations()
    {
        ContainerSnapshot[] chests = { C(401, 0, 0, 4), C(402, 2, 0, 2, S("Wood", 10)) };
        Dictionary<ContainerId, CategoryAssignment> assignments = Assign((401, CategoryAssignment.Keep), (402, CategoryAssignment.Keep));

        DepositPlan deposit = Run(Inventory(At(0, 1, S("Wood", 50)), At(1, 1, S("Stone", 5))), NoExclusions(), Base(chests, assignments));

        Assert.Empty(deposit.Plan.Moves);
        Assert.Equal(new[] { NoRoom(Player, 0, "Wood", 1, 50, 50, "Wood"), NoRoom(Player, 1, "Stone", 1, 50, 5, "Stone") }, NoRoomOf(deposit.Plan));
        Assert.Equal(new[] { Skip(401, SkipReason.Keep, "Keep", null, ExclusionReason.Keep), Skip(402, SkipReason.Keep, "Keep", null, ExclusionReason.Keep) }, SkipsOf(deposit.Plan));
        Assert.Equal(new[] { Layout(Player, ChestRole.Source, null, S("Wood", 50), S("Stone", 5)) }, LayoutsOf(deposit.Plan));
        Assert.Null(deposit.Plan.FindLayout(Id(401)));
        Assert.NotNull(deposit.Plan.FindSkipped(Id(402)));
    }

    // GW-U-1063 (AC-4.9, 6.8): an empty inventory, an all-excluded inventory and an empty base plan nothing and throw nothing.
    [Fact]
    public void EmptyAndAllExcludedInventoriesPlanNothing()
    {
        DepositPlan empty = Run(Inventory(), DefaultExclusions(), D());
        Assert.Empty(empty.Plan.Moves);
        Assert.Empty(empty.Plan.NoRoom);
        Assert.Empty(empty.Excluded);
        Assert.Equal(0, empty.DepositableStacks);
        Assert.Equal(6, empty.Plan.Layouts.Count);
        Assert.Empty(empty.Plan.Layouts[0].Stacks);
        Assert.Empty(empty.Plan.ClaimedOverflow);

        DepositPlan excluded = Run(Inventory(At(0, 0, S("Wood", 50)), At(1, 0, S("Stone", 50))), DefaultExclusions(), D());
        Assert.Empty(excluded.Plan.Moves);
        Assert.Empty(excluded.Plan.NoRoom);
        Assert.Equal(2, excluded.Excluded.Count);
        Assert.Equal("Wood x50 q1 max50; Stone x50 q1 max50", Join(excluded.Plan.Layouts[0].Stacks));

        DepositPlan noBase = Run(Inventory(At(0, 1, S("Wood", 50))), NoExclusions(), Base(Array.Empty<ContainerSnapshot>()));
        Assert.Empty(noBase.Plan.Moves);
        Assert.Equal(new[] { NoRoom(Player, 0, "Wood", 1, 50, 50, "Wood") }, NoRoomOf(noBase.Plan));
        Assert.Single(noBase.Plan.Layouts);
    }

    // GW-U-1064 (P2, P10): planning twice gives the same plan and changes no input.
    [Fact]
    public void PlanningIsDeterministicAndReadsOnly()
    {
        PlayerInventorySnapshot player = I();
        ContainerSnapshot playerCopy = new ContainerSnapshot(player.Id, new WorldPosition(0f, 0f, 0f), 8, 4, player.Container.Contents, ContainerFlags.None);
        PlanInput input = D();
        ContainerSnapshot[] copies = input.Containers.Select(c => new ContainerSnapshot(c.Id, c.Position, c.Width, c.Height, c.Contents, c.Flags)).ToArray();

        DepositPlan first = Run(player, DefaultExclusions(FavouriteSlot), input);
        DepositPlan second = Run(player, DefaultExclusions(FavouriteSlot), input);

        AssertSamePlan(first.Plan, second.Plan);
        Assert.Equal(ExcludedOf(first), ExcludedOf(second));
        Assert.Equal(playerCopy, player.Container);
        Assert.Equal(copies, input.Containers);
        Assert.Equal(12, player.Stacks.Count);
    }

    // GW-U-1065 (5.2): validation order and parameter names; the inventory id among the containers is rejected.
    [Fact]
    public void ValidationNamesTheParametersInOrder()
    {
        PlanInput d = D();
        PlayerInventorySnapshot player = I();
        DepositExclusions exclusions = NoExclusions();

        Assert.Equal("player", Assert.Throws<ArgumentNullException>(() => DepositPlanner.Plan(null!, null!, null!, null!, null!, null!, null!, d.Options, 50)).ParamName);
        Assert.Equal("exclusions", Assert.Throws<ArgumentNullException>(() => DepositPlanner.Plan(player, null!, null!, null!, null!, null!, null!, d.Options, 50)).ParamName);
        Assert.Equal("containers", Assert.Throws<ArgumentNullException>(() => DepositPlanner.Plan(player, exclusions, null!, null!, null!, null!, null!, d.Options, 50)).ParamName);
        Assert.Equal("assignments", Assert.Throws<ArgumentNullException>(() => DepositPlanner.Plan(player, exclusions, d.Containers, null!, null!, null!, null!, d.Options, 50)).ParamName);
        Assert.Equal("inUseBy", Assert.Throws<ArgumentNullException>(() => DepositPlanner.Plan(player, exclusions, d.Containers, d.Assignments, null!, null!, null!, d.Options, 50)).ParamName);
        Assert.Equal("catalog", Assert.Throws<ArgumentNullException>(() => DepositPlanner.Plan(player, exclusions, d.Containers, d.Assignments, d.InUseBy, null!, null!, d.Options, 50)).ParamName);
        Assert.Equal("itemKinds", Assert.Throws<ArgumentNullException>(() => DepositPlanner.Plan(player, exclusions, d.Containers, d.Assignments, d.InUseBy, d.Catalog, null!, d.Options, 50)).ParamName);

        ContainerSnapshot[] withPlayer = d.Containers.Concat(new[] { player.Container }).ToArray();
        Assert.Equal("containers", Assert.Throws<ArgumentException>(() => Run(player, exclusions, Base(withPlayer))).ParamName);
        ContainerSnapshot[] duplicate = d.Containers.Concat(new[] { d.Containers[0] }).ToArray();
        Assert.Equal("containers", Assert.Throws<ArgumentException>(() => Run(player, exclusions, Base(duplicate))).ParamName);

        // Snapshot validation.
        Assert.Equal("id", Assert.Throws<ArgumentException>(() => new PlayerInventorySnapshot(default, 8, 4, Array.Empty<PlayerInventoryStack>())).ParamName);
        Assert.Equal("width", Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerInventorySnapshot(PlayerId, 0, 4, Array.Empty<PlayerInventoryStack>())).ParamName);
        Assert.Equal("height", Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerInventorySnapshot(PlayerId, 8, 257, Array.Empty<PlayerInventoryStack>())).ParamName);
        Assert.Equal("stacks", Assert.Throws<ArgumentNullException>(() => new PlayerInventorySnapshot(PlayerId, 8, 4, null!)).ParamName);
        Assert.Equal("stacks", Assert.Throws<ArgumentException>(() => Inventory(At(0, 1, S("Wood", 1)), null!)).ParamName);
        Assert.Equal("stacks", Assert.Throws<ArgumentException>(() => Inventory(At(0, 1, S("Wood", 1)), At(0, 1, S("Stone", 1)))).ParamName);
        Assert.Equal("stacks", Assert.Throws<ArgumentException>(() => Inventory(At(8, 1, S("Wood", 1)))).ParamName);
        Assert.Equal("stacks", Assert.Throws<ArgumentException>(() => Inventory(At(0, 4, S("Wood", 1)))).ParamName);

        // Nothing else throws: any threshold, default options, unknown prefabs, a 1 x 1 grid.
        foreach (int threshold in new[] { int.MinValue, -1, 0, 101, int.MaxValue })
        {
            Run(player, exclusions, Base(DChests(), d.Assignments, threshold));
        }

        DepositPlanner.Plan(player, exclusions, d.Containers, d.Assignments, d.InUseBy, d.Catalog, d.Kinds, default(EligibilityOptions), 50);
        PlayerInventorySnapshot tiny = new PlayerInventorySnapshot(PlayerId, 1, 1, new[] { At(0, 0, S("Unknown", 3)) });
        Assert.Single(Run(tiny, exclusions, d).Excluded);
        Assert.Equal(tiny.Container.Contents, tiny.Stacks.Select(stack => stack.Stack));
    }
}
