namespace BaseStorage.Core.Networking.Peers;

using System;

/// <summary>
/// The state of one tracked peer (contract version-rpc-contracts.md 3.2). Immutable: the table replaces an entry when it changes.
/// </summary>
public sealed class PeerVersionEntry
{
    internal PeerVersionEntry(long peerId, bool remoteIsServer, TimeSpan deadline, VersionCheckResult? result, TimeSpan? disconnectAt)
    {
        PeerId = peerId;
        RemoteIsServer = remoteIsServer;
        Deadline = deadline;
        Result = result;
        DisconnectAt = disconnectAt;
    }

    /// <summary>Gets the table-assigned peer id.</summary>
    public long PeerId { get; }

    /// <summary>Gets a value indicating whether the remote peer is the server, so the local side is a client.</summary>
    public bool RemoteIsServer { get; }

    /// <summary>Gets the clock reading at which a client without a payload is treated as having no mod. Unused for a server peer.</summary>
    public TimeSpan Deadline { get; }

    /// <summary>Gets the decision, or null while the payload is awaited.</summary>
    public VersionCheckResult? Result { get; }

    /// <summary>Gets the clock reading at which a refused client is disconnected, or null when the peer was not refused.</summary>
    public TimeSpan? DisconnectAt { get; }

    /// <summary>Gets a value indicating whether a decision has been made.</summary>
    public bool IsDecided => Result is not null;

    internal PeerVersionEntry WithResult(VersionCheckResult result, TimeSpan? disconnectAt) =>
        new PeerVersionEntry(PeerId, RemoteIsServer, Deadline, result, disconnectAt);
}
