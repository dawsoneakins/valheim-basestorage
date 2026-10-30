namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Resolution;
using static BaseStorage.Core.Tests.Bases.ResolutionFixtures;

/// <summary>AC-3 (ADR-GWIN-024, ADR-GWIN-025): bad source data and read failures fail the rebuild, and no failure serves a stale result.</summary>
public sealed class BaseCacheFailureTests
{
    // GW-U-251 (AC-3): bad source data fails the rebuild with the contracted exception type.
    [Theory]
    [InlineData("StationsNull", typeof(InvalidOperationException))]
    [InlineData("ContainersNull", typeof(InvalidOperationException))]
    [InlineData("NullStationElement", typeof(ArgumentException))]
    [InlineData("NullContainerElement", typeof(ArgumentException))]
    public void BadSourceDataFailsTheRebuild(string fault, Type expected)
    {
        CountingSnapshotSource source = LoadedSource();
        ApplyFault(source, fault);
        var cache = new BaseCache(source, new ManualClock());

        Exception? thrown = Record.Exception(() =>
        {
            _ = cache.Resolve(BaseStart.FromStation(StationKey(1)), DefaultOptions());
        });

        Assert.NotNull(thrown);
        Assert.Equal(expected, thrown!.GetType());
    }

    // GW-U-251 (AC-3): a read that throws is rethrown as the same instance.
    [Fact]
    public void AThrowingReadRethrowsTheSameInstance()
    {
        CountingSnapshotSource source = LoadedSource();
        var failure = new InvalidOperationException("source failed");
        source.ThrowOnRead = failure;
        var cache = new BaseCache(source, new ManualClock());

        Exception? thrown = Record.Exception(() =>
        {
            _ = cache.Resolve(BaseStart.FromStation(StationKey(1)), DefaultOptions());
        });

        Assert.Same(failure, thrown);
    }

    // GW-U-252 (AC-3): a failed rebuild is retried on each resolve, never serves the old result, and succeeds once the fault clears.
    [Fact]
    public void AFailedRebuildIsRetriedAndNeverServesAStaleResult()
    {
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, new ManualClock());
        BaseStart start = BaseStart.FromStation(StationKey(1));
        _ = cache.Resolve(start, DefaultOptions());

        cache.OnContainerPlaced();
        source.ThrowOnRead = new InvalidOperationException("first failure");
        Assert.NotNull(Record.Exception(() =>
        {
            _ = cache.Resolve(start, DefaultOptions());
        }));
        Assert.NotNull(Record.Exception(() =>
        {
            _ = cache.Resolve(start, DefaultOptions());
        }));
        Assert.Equal(3, source.StationReads);

        source.ThrowOnRead = null;
        BaseResolution recovered = cache.Resolve(start, DefaultOptions());

        Assert.Equal(4, source.StationReads);
        Assert.True(recovered.IsInBase);
        Assert.Equal(new int[] { 1, 2 }, Serials(recovered.Stations));
    }

    // GW-U-252 (AC-3): when the window expires and the source throws, the resolve throws rather than returning the old result.
    [Fact]
    public void AnExpiredWindowWithAFailingSourceThrowsInsteadOfServingTheOldResult()
    {
        var clock = new ManualClock();
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, clock);
        BaseStart start = BaseStart.FromStation(StationKey(1));
        _ = cache.Resolve(start, DefaultOptions());

        clock.Advance(TimeSpan.FromSeconds(2));
        source.ThrowOnRead = new InvalidOperationException("window expired");
        Exception? thrown = Record.Exception(() =>
        {
            _ = cache.Resolve(start, DefaultOptions());
        });

        Assert.IsType<InvalidOperationException>(thrown);
    }

    // GW-U-253 (AC-3): the start is validated before the clock or the source is read.
    [Fact]
    public void AnInvalidStartIsRejectedBeforeTheSourceIsRead()
    {
        CountingSnapshotSource source = LoadedSource();
        var cache = new BaseCache(source, new ManualClock());

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            _ = cache.Resolve(default(BaseStart), DefaultOptions());
        });

        Assert.Equal("start", ex.ParamName);
        Assert.Equal(0, source.StationReads);
    }

    // GW-U-253 (AC-3): a null snapshot source is rejected for "source".
    [Fact]
    public void NullSourceIsRejectedForSource()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new BaseCache(null!, new ManualClock());
        });

        Assert.Equal("source", ex.ParamName);
    }

    // GW-U-253 (AC-3): a null clock is rejected for "clock".
    [Fact]
    public void NullClockIsRejectedForClock()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new BaseCache(LoadedSource(), null!);
        });

        Assert.Equal("clock", ex.ParamName);
    }

    private static void ApplyFault(CountingSnapshotSource source, string fault)
    {
        switch (fault)
        {
            case "StationsNull":
                source.ReturnNullStations = true;
                break;
            case "ContainersNull":
                source.ReturnNullContainers = true;
                break;
            case "NullStationElement":
                source.Stations.Add(null!);
                break;
            case "NullContainerElement":
                source.Containers.Add(null!);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(fault));
        }
    }
}
