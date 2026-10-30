namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.HoverFixtures;

/// <summary>
/// AC-5 (ADR-GWIN-032): BaseCache.IsCurrent says whether Resolve would answer without a rebuild, reads nothing, and Generation counts successful
/// rebuilds. The memo in HoverInspector relies on both.
/// </summary>
public sealed class BaseCacheCurrencyTests
{
    private static readonly BaseStart FromBench = BaseStart.FromStation(StationKey(17));

    // GW-U-555 (AC-5): a fresh cache is not current and has generation 0.
    [Fact]
    public void AFreshCacheIsNotCurrentAndHasGenerationZero()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        var cache = new BaseCache(Source(Benches(2), C1()), clock);

        Assert.Equal(0L, cache.Generation);
        Assert.False(cache.IsCurrent(Options()));
    }

    // GW-U-555 (AC-5): after a resolve the cache is current and has generation 1.
    [Fact]
    public void AfterResolveTheCacheIsCurrentAndHasGenerationOne()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        var cache = new BaseCache(Source(Benches(2), C1()), clock);
        _ = cache.Resolve(FromBench, Options());

        Assert.True(cache.IsCurrent(Options()));
        Assert.Equal(1L, cache.Generation);
    }

    // GW-U-555 (AC-5): IsCurrent never reads the source and never rebuilds, over 100 calls.
    [Fact]
    public void IsCurrentNeverReadsOrRebuilds()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        CountingSnapshotSource source = Source(Benches(2), C1());
        var cache = new BaseCache(source, clock);
        _ = cache.Resolve(FromBench, Options());

        for (int i = 0; i < 100; i++)
        {
            Assert.True(cache.IsCurrent(Options()));
        }

        Assert.Equal(1, source.StationReads);
        Assert.Equal(1L, cache.Generation);
    }

    // GW-U-555 (AC-5): the window edge. One tick before built + 10 s is current; at built + 10 s it is not, and Resolve rebuilds there.
    [Fact]
    public void TheWindowEdgeMatchesTheRebuildInstant()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        CountingSnapshotSource source = Source(Benches(2), C1());
        var cache = new BaseCache(source, clock);
        _ = cache.Resolve(FromBench, Options());

        clock.Elapsed = TimeSpan.FromSeconds(110) - TimeSpan.FromTicks(1);
        Assert.True(cache.IsCurrent(Options()));

        clock.Elapsed = TimeSpan.FromSeconds(110);
        Assert.False(cache.IsCurrent(Options()));

        _ = cache.Resolve(FromBench, Options());
        Assert.Equal(2, source.StationReads);
    }

    // GW-U-555 (AC-5): each of the four On* events makes the cache not current.
    [Theory]
    [InlineData("OnStationPlaced")]
    [InlineData("OnStationRemoved")]
    [InlineData("OnContainerPlaced")]
    [InlineData("OnContainerRemoved")]
    public void EachEventMakesTheCacheNotCurrent(string eventName)
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        var cache = new BaseCache(Source(Benches(2), C1()), clock);
        _ = cache.Resolve(FromBench, Options());
        Assert.True(cache.IsCurrent(Options()));

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
                throw new InvalidOperationException("Unknown event " + eventName);
        }

        Assert.False(cache.IsCurrent(Options()));
    }

    // GW-U-555 (AC-5): a clock that goes backwards makes the cache not current.
    [Fact]
    public void AClockDecreaseMakesTheCacheNotCurrent()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        var cache = new BaseCache(Source(Benches(2), C1()), clock);
        _ = cache.Resolve(FromBench, Options());

        clock.Elapsed = TimeSpan.FromSeconds(50);

        Assert.False(cache.IsCurrent(Options()));
    }

    // GW-U-555 (AC-5): a different station cap makes the cache not current, and the same cap keeps it current.
    [Fact]
    public void ADifferentStationCapMakesTheCacheNotCurrent()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        var cache = new BaseCache(Source(Benches(2), C1()), clock);
        _ = cache.Resolve(FromBench, Options());

        Assert.False(cache.IsCurrent(new BaseCacheOptions(10, 2)));
        Assert.True(cache.IsCurrent(Options()));
    }

    // GW-U-555 (AC-5): a window of 0 is never current.
    [Fact]
    public void AZeroWindowIsNeverCurrent()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        var cache = new BaseCache(Source(Benches(2), C1()), clock);
        _ = cache.Resolve(FromBench, new BaseCacheOptions(0, 64));

        Assert.False(cache.IsCurrent(new BaseCacheOptions(0, 64)));
    }

    // GW-U-555 (AC-5): a failed rebuild leaves Generation unchanged and the cache not current.
    [Fact]
    public void AFailedRebuildLeavesGenerationAndIsNotCurrent()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        CountingSnapshotSource source = Source(Benches(2), C1());
        var cache = new BaseCache(source, clock);
        _ = cache.Resolve(FromBench, Options());
        Assert.Equal(1L, cache.Generation);

        clock.Elapsed = TimeSpan.FromSeconds(120);
        source.ThrowOnRead = new InvalidOperationException("read failed");

        Assert.Throws<InvalidOperationException>(() => cache.Resolve(FromBench, Options()));
        Assert.Equal(1L, cache.Generation);
        Assert.False(cache.IsCurrent(Options()));
    }

    // GW-U-555 (AC-5): IsCurrent reads the clock exactly once per call.
    [Fact]
    public void IsCurrentReadsTheClockOncePerCall()
    {
        var clock = new ReadCountingClock();
        var cache = new BaseCache(Source(Benches(2), C1()), clock);
        _ = cache.Resolve(FromBench, Options());
        int before = clock.Reads;

        _ = cache.IsCurrent(Options());

        Assert.Equal(before + 1, clock.Reads);
    }

    /// <summary>A clock that counts its reads, so a test can check how often a method reads time.</summary>
    private sealed class ReadCountingClock : IClock
    {
        /// <summary>Gets the number of times the clock was read.</summary>
        public int Reads { get; private set; }

        /// <summary>Gets the time, which stays at 100 s, and counts the read.</summary>
        public TimeSpan Elapsed
        {
            get
            {
                Reads++;
                return TimeSpan.FromSeconds(100);
            }
        }
    }
}
