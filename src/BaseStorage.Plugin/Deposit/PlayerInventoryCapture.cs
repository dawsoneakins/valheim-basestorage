namespace BaseStorage.Plugin.Deposit;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Categories;
using BaseStorage.Plugin.Favourites;

/// <summary>
/// Reads the local player's inventory into a <see cref="PlayerInventorySnapshot"/> (contract deposit-button-contracts.md, rule DB12).
/// The stacks are listed in the game's list order, so stack index i is list index i. Any doubt about the data gives null and the
/// Deposit does not start (principle 1). Main thread only; Unity null checks use <c>==</c> (W4).
/// </summary>
internal static class PlayerInventoryCapture
{
    private const int MaxDimension = 256;
    private const int MaxSlot = 255;

    /// <summary>Gets the inventory id of the local player.</summary>
    /// <returns>The id; the default (none) when there is no local player or no session.</returns>
    internal static ContainerId InventoryId()
    {
        long session = ZDOMan.GetSessionID();
        return Player.m_localPlayer != null && session != 0L ? new ContainerId(session, 0u) : default;
    }

    /// <summary>Captures the inventory of a player.</summary>
    /// <param name="player">The player, or null.</param>
    /// <param name="id">The inventory id; must not be none.</param>
    /// <returns>The snapshot, or null on any doubt. Never throws for game data.</returns>
    internal static PlayerInventorySnapshot? Capture(Player? player, ContainerId id)
    {
        if (player == null || id.IsNone)
        {
            return null;
        }

        try
        {
            Inventory inventory = player.GetInventory();
            if (inventory == null)
            {
                return null;
            }

            int width = inventory.GetWidth();
            int height = inventory.GetHeight();
            if (width < 1 || width > MaxDimension || height < 1 || height > MaxDimension)
            {
                return null;
            }

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null)
            {
                return null;
            }

            var stacks = new List<PlayerInventoryStack>(items.Count);
            var seen = new HashSet<InventorySlot>();
            for (int i = 0; i < items.Count; i++)
            {
                PlayerInventoryStack? stack = MapItem(player, items[i], width, height);
                if (stack == null || !seen.Add(stack.Slot))
                {
                    return null;
                }

                stacks.Add(stack);
            }

            return new PlayerInventorySnapshot(id, width, height, stacks);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static PlayerInventoryStack? MapItem(Player player, ItemDrop.ItemData item, int width, int height)
    {
        if (item == null || item.m_dropPrefab == null || item.m_shared == null)
        {
            return null;
        }

        ItemDrop.ItemData.SharedData shared = item.m_shared;
        string prefabName = item.m_dropPrefab.name;
        if (item.m_stack < 1 || item.m_quality < 1 || shared.m_maxStackSize < 1 || string.IsNullOrWhiteSpace(prefabName))
        {
            return null;
        }

        if (!ProfileFavourites.TryGetSlot(item.m_gridPos.x, item.m_gridPos.y, out InventorySlot slot) || slot.X > MaxSlot || slot.Y > MaxSlot
            || slot.X >= width || slot.Y >= height)
        {
            return null;
        }

        InventoryItemTraits traits = DepositTraitRules.Of(
            item.m_equipped || player.IsItemEquiped(item),
            shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable,
            shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable,
            ItemKindAdapter.From(shared) == ItemKind.Food,
            prefabName);
        return new PlayerInventoryStack(slot, new ItemStackSnapshot(prefabName, item.m_stack, item.m_quality, shared.m_maxStackSize), traits);
    }
}
