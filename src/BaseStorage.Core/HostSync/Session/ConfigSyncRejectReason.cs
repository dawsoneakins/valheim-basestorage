namespace BaseStorage.Core.HostSync.Session;

/// <summary>Why a received payload was rejected (contract config-sync-contracts.md 3.3).</summary>
public enum ConfigSyncRejectReason
{
    /// <summary>Not a rejection.</summary>
    None = 0,

    /// <summary>Received while the mode is not Client: a host or solo player never applies a payload.</summary>
    NotClient = 1,

    /// <summary>The peer is not the host whose handshake was accepted, or no handshake was accepted.</summary>
    NoAcceptedHost = 2,

    /// <summary>The bytes are not a canonical payload (null, empty, oversize, bad tag, bad value and so on).</summary>
    Malformed = 3,
}
