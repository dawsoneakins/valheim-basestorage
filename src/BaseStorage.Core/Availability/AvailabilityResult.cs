namespace BaseStorage.Core.Availability;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>The outcome of <see cref="BaseAvailability.Compute"/>: one availability per requirement, the first missing one, and the containers counted or skipped (contract section 9.5).</summary>
public sealed class AvailabilityResult
{
    /// <summary>Creates a result. Produced only by <see cref="BaseAvailability.Compute"/>, so it is internal.</summary>
    internal AvailabilityResult(
        RequirementAvailability[] requirements,
        ContainerSnapshot[] countedContainers,
        ContainerSnapshot[] skippedInUse)
    {
        Requirements = Array.AsReadOnly(requirements);
        CountedContainers = Array.AsReadOnly(countedContainers);
        SkippedInUse = Array.AsReadOnly(skippedInUse);
        foreach (RequirementAvailability availability in requirements)
        {
            if (!availability.IsMet)
            {
                FirstMissing = availability;
                break;
            }
        }
    }

    /// <summary>Gets one availability per input requirement, in the same order and of the same length; read-only.</summary>
    public IReadOnlyList<RequirementAvailability> Requirements { get; }

    /// <summary>Gets the first requirement that is not met, or null when every requirement is met.</summary>
    public RequirementAvailability? FirstMissing { get; }

    /// <summary>Gets a value indicating whether every requirement is met (derived: <see cref="FirstMissing"/> is null).</summary>
    public bool AllMet => FirstMissing is null;

    /// <summary>Gets the eligible containers that were counted, in ascending id order; read-only.</summary>
    public IReadOnlyList<ContainerSnapshot> CountedContainers { get; }

    /// <summary>Gets the eligible containers that were in use and therefore skipped, in ascending id order; read-only.</summary>
    public IReadOnlyList<ContainerSnapshot> SkippedInUse { get; }
}
