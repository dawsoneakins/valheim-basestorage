namespace BaseStorage.Core.Tests.Availability;

using System;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Availability.PlanFixtures;

/// <summary>AC-9 (ADR-GWIN-028 step 1): the planner draws only from the containers that Compute counts, so every exclusion is inherited.</summary>
public sealed class ConsumptionPlanEligibilityTests
{
    // GW-U-450 (AC-9): on 9.7 keepOn, the in-use chest 57, the tombstone 70 and chest 88 never appear in the plan.
    [Fact]
    public void KeepOnPlanNeverNamesTheInUseTombstoneOrEmptyChests()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 69));

        Assert.Equal(new[] { Take(snapshot, 1, 0, 12), Take(snapshot, 42, 1, 17), Take(snapshot, 63, 0, 40) }, plan.Takes);
        Assert.Equal(new uint[] { 42, 63 }, Ids(plan.ExpectedContainers));
        Assert.DoesNotContain(plan.Takes, take => take.ContainerId.Id == 57 || take.ContainerId.Id == 70 || take.ContainerId.Id == 88);
    }

    // GW-U-450 (AC-9): with keep off, the Keep chest 63 is excluded, so 30 wood is not plannable.
    [Fact]
    public void KeepOffPlanStopsShortOfTheKeepChest()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, false);

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 29));

        Assert.Equal(new[] { Take(snapshot, 1, 0, 12), Take(snapshot, 42, 1, 17) }, plan.Takes);
        Assert.Equal(new uint[] { 42 }, Ids(plan.ExpectedContainers));
        Assert.Null(PlanOrNull(snapshot, new CraftRequirement("Wood", 30)));
    }

    // GW-U-450 (AC-9): on keep on, leather scraps come from the inventory and the Keep chest 63.
    [Fact]
    public void KeepOnLeatherScrapsIncludesTheKeepChest()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("LeatherScraps", 13));

        Assert.Equal(new[] { Take(snapshot, 1, 2, 3), Take(snapshot, 63, 1, 10) }, plan.Takes);
    }

    // GW-U-450 (AC-9): on keep off, leather scraps are only the 3 in the inventory, so 4 is null.
    [Fact]
    public void KeepOffLeatherScrapsIsNullBeyondTheInventory()
    {
        World world = Create();

        Assert.Null(PlanOrNull(Snapshot(world, false), new CraftRequirement("LeatherScraps", 4)));
    }

    // GW-U-450 (AC-9): bronze is only in chest 57, which is in use, so there is no plan for bronze.
    [Fact]
    public void InUseBronzeIsNotPlanned()
    {
        World world = Create();

        Assert.Null(PlanOrNull(Snapshot(world, true), new CraftRequirement("Bronze", 1)));
    }

    // GW-U-451 (AC-9a): with vehicles excluded and keep off, only chest 20 is drawn from, and the other five chests are out.
    [Fact]
    public void WorldEWithVehiclesAndKeepExcludedDrawsOnlyFromChest20()
    {
        BaseSnapshot snapshot = WorldE(excludeVehicles: true, keepChestsCraftable: false);

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 1));

        Assert.Equal(new[] { Take(snapshot, 20, 0, 1) }, plan.Takes);
        Assert.Equal(new uint[] { 20 }, Ids(plan.ExpectedContainers));
        Assert.Null(PlanOrNull(snapshot, new CraftRequirement("Wood", 2)));
    }

    // GW-U-451 (AC-9b): with vehicles allowed and keep chests craftable, chests 10, 15 and 20 are drawn from, and 11 to 14 stay excluded.
    [Fact]
    public void WorldEWithVehiclesAllowedAndKeepIncludedDrawsFromTenFifteenTwenty()
    {
        BaseSnapshot snapshot = WorldE(excludeVehicles: false, keepChestsCraftable: true);

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 21));

        Assert.Equal(new[] { Take(snapshot, 10, 0, 10), Take(snapshot, 15, 0, 10), Take(snapshot, 20, 0, 1) }, plan.Takes);
        Assert.Equal(new uint[] { 10, 15, 20 }, Ids(plan.ExpectedContainers));
        Assert.Null(PlanOrNull(snapshot, new CraftRequirement("Wood", 22)));
    }

    // GW-U-451 (AC-9c): an inventory with Tombstone, InUseByOther and AccessDenied flags is still counted in full.
    [Fact]
    public void FlaggedInventoryIsStillPlannedInFull()
    {
        ContainerSnapshot inventory = Make(
            1u, 0f, 0f, 0f, 8, 4,
            ContainerFlags.Tombstone | ContainerFlags.InUseByOther | ContainerFlags.AccessDenied,
            Stack("Wood", 2, 1, 50));
        BaseSnapshot snapshot = Custom(inventory, Array.Empty<ContainerSnapshot>(), NoAssignments(), true, true);

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 2));

        Assert.Equal(new[] { Take(snapshot, 1, 0, 2) }, plan.Takes);
        Assert.Empty(plan.ExpectedContainers);
    }

    // GW-U-451 (AC-9d): with two containers that share id 20, only the first is counted, so the second's 99 wood is never planned.
    [Fact]
    public void DuplicateIdKeepsTheFirstContainerOnly()
    {
        ContainerSnapshot first = Make(20u, 50f, 0f, 0f, 5, 2, ContainerFlags.PlayerBuilt, Stack("Wood", 1, 1, 50));
        ContainerSnapshot second = Make(20u, 50f, 0f, 0f, 5, 2, ContainerFlags.PlayerBuilt, Stack("Wood", 99, 1, 50));
        BaseSnapshot snapshot = Custom(EmptyInventory(), new[] { first, second }, NoAssignments(), true, true);

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 1));

        Assert.Equal(new ConsumptionTake(Id(20), 0, first.Contents[0], 1, 0), plan.Takes[0]);
        Assert.Single(plan.Takes);
        Assert.Single(plan.ExpectedContainers);
        Assert.Same(first, plan.ExpectedContainers[0]);
        Assert.Null(PlanOrNull(snapshot, new CraftRequirement("Wood", 2)));
    }
}
