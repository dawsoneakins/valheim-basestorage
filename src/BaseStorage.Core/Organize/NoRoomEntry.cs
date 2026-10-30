namespace BaseStorage.Core.Organize;

using BaseStorage.Core.Snapshots;

/// <summary>
/// A misplaced stack remainder that found no room in this plan (contract 4.4). The items stay in the source chest. There is
/// at most one entry per source stack.
/// </summary>
public sealed class NoRoomEntry
{
    /// <summary>Creates an entry. Only the planner creates entries.</summary>
    /// <param name="source">The source chest id.</param>
    /// <param name="sourceStackIndex">The index of the source stack in the source chest's input Contents.</param>
    /// <param name="item">The item's identity.</param>
    /// <param name="count">The number of items that could not be placed, at least 1.</param>
    /// <param name="categoryName">The category the stack needed.</param>
    internal NoRoomEntry(ContainerId source, int sourceStackIndex, ItemKey item, int count, string categoryName)
    {
        Source = source;
        SourceStackIndex = sourceStackIndex;
        PrefabName = item.PrefabName;
        Quality = item.Quality;
        MaxStack = item.MaxStack;
        Count = count;
        CategoryName = categoryName;
    }

    /// <summary>Gets the source chest id; the items stay there.</summary>
    public ContainerId Source { get; }

    /// <summary>Gets the index of the source stack in the source chest's input Contents.</summary>
    public int SourceStackIndex { get; }

    /// <summary>Gets the prefab name of the item.</summary>
    public string PrefabName { get; }

    /// <summary>Gets the quality of the item.</summary>
    public int Quality { get; }

    /// <summary>Gets the max stack of the item.</summary>
    public int MaxStack { get; }

    /// <summary>Gets the number of items that could not be placed, at least 1.</summary>
    public int Count { get; }

    /// <summary>Gets the category the stack needed.</summary>
    public string CategoryName { get; }
}
