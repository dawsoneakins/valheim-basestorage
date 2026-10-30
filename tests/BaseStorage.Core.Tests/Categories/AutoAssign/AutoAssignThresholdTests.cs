namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-7 (A4, A5; contract 5.3): the threshold boundary is exact, and it is measured in stacks.</summary>
public sealed class AutoAssignThresholdTests
{
    // GW-U-522
    [Theory]
    [InlineData("50 M,M,W,St", 50, "M,M,W,St", AutoAssignOutcome.Assigned, "Metals & Ores")]
    [InlineData("50 M,W,St,F", 50, "M,W,St,F", AutoAssignOutcome.SourceOnly, null)]
    [InlineData("50 M,M,M,W,St", 50, "M,M,M,W,St", AutoAssignOutcome.Assigned, "Metals & Ores")]
    [InlineData("50 M,M,W,St,F", 50, "M,M,W,St,F", AutoAssignOutcome.SourceOnly, null)]
    [InlineData("75 M,M,M,W", 75, "M,M,M,W", AutoAssignOutcome.Assigned, "Metals & Ores")]
    [InlineData("75 M,M,W,St", 75, "M,M,W,St", AutoAssignOutcome.SourceOnly, null)]
    [InlineData("50 W", 50, "W", AutoAssignOutcome.Assigned, "Wood")]
    [InlineData("100 W,W", 100, "W,W", AutoAssignOutcome.Assigned, "Wood")]
    [InlineData("100 W,W,St", 100, "W,W,St", AutoAssignOutcome.SourceOnly, null)]
    [InlineData("101 W", 101, "W", AutoAssignOutcome.SourceOnly, null)]
    public void ThresholdBoundaryIsExactIntegerArithmetic(
        string label,
        int threshold,
        string codes,
        AutoAssignOutcome outcome,
        string? name)
    {
        AutoAssignResult r = ResolveCodes(threshold, codes);

        AssertChest(label, outcome, name, At(r, 201));
    }

    // GW-U-523
    [Fact]
    public void PercentIsOverStacksNotItemQuantityForChest111()
    {
        AutoAssignResult r = ResolveB();

        AssertChest("chest 111 (3 Wood stacks of 1, 1 Stone stack of 50)", AutoAssignOutcome.Assigned, "Wood", At(r, 111));
    }

    // GW-U-523
    [Fact]
    public void StackCountDecidesWhateverTheQuantityOfEachStack()
    {
        AutoAssignResult r = ResolveOne(C(201, 0, 0, S("Stone", 999), S("Wood", 1), S("Wood", 1)), 50);

        AssertChest("Stone 999, Wood 1, Wood 1 at 50", AutoAssignOutcome.Assigned, "Wood", At(r, 201));
    }

    // GW-U-523
    [Fact]
    public void QualityAndMaxStackOfTheStoneStackDoNotChangeTheOutcome()
    {
        ItemStackSnapshot stone = new ItemStackSnapshot("Stone", 50, 4, 999);

        AutoAssignResult r = ResolveOne(C(111, 12, 0, S("Wood", 1), S("Wood", 1), S("Wood", 1), stone), 50);

        AssertChest("chest 111 with Quality 4 and MaxStack 999 on Stone", AutoAssignOutcome.Assigned, "Wood", At(r, 111));
    }
}
