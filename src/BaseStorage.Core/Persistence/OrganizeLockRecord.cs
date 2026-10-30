namespace BaseStorage.Core.Persistence;

using System;

/// <summary>The lock stored on the lowest-id station of a base while an Organize or Deposit runs.</summary>
public sealed class OrganizeLockRecord : IEquatable<OrganizeLockRecord>
{
    /// <summary>Creates a lock record.</summary>
    /// <param name="holderId">The holder's game player id; any non-zero value.</param>
    /// <param name="holderName">The holder's display name; not blank, no control characters.</param>
    /// <param name="expiresAt">The reading on the clock timeline at which the lock lapses.</param>
    public OrganizeLockRecord(long holderId, string holderName, TimeSpan expiresAt)
    {
        if (holderId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(holderId), "The holder id must not be zero.");
        }

        NameRule.Require(holderName, nameof(holderName));
        HolderId = holderId;
        HolderName = holderName;
        ExpiresAt = expiresAt;
    }

    /// <summary>Gets the holder's game player id.</summary>
    public long HolderId { get; }

    /// <summary>Gets the holder's display name.</summary>
    public string HolderName { get; }

    /// <summary>Gets the clock reading at which the lock lapses.</summary>
    public TimeSpan ExpiresAt { get; }

    /// <summary>Determines whether id, name (ordinal) and expiry are all equal.</summary>
    /// <param name="other">The record to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(OrganizeLockRecord? other) =>
        other is not null
        && HolderId == other.HolderId
        && string.Equals(HolderName, other.HolderName, StringComparison.Ordinal)
        && ExpiresAt == other.ExpiresAt;

    /// <summary>Determines whether the object is an equal record.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as OrganizeLockRecord);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() =>
        unchecked((((HolderId.GetHashCode() * 31) + StringComparer.Ordinal.GetHashCode(HolderName)) * 31) + ExpiresAt.GetHashCode());

    /// <summary>Determines whether two records are equal.</summary>
    /// <param name="left">The first record.</param>
    /// <param name="right">The second record.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(OrganizeLockRecord? left, OrganizeLockRecord? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two records differ.</summary>
    /// <param name="left">The first record.</param>
    /// <param name="right">The second record.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(OrganizeLockRecord? left, OrganizeLockRecord? right) => !(left == right);
}
