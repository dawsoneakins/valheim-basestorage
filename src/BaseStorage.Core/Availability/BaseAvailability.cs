namespace BaseStorage.Core.Availability;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Snapshots;

/// <summary>Answers how many of each required item the player can use for a craft, upgrade or build, and which requirement is missing first (contract section 9.6).</summary>
/// <remarks>
/// The methods are pure. They never mutate their arguments, read a clock, log or do I/O, and are safe to call from any thread on immutable inputs.
/// </remarks>
public static class BaseAvailability
{
    /// <summary>Computes the availability of every requirement against the snapshot.</summary>
    /// <param name="snapshot">The inventory, containers, assignments and options; must not be null.</param>
    /// <param name="requirements">The requirements in the order to report them; must not be null and must not contain null elements. May be empty.</param>
    /// <returns>The per-requirement availability, the first missing requirement, and the counted and skipped containers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> or <paramref name="requirements"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="requirements"/> contains a null element.</exception>
    public static AvailabilityResult Compute(BaseSnapshot snapshot, IReadOnlyList<CraftRequirement> requirements)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (requirements is null)
        {
            throw new ArgumentNullException(nameof(requirements));
        }

        foreach (CraftRequirement requirement in requirements)
        {
            if (requirement is null)
            {
                throw new ArgumentException("Must not contain null elements.", nameof(requirements));
            }
        }

        EligibilityResult eligibility = ContainerEligibility.Evaluate(
            snapshot.Containers,
            snapshot.Assignments,
            ContainerPurpose.CraftAvailability,
            snapshot.Options);
        var counted = new List<ContainerSnapshot>();
        var skipped = new List<ContainerSnapshot>();
        foreach (ContainerSnapshot container in eligibility.Eligible)
        {
            if ((container.Flags & ContainerFlags.InUseByOther) != 0)
            {
                skipped.Add(container);
            }
            else
            {
                counted.Add(container);
            }
        }

        var results = new RequirementAvailability[requirements.Count];
        for (int i = 0; i < results.Length; i++)
        {
            CraftRequirement requirement = requirements[i];
            int inInventory = Count(snapshot.PlayerInventory, requirement);
            int inContainers = 0;
            foreach (ContainerSnapshot container in counted)
            {
                inContainers = Add(inContainers, Count(container, requirement));
            }

            results[i] = new RequirementAvailability(requirement, inInventory, inContainers);
        }

        return new AvailabilityResult(results, counted.ToArray(), skipped.ToArray());
    }

    /// <summary>Returns the first requirement that is not met, equal to <c>Compute(snapshot, requirements).FirstMissing</c>.</summary>
    /// <param name="snapshot">The inventory, containers, assignments and options; must not be null.</param>
    /// <param name="requirements">The requirements in the order to check them; must not be null and must not contain null elements. May be empty.</param>
    /// <returns>The first unmet availability, or null when every requirement is met.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> or <paramref name="requirements"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="requirements"/> contains a null element.</exception>
    public static RequirementAvailability? FindFirstMissing(BaseSnapshot snapshot, IReadOnlyList<CraftRequirement> requirements)
    {
        return Compute(snapshot, requirements).FirstMissing;
    }

    private static int Count(ContainerSnapshot container, CraftRequirement requirement)
    {
        int sum = 0;
        foreach (ItemStackSnapshot stack in container.Contents)
        {
            if (Matches(stack, requirement))
            {
                sum = Add(sum, stack.Count);
            }
        }

        return sum;
    }

    internal static bool Matches(ItemStackSnapshot stack, CraftRequirement requirement)
    {
        if (!string.Equals(stack.PrefabName, requirement.PrefabName, StringComparison.Ordinal))
        {
            return false;
        }

        return requirement.QualityMatch switch
        {
            QualityMatch.Exact => stack.Quality == requirement.Quality,
            QualityMatch.AtLeast => stack.Quality >= requirement.Quality,
            _ => true,
        };
    }

    private static int Add(int left, int right) => (int)Math.Min((long)left + right, int.MaxValue);
}
