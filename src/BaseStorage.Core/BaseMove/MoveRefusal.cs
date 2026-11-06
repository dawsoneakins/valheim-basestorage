namespace BaseStorage.Core.BaseMove;

/// <summary>Why the host refused or cancelled a move.</summary>
public enum MoveRefusal
{
    /// <summary>Nothing is wrong.</summary>
    None = 0,

    /// <summary>Another move is running.</summary>
    Busy = 1,

    /// <summary>The source or destination base could not be found.</summary>
    NoSuchBase = 2,

    /// <summary>A player other than the requester or the host is at one of the bases; the message names them.</summary>
    PlayerAtBase = 3,

    /// <summary>The two bases share a chest or an active area, so Organize is the right tool.</summary>
    BasesTooNear = 4,

    /// <summary>The chests changed after the preview.</summary>
    Changed = 5,

    /// <summary>A chest could not be held or did not catch up in time.</summary>
    TimedOut = 6,

    /// <summary>The move failed before anything was written.</summary>
    Failed = 7,
}
