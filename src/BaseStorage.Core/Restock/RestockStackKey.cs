namespace BaseStorage.Core.Restock;

using System;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Snapshots.Identities;

/// <summary>Internal stack key (ADR-GWIN-171): prefab, quality, max stack and identity (Q1).</summary>
internal sealed class RestockStackKey : IEquatable<RestockStackKey>
{
    private readonly string prefabName;
    private readonly int quality;
    private readonly int maxStack;
    private readonly ItemStackIdentity identity;

    internal RestockStackKey(ItemStackSnapshot stack)
    {
        prefabName = stack.PrefabName;
        quality = stack.Quality;
        maxStack = stack.MaxStack;
        identity = stack.Identity;
    }

    public bool Equals(RestockStackKey? other) =>
        other is not null
        && string.Equals(prefabName, other.prefabName, StringComparison.Ordinal)
        && quality == other.quality
        && maxStack == other.maxStack
        && identity.Equals(other.identity);

    public override bool Equals(object? obj) => Equals(obj as RestockStackKey);

    public override int GetHashCode()
    {
        unchecked
        {
            int h = StringComparer.Ordinal.GetHashCode(prefabName);
            h = (h * 31) + quality;
            h = (h * 31) + maxStack;
            return (h * 31) + identity.GetHashCode();
        }
    }
}
