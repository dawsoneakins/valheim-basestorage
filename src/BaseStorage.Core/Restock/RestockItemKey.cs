namespace BaseStorage.Core.Restock;

using System;
using BaseStorage.Core.Snapshots;

/// <summary>Internal item key (ADR-GWIN-171): prefab, quality and max stack, without identity (Q2).</summary>
internal sealed class RestockItemKey : IEquatable<RestockItemKey>
{
    private readonly string prefabName;
    private readonly int quality;
    private readonly int maxStack;

    internal RestockItemKey(ItemStackSnapshot stack)
    {
        prefabName = stack.PrefabName;
        quality = stack.Quality;
        maxStack = stack.MaxStack;
    }

    public bool Equals(RestockItemKey? other) =>
        other is not null
        && string.Equals(prefabName, other.prefabName, StringComparison.Ordinal)
        && quality == other.quality
        && maxStack == other.maxStack;

    public override bool Equals(object? obj) => Equals(obj as RestockItemKey);

    public override int GetHashCode()
    {
        unchecked
        {
            int h = StringComparer.Ordinal.GetHashCode(prefabName);
            h = (h * 31) + quality;
            return (h * 31) + maxStack;
        }
    }
}
