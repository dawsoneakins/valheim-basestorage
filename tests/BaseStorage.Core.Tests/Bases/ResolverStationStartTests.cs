namespace BaseStorage.Core.Tests.Bases;

using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.ResolutionFixtures;

/// <summary>AC-1 (ADR-GWIN-024, ADR-GWIN-025): a station start resolves the base that contains that station.</summary>
public sealed class ResolverStationStartTests
{
    // GW-U-228 (AC-1): the base of the station, in fill order, with its members.
    [Theory]
    [InlineData(1, new int[] { 1, 2 }, new int[] { 10, 13 })]
    [InlineData(2, new int[] { 2, 1 }, new int[] { 10, 13 })]
    [InlineData(3, new int[] { 3 }, new int[] { 11 })]
    public void StationStartResolvesTheBaseOfThatStation(int station, int[] expectedStations, int[] expectedContainers)
    {
        BaseResolution result = BaseResolver.Resolve(WorldStations(), WorldContainers(), BaseStart.FromStation(StationKey(station)), 64);

        Assert.True(result.IsInBase);
        Assert.Equal(expectedStations, Serials(result.Stations));
        Assert.Equal(expectedContainers, Serials(result.Containers));
        Assert.False(result.IsCapped);
        Assert.Null(result.MessageKey);
    }

    // GW-U-229 (AC-1): two unconnected bases are not merged; each station gives only its own base, with no containers.
    [Theory]
    [InlineData(8, new int[] { 8 })]
    [InlineData(9, new int[] { 9 })]
    public void StationStartDoesNotMergeUnconnectedBases(int station, int[] expectedStations)
    {
        BaseResolution result = BaseResolver.Resolve(WorldStations(), WorldContainers(), BaseStart.FromStation(StationKey(station)), 64);

        Assert.Equal(expectedStations, Serials(result.Stations));
        Assert.Empty(result.Containers);
    }

    // GW-U-233 (AC-1): the cap passes through to the graph; a cap of 1 stops a station start after the start station.
    [Fact]
    public void StationStartCapStopsAtOneStation()
    {
        BaseResolution result = BaseResolver.Resolve(WorldStations(), WorldContainers(), BaseStart.FromStation(StationKey(1)), 1);

        Assert.True(result.IsInBase);
        Assert.True(result.IsCapped);
        Assert.Equal(new int[] { 1 }, Serials(result.Stations));
        Assert.Equal(new int[] { 10 }, Serials(result.Containers));
    }

    // GW-U-233 (AC-1): the same cap through a position start, whose seed is station 1.
    [Fact]
    public void PositionStartCapStopsAtOneStation()
    {
        BaseStart start = BaseStart.FromPosition(new WorldPosition(10f, 0f, 0f));

        BaseResolution result = BaseResolver.Resolve(WorldStations(), WorldContainers(), start, 1);

        Assert.True(result.IsInBase);
        Assert.True(result.IsCapped);
        Assert.Equal(new int[] { 1 }, Serials(result.Stations));
        Assert.Equal(new int[] { 10 }, Serials(result.Containers));
    }
}
