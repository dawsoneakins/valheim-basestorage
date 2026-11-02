namespace BaseStorage.Core.Menu;

/// <summary>One item in a chest that matches a search.</summary>
public sealed class BaseSearchHit
{
    /// <summary>Creates a hit.</summary>
    /// <param name="name">The item's display name.</param>
    /// <param name="count">How many the chest holds.</param>
    public BaseSearchHit(string name, int count)
    {
        Name = name;
        Count = count;
    }

    /// <summary>Gets the item's display name.</summary>
    public string Name { get; }

    /// <summary>Gets how many the chest holds.</summary>
    public int Count { get; }
}
