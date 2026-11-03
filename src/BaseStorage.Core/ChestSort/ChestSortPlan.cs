namespace BaseStorage.Core.ChestSort;

using System.Collections.Generic;

/// <summary>The sorted layout of one chest. Every item is accounted for: nothing is created and nothing is lost.</summary>
public sealed class ChestSortPlan
{
    /// <summary>Creates a plan. Only the planner creates plans.</summary>
    /// <param name="slots">The stacks that stay, first slot first.</param>
    /// <param name="removed">The indexes of the stacks merged away.</param>
    internal ChestSortPlan(IReadOnlyList<ChestSortSlot> slots, IReadOnlyList<int> removed)
    {
        Slots = slots;
        Removed = removed;
    }

    /// <summary>Gets the stacks that stay, first slot first.</summary>
    public IReadOnlyList<ChestSortSlot> Slots { get; }

    /// <summary>Gets the indexes of the stacks merged away.</summary>
    public IReadOnlyList<int> Removed { get; }
}
