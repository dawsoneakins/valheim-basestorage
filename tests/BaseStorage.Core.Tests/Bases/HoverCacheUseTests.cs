namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Inspection;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.HoverFixtures;

/// <summary>AC-5 (ADR-GWIN-032, ADR-GWIN-033): repeated hovers read the world once, the hit path allocates nothing, and the memo re-resolves exactly when the cache would.</summary>
public sealed class HoverCacheUseTests
{
    private const string Encoded = "c:Ores";

    // GW-U-552 (AC-5): repeated hovers inside the window read the world once; the window still holds at 109.999 s.
    [Fact]
    public void RepeatedHoversInsideTheWindowReadTheWorldOnce()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        ContainerSnapshot c1 = C1();
        ContainerSnapshot c2 = C2();
        CountingSnapshotSource source = Source(Benches(2), c1, c2);
        var inspector = new HoverInspector(new BaseCache(source, clock));
        BaseCacheOptions options = Options();

        string first = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);
        for (int i = 0; i < 1000; i++)
        {
            Assert.Same(first, inspector.ChestLine(c1.Id, c1.Position, Encoded, options));
        }

        Assert.Equal(1, source.StationReads);
        Assert.Equal(1, source.ContainerReads);

        string station = inspector.StationLine(StationKey(17), options);
        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(station, inspector.StationLine(StationKey(17), options));
        }

        Assert.Equal(1, source.StationReads);

        for (int i = 0; i < 100; i++)
        {
            _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);
            _ = inspector.ChestLine(c2.Id, c2.Position, Encoded, options);
            _ = inspector.StationLine(StationKey(17), options);
        }

        Assert.Equal(1, source.StationReads);
        Assert.Equal(1, source.ContainerReads);

        clock.Elapsed = TimeSpan.FromSeconds(109.999);
        _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);

        Assert.Equal(1, source.StationReads);
        Assert.Equal(1, source.ContainerReads);
    }

    // GW-U-553 (AC-5): after a warm-up, the hit path allocates nothing.
    [Fact]
    public void ChestHitsAllocateNothing()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        ContainerSnapshot c1 = C1();
        CountingSnapshotSource source = Source(Benches(2), c1);
        var inspector = new HoverInspector(new BaseCache(source, clock));
        BaseCacheOptions options = Options();
        WorldPosition at = c1.Position;
        ContainerId id = c1.Id;
        string warm = inspector.ChestLine(id, at, Encoded, options);

        long before = GC.GetAllocatedBytesForCurrentThread();
        string last = warm;
        for (int i = 0; i < 1000; i++)
        {
            last = inspector.ChestLine(id, at, Encoded, options);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0L, allocated);
        Assert.Same(warm, last);
    }

    // GW-U-553 (AC-5): after a warm-up, the station hit path allocates nothing.
    [Fact]
    public void StationHitsAllocateNothing()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        CountingSnapshotSource source = Source(Benches(2), C1());
        var inspector = new HoverInspector(new BaseCache(source, clock));
        BaseCacheOptions options = Options();
        StationId station = StationKey(17);
        string warm = inspector.StationLine(station, options);

        long before = GC.GetAllocatedBytesForCurrentThread();
        string last = warm;
        for (int i = 0; i < 1000; i++)
        {
            last = inspector.StationLine(station, options);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0L, allocated);
        Assert.Same(warm, last);
    }

    // GW-U-553 (AC-5): the probe is not vacuous. Alternating encoded values miss the memo and allocate.
    [Fact]
    public void AlternatingEncodedValuesMissTheMemoAndAllocate()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        ContainerSnapshot c1 = C1();
        CountingSnapshotSource source = Source(Benches(2), c1);
        var inspector = new HoverInspector(new BaseCache(source, clock));
        BaseCacheOptions options = Options();
        _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);
        _ = inspector.ChestLine(c1.Id, c1.Position, "k", options);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10; i++)
        {
            _ = inspector.ChestLine(c1.Id, c1.Position, i % 2 == 0 ? Encoded : "k", options);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated > 0, "The contrast case allocated nothing, so the probe cannot see allocations.");
    }

    // GW-U-554 (AC-5): the memo re-resolves exactly when BaseCache would. Each row starts from one primed hover with reads 1.
    [Theory]
    [InlineData("a-window-end")]
    [InlineData("b-station-placed")]
    [InlineData("c-station-removed")]
    [InlineData("d-container-placed")]
    [InlineData("e-container-removed")]
    [InlineData("f-station-added-and-placed")]
    [InlineData("g-another-caller-rebuilds")]
    [InlineData("h-cap-changed")]
    [InlineData("i-clock-goes-back")]
    [InlineData("j-another-encoded-value")]
    [InlineData("k-another-chest")]
    public void TheMemoReResolvesExactlyWhenTheCacheWould(string row)
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        ContainerSnapshot c1 = C1();
        ContainerSnapshot c2 = C2();
        CountingSnapshotSource source = Source(Benches(2), c1, c2);
        var cache = new BaseCache(source, clock);
        var inspector = new HoverInspector(cache);
        BaseCacheOptions options = Options();
        Assert.Equal("[In base: 2 stations] [Ores]", inspector.ChestLine(c1.Id, c1.Position, Encoded, options));
        Assert.Equal(1, source.StationReads);

        switch (row)
        {
            case "a-window-end":
                clock.Elapsed = TimeSpan.FromSeconds(110);
                _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);
                Assert.Equal(2, source.StationReads);
                break;
            case "b-station-placed":
                cache.OnStationPlaced();
                _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);
                Assert.Equal(2, source.StationReads);
                break;
            case "c-station-removed":
                cache.OnStationRemoved();
                _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);
                Assert.Equal(2, source.StationReads);
                break;
            case "d-container-placed":
                cache.OnContainerPlaced();
                _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);
                Assert.Equal(2, source.StationReads);
                break;
            case "e-container-removed":
                cache.OnContainerRemoved();
                _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);
                Assert.Equal(2, source.StationReads);
                break;
            case "f-station-added-and-placed":
                source.Stations.Add(Bench(2));
                cache.OnStationPlaced();
                Assert.Equal("[In base: 3 stations] [Ores]", inspector.ChestLine(c1.Id, c1.Position, Encoded, options));
                Assert.Equal(2, source.StationReads);
                break;
            case "g-another-caller-rebuilds":
                cache.OnContainerPlaced();
                _ = cache.Resolve(BaseStart.FromStation(StationKey(17)), options);
                Assert.Equal(2, source.StationReads);
                _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);
                Assert.Equal(2, source.StationReads);
                break;
            case "h-cap-changed":
                _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, new BaseCacheOptions(10, 1));
                Assert.Equal(2, source.StationReads);
                break;
            case "i-clock-goes-back":
                clock.Elapsed = TimeSpan.FromSeconds(50);
                _ = inspector.ChestLine(c1.Id, c1.Position, Encoded, options);
                Assert.Equal(2, source.StationReads);
                break;
            case "j-another-encoded-value":
                Assert.Equal("[In base: 2 stations] [Keep]", inspector.ChestLine(c1.Id, c1.Position, "k", options));
                Assert.Equal(1, source.StationReads);
                break;
            case "k-another-chest":
                Assert.Equal("[In base: 2 stations] [Ores]", inspector.ChestLine(c2.Id, c2.Position, Encoded, options));
                Assert.Equal(1, source.StationReads);
                break;
            default:
                throw new InvalidOperationException("Unknown row " + row);
        }
    }
}
