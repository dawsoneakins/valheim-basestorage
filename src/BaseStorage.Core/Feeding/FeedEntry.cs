namespace BaseStorage.Core.Feeding;

/// <summary>One item a station is kept fed with.</summary>
public sealed class FeedEntry
{
    /// <summary>Creates an entry.</summary>
    /// <param name="prefabName">The item's prefab name.</param>
    /// <param name="keep">How many of the item must stay in the base; negative values are read as 0.</param>
    public FeedEntry(string prefabName, int keep)
    {
        PrefabName = prefabName;
        Keep = keep < 0 ? 0 : keep;
    }

    /// <summary>Gets the item's prefab name.</summary>
    public string PrefabName { get; }

    /// <summary>Gets how many of the item must stay in the base.</summary>
    public int Keep { get; }
}
