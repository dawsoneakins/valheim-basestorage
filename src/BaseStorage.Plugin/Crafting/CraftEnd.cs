namespace BaseStorage.Plugin.Crafting;

/// <summary>How a handled craft attempt ended (contract craft-from-base 3.1).</summary>
internal enum CraftEnd
{
    /// <summary>The commit created the item; materials consumed.</summary>
    Created = 0,

    /// <summary>The commit did not create the item; every taken stack was put back.</summary>
    Returned = 1,

    /// <summary>The executor ended without success; nothing consumed.</summary>
    Failed = 2,

    /// <summary>A precondition failed before any take; nothing consumed.</summary>
    PreFlightFailed = 3,

    /// <summary>No plan at craft time (materials not there); nothing started.</summary>
    Unavailable = 4,

    /// <summary>Panel closed, selection or station changed, or no local player; nothing consumed.</summary>
    Abandoned = 5,
}
