namespace BaseStorage.Core.Tests.Bases;

using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.ResolutionFixtures;

/// <summary>AC-3 (ADR-GWIN-024, ADR-GWIN-025): within one window the cache memoises one result per seed, and an event clears the memo.</summary>
public sealed class BaseCacheMemoTests
{
    // GW-U-250 (AC-3): starts with the same seed share one result within a window; another seed and the no-base result do not.
    [Fact]
    public void StartsWithTheSameSeedShareOneResultWithinAWindow()
    {
        var cache = new BaseCache(LoadedSource(), new ManualClock());

        BaseResolution byStation = cache.Resolve(BaseStart.FromStation(StationKey(1)), DefaultOptions());
        BaseResolution byPosition = cache.Resolve(BaseStart.FromPosition(new WorldPosition(10f, 0f, 0f)), DefaultOptions());
        BaseResolution otherBase = cache.Resolve(BaseStart.FromStation(StationKey(3)), DefaultOptions());
        BaseResolution noBase = cache.Resolve(BaseStart.FromPosition(new WorldPosition(-25f, 0f, 0f)), DefaultOptions());

        Assert.Same(byStation, byPosition);
        Assert.NotSame(byStation, otherBase);
        Assert.Same(BaseResolution.NotInBase, noBase);
    }

    // GW-U-250 (AC-3): an event clears the memo, so the same start gets a new result.
    [Fact]
    public void AnEventClearsTheMemo()
    {
        var cache = new BaseCache(LoadedSource(), new ManualClock());
        BaseResolution earlier = cache.Resolve(BaseStart.FromStation(StationKey(1)), DefaultOptions());

        cache.OnContainerPlaced();
        BaseResolution later = cache.Resolve(BaseStart.FromStation(StationKey(1)), DefaultOptions());

        Assert.NotSame(earlier, later);
    }
}
