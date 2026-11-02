namespace BaseStorage.Core.Organize;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;

/// <summary>
/// Shares out the empty chests of a base when there are too few for every category that is short of room, so the categories that need
/// the most space get them, not whichever came first. Pure.
/// </summary>
internal static class SpareChestShares
{
    /// <summary>Works out how many empty chests each category may claim.</summary>
    /// <param name="auto">The chests of the base with their categories.</param>
    /// <param name="catalog">The catalog; after Misc, earlier categories win ties.</param>
    /// <param name="itemKinds">The item kind per prefab name.</param>
    /// <returns>The claims per category, or null when there are enough empty chests for everyone.</returns>
    internal static Dictionary<string, int>? Of(AutoAssignResult auto, CategoryCatalog catalog, IReadOnlyDictionary<string, ItemKind> itemKinds)
    {
        var shortfalls = Shortfalls(auto, catalog, itemKinds);
        var claims = new Dictionary<string, int>(StringComparer.Ordinal);
        var spares = auto.OverflowChests;
        for (var i = 0; i < spares.Count; i++)
        {
            var pick = Largest(shortfalls, catalog);
            if (pick is null)
            {
                return null;
            }

            shortfalls[pick] -= spares[i].SlotCount;
            claims.TryGetValue(pick, out var count);
            claims[pick] = count + 1;
        }

        return Largest(shortfalls, catalog) is null ? null : claims;
    }

    /// <summary>The slots each category needs beyond its chests, counting each item as merged into full stacks.</summary>
    private static Dictionary<string, long> Shortfalls(AutoAssignResult auto, CategoryCatalog catalog, IReadOnlyDictionary<string, ItemKind> itemKinds)
    {
        var totals = new Dictionary<ItemKey, long>();
        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        for (var c = 0; c < auto.Chests.Count; c++)
        {
            var chest = auto.Chests[c];
            var contents = chest.Container.Contents;
            for (var s = 0; s < contents.Count; s++)
            {
                var key = new ItemKey(contents[s].PrefabName, contents[s].Quality, contents[s].MaxStack);
                totals.TryGetValue(key, out var total);
                totals[key] = total + contents[s].Count;
            }

            if (chest.CategoryName is not null)
            {
                result.TryGetValue(chest.CategoryName, out var room);
                result[chest.CategoryName] = room - chest.Container.SlotCount;
            }
        }

        foreach (var pair in totals)
        {
            var kind = itemKinds.TryGetValue(pair.Key.PrefabName, out var known) ? known : ItemKind.Other;
            var category = catalog.Resolve(pair.Key.PrefabName, kind);
            result.TryGetValue(category, out var need);
            result[category] = need + ((pair.Value + pair.Key.MaxStack - 1) / pair.Key.MaxStack);
        }

        return result;
    }

    private static string? Largest(Dictionary<string, long> shortfalls, CategoryCatalog catalog)
    {
        string? best = null;
        long most = 0;
        foreach (var pair in shortfalls)
        {
            if (pair.Value > most || (pair.Value == most && best is not null && Comes(pair.Key, best, catalog)))
            {
                best = pair.Key;
                most = pair.Value;
            }
        }

        return best;
    }

    private static bool Comes(string name, string before, CategoryCatalog catalog)
    {
        var a = Rank(name, catalog);
        var b = Rank(before, catalog);
        return a != b ? a < b : string.CompareOrdinal(name, before) < 0;
    }

    /// <summary>Misc comes first in a tie: a Misc chest takes in the stacks of every category that gets no chest.</summary>
    private static int Rank(string name, CategoryCatalog catalog)
    {
        if (string.Equals(name, FallbackCategories.Misc, StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        var index = catalog.IndexOf(name);
        return index < 0 ? int.MaxValue : index;
    }
}
