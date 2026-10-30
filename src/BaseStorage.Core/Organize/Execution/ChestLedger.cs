namespace BaseStorage.Core.Organize.Execution;

using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The expected state of one chest during an execution (contract X5, 4.4): the per-item totals the executor expects to see live, and
/// the remaining count of each planned input stack. It starts from the plan's input snapshot and changes only on a completed move.
/// </summary>
internal sealed class ChestLedger
{
    private readonly Dictionary<ItemKey, long> totals = new Dictionary<ItemKey, long>();
    private readonly Dictionary<int, int> remaining = new Dictionary<int, int>();
    private readonly IReadOnlyList<ItemStackSnapshot> input;

    /// <summary>Creates a ledger from the plan's input contents of the chest.</summary>
    /// <param name="input">The input stacks, in input order; empty when the plan has no snapshot of the chest.</param>
    internal ChestLedger(IReadOnlyList<ItemStackSnapshot> input)
    {
        this.input = input;
        for (var i = 0; i < input.Count; i++)
        {
            var stack = input[i];
            Give(KeyOf(stack), stack.Count);
            remaining[i] = stack.Count;
        }
    }

    /// <summary>Returns the item identity of a stack.</summary>
    /// <param name="stack">The stack.</param>
    /// <returns>The key: prefab name, quality and max stack.</returns>
    internal static ItemKey KeyOf(ItemStackSnapshot stack) => new ItemKey(stack.PrefabName, stack.Quality, stack.MaxStack);

    /// <summary>Returns the item identity of a planned move.</summary>
    /// <param name="move">The move.</param>
    /// <returns>The key: prefab name, quality and max stack.</returns>
    internal static ItemKey KeyOf(PlannedMove move) => new ItemKey(move.PrefabName, move.Quality, move.MaxStack);

    /// <summary>Determines whether live contents carry exactly the expected per-item totals. Stack order and slot count are not compared.</summary>
    /// <param name="live">The live contents.</param>
    /// <returns>True when every item total matches and no other item is present.</returns>
    internal bool Matches(IReadOnlyList<ItemStackSnapshot> live)
    {
        var seen = new Dictionary<ItemKey, long>();
        for (var i = 0; i < live.Count; i++)
        {
            var key = KeyOf(live[i]);
            seen.TryGetValue(key, out var current);
            seen[key] = current + live[i].Count;
        }

        if (seen.Count != totals.Count)
        {
            return false;
        }

        foreach (var pair in totals)
        {
            if (!seen.TryGetValue(pair.Key, out var liveCount) || liveCount != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Returns the stack the executor expects to find live for a planned input stack: its identity with the remaining count.</summary>
    /// <param name="inputIndex">The index of the stack in the plan's input contents.</param>
    /// <returns>The expected stack, or null when the index is unknown or nothing of the stack remains.</returns>
    internal ItemStackSnapshot? Expected(int inputIndex)
    {
        if (inputIndex < 0 || inputIndex >= input.Count || !remaining.TryGetValue(inputIndex, out var left) || left < 1)
        {
            return null;
        }

        var stack = input[inputIndex];
        return new ItemStackSnapshot(stack.PrefabName, left, stack.Quality, stack.MaxStack);
    }

    /// <summary>Records that items left this chest from a planned input stack.</summary>
    /// <param name="inputIndex">The index of the stack in the plan's input contents.</param>
    /// <param name="key">The item identity.</param>
    /// <param name="count">The number of items removed.</param>
    internal void Take(int inputIndex, ItemKey key, int count)
    {
        remaining[inputIndex] = remaining.TryGetValue(inputIndex, out var left) ? left - count : -count;
        totals.TryGetValue(key, out var current);
        var next = current - count;
        if (next <= 0)
        {
            totals.Remove(key);
        }
        else
        {
            totals[key] = next;
        }
    }

    /// <summary>Records that items arrived in this chest.</summary>
    /// <param name="key">The item identity.</param>
    /// <param name="count">The number of items added.</param>
    internal void Give(ItemKey key, int count)
    {
        totals.TryGetValue(key, out var current);
        totals[key] = current + count;
    }
}
