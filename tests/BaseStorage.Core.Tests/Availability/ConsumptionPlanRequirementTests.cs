namespace BaseStorage.Core.Tests.Availability;

using System;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Availability.PlanFixtures;

/// <summary>AC-5, AC-6, AC-8 and AC-13 (ADR-GWIN-028 steps 5 and 6): several requirements, exact amounts, no partial plans and zero amounts.</summary>
public sealed class ConsumptionPlanRequirementTests
{
    // GW-U-445 (AC-5): each requirement type is matched on its own: the exact q3 sword takes slot 0, then the any-quality sword skips it.
    [Fact]
    public void SeveralRequirementTypesEachMatchIndependently()
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

        Assert.Equal(
            new[]
            {
                Take(snapshot, 1, 0, 5, 0),
                Take(snapshot, 4, 2, 3, 0),
                Take(snapshot, 4, 0, 1, 1),
                Take(snapshot, 4, 1, 1, 2),
                Take(snapshot, 4, 3, 9, 3),
            },
            plan.Takes);
        Assert.Equal(new uint[] { 4 }, Ids(plan.ExpectedContainers));
        Assert.Equal(requirements, plan.Requirements);
    }

    // GW-U-445 (AC-5): an at-least-quality-2 sword is paid from the q3 stack alone.
    [Fact]
    public void AtLeastQualityTwoTakesTheQualityThreeStack()
    {
        BaseSnapshot snapshot = WorldQ();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("SwordBronze", 1, QualityMatch.AtLeast, 2));

        Assert.Equal(new[] { Take(snapshot, 4, 0, 1, 0) }, plan.Takes);
    }

    // GW-U-446 (AC-5): two requirements on the same wood stack each take part of it, and together they count the stack once.
    [Fact]
    public void TwoRequirementsOnOneStackSumToAtMostTheStackCount()
    {
        BaseSnapshot snapshot = WorldQ();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 8), new CraftRequirement("Wood", 4));

        Assert.Equal(new[] { Take(snapshot, 1, 0, 5, 0), Take(snapshot, 4, 2, 3, 0), Take(snapshot, 4, 2, 4, 1) }, plan.Takes);
        Assert.Equal(7, plan.Takes.Where(take => take.ContainerId.Id == 4 && take.StackIndex == 2).Sum(take => take.Count));
    }

    // GW-U-446 (AC-5): 12 wood in all cannot pay 13, even though each requirement alone is met (no double counting).
    [Fact]
    public void OverlappingRequirementsAreNotPlannedWhenTheTotalIsShort()
    {
        BaseSnapshot snapshot = WorldQ();

        Assert.Null(PlanOrNull(snapshot, new[] { new CraftRequirement("Wood", 8), new CraftRequirement("Wood", 5) }));
        Assert.True(BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("Wood", 8), new CraftRequirement("Wood", 5) }).AllMet);
    }

    // GW-U-446 (AC-5): 113 wood in all cannot pay 120 across two requirements.
    [Fact]
    public void TwoRequirementsThatTogetherExceedTheWoodAreNull()
    {
        BaseSnapshot snapshot = WorldP();

        Assert.Null(PlanOrNull(snapshot, new[] { new CraftRequirement("Wood", 60), new CraftRequirement("Wood", 60) }));
    }

    // GW-U-446 (AC-5, ADR-GWIN-028 known limitation): allocation is greedy in input order, so the any-quality sword takes the q3 stack first.
    [Fact]
    public void GreedyAllocationInInputOrderIsPinned()
    {
        BaseSnapshot snapshot = WorldQ();

        Assert.Null(PlanOrNull(
            snapshot,
            new[] { new CraftRequirement("SwordBronze", 1), new CraftRequirement("SwordBronze", 1, QualityMatch.Exact, 3) }));
    }

    // GW-U-447 (AC-6): an amount equal to every item in the snapshot is planned, and one more is null.
    [Fact]
    public void ExactBoundaryOnWorldPIsPlannedAndOneMoreIsNull()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 113));

        Assert.Equal(
            new[]
            {
                Take(snapshot, 1, 2, 4), Take(snapshot, 1, 0, 10), Take(snapshot, 5, 1, 2), Take(snapshot, 5, 0, 8),
                Take(snapshot, 5, 2, 8), Take(snapshot, 7, 1, 6), Take(snapshot, 7, 0, 20), Take(snapshot, 2, 0, 50),
                Take(snapshot, 9, 0, 5),
            },
            plan.Takes);
        Assert.Equal(new uint[] { 2, 5, 7, 9 }, Ids(plan.ExpectedContainers));
        Assert.Null(PlanOrNull(snapshot, new CraftRequirement("Wood", 114)));
    }

    // GW-U-447 (AC-6): on 9.7 keepOn, 69 wood is the exact boundary and 70 is null.
    [Fact]
    public void ExactBoundaryOnKeepOnIsPlannedAndOneMoreIsNull()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);

        Assert.NotNull(PlanOrNull(snapshot, new CraftRequirement("Wood", 69)));
        Assert.Null(PlanOrNull(snapshot, new CraftRequirement("Wood", 70)));
    }

    // GW-U-447 (AC-6): on world Q, 9 resin is the exact stack and 10 is null.
    [Fact]
    public void ExactResinStackIsPlannedAndOneMoreIsNull()
    {
        BaseSnapshot snapshot = WorldQ();

        Assert.Equal(new[] { Take(snapshot, 4, 3, 9) }, Planned(snapshot, new CraftRequirement("Resin", 9)).Takes);
        Assert.Null(PlanOrNull(snapshot, new CraftRequirement("Resin", 10)));
    }

    // GW-U-449 (AC-8): a plan that is short on several requirements returns null and does not throw.
    [Fact]
    public void NineSevenKeepOnShortfallIsNullWithoutAnException()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);
        ConsumptionPlan? plan = null;

        Exception? thrown = Record.Exception(() => plan = PlanOrNull(snapshot, Requirements()));

        Assert.Null(thrown);
        Assert.Null(plan);
    }

    // GW-U-449 (AC-8): wood and stone are covered but silver is not, so the whole plan is null, with no partial result.
    [Fact]
    public void OneUncoveredRequirementMakesThePlanNullWithoutAnException()
    {
        BaseSnapshot snapshot = WorldP();
        ConsumptionPlan? plan = null;

        Exception? thrown = Record.Exception(() => plan = PlanOrNull(
            snapshot,
            new[] { new CraftRequirement("Wood", 3), new CraftRequirement("Stone", 3), new CraftRequirement("Silver", 1) }));

        Assert.Null(thrown);
        Assert.Null(plan);
    }

    // GW-U-449 (AC-8): 114 wood is short by one, and the second requirement is never reached.
    [Fact]
    public void ShortByOneOnTheFirstRequirementIsNullWithoutAnException()
    {
        BaseSnapshot snapshot = WorldP();
        ConsumptionPlan? plan = null;

        Exception? thrown = Record.Exception(() => plan = PlanOrNull(
            snapshot,
            new[] { new CraftRequirement("Wood", 114), new CraftRequirement("Wood", 1) }));

        Assert.Null(thrown);
        Assert.Null(plan);
    }

    // GW-U-456 (AC-13): a zero amount of wood is a valid, empty plan, not a failure.
    [Fact]
    public void ZeroWoodGivesAnEmptyPlan()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan? plan = PlanOrNull(snapshot, new[] { new CraftRequirement("Wood", 0) });

        Assert.NotNull(plan);
        Assert.Empty(plan!.Takes);
        Assert.Empty(plan.ExpectedContainers);
    }

    // GW-U-456 (AC-13): an absent prefab with amount 0 is met, and gives an empty plan.
    [Fact]
    public void ZeroOfAnAbsentPrefabGivesAnEmptyPlan()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan? plan = PlanOrNull(snapshot, new[] { new CraftRequirement("Silver", 0) });

        Assert.NotNull(plan);
        Assert.Empty(plan!.Takes);
    }

    // GW-U-456 (AC-13): a zero requirement emits nothing, and the indices of the other takes still refer to input positions.
    [Fact]
    public void ZeroRequirementEmitsNothingAndKeepsInputIndices()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 0), new CraftRequirement("Wood", 14));

        Assert.Equal(new[] { Take(snapshot, 1, 2, 4, 1), Take(snapshot, 1, 0, 10, 1) }, plan.Takes);
    }

    // GW-U-456 (AC-13): an empty requirement list is a valid empty plan.
    [Fact]
    public void EmptyRequirementListGivesAnEmptyPlan()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan? plan = PlanOrNull(snapshot, Array.Empty<CraftRequirement>());

        Assert.NotNull(plan);
        Assert.Empty(plan!.Takes);
        Assert.Empty(plan.ExpectedContainers);
        Assert.Empty(plan.Requirements);
    }

    // GW-U-456 (AC-13): on 9.7 keepOn, bronze at amount 0 is met even though no bronze is counted, so it is not null.
    [Fact]
    public void ZeroBronzeOnKeepOnIsNotNull()
    {
        World world = Create();

        ConsumptionPlan? plan = PlanOrNull(Snapshot(world, true), new[] { new CraftRequirement("Bronze", 0) });

        Assert.NotNull(plan);
        Assert.Empty(plan!.Takes);
    }
}
