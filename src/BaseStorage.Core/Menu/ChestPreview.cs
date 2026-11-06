namespace BaseStorage.Core.Menu;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Snapshots;

/// <summary>What a chest's row shows at a glance: its most plentiful items, and how many other kinds there are.</summary>
public sealed class ChestPreview
{
    private ChestPreview(IReadOnlyList<string> prefabs, int hidden)
    {
        Prefabs = prefabs;
        Hidden = hidden;
    }

    /// <summary>Gets the items to show, most plentiful first; identical amounts are in name order, so the row doesn't flicker.</summary>
    public IReadOnlyList<string> Prefabs { get; }

    /// <summary>Gets how many other kinds of item there are.</summary>
    public int Hidden { get; }

    /// <summary>Works out a chest's preview.</summary>
    /// <param name="contents">The chest's stacks.</param>
    /// <param name="maxIcons">How many icons fit; must not be negative.</param>
    /// <returns>The preview.</returns>
    public static ChestPreview Of(IReadOnlyList<ItemStackSnapshot> contents, int maxIcons)
    {
        if (contents == null)
        {
            throw new ArgumentNullException(nameof(contents));
        }

        if (maxIcons < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxIcons));
        }

        List<string> kinds = contents
            .GroupBy(stack => stack.PrefabName, StringComparer.Ordinal)
            .OrderByDescending(group => group.Sum(stack => (long)stack.Count))
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.Key)
            .ToList();
        return new ChestPreview(kinds.Take(maxIcons).ToList(), Math.Max(0, kinds.Count - maxIcons));
    }
}
