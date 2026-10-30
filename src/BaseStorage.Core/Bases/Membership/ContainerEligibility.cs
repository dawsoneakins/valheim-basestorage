namespace BaseStorage.Core.Bases.Membership;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Decides which containers a feature may use for a purpose, and gives one reason code for each container it may not use.
/// </summary>
/// <remarks>
/// Rules are applied in a fixed precedence (contract section 2.1): Tombstone, Vehicle (only when ExcludeVehicles is on),
/// NotPlayerBuilt, AccessDenied, Keep (Organize always; CraftAvailability only when KeepChestsCraftable is off).
/// InUseByOther is not an exclusion. The methods are pure and never mutate their arguments.
/// </remarks>
public static class ContainerEligibility
{
    /// <summary>Returns the reason <paramref name="container"/> is excluded for <paramref name="purpose"/>, or null when it is eligible.</summary>
    /// <param name="container">The container to test; must not be null.</param>
    /// <param name="isKeep">True when the container is assigned Keep.</param>
    /// <param name="purpose">The purpose the container would be used for; must be a defined value.</param>
    /// <param name="options">The ExcludeVehicles and KeepChestsCraftable settings.</param>
    /// <returns>The highest-precedence matching reason, or null when no rule matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="container"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="purpose"/> is not a defined value.</exception>
    public static ExclusionReason? GetExclusionReason(
        ContainerSnapshot container,
        bool isKeep,
        ContainerPurpose purpose,
        EligibilityOptions options)
    {
        if (container is null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        MembershipInput.RequireDefined(purpose, nameof(purpose));
        return Reason(container, isKeep, purpose, options);
    }

    /// <summary>Splits <paramref name="containers"/> into the eligible containers and the excluded containers with their reasons.</summary>
    /// <param name="containers">The containers to evaluate, usually the members of one base; must not be null and must not contain null elements.</param>
    /// <param name="assignments">The persisted category assignment per container id; a missing id means Auto. Must not be null and must not contain null values.</param>
    /// <param name="purpose">The purpose the containers would be used for; must be a defined value.</param>
    /// <param name="options">The ExcludeVehicles and KeepChestsCraftable settings.</param>
    /// <returns>The eligible and excluded containers, each in ascending <see cref="ContainerId"/> order, first snapshot per id only.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="containers"/> or <paramref name="assignments"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="containers"/> contains a null element, or <paramref name="assignments"/> contains a null value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="purpose"/> is not a defined value.</exception>
    public static EligibilityResult Evaluate(
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        ContainerPurpose purpose,
        EligibilityOptions options)
    {
        if (containers is null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        List<ContainerSnapshot> unique = MembershipInput.FirstPerId(containers, nameof(containers));
        if (assignments is null)
        {
            throw new ArgumentNullException(nameof(assignments));
        }

        foreach (KeyValuePair<ContainerId, CategoryAssignment> pair in assignments)
        {
            if (pair.Value is null)
            {
                throw new ArgumentException("The assignments contain a null value.", nameof(assignments));
            }
        }

        MembershipInput.RequireDefined(purpose, nameof(purpose));
        MembershipInput.SortById(unique);
        var eligible = new List<ContainerSnapshot>();
        var excluded = new List<ContainerExclusion>();
        foreach (ContainerSnapshot container in unique)
        {
            var isKeep = assignments.TryGetValue(container.Id, out CategoryAssignment? a) && a.IsKeep;
            ExclusionReason? reason = Reason(container, isKeep, purpose, options);
            if (reason.HasValue)
            {
                excluded.Add(new ContainerExclusion(container, reason.Value));
            }
            else
            {
                eligible.Add(container);
            }
        }

        return new EligibilityResult(eligible.ToArray(), excluded.ToArray());
    }

    private static ExclusionReason? Reason(ContainerSnapshot container, bool isKeep, ContainerPurpose purpose, EligibilityOptions options)
    {
        ContainerFlags f = container.Flags;
        if ((f & ContainerFlags.Tombstone) != 0)
        {
            return ExclusionReason.Tombstone;
        }

        if (options.ExcludeVehicles && (f & ContainerFlags.Vehicle) != 0)
        {
            return ExclusionReason.Vehicle;
        }

        if ((f & ContainerFlags.PlayerBuilt) == 0)
        {
            return ExclusionReason.NotPlayerBuilt;
        }

        if ((f & ContainerFlags.AccessDenied) != 0)
        {
            return ExclusionReason.AccessDenied;
        }

        if (isKeep && (purpose == ContainerPurpose.Organize || !options.KeepChestsCraftable))
        {
            return ExclusionReason.Keep;
        }

        return null;
    }
}
