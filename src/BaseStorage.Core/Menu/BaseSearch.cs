namespace BaseStorage.Core.Menu;

using System;
using System.Collections.Generic;

/// <summary>
/// Narrows the menu's chest list to the chests holding a searched item, most first. Pure. A search matches any part of an item's display
/// name, ignoring case, and stacks of one item in a chest count as one hit.
/// </summary>
public static class BaseSearch
{
    /// <summary>Keeps the rows whose chest holds a matching item.</summary>
    /// <param name="rows">The rows.</param>
    /// <param name="query">The searched text; surrounding spaces are ignored, and empty text keeps every row.</param>
    /// <param name="nameOf">Gives the display name of a prefab.</param>
    /// <returns>The rows that match, each with its hits.</returns>
    public static IReadOnlyList<BaseMenuRow> Apply(IReadOnlyList<BaseMenuRow> rows, string query, Func<string, string> nameOf)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (nameOf is null)
        {
            throw new ArgumentNullException(nameof(nameOf));
        }

        var text = query.Trim();
        if (text.Length == 0)
        {
            return rows;
        }

        var found = new List<KeyValuePair<int, BaseMenuRow>>();
        for (var i = 0; i < rows.Count; i++)
        {
            var hits = HitsIn(rows[i], text, nameOf);
            if (hits.Count > 0)
            {
                found.Add(new KeyValuePair<int, BaseMenuRow>(i, rows[i].WithHits(hits)));
            }
        }

        // The original position breaks ties, because List.Sort is not stable and the list must not reshuffle between refreshes.
        found.Sort(static (a, b) =>
        {
            var byCount = TotalOf(b.Value).CompareTo(TotalOf(a.Value));
            return byCount != 0 ? byCount : a.Key.CompareTo(b.Key);
        });
        var result = new BaseMenuRow[found.Count];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = found[i].Value;
        }

        return result;
    }

    /// <summary>Adds up the matching items across rows.</summary>
    /// <param name="rows">The rows from <see cref="Apply"/>.</param>
    /// <returns>The total.</returns>
    public static int Total(IReadOnlyList<BaseMenuRow> rows)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        long total = 0;
        for (var i = 0; i < rows.Count; i++)
        {
            total += TotalOf(rows[i]);
        }

        return total > int.MaxValue ? int.MaxValue : (int)total;
    }

    private static long TotalOf(BaseMenuRow row)
    {
        long total = 0;
        for (var i = 0; i < row.Hits.Count; i++)
        {
            total += row.Hits[i].Count;
        }

        return total;
    }

    private static List<BaseSearchHit> HitsIn(BaseMenuRow row, string text, Func<string, string> nameOf)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var contents = row.Chest.Contents;
        for (var i = 0; i < contents.Count; i++)
        {
            var name = nameOf(contents[i].PrefabName);
            if (name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                counts.TryGetValue(name, out var count);
                counts[name] = count + contents[i].Count;
            }
        }

        var hits = new List<BaseSearchHit>(counts.Count);
        foreach (var pair in counts)
        {
            hits.Add(new BaseSearchHit(pair.Key, pair.Value));
        }

        hits.Sort(static (a, b) =>
        {
            var byCount = b.Count.CompareTo(a.Count);
            return byCount != 0 ? byCount : string.CompareOrdinal(a.Name, b.Name);
        });
        return hits;
    }
}
