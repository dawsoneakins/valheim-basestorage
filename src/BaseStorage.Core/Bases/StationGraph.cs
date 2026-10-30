namespace BaseStorage.Core.Bases;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Finds the base that a crafting station belongs to, as a flood fill over build ranges (S-E002-001, contract section 7).
/// </summary>
/// <remarks>
/// A station is eligible when its build range is above 0, and an ineligible station never joins or bridges a base.
/// Two eligible stations are connected when either one is inside the other's build range, measured with the single rule
/// in <c>BuildRangeRule</c> (ADR-GWIN-016: horizontal distance, strict comparison). The fill is breadth-first from the
/// start station, with neighbours taken in ascending <see cref="StationId"/> order, and it stops once the cap is exceeded.
/// An empty result means the start station is missing or ineligible.
/// </remarks>
public static class StationGraph
{
    /// <summary>
    /// Returns the stations of the base that contains <paramref name="start"/>, in fill order, up to <paramref name="maxBaseStations"/>.
    /// </summary>
    /// <param name="stations">The stations to search; must not be null and must not contain null elements.</param>
    /// <param name="start">The id of the station the fill starts from.</param>
    /// <param name="maxBaseStations">The station cap; values below 1 are treated as 1.</param>
    /// <returns>The stations of the base in fill order, and whether more stations were connected than the cap allowed.</returns>
    public static StationGraphResult FindBase(IReadOnlyList<StationSnapshot> stations, StationId start, int maxBaseStations)
    {
        if (stations is null)
        {
            throw new ArgumentNullException(nameof(stations));
        }

        var seen = new HashSet<StationId>();
        var eligible = new List<StationSnapshot>();
        for (var i = 0; i < stations.Count; i++)
        {
            var station = stations[i];
            if (station is null)
            {
                throw new ArgumentException("The station list must not contain null elements.", nameof(stations));
            }

            if (seen.Add(station.Id) && station.BuildRange > 0)
            {
                eligible.Add(station);
            }
        }

        var cap = maxBaseStations < 1 ? 1 : maxBaseStations;
        eligible.Sort((a, b) => a.Id.CompareTo(b.Id));

        var startIndex = -1;
        for (var i = 0; i < eligible.Count; i++)
        {
            if (eligible[i].Id == start)
            {
                startIndex = i;
                break;
            }
        }

        if (startIndex < 0)
        {
            return new StationGraphResult(Array.Empty<StationSnapshot>(), false);
        }

        var visited = new bool[eligible.Count];
        var found = new List<StationSnapshot>();
        var queue = new Queue<int>();
        visited[startIndex] = true;
        found.Add(eligible[startIndex]);
        queue.Enqueue(startIndex);

        while (queue.Count > 0)
        {
            var current = eligible[queue.Dequeue()];
            for (var i = 0; i < eligible.Count; i++)
            {
                if (visited[i])
                {
                    continue;
                }

                var other = eligible[i];
                var connected = BuildRangeRule.IsInRange(current.Position, current.BuildRange, other.Position)
                    || BuildRangeRule.IsInRange(other.Position, other.BuildRange, current.Position);
                if (!connected)
                {
                    continue;
                }

                if (found.Count >= cap)
                {
                    return new StationGraphResult(found.ToArray(), true);
                }

                visited[i] = true;
                found.Add(other);
                queue.Enqueue(i);
            }
        }

        return new StationGraphResult(found.ToArray(), false);
    }
}
