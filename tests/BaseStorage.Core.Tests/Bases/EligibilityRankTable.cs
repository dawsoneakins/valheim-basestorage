namespace BaseStorage.Core.Tests.Bases;

using BaseStorage.Core.Bases.Membership;

/// <summary>
/// An independent literal copy of contract table 2.1 (ADR-GWIN-023). The five predicates are listed in rank order, and the first
/// match is the expected reason. Used only by the exhaustive sweep, so Core is never the oracle for its own expected values.
/// </summary>
internal static class EligibilityRankTable
{
    /// <summary>The expected reason for one combination of flags, purpose and settings, or null when the container is eligible.</summary>
    /// <param name="tombstone">The container has the Tombstone flag.</param>
    /// <param name="vehicle">The container has the Vehicle flag.</param>
    /// <param name="playerBuilt">The container has the PlayerBuilt flag.</param>
    /// <param name="accessDenied">The container has the AccessDenied flag.</param>
    /// <param name="isKeep">The container is assigned Keep.</param>
    /// <param name="purpose">The purpose the container would be used for.</param>
    /// <param name="excludeVehicles">The ExcludeVehicles setting.</param>
    /// <param name="keepChestsCraftable">The KeepChestsCraftable setting.</param>
    /// <returns>The reason of the first matching rank, or null when none matches.</returns>
    internal static ExclusionReason? Expected(
        bool tombstone,
        bool vehicle,
        bool playerBuilt,
        bool accessDenied,
        bool isKeep,
        ContainerPurpose purpose,
        bool excludeVehicles,
        bool keepChestsCraftable)
    {
        if (tombstone)
        {
            return ExclusionReason.Tombstone;
        }

        if (vehicle && excludeVehicles)
        {
            return ExclusionReason.Vehicle;
        }

        if (!playerBuilt)
        {
            return ExclusionReason.NotPlayerBuilt;
        }

        if (accessDenied)
        {
            return ExclusionReason.AccessDenied;
        }

        if (isKeep && (purpose == ContainerPurpose.Organize || !keepChestsCraftable))
        {
            return ExclusionReason.Keep;
        }

        return null;
    }
}
