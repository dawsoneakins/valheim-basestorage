namespace BaseStorage.Core.Snapshots;

using System;
using BaseStorage.Core.Snapshots.Identities;

/// <summary>One stack of items as it sits in an inventory.</summary>
public sealed class ItemStackSnapshot : IEquatable<ItemStackSnapshot>
{
    /// <summary>Creates a stack snapshot whose identity is <see cref="ItemStackIdentity.Empty"/>.</summary>
    /// <param name="prefabName">The item's prefab name; not null, empty or whitespace.</param>
    /// <param name="count">Items in the stack; at least 1. May exceed the max stack.</param>
    /// <param name="quality">Quality level; at least 1.</param>
    /// <param name="maxStack">The item type's max stack size; at least 1.</param>
    public ItemStackSnapshot(string prefabName, int count, int quality, int maxStack)
        : this(prefabName, count, quality, maxStack, ItemStackIdentity.Empty)
    {
    }

    /// <summary>Creates a stack snapshot with an identity that decides whether stacks may merge.</summary>
    /// <param name="prefabName">The item's prefab name; not null, empty or whitespace.</param>
    /// <param name="count">Items in the stack; at least 1. May exceed the max stack.</param>
    /// <param name="quality">Quality level; at least 1.</param>
    /// <param name="maxStack">The item type's max stack size; at least 1.</param>
    /// <param name="identity">The item data of this stack; not null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="prefabName"/> or <paramref name="identity"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="prefabName"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/>, <paramref name="quality"/> or <paramref name="maxStack"/> is below 1.</exception>
    public ItemStackSnapshot(string prefabName, int count, int quality, int maxStack, ItemStackIdentity identity)
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

        if (identity is null)
        {
            throw new ArgumentNullException(nameof(identity));
        }

        PrefabName = prefabName;
        Count = count;
        Quality = quality;
        MaxStack = maxStack;
        Identity = identity;
    }

    /// <summary>Gets the item's prefab name.</summary>
    public string PrefabName { get; }

    /// <summary>Gets the number of items in this stack.</summary>
    public int Count { get; }

    /// <summary>Gets the item quality level; 1 is base quality.</summary>
    public int Quality { get; }

    /// <summary>Gets the item type's max stack size.</summary>
    public int MaxStack { get; }

    /// <summary>Gets the item data that decides whether this stack may merge with another; never null.</summary>
    public ItemStackIdentity Identity { get; }

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

    /// <summary>Determines whether all fields and the identity are equal; the prefab name is compared ordinally.</summary>
    /// <param name="other">The snapshot to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(ItemStackSnapshot? other) =>
        other is not null
        && string.Equals(PrefabName, other.PrefabName, StringComparison.Ordinal)
        && Count == other.Count
        && Quality == other.Quality
        && MaxStack == other.MaxStack
        && Identity.Equals(other.Identity);

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
            h = (h * 31) + MaxStack;
            return (h * 31) + Identity.GetHashCode();
        }
    }
}
