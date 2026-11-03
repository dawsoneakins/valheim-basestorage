namespace BaseStorage.Core.Menu;

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Narrows the menu's chest list to the chests holding a searched item, best match first. Pure. An exact name beats the start of a name,
/// which beats the start of a word, which beats any part of a name. Several words match in any order, and an item also matches by its
/// category, last of all. A chest given a category or Keep matches by that label the same way, so it is listed even when empty. Equal
/// matches go to the chest holding most, and stacks of one item in a chest count as one hit.
/// </summary>
public static class BaseSearch
{
    private const int NoMatch = -1;
    private const int Exact = 0;
    private const int Start = 1;
    private const int WordStart = 2;
    private const int Part = 3;
    private const int AllWords = 4;
    private const int Category = 5;

    private static readonly char[] Spaces = { ' ', '\t' };

    /// <summary>Keeps the rows whose chest holds a matching item.</summary>
    /// <param name="rows">The rows.</param>
    /// <param name="query">The searched text; surrounding spaces are ignored, and empty text keeps every row.</param>
    /// <param name="nameOf">Gives the display name of a prefab.</param>
    /// <param name="categoryOf">Gives the category of a prefab, or null to search names only.</param>
    /// <returns>The rows that match, each with its hits.</returns>
    public static IReadOnlyList<BaseMenuRow> Apply(IReadOnlyList<BaseMenuRow> rows, string query, Func<string, string> nameOf, Func<string, string>? categoryOf = null)
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

        var words = text.Split(Spaces, StringSplitOptions.RemoveEmptyEntries);
        var found = new List<KeyValuePair<int, BaseMenuRow>>();
        for (var i = 0; i < rows.Count; i++)
        {
            var hits = HitsIn(rows[i], text, words, nameOf, categoryOf);
            var chest = ChestRank(rows[i], text, words);
            var rank = hits.Count == 0 ? chest : chest == NoMatch ? hits[0].Rank : Math.Min(chest, hits[0].Rank);
            if (rank != NoMatch)
            {
                found.Add(new KeyValuePair<int, BaseMenuRow>(i, rows[i].WithHits(hits, rank)));
            }
        }

        // The original position breaks ties, because List.Sort is not stable and the list must not reshuffle between refreshes.
        found.Sort(static (a, b) =>
        {
            var byRank = a.Value.SearchRank.CompareTo(b.Value.SearchRank);
            if (byRank != 0)
            {
                return byRank;
            }

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

    /// <summary>Lists the different items found across rows, the best match first.</summary>
    /// <param name="rows">The rows from <see cref="Apply"/>.</param>
    /// <returns>The display names.</returns>
    public static IReadOnlyList<string> Items(IReadOnlyList<BaseMenuRow> rows)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < rows.Count; i++)
        {
            for (var h = 0; h < rows[i].Hits.Count; h++)
            {
                if (seen.Add(rows[i].Hits[h].Name))
                {
                    names.Add(rows[i].Hits[h].Name);
                }
            }
        }

        return names;
    }

    /// <summary>Wraps the parts of a name that the search matched.</summary>
    /// <param name="name">The display name.</param>
    /// <param name="query">The searched text.</param>
    /// <param name="open">The text put before a matched part.</param>
    /// <param name="close">The text put after a matched part.</param>
    /// <returns>The marked name; the name itself when nothing in it matched, as for a match by category.</returns>
    public static string Mark(string name, string query, string open, string close)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var marked = new bool[name.Length];
        var any = false;
        var text = query.Trim();
        var words = name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ? new[] { text } : text.Split(Spaces, StringSplitOptions.RemoveEmptyEntries);
        for (var w = 0; w < words.Length && text.Length > 0; w++)
        {
            var at = name.IndexOf(words[w], StringComparison.OrdinalIgnoreCase);
            while (at >= 0)
            {
                any = true;
                for (var i = at; i < at + words[w].Length; i++)
                {
                    marked[i] = true;
                }

                at = at + 1 < name.Length ? name.IndexOf(words[w], at + 1, StringComparison.OrdinalIgnoreCase) : -1;
            }
        }

        if (!any)
        {
            return name;
        }

        var result = new StringBuilder(name.Length + open.Length + close.Length);
        for (var i = 0; i < name.Length; i++)
        {
            if (marked[i] && (i == 0 || !marked[i - 1]))
            {
                result.Append(open);
            }

            result.Append(name[i]);
            if (marked[i] && (i == name.Length - 1 || !marked[i + 1]))
            {
                result.Append(close);
            }
        }

        return result.ToString();
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

    /// <summary>Matches the chest's name and own label. An Auto chest has no category of its own, so it is found by its name or what it holds.</summary>
    private static int ChestRank(BaseMenuRow row, string text, string[] words)
    {
        var byName = row.Name.Length == 0 ? NoMatch : RankOf(row.Name, string.Empty, text, words, null);
        var byLabel = row.Assignment is null ? NoMatch : RankOf(row.RuleLabel, string.Empty, text, words, null);
        return byName == NoMatch ? byLabel : byLabel == NoMatch ? byName : Math.Min(byName, byLabel);
    }

    private static int RankOf(string name, string prefab, string text, string[] words, Func<string, string>? categoryOf)
    {
        var at = name.IndexOf(text, StringComparison.OrdinalIgnoreCase);
        if (at == 0)
        {
            return name.Length == text.Length ? Exact : Start;
        }

        if (at > 0)
        {
            return name[at - 1] == ' ' ? WordStart : Part;
        }

        if (HasEvery(name, words))
        {
            return AllWords;
        }

        return categoryOf is not null && HasEvery(name + " " + categoryOf(prefab), words) ? Category : NoMatch;
    }

    private static bool HasEvery(string text, string[] words)
    {
        for (var w = 0; w < words.Length; w++)
        {
            if (text.IndexOf(words[w], StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static List<BaseSearchHit> HitsIn(BaseMenuRow row, string text, string[] words, Func<string, string> nameOf, Func<string, string>? categoryOf)
    {
        var counts = new Dictionary<string, BaseSearchHit>(StringComparer.Ordinal);
        var contents = row.Chest.Contents;
        for (var i = 0; i < contents.Count; i++)
        {
            var prefab = contents[i].PrefabName;
            var name = nameOf(prefab);
            if (counts.TryGetValue(name, out var known))
            {
                counts[name] = new BaseSearchHit(name, known.Count + contents[i].Count, known.Prefab, known.Rank);
                continue;
            }

            var rank = RankOf(name, prefab, text, words, categoryOf);
            if (rank != NoMatch)
            {
                counts[name] = new BaseSearchHit(name, contents[i].Count, prefab, rank);
            }
        }

        var hits = new List<BaseSearchHit>(counts.Values);
        hits.Sort(static (a, b) =>
        {
            var byRank = a.Rank.CompareTo(b.Rank);
            if (byRank != 0)
            {
                return byRank;
            }

            var byCount = b.Count.CompareTo(a.Count);
            return byCount != 0 ? byCount : string.CompareOrdinal(a.Name, b.Name);
        });
        return hits;
    }
}
