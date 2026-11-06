namespace BaseStorage.Core.BaseMove;

/// <summary>Where the host's current move stands.</summary>
public enum MoveHostState
{
    /// <summary>No move is running.</summary>
    Idle = 0,

    /// <summary>Finding the bases and planning.</summary>
    Planning = 1,

    /// <summary>The preview was sent; waiting for the requester.</summary>
    AwaitingConfirm = 2,

    /// <summary>The requester confirmed; the host is holding its own loaded chests.</summary>
    HoldingOwn = 3,

    /// <summary>Waiting for the host's copy of the requester's held chests to catch up.</summary>
    AwaitingRevisions = 4,
}
