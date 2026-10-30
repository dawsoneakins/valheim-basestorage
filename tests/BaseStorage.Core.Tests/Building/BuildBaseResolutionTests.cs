namespace BaseStorage.Core.Tests.Building;

using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Bases;

/// <summary>GW-U-1606 and GW-U-1607 (AC-1, AC-2; contract BF2 and B5): the base is the one whose station range holds the player.</summary>
public sealed class BuildBaseResolutionTests
{
    private static StationSnapshot Workbench(int serial, float x, float buildRange) =>
        new StationSnapshot(ResolutionFixtures.StationKey(serial), new WorldPosition(x, 0f, 0f), buildRange);

    private static CountingSnapshotSource TwoWorkbenchBase()
    {
        var source = new CountingSnapshotSource();
        source.Stations.Add(Workbench(1, 0f, 20f));
        source.Stations.Add(Workbench(2, 15f, 20f));
        source.Containers.Add(ResolutionFixtures.Container14());
        return source;
    }

    // GW-U-1606 (BF2, B5, AC-1): a player inside the second Workbench's range only is in the base, and the chest of the first is included.
    [Fact]
    public void APlayerInRangeOfOnlyTheSecondWorkbenchIsInTheBaseAndItsChestsAreIncluded()
    {
        CountingSnapshotSource source = TwoWorkbenchBase();
        var cache = new BaseCache(source, new Bases.ManualClock());

        BaseResolution resolution = cache.Resolve(BaseStart.FromPosition(new WorldPosition(25f, 0f, 0f)), ResolutionFixtures.DefaultOptions());

        Assert.True(resolution.IsInBase);
        Assert.Contains(14, ResolutionFixtures.Serials(resolution.Containers));
    }

    // GW-U-1606 (BF2, NFR): a second resolve in the same window reuses the cached resolution and makes no new station scan.
    [Fact]
    public void ASecondResolveInTheSameWindowReusesTheCachedResolution()
    {
        CountingSnapshotSource source = TwoWorkbenchBase();
        var cache = new BaseCache(source, new Bases.ManualClock());
        BaseStart start = BaseStart.FromPosition(new WorldPosition(25f, 0f, 0f));
        cache.Resolve(start, ResolutionFixtures.DefaultOptions());
        int stationReads = source.StationReads;

        BaseResolution again = cache.Resolve(start, ResolutionFixtures.DefaultOptions());

        Assert.True(again.IsInBase);
        Assert.Equal(stationReads, source.StationReads);
    }

    // GW-U-1607 (B5, AC-2): a position outside every station range is not in a base and has no containers.
    [Fact]
    public void APositionOutsideEveryStationRangeIsNotInABase()
    {
        CountingSnapshotSource source = TwoWorkbenchBase();
        var cache = new BaseCache(source, new Bases.ManualClock());

        BaseResolution resolution = cache.Resolve(BaseStart.FromPosition(new WorldPosition(200f, 0f, 0f)), ResolutionFixtures.DefaultOptions());

        Assert.False(resolution.IsInBase);
        Assert.Empty(resolution.Containers);
    }
}
