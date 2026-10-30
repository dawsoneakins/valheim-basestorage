namespace BaseStorage.Core.Tests.Bases;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.ResolutionFixtures;

/// <summary>AC-1 (ADR-GWIN-016, ADR-GWIN-024, ADR-GWIN-025): a position start resolves the base whose station range contains the position.</summary>
public sealed class ResolverPositionStartTests
{
    // GW-U-230 (AC-1): the seed is the lowest-id eligible station whose range contains the point; Y is ignored.
    [Theory]
    [InlineData(10f, 0f, 0f, new int[] { 1, 2 }, new int[] { 10, 13 })]
    [InlineData(30f, 0f, 0f, new int[] { 2, 1 }, new int[] { 10, 13 })]
    [InlineData(100f, 0f, 5f, new int[] { 3 }, new int[] { 11 })]
    [InlineData(100f, 500f, 0f, new int[] { 3 }, new int[] { 11 })]
    public void PositionStartResolvesTheBaseWhoseRangeContainsThePosition(
        float x,
        float y,
        float z,
        int[] expectedStations,
        int[] expectedContainers)
    {
        BaseResolution result = BaseResolver.Resolve(WorldStations(), WorldContainers(), BaseStart.FromPosition(new WorldPosition(x, y, z)), 64);

        Assert.True(result.IsInBase);
        Assert.Equal(expectedStations, Serials(result.Stations));
        Assert.Equal(expectedContainers, Serials(result.Containers));
    }

    // GW-U-231 (AC-1): a point in range of two unconnected stations takes the lowest station id.
    [Fact]
    public void OverlappingUnconnectedBasesPickTheLowestStationId()
    {
        BaseResolution result = BaseResolver.Resolve(WorldStations(), WorldContainers(), BaseStart.FromPosition(new WorldPosition(318.5f, 0f, 0f)), 64);

        Assert.True(result.IsInBase);
        Assert.Equal(new int[] { 8 }, Serials(result.Stations));
        Assert.Empty(result.Containers);
    }

    // GW-U-231 (AC-1): the same point with the stations listed 9 first still gives station 8.
    [Fact]
    public void OverlappingUnconnectedBasesIgnoreTheListOrder()
    {
        StationSnapshot[] world = WorldStations();
        StationSnapshot[] reordered = { world[5], world[4], world[0], world[1], world[2], world[3] };

        BaseResolution result = BaseResolver.Resolve(reordered, WorldContainers(), BaseStart.FromPosition(new WorldPosition(318.5f, 0f, 0f)), 64);

        Assert.True(result.IsInBase);
        Assert.Equal(new int[] { 8 }, Serials(result.Stations));
    }

    // GW-U-232 (AC-1): a station start and a position start in the same base agree, and neither mutates its inputs.
    [Fact]
    public void StationAndPositionStartsAgreeOnOneBase()
    {
        List<StationSnapshot> stations = WorldStations().ToList();
        List<ContainerSnapshot> containers = WorldContainers().ToList();
        StationSnapshot[] stationsBefore = stations.ToArray();
        ContainerSnapshot[] containersBefore = containers.ToArray();

        BaseResolution byStation = BaseResolver.Resolve(stations, containers, BaseStart.FromStation(StationKey(1)), 64);
        BaseResolution byPosition = BaseResolver.Resolve(stations, containers, BaseStart.FromPosition(new WorldPosition(1f, 0f, 0f)), 64);

        Assert.Equal(Serials(byStation.Stations), Serials(byPosition.Stations));
        Assert.Equal(Serials(byStation.Containers), Serials(byPosition.Containers));
        Assert.Equal(new int[] { 1, 2 }, Serials(byPosition.Stations));
        Assert.Equal(new int[] { 10, 13 }, Serials(byPosition.Containers));
        Assert.Equal(stationsBefore, stations);
        Assert.Equal(containersBefore, containers);
    }
}
