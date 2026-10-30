namespace BaseStorage.Core.Tests.Building;

using System.Collections.Generic;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Crafting;
using BaseStorage.Core.Tests.Fakes;

/// <summary>GW-U-1604 and GW-U-1605 (AC-3, AC-4; contract 5.3 and 5.9 BT1 fixture): the piece check's exact amount and eligibility.</summary>
public sealed class BuildAvailabilityTests
{
    private static ItemStackSnapshot Wood(int count) => CraftFixtures.Wood(count);

    private static ItemStackSnapshot Resin(int count) => CraftFixtures.Resin(count);

    // GW-U-1604 (5.3 row 1, AC-3): exactly 15 Wood across the inventory and two base chests is buildable.
    [Fact]
    public void ExactlyFifteenWoodAcrossInventoryAndBaseChestsIsBuildable()
    {
        ContainerSnapshot inventory = BuildRig.InventoryOf(Wood(3));
        ContainerSnapshot c7 = BuildRig.ChestOf(CraftFixtures.C7, 10f, ContainerFlags.PlayerBuilt, Wood(4));

        RequirementAvailability? missing = BaseAvailability.FindFirstMissing(
            CraftFixtures.Snapshot(inventory, new[] { BuildRig.C5Snapshot(), c7 }),
            BuildRig.PieceRequirements());

        Assert.Null(missing);
    }

    // GW-U-1604 (5.3 row 2, AC-3): one Wood short in total is not buildable and the Wood requirement is the first missing.
    [Fact]
    public void OneWoodShortInTotalIsNotBuildable()
    {
        ContainerSnapshot inventory = BuildRig.InventoryOf(Wood(3));
        ContainerSnapshot c7 = BuildRig.ChestOf(CraftFixtures.C7, 10f, ContainerFlags.PlayerBuilt, Wood(3));

        RequirementAvailability? missing = BaseAvailability.FindFirstMissing(
            CraftFixtures.Snapshot(inventory, new[] { BuildRig.C5Snapshot(), c7 }),
            BuildRig.PieceRequirements());

        Assert.NotNull(missing);
        Assert.Equal("Wood", missing!.Requirement.PrefabName);
    }

    // GW-U-1604 (AC-3, inventory only): Wood 15 and Resin 2 in the inventory with no base members is buildable.
    [Fact]
    public void ExactAmountsInTheInventoryAloneAreBuildable()
    {
        ContainerSnapshot inventory = BuildRig.InventoryOf(Wood(15), Resin(2));

        Assert.Null(BaseAvailability.FindFirstMissing(CraftFixtures.Snapshot(inventory, Array.Empty<ContainerSnapshot>()), BuildRig.PieceRequirements()));
    }

    // GW-U-1605 (5.3 row 3, AC-4): an ineligible chest is not counted (the Wood requirement is missing and no plan exists),
    // and the two controls with the chest eligible are buildable.
    [Theory]
    [InlineData(ContainerFlags.InUseByOther, false, true, true, true)]
    [InlineData(ContainerFlags.Vehicle, false, true, true, true)]
    [InlineData(ContainerFlags.Tombstone, false, true, true, true)]
    [InlineData(ContainerFlags.AccessDenied, false, true, true, true)]
    [InlineData(ContainerFlags.None, true, true, false, true)]
    [InlineData(ContainerFlags.Vehicle, false, false, true, false)]
    [InlineData(ContainerFlags.None, true, true, true, false)]
    public void AnIneligibleChestIsNotCounted(ContainerFlags c7Flags, bool keepC7, bool excludeVehicles, bool keepChestsCraftable, bool expectsMissing)
    {
        ContainerSnapshot inventory = BuildRig.InventoryOf(Wood(3));
        ContainerSnapshot c7 = BuildRig.ChestOf(CraftFixtures.C7, 10f, ContainerFlags.PlayerBuilt | c7Flags, Wood(10));
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        if (keepC7)
        {
            assignments[CraftFixtures.C7] = CategoryAssignment.Keep;
        }

        var snapshot = new BaseSnapshot(
            inventory,
            new[] { BuildRig.C5Snapshot(), c7 },
            assignments,
            new EligibilityOptions(excludeVehicles, keepChestsCraftable));

        RequirementAvailability? missing = BaseAvailability.FindFirstMissing(snapshot, BuildRig.PieceRequirements());
        ConsumptionPlan? plan = ConsumptionPlanner.Plan(snapshot, BuildRig.PieceRequirements());

        Assert.Equal(expectsMissing, missing is not null);
        Assert.Equal(expectsMissing, plan is null);
        if (expectsMissing)
        {
            Assert.Equal("Wood", missing!.Requirement.PrefabName);
        }
    }
}
