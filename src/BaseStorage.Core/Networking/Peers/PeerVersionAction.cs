namespace BaseStorage.Core.Networking.Peers;

/// <summary>What the Plugin does with a peer after a handshake event (contract version-rpc-contracts.md 3.1).</summary>
public enum PeerVersionAction
{
    /// <summary>Nothing: the peer is unknown or already decided, or the host payload could not be read.</summary>
    Ignore = 0,

    /// <summary>The peer may stay; the outcome is logged with both versions.</summary>
    Accept = 1,

    /// <summary>The host refuses a client whose version differs: notice, log, the vanilla version error, disconnect after the grace.</summary>
    RefuseMismatch = 2,

    /// <summary>The host refuses a client without a usable payload: notice, log, the vanilla version error, disconnect after the grace.</summary>
    RefuseMissing = 3,

    /// <summary>The grace after a refusal has elapsed: the host disconnects the peer through the vanilla path.</summary>
    Disconnect = 4,

    /// <summary>The client's own check of the host version failed: raise the vanilla version error so the game leaves cleanly.</summary>
    LeaveMismatch = 5,
}
