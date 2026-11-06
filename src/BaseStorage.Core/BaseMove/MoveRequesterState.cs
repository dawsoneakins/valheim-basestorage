namespace BaseStorage.Core.BaseMove;

/// <summary>Where a requester's move stands.</summary>
public enum MoveRequesterState
{
    /// <summary>No move is running.</summary>
    Idle = 0,

    /// <summary>Waiting for the host's preview.</summary>
    AwaitingPreview = 1,

    /// <summary>The preview is showing.</summary>
    Previewing = 2,

    /// <summary>The player confirmed and the source chests are being held.</summary>
    Holding = 3,

    /// <summary>The confirm was sent; the host may write at any moment, so the holds must stay.</summary>
    AwaitingWrite = 4,

    /// <summary>The host wrote; the holds stay until this game has the written revisions.</summary>
    Settling = 5,
}
