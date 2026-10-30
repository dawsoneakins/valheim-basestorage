namespace BaseStorage.Core.Persistence;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>The set of inventory slots a player flagged as favourite.</summary>
public sealed class FavouriteSlots : IEquatable<FavouriteSlots>
{
    private static readonly FavouriteSlots EmptyValue = new FavouriteSlots(Array.Empty<InventorySlot>());

    /// <summary>Creates a set; duplicates collapse and input order does not matter.</summary>
    /// <param name="slots">The slots to include.</param>
    public FavouriteSlots(IEnumerable<InventorySlot> slots)
    {
        if (slots is null)
        {
            throw new ArgumentNullException(nameof(slots));
        }

        InventorySlot[] copy = slots.Distinct().ToArray();
        Array.Sort(copy);
        Slots = new ReadOnlyCollection<InventorySlot>(copy);
    }

    /// <summary>Gets the empty set.</summary>
    public static FavouriteSlots Empty => EmptyValue;

    /// <summary>Gets the distinct slots in ascending row-major order.</summary>
    public IReadOnlyList<InventorySlot> Slots { get; }

    /// <summary>Gets the number of slots.</summary>
    public int Count => Slots.Count;

    /// <summary>Determines whether the slot is in the set.</summary>
    /// <param name="slot">The slot to look for.</param>
    /// <returns>True when present.</returns>
    public bool Contains(InventorySlot slot) => Slots.Contains(slot);

    /// <summary>Determines whether both sets hold the same slots.</summary>
    /// <param name="other">The set to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(FavouriteSlots? other) => other is not null && Slots.SequenceEqual(other.Slots);

    /// <summary>Determines whether the object is an equal set.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as FavouriteSlots);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        int hash = 17;
        foreach (InventorySlot slot in Slots)
        {
            hash = unchecked((hash * 31) + slot.GetHashCode());
        }

        return hash;
    }

    /// <summary>Determines whether two sets are equal.</summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(FavouriteSlots? left, FavouriteSlots? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two sets differ.</summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(FavouriteSlots? left, FavouriteSlots? right) => !(left == right);
}
