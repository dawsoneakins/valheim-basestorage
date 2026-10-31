namespace BaseStorage.Core.Feeding;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// The items one station is kept fed with, in the order the player chose them, stored on the station as <c>Prefab=keep|Prefab=keep</c>.
/// Immutable. Decoding never throws: a damaged entry is skipped, so a bad value can only stop feeding, never break the station.
/// </summary>
public sealed class FeedSelection
{
    private const char EntrySeparator = '|';
    private const char ValueSeparator = '=';

    private FeedSelection(IReadOnlyList<FeedEntry> entries)
    {
        Entries = entries;
    }

    /// <summary>Gets the selection with no items.</summary>
    public static FeedSelection Empty { get; } = new FeedSelection(Array.Empty<FeedEntry>());

    /// <summary>Gets the items, in the order they were chosen.</summary>
    public IReadOnlyList<FeedEntry> Entries { get; }

    /// <summary>Reads stored text.</summary>
    /// <param name="raw">The stored text, or null for none.</param>
    /// <returns>The selection; the first entry of a repeated item wins.</returns>
    public static FeedSelection Decode(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return Empty;
        }

        var entries = new List<FeedEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in raw!.Split(EntrySeparator))
        {
            var split = part.IndexOf(ValueSeparator);
            if (split < 1
                || !int.TryParse(part.Substring(split + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var keep))
            {
                continue;
            }

            var prefab = part.Substring(0, split);
            if (seen.Add(prefab))
            {
                entries.Add(new FeedEntry(prefab, keep));
            }
        }

        return entries.Count == 0 ? Empty : new FeedSelection(entries);
    }

    /// <summary>Writes the selection as stored text.</summary>
    /// <returns>The text; empty for no items.</returns>
    public string Encode()
    {
        var parts = new string[Entries.Count];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = Entries[i].PrefabName + ValueSeparator + Entries[i].Keep.ToString(CultureInfo.InvariantCulture);
        }

        return string.Join(EntrySeparator.ToString(), parts);
    }

    /// <summary>Finds the entry of an item.</summary>
    /// <param name="prefabName">The item's prefab name.</param>
    /// <returns>The entry, or null when the item is not selected.</returns>
    public FeedEntry? Find(string prefabName)
    {
        for (var i = 0; i < Entries.Count; i++)
        {
            if (string.Equals(Entries[i].PrefabName, prefabName, StringComparison.Ordinal))
            {
                return Entries[i];
            }
        }

        return null;
    }

    /// <summary>Selects an item, or changes its keep amount and leaves its place in the order.</summary>
    /// <param name="prefabName">The item's prefab name.</param>
    /// <param name="keep">How many must stay in the base.</param>
    /// <returns>The new selection.</returns>
    public FeedSelection With(string prefabName, int keep)
    {
        var entries = new List<FeedEntry>(Entries.Count + 1);
        var replaced = false;
        for (var i = 0; i < Entries.Count; i++)
        {
            var same = string.Equals(Entries[i].PrefabName, prefabName, StringComparison.Ordinal);
            entries.Add(same ? new FeedEntry(prefabName, keep) : Entries[i]);
            replaced |= same;
        }

        if (!replaced)
        {
            entries.Add(new FeedEntry(prefabName, keep));
        }

        return new FeedSelection(entries);
    }

    /// <summary>Removes an item.</summary>
    /// <param name="prefabName">The item's prefab name.</param>
    /// <returns>The new selection.</returns>
    public FeedSelection Without(string prefabName)
    {
        var entries = new List<FeedEntry>(Entries.Count);
        for (var i = 0; i < Entries.Count; i++)
        {
            if (!string.Equals(Entries[i].PrefabName, prefabName, StringComparison.Ordinal))
            {
                entries.Add(Entries[i]);
            }
        }

        return entries.Count == 0 ? Empty : new FeedSelection(entries);
    }
}
