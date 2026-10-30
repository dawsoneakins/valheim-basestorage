namespace BaseStorage.Core.Locking;

using System;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;

/// <summary>
/// The result of an organize lock request (contract section 3.2). Instances come only from the <c>Granted</c> and <c>Refused</c>
/// factories and from <see cref="NotHeld"/>. The type has value equality on <see cref="Status"/> and <see cref="Record"/>.
/// </summary>
public sealed class OrganizeLockResult : IEquatable<OrganizeLockResult>
{
    private static readonly OrganizeLockResult NotHeldInstance = new OrganizeLockResult(OrganizeLockStatus.NotHeld, null);

    private OrganizeLockResult(OrganizeLockStatus status, OrganizeLockRecord? record)
    {
        Status = status;
        Record = record;
    }

    /// <summary>Gets the result of a renew request that found no live lock of the requester and none of anyone else.</summary>
    public static OrganizeLockResult NotHeld
    {
        get => NotHeldInstance;
    }

    /// <summary>Gets the outcome of the request.</summary>
    public OrganizeLockStatus Status { get; }

    /// <summary>Gets a value indicating whether the request was granted (<see cref="Status"/> is <see cref="OrganizeLockStatus.Granted"/>).</summary>
    public bool IsGranted => Status == OrganizeLockStatus.Granted;

    /// <summary>Gets the record written (granted), the blocking live record (refused), or null (not held).</summary>
    public OrganizeLockRecord? Record { get; }

    /// <summary>Gets the display name of the blocking holder for a refusal; null otherwise.</summary>
    public string? HolderName => Status == OrganizeLockStatus.Refused ? Record!.HolderName : null;

    /// <summary>Gets the string-table key of the refusal notice (<c>StringKeys.MessageBaseInProgress</c>) for a refusal; null otherwise.</summary>
    public string? MessageKey => Status == OrganizeLockStatus.Refused ? StringKeys.MessageBaseInProgress : null;

    /// <summary>Determines whether two results are equal.</summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(OrganizeLockResult? left, OrganizeLockResult? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two results differ.</summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(OrganizeLockResult? left, OrganizeLockResult? right) => !(left == right);

    /// <summary>Creates the result of a granted request.</summary>
    /// <param name="record">The record that was written to the lock station; never null.</param>
    /// <returns>A result with status <see cref="OrganizeLockStatus.Granted"/>.</returns>
    public static OrganizeLockResult Granted(OrganizeLockRecord record) =>
        new OrganizeLockResult(OrganizeLockStatus.Granted, record ?? throw new ArgumentNullException(nameof(record)));

    /// <summary>Creates the result of a refused request.</summary>
    /// <param name="liveRecord">The foreign live record that blocks the request; never null.</param>
    /// <returns>A result with status <see cref="OrganizeLockStatus.Refused"/>.</returns>
    public static OrganizeLockResult Refused(OrganizeLockRecord liveRecord) =>
        new OrganizeLockResult(OrganizeLockStatus.Refused, liveRecord ?? throw new ArgumentNullException(nameof(liveRecord)));

    /// <summary>Formats the refusal notice through the strings table; a granted or not-held result has no notice.</summary>
    /// <param name="table">The strings table that resolves <see cref="MessageKey"/>; never null.</param>
    /// <returns>The notice text for a refusal, or null when there is no notice.</returns>
    public string? FormatMessage(StringTable table)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        return Status == OrganizeLockStatus.Refused ? table.Format(StringKeys.MessageBaseInProgress, Record!.HolderName) : null;
    }

    /// <summary>Determines whether the status and the record are equal.</summary>
    /// <param name="other">The result to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(OrganizeLockResult? other) =>
        other is not null && Status == other.Status && Equals(Record, other.Record);

    /// <summary>Determines whether the object is an equal result.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as OrganizeLockResult);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => unchecked((((int)Status * 31) + (Record?.GetHashCode() ?? 0)) * 31);
}
