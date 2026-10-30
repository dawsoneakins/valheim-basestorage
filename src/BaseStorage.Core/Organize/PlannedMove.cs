namespace BaseStorage.Core.Organize;

using BaseStorage.Core.Snapshots;

/// <summary>
/// One planned transfer of part of a source stack into one destination chest (contract 4.3). The moves of one source stack
/// are adjacent and in the order each destination was first used. Source and Destination are never the same chest.
/// </summary>
public sealed class PlannedMove
{
    /// <summary>Creates a move. Only the planner creates moves.</summary>
    /// <param name="source">The source chest id.</param>
    /// <param name="sourceStackIndex">The index of the source stack in the source chest's input Contents.</param>
    /// <param name="destination">The destination chest id.</param>
    /// <param name="item">The moved item's identity.</param>
    /// <param name="count">The number of items moved, at least 1.</param>
    /// <param name="categoryName">The stack's category, which is the destination's chest category.</param>
    internal PlannedMove(ContainerId source, int sourceStackIndex, ContainerId destination, ItemKey item, int count, string categoryName)
    {
        Source = source;
        SourceStackIndex = sourceStackIndex;
        Destination = destination;
        PrefabName = item.PrefabName;
        Quality = item.Quality;
        MaxStack = item.MaxStack;
        Count = count;
        CategoryName = categoryName;
    }

    /// <summary>Gets the source chest id, a usable chest.</summary>
    public ContainerId Source { get; }

    /// <summary>Gets the index of the source stack in the source chest's input Contents.</summary>
    public int SourceStackIndex { get; }

    /// <summary>Gets the destination chest id: a category or claimed overflow chest of <see cref="CategoryName"/>.</summary>
    public ContainerId Destination { get; }

    /// <summary>Gets the prefab name of the moved item.</summary>
    public string PrefabName { get; }

    /// <summary>Gets the quality of the moved item.</summary>
    public int Quality { get; }

    /// <summary>Gets the max stack of the moved item.</summary>
    public int MaxStack { get; }

    /// <summary>Gets the number of items moved, at least 1.</summary>
    public int Count { get; }

    /// <summary>Gets the category of the stack, which is the destination's chest category.</summary>
    public string CategoryName { get; }
}
