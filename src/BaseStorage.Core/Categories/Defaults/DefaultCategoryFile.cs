namespace BaseStorage.Core.Categories.Defaults;

using System;
using System.Collections.Generic;
using System.Text;
using BaseStorage.Core.Categories;

/// <summary>
/// Builds the text of the default category file that lists every item exactly once (spec AC-4, AC-5, AC-6, AC-9).
/// The output is LF only and independent of input order and culture. The Plugin writes it as UTF-8 without a byte order mark.
/// </summary>
public static class DefaultCategoryFile
{
    private const string Header =
        "# BaseStorage categories file (BaseStorage.categories.txt)\n"
        + "# One line per category: Category Name: PrefabA, PrefabB, ...\n"
        + "# Lines starting with # and blank lines are ignored. A prefab belongs to the first category that lists it.\n"
        + "# Items not listed here fall back by type: food to Cooked Food, trophies to Trophies,\n"
        + "# weapons, armour, tools and shields to Gear, ammo to Ammo, everything else to Misc.\n"
        + "# BaseStorage wrote this file on first run and never overwrites it, so your edits are kept.\n"
        + "\n";

    /// <summary>The category file name, without folder: "BaseStorage.categories.txt".</summary>
    public const string FileName = "BaseStorage.categories.txt";

    /// <summary>Builds the full text of a default category file that lists every distinct item exactly once.</summary>
    /// <param name="items">The items to list. A null element is an argument error.</param>
    /// <returns>The header followed by the 14 category lines, each terminated by a line feed.</returns>
    /// <exception cref="ArgumentNullException">items is null.</exception>
    /// <exception cref="ArgumentException">items contains a null element.</exception>
    public static string Generate(IEnumerable<CategoryItem> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        IReadOnlyList<string> names = DefaultCategories.Names;
        var nameIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < names.Count; i++)
        {
            nameIndex[names[i]] = i;
        }

        var mappedPresent = new Dictionary<string, int>(StringComparer.Ordinal);
        var fallbackPlacement = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (CategoryItem? item in items)
        {
            if (item is null)
            {
                throw new ArgumentException("The items must not contain a null element.", nameof(items));
            }

            string? mapped = DefaultCategories.Catalog.GetMappedCategory(item.PrefabName);
            if (mapped != null)
            {
                mappedPresent[item.PrefabName] = nameIndex[mapped];
                continue;
            }

            int index = nameIndex[FallbackCategories.For(item.Kind)];
            if (!fallbackPlacement.TryGetValue(item.PrefabName, out int existing) || index < existing)
            {
                fallbackPlacement[item.PrefabName] = index;
            }
        }

        var extrasByCategory = new List<string>[names.Count];
        foreach (KeyValuePair<string, int> pair in fallbackPlacement)
        {
            (extrasByCategory[pair.Value] ??= new List<string>()).Add(pair.Key);
        }

        var builder = new StringBuilder(Header);
        for (int i = 0; i < names.Count; i++)
        {
            var tokens = new List<string>();
            foreach (string prefab in DefaultCategories.GetPrefabs(names[i]))
            {
                if (mappedPresent.TryGetValue(prefab, out int owner) && owner == i)
                {
                    tokens.Add(prefab);
                }
            }

            List<string>? extras = extrasByCategory[i];
            if (extras != null)
            {
                extras.Sort(StringComparer.Ordinal);
                tokens.AddRange(extras);
            }

            builder.Append(names[i]).Append(':');
            if (tokens.Count > 0)
            {
                builder.Append(' ').Append(string.Join(", ", tokens));
            }

            builder.Append('\n');
        }

        return builder.ToString();
    }
}
