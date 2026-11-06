namespace BaseStorage.Plugin.Favourites;

using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Organize;
using BaseStorage.Plugin.Ui;
using UnityEngine;

/// <summary>Shift + right-click on an inventory stack: sends it straight to the base's chests, with no mode, button or preview. Main thread only.</summary>
internal static class QuickPutAway
{
    /// <summary>Handles one right-button press on a grid slot.</summary>
    /// <param name="grid">The grid that received the press.</param>
    /// <param name="element">The slot element's input handler.</param>
    /// <returns>True to run the vanilla handler, false when the click was handled here.</returns>
    internal static bool RunVanilla(InventoryGrid grid, UIInputHandler element)
    {
        if (!ZInput.GetKey(KeyCode.LeftShift, true) && !ZInput.GetKey(KeyCode.RightShift, true))
        {
            return true;
        }

        InventoryGui? gui = InventoryGui.instance;
        Player? player = Player.m_localPlayer;
        if (gui == null || player == null || element == null || !ReferenceEquals(grid, gui.m_playerGrid) || gui.m_dragItem != null)
        {
            return true;
        }

        Vector2i pos = grid.GetButtonPos(element.gameObject);
        Inventory? inventory = grid.GetInventory();
        ItemDrop.ItemData? item = inventory == null ? null : inventory.GetItemAt(pos.x, pos.y);
        if (item == null)
        {
            return true;
        }

        if (item.m_equipped)
        {
            Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageQuickPutAwayEquipped);
            return false;
        }

        Vector3 at = player.transform.position;
        BaseStart start = BaseStart.FromPosition(new WorldPosition(at.x, at.y, at.z));
        OrganizeRunner.StartQuickDepositAt(start, new FavouriteSlots(new[] { new InventorySlot(pos.x, pos.y) }));
        return false;
    }
}
