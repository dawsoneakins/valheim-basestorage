namespace BaseStorage.Plugin.Building;

/// <summary>Why the build-from-base gate is open or closed (contract build-from-base 3.1 and 5.1).</summary>
internal enum BuildGateReason
{
    /// <summary>The gate is open: the build uses the base.</summary>
    UsesBase = 0,

    /// <summary>The CraftFromBase setting is off.</summary>
    SettingOff = 1,

    /// <summary>The player is not the local player.</summary>
    NotLocalPlayer = 2,

    /// <summary>The no-cost cheat is active.</summary>
    NoCost = 3,

    /// <summary>A craft is pending.</summary>
    CraftPending = 4,

    /// <summary>The player is in no base.</summary>
    NotInBase = 5,

    /// <summary>The check mode is not the build mode.</summary>
    Mode = 6,

    /// <summary>The piece is free to build in this world.</summary>
    FreeBuild = 7,

    /// <summary>A requirement line is an upgrader resource.</summary>
    UpgraderResource = 8,

    /// <summary>The requirements or the inventory could not be read.</summary>
    Unreadable = 9,
}
