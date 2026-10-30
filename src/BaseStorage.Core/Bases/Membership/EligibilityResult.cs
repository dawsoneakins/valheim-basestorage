namespace BaseStorage.Core.Bases.Membership;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>The outcome of <see cref="ContainerEligibility.Evaluate"/>: eligible containers, and excluded containers with one reason each.</summary>
public sealed class EligibilityResult
{
    internal EligibilityResult(ContainerSnapshot[] eligible, ContainerExclusion[] excluded)
    {
        Eligible = Array.AsReadOnly(eligible);
        Excluded = Array.AsReadOnly(excluded);
    }

    /// <summary>Gets the eligible containers in ascending id order; never null, and read-only.</summary>
    public IReadOnlyList<ContainerSnapshot> Eligible { get; }

    /// <summary>Gets the excluded containers with their reasons, in ascending id order; never null, and read-only.</summary>
    public IReadOnlyList<ContainerExclusion> Excluded { get; }
}
