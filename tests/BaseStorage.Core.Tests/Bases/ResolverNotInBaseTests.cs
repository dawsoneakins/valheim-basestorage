namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.ResolutionFixtures;

/// <summary>AC-2 (ADR-GWIN-024, ADR-GWIN-025): a start in no base gets the one shared no-base result.</summary>
public sealed class ResolverNotInBaseTests
{
    // GW-U-234 (AC-2): a position in no base's range gives the no-base result: container 12's spot, the exact range of station 3,
    // the range-0 station 4, and a point far away.
    [Theory]
    [InlineData(60f, 0f, 60f)]
    [InlineData(120f, 0f, 0f)]
    [InlineData(50f, 0f, 50f)]
    [InlineData(-25f, 0f, 0f)]
    public void PositionInNoBaseGivesTheNoBaseResult(float x, float y, float z)
    {
        BaseResolution result = BaseResolver.Resolve(WorldStations(), WorldContainers(), BaseStart.FromPosition(new WorldPosition(x, y, z)), 64);

        Assert.Same(BaseResolution.NotInBase, result);
    }

    // GW-U-235 (AC-2): a station start with no eligible station behind it gives the no-base result: an ineligible station, a missing
    // station, and any start over an empty station list.
    [Theory]
    [InlineData(4, false)]
    [InlineData(99, false)]
    [InlineData(1, true)]
    public void StationStartWithNoBaseGivesTheNoBaseResult(int station, bool emptyWorld)
    {
        StationSnapshot[] stations = emptyWorld ? Array.Empty<StationSnapshot>() : WorldStations();

        BaseResolution result = BaseResolver.Resolve(stations, WorldContainers(), BaseStart.FromStation(StationKey(station)), 64);

        Assert.Same(BaseResolution.NotInBase, result);
    }

    // GW-U-236 (AC-2): just inside the range of station 3 is a base, and that result is not the no-base singleton.
    [Fact]
    public void JustInsideTheRangeIsABaseAndNotTheNoBaseResult()
    {
        BaseResolution result = BaseResolver.Resolve(WorldStations(), WorldContainers(), BaseStart.FromPosition(new WorldPosition(119.9f, 0f, 0f)), 64);

        Assert.True(result.IsInBase);
        Assert.Equal(new int[] { 3 }, Serials(result.Stations));
        Assert.NotSame(BaseResolution.NotInBase, result);
    }
}
