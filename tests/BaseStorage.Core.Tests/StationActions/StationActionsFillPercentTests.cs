namespace BaseStorage.Core.Tests.StationActions;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// GW-U-2252..2253 (AC-1, AC-2, station-actions contract 12.1 and BF6): the Plugin's FillPercent through StationActionsTypes, on 5 by 2
/// chests resolved by BaseResolver. The member count and the fill are read on the same chests in every row. Red until StationActionsFacts
/// gains FillPercent (S-E010-001 implement phase).
/// </summary>
public sealed class StationActionsFillPercentTests
{
    private const long UserId = 3141592653L;
    private static readonly StationId Bench = new StationId(UserId, 17u);
    private static readonly EligibilityOptions Options = new EligibilityOptions(excludeVehicles: true, keepChestsCraftable: true);

    // GW-U-2252 (12.1 row 1): a start outside every base gives 0.
    [Fact]
    public void AStartOutsideEveryBaseGivesZero()
    {
        Assert.Equal(0, FillPercent(BaseResolution.NotInBase, NoAssignments()));
    }

    // GW-U-2252 (12.1 row 2): a workbench with no chest gives 0.
    [Fact]
    public void ABaseWithNoChestGivesZero()
    {
        Assert.Equal(0, FillPercent(Resolve(), NoAssignments()));
    }

    // GW-U-2252 (12.1 row 3): three empty chests give 0.
    [Fact]
    public void ThreeEmptyChestsGiveZero()
    {
        Assert.Equal(0, FillPercent(Resolve(Chest(1, 0), Chest(2, 0), Chest(3, 0)), NoAssignments()));
    }

    // GW-U-2252 (12.1 row 4): one stack in one of three chests (1 of 30 slots) gives 3.
    [Fact]
    public void OneStackInOneOfThreeChestsGivesThree()
    {
        Assert.Equal(3, FillPercent(Resolve(Chest(1, 1), Chest(2, 0), Chest(3, 0)), NoAssignments()));
    }

    // GW-U-2252 (12.1 row 5): one stack in each of two chests (2 of 30 slots) gives 7.
    [Fact]
    public void TwoStacksInTwoOfThreeChestsGiveSeven()
    {
        Assert.Equal(7, FillPercent(Resolve(Chest(1, 1), Chest(2, 1), Chest(3, 0)), NoAssignments()));
    }

    // GW-U-2252 (12.1 row 6): three full chests give 100.
    [Fact]
    public void ThreeFullChestsGiveOneHundred()
    {
        Assert.Equal(100, FillPercent(Resolve(Chest(1, 10), Chest(2, 10), Chest(3, 10)), NoAssignments()));
    }

    // GW-U-2252 (12.1 nulls): a null resolution throws ArgumentNullException named resolution.
    [Fact]
    public void ANullResolutionThrowsWithItsName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => FillPercent(null!, NoAssignments()));

        Assert.Equal("resolution", ex.ParamName);
    }

    // GW-U-2252 (12.1 nulls): a null assignments throws ArgumentNullException named assignments.
    [Fact]
    public void ANullAssignmentsThrowsWithItsName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => FillPercent(Resolve(), null!));

        Assert.Equal("assignments", ex.ParamName);
    }

    // GW-U-2253 (AC-1, BF6, 12.1 rows 7 and 8): three full chests with one on Keep give (2, 100), so M and P count the same chests.
    [Fact]
    public void ThreeFullChestsWithOneOnKeepGiveTwoChestsAndOneHundredPercent()
    {
        ContainerSnapshot a = Chest(1, 10);
        ContainerSnapshot b = Chest(2, 10);
        ContainerSnapshot c = Chest(3, 10);
        BaseResolution resolution = Resolve(a, b, c);
        Dictionary<ContainerId, CategoryAssignment> keepA = KeepOf(a);

        Assert.Equal((2, 100), (EligibleChestCount(resolution, keepA), FillPercent(resolution, keepA)));
    }

    // GW-U-2253 (AC-1, BF6, 12.1 row 9): one full chest on Keep plus one empty Auto chest give (1, 0).
    [Fact]
    public void OneFullKeepChestPlusOneEmptyAutoChestGiveOneChestAndZeroPercent()
    {
        ContainerSnapshot full = Chest(1, 10);
        ContainerSnapshot empty = Chest(2, 0);
        BaseResolution resolution = Resolve(full, empty);
        Dictionary<ContainerId, CategoryAssignment> keepFull = KeepOf(full);

        Assert.Equal((1, 0), (EligibleChestCount(resolution, keepFull), FillPercent(resolution, keepFull)));
    }

    // GW-U-2253 (AC-1, BF6, 12.1 row 10): a tombstone with 10 stacks plus one empty chest give (1, 0); the tombstone is not eligible.
    [Fact]
    public void ATombstoneWithTenStacksPlusOneEmptyChestGivesOneChestAndZeroPercent()
    {
        ContainerSnapshot tombstone = Chest(1, 10, ContainerFlags.Tombstone);
        ContainerSnapshot empty = Chest(2, 0);
        BaseResolution resolution = Resolve(tombstone, empty);

        Assert.Equal((1, 0), (EligibleChestCount(resolution, NoAssignments()), FillPercent(resolution, NoAssignments())));
    }

    private static int FillPercent(BaseResolution resolution, Dictionary<ContainerId, CategoryAssignment> assignments) =>
        StationActionsTypes.FillPercent(resolution, assignments, Options);

    private static int EligibleChestCount(BaseResolution resolution, Dictionary<ContainerId, CategoryAssignment> assignments) =>
        (int)StationActionsTypes.CallStatic("StationActionsFacts", "EligibleChestCount", resolution, assignments, Options)!;

    private static BaseResolution Resolve(params ContainerSnapshot[] containers) =>
        BaseResolver.Resolve(new[] { Workbench() }, containers, BaseStart.FromStation(Bench), 64);

    private static StationSnapshot Workbench() => new StationSnapshot(Bench, new WorldPosition(0f, 0f, 0f), 20f);

    private static ContainerSnapshot Chest(uint serial, int stacks, ContainerFlags flags = ContainerFlags.PlayerBuilt) =>
        new ContainerSnapshot(
            new ContainerId(UserId, serial),
            new WorldPosition(2f * serial, 0f, 0f),
            5,
            2,
            Enumerable.Range(0, stacks).Select(_ => new ItemStackSnapshot("Wood", 10, 1, 50)).ToArray(),
            flags);

    private static Dictionary<ContainerId, CategoryAssignment> NoAssignments() => new Dictionary<ContainerId, CategoryAssignment>();

    private static Dictionary<ContainerId, CategoryAssignment> KeepOf(ContainerSnapshot container) =>
        new Dictionary<ContainerId, CategoryAssignment> { [container.Id] = CategoryAssignment.Keep };
}
