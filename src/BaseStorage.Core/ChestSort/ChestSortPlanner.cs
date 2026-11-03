namespace BaseStorage.Core.ChestSort;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Sorts one chest. Pure. Identical stacks merge, then stacks are ordered by catalog category, item name and quality. Stacks that differ in
/// quality, durability, crafter or custom data stay separate, so a sort never changes an item. Nothing moves between chests.
/// </summary>
public static class ChestSortPlanner
{
    /// <summary>Plans the sort.</summary>
    /// <param name="entries">The chest's stacks.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is null.</exception>
    /// <exception cref="ArgumentException">An entry is null.</exception>
    public static ChestSortPlan Plan(IReadOnlyList<ChestSortEntry> entries)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        var groups = new List<Group>();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i] ?? throw new ArgumentException("The entries contain a null element.", nameof(entries));
            Find(groups, entry).Add(i, entry.Stack.Count);
        }

        groups.Sort(Compare);
        var slots = new List<ChestSortSlot>(entries.Count);
        var removed = new List<int>();
        for (var g = 0; g < groups.Count; g++)
        {
            groups[g].Lay(slots, removed);
        }

        return new ChestSortPlan(slots, removed);
    }

    private static Group Find(List<Group> groups, ChestSortEntry entry)
    {
        for (var i = 0; i < groups.Count; i++)
        {
            if (groups[i].Holds(entry.Stack))
            {
                return groups[i];
            }
        }

        var added = new Group(entry, groups.Count);
        groups.Add(added);
        return added;
    }

    private static int Compare(Group a, Group b)
    {
        var order = a.Entry.CategoryRank.CompareTo(b.Entry.CategoryRank);
        if (order == 0)
        {
            order = string.Compare(a.Entry.DisplayName, b.Entry.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        if (order == 0)
        {
            order = string.CompareOrdinal(a.Entry.Stack.PrefabName, b.Entry.Stack.PrefabName);
        }

        if (order == 0)
        {
            order = b.Entry.Stack.Quality.CompareTo(a.Entry.Stack.Quality);
        }

        return order != 0 ? order : a.Order.CompareTo(b.Order);
    }

    private sealed class Group
    {
        private readonly List<int> sources = new List<int>();
        private long total;
        private int size;

        internal Group(ChestSortEntry entry, int order)
        {
            Entry = entry;
            Order = order;
            size = Math.Max(1, entry.Stack.MaxStack);
        }

        internal ChestSortEntry Entry { get; }

        internal int Order { get; }

        internal bool Holds(ItemStackSnapshot stack)
        {
            var mine = Entry.Stack;
            return string.Equals(mine.PrefabName, stack.PrefabName, StringComparison.Ordinal)
                && mine.Quality == stack.Quality
                && mine.MaxStack == stack.MaxStack
                && mine.Identity.Equals(stack.Identity);
        }

        internal void Add(int index, int count)
        {
            sources.Add(index);
            total += count;

            // A stack can hold more than the item's limit, from another mod. It keeps its size, so the group still fits its slots.
            size = Math.Max(size, count);
        }

        internal void Lay(List<ChestSortSlot> slots, List<int> removed)
        {
            var left = total;
            for (var i = 0; i < sources.Count; i++)
            {
                if (left == 0)
                {
                    removed.Add(sources[i]);
                    continue;
                }

                var count = (int)Math.Min(left, size);
                left -= count;
                slots.Add(new ChestSortSlot(sources[i], count));
            }
        }
    }
}
