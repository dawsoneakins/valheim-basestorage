namespace BaseStorage.Plugin.Favourites;

/// <summary>The result of one favourite toggle (contract S-E004-014 section 3.1).</summary>
internal enum FavouriteToggleOutcome
{
    /// <summary>The slot is a favourite after the toggle.</summary>
    Added = 0,

    /// <summary>The slot is not a favourite after the toggle.</summary>
    Removed = 1,
}
