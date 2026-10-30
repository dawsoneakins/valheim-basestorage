namespace BaseStorage.Core.Bases.Inspection;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;

/// <summary>
/// Builds the base lines that the hover text shows for a chest and for a crafting station (ADR-GWIN-032, spec S-E002-005).
/// The line is read from the cached base through <see cref="BaseCache"/>, and the last answer for each hovered object is kept
/// so that repeated hovers do not search the base again.
/// </summary>
/// <remarks>
/// Not thread-safe. Valheim calls the hover patches only from the main thread, as for <see cref="BaseCache"/>.
/// </remarks>
public sealed class HoverInspector
{
    private readonly BaseCache cache;
    private readonly Dictionary<ContainerId, ChestEntry> chestMemo = new Dictionary<ContainerId, ChestEntry>();
    private readonly Dictionary<StationId, string> stationMemo = new Dictionary<StationId, string>();
    private long memoGeneration = -1;

    /// <summary>Creates an inspector over a cache. The inspector reads bases only through this cache.</summary>
    /// <param name="cache">The base cache to read through; must not be null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> is null.</exception>
    public HoverInspector(BaseCache cache)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>Returns the base line for a chest, reusing the last answer when the cache has not changed for that chest.</summary>
    /// <param name="container">The id of the chest being hovered.</param>
    /// <param name="position">The position of the chest; used only when the base is searched again.</param>
    /// <param name="encodedCategory">The stored category value of the chest (<c>"k"</c> or <c>"c:&lt;name&gt;"</c>), or null for no value.</param>
    /// <param name="options">The cache window and station cap for this call.</param>
    /// <returns>The single line for the chest, with no line break.</returns>
    public string ChestLine(ContainerId container, WorldPosition position, string? encodedCategory, BaseCacheOptions options)
    {
        if (!container.IsNone && cache.IsCurrent(options) && cache.Generation == memoGeneration
            && chestMemo.TryGetValue(container, out ChestEntry? hit)
            && hit.Position == position
            && string.Equals(hit.Encoded, encodedCategory, StringComparison.Ordinal))
        {
            return hit.Line;
        }

        BaseResolution resolution = cache.Resolve(BaseStart.FromPosition(position), options);
        string line = FormatChest(resolution, CategoryCodec.Decode(encodedCategory));
        if (!container.IsNone)
        {
            SyncMemo();
            chestMemo[container] = new ChestEntry(position, encodedCategory, line);
        }

        return line;
    }

    /// <summary>Returns the base line for a crafting station, reusing the last answer when the cache has not changed for that station.</summary>
    /// <param name="station">The id of the crafting station being hovered.</param>
    /// <param name="options">The cache window and station cap for this call.</param>
    /// <returns>The single line naming the chests in the station's base, or the empty string when the station is in no base.</returns>
    public string StationLine(StationId station, BaseCacheOptions options)
    {
        if (station.IsNone)
        {
            return string.Empty;
        }

        if (cache.IsCurrent(options) && cache.Generation == memoGeneration && stationMemo.TryGetValue(station, out string? hit))
        {
            return hit;
        }

        BaseResolution resolution = cache.Resolve(BaseStart.FromStation(station), options);
        string line = FormatStation(resolution);
        SyncMemo();
        _ = stationMemo.Remove(station);
        stationMemo.Add(station, line);
        return line;
    }

    /// <summary>Formats the chest line from a resolved base and the chest's category assignment.</summary>
    /// <param name="resolution">The base that the chest is in, or the no-base result.</param>
    /// <param name="assignment">The chest's category assignment, or null when the chest is Auto.</param>
    /// <returns>The single line: the base segment, then the category or <c>[Keep]</c> when assigned.</returns>
    public static string FormatChest(BaseResolution resolution, CategoryAssignment? assignment)
    {
        if (resolution is null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        string? label = assignment is null
            ? null
            : assignment.IsKeep ? StringTable.English.Get(StringKeys.ChestCategoryKeep) : assignment.CategoryName;
        return HoverLineFormatter.Chest(resolution.IsInBase ? resolution.Stations.Count : 0, label, null);
    }

    /// <summary>Formats the station line from a resolved base.</summary>
    /// <param name="resolution">The base that the station is in, or the no-base result.</param>
    /// <returns>The single line naming the chests in the base, or the empty string when the station is in no base.</returns>
    public static string FormatStation(BaseResolution resolution)
    {
        if (resolution is null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        if (!resolution.IsInBase)
        {
            return string.Empty;
        }

        return HoverLineFormatter.Station(CountChests(resolution));
    }

    /// <summary>Counts the chests of a base. Carts, ships and tombstones are not chests.</summary>
    /// <param name="resolution">The base to count.</param>
    /// <returns>The number of member containers that are neither tombstones nor vehicles; zero when the start is in no base.</returns>
    public static int CountChests(BaseResolution resolution)
    {
        if (resolution is null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        var count = 0;
        foreach (ContainerSnapshot container in resolution.Containers)
        {
            if ((container.Flags & (ContainerFlags.Tombstone | ContainerFlags.Vehicle)) == 0)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Adds one line to a hover text, with a line break between them.</summary>
    /// <param name="hoverText">The vanilla hover text, or null or empty when vanilla shows none.</param>
    /// <param name="line">The single line to add; must not be null.</param>
    /// <returns>The hover text with the line added, or the hover text unchanged when it is null or empty or the line is empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="line"/> is null.</exception>
    public static string? Append(string? hoverText, string line)
    {
        if (line is null)
        {
            throw new ArgumentNullException(nameof(line));
        }

        if (string.IsNullOrEmpty(hoverText) || line.Length == 0)
        {
            return hoverText;
        }

        return hoverText + "\n" + line;
    }

    private void SyncMemo()
    {
        if (memoGeneration != cache.Generation)
        {
            chestMemo.Clear();
            stationMemo.Clear();
            memoGeneration = cache.Generation;
        }
    }

    private sealed class ChestEntry
    {
        internal ChestEntry(WorldPosition position, string? encoded, string line)
        {
            Position = position;
            Encoded = encoded;
            Line = line;
        }

        internal WorldPosition Position { get; }

        internal string? Encoded { get; }

        internal string Line { get; }
    }
}
