namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Resolution;
using static BaseStorage.Core.Tests.Bases.ResolutionFixtures;

/// <summary>AC-3 (ADR-GWIN-024, ADR-GWIN-025): each placed or removed station or container invalidates the cache, and events coalesce.</summary>
public sealed class BaseCacheInvalidationTests
{
    // GW-U-247 (AC-3): each event forces an immediate rebuild, with no clock advance.
    [Theory]
    [InlineData("OnStationPlaced")]
    [InlineData("OnStationRemoved")]
    [InlineData("OnContainerPlaced")]
    [InlineData("OnContainerRemoved")]
    public void EachEventForcesAnImmediateRebuild(string eventName)
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        BaseStart start = BaseStart.FromStation(StationKey(1));
        BaseResolution first = cache.Resolve(start, DefaultOptions());

        RaiseEvent(cache, eventName);
        BaseResolution second = cache.Resolve(start, DefaultOptions());

        Assert.Equal(2, source.StationReads);
        Assert.Equal(2, source.ContainerReads);
        Assert.NotSame(first, second);
    }

    // GW-U-248 (AC-3): after the change and its event, the next resolve reflects the change at once, with no clock advance.
    [Theory]
    [InlineData("StationPlaced", 3, new int[] { 3, 5 }, new int[] { 11 })]
    [InlineData("StationRemoved", 1, new int[] { 1 }, new int[] { 10 })]
    [InlineData("ContainerPlaced", 1, new int[] { 1, 2 }, new int[] { 10, 13, 14 })]
    [InlineData("ContainerRemoved", 1, new int[] { 1, 2 }, new int[] { 13 })]
    public void TheRebuildReflectsTheChangeAtOnce(
        string change,
        int startStation,
        int[] expectedStations,
        int[] expectedContainers)
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        BaseStart start = BaseStart.FromStation(StationKey(startStation));
        _ = cache.Resolve(start, DefaultOptions());

        ApplyChange(cache, source, change);
        BaseResolution result = cache.Resolve(start, DefaultOptions());

        Assert.Equal(expectedStations, Serials(result.Stations));
        Assert.Equal(expectedContainers, Serials(result.Containers));
    }

    // GW-U-249 (AC-3): events before any resolve read nothing and throw nothing; then several events coalesce into one rebuild.
    [Fact]
    public void EventsOnlyMarkDirtyAndCoalesce()
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        BaseStart start = BaseStart.FromStation(StationKey(1));

        for (var round = 0; round < 2; round++)
        {
            cache.OnStationPlaced();
            cache.OnStationRemoved();
            cache.OnContainerPlaced();
            cache.OnContainerRemoved();
        }

        Assert.Equal(0, source.StationReads);

        _ = cache.Resolve(start, DefaultOptions());
        Assert.Equal(1, source.StationReads);

        cache.OnStationPlaced();
        cache.OnContainerPlaced();
        cache.OnStationRemoved();
        _ = cache.Resolve(start, DefaultOptions());

        Assert.Equal(2, source.StationReads);
    }

    private static void RaiseEvent(BaseCache cache, string eventName)
    {
        switch (eventName)
        {
            case "OnStationPlaced":
                cache.OnStationPlaced();
                break;
            case "OnStationRemoved":
                cache.OnStationRemoved();
                break;
            case "OnContainerPlaced":
                cache.OnContainerPlaced();
                break;
            case "OnContainerRemoved":
                cache.OnContainerRemoved();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(eventName));
        }
    }

    private static void ApplyChange(BaseCache cache, CountingSnapshotSource source, string change)
    {
        switch (change)
        {
            case "StationPlaced":
                source.Stations.Add(Station5());
                cache.OnStationPlaced();
                break;
            case "StationRemoved":
                source.Stations.RemoveAll(station => station.Id == StationKey(2));
                cache.OnStationRemoved();
                break;
            case "ContainerPlaced":
                source.Containers.Add(Container14());
                cache.OnContainerPlaced();
                break;
            case "ContainerRemoved":
                source.Containers.RemoveAll(container => container.Id == ContainerKey(10));
                cache.OnContainerRemoved();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change));
        }
    }
}
