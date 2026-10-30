namespace BaseStorage.Core.Networking;

/// <summary>The outcome of a host-side version check (contract version-handshake-contracts.md 4.1).</summary>
public enum VersionCheckStatus
{
    /// <summary>The peer may join: the versions are the same, or RequireSameVersion is off.</summary>
    Accepted = 0,

    /// <summary>RequireSameVersion is on and the client version differs from the host version; both versions are known.</summary>
    RejectedMismatch = 1,

    /// <summary>RequireSameVersion is on and the client has no usable version: no mod, no payload, or a malformed payload.</summary>
    RejectedMissing = 2,
}
