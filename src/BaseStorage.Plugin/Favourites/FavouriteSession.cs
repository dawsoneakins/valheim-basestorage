namespace BaseStorage.Plugin.Favourites;

/// <summary>
/// Holds the one <see cref="ProfileFavourites"/> adapter of the local player's custom data (contract 3.2 lifetime, ADR-GWIN-053).
/// A new adapter is created when the local player or its custom data dictionary changes, for example after a relog, so a cache never
/// outlives its player. This is the only Plugin file that names the custom data field. Touched only from the main thread, by the patches.
/// </summary>
internal static class FavouriteSession
{
    private static Player? player;
    private static ProfileFavourites? favourites;

    /// <summary>Gets the adapter for the local player, or null when there is no local player or it has no custom data.</summary>
    /// <returns>The adapter, or null. Null also clears the cached player and adapter.</returns>
    internal static ProfileFavourites? ForLocalPlayer()
    {
        Player? local = Player.m_localPlayer;
        if (local == null || local.m_customData == null)
        {
            player = null;
            favourites = null;
            return null;
        }

        if (favourites == null
            || !ReferenceEquals(player, local)
            || !ReferenceEquals(favourites.CustomData, local.m_customData))
        {
            player = local;
            favourites = new ProfileFavourites(local.m_customData);
        }

        return favourites;
    }
}
