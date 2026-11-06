namespace BaseStorage.Core.BaseMove;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Snapshots;

/// <summary>Principle 1 for a move: the writer compares the item totals before and after, and writes nothing unless they match. Pure.</summary>
public static class MoveConservation
{
    /// <summary>Tells whether two sets of chests hold exactly the same items, by prefab and quality.</summary>
    /// <param name="before">The chests before the move. Not null.</param>
    /// <param name="after">The same chests after the move. Not null.</param>
    /// <returns>True when every total matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a list is null.</exception>
    public static bool Holds(IReadOnlyList<ContainerSnapshot> before, IReadOnlyList<ContainerSnapshot> after)
    {
        if (before is null)
        {
            throw new ArgumentNullException(nameof(before));
        }

        if (after is null)
        {
            throw new ArgumentNullException(nameof(after));
        }

        var a = Totals(before);
        var b = Totals(after);
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var pair in a)
        {
            if (!b.TryGetValue(pair.Key, out var other) || other != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, long> Totals(IReadOnlyList<ContainerSnapshot> chests)
    {
        var totals = new Dictionary<string, long>(StringComparer.Ordinal);
        for (var c = 0; c < chests.Count; c++)
        {
            var contents = chests[c].Contents;
            for (var i = 0; i < contents.Count; i++)
            {
                var key = contents[i].PrefabName + "|" + contents[i].Quality.ToString(CultureInfo.InvariantCulture);
                totals.TryGetValue(key, out var total);
                totals[key] = total + contents[i].Count;
            }
        }

        return totals;
    }
}
