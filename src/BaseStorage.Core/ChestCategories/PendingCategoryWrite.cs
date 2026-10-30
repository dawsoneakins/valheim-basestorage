namespace BaseStorage.Core.ChestCategories;

using System;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// One chest category write that waits for its reservation (contract S-E004-003 section 2.4): the chest, the value to write and
/// the window on the <c>IClock.Elapsed</c> timeline after which the write is given up. Immutable; equality compares every member.
/// </summary>
public sealed class PendingCategoryWrite : IEquatable<PendingCategoryWrite>
{
    private readonly ContainerId chest;
    private readonly CategoryAssignment? desired;
    private readonly TimeSpan startedAt;
    private readonly TimeSpan deadline;

    /// <summary>Creates a pending write.</summary>
    /// <param name="chest">The chest; must not be the none id.</param>
    /// <param name="desired">The value to write; null for Auto.</param>
    /// <param name="startedAt">The clock reading when the write was asked for.</param>
    /// <param name="deadline">The clock reading at which the write is given up; not before <paramref name="startedAt"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="chest"/> is the none id.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="deadline"/> is before <paramref name="startedAt"/>.</exception>
    public PendingCategoryWrite(ContainerId chest, CategoryAssignment? desired, TimeSpan startedAt, TimeSpan deadline)
    {
        if (chest.IsNone)
        {
            throw new ArgumentException("The chest must not be the none id.", nameof(chest));
        }

        if (deadline < startedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(deadline), "The deadline must not be before the start.");
        }

        this.chest = chest;
        this.desired = desired;
        this.startedAt = startedAt;
        this.deadline = deadline;
    }

    /// <summary>Gets the chest.</summary>
    public ContainerId Chest => chest;

    /// <summary>Gets the value to write; null for Auto.</summary>
    public CategoryAssignment? Desired => desired;

    /// <summary>Gets the clock reading when the write was asked for.</summary>
    public TimeSpan StartedAt => startedAt;

    /// <summary>Gets the clock reading at which the write is given up.</summary>
    public TimeSpan Deadline => deadline;

    /// <summary>Tells whether the write is past its deadline. A clock that went back before the start also counts as expired.</summary>
    /// <param name="now">The current clock reading.</param>
    /// <returns>True when <paramref name="now"/> is at or past the deadline, or before the start.</returns>
    public bool IsExpired(TimeSpan now) => now >= deadline || now < startedAt;

    /// <summary>Compares every member with another pending write.</summary>
    /// <param name="other">The pending write to compare with, or null.</param>
    /// <returns>True when all members are equal.</returns>
    public bool Equals(PendingCategoryWrite? other) =>
        other is not null
        && chest == other.chest
        && desired == other.desired
        && startedAt == other.startedAt
        && deadline == other.deadline;

    /// <summary>Compares this pending write with an object.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when the object is an equal pending write.</returns>
    public override bool Equals(object? obj) => Equals(obj as PendingCategoryWrite);

    /// <summary>Gets a hash code that agrees with <see cref="Equals(PendingCategoryWrite?)"/>.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = chest.GetHashCode();
            hash = (hash * 31) + (desired is null ? 0 : desired.GetHashCode());
            hash = (hash * 31) + startedAt.GetHashCode();
            hash = (hash * 31) + deadline.GetHashCode();
            return hash;
        }
    }

    /// <summary>Compares two pending writes; two nulls are equal.</summary>
    /// <param name="left">The left value, or null.</param>
    /// <param name="right">The right value, or null.</param>
    /// <returns>True when the values are equal.</returns>
    public static bool operator ==(PendingCategoryWrite? left, PendingCategoryWrite? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Compares two pending writes for inequality.</summary>
    /// <param name="left">The left value, or null.</param>
    /// <param name="right">The right value, or null.</param>
    /// <returns>True when the values are not equal.</returns>
    public static bool operator !=(PendingCategoryWrite? left, PendingCategoryWrite? right) => !(left == right);
}
