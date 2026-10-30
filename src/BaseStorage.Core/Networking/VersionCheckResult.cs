namespace BaseStorage.Core.Networking;

using System;
using BaseStorage.Core.Ui;

/// <summary>
/// The result of a version check (contract version-handshake-contracts.md 4.2). Instances come only from the <c>Accepted</c>,
/// <c>Mismatch</c> and <c>Missing</c> factories and from <see cref="VersionCheck"/>. The type has value equality on
/// <see cref="Status"/>, <see cref="HostVersion"/> and <see cref="ClientVersion"/>.
/// </summary>
public sealed class VersionCheckResult : IEquatable<VersionCheckResult>
{
    private VersionCheckResult(VersionCheckStatus status, string hostVersion, string? clientVersion)
    {
        Status = status;
        HostVersion = hostVersion;
        ClientVersion = clientVersion;
    }

    /// <summary>Gets the outcome of the check.</summary>
    public VersionCheckStatus Status { get; }

    /// <summary>Gets a value indicating whether the peer may join (<see cref="Status"/> is <see cref="VersionCheckStatus.Accepted"/>).</summary>
    public bool IsAccepted => Status == VersionCheckStatus.Accepted;

    /// <summary>Gets the host version. Always well formed.</summary>
    public string HostVersion { get; }

    /// <summary>Gets the client version, or null when the client had no usable version. Well formed when present.</summary>
    public string? ClientVersion { get; }

    /// <summary>Gets the string-table key of the rejection notice (<c>StringKeys.NetworkVersionMismatch</c>) for a mismatch; null otherwise.</summary>
    public string? MessageKey => Status == VersionCheckStatus.RejectedMismatch ? StringKeys.NetworkVersionMismatch : null;

    /// <summary>Determines whether two results are equal.</summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(VersionCheckResult? left, VersionCheckResult? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two results differ.</summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(VersionCheckResult? left, VersionCheckResult? right) => !(left == right);

    /// <summary>Creates an accepted result: the same version, or a different or absent version while RequireSameVersion is off.</summary>
    /// <param name="hostVersion">The host version; well formed.</param>
    /// <param name="clientVersion">The client version; well formed, or null when the client had no usable version.</param>
    /// <returns>A result with status <see cref="VersionCheckStatus.Accepted"/>.</returns>
    /// <exception cref="ArgumentNullException">The host version is null.</exception>
    /// <exception cref="ArgumentException">A version is not well formed.</exception>
    public static VersionCheckResult Accepted(string hostVersion, string? clientVersion)
    {
        ModVersion.Require(hostVersion, nameof(hostVersion));
        if (clientVersion is not null)
        {
            ModVersion.Require(clientVersion, nameof(clientVersion));
        }

        return new VersionCheckResult(VersionCheckStatus.Accepted, hostVersion, clientVersion);
    }

    /// <summary>Creates a rejection for a client whose version differs from the host version.</summary>
    /// <param name="hostVersion">The host version; well formed.</param>
    /// <param name="clientVersion">The client version; well formed and different from the host version.</param>
    /// <returns>A result with status <see cref="VersionCheckStatus.RejectedMismatch"/>.</returns>
    /// <exception cref="ArgumentNullException">A version is null.</exception>
    /// <exception cref="ArgumentException">A version is not well formed, or the two versions are equal.</exception>
    public static VersionCheckResult Mismatch(string hostVersion, string clientVersion)
    {
        ModVersion.Require(hostVersion, nameof(hostVersion));
        ModVersion.Require(clientVersion, nameof(clientVersion));
        if (string.Equals(hostVersion, clientVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException("A mismatch needs two different versions.", nameof(clientVersion));
        }

        return new VersionCheckResult(VersionCheckStatus.RejectedMismatch, hostVersion, clientVersion);
    }

    /// <summary>Creates a rejection for a client without a usable version: no mod, no payload, or a malformed payload.</summary>
    /// <param name="hostVersion">The host version; well formed.</param>
    /// <returns>A result with status <see cref="VersionCheckStatus.RejectedMissing"/> and a null <see cref="ClientVersion"/>.</returns>
    /// <exception cref="ArgumentNullException">The host version is null.</exception>
    /// <exception cref="ArgumentException">The host version is not well formed.</exception>
    public static VersionCheckResult Missing(string hostVersion) =>
        new VersionCheckResult(VersionCheckStatus.RejectedMissing, ModVersion.Require(hostVersion, nameof(hostVersion)), null);

    /// <summary>Formats the mismatch notice through the strings table; an accepted or missing result has no notice.</summary>
    /// <param name="table">The strings table that resolves <see cref="MessageKey"/>; never null.</param>
    /// <returns>The notice text for a mismatch, or null when there is no notice.</returns>
    /// <exception cref="ArgumentNullException">The table is null.</exception>
    public string? FormatMessage(StringTable table)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        return Status == VersionCheckStatus.RejectedMismatch
            ? table.Format(StringKeys.NetworkVersionMismatch, HostVersion, ClientVersion)
            : null;
    }

    /// <summary>Determines whether the status and both versions are equal (versions compared ordinally).</summary>
    /// <param name="other">The result to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(VersionCheckResult? other) =>
        other is not null
        && Status == other.Status
        && string.Equals(HostVersion, other.HostVersion, StringComparison.Ordinal)
        && string.Equals(ClientVersion, other.ClientVersion, StringComparison.Ordinal);

    /// <summary>Determines whether the object is an equal result.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as VersionCheckResult);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Status;
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(HostVersion);
            hash = (hash * 31) + (ClientVersion is null ? 0 : StringComparer.Ordinal.GetHashCode(ClientVersion));
            return hash;
        }
    }
}
