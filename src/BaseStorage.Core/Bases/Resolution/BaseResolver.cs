namespace BaseStorage.Core.Bases.Resolution;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Resolves which base a start is in, as a pure function of its inputs (ADR-GWIN-024). It keeps no state and never mutates its inputs.
/// Eligibility for a feature (purpose, container assignments) is not applied here; the feature applies it to the returned containers.
/// </summary>
public static class BaseResolver
{
    /// <summary>
    /// Returns the base that <paramref name="start"/> is in: its stations in fill order (up to <paramref name="maxBaseStations"/>)
    /// and the containers within build range of them, or <c>BaseResolution.NotInBase</c> when the start is in no base.
    /// </summary>
    /// <param name="stations">The crafting stations of the world; must not be null and must not contain null elements.</param>
    /// <param name="containers">The containers of the world; must not be null and must not contain null elements.</param>
    /// <param name="start">Where the lookup starts; must be a station start or a position start, not <c>default</c>.</param>
    /// <param name="maxBaseStations">The station cap; values below 1 are treated as 1.</param>
    /// <returns>The base that contains the start, or the no-base result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stations"/> or <paramref name="containers"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="start"/> has no defined kind, or a list contains a null element.</exception>
    public static BaseResolution Resolve(
        IReadOnlyList<StationSnapshot> stations,
        IReadOnlyList<ContainerSnapshot> containers,
        BaseStart start,
        int maxBaseStations)
    {
        if (stations is null)
        {
            throw new ArgumentNullException(nameof(stations));
        }

        if (containers is null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        RequireDefined(start);
        if (!TryFindSeed(stations, start, out StationId seed))
        {
            return BaseResolution.NotInBase;
        }

        return Build(stations, containers, seed, maxBaseStations);
    }

    internal static void RequireDefined(BaseStart start)
    {
        if (start.Kind != BaseStartKind.Station && start.Kind != BaseStartKind.Position)
        {
            throw new ArgumentException("The start has no defined kind.", nameof(start));
        }
    }

    internal static bool TryFindSeed(IReadOnlyList<StationSnapshot> stations, BaseStart start, out StationId seed)
    {
        seed = default;
        var found = false;
        var seen = new HashSet<StationId>();
        for (var i = 0; i < stations.Count; i++)
        {
            StationSnapshot station = stations[i];
            if (station is null)
            {
                throw new ArgumentException("The station list must not contain null elements.", nameof(stations));
            }

            if (!seen.Add(station.Id) || station.BuildRange <= 0)
            {
                continue;
            }

            if (start.Kind == BaseStartKind.Station)
            {
                if (station.Id == start.Station)
                {
                    seed = station.Id;
                    return true;
                }

                continue;
            }

            if (BuildRangeRule.IsInRange(station.Position, station.BuildRange, start.Position)
                && (!found || station.Id.CompareTo(seed) < 0))
            {
                seed = station.Id;
                found = true;
            }
        }

        return found;
    }

    internal static BaseResolution Build(
        IReadOnlyList<StationSnapshot> stations,
        IReadOnlyList<ContainerSnapshot> containers,
        StationId seed,
        int maxBaseStations)
    {
        StationGraphResult graph = StationGraph.FindBase(stations, seed, maxBaseStations);
        IReadOnlyList<ContainerSnapshot> members = BaseMembership.FindMembers(graph.Stations, containers);
        return new BaseResolution(true, graph.Stations, members, graph.IsCapped, null);
    }
}
