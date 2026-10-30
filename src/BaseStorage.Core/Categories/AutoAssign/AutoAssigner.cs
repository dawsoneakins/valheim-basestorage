namespace BaseStorage.Core.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Resolves every container of a base at the start of an Organize: an unassigned chest takes the category that holds at least
/// the threshold percent of its stacks, a chest with items and no such category stays a source, and an empty chest goes to the
/// overflow pool. Pure and thread-safe: the inputs are only read.
/// </summary>
public static class AutoAssigner
{
    /// <summary>Resolves every container of a base snapshot at the start of an Organize.</summary>
    /// <param name="containers">The base's Organize-eligible member chests, in snapshot order. Not null, no null elements, no duplicate ids.</param>
    /// <param name="assignments">The persisted assignment per chest id. A missing id means unassigned. Not null, no null values.</param>
    /// <param name="catalog">The current category catalog. Not null.</param>
    /// <param name="itemKinds">The item kind per prefab name; a missing prefab is Other. Not null.</param>
    /// <param name="autoAssignThresholdPct">The threshold in percent, any int, taken as given.</param>
    /// <returns>The immutable result, with one entry per container in snapshot order.</returns>
    /// <exception cref="ArgumentNullException">Thrown for a null <paramref name="containers"/>, <paramref name="assignments"/>, <paramref name="catalog"/> or <paramref name="itemKinds"/>.</exception>
    /// <exception cref="ArgumentException">Thrown for a null element or a duplicate id in <paramref name="containers"/>, or a null value in <paramref name="assignments"/>.</exception>
    public static AutoAssignResult Resolve(
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        CategoryCatalog catalog,
        IReadOnlyDictionary<string, ItemKind> itemKinds,
        int autoAssignThresholdPct)
    {
        if (containers is null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        var seen = new HashSet<ContainerId>();
        for (var c = 0; c < containers.Count; c++)
        {
            var container = containers[c];
            if (container is null)
            {
                throw new ArgumentException("containers contains a null element.", nameof(containers));
            }

            if (!seen.Add(container.Id))
            {
                throw new ArgumentException("containers contains a duplicate id.", nameof(containers));
            }
        }

        if (assignments is null)
        {
            throw new ArgumentNullException(nameof(assignments));
        }

        if (assignments is ICollection<KeyValuePair<ContainerId, CategoryAssignment>> all)
        {
            var copy = new KeyValuePair<ContainerId, CategoryAssignment>[all.Count];
            all.CopyTo(copy, 0);
            for (var a = 0; a < copy.Length; a++)
            {
                if (copy[a].Value is null)
                {
                    throw new ArgumentException("assignments contains a null value.", nameof(assignments));
                }
            }
        }
        else
        {
            for (var c = 0; c < containers.Count; c++)
            {
                if (assignments.TryGetValue(containers[c].Id, out var value) && value is null)
                {
                    throw new ArgumentException("assignments contains a null value.", nameof(assignments));
                }
            }
        }

        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (itemKinds is null)
        {
            throw new ArgumentNullException(nameof(itemKinds));
        }

        var entries = new ChestAutoAssignment[containers.Count];
        var overflow = new List<ContainerSnapshot>();
        for (var i = 0; i < containers.Count; i++)
        {
            var container = containers[i];
            ChestAutoAssignment entry;
            if (assignments.TryGetValue(container.Id, out var assigned))
            {
                entry = assigned.IsKeep
                    ? new ChestAutoAssignment(container, AutoAssignOutcome.Keep, CategoryAssignment.Keep)
                    : new ChestAutoAssignment(container, AutoAssignOutcome.AlreadyAssigned, assigned);
            }
            else if (container.Contents.Count == 0)
            {
                entry = new ChestAutoAssignment(container, AutoAssignOutcome.Overflow, null);
                overflow.Add(container);
            }
            else
            {
                entry = ResolveContents(container, catalog, itemKinds, autoAssignThresholdPct);
            }

            entries[i] = entry;
        }

        return new AutoAssignResult(autoAssignThresholdPct, entries, overflow.ToArray());
    }

    /// <summary>
    /// Gives the remaining mixed, unassigned chests of a threshold result a category where that category is short of
    /// capacity (S-A-001, contract section 2.3). Pure: it reads its arguments and returns a new result.
    /// </summary>
    /// <param name="resolved">The result of the threshold step, <c>Resolve</c>, over the chests whose demand counts. Not null.</param>
    /// <param name="catalog">The same catalog that was passed to <c>Resolve</c>. Not null.</param>
    /// <param name="itemKinds">The same kinds table that was passed to <c>Resolve</c>; a missing prefab is Other. Not null.</param>
    /// <returns>A new result. Only entries that were <c>SourceOnly</c> may become <c>Assigned</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown for a null <paramref name="resolved"/>, <paramref name="catalog"/> or <paramref name="itemKinds"/>.</exception>
    public static AutoAssignResult AssignMixedChests(
        AutoAssignResult resolved,
        CategoryCatalog catalog,
        IReadOnlyDictionary<string, ItemKind> itemKinds)
    {
        if (resolved is null)
        {
            throw new ArgumentNullException(nameof(resolved));
        }

        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (itemKinds is null)
        {
            throw new ArgumentNullException(nameof(itemKinds));
        }

        var chests = resolved.Chests;
        var count = chests.Count;
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        var names = new List<string>();
        var stackCats = new int[count][];
        for (var i = 0; i < count; i++)
        {
            var outcome = chests[i].Outcome;
            if (outcome == AutoAssignOutcome.Keep || outcome == AutoAssignOutcome.Overflow)
            {
                continue;
            }

            var contents = chests[i].Container.Contents;
            var cats = new int[contents.Count];
            for (var s = 0; s < contents.Count; s++)
            {
                var prefab = contents[s].PrefabName;
                var kind = itemKinds.TryGetValue(prefab, out var k) ? k : ItemKind.Other;
                cats[s] = CategoryId(catalog.Resolve(prefab, kind), ids, names);
            }

            stackCats[i] = cats;
        }

        for (var i = 0; i < count; i++)
        {
            var outcome = chests[i].Outcome;
            if (outcome == AutoAssignOutcome.AlreadyAssigned || outcome == AutoAssignOutcome.Assigned)
            {
                var name = chests[i].CategoryName;
                if (name is not null)
                {
                    CategoryId(name, ids, names);
                }
            }
        }

        var n = names.Count;
        var demand = new long[n];
        var capacity = new long[n];
        for (var i = 0; i < count; i++)
        {
            var cats = stackCats[i];
            if (cats is null)
            {
                continue;
            }

            for (var s = 0; s < cats.Length; s++)
            {
                demand[cats[s]]++;
            }

            var outcome = chests[i].Outcome;
            var name = chests[i].CategoryName;
            if ((outcome == AutoAssignOutcome.AlreadyAssigned || outcome == AutoAssignOutcome.Assigned) && name is not null)
            {
                capacity[ids[name]] += chests[i].Container.SlotCount;
            }
        }

        var order = RankCategories(names, catalog);
        var setAside = new bool[n];
        var given = new string?[count];
        var taken = new bool[count];
        while (true)
        {
            var anyRemaining = false;
            for (var i = 0; i < count; i++)
            {
                if (chests[i].Outcome == AutoAssignOutcome.SourceOnly && !taken[i])
                {
                    anyRemaining = true;
                    break;
                }
            }

            if (!anyRemaining)
            {
                break;
            }

            var pick = -1;
            long best = 0;
            for (var r = 0; r < n; r++)
            {
                var c = order[r];
                var shortfall = demand[c] - capacity[c];
                if (!setAside[c] && demand[c] > 0 && shortfall > best)
                {
                    best = shortfall;
                    pick = c;
                }
            }

            if (pick < 0)
            {
                break;
            }

            var chosen = -1;
            var chosenHeld = 0;
            for (var i = 0; i < count; i++)
            {
                if (chests[i].Outcome != AutoAssignOutcome.SourceOnly || taken[i])
                {
                    continue;
                }

                var cats = stackCats[i];
                var held = 0;
                for (var s = 0; s < cats.Length; s++)
                {
                    if (cats[s] == pick)
                    {
                        held++;
                    }
                }

                if (held > chosenHeld)
                {
                    chosenHeld = held;
                    chosen = i;
                }
            }

            if (chosen < 0)
            {
                setAside[pick] = true;
                continue;
            }

            taken[chosen] = true;
            given[chosen] = names[pick];
            capacity[pick] += chests[chosen].Container.SlotCount;
        }

        var entries = new ChestAutoAssignment[count];
        var mixed = new List<ChestAutoAssignment>();
        for (var i = 0; i < count; i++)
        {
            var name = given[i];
            if (name is null)
            {
                entries[i] = chests[i];
                continue;
            }

            entries[i] = new ChestAutoAssignment(chests[i].Container, AutoAssignOutcome.Assigned, CategoryAssignment.ForCategory(name));
            mixed.Add(entries[i]);
        }

        var overflow = new ContainerSnapshot[resolved.OverflowChests.Count];
        for (var i = 0; i < overflow.Length; i++)
        {
            overflow[i] = resolved.OverflowChests[i];
        }

        return new AutoAssignResult(resolved.AutoAssignThresholdPct, entries, overflow, mixed.ToArray());
    }

    private static int CategoryId(string name, Dictionary<string, int> ids, List<string> names)
    {
        if (!ids.TryGetValue(name, out var id))
        {
            id = names.Count;
            ids[name] = id;
            names.Add(name);
        }

        return id;
    }

    private static int[] RankCategories(List<string> names, CategoryCatalog catalog)
    {
        var order = new int[names.Count];
        var indexes = new int[names.Count];
        for (var i = 0; i < order.Length; i++)
        {
            var index = catalog.IndexOf(names[i]);
            indexes[i] = index < 0 ? int.MaxValue : index;
            var at = i;
            while (at > 0 && IsBetterRank(names[i], indexes[i], names[order[at - 1]], indexes[order[at - 1]]))
            {
                order[at] = order[at - 1];
                at--;
            }

            order[at] = i;
        }

        return order;
    }

    private static ChestAutoAssignment ResolveContents(
        ContainerSnapshot container,
        CategoryCatalog catalog,
        IReadOnlyDictionary<string, ItemKind> itemKinds,
        int threshold)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var s = 0; s < container.Contents.Count; s++)
        {
            var stack = container.Contents[s];
            var kind = itemKinds.TryGetValue(stack.PrefabName, out var k) ? k : ItemKind.Other;
            var name = catalog.Resolve(stack.PrefabName, kind);
            counts.TryGetValue(name, out var current);
            counts[name] = current + 1;
        }

        string? winner = null;
        var winnerIndex = int.MaxValue;
        var max = 0;
        foreach (var pair in counts)
        {
            var index = catalog.IndexOf(pair.Key);
            var rankIndex = index < 0 ? int.MaxValue : index;
            var better = winner is null
                || pair.Value > max
                || (pair.Value == max && IsBetterRank(pair.Key, rankIndex, winner, winnerIndex));
            if (better)
            {
                winner = pair.Key;
                winnerIndex = rankIndex;
                max = pair.Value;
            }
        }

        var n = container.Contents.Count;
        if (winner is not null && ((long)max * 100) >= ((long)threshold * n))
        {
            return new ChestAutoAssignment(container, AutoAssignOutcome.Assigned, CategoryAssignment.ForCategory(winner));
        }

        return new ChestAutoAssignment(container, AutoAssignOutcome.SourceOnly, null);
    }

    private static bool IsBetterRank(string name, int index, string current, int currentIndex)
    {
        if (index != currentIndex)
        {
            return index < currentIndex;
        }

        return string.CompareOrdinal(name, current) < 0;
    }
}
