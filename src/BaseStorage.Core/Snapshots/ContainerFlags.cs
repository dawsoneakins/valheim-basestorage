namespace BaseStorage.Core.Snapshots;

using System;

/// <summary>Game-observed state of a container.</summary>
[Flags]
public enum ContainerFlags
{
    /// <summary>No flags.</summary>
    None = 0,

    /// <summary>A player-built, stationary piece.</summary>
    PlayerBuilt = 1,

    /// <summary>Part of a cart or ship.</summary>
    Vehicle = 2,

    /// <summary>A player tombstone.</summary>
    Tombstone = 4,

    /// <summary>The local player cannot open it under vanilla rules.</summary>
    AccessDenied = 8,

    /// <summary>Another player currently has it open.</summary>
    InUseByOther = 16,
}
