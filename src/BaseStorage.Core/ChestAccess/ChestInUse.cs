namespace BaseStorage.Core.ChestAccess;

using System;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The result of an in-use query (contract S-E005-002 section 2.3): the chest and the label of the player who has it open.
/// The holder name is never null, empty or whitespace-only; it is already the fallback label when the name was unknown.
/// </summary>
public sealed class ChestInUse : IEquatable<ChestInUse>
{
    private readonly ContainerId chest;
    private readonly string holderName;

    /// <summary>Creates the in-use result.</summary>
    /// <param name="chest">The chest that is in use. It must not be the none id.</param>
    /// <param name="holderName">The holder label. It must not be null, empty or whitespace-only.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="chest"/> is the none id, or when <paramref name="holderName"/> is empty or whitespace-only.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="holderName"/> is null.</exception>
    public ChestInUse(ContainerId chest, string holderName)
    {
        if (chest.IsNone)
        {
            throw new ArgumentException("The chest must not be the none id.", nameof(chest));
        }

        if (holderName is null)
        {
            throw new ArgumentNullException(nameof(holderName));
        }

        if (string.IsNullOrWhiteSpace(holderName))
        {
            throw new ArgumentException("The holder name must not be empty or whitespace-only.", nameof(holderName));
        }

        this.chest = chest;
        this.holderName = holderName;
    }

    /// <summary>Gets the chest that is in use.</summary>
    public ContainerId Chest => chest;

    /// <summary>Gets the holder label, which is never null, empty or whitespace-only.</summary>
    public string HolderName => holderName;

    /// <summary>Compares the chest and the holder name (ordinal) with another result.</summary>
    /// <param name="other">The result to compare with, or null.</param>
    /// <returns>True when both are equal.</returns>
    public bool Equals(ChestInUse? other) =>
        other is not null && chest == other.chest && string.Equals(holderName, other.holderName, StringComparison.Ordinal);

    /// <summary>Compares this result with an object.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when the object is an equal result.</returns>
    public override bool Equals(object? obj) => Equals(obj as ChestInUse);

    /// <summary>Gets a hash code that agrees with <see cref="Equals(ChestInUse?)"/>.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => unchecked((chest.GetHashCode() * 31) + StringComparer.Ordinal.GetHashCode(holderName));

    /// <summary>Compares two results; two nulls are equal.</summary>
    /// <param name="left">The left result, or null.</param>
    /// <param name="right">The right result, or null.</param>
    /// <returns>True when the results are equal.</returns>
    public static bool operator ==(ChestInUse? left, ChestInUse? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Compares two results for inequality.</summary>
    /// <param name="left">The left result, or null.</param>
    /// <param name="right">The right result, or null.</param>
    /// <returns>True when the results are not equal.</returns>
    public static bool operator !=(ChestInUse? left, ChestInUse? right) => !(left == right);
}
