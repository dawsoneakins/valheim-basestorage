namespace BaseStorage.Core.Organize;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>The in-chest compaction and ordering of a layout (contract 4.6).</summary>
internal static class LayoutCompactor
{
    private static readonly Comparison<Group> GroupOrder = CompareGroups;

    /// <summary>
    /// Compacts the final working stacks of one chest: each item's total is split into full stacks then one remainder, and the
    /// items are ordered by prefab name (ordinal), quality and max stack. Totals are summed as long values.
    /// </summary>
    /// <param name="stacks">The final working stacks of the chest; stacks with a zero count are ignored.</param>
    /// <returns>The compacted stacks, never more than the number of non-empty working stacks.</returns>
    internal static ItemStackSnapshot[] Compact(List<WorkingStack> stacks)
    {
        var groups = new List<Group>();
        for (var i = 0; i < stacks.Count; i++)
        {
            var stack = stacks[i];
            if (stack.Count == 0)
            {
                continue;
            }

            var group = Find(groups, stack.Key);
            if (group is null)
            {
                group = new Group(stack.Key);
                groups.Add(group);
            }

            group.Total += stack.Count;
        }

        groups.Sort(GroupOrder);

        var result = new List<ItemStackSnapshot>(stacks.Count);
        for (var g = 0; g < groups.Count; g++)
        {
            var group = groups[g];
            var key = group.Key;
            var max = key.MaxStack;
            var full = group.Total / max;
            for (var f = 0L; f < full; f++)
            {
                result.Add(new ItemStackSnapshot(key.PrefabName, max, key.Quality, max));
            }

            var remainder = (int)(group.Total % max);
            if (remainder > 0)
            {
                result.Add(new ItemStackSnapshot(key.PrefabName, remainder, key.Quality, max));
            }
        }

        return result.ToArray();
    }

    private static Group? Find(List<Group> groups, ItemKey key)
    {
        for (var i = 0; i < groups.Count; i++)
        {
            if (groups[i].Key.Equals(key))
            {
                return groups[i];
            }
        }

        return null;
    }

    private static int CompareGroups(Group left, Group right)
    {
        var byName = string.CompareOrdinal(left.Key.PrefabName, right.Key.PrefabName);
        if (byName != 0)
        {
            return byName;
        }

        var byQuality = left.Key.Quality.CompareTo(right.Key.Quality);
        return byQuality != 0 ? byQuality : left.Key.MaxStack.CompareTo(right.Key.MaxStack);
    }

    /// <summary>One item's running total inside a compaction.</summary>
    private sealed class Group
    {
        internal Group(ItemKey key)
        {
            Key = key;
        }

        internal ItemKey Key { get; }

        internal long Total { get; set; }
    }
}
