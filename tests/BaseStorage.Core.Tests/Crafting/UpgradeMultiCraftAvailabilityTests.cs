namespace BaseStorage.Core.Tests.Crafting;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using Xunit;

/// <summary>
/// GW-U-1701 to GW-U-1708 (AC-1 to AC-6; contract upgrade-multicraft 4.1 to 4.3): the quality-scaled and multiplied requirement lists
/// through the real BaseAvailability and ConsumptionPlanner over the literal tables. Expected values are literals of the contract.
/// </summary>
public sealed class UpgradeMultiCraftAvailabilityTests
{
    /// <summary>The target quality rows of contract 4.1: quality, and the Bronze, Wood and LeatherScraps amounts.</summary>
    /// <returns>The rows.</returns>
    public static TheoryData<int, int, int, int> QualityLines() => new()
    {
        { 2, 4, 1, 1 },
        { 3, 8, 2, 2 },
        { 4, 16, 4, 4 },
    };

    /// <summary>The multiplier rows M1 to M6 of contract 4.3.</summary>
    /// <returns>One argument per row: the multiplier.</returns>
    public static IEnumerable<object[]> Multipliers() => Enumerable.Range(1, 6).Select(m => new object[] { m });

    /// <summary>The multiplier rows 2 to 6 of contract 4.3 (the AC-5 multiplier assertion covers amounts 2 to 6).</summary>
    /// <returns>One argument per row: the multiplier.</returns>
    public static IEnumerable<object[]> MultipliersFromTwo() => Enumerable.Range(2, 5).Select(m => new object[] { m });

    /// <summary>The upgrade rows Q1, Q3, Q4 and Q6 that are met: the Bronze in chest c5, the Bronze in chest c7, the target quality.</summary>
    /// <returns>The rows.</returns>
    public static TheoryData<int, int, int> MetQualityRows() => new()
    {
        { 7, 4, 4 },
        { 7, 3, 3 },
        { 3, 0, 3 },
        { 7, 4, 2 },
    };

    // GW-U-1701 (AC-1, 4.1): the quality-scaled Bronze sword list for q = 2, 3 and 4 is the vanilla table, each line matches any quality.
    [Theory]
    [MemberData(nameof(QualityLines))]
    public void TheSwordRequirementListAtEachTargetQualityIsTheVanillaTable(int q, int bronze, int wood, int leather)
    {
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.SwordLines(q), 1);

        Assert.Equal(new[] { ("Bronze", bronze), ("Wood", wood), ("LeatherScraps", leather) }, reqs.Select(req => (req.PrefabName, req.Amount)).ToArray());
        Assert.All(reqs, req => Assert.Equal(QualityMatch.Any, req.QualityMatch));
    }

    // GW-U-1701 (case-count guard): the quality table has the three rows of contract 4.1.
    [Fact]
    public void TheQualityTableHasThreeRows()
    {
        Assert.Equal(3, QualityLines().Count());
    }

    // GW-U-1702 (AC-1, AC-6, Q1 exact at level 4): World U(7, 4) meets the level 4 Bronze need exactly, and the plan takes exactly 16 Bronze.
    [Fact]
    public void TheLevelFourExactBoundaryIsSufficientAndPlansExactlyThatAmount()
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(7, 4));
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.SwordLines(4), 1);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, reqs);
        Assert.Equal(5, result.Requirements[0].InInventory);
        Assert.Equal(11, result.Requirements[0].InContainers);
        Assert.True(result.Requirements[0].IsMet);
        Assert.Equal(0, result.Requirements[0].Missing);
        Assert.Null(BaseAvailability.FindFirstMissing(snapshot, reqs));

        ConsumptionPlan plan = ConsumptionPlanner.Plan(snapshot, reqs) ?? throw new InvalidOperationException("The exact level 4 need has no plan.");
        Assert.Equal(
            new[]
            {
                (CraftFixtures.Inventory, 0, 5, 0),
                (CraftFixtures.C5, 0, 7, 0),
                (CraftFixtures.C7, 0, 4, 0),
                (CraftFixtures.Inventory, 1, 4, 1),
                (CraftFixtures.Inventory, 2, 4, 2),
            },
            TakeRows(plan));
        Assert.Equal(new[] { CraftFixtures.C7, CraftFixtures.C5 }, plan.ExpectedContainers.Select(container => container.Id).ToArray());
        Assert.Equal(reqs, plan.Requirements);
        Assert.All(Enumerable.Range(0, reqs.Count), index => Assert.Equal(reqs[index].Amount, plan.Takes.Where(take => take.RequirementIndex == index).Sum(take => take.Count)));
        Assert.Equal(0, UpgradeMultiCraftFixtures.Apply(snapshot, plan.Takes)["Bronze"]);
    }

    // GW-U-1703 (AC-6, Q2 one less at level 4): World U(7, 3) is one Bronze short, names Bronze as the first missing line, and has no plan.
    [Fact]
    public void OneLessThanTheLevelFourBoundaryIsShortAndHasNoPlan()
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(7, 3));
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.SwordLines(4), 1);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, reqs);
        Assert.False(result.Requirements[0].IsMet);
        Assert.Equal(1, result.Requirements[0].Missing);

        RequirementAvailability? first = BaseAvailability.FindFirstMissing(snapshot, reqs);
        Assert.Equal(new RequirementAvailability(new CraftRequirement("Bronze", 16), 5, 10), first);
        Assert.Equal(result.FirstMissing, first);
        Assert.Null(ConsumptionPlanner.Plan(snapshot, reqs));
    }

    // GW-U-1704 (AC-6, Q3 level 3 surplus): World U(7, 3) meets the level 3 need with surplus, takes c5 only, and leaves c7 untouched.
    [Fact]
    public void ALevelThreeSurplusIsMetAndPlansFromChestFiveOnly()
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(7, 3));
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.SwordLines(3), 1);

        Assert.Null(BaseAvailability.FindFirstMissing(snapshot, reqs));
        ConsumptionPlan plan = ConsumptionPlanner.Plan(snapshot, reqs) ?? throw new InvalidOperationException("The level 3 surplus has no plan.");
        Assert.Equal(
            new[] { (CraftFixtures.Inventory, 0, 5, 0), (CraftFixtures.C5, 0, 3, 0), (CraftFixtures.Inventory, 1, 2, 1), (CraftFixtures.Inventory, 2, 2, 2) },
            TakeRows(plan));
        Assert.Equal(new[] { CraftFixtures.C5 }, plan.ExpectedContainers.Select(container => container.Id).ToArray());
    }

    // GW-U-1704 (AC-6, Q4 level 3 exact): World U(3, 0) meets the level 3 need exactly, the same takes, and the Bronze left is 0.
    [Fact]
    public void ALevelThreeExactBoundaryIsMetAndLeavesNoBronze()
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(3, 0));
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.SwordLines(3), 1);

        Assert.Null(BaseAvailability.FindFirstMissing(snapshot, reqs));
        ConsumptionPlan plan = ConsumptionPlanner.Plan(snapshot, reqs) ?? throw new InvalidOperationException("The level 3 exact need has no plan.");
        Assert.Equal(
            new[] { (CraftFixtures.Inventory, 0, 5, 0), (CraftFixtures.C5, 0, 3, 0), (CraftFixtures.Inventory, 1, 2, 1), (CraftFixtures.Inventory, 2, 2, 2) },
            TakeRows(plan));
        Assert.Equal(0, UpgradeMultiCraftFixtures.Apply(snapshot, plan.Takes)["Bronze"]);
    }

    // GW-U-1704 (AC-6, Q5 level 3 one less): World U(2, 0) is two Bronze in chest c5 short of 8, with the Bronze 8 row first missing and no plan.
    [Fact]
    public void ALevelThreeOneLessIsShortWithTheBronzeRowFirstMissing()
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(2, 0));
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.SwordLines(3), 1);

        Assert.Equal(new RequirementAvailability(new CraftRequirement("Bronze", 8), 5, 2), BaseAvailability.FindFirstMissing(snapshot, reqs));
        Assert.Null(ConsumptionPlanner.Plan(snapshot, reqs));
    }

    // GW-U-1705 (AC-1, Q6 inventory only at level 2): World U(7, 4) is met from the inventory alone, so no chest is listed (CF7).
    [Fact]
    public void AnInventoryOnlyUpgradeAtLevelTwoPlansNoChest()
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(7, 4));
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.SwordLines(2), 1);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, reqs);
        Assert.Equal(5, result.Requirements[0].InInventory);
        Assert.Equal(11, result.Requirements[0].InContainers);
        Assert.True(result.Requirements[0].IsMet);

        ConsumptionPlan plan = ConsumptionPlanner.Plan(snapshot, reqs) ?? throw new InvalidOperationException("The inventory holds the level 2 need.");
        Assert.Equal(new[] { (CraftFixtures.Inventory, 0, 4, 0), (CraftFixtures.Inventory, 1, 1, 1), (CraftFixtures.Inventory, 2, 1, 2) }, TakeRows(plan));
        Assert.Empty(plan.ExpectedContainers);
    }

    // GW-U-1706 (AC-3, AC-5, M1-M6): World A meets the multiplied arrow need, and the plan takes exactly the M1-M6 rows of contract 4.3.
    [Theory]
    [MemberData(nameof(Multipliers))]
    public void TheMultipliedArrowNeedIsMetAndPlansTheContractTakes(int m)
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldA());
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.ArrowLines(), m);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, reqs);
        Assert.Equal(new[] { 10, 38 }, new[] { result.Requirements[0].InInventory, result.Requirements[0].InContainers });
        Assert.Equal(new[] { 4, 8 }, new[] { result.Requirements[1].InInventory, result.Requirements[1].InContainers });
        Assert.True(result.AllMet);

        ConsumptionPlan plan = ConsumptionPlanner.Plan(snapshot, reqs) ?? throw new InvalidOperationException("The multiplied need has no plan.");
        Assert.Equal(ExpectedMultiplierTakes(m), TakeRows(plan));
        Assert.Equal(ExpectedMultiplierContainers(m), plan.ExpectedContainers.Select(container => container.Id).ToArray());
    }

    // GW-U-1706 (case-count guard): the multiplier table has the six rows M1 to M6 of contract 4.3.
    [Fact]
    public void TheMultiplierTableHasSixRows()
    {
        Assert.Equal(6, Multipliers().Count());
        Assert.Equal(5, MultipliersFromTwo().Count());
    }

    // GW-U-1706 (AC-5, the multiplier test): the availability requirement and the plan carry per-craft x m, and the takes of each requirement sum to it.
    [Theory]
    [MemberData(nameof(MultipliersFromTwo))]
    public void TheMultiplierIsInTheAvailabilityResultAndTheConsumptionPlan(int m)
    {
        int[] perCraft = { 8, 2 };
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldA());
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.ArrowLines(), m);
        AvailabilityResult result = BaseAvailability.Compute(snapshot, reqs);
        ConsumptionPlan plan = ConsumptionPlanner.Plan(snapshot, reqs) ?? throw new InvalidOperationException("The multiplied need has no plan.");

        for (int index = 0; index < perCraft.Length; index++)
        {
            Assert.Equal(perCraft[index] * m, result.Requirements[index].Requirement.Amount);
            Assert.Equal(perCraft[index] * m, plan.Requirements[index].Amount);
            Assert.Equal(perCraft[index] * m, plan.Takes.Where(take => take.RequirementIndex == index).Sum(take => take.Count));
        }
    }

    // GW-U-1707 (AC-3, AC-6, M7 short x7): Wood is first missing at 56 with 10 in the inventory and 38 in the chests; Feathers is also short; no plan.
    [Fact]
    public void SevenArrowsAreShortOnWoodFirst()
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldA());
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.ArrowLines(), 7);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, reqs);
        Assert.False(result.AllMet);
        Assert.Equal(new RequirementAvailability(new CraftRequirement("Wood", 56), 10, 38), BaseAvailability.FindFirstMissing(snapshot, reqs));
        Assert.Equal(2, result.Requirements[1].Missing);
        Assert.Null(ConsumptionPlanner.Plan(snapshot, reqs));
    }

    // GW-U-1707 (AC-6, one step past the exact boundary): 48 Wood are there for six crafts; seven crafts need 56, so the Wood row is short.
    [Fact]
    public void OneStepPastTheSixCraftBoundaryIsShort()
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldA());
        IReadOnlyList<CraftRequirement> six = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.ArrowLines(), 6);
        IReadOnlyList<CraftRequirement> seven = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.ArrowLines(), 7);

        Assert.Equal(48, BaseAvailability.Compute(snapshot, six).Requirements[0].Total);
        Assert.Equal(48, UpgradeMultiCraftFixtures.Totals(snapshot)["Wood"]);
        Assert.Equal(new RequirementAvailability(new CraftRequirement("Wood", 56), 10, 38), BaseAvailability.FindFirstMissing(snapshot, seven));
    }

    // GW-U-1708 (AC-2, 4.2 tail): on every met upgrade row no take names SwordBronze, and the takes leave total minus need with no negative count.
    [Theory]
    [MemberData(nameof(MetQualityRows))]
    public void EveryMetUpgradeRowLeavesTheResidualAndNeverTakesTheUpgradeItem(int bronzeInChestFive, int bronzeInChestSeven, int q)
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(bronzeInChestFive, bronzeInChestSeven));
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.SwordLines(q), 1);
        ConsumptionPlan plan = ConsumptionPlanner.Plan(snapshot, reqs) ?? throw new InvalidOperationException("The met upgrade row has no plan.");

        Assert.DoesNotContain(plan.Takes, take => take.Stack.PrefabName == "SwordBronze");
        AssertResidual(snapshot, reqs, plan);
    }

    // GW-U-1708 (AC-4, 4.3 tail): on every multiplied row M1 to M6 the takes leave total minus per-craft x m with no negative count.
    [Theory]
    [MemberData(nameof(Multipliers))]
    public void EveryMultipliedRowLeavesTheResidualWithNoNegativeCount(int m)
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldA());
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.ArrowLines(), m);
        ConsumptionPlan plan = ConsumptionPlanner.Plan(snapshot, reqs) ?? throw new InvalidOperationException("The multiplied need has no plan.");

        AssertResidual(snapshot, reqs, plan);
    }

    // GW-U-1708 (case-count guard): the upgrade table has the four met rows Q1, Q3, Q4 and Q6.
    [Fact]
    public void TheMetUpgradeTableHasFourRows()
    {
        Assert.Equal(4, MetQualityRows().Count());
    }

    // GW-U-1706 (AC-5, M6 residual): after six crafts no Wood and no Feathers are left anywhere.
    [Fact]
    public void SixCraftsLeaveNoWoodAndNoFeathers()
    {
        BaseSnapshot snapshot = UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldA());
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(UpgradeMultiCraftFixtures.ArrowLines(), 6);
        ConsumptionPlan plan = ConsumptionPlanner.Plan(snapshot, reqs) ?? throw new InvalidOperationException("The exact six craft need has no plan.");

        var residual = UpgradeMultiCraftFixtures.Apply(snapshot, plan.Takes);
        Assert.Equal(0, residual["Wood"]);
        Assert.Equal(0, residual["Feathers"]);
    }

    private static (ContainerId Id, int Slot, int Count, int Req)[] TakeRows(ConsumptionPlan plan) =>
        plan.Takes.Select(take => (take.ContainerId, take.StackIndex, take.Count, take.RequirementIndex)).ToArray();

    private static void AssertResidual(BaseSnapshot snapshot, IReadOnlyList<CraftRequirement> reqs, ConsumptionPlan plan)
    {
        var before = UpgradeMultiCraftFixtures.Totals(snapshot);
        var residual = UpgradeMultiCraftFixtures.Apply(snapshot, plan.Takes);

        foreach (string prefab in before.Keys)
        {
            int need = reqs.Where(req => req.PrefabName == prefab).Sum(req => req.Amount);
            Assert.Equal(before[prefab] - need, residual[prefab]);
            Assert.True(residual[prefab] >= 0, $"{prefab} has a negative count.");
        }
    }

    private static (ContainerId Id, int Slot, int Count, int Req)[] ExpectedMultiplierTakes(int m) => m switch
    {
        1 => new[] { (CraftFixtures.Inventory, 0, 8, 0), (CraftFixtures.Inventory, 1, 2, 1) },
        2 => new[] { (CraftFixtures.Inventory, 0, 10, 0), (CraftFixtures.C5, 0, 6, 0), (CraftFixtures.Inventory, 1, 4, 1) },
        3 => new[] { (CraftFixtures.Inventory, 0, 10, 0), (CraftFixtures.C5, 0, 14, 0), (CraftFixtures.Inventory, 1, 4, 1), (CraftFixtures.C5, 1, 2, 1) },
        4 => new[]
        {
            (CraftFixtures.Inventory, 0, 10, 0), (CraftFixtures.C5, 0, 20, 0), (CraftFixtures.C7, 0, 2, 0),
            (CraftFixtures.Inventory, 1, 4, 1), (CraftFixtures.C5, 1, 3, 1), (CraftFixtures.C7, 1, 1, 1),
        },
        5 => new[]
        {
            (CraftFixtures.Inventory, 0, 10, 0), (CraftFixtures.C5, 0, 20, 0), (CraftFixtures.C7, 0, 10, 0),
            (CraftFixtures.Inventory, 1, 4, 1), (CraftFixtures.C5, 1, 3, 1), (CraftFixtures.C7, 1, 3, 1),
        },
        6 => new[]
        {
            (CraftFixtures.Inventory, 0, 10, 0), (CraftFixtures.C5, 0, 20, 0), (CraftFixtures.C7, 0, 18, 0),
            (CraftFixtures.Inventory, 1, 4, 1), (CraftFixtures.C5, 1, 3, 1), (CraftFixtures.C7, 1, 5, 1),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(m)),
    };

    private static ContainerId[] ExpectedMultiplierContainers(int m) => m switch
    {
        1 => Array.Empty<ContainerId>(),
        2 or 3 => new[] { CraftFixtures.C5 },
        4 or 5 or 6 => new[] { CraftFixtures.C7, CraftFixtures.C5 },
        _ => throw new ArgumentOutOfRangeException(nameof(m)),
    };
}
