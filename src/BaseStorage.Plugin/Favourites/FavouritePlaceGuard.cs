namespace BaseStorage.Plugin.Favourites;

using BaseStorage.Core.Persistence;

/// <summary>
/// Keeps favourites out of vanilla Place stacks. Vanilla skips equipped items there, so while it runs from the local player's inventory a
/// favourite counts as equipped, and at no other time. Main thread only.
/// </summary>
internal static class FavouritePlaceGuard
{
    /// <summary>Gets a value indicating whether a Place stacks from the local player's inventory is running.</summary>
    internal static bool Active { get; private set; }

    /// <summary>Starts guarding when the stacks come from the local player's inventory.</summary>
    /// <param name="fromInventory">The inventory the stacks come from.</param>
    internal static void Begin(Inventory fromInventory)
    {
        Player? local = Player.m_localPlayer;
        Active = local != null && ReferenceEquals(fromInventory, local.GetInventory());
    }

    /// <summary>Stops guarding.</summary>
    internal static void End() => Active = false;

    /// <summary>Tells whether an item sits in a favourite slot.</summary>
    /// <param name="item">The item.</param>
    /// <returns>True when Place stacks must leave it.</returns>
    internal static bool Protects(ItemDrop.ItemData item)
    {
        ProfileFavourites? favourites = FavouriteSession.ForLocalPlayer();
        return favourites != null
            && item != null
            && ProfileFavourites.TryGetSlot(item.m_gridPos.x, item.m_gridPos.y, out InventorySlot slot)
            && favourites.IsFavourite(slot);
    }
}
