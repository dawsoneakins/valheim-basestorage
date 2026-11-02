namespace BaseStorage.Core.Menu;

/// <summary>One item in a chest that matches a search.</summary>
public sealed class BaseSearchHit
{
    /// <summary>Creates a hit.</summary>
    /// <param name="name">The item's display name.</param>
    /// <param name="count">How many the chest holds.</param>
    /// <param name="prefab">The item's prefab name, which finds its icon.</param>
    /// <param name="rank">How well the item matched; lower is better.</param>
    public BaseSearchHit(string name, int count, string prefab = "", int rank = 0)
    {
        Rank = rank;
        Name = name;
        Count = count;
        Prefab = prefab;
    }

    /// <summary>Gets the item's display name.</summary>
    public string Name { get; }

    /// <summary>Gets how many the chest holds.</summary>
    public int Count { get; }

    /// <summary>Gets the item's prefab name.</summary>
    public string Prefab { get; }

    /// <summary>Gets how well the item matched; lower is better.</summary>
    internal int Rank { get; }
}
