namespace BaseStorage.Core.ChestSort;

/// <summary>One stack after sorting: which existing stack stays, and how many it holds.</summary>
public sealed class ChestSortSlot
{
    /// <summary>Creates a slot. Only the planner creates slots.</summary>
    /// <param name="source">The index of the existing stack.</param>
    /// <param name="count">How many it holds after sorting.</param>
    internal ChestSortSlot(int source, int count)
    {
        Source = source;
        Count = count;
    }

    /// <summary>Gets the index of the existing stack, in the order given to the planner.</summary>
    public int Source { get; }

    /// <summary>Gets how many items the stack holds after sorting.</summary>
    public int Count { get; }
}
