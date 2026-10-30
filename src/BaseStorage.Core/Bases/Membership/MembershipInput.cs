namespace BaseStorage.Core.Bases.Membership;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Shared input handling for the membership and eligibility methods (ADR-GWIN-022). Internal, so it is not part of the public surface.
/// </summary>
internal static class MembershipInput
{
    /// <summary>
    /// Scans the whole list for null elements, then keeps the first snapshot per id and drops later duplicates (contract M5).
    /// </summary>
    /// <param name="containers">The container list; the caller has already checked that it is not null.</param>
    /// <param name="paramName">The parameter name used in the exception thrown for a null element.</param>
    /// <returns>A new list with one snapshot per id, in the order of the first occurrences.</returns>
    /// <exception cref="ArgumentException"><paramref name="containers"/> contains a null element.</exception>
    internal static List<ContainerSnapshot> FirstPerId(IReadOnlyList<ContainerSnapshot> containers, string paramName)
    {
        var seen = new HashSet<ContainerId>();
        var result = new List<ContainerSnapshot>(containers.Count);
        for (var i = 0; i < containers.Count; i++)
        {
            ContainerSnapshot? c = containers[i];
            if (c is null)
            {
                throw new ArgumentException("The list contains a null element.", paramName);
            }

            if (seen.Add(c.Id))
            {
                result.Add(c);
            }
        }

        return result;
    }

    /// <summary>Sorts the list in ascending <see cref="ContainerId"/> order (contract M6).</summary>
    /// <param name="list">The list to sort in place; ids must be unique.</param>
    internal static void SortById(List<ContainerSnapshot> list)
    {
        list.Sort((a, b) => a.Id.CompareTo(b.Id));
    }

    /// <summary>Throws unless <paramref name="purpose"/> is a defined <see cref="ContainerPurpose"/> value (contract M4).</summary>
    /// <param name="purpose">The purpose to check.</param>
    /// <param name="paramName">The parameter name used in the exception thrown for an undefined value.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="purpose"/> is not a defined value.</exception>
    internal static void RequireDefined(ContainerPurpose purpose, string paramName)
    {
        if (purpose != ContainerPurpose.Organize && purpose != ContainerPurpose.CraftAvailability)
        {
            throw new ArgumentOutOfRangeException(paramName, purpose, "The purpose is not a defined value.");
        }
    }
}
