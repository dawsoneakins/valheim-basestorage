namespace BaseStorage.Core.Networking;

using System;

/// <summary>
/// The host-side version decision (contract version-handshake-contracts.md 4.3): accept a peer, or reject it with both versions,
/// honouring the host's RequireSameVersion setting. Every member is a pure function of its arguments (rule V6).
/// </summary>
public static class VersionCheck
{
    /// <summary>
    /// Compares two versions. An ordinal-equal client version is accepted. A different client version is a mismatch when
    /// <paramref name="requireSameVersion"/> is true and accepted otherwise. A null client version (no usable version) is
    /// missing when <paramref name="requireSameVersion"/> is true and accepted otherwise.
    /// </summary>
    /// <param name="hostVersion">The host version; well formed.</param>
    /// <param name="clientVersion">The client version; well formed, or null when the client has no usable version.</param>
    /// <param name="requireSameVersion">The host's RequireSameVersion setting.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="ArgumentNullException">The host version is null.</exception>
    /// <exception cref="ArgumentException">A version is not well formed.</exception>
    public static VersionCheckResult Compare(string hostVersion, string? clientVersion, bool requireSameVersion)
    {
        ModVersion.Require(hostVersion, nameof(hostVersion));
        if (clientVersion is null)
        {
            return requireSameVersion ? VersionCheckResult.Missing(hostVersion) : VersionCheckResult.Accepted(hostVersion, null);
        }

        ModVersion.Require(clientVersion, nameof(clientVersion));
        if (!requireSameVersion || string.Equals(hostVersion, clientVersion, StringComparison.Ordinal))
        {
            return VersionCheckResult.Accepted(hostVersion, clientVersion);
        }

        return VersionCheckResult.Mismatch(hostVersion, clientVersion);
    }

    /// <summary>Decodes a client payload text with <see cref="VersionHandshake.DecodePayload(string)"/> and compares. Never throws for the payload.</summary>
    /// <param name="hostVersion">The host version; well formed.</param>
    /// <param name="clientPayload">The received payload text, possibly null or malformed.</param>
    /// <param name="requireSameVersion">The host's RequireSameVersion setting.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="ArgumentNullException">The host version is null.</exception>
    /// <exception cref="ArgumentException">The host version is not well formed.</exception>
    public static VersionCheckResult CheckPayload(string hostVersion, string? clientPayload, bool requireSameVersion) =>
        Compare(hostVersion, VersionHandshake.DecodePayload(clientPayload), requireSameVersion);

    /// <summary>Decodes client payload bytes with <see cref="VersionHandshake.DecodePayload(byte[])"/> and compares. Never throws for the payload.</summary>
    /// <param name="hostVersion">The host version; well formed.</param>
    /// <param name="clientPayload">The received payload bytes, possibly null or malformed.</param>
    /// <param name="requireSameVersion">The host's RequireSameVersion setting.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="ArgumentNullException">The host version is null.</exception>
    /// <exception cref="ArgumentException">The host version is not well formed.</exception>
    public static VersionCheckResult CheckPayload(string hostVersion, byte[]? clientPayload, bool requireSameVersion) =>
        Compare(hostVersion, VersionHandshake.DecodePayload(clientPayload), requireSameVersion);
}
