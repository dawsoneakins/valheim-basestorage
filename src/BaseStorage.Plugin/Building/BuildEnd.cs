namespace BaseStorage.Plugin.Building;

/// <summary>How a handled build attempt ended (contract build-from-base 3.1).</summary>
public enum BuildEnd
{
    /// <summary>The commit created the piece; materials consumed.</summary>
    Placed = 0,

    /// <summary>The commit did not create the piece; every taken stack was put back.</summary>
    Returned = 1,

    /// <summary>The executor ended without success; nothing consumed.</summary>
    Failed = 2,

    /// <summary>A pre-flight row failed while waiting; nothing consumed.</summary>
    PreFlightFailed = 3,

    /// <summary>No plan at place time (materials not there); nothing started.</summary>
    Unavailable = 4,

    /// <summary>Place mode left, piece changed, a panel opened, player dead or gone; nothing consumed.</summary>
    Abandoned = 5,

    /// <summary>The placement was not valid at the click; vanilla showed its reason; nothing started.</summary>
    PlacementRefused = 6,
}
