namespace BaseStorage.Plugin.Restock;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Re-reads a listed chest from its live <see cref="Container"/> with identity-filled stacks (contract restock-run-contracts.md, P4).
/// It uses the same prefab, count, quality and max-stack rules as the snapshot source, plus <see cref="GameItemIdentity"/>, and keeps
/// the listed Id, Position, Width, Height and Flags. The snapshot source itself is not changed, so Organize and Deposit keep the empty identity.
/// </summary>
internal static class RestockChestCapture
{
    /// <summary>Tells whether a game item can be mapped to a stack: the same checks as the snapshot source.</summary>
    /// <param name="item">The item, or null.</param>
    /// <returns>True when the item has a prefab, a shared record and positive count, quality and max stack.</returns>
    internal static bool IsMappable(ItemDrop.ItemData? item)
    {
        return item != null
            && item.m_dropPrefab != null
            && item.m_shared != null
            && item.m_stack >= 1
            && item.m_quality >= 1
            && item.m_shared.m_maxStackSize >= 1
            && !string.IsNullOrWhiteSpace(item.m_dropPrefab.name);
    }

    /// <summary>Re-reads one chest.</summary>
    /// <param name="listed">The chest as it was listed; its Id, Position, Width, Height and Flags are kept.</param>
    /// <param name="container">The live container, or null.</param>
    /// <returns>The chest with identity-filled stacks; null when it is gone, unreadable or holds an item that cannot be mapped.</returns>
    internal static ContainerSnapshot? Read(ContainerSnapshot listed, Container? container)
    {
        if (container == null || container.m_nview == null || !container.m_nview.IsValid() || container.m_inventory == null)
        {
            return null;
        }

        List<ItemDrop.ItemData> items = container.GetInventory().GetAllItems();
        if (items == null)
        {
            return null;
        }

        try
        {
            var stacks = new List<ItemStackSnapshot>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                if (!IsMappable(item))
                {
                    return null;
                }

                stacks.Add(new ItemStackSnapshot(item.m_dropPrefab.name, item.m_stack, item.m_quality, item.m_shared.m_maxStackSize, GameItemIdentity.Of(item)));
            }

            return new ContainerSnapshot(listed.Id, listed.Position, listed.Width, listed.Height, stacks, listed.Flags);
        }
        catch (ArgumentException)
        {
            // A value the Core rejects (for example a non-finite durability): the chest is left out of the restock.
            return null;
        }
    }
}
