namespace BaseStorage.Core.Tests.Availability;

using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Availability.PlanFixtures;

/// <summary>AC-10 and AC-11 (ADR-GWIN-028 steps 5 and 7): the expected contents of touched chests, take stacks, and the amount sweep.</summary>
public sealed class ConsumptionPlanExpectationTests
{
    // GW-U-452 (AC-10): only the touched chest is recorded, as the snapshot's own instance, with its full contents.
    [Fact]
    public void OnlyTheTouchedChestIsRecordedAsTheSnapshotInstance()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("CopperOre", 30));

        Assert.Single(plan.ExpectedContainers);
        Assert.Same(world.C42, plan.ExpectedContainers[0]);
        Assert.Equal(Create().C42, plan.ExpectedContainers[0]);
        Assert.Equal(new[] { Take(snapshot, 42, 0, 30) }, plan.Takes);
    }

    // GW-U-452 (AC-10): on world P, the expected containers are in ascending id order (2, 5, 7), not visit order, and chest 9 is not recorded.
    [Fact]
    public void TouchedChestsAreRecordedInAscendingIdOrder()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 59));

        Assert.Equal(new uint[] { 2, 5, 7 }, Ids(plan.ExpectedContainers));
    }

    // GW-U-452 (AC-10): every take's stack is the source container's own stack instance, including the inventory's.
    [Fact]
    public void EveryTakeStackIsTheSourceContainersStackInstance()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 59));

        foreach (ConsumptionTake take in plan.Takes)
        {
            ContainerSnapshot source = Source(snapshot, take);
            Assert.Same(source.Contents[take.StackIndex], take.Stack);
        }
    }

    // GW-U-452 (AC-10): every expected container has at least one take that names it.
    [Fact]
    public void EveryExpectedContainerHasATake()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 59));

        foreach (ContainerSnapshot expected in plan.ExpectedContainers)
        {
            Assert.Contains(plan.Takes, take => take.ContainerId == expected.Id);
        }
    }

    // GW-U-453 (AC-11): for every amount from 0 to 120 on world P, the plan is null above 113, and below that the invariants hold.
    [Fact]
    public void InvariantsHoldForEveryWoodAmountFromZeroToOneHundredTwenty()
    {
        for (int amount = 0; amount <= 120; amount++)
        {
            BaseSnapshot snapshot = WorldP();
            CraftRequirement[] requirements = { new CraftRequirement("Wood", amount) };
            ConsumptionPlan? plan = ConsumptionPlanner.Plan(snapshot, requirements);

            if (amount > 113)
            {
                Assert.True(plan is null, $"Wood {amount} exceeds the 113 in the world and must have no plan.");
                continue;
            }

            Assert.True(plan is not null, $"Wood {amount} is in the world and must have a plan.");
            ConsumptionPlan made = plan!;
            AssertInvariants(snapshot, requirements, made, "Wood " + amount);
            foreach (ConsumptionTake take in made.Takes)
            {
                bool fromWoodSource = take.ContainerId == made.PlayerInventoryId || new uint[] { 2, 5, 7, 9 }.Contains(take.ContainerId.Id);
                Assert.True(fromWoodSource, $"Wood {amount}: a take names container {take.ContainerId.Id}, which is not in the world's wood chests.");
            }
        }
    }

    // GW-U-453 (AC-11): the same invariants hold per requirement index on the GW-U-445 plan.
    [Fact]
    public void InvariantsHoldPerRequirementIndexOnTheQualityPlan()
    {
        BaseSnapshot snapshot = WorldQ();
        CraftRequirement[] requirements =
        {
            new CraftRequirement("Wood", 8),
            new CraftRequirement("SwordBronze", 1, QualityMatch.Exact, 3),
            new CraftRequirement("SwordBronze", 1),
            new CraftRequirement("Resin", 9),
        };

        ConsumptionPlan plan = Planned(snapshot, requirements);

        AssertInvariants(snapshot, requirements, plan, "GW-U-445 plan");
    }

    // GW-U-453 (AC-11): the same invariants hold per requirement index on the GW-U-446(a) plan.
    [Fact]
    public void InvariantsHoldPerRequirementIndexOnTheOverlappingWoodPlan()
    {
        BaseSnapshot snapshot = WorldQ();
        CraftRequirement[] requirements = { new CraftRequirement("Wood", 8), new CraftRequirement("Wood", 4) };

        ConsumptionPlan plan = Planned(snapshot, requirements);

        AssertInvariants(snapshot, requirements, plan, "GW-U-446(a) plan");
    }
}
