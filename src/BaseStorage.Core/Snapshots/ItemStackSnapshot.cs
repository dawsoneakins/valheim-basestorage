namespace BaseStorage.Core.Snapshots;

using System;

/// <summary>One stack of items as it sits in an inventory.</summary>
public sealed class ItemStackSnapshot : IEquatable<ItemStackSnapshot>
{
    /// <summary>Creates a stack snapshot.</summary>
    /// <param name="prefabName">The item's prefab name; not null, empty or whitespace.</param>
    /// <param name="count">Items in the stack; at least 1. May exceed the max stack.</param>
    /// <param name="quality">Quality level; at least 1.</param>
    /// <param name="maxStack">The item type's max stack size; at least 1.</param>
    public ItemStackSnapshot(string prefabName, int count, int quality, int maxStack)
    {
        if (prefabName is null)
        {
            throw new ArgumentNullException(nameof(prefabName));
        }

        if (string.IsNullOrWhiteSpace(prefabName))
        {
            throw new ArgumentException("Must not be empty or whitespace.", nameof(prefabName));
        }

        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Must be at least 1.");
        }

        if (quality < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quality), "Must be at least 1.");
        }

        if (maxStack < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStack), "Must be at least 1.");
        }

        PrefabName = prefabName;
        Count = count;
        Quality = quality;
        MaxStack = maxStack;
    }

    /// <summary>Gets the item's prefab name.</summary>
    public string PrefabName { get; }

    /// <summary>Gets the number of items in this stack.</summary>
    public int Count { get; }

    /// <summary>Gets the item quality level; 1 is base quality.</summary>
    public int Quality { get; }

    /// <summary>Gets the item type's max stack size.</summary>
    public int MaxStack { get; }

    /// <summary>Determines whether two snapshots are equal.</summary>
    /// <param name="left">The first snapshot.</param>
    /// <param name="right">The second snapshot.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(ItemStackSnapshot? left, ItemStackSnapshot? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two snapshots differ.</summary>
    /// <param name="left">The first snapshot.</param>
    /// <param name="right">The second snapshot.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(ItemStackSnapshot? left, ItemStackSnapshot? right) => !(left == right);

    /// <summary>Determines whether all fields are equal; the prefab name is compared ordinally.</summary>
    /// <param name="other">The snapshot to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(ItemStackSnapshot? other) =>
        other is not null
        && string.Equals(PrefabName, other.PrefabName, StringComparison.Ordinal)
        && Count == other.Count
        && Quality == other.Quality
        && MaxStack == other.MaxStack;

    /// <summary>Determines whether the object is an equal snapshot.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as ItemStackSnapshot);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = StringComparer.Ordinal.GetHashCode(PrefabName);
            h = (h * 31) + Count;
            h = (h * 31) + Quality;
            return (h * 31) + MaxStack;
        }
    }
}
