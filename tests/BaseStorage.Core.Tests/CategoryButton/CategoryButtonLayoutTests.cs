namespace BaseStorage.Core.Tests.CategoryButton;

using BaseStorage.Core.CategoryButton;

/// <summary>GW-U-1300..1303 (AC-1): the Core placement rule of the Category button (contract S-E004-004 section 2, rows of 2.1).</summary>
public sealed class CategoryButtonLayoutTests
{
    // GW-U-1300: the gap is the design-system spacing.2 and the row grows left only past the middle anchor.
    [Fact]
    public void GapIsEightAndGrowsLeftOnlyPastTheMiddleAnchor()
    {
        Assert.Equal(8f, CategoryButtonLayout.Gap);
        Assert.True(CategoryButtonLayout.GrowsLeft(1f));
        Assert.True(CategoryButtonLayout.GrowsLeft(0.51f));
        Assert.False(CategoryButtonLayout.GrowsLeft(0.5f));
        Assert.False(CategoryButtonLayout.GrowsLeft(0f));
        Assert.False(CategoryButtonLayout.GrowsLeft(float.NaN));
    }

    // GW-U-1301: the outermost sibling is the smaller x when growing left and the larger x otherwise; a missing sibling (NaN) is skipped.
    [Theory]
    [InlineData(-60f, -180f, true, -180f)]
    [InlineData(-60f, -180f, false, -60f)]
    [InlineData(-180f, -60f, true, -180f)]
    [InlineData(-180f, -60f, false, -60f)]
    [InlineData(40f, float.NaN, true, 40f)]
    [InlineData(40f, float.NaN, false, 40f)]
    [InlineData(float.NaN, 40f, true, 40f)]
    public void OuterXPicksTheOutermostSiblingInTheGrowthDirection(float first, float second, bool growsLeft, float expected)
    {
        Assert.Equal(expected, CategoryButtonLayout.OuterX(first, second, growsLeft));
    }

    // GW-U-1302: the clone sits one width plus one gap outside the outermost sibling.
    [Theory]
    [InlineData(-180f, 120f, true, 8f, -308f)]
    [InlineData(160f, 120f, false, 8f, 288f)]
    [InlineData(0f, 0f, false, 0f, 0f)]
    [InlineData(0f, 100f, true, 0f, -100f)]
    public void AnchoredXOffsetsByWidthAndGap(float outerX, float width, bool growsLeft, float gap, float expected)
    {
        Assert.Equal(expected, CategoryButtonLayout.AnchoredX(outerX, width, growsLeft, gap));
    }

    // GW-U-1303: a negative or NaN width or gap and a NaN position are rejected by name.
    [Fact]
    public void AnchoredXRejectsNegativeOrNaNArgumentsByName()
    {
        Assert.Equal("width", Assert.Throws<ArgumentOutOfRangeException>(() => CategoryButtonLayout.AnchoredX(0f, -1f, false, 8f)).ParamName);
        Assert.Equal("width", Assert.Throws<ArgumentOutOfRangeException>(() => CategoryButtonLayout.AnchoredX(0f, float.NaN, false, 8f)).ParamName);
        Assert.Equal("gap", Assert.Throws<ArgumentOutOfRangeException>(() => CategoryButtonLayout.AnchoredX(0f, 120f, false, -1f)).ParamName);
        Assert.Equal("gap", Assert.Throws<ArgumentOutOfRangeException>(() => CategoryButtonLayout.AnchoredX(0f, 120f, true, float.NaN)).ParamName);
        Assert.Equal("outerX", Assert.Throws<ArgumentOutOfRangeException>(() => CategoryButtonLayout.AnchoredX(float.NaN, 120f, false, 8f)).ParamName);
    }
}
