namespace BaseStorage.Plugin.Crafting;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Availability;

/// <summary>Turns recipe lines into Core craft requirements with the game's line filter (contract craft-from-base 3.3).</summary>
internal static class CraftRequirementLines
{
    /// <summary>Builds the requirement list for a craft.</summary>
    /// <param name="lines">The recipe lines in order.</param>
    /// <param name="multiplier">The craft multiplier; at least 1.</param>
    /// <returns>The read-only requirement list, or null when an amount overflows.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lines"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="multiplier"/> is less than 1.</exception>
    internal static IReadOnlyList<CraftRequirement>? Build(IReadOnlyList<RecipeLine> lines, int multiplier)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        if (multiplier < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        }

        var result = new List<CraftRequirement>(lines.Count);
        for (int index = 0; index < lines.Count; index++)
        {
            RecipeLine line = lines[index];
            string? name = line.PrefabName;
            if (name is null || string.IsNullOrWhiteSpace(name) || line.UpgraderResource)
            {
                continue;
            }

            long amount = (long)line.AmountPerCraft * multiplier;
            if (amount <= 0)
            {
                continue;
            }

            if (amount > int.MaxValue)
            {
                return null;
            }

            result.Add(new CraftRequirement(name, (int)amount));
        }

        return new ReadOnlyLines(result);
    }

    private sealed class ReadOnlyLines : IReadOnlyList<CraftRequirement>
    {
        private readonly List<CraftRequirement> items;

        internal ReadOnlyLines(List<CraftRequirement> items)
        {
            this.items = items;
        }

        public int Count => items.Count;

        public CraftRequirement this[int index] => items[index];

        public IEnumerator<CraftRequirement> GetEnumerator() => items.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
