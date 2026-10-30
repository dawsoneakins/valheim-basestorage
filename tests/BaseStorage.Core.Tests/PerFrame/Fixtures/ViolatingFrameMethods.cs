namespace BaseStorage.Core.Tests.PerFrame.Fixtures.Violating;

using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Resolution;

/// <summary>An Update that calls the base resolver directly.</summary>
public sealed class ResolvesInUpdate
{
    public void Update()
    {
        _ = BaseResolver.Resolve(null!, null!, default, 0);
    }
}

/// <summary>A LateUpdate that rebuilds the cache through BaseCache.Resolve.</summary>
public sealed class RebuildsCacheInLateUpdate
{
    private readonly BaseCache cache = null!;

    public void LateUpdate()
    {
        _ = this.cache.Resolve(default, default);
    }
}

/// <summary>An Update that reads the world through the snapshot source interface.</summary>
public sealed class ReadsSnapshotsInUpdate
{
    private readonly IBaseSnapshotSource source = null!;

    public void Update()
    {
        _ = this.source.GetStations();
    }
}

/// <summary>A static Update that runs the consumption planner.</summary>
public static class PlansInStaticUpdate
{
    public static void Update()
    {
        _ = ConsumptionPlanner.Plan(null!, null!);
    }
}
