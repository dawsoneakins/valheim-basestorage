namespace BaseStorage.Core.Tests.Bases;

using static BaseStorage.Core.Tests.Bases.StationGraphFixtures;

/// <summary>
/// AC-2 (ADR-GWIN-016): the verified build-range rule at its boundaries. BuildRangeRule is internal, so these tests reach it only
/// through StationGraph.FindBase, using the [ADR] fixtures.
/// </summary>
public sealed class BuildRangeRuleTests
{
    // GW-U-124 (AC-2): the selector is the verified rule. Changing it must be a visible, reviewed edit.
    [Fact]
    public void VerifiedRuleIsHorizontalStrictPerAdrGwin016()
    {
        Assert.True(
            VerifiedRule == RuleVariant.HorizontalStrict,
            "The verified rule is horizontal distance with strict less-than (ADR-GWIN-016). Change it only with a new ADR.");
    }

    // GW-U-124 (AC-2): exactly 20 m is outside (strict), just inside connects, just outside does not.
    [Theory]
    [InlineData("G9a")]
    [InlineData("G9b")]
    [InlineData("G9c")]
    public void HorizontalBoundaryAtTwentyMetresIsStrict(string id)
    {
        AssertRow(id);
    }

    // GW-U-124 (AC-2): height is ignored, so a station straight up at exactly 20 m, and 20.01 m, and 19.99 m, all follow the horizontal rule.
    [Theory]
    [InlineData("G9d")]
    [InlineData("G9e")]
    [InlineData("G9f")]
    public void HeightIsIgnoredAtTheVerticalBoundary(string id)
    {
        AssertRow(id);
    }
}
