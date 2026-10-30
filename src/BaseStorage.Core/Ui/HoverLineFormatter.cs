namespace BaseStorage.Core.Ui;

using System;

/// <summary>Builds the one line BaseStorage adds to a hover text.</summary>
public static class HoverLineFormatter
{
    /// <summary>Builds the one line BaseStorage adds to a chest's hover text.</summary>
    /// <param name="stationCount">The number of stations in the chest's base; zero means not in a base.</param>
    /// <param name="categoryLabel">The already-resolved category label, or null or blank to omit it.</param>
    /// <param name="cycleKeyLabel">The label of the category cycle key, or null or blank when it is not bound.</param>
    /// <returns>A single line with no leading or trailing whitespace.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stationCount"/> is negative.</exception>
    public static string Chest(int stationCount, string? categoryLabel, string? cycleKeyLabel)
    {
        if (stationCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stationCount), stationCount, "The station count must not be negative.");
        }

        StringTable table = StringTable.English;
        string line = stationCount == 0
            ? table.Get(StringKeys.HoverChestNotInBase)
            : table.FormatPlural(StringKeys.HoverChestInBase, stationCount, stationCount);

        if (!string.IsNullOrWhiteSpace(categoryLabel))
        {
            line += " " + table.Format(StringKeys.HoverChestCategory, SingleLine.Of(categoryLabel!));
        }

        if (!string.IsNullOrWhiteSpace(cycleKeyLabel))
        {
            line += " " + table.Format(StringKeys.HoverChestCycleHint, SingleLine.Of(cycleKeyLabel!));
        }

        // Safety net: caller text is already single-lined above, but the contract guarantees one trimmed line.
        return SingleLine.Of(line).Trim();
    }

    /// <summary>Builds the one line BaseStorage adds to a crafting station's hover text.</summary>
    /// <param name="chestCount">The number of chests in the station's base.</param>
    /// <returns>A single line naming the chest count.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="chestCount"/> is negative.</exception>
    public static string Station(int chestCount)
    {
        if (chestCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chestCount), chestCount, "The chest count must not be negative.");
        }

        return StringTable.English.FormatPlural(StringKeys.HoverStationBaseChests, chestCount, chestCount);
    }
}
