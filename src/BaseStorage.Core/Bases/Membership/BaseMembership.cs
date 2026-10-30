namespace BaseStorage.Core.Bases.Membership;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Finds the containers that belong to one base: those within build range of at least one of the base's stations.
/// </summary>
/// <remarks>
/// Range uses the single rule in <c>BuildRangeRule</c> (ADR-GWIN-016: horizontal distance, strict comparison), so a container at
/// exactly a station's build range is not in range. A container in range of two bases is a member of each; call once per base.
/// </remarks>
public static class BaseMembership
{
    /// <summary>Returns the containers within build range of at least one station in <paramref name="stations"/>.</summary>
    /// <param name="stations">The stations of one base, usually <see cref="StationGraphResult.Stations"/>; must not be null and must not contain null elements.</param>
    /// <param name="containers">The loaded containers to test; must not be null and must not contain null elements.</param>
    /// <returns>The member containers in ascending <see cref="ContainerId"/> order, first snapshot per id only; never null, and read-only.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stations"/> or <paramref name="containers"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="stations"/> or <paramref name="containers"/> contains a null element.</exception>
    public static IReadOnlyList<ContainerSnapshot> FindMembers(
        IReadOnlyList<StationSnapshot> stations,
        IReadOnlyList<ContainerSnapshot> containers)
    {
        if (stations is null)
        {
            throw new ArgumentNullException(nameof(stations));
        }

        for (var i = 0; i < stations.Count; i++)
        {
            if (stations[i] is null)
            {
                throw new ArgumentException("The stations list contains a null element.", nameof(stations));
            }
        }

        if (containers is null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        List<ContainerSnapshot> unique = MembershipInput.FirstPerId(containers, nameof(containers));
        var members = new List<ContainerSnapshot>();
        foreach (ContainerSnapshot container in unique)
        {
            for (var i = 0; i < stations.Count; i++)
            {
                StationSnapshot station = stations[i];
                if (BuildRangeRule.IsInRange(station.Position, station.BuildRange, container.Position))
                {
                    members.Add(container);
                    break;
                }
            }
        }

        MembershipInput.SortById(members);
        return members.AsReadOnly();
    }
}
