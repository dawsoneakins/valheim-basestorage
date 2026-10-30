namespace BaseStorage.Core.ChestCategories;

using System;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The end of one chest category write (contract S-E004-003 section 2.3): the chest, the value that was asked for, how it
/// ended and, for <see cref="CategoryWriteOutcome.InUse"/>, the label of the player who has the chest open. Immutable;
/// equality compares every member with the holder label compared ordinally.
/// </summary>
public sealed class CategoryWriteResult : IEquatable<CategoryWriteResult>
{
    private readonly ContainerId chest;
    private readonly CategoryAssignment? desired;
    private readonly CategoryWriteOutcome outcome;
    private readonly string? holderLabel;

    /// <summary>Creates a result.</summary>
    /// <param name="chest">The chest; must not be the none id.</param>
    /// <param name="desired">The value that was asked for; null for Auto.</param>
    /// <param name="outcome">How the write ended.</param>
    /// <param name="holderLabel">The label of the player who has the chest open, or null when not known or not relevant.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="chest"/> is the none id.</exception>
    public CategoryWriteResult(ContainerId chest, CategoryAssignment? desired, CategoryWriteOutcome outcome, string? holderLabel)
    {
        if (chest.IsNone)
        {
            throw new ArgumentException("The chest must not be the none id.", nameof(chest));
        }

        this.chest = chest;
        this.desired = desired;
        this.outcome = outcome;
        this.holderLabel = holderLabel;
    }

    /// <summary>Gets the chest.</summary>
    public ContainerId Chest => chest;

    /// <summary>Gets the value that was asked for; null for Auto.</summary>
    public CategoryAssignment? Desired => desired;

    /// <summary>Gets how the write ended.</summary>
    public CategoryWriteOutcome Outcome => outcome;

    /// <summary>Gets the label of the player who has the chest open, or null.</summary>
    public string? HolderLabel => holderLabel;

    /// <summary>Gets a value that is true when the value was written.</summary>
    public bool Succeeded => outcome == CategoryWriteOutcome.Applied;

    /// <summary>Compares every member with another result.</summary>
    /// <param name="other">The result to compare with, or null.</param>
    /// <returns>True when all members are equal.</returns>
    public bool Equals(CategoryWriteResult? other) =>
        other is not null
        && chest == other.chest
        && desired == other.desired
        && outcome == other.outcome
        && string.Equals(holderLabel, other.holderLabel, StringComparison.Ordinal);

    /// <summary>Compares this result with an object.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when the object is an equal result.</returns>
    public override bool Equals(object? obj) => Equals(obj as CategoryWriteResult);

    /// <summary>Gets a hash code that agrees with <see cref="Equals(CategoryWriteResult?)"/>.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = chest.GetHashCode();
            hash = (hash * 31) + (desired is null ? 0 : desired.GetHashCode());
            hash = (hash * 31) + (int)outcome;
            hash = (hash * 31) + (holderLabel is null ? 0 : StringComparer.Ordinal.GetHashCode(holderLabel));
            return hash;
        }
    }

    /// <summary>Compares two results; two nulls are equal.</summary>
    /// <param name="left">The left result, or null.</param>
    /// <param name="right">The right result, or null.</param>
    /// <returns>True when the results are equal.</returns>
    public static bool operator ==(CategoryWriteResult? left, CategoryWriteResult? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Compares two results for inequality.</summary>
    /// <param name="left">The left result, or null.</param>
    /// <param name="right">The right result, or null.</param>
    /// <returns>True when the results are not equal.</returns>
    public static bool operator !=(CategoryWriteResult? left, CategoryWriteResult? right) => !(left == right);
}
