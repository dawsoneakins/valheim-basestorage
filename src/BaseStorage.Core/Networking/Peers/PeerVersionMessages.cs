namespace BaseStorage.Core.Networking.Peers;

using System;
using System.Globalization;

/// <summary>
/// The Info log line of each handshake outcome (contract version-rpc-contracts.md 4, L1-L5). Log text, not user-facing text:
/// the notice comes from the strings table through <see cref="PeerVersionOutcome.FormatMessage"/>. Invariant culture throughout.
/// </summary>
public static class PeerVersionMessages
{
    private const string Prefix = "Version handshake: peer ";

    /// <summary>Formats the log line of an outcome, or null for an action that logs nothing.</summary>
    /// <param name="outcome">The outcome; never null.</param>
    /// <returns>The log line, or null for <see cref="PeerVersionAction.Ignore"/>.</returns>
    /// <exception cref="ArgumentNullException">The outcome is null.</exception>
    public static string? Format(PeerVersionOutcome outcome)
    {
        if (outcome is null)
        {
            throw new ArgumentNullException(nameof(outcome));
        }

        string head = Prefix + outcome.PeerId.ToString(CultureInfo.InvariantCulture);
        VersionCheckResult? result = outcome.Result;
        switch (outcome.Action)
        {
            case PeerVersionAction.Accept:
                return head + " accepted, host " + Host(result) + ", client " + Client(result);
            case PeerVersionAction.RefuseMismatch:
                return head + " refused, versions differ, host " + Host(result) + ", client " + Client(result);
            case PeerVersionAction.RefuseMissing:
                return head + " refused, no BaseStorage payload, host " + Host(result);
            case PeerVersionAction.Disconnect:
                return head + " disconnected after refusal";
            case PeerVersionAction.LeaveMismatch:
                return head + " is the host and versions differ, leaving, host " + Host(result) + ", client " + Client(result);
            default:
                return null;
        }
    }

    private static string Host(VersionCheckResult? result) => result?.HostVersion ?? "unknown";

    private static string Client(VersionCheckResult? result) => result?.ClientVersion ?? "none";
}
