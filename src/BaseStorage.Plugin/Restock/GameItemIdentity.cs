namespace BaseStorage.Plugin.Restock;

using System.Collections.Generic;
using BaseStorage.Core.Snapshots.Identities;

/// <summary>
/// The one mapper from a game item to its <see cref="ItemStackIdentity"/> (contract restock-run-contracts.md, P2). Every restock read
/// (carried capture, chest re-read, the layer's live reads) calls it, so both sides of an equality check read the same six fields.
/// The custom data is only read; it is copied by the Core constructor and never handed out for writing.
/// </summary>
internal static class GameItemIdentity
{
    private static readonly Dictionary<string, string> EmptyData = new Dictionary<string, string>(0);

    /// <summary>Maps the identity fields of an item.</summary>
    /// <param name="item">The item; not null.</param>
    /// <returns>The identity.</returns>
    /// <exception cref="System.ArgumentException">A field is a value the Core rejects (for example a non-finite durability); the caller decides what that means.</exception>
    internal static ItemStackIdentity Of(ItemDrop.ItemData item)
    {
        return new ItemStackIdentity(item.m_durability, item.m_crafterID, item.m_crafterName ?? string.Empty, item.m_variant, item.m_worldLevel, item.m_customData ?? EmptyData);
    }
}
