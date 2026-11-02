namespace BaseStorage.Plugin.Favourites;

using System.Collections.Generic;
using BaseStorage.Core.Persistence;

/// <summary>
/// Holds the one <see cref="ProfileFavourites"/> adapter of the local player's custom data (contract 3.2 lifetime, ADR-GWIN-053).
/// A new adapter is created when the local player or its custom data dictionary changes, for example after a relog, so a cache never
/// outlives its player. This is the only Plugin file that names the custom data field. Touched only from the main thread, by the patches.
/// </summary>
internal static class FavouriteSession
{
    private static Player? player;
    private static ProfileFavourites? favourites;
    private static FavouriteItemStore? items;

    /// <summary>Gets the adapter for the local player, or null when there is no local player or it has no custom data.</summary>
    /// <returns>The adapter, or null. Null also clears the cached player and adapter.</returns>
    internal static ProfileFavourites? ForLocalPlayer()
    {
        Player? local = Player.m_localPlayer;
        if (local == null || local.m_customData == null)
        {
            player = null;
            favourites = null;
            items = null;
            return null;
        }

        if (favourites == null
            || !ReferenceEquals(player, local)
            || !ReferenceEquals(favourites.CustomData, local.m_customData))
        {
            player = local;
            favourites = new ProfileFavourites(local.m_customData);
            items = new FavouriteItemStore(local.m_customData);
        }

        return favourites;
    }

    /// <summary>Gets the favourite item types of the local player, taking over any slots an older version marked.</summary>
    /// <returns>The store, or null when there is no local player.</returns>
    internal static FavouriteItemStore? ItemsForLocalPlayer()
    {
        ProfileFavourites? legacy = ForLocalPlayer();
        Inventory? inventory = legacy == null ? null : player!.GetInventory();
        if (legacy != null && inventory != null && legacy.Current.Count > 0 && inventory.NrOfItems() > 0)
        {
            items!.AdoptSlots(legacy.Current, inventory);
        }

        return items;
    }

    /// <summary>Tells whether an item is a favourite of the local player.</summary>
    /// <param name="item">The item.</param>
    /// <returns>True when it is.</returns>
    internal static bool IsFavourite(ItemDrop.ItemData item)
    {
        FavouriteItemStore? store = ItemsForLocalPlayer();
        return store != null && item != null && store.Current.Contains(item.m_shared.m_name);
    }

    /// <summary>Gets the slots of the local player's inventory that hold a favourite.</summary>
    /// <returns>The slots.</returns>
    internal static FavouriteSlots OccupiedSlots()
    {
        Inventory? inventory = Player.m_localPlayer == null ? null : Player.m_localPlayer.GetInventory();
        if (inventory == null)
        {
            return FavouriteSlots.Empty;
        }

        var slots = new List<InventorySlot>();
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (IsFavourite(item) && ProfileFavourites.TryGetSlot(item.m_gridPos.x, item.m_gridPos.y, out InventorySlot slot))
            {
                slots.Add(slot);
            }
        }

        return new FavouriteSlots(slots);
    }
}
