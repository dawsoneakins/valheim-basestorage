namespace BaseStorage.Core.Tests.Availability;

using System.Linq;
using System.Threading.Tasks;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Availability.PlanFixtures;

/// <summary>AC-12 (ADR-GWIN-028 step 2): the plan depends only on the snapshot's values, and never on caller order, and it does not mutate its inputs.</summary>
public sealed class ConsumptionPlanDeterminismTests
{
    // GW-U-454 (AC-12): the same call twice gives element-wise equal takes and expected containers.
    [Fact]
    public void SameCallTwiceGivesEqualPlans()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan first = Planned(snapshot, new CraftRequirement("Wood", 59));
        ConsumptionPlan second = Planned(snapshot, new CraftRequirement("Wood", 59));

        Assert.Equal(first.Takes, second.Takes);
        Assert.Equal(first.ExpectedContainers, second.ExpectedContainers);
    }

    // GW-U-454 (AC-12): the chest caller orders (5, 7, 2, 9) and (2, 9, 5, 7) give the same takes as the default order.
    [Fact]
    public void CallerOrderDoesNotChangeTheTakes()
    {
        ConsumptionPlan original = Planned(WorldP(), new CraftRequirement("Wood", 59));

        ConsumptionPlan ascendingOrder = Planned(WorldP(new uint[] { 5, 7, 2, 9 }), new CraftRequirement("Wood", 59));
        ConsumptionPlan rotatedOrder = Planned(WorldP(new uint[] { 2, 9, 5, 7 }), new CraftRequirement("Wood", 59));

        Assert.Equal(original.Takes, ascendingOrder.Takes);
        Assert.Equal(original.Takes, rotatedOrder.Takes);
    }

    // GW-U-454 (AC-12): with chests 5 and 7 tied at distance 5, the swapped positions still visit chest 5 first, by id.
    [Fact]
    public void TiedDistanceVisitsTheLowerIdFirst()
    {
        BaseSnapshot swapped = WorldP(c5X: 0f, c5Z: -5f, c7X: 3f, c7Z: 4f);

        ConsumptionPlan plan = Planned(swapped, new CraftRequirement("Wood", 59));

        Assert.Equal(5u, plan.Takes.First(take => take.ContainerId.Id != 1).ContainerId.Id);
    }

    // GW-U-454 (AC-12): chest 7 moved to distance 4.999 is strictly nearer than chest 5 at distance 5, so it is visited first.
    [Fact]
    public void NearerChestIsVisitedFirstEvenByALittle()
    {
        BaseSnapshot moved = WorldP(c7X: 0f, c7Z: -4.999f);

        ConsumptionPlan plan = Planned(moved, new CraftRequirement("Wood", 59));

        Assert.Equal(7u, plan.Takes.First(take => take.ContainerId.Id != 1).ContainerId.Id);
    }

    // GW-U-454 (AC-12): on 9.7 keepOn, a snapshot built from the reversed container array gives the same plan as the original.
    [Fact]
    public void ReversedContainerArrayGivesTheSamePlanOnKeepOn()
    {
        World world = Create();
        ContainerSnapshot[] reversed = Enumerable.Reverse(world.Containers).ToArray();
        BaseSnapshot reversedSnapshot = Custom(world.Inventory, reversed, world.Assignments, true, true);

        ConsumptionPlan original = Planned(Snapshot(Create(), true), new CraftRequirement("Wood", 69));
        ConsumptionPlan fromReversed = Planned(reversedSnapshot, new CraftRequirement("Wood", 69));

        Assert.Equal(original.Takes, fromReversed.Takes);
        Assert.Equal(original.ExpectedContainers, fromReversed.ExpectedContainers);
    }

    // GW-U-455 (AC-12): planning does not modify any input container, the inventory, the containers, the assignments or the requirements.
    [Fact]
    public void PlanningLeavesTheSnapshotAndRequirementsUnchanged()
    {
        BaseSnapshot snapshot = WorldP();
        BaseSnapshot fresh = WorldP();
        CraftRequirement[] requirements = { new CraftRequirement("Wood", 59) };

        Planned(snapshot, requirements);

        Assert.Equal(fresh.PlayerInventory, snapshot.PlayerInventory);
        Assert.Equal(fresh.Containers, snapshot.Containers);
        Assert.Equal(new[] { new CraftRequirement("Wood", 59) }, requirements);
    }

    // GW-U-455 (AC-12): on 9.7 keepOn, the assignments are unchanged after planning, and a second Compute still matches A1 to A5.
    [Fact]
    public void KeepOnPlanLeavesAssignmentsAndTheSecondComputeIntact()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);
        CraftRequirement[] requirements = Requirements();

        Planned(snapshot, new CraftRequirement("Wood", 69));

        Assert.Equal(Create().Assignments, snapshot.Assignments);
        AvailabilityResult result = BaseAvailability.Compute(snapshot, requirements);
        Assert.Equal(KeepOnRows, result.Requirements.Select(row => Counts(row)).ToArray());
    }

    // GW-U-455 (AC-12): planning on another thread gives the same takes as planning on this one.
    [Fact]
    public async Task PlanningOnAnotherThreadGivesTheSameTakes()
    {
        BaseSnapshot snapshot = WorldP();
        CraftRequirement[] requirements = { new CraftRequirement("Wood", 59) };
        ConsumptionPlan sameThread = Planned(snapshot, requirements);

        ConsumptionPlan? otherThread = await Task.Run(() => ConsumptionPlanner.Plan(snapshot, requirements));

        Assert.NotNull(otherThread);
        Assert.Equal(sameThread.Takes, otherThread!.Takes);
    }
}
