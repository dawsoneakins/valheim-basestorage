namespace BaseStorage.Plugin.Favourites;

using BaseStorage.Core.Persistence;
using BaseStorage.Plugin.Ui;
using UnityEngine;

/// <summary>
/// The Alt+click handler of the player inventory grid (contract S-E004-014 section 4). It decides T1 to T6 in contract order and
/// stops at the first condition that fails, so a click the mod does not handle runs vanilla with no side effect.
/// </summary>
internal static class FavouriteClicks
{
    /// <summary>
    /// Handles one left-button press on a grid slot. Returns true to run vanilla; returns false only after a favourite was toggled,
    /// so the handled click never picks up, splits, moves, uses or drops the item.
    /// </summary>
    /// <param name="grid">The grid that received the press.</param>
    /// <param name="clickHandler">The slot element's input handler.</param>
    /// <returns>True to run the vanilla handler, false to skip it.</returns>
    internal static bool RunVanilla(InventoryGrid grid, UIInputHandler clickHandler)
    {
        if (!ZInput.GetKey(KeyCode.LeftAlt, true) && !ZInput.GetKey(KeyCode.RightAlt, true))
        {
            return true;
        }

        InventoryGui? gui = InventoryGui.instance;
        if (gui == null || !ReferenceEquals(grid, gui.m_playerGrid))
        {
            return true;
        }

        if (gui.m_dragItem != null)
        {
            return true;
        }

        ProfileFavourites? favourites = FavouriteSession.ForLocalPlayer();
        if (favourites == null || clickHandler == null)
        {
            return true;
        }

        Vector2i pos = grid.GetButtonPos(clickHandler.gameObject);
        if (!ProfileFavourites.TryGetSlot(pos.x, pos.y, out InventorySlot slot))
        {
            return true;
        }

        Inventory? inventory = grid.GetInventory();
        ItemDrop.ItemData? item = inventory == null ? null : inventory.GetItemAt(pos.x, pos.y);
        if (item == null)
        {
            return true;
        }

        FavouriteToggleOutcome outcome = favourites.Toggle(slot);
        string name = Localization.instance != null
            ? Localization.instance.Localize(item.m_shared.m_name)
            : item.m_shared.m_name;
        Notices.Show(NoticePlacement.TopLeft, ProfileFavourites.MessageKeyFor(outcome), name);
        return false;
    }
}
