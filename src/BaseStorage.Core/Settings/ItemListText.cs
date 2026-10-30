namespace BaseStorage.Core.Settings;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Parses and formats the comma-separated item list text used by ItemList settings.</summary>
public static class ItemListText
{
    /// <summary>Splits list text into trimmed, non-empty entries. Keeps order, duplicates and casing.</summary>
    /// <param name="text">The list text. Must not be null.</param>
    /// <returns>The entries as a read-only list. Empty text gives an empty list.</returns>
    /// <exception cref="ArgumentNullException">The text is null.</exception>
    public static IReadOnlyList<string> Parse(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var result = new List<string>();
        foreach (string part in text.Split(','))
        {
            string trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                result.Add(trimmed);
            }
        }

        return new ReadOnlyCollection<string>(result);
    }

    /// <summary>Joins entries into list text. Drops entries that are empty after trimming.</summary>
    /// <param name="entries">The entries. Must not be null, and no entry may be null or contain a comma.</param>
    /// <returns>The entries, trimmed and joined with commas.</returns>
    /// <exception cref="ArgumentNullException">The entries are null.</exception>
    /// <exception cref="ArgumentException">An entry is null or contains a comma.</exception>
    public static string Format(IEnumerable<string> entries)
    {
        if (entries == null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        var kept = new List<string>();
        foreach (string entry in entries)
        {
            if (entry == null)
            {
                throw new ArgumentException("An entry is null.", nameof(entries));
            }

            string trimmed = entry.Trim();
            if (trimmed.Contains(","))
            {
                throw new ArgumentException("An entry contains a comma.", nameof(entries));
            }

            if (trimmed.Length > 0)
            {
                kept.Add(trimmed);
            }
        }

        return string.Join(",", kept);
    }
}
