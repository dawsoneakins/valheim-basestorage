namespace BaseStorage.Plugin.CharacterMenu;

using System.Collections.Generic;
using BaseStorage.Core.CharacterMenu;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Ui;

/// <summary>Moves gear between the inventory grid and the worn row. Main thread only.</summary>
internal static class WornItems
{
    /// <summary>The state of a position check that didn't change the inventory's height.</summary>
    internal const int NotWidened = -1;

    /// <summary>Finds the worn slot an item belongs in.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The slot, or null when the item isn't worn gear.</returns>
    internal static WornSlot? SlotOf(ItemDrop.ItemData? item)
    {
        if (item?.m_shared == null)
        {
            return null;
        }

        switch (item.m_shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.Helmet:
                return WornSlot.Helmet;
            case ItemDrop.ItemData.ItemType.Chest:
                return WornSlot.Chest;
            case ItemDrop.ItemData.ItemType.Legs:
                return WornSlot.Legs;
            case ItemDrop.ItemData.ItemType.Shoulder:
                return WornSlot.Cape;
            case ItemDrop.ItemData.ItemType.Utility:
                return WornSlot.Utility;
            case ItemDrop.ItemData.ItemType.Trinket:
                return WornSlot.Trinket;
            default:
                return null;
        }
    }

    /// <summary>Gets the item in a worn slot.</summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The slot.</param>
    /// <returns>The item, or null.</returns>
    internal static ItemDrop.ItemData? InSlot(Player player, WornSlot slot)
    {
        InventorySlot position = WornSlots.PositionOf(slot);
        return player.GetInventory().GetItemAt(position.X, position.Y);
    }

    /// <summary>After the game equips an item, moves it into its slot.</summary>
    /// <param name="who">Who equipped it.</param>
    /// <param name="item">The item. A failed equip leaves it unequipped, which ends this here.</param>
    /// <returns>The item, for the patch's wrapper.</returns>
    internal static ItemDrop.ItemData OnEquipped(Humanoid who, ItemDrop.ItemData item)
    {
        WornSlot? slot = SlotOf(item);
        if (!(who is Player player) || !slot.HasValue || !item.m_equipped)
        {
            return item;
        }

        Inventory inventory = player.GetInventory();
        InventorySlot position = WornSlots.PositionOf(slot.Value);
        if (!inventory.ContainsItem(item) || (item.m_gridPos.x == position.X && item.m_gridPos.y == position.Y))
        {
            return item;
        }

        ItemDrop.ItemData? occupant = inventory.GetItemAt(position.X, position.Y);
        WornSwap swap = WornSlots.PutOn(new InventorySlot(item.m_gridPos.x, item.m_gridPos.y), slot.Value, occupant != null);
        item.m_gridPos = new Vector2i(swap.Item.X, swap.Item.Y);
        if (occupant != null && swap.Occupant.HasValue)
        {
            occupant.m_gridPos = new Vector2i(swap.Occupant.Value.X, swap.Occupant.Value.Y);
        }

        inventory.Changed(false, false);
        return item;
    }

    /// <summary>After the game unequips an item, returns it to the grid when there's room.</summary>
    /// <param name="who">Who unequipped it.</param>
    /// <param name="item">The item, which the game allows to be null.</param>
    /// <returns>The item, for the patch's wrapper.</returns>
    internal static ItemDrop.ItemData? OnUnequipped(Humanoid who, ItemDrop.ItemData? item)
    {
        if (who is Player player && item != null && !item.m_equipped && player.GetInventory().ContainsItem(item))
        {
            ReturnToGrid(player.GetInventory(), item);
        }

        return item;
    }

    /// <summary>Takes an item off from its slot, and says so if it has nowhere to go.</summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The item.</param>
    internal static void TakeOff(Player player, ItemDrop.ItemData item)
    {
        if (item.m_equipped)
        {
            player.UnequipItem(item);
        }
        else
        {
            ReturnToGrid(player.GetInventory(), item);
        }

        if (WornSlots.IsWornPosition(item.m_gridPos.x, item.m_gridPos.y))
        {
            Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageWornNoRoom);
        }
    }

    /// <summary>Returns anything left unequipped in the worn row to the grid, now there may be room.</summary>
    /// <param name="player">The player.</param>
    internal static void ReturnStranded(Player player)
    {
        Inventory inventory = player.GetInventory();
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        for (int i = 0; i < items.Count; i++)
        {
            if (!items[i].m_equipped)
            {
                ReturnToGrid(inventory, items[i]);
            }
        }
    }

    /// <summary>Counts an inventory's free grid spaces, leaving out items outside its grid.</summary>
    /// <param name="inventory">The inventory.</param>
    /// <returns>The free spaces.</returns>
    internal static int EmptySlots(Inventory inventory)
    {
        int outside = 0;
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        for (int i = 0; i < items.Count; i++)
        {
            Vector2i position = items[i].m_gridPos;
            if (position.x < 0 || position.y < 0 || position.x >= inventory.GetWidth() || position.y >= inventory.GetHeight())
            {
                outside++;
            }
        }

        return WornSlots.EmptySlots(inventory.GetWidth(), inventory.GetHeight(), items.Count, outside);
    }

    /// <summary>Counts the worn row as inside a player's inventory, so the game's position check doesn't drop worn gear.</summary>
    /// <param name="who">Who the check runs for.</param>
    /// <returns>The height to restore afterwards, or <see cref="NotWidened"/> when nothing changed.</returns>
    internal static int WidenForDropCheck(Humanoid who)
    {
        if (!(who is Player player))
        {
            return NotWidened;
        }

        Inventory inventory = player.GetInventory();
        int height = inventory.m_height;
        inventory.m_height = WornSlots.HeightWithWornRow(height);
        return height;
    }

    /// <summary>Puts back the height <see cref="WidenForDropCheck"/> changed.</summary>
    /// <param name="who">Who the check ran for.</param>
    /// <param name="height">The height to restore, or <see cref="NotWidened"/>.</param>
    /// <returns>The height, for the patch's wrapper.</returns>
    internal static int RestoreAfterDropCheck(Humanoid who, int height)
    {
        if (height != NotWidened && who is Player player)
        {
            player.GetInventory().m_height = height;
        }

        return height;
    }

    /// <summary>Gives a grave the extra row a worn item needs in order to be seen.</summary>
    /// <param name="grave">The grave's inventory.</param>
    internal static void WidenGrave(Inventory grave)
    {
        if (grave.m_height < WornSlots.GraveHeight)
        {
            grave.m_height = WornSlots.GraveHeight;
        }
    }

    /// <summary>Gives a grave the extra row when its container wakes.</summary>
    /// <param name="container">The container, which may not be a grave.</param>
    internal static void WidenIfGrave(Container container)
    {
        if (container != null && container.GetComponent<TombStone>() != null && container.GetInventory() != null)
        {
            WidenGrave(container.GetInventory());
        }
    }

    private static void ReturnToGrid(Inventory inventory, ItemDrop.ItemData item)
    {
        if (!WornSlots.IsWornPosition(item.m_gridPos.x, item.m_gridPos.y))
        {
            return;
        }

        Vector2i free = inventory.FindEmptySlot(inventory.TopFirst(item));
        if (free.x >= 0)
        {
            item.m_gridPos = free;
            inventory.Changed(false, false);
        }
    }
}
