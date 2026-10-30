namespace BaseStorage.Core.Tests.StationPanel;

using System;
using BaseStorage.Core.StationPanel;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-1 (contract 2, R5, R7): PanelRect construction, derived edges, value equality, Overlaps, Inflate and validation.</summary>
public sealed class PanelRectTests
{
    // GW-U-520 (AC-1): the contract 2 example rows. Right and Top are derived, equal rectangles agree on every equality path, -0f is stored as +0f,
    // negative origins and zero sizes are accepted, and changing one field at a time gives inequality.
    [Fact]
    public void ConstructionDerivesEdgesAndHasValueEquality()
    {
        var rect = new PanelRect(600f, 244f, 180f, 46f);
        Assert.Equal(600f, rect.X);
        Assert.Equal(244f, rect.Y);
        Assert.Equal(780f, rect.Right);
        Assert.Equal(290f, rect.Top);

        ValueSemantics.AssertEqual(rect, new PanelRect(600f, 244f, 180f, 46f));
        Assert.True(new PanelRect(600f, 244f, 180f, 46f) == new PanelRect(600f, 244f, 180f, 46f));
    }

    [Fact]
    public void NegativeZeroIsStoredAsPositiveZeroAndEqualsTheDefault()
    {
        var negativeZero = new PanelRect(-0f, 0f, 0f, 0f);
        Assert.Equal(BitConverter.SingleToInt32Bits(0f), BitConverter.SingleToInt32Bits(negativeZero.X));
        ValueSemantics.AssertEqual(negativeZero, default(PanelRect));
        Assert.True(negativeZero == default(PanelRect));
        Assert.Equal(default(PanelRect).GetHashCode(), negativeZero.GetHashCode());
    }

    [Fact]
    public void NegativeOriginsAndZeroSizesAreAccepted()
    {
        Assert.Null(Record.Exception(() => new PanelRect(-500f, -3f, 0f, 0f)));
        Assert.Null(Record.Exception(() => new PanelRect(0f, 0f, 0f, 46f)));
        Assert.Equal(-500f, new PanelRect(-500f, -3f, 0f, 0f).X);
    }

    [Fact]
    public void ChangingOneFieldAtATimeGivesInequality()
    {
        var baseRect = new PanelRect(600f, 244f, 180f, 46f);
        ValueSemantics.AssertNotEqual(baseRect, new PanelRect(601f, 244f, 180f, 46f));
        ValueSemantics.AssertNotEqual(baseRect, new PanelRect(600f, 245f, 180f, 46f));
        ValueSemantics.AssertNotEqual(baseRect, new PanelRect(600f, 244f, 181f, 46f));
        ValueSemantics.AssertNotEqual(baseRect, new PanelRect(600f, 244f, 180f, 47f));
        ValueSemantics.AssertNullHandling(typeof(PanelRect), baseRect);
    }

    // GW-U-521 (AC-1): Inflate, strict Overlaps (touching is not overlap), symmetry, and validation with the exact ParamName.
    [Fact]
    public void InflateGrowsEveryEdgeByTheAmount()
    {
        var rect = new PanelRect(600f, 244f, 180f, 46f);
        Assert.Equal(new PanelRect(592f, 236f, 196f, 62f), rect.Inflate(8f));
        Assert.Equal(788f, rect.Inflate(8f).Right);
        Assert.Equal(298f, rect.Inflate(8f).Top);
    }

    [Fact]
    public void OverlapsTreatsSharedEdgesAsNotOverlapping()
    {
        PanelRect inflatedCraft = new PanelRect(592f, 236f, 196f, 62f);
        Assert.False(new PanelRect(788f, 244f, 180f, 46f).Overlaps(inflatedCraft));
        Assert.True(new PanelRect(787f, 244f, 180f, 46f).Overlaps(inflatedCraft));
    }

    [Fact]
    public void ZeroSizeRectOverlapsOnlyWhenStrictlyInside()
    {
        PanelRect big = new PanelRect(400f, 200f, 200f, 200f);
        Assert.True(new PanelRect(500f, 300f, 0f, 0f).Overlaps(big));
        Assert.False(new PanelRect(600f, 300f, 0f, 0f).Overlaps(big));
    }

    [Fact]
    public void OverlapsIsSymmetricForEveryPair()
    {
        PanelRect[] rects =
        {
            new PanelRect(40f, 220f, 300f, 560f),
            new PanelRect(592f, 236f, 196f, 62f),
            new PanelRect(500f, 300f, 0f, 0f),
            new PanelRect(340f, 220f, 10f, 10f),
        };

        foreach (PanelRect left in rects)
        {
            foreach (PanelRect right in rects)
            {
                Assert.Equal(left.Overlaps(right), right.Overlaps(left));
            }
        }
    }

    [Theory]
    [InlineData("width", 0f, 0f, -1f, 10f)]
    [InlineData("height", 0f, 0f, 10f, -1f)]
    [InlineData("x", float.NaN, 0f, 1f, 1f)]
    [InlineData("x", float.PositiveInfinity, 0f, 1f, 1f)]
    [InlineData("x", float.NegativeInfinity, 0f, 1f, 1f)]
    [InlineData("y", 0f, float.NaN, 1f, 1f)]
    [InlineData("y", 0f, float.PositiveInfinity, 1f, 1f)]
    [InlineData("y", 0f, float.NegativeInfinity, 1f, 1f)]
    [InlineData("width", 0f, 0f, float.NaN, 1f)]
    [InlineData("width", 0f, 0f, float.PositiveInfinity, 1f)]
    [InlineData("width", 0f, 0f, float.NegativeInfinity, 1f)]
    [InlineData("height", 0f, 0f, 1f, float.NaN)]
    [InlineData("height", 0f, 0f, 1f, float.PositiveInfinity)]
    [InlineData("height", 0f, 0f, 1f, float.NegativeInfinity)]
    public void ConstructionRejectsNonFiniteAndNegativeSizesWithTheParamName(string paramName, float x, float y, float width, float height)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => new PanelRect(x, y, width, height));
        Assert.Equal(paramName, ex.ParamName);
    }

    [Theory]
    [InlineData(-2f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void InflateRejectsInvalidAmountWithTheAmountParamName(float amount)
    {
        var rect = new PanelRect(0f, 0f, 1f, 1f);
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => rect.Inflate(amount));
        Assert.Equal("amount", ex.ParamName);
    }
}
