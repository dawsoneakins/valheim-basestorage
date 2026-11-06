namespace BaseStorage.Plugin.Favourites;

using System.Collections.Generic;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Organize;
using BaseStorage.Plugin.Ui;

/// <summary>
/// The items picked for Put away selected. It holds the item objects, not their slots, so sorting the inventory keeps the selection. It is
/// never saved: an item that left the inventory simply stops counting. Main thread only.
/// </summary>
internal static class DepositSelection
{
    /// <summary>The red part of the selected marker's colour.</summary>
    internal const float MarkerR = 0.25f;

    /// <summary>The green part of the selected marker's colour.</summary>
    internal const float MarkerG = 0.6f;

    /// <summary>The blue part of the selected marker's colour.</summary>
    internal const float MarkerB = 1f;

    private static readonly HashSet<ItemDrop.ItemData> Items = new HashSet<ItemDrop.ItemData>();

    /// <summary>Gets a value indicating whether clicking an inventory item selects it.</summary>
    internal static bool SelectMode { get; private set; }

    /// <summary>Handles the menu button: the first press turns select mode on, the second puts the selection away.</summary>
    /// <param name="player">The local player.</param>
    /// <param name="start">The base's start.</param>
    internal static void OnButton(Player player, BaseStart start)
    {
        if (!SelectMode)
        {
            SelectMode = true;
            Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageSelectMode);
            return;
        }

        SelectMode = false;
        PutAway(player, start);
    }

    /// <summary>Leaves select mode and forgets the selection.</summary>
    internal static void Cancel()
    {
        SelectMode = false;
        Items.Clear();
    }

    /// <summary>Counts the selected stacks that would really move: equipped items never do.</summary>
    /// <param name="player">The local player.</param>
    /// <returns>The stacks.</returns>
    internal static int MovableCount(Player player)
    {
        int count = 0;
        foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
        {
            if (Items.Contains(item) && !item.m_equipped)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Tells whether an item is selected.</summary>
    /// <param name="item">The item.</param>
    /// <returns>True when selected.</returns>
    internal static bool Contains(ItemDrop.ItemData item) => Items.Contains(item);

    /// <summary>Selects or deselects an item and says which.</summary>
    /// <param name="item">The item.</param>
    internal static void ToggleWithNotice(ItemDrop.ItemData item)
    {
        bool selected = Items.Add(item);
        if (!selected)
        {
            Items.Remove(item);
        }

        string name = Localization.instance != null ? Localization.instance.Localize(item.m_shared.m_name) : item.m_shared.m_name;
        if (selected)
        {
            Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageItemSelected, name);
        }
        else
        {
            Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageItemDeselected, name);
        }
    }

    private static void PutAway(Player player, BaseStart start)
    {
        var slots = new List<InventorySlot>();
        foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
        {
            if (Items.Contains(item))
            {
                slots.Add(new InventorySlot(item.m_gridPos.x, item.m_gridPos.y));
            }
        }

        if (slots.Count == 0)
        {
            Items.Clear();
            Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageNothingSelected);
            return;
        }

        OrganizeStartOutcome outcome = OrganizeRunner.StartDepositSelectedAt(start, new FavouriteSlots(slots));
        if (outcome != OrganizeStartOutcome.AlreadyActive && outcome != OrganizeStartOutcome.NoPlayer)
        {
            Items.Clear();
        }
    }
}
