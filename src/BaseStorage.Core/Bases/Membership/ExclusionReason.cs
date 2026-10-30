namespace BaseStorage.Core.Bases.Membership;

/// <summary>
/// Why a member container is not usable for a purpose. Exactly one reason is reported per excluded container.
/// The numeric value is the precedence rank: when several rules match, the reason with the lowest value is reported.
/// </summary>
public enum ExclusionReason
{
    /// <summary>The container is a player tombstone (<c>ContainerFlags.Tombstone</c>). Applies regardless of settings and purpose.</summary>
    Tombstone = 1,

    /// <summary>The container is on a cart or ship (<c>ContainerFlags.Vehicle</c>) and ExcludeVehicles is on. Applies to every purpose.</summary>
    Vehicle = 2,

    /// <summary>The container has no <c>ContainerFlags.PlayerBuilt</c> flag. Applies regardless of settings and purpose.</summary>
    NotPlayerBuilt = 3,

    /// <summary>The local player cannot open the container (<c>ContainerFlags.AccessDenied</c>). Applies regardless of settings and purpose.</summary>
    AccessDenied = 4,

    /// <summary>The container is assigned Keep, and the purpose is Organize, or the purpose is CraftAvailability with KeepChestsCraftable off.</summary>
    Keep = 5,
}
