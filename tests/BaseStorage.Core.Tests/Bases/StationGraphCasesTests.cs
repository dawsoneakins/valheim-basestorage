namespace BaseStorage.Core.Tests.Bases;

using System.Linq;
using static BaseStorage.Core.Tests.Bases.StationGraphFixtures;

/// <summary>AC-3: the connection scenarios of spec AC-3 (single station, chains, separate bases, broken chain, overlap, heights).</summary>
public sealed class StationGraphCasesTests
{
    // GW-U-126 (AC-3): a single station, and the same with cap 1.
    [Theory]
    [InlineData("G1")]
    [InlineData("G10i")]
    public void SingleStationIsItsOwnBase(string id)
    {
        AssertRow(id);
    }

    // GW-U-127 (AC-3): a chain of five, all five returned from either end or the middle.
    [Theory]
    [InlineData("G2a")]
    [InlineData("G2b")]
    public void ChainOfFiveReturnsAllFiveAndIsNotCapped(string id)
    {
        AssertRow(id);

        Assert.Equal(5, Run(id).Stations.Count);
    }

    // GW-U-128 (AC-3): two separate bases do not merge.
    [Fact]
    public void TwoSeparateBasesDoNotMerge()
    {
        AssertRow("G3a");
        AssertRow("G3b");

        var fromFirst = Run("G3a").Stations.Select(s => s.Id.Id).ToArray();
        var fromSecond = Run("G3b").Stations.Select(s => s.Id.Id).ToArray();

        Assert.DoesNotContain(10u, fromFirst);
        Assert.DoesNotContain(11u, fromFirst);
        Assert.DoesNotContain(1u, fromSecond);
        Assert.DoesNotContain(2u, fromSecond);
    }

    // GW-U-129 (AC-3): a chain broken in the middle by a 30 m gap.
    [Theory]
    [InlineData("G4a")]
    [InlineData("G4b")]
    public void ChainBrokenInTheMiddleStopsAtTheGap(string id)
    {
        AssertRow(id);
    }

    // GW-U-130 (AC-3): overlapping ranges, including the one-direction-only case.
    [Theory]
    [InlineData("G5")]
    [InlineData("G6a")]
    [InlineData("G6b")]
    public void OverlappingRangesConnectWhenEitherSideReaches(string id)
    {
        AssertRow(id);
    }

    // GW-U-131 (AC-3): stations at different heights follow the verified metric (ADR-GWIN-016).
    [Theory]
    [InlineData("G8a")]
    [InlineData("G8b")]
    [InlineData("G8c")]
    [InlineData("G8d")]
    public void StationsAtDifferentHeightsFollowTheVerifiedMetric(string id)
    {
        AssertRow(id);
    }
}
