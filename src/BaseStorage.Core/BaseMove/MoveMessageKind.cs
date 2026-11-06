namespace BaseStorage.Core.BaseMove;

/// <summary>The kinds of message a move sends over its one RPC.</summary>
public enum MoveMessageKind
{
    /// <summary>Requester to host: list the named bases other than the one around this station.</summary>
    ListAsk = 0,

    /// <summary>Host to requester: the named bases.</summary>
    ListReply = 1,

    /// <summary>Requester to host: plan a move.</summary>
    Request = 2,

    /// <summary>Host to requester: the preview or a refusal.</summary>
    Preview = 3,

    /// <summary>Requester to host: the player confirmed, with the held chests.</summary>
    Confirm = 4,

    /// <summary>Requester to host: the player backed out or the holds failed.</summary>
    Cancel = 5,

    /// <summary>Host to requester: the move was cancelled and nothing was written.</summary>
    Cancelled = 6,

    /// <summary>Host to requester: the write happened.</summary>
    Done = 7,

    /// <summary>A requester asks the host to rename a base.</summary>
    Rename = 8,

    /// <summary>The host renamed a base, with the new list.</summary>
    Renamed = 9,

    /// <summary>The host refused a rename because of a ward.</summary>
    RenameRefused = 10,
}
