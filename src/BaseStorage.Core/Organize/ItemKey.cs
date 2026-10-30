namespace BaseStorage.Core.Organize;

using System;

/// <summary>The item identity of a stack (P5): ordinal prefab name, quality and max stack. Only stacks of the same key merge.</summary>
internal readonly struct ItemKey : IEquatable<ItemKey>
{
    /// <summary>Creates the key of a stack's identity fields.</summary>
    /// <param name="prefabName">The prefab name, compared ordinally.</param>
    /// <param name="quality">The quality.</param>
    /// <param name="maxStack">The max stack.</param>
    internal ItemKey(string prefabName, int quality, int maxStack)
    {
        PrefabName = prefabName;
        Quality = quality;
        MaxStack = maxStack;
    }

    /// <summary>Gets the prefab name.</summary>
    internal string PrefabName { get; }

    /// <summary>Gets the quality.</summary>
    internal int Quality { get; }

    /// <summary>Gets the max stack, which is the limit of every stack of this item.</summary>
    internal int MaxStack { get; }

    /// <summary>Determines whether two keys name the same item.</summary>
    /// <param name="other">The other key.</param>
    /// <returns>True when the prefab names are ordinal-equal and the quality and max stack are equal.</returns>
    public bool Equals(ItemKey other) =>
        string.Equals(PrefabName, other.PrefabName, StringComparison.Ordinal)
        && Quality == other.Quality
        && MaxStack == other.MaxStack;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ItemKey other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = StringComparer.Ordinal.GetHashCode(PrefabName);
            hash = (hash * 31) + Quality;
            return (hash * 31) + MaxStack;
        }
    }
}
