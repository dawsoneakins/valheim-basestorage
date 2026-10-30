namespace BaseStorage.Core.Tests.Availability;

using System;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;

/// <summary>AC-1 (contract 9.6 C1, C4, C5, C6 and V9): local edge fixtures for the counting rules that have no 9.7 row.</summary>
public sealed class AvailabilityEdgeTests
{
    // GW-U-403 (AC-1, C1): inventory flags are ignored, so an inventory flagged Tombstone | InUseByOther | AccessDenied is counted in full.
    [Fact]
    public void InventoryIsCountedInFullWhateverItsFlags()
    {
        ContainerSnapshot inventory = Make(
            1u, 0f, 0f, 0f, 8, 4,
            ContainerFlags.Tombstone | ContainerFlags.InUseByOther | ContainerFlags.AccessDenied,
            Stack("Wood", 12, 1, 50),
            Stack("Resin", 5, 1, 50));
        BaseSnapshot snapshot = Custom(inventory, Array.Empty<ContainerSnapshot>(), NoAssignments(), true, true);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("Wood", 60), new CraftRequirement("Resin", 5) });

        Assert.Equal(12, result.Requirements[0].InInventory);
        Assert.Equal(5, result.Requirements[1].InInventory);
        Assert.Equal(0, result.Requirements[0].InContainers);
    }

    // GW-U-403 (AC-1, C4): containers that eligibility excludes are in neither list, even when they are also in use, and contribute 0.
    [Fact]
    public void ExcludedContainersAreInNeitherListEvenWhenInUse()
    {
        ContainerSnapshot tombstone = Box(90u, ContainerFlags.Tombstone | ContainerFlags.InUseByOther, Stack("Wood", 10, 1, 50));
        ContainerSnapshot accessDenied = Box(
            91u, ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied | ContainerFlags.InUseByOther, Stack("Wood", 10, 1, 50));
        BaseSnapshot snapshot = Custom(EmptyInventory(), new[] { tombstone, accessDenied }, NoAssignments(), true, true);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("Wood", 20) });

        Assert.Empty(result.CountedContainers);
        Assert.Empty(result.SkippedInUse);
        Assert.Equal(0, result.Requirements[0].InContainers);
    }

    // GW-U-403 (AC-1, C2 and C4): with ExcludeVehicles off, a PlayerBuilt | Vehicle | InUseByOther container is in SkippedInUse and contributes 0.
    [Fact]
    public void InUseVehicleIsSkippedWhenVehiclesAreNotExcluded()
    {
        ContainerSnapshot cart = Box(
            92u, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle | ContainerFlags.InUseByOther, Stack("Wood", 10, 1, 50));
        BaseSnapshot snapshot = Custom(EmptyInventory(), new[] { cart }, NoAssignments(), false, true);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("Wood", 10) });

        Assert.Equal(new uint[] { 92 }, Ids(result.SkippedInUse));
        Assert.Empty(result.CountedContainers);
        Assert.Equal(0, result.Requirements[0].InContainers);
    }

    // GW-U-404 (AC-1, C6): an over-full stack ("Wood", 75 on a max of 50) contributes its full count of 75.
    [Fact]
    public void OverFullStackContributesItsFullCount()
    {
        ContainerSnapshot chest = Box(93u, ContainerFlags.PlayerBuilt, Stack("Wood", 75, 1, 50));
        BaseSnapshot snapshot = Custom(EmptyInventory(), new[] { chest }, NoAssignments(), true, true);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("Wood", 75) });

        Assert.Equal((0, 75, 75, true, 0), Counts(result.Requirements[0]));
    }

    // GW-U-404 (AC-1, C6): two counted containers that each hold int.MaxValue saturate the sum at int.MaxValue, and do not throw.
    [Fact]
    public void ContainerSumsSaturateAtIntMaxValue()
    {
        ContainerSnapshot first = Box(94u, ContainerFlags.PlayerBuilt, Stack("Wood", int.MaxValue, 1, 50));
        ContainerSnapshot second = Box(95u, ContainerFlags.PlayerBuilt, Stack("Wood", int.MaxValue, 1, 50));
        BaseSnapshot snapshot = Custom(EmptyInventory(), new[] { first, second }, NoAssignments(), true, true);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("Wood", 1) });

        Assert.Equal(int.MaxValue, result.Requirements[0].InContainers);
        Assert.Equal(int.MaxValue, result.Requirements[0].Total);
    }

    // GW-U-404 (AC-1, C6): an inventory stack of int.MaxValue plus a container stack of 1 saturates Total, and Missing is 0.
    [Fact]
    public void InventoryPlusContainerSaturatesTotalAndMissingIsZero()
    {
        ContainerSnapshot inventory = Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, Stack("Wood", int.MaxValue, 1, 50));
        ContainerSnapshot chest = Box(96u, ContainerFlags.PlayerBuilt, Stack("Wood", 1, 1, 50));
        BaseSnapshot snapshot = Custom(inventory, new[] { chest }, NoAssignments(), true, true);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("Wood", int.MaxValue) });

        Assert.Equal((int.MaxValue, 1, int.MaxValue, true, 0), Counts(result.Requirements[0]));
    }

    // GW-U-405 (AC-1, C5): matching is ordinal and never trims or folds case. "wood" and "Wood " match nothing, and "Silver" is absent everywhere.
    [Theory]
    [InlineData("wood")]
    [InlineData("WOOD")]
    [InlineData("Wood ")]
    [InlineData("Silver")]
    public void PrefabMatchingIsOrdinalAndAbsentPrefabsCountZero(string prefab)
    {
        World world = Create();

        AvailabilityResult result = BaseAvailability.Compute(Snapshot(world, true), new[] { new CraftRequirement(prefab, 1) });

        Assert.Equal((0, 0, 0, false, 1), Counts(result.Requirements[0]));
    }

    // GW-U-406 (AC-1, V9): a duplicate container id counts once, and the first one wins.
    [Fact]
    public void DuplicateContainerIdCountsOnce()
    {
        World world = Create();
        var snapshot = new BaseSnapshot(world.Inventory, new[] { world.C42, world.C42 }, world.Assignments, new EligibilityOptions(true, true));

        AvailabilityResult result = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("CopperOre", 1) });

        Assert.Equal(30, result.Requirements[0].InContainers);
        Assert.Equal(new uint[] { 42 }, Ids(result.CountedContainers));
    }
}
