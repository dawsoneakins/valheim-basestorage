namespace BaseStorage.Plugin.StationActions;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Fill;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>The pure base facts of the station panel: station count, eligible chest count and the live Organize lock holder (contract S-E004-012 section 3.1).</summary>
internal static class StationActionsFacts
{
    /// <summary>Counts the stations of the base.</summary>
    /// <param name="resolution">The resolution of the station's base.</param>
    /// <returns>The station count; 0 when the start is in no base.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolution"/> is null.</exception>
    internal static int StationCount(BaseResolution resolution)
    {
        if (resolution is null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        return resolution.IsInBase ? resolution.Stations.Count : 0;
    }

    /// <summary>Counts the member chests eligible for Organize.</summary>
    /// <param name="resolution">The resolution of the station's base.</param>
    /// <param name="assignments">The category assignments by chest.</param>
    /// <param name="options">The eligibility options.</param>
    /// <returns>The eligible count; 0 when the start is in no base.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    internal static int EligibleChestCount(
        BaseResolution resolution,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        EligibilityOptions options)
    {
        if (resolution is null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        if (assignments is null)
        {
            throw new ArgumentNullException(nameof(assignments));
        }

        if (!resolution.IsInBase)
        {
            return 0;
        }

        return ContainerEligibility.Evaluate(resolution.Containers, assignments, ContainerPurpose.Organize, options).Eligible.Count;
    }

    /// <summary>Computes the fill percent over the eligible chests of the base, the same set that <see cref="EligibleChestCount"/> counts.</summary>
    /// <param name="resolution">The resolution of the station's base.</param>
    /// <param name="assignments">The category assignments by container.</param>
    /// <param name="options">The eligibility options.</param>
    /// <returns>The fill percent from 0 to 100; 0 when the start is in no base.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    internal static int FillPercent(
        BaseResolution resolution,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        EligibilityOptions options)
    {
        if (resolution is null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        if (assignments is null)
        {
            throw new ArgumentNullException(nameof(assignments));
        }

        if (!resolution.IsInBase)
        {
            return 0;
        }

        return BaseFill.FromContainers(ContainerEligibility.Evaluate(resolution.Containers, assignments, ContainerPurpose.Organize, options).Eligible).Percent;
    }

    /// <summary>Finds the holder name of the first live lock record over the stations, in ascending station id order.</summary>
    /// <param name="stations">The stations of the base.</param>
    /// <param name="store">The lock store; only read.</param>
    /// <param name="now">The clock reading.</param>
    /// <param name="organizeLockSeconds">The OrganizeLockSeconds setting.</param>
    /// <returns>The holder name, or null when no record is live.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    internal static string? HolderName(IReadOnlyList<StationSnapshot> stations, IOrganizeLockStore store, TimeSpan now, int organizeLockSeconds)
    {
        if (stations is null)
        {
            throw new ArgumentNullException(nameof(stations));
        }

        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        var ids = new List<StationId>(stations.Count);
        for (int i = 0; i < stations.Count; i++)
        {
            ids.Add(stations[i].Id);
        }

        ids.Sort();
        TimeSpan duration = TimeSpan.FromSeconds(Math.Max(1, organizeLockSeconds));
        TimeSpan limit = TimeSpan.MaxValue - now < duration ? TimeSpan.MaxValue : now + duration;
        bool hasLast = false;
        StationId last = default;
        for (int i = 0; i < ids.Count; i++)
        {
            StationId id = ids[i];
            if (hasLast && id.Equals(last))
            {
                continue;
            }

            hasLast = true;
            last = id;
            OrganizeLockRecord? record = OrganizeLockCodec.Decode(store.Read(id));
            if (record != null && now < record.ExpiresAt && record.ExpiresAt <= limit && !string.IsNullOrWhiteSpace(record.HolderName))
            {
                return record.HolderName;
            }
        }

        return null;
    }
}
