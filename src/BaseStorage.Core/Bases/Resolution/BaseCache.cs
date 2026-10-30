namespace BaseStorage.Core.Bases.Resolution;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Caches base resolution over one world snapshot per window, with event-driven invalidation (ADR-GWIN-024, spec S-E002-003).
/// A rebuild reads the source once, copies it, and reuses the copy until the window (<c>BaseCacheSeconds</c>) expires, the clock goes
/// backwards, the station cap changes, or one of the four <c>On*</c> events marks the cache dirty.
/// </summary>
/// <remarks>
/// Not thread-safe. Valheim calls it only from the main thread (S-E002-004). A failed rebuild leaves the cache dirty, so the next
/// resolve tries again and never serves a stale result.
/// </remarks>
public sealed class BaseCache
{
    private readonly IBaseSnapshotSource source;
    private readonly IClock clock;
    private readonly Dictionary<StationId, BaseResolution> memo = new Dictionary<StationId, BaseResolution>();
    private StationSnapshot[] stations = Array.Empty<StationSnapshot>();
    private ContainerSnapshot[] containers = Array.Empty<ContainerSnapshot>();
    private TimeSpan builtAt;
    private int builtCap;
    private bool dirty = true;
    private long generation;

    /// <summary>Creates an empty cache. The first resolve always rebuilds.</summary>
    /// <param name="source">The world to read on each rebuild; must not be null.</param>
    /// <param name="clock">The clock that measures the window; must not be null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="clock"/> is null.</exception>
    public BaseCache(IBaseSnapshotSource source, IClock clock)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Returns the base that <paramref name="start"/> is in, rebuilding the snapshot only when the window rules require it.</summary>
    /// <param name="start">Where the lookup starts; must be a station start or a position start, not <c>default</c>.</param>
    /// <param name="options">The window and the station cap for this call.</param>
    /// <returns>The base that contains the start, or the no-base result.</returns>
    /// <exception cref="ArgumentException"><paramref name="start"/> has no defined kind.</exception>
    public BaseResolution Resolve(BaseStart start, BaseCacheOptions options)
    {
        BaseResolver.RequireDefined(start);
        TimeSpan now = clock.Elapsed;
        if (!IsCurrentAt(now, options))
        {
            Rebuild(now, options.MaxBaseStations);
        }

        if (!BaseResolver.TryFindSeed(stations, start, out StationId seed))
        {
            return BaseResolution.NotInBase;
        }

        if (!memo.TryGetValue(seed, out BaseResolution? resolution))
        {
            resolution = BaseResolver.Build(stations, containers, seed, options.MaxBaseStations);
            memo[seed] = resolution;
        }

        return resolution;
    }

    private void Rebuild(TimeSpan now, int cap)
    {
        dirty = true;
        memo.Clear();
        IReadOnlyList<StationSnapshot>? sourceStations = source.GetStations();
        IReadOnlyList<ContainerSnapshot>? sourceContainers = source.GetContainers();
        if (sourceStations is null || sourceContainers is null)
        {
            throw new InvalidOperationException("The snapshot source returned a null list.");
        }

        var stationCopy = new StationSnapshot[sourceStations.Count];
        for (var i = 0; i < stationCopy.Length; i++)
        {
            stationCopy[i] = sourceStations[i]
                ?? throw new ArgumentException("The station list must not contain null elements.");
        }

        var containerCopy = new ContainerSnapshot[sourceContainers.Count];
        for (var i = 0; i < containerCopy.Length; i++)
        {
            containerCopy[i] = sourceContainers[i]
                ?? throw new ArgumentException("The container list must not contain null elements.");
        }

        stations = stationCopy;
        containers = containerCopy;
        builtAt = now;
        builtCap = cap;
        dirty = false;
        generation++;
    }

    /// <summary>
    /// Gets the number of successful rebuilds so far. It starts at 0 and rises by 1 at the end of each successful rebuild, so a caller
    /// can tell that the base was read again, even when the rebuild was started by another caller.
    /// </summary>
    public long Generation => generation;

    /// <summary>
    /// Reports whether <see cref="Resolve"/> with these options would answer from the current snapshot, without rebuilding.
    /// It reads the clock once, never reads the source, and never clears the memo.
    /// </summary>
    /// <param name="options">The window and the station cap to test against.</param>
    /// <returns>True when the next resolve with these options would not rebuild; false otherwise.</returns>
    public bool IsCurrent(BaseCacheOptions options) => IsCurrentAt(clock.Elapsed, options);

    private bool IsCurrentAt(TimeSpan now, BaseCacheOptions options) =>
        !dirty
        && now >= builtAt
        && now - builtAt < TimeSpan.FromSeconds(options.BaseCacheSeconds)
        && options.MaxBaseStations == builtCap;

    /// <summary>Marks the cache dirty because a station was placed. The next resolve rebuilds.</summary>
    public void OnStationPlaced() => dirty = true;

    /// <summary>Marks the cache dirty because a station was removed. The next resolve rebuilds.</summary>
    public void OnStationRemoved() => dirty = true;

    /// <summary>Marks the cache dirty because a container was placed. The next resolve rebuilds.</summary>
    public void OnContainerPlaced() => dirty = true;

    /// <summary>Marks the cache dirty because a container was removed. The next resolve rebuilds.</summary>
    public void OnContainerRemoved() => dirty = true;
}
