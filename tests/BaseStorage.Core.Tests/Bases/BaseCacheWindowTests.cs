namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Resolution;
using static BaseStorage.Core.Tests.Bases.ResolutionFixtures;

/// <summary>AC-3 (ADR-GWIN-024, ADR-GWIN-025): the window bounds the rebuilds, and reads come only from the copied snapshot.</summary>
public sealed class BaseCacheWindowTests
{
    // GW-U-241 (AC-3): inside the window there is no rebuild, and the first result is returned again.
    [Fact]
    public void InsideTheWindowThereIsNoRebuild()
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        BaseStart start = BaseStart.FromStation(StationKey(1));

        BaseResolution first = cache.Resolve(start, DefaultOptions());

        Assert.Equal(1, source.StationReads);
        Assert.Equal(1, source.ContainerReads);
        Assert.Equal(new[] { "GetStations", "GetContainers" }, source.CallLog);

        clock.Advance(TimeSpan.FromSeconds(1));
        BaseResolution second = cache.Resolve(start, DefaultOptions());
        clock.Advance(TimeSpan.FromSeconds(0.999));
        BaseResolution third = cache.Resolve(start, DefaultOptions());

        Assert.Equal(1, source.StationReads);
        Assert.Equal(1, source.ContainerReads);
        Assert.Same(first, second);
        Assert.Same(first, third);
    }

    // GW-U-242 (AC-3): exactly at the window (>=) and well after it, the next resolve rebuilds once, and a second resolve at the same time does not.
    [Theory]
    [InlineData(2.0)]
    [InlineData(5.0)]
    public void AfterTheWindowThereIsOneRebuild(double seconds)
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        BaseStart start = BaseStart.FromStation(StationKey(1));
        BaseResolution first = cache.Resolve(start, DefaultOptions());

        clock.Advance(TimeSpan.FromSeconds(seconds));
        BaseResolution rebuilt = cache.Resolve(start, DefaultOptions());
        BaseResolution again = cache.Resolve(start, DefaultOptions());

        Assert.Equal(2, source.StationReads);
        Assert.NotSame(first, rebuilt);
        Assert.Same(rebuilt, again);
    }

    // GW-U-243 (AC-3): the window serves the copied snapshot; a container added without an event shows up only after the window expires.
    [Fact]
    public void TheWindowServesACopiedSnapshot()
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        BaseStart start = BaseStart.FromStation(StationKey(1));
        _ = cache.Resolve(start, DefaultOptions());

        source.Containers.Add(Container14());

        BaseResolution inside = cache.Resolve(start, DefaultOptions());
        Assert.Equal(new int[] { 10, 13 }, Serials(inside.Containers));

        clock.Advance(TimeSpan.FromSeconds(2));
        BaseResolution after = cache.Resolve(start, DefaultOptions());
        Assert.Equal(new int[] { 10, 13, 14 }, Serials(after.Containers));
    }

    // GW-U-244 (AC-3): a window of 0 seconds rebuilds on every resolve, with no clock advance.
    [Fact]
    public void ZeroSecondsRebuildsOnEveryResolve()
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        var options = new BaseCacheOptions(0, 64);
        BaseStart start = BaseStart.FromStation(StationKey(1));

        _ = cache.Resolve(start, options);
        _ = cache.Resolve(start, options);
        _ = cache.Resolve(start, options);

        Assert.Equal(3, source.StationReads);
    }

    // GW-U-245 (AC-3): the window is read from the options of each call, so a longer window does not rebuild and a shorter one does.
    [Fact]
    public void TheWindowIsReadFromTheOptionsOfEachCall()
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        BaseStart start = BaseStart.FromStation(StationKey(1));
        _ = cache.Resolve(start, new BaseCacheOptions(2, 64));
        clock.Advance(TimeSpan.FromSeconds(3));

        _ = cache.Resolve(start, new BaseCacheOptions(10, 64));
        Assert.Equal(1, source.StationReads);

        _ = cache.Resolve(start, new BaseCacheOptions(2, 64));
        Assert.Equal(2, source.StationReads);
    }

    // GW-U-245 (AC-3): a change of the station cap alone rebuilds at the same time, and the new cap applies.
    [Fact]
    public void ChangingOnlyTheCapRebuildsAtOnce()
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        BaseStart start = BaseStart.FromStation(StationKey(1));
        _ = cache.Resolve(start, new BaseCacheOptions(2, 64));

        BaseResolution capped = cache.Resolve(start, new BaseCacheOptions(2, 1));

        Assert.Equal(2, source.StationReads);
        Assert.True(capped.IsCapped);
        Assert.Equal(new int[] { 1 }, Serials(capped.Stations));
    }

    // GW-U-246 (AC-3, contract 3.1, fail closed): a clock reading that goes backwards forces a rebuild.
    [Fact]
    public void ClockGoingBackwardsRebuilds()
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        BaseStart start = BaseStart.FromStation(StationKey(1));
        _ = cache.Resolve(start, DefaultOptions());

        clock.Elapsed = TimeSpan.FromSeconds(3599);
        _ = cache.Resolve(start, DefaultOptions());

        Assert.Equal(2, source.StationReads);
    }
}
