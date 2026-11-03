namespace BaseStorage.Plugin.Deposit;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Categories;
using BaseStorage.Plugin.Favourites;
using BaseStorage.Plugin.Restock;

/// <summary>
/// Reads the local player's inventory into a <see cref="PlayerInventorySnapshot"/> (contract deposit-button-contracts.md, rule DB12)
/// or into the carried stacks of a Restock (contract restock-run-contracts.md, P2 and P3). The stacks are listed in the game's list
/// order, so stack index i is list index i. Any doubt about the data gives null with a reason, and the run does not start (principle 1);
/// the caller logs the reason. Main thread only; Unity null checks use <c>==</c> (W4).
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
    /// <param name="reason">Null on success; the cause of a null result otherwise.</param>
    /// <returns>The snapshot, or null on any doubt. Never throws for game data.</returns>
    internal static PlayerInventorySnapshot? Capture(Player? player, ContainerId id, out string? reason)
    {
        reason = null;
        if (player == null)
        {
            reason = "there is no local player";
            return null;
        }

        if (id.IsNone)
        {
            reason = "the inventory id is none";
            return null;
        }

        try
        {
            Inventory inventory = player.GetInventory();
            if (inventory == null)
            {
                reason = "the player has no inventory";
                return null;
            }

            int width = inventory.GetWidth();
            int height = inventory.GetHeight();
            if (width < 1 || width > MaxDimension || height < 1 || height > MaxDimension)
            {
                reason = "the inventory size " + width + "x" + height + " is out of range";
                return null;
            }

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null)
            {
                reason = "the inventory has no item list";
                return null;
            }

            var stacks = new List<PlayerInventoryStack>(items.Count);
            var seen = new HashSet<InventorySlot>();
            for (int i = 0; i < items.Count; i++)
            {
                if (!TryMap(items[i], width, height, out ItemStackSnapshot? snapshot, out InventorySlot slot, out string failure))
                {
                    reason = "stack " + i + " " + failure;
                    return null;
                }

                if (!seen.Add(slot))
                {
                    reason = "stack " + i + " shares slot " + slot.X + "," + slot.Y + " with an earlier stack";
                    return null;
                }

                stacks.Add(new PlayerInventoryStack(slot, snapshot!, Traits(player, items[i])));
            }

            return new PlayerInventorySnapshot(id, width, height, stacks);
        }
        catch (Exception ex)
        {
            reason = ex.GetType().Name + ": " + ex.Message;
            return null;
        }
    }

    /// <summary>Captures the stacks a Restock may top up: every occupied slot in the game's list order, with identity and marks.</summary>
    /// <param name="player">The player, or null.</param>
    /// <param name="reason">Null on success; the cause of a null result otherwise.</param>
    /// <param name="slots">The inventory slot of each returned stack, by list index; empty when the result is null.</param>
    /// <returns>The carried stacks, or null on any doubt. Never throws for game data.</returns>
    internal static List<CarriedStack>? CaptureCarried(Player? player, out string? reason, out Vector2i[] slots)
    {
        reason = null;
        slots = Array.Empty<Vector2i>();
        if (player == null)
        {
            reason = "there is no local player";
            return null;
        }

        try
        {
            Inventory inventory = player.GetInventory();
            if (inventory == null)
            {
                reason = "the player has no inventory";
                return null;
            }

            int width = inventory.GetWidth();
            int height = inventory.GetHeight();
            if (width < 1 || width > MaxDimension || height < 1 || height > MaxDimension)
            {
                reason = "the inventory size " + width + "x" + height + " is out of range";
                return null;
            }

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null)
            {
                reason = "the inventory has no item list";
                return null;
            }

            var stacks = new List<CarriedStack>(items.Count);
            var found = new Vector2i[items.Count];
            var seen = new HashSet<InventorySlot>();
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                if (!TryMap(item, width, height, out ItemStackSnapshot? snapshot, out InventorySlot slot, out string failure))
                {
                    reason = "stack " + i + " " + failure;
                    return null;
                }

                if (!seen.Add(slot))
                {
                    reason = "stack " + i + " shares slot " + slot.X + "," + slot.Y + " with an earlier stack";
                    return null;
                }

                CarriedMarks marks = CarriedMarks.None;
                if (item.m_gridPos.y == 0)
                {
                    marks |= CarriedMarks.Hotbar;
                }

                if (FavouriteSession.IsFavourite(item))
                {
                    marks |= CarriedMarks.FavouriteType;
                }

                var identified = new ItemStackSnapshot(snapshot!.PrefabName, snapshot.Count, snapshot.Quality, snapshot.MaxStack, GameItemIdentity.Of(item));
                stacks.Add(new CarriedStack(identified, marks));
                found[i] = item.m_gridPos;
            }

            slots = found;
            return stacks;
        }
        catch (Exception ex)
        {
            reason = ex.GetType().Name + ": " + ex.Message;
            return null;
        }
    }

    private static bool TryMap(ItemDrop.ItemData item, int width, int height, out ItemStackSnapshot? snapshot, out InventorySlot slot, out string failure)
    {
        snapshot = null;
        slot = default;
        failure = string.Empty;
        if (item == null || item.m_dropPrefab == null || item.m_shared == null)
        {
            failure = "is missing its prefab or shared data";
            return false;
        }

        ItemDrop.ItemData.SharedData shared = item.m_shared;
        string prefabName = item.m_dropPrefab.name;
        if (item.m_stack < 1 || item.m_quality < 1 || shared.m_maxStackSize < 1 || string.IsNullOrWhiteSpace(prefabName))
        {
            failure = "has a count, quality, max stack or prefab name that is not valid";
            return false;
        }

        if (!ProfileFavourites.TryGetSlot(item.m_gridPos.x, item.m_gridPos.y, out slot) || slot.X > MaxSlot || slot.Y > MaxSlot
            || slot.X >= width || slot.Y >= height)
        {
            failure = "is at slot " + item.m_gridPos.x + "," + item.m_gridPos.y + ", outside the inventory";
            return false;
        }

        snapshot = new ItemStackSnapshot(prefabName, item.m_stack, item.m_quality, shared.m_maxStackSize);
        return true;
    }

    private static InventoryItemTraits Traits(Player player, ItemDrop.ItemData item)
    {
        ItemDrop.ItemData.SharedData shared = item.m_shared;
        return DepositTraitRules.Of(
            item.m_equipped || player.IsItemEquiped(item),
            shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable,
            shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable,
            ItemKindAdapter.From(shared) == ItemKind.Food,
            item.m_dropPrefab.name);
    }
}
