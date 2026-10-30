namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Persistence;

/// <summary>
/// The contract 3.3 literals and the dictionary seeds shared by the favourites adapter tests (ADR-GWIN-045). Not a test row.
/// </summary>
internal static class FavouriteFixtures
{
    /// <summary>A key that another mod owns. It must never be read or written (FV3).</summary>
    public const string OtherKey = "othermod.flag";

    /// <summary>The value of <see cref="OtherKey"/>.</summary>
    public const string OtherValue = "on";

    /// <summary>A second foreign key, used by the key-scope tests.</summary>
    public const string ZzKey = "zz.other";

    /// <summary>The value of <see cref="ZzKey"/>.</summary>
    public const string ZzValue = "x";

    /// <summary>Builds a recorder holding {othermod.flag: on}, plus the favourites key when the value is not null.</summary>
    /// <param name="value">The raw favourites value, or null for an absent key.</param>
    /// <returns>A new recorder.</returns>
    public static RecordingDictionary Seed(string? value)
    {
        var entries = new List<KeyValuePair<string, string>> { new(OtherKey, OtherValue) };
        if (value is not null)
        {
            entries.Add(new KeyValuePair<string, string>(PersistenceKeys.Favourites, value));
        }

        return new RecordingDictionary(entries);
    }

    /// <summary>Copies the entries of a dictionary into a sorted array, so two states compare by content.</summary>
    /// <param name="dictionary">The dictionary to copy.</param>
    /// <returns>The entries ordered by key, ordinal.</returns>
    public static KeyValuePair<string, string>[] Sorted(IDictionary<string, string> dictionary) =>
        dictionary.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToArray();
}
