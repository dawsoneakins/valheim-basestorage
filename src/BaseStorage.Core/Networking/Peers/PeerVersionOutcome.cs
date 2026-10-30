namespace BaseStorage.Core.Networking.Peers;

using System;
using BaseStorage.Core.Ui;

/// <summary>
/// One handshake event for one peer: the action the Plugin takes and the check result behind it (contract
/// version-rpc-contracts.md 3.3). The notice key follows the action: the mismatch key for a refused or left mismatch, the missing
/// key for a refused missing payload, none otherwise.
/// </summary>
public sealed class PeerVersionOutcome
{
    internal PeerVersionOutcome(long peerId, bool remoteIsServer, PeerVersionAction action, VersionCheckResult? result)
    {
        PeerId = peerId;
        RemoteIsServer = remoteIsServer;
        Action = action;
        Result = result;
    }

    /// <summary>Gets the table-assigned peer id.</summary>
    public long PeerId { get; }

    /// <summary>Gets a value indicating whether the remote peer is the server.</summary>
    public bool RemoteIsServer { get; }

    /// <summary>Gets the action the Plugin takes.</summary>
    public PeerVersionAction Action { get; }

    /// <summary>Gets the check result behind the action, or null for <see cref="PeerVersionAction.Ignore"/>.</summary>
    public VersionCheckResult? Result { get; }

    /// <summary>
    /// Gets the strings-table key of the notice: <c>StringKeys.NetworkVersionMismatch</c> for a mismatch (refused or left),
    /// <c>StringKeys.NetworkVersionMissing</c> for a refused missing payload, null for every other action.
    /// </summary>
    public string? MessageKey
    {
        get
        {
            switch (Action)
            {
                case PeerVersionAction.RefuseMismatch:
                case PeerVersionAction.LeaveMismatch:
                    return StringKeys.NetworkVersionMismatch;
                case PeerVersionAction.RefuseMissing:
                    return StringKeys.NetworkVersionMissing;
                default:
                    return null;
            }
        }
    }

    /// <summary>Formats the notice through the strings table with the host version as {0} and the client version as {1}.</summary>
    /// <param name="table">The strings table; never null.</param>
    /// <returns>The notice text, or null when <see cref="MessageKey"/> is null.</returns>
    /// <exception cref="ArgumentNullException">The table is null.</exception>
    public string? FormatMessage(StringTable table)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        string? key = MessageKey;
        return key is null || Result is null ? null : table.Format(key, Result.HostVersion, Result.ClientVersion);
    }
}
