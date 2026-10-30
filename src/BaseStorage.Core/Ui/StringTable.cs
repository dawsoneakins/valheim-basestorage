namespace BaseStorage.Core.Ui;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

/// <summary>The English strings table. Every user-facing string the mod shows lives here.</summary>
public sealed class StringTable
{
    private readonly Dictionary<string, Entry> _entries;

    private StringTable(Dictionary<string, Entry> entries)
    {
        _entries = entries;
        Keys = new ReadOnlyCollection<string>(entries.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList());
    }

    private readonly struct Entry
    {
        public Entry(string text)
        {
            Text = text;
            MaxIndex = MaxPlaceholderIndex(text);
        }

        public string Text { get; }

        public int MaxIndex { get; }
    }

    /// <summary>Gets the built-in English table holding every key of the contract.</summary>
    public static StringTable English { get; } = Build(EnglishStrings.Pairs);

    private static StringTable Build((string Key, string Text)[] pairs)
    {
        var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach ((string key, string text) in pairs)
        {
            entries.Add(key, new Entry(text));
        }

        return new StringTable(entries);
    }

    private static int MaxPlaceholderIndex(string template)
    {
        int max = -1;
        int i = 0;
        while (i < template.Length)
        {
            if (template[i] != '{')
            {
                i++;
                continue;
            }

            int j = i + 1;
            int value = 0;
            while (j < template.Length && template[j] >= '0' && template[j] <= '9')
            {
                value = (value * 10) + (template[j] - '0');
                j++;
            }

            if (j > i + 1 && j < template.Length && template[j] == '}')
            {
                max = Math.Max(max, value);
            }

            i = j;
        }

        return max;
    }

    /// <summary>Gets every key in the table, in ordinal order, as a read-only view.</summary>
    public IReadOnlyCollection<string> Keys { get; }

    /// <summary>Determines whether the table holds the key (ordinal, case-sensitive).</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>True when the key exists.</returns>
    public bool Contains(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _entries.ContainsKey(key);
    }

    /// <summary>Gets the raw template text for the key.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The template verbatim, or the key itself when it is missing.</returns>
    public string Get(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _entries.TryGetValue(key, out Entry entry) ? entry.Text : key;
    }

    /// <summary>Formats the template for the key with the arguments, using the invariant culture.</summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="args">The placeholder values; null is treated as empty and extra values are ignored.</param>
    /// <returns>The formatted text, the unformatted template when an argument is missing, or the key when it is missing.</returns>
    public string Format(string key, params object?[]? args)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (!_entries.TryGetValue(key, out Entry entry))
        {
            return key;
        }

        object?[] values = args ?? Array.Empty<object?>();
        if (entry.MaxIndex >= values.Length)
        {
            return entry.Text;
        }

        return string.Format(CultureInfo.InvariantCulture, entry.Text, values);
    }

    /// <summary>Formats the plural variant of the key selected by the count.</summary>
    /// <param name="key">The plural base key.</param>
    /// <param name="count">The count that selects the plural form.</param>
    /// <param name="args">The placeholder values; the count is not inserted automatically.</param>
    /// <returns>The formatted text, or the selected full key when it is missing.</returns>
    public string FormatPlural(string key, int count, params object?[]? args)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return Format(key + "." + Plural.Form(count), args);
    }
}
