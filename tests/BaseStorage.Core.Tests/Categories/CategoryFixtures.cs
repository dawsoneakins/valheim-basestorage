namespace BaseStorage.Core.Tests.Categories;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using BaseStorage.Core.Categories;

/// <summary>
/// Contract 5.1 fixture file F, its expected names, prefabs and issues, and the contract 5.2 resolver rows, as literals
/// (ADR-GWIN-019). Expected values are never read from <see cref="FallbackCategories"/> or from the parser.
/// </summary>
internal static class CategoryFixtures
{
    /// <summary>Contract 5.1 file F with LF line endings, copied verbatim from the contract.</summary>
    internal const string F = "# BaseStorage categories: one line per category\nMetals & Ores: CopperOre, Copper, TinOre, Tin\n\nWood: Wood, FineWood, RoundLog\nCooked Food: CookedMeat, QueensJam\nCopperOre Copper\n: Stone\nTrophies: TrophyDeer, TrophyBoar\nMonster Drops: TrophyDeer, LeatherScraps,\nWood: ElderBark, Wood\nMisc:\nKeep: Coins\n";

    /// <summary>The category names of F in file order (contract 5.1).</summary>
    internal static readonly string[] FNames = { "Metals & Ores", "Wood", "Cooked Food", "Trophies", "Monster Drops", "Misc" };

    /// <summary>The prefabs of each category of F, in the same order as <see cref="FNames"/> (contract 5.1).</summary>
    internal static readonly string[][] FPrefabs =
    {
        new[] { "CopperOre", "Copper", "TinOre", "Tin" },
        new[] { "Wood", "FineWood", "RoundLog", "ElderBark" },
        new[] { "CookedMeat", "QueensJam" },
        new[] { "TrophyDeer", "TrophyBoar" },
        new[] { "LeatherScraps" },
        Array.Empty<string>(),
    };

    /// <summary>The six issues of F in line order (contract 5.1).</summary>
    internal static CategoryParseIssue[] FIssues() => new[]
    {
        new CategoryParseIssue(6, CategoryParseIssueKind.MissingSeparator, "CopperOre Copper", null, null, null),
        new CategoryParseIssue(7, CategoryParseIssueKind.EmptyCategoryName, ": Stone", null, null, null),
        new CategoryParseIssue(9, CategoryParseIssueKind.DuplicatePrefab, "Monster Drops: TrophyDeer, LeatherScraps,", "Monster Drops", "TrophyDeer", "Trophies"),
        new CategoryParseIssue(10, CategoryParseIssueKind.DuplicateCategory, "Wood: ElderBark, Wood", "Wood", null, "Wood"),
        new CategoryParseIssue(10, CategoryParseIssueKind.DuplicatePrefab, "Wood: ElderBark, Wood", "Wood", "Wood", "Wood"),
        new CategoryParseIssue(12, CategoryParseIssueKind.ReservedCategoryName, "Keep: Coins", "Keep", null, null),
    };

    /// <summary>Contract 5.2 resolver rows over the catalog of F: prefab, item kind and the expected category.</summary>
    internal static readonly (string Prefab, ItemKind Kind, string Expected)[] ResolverRows =
    {
        ("TinOre", ItemKind.Other, "Metals & Ores"),
        ("TrophyDeer", ItemKind.Trophy, "Trophies"),
        ("ElderBark", ItemKind.Other, "Wood"),
        ("CookedMeat", ItemKind.Other, "Cooked Food"),
        ("SerpentStew", ItemKind.Food, "Cooked Food"),
        ("TrophyNeck", ItemKind.Trophy, "Trophies"),
        ("SwordIron", ItemKind.Weapon, "Gear"),
        ("ArmorBronzeChest", ItemKind.Armour, "Gear"),
        ("Hammer", ItemKind.Tool, "Gear"),
        ("ShieldWood", ItemKind.Shield, "Gear"),
        ("ArrowWood", ItemKind.Ammo, "Ammo"),
        ("MeadHealthMinor", ItemKind.Other, "Misc"),
        ("Coins", ItemKind.Other, "Misc"),
        ("copperore", ItemKind.Other, "Misc"),
        ("Resin", (ItemKind)99, "Misc"),
    };

    /// <summary>The resolver rows as xunit member data.</summary>
    internal static IEnumerable<object[]> ResolverRowData() =>
        ResolverRows.Select(row => new object[] { row.Prefab, row.Kind, row.Expected });

    /// <summary>The culture names of the NFR check (empty string is InvariantCulture).</summary>
    internal static readonly string[] CultureNames = { "", "en-US", "de-DE", "tr-TR", "ar-SA" };

    /// <summary>Runs the body with the given culture on both CurrentCulture and CurrentUICulture, then restores them.</summary>
    /// <param name="cultureName">The culture name; empty means InvariantCulture.</param>
    /// <param name="body">The work to run under the culture.</param>
    internal static void WithCulture(string cultureName, Action body)
    {
        CultureInfo culture = cultureName.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(cultureName);
        CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;
        CultureInfo previousUiCulture = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            body();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previousCulture;
            Thread.CurrentThread.CurrentUICulture = previousUiCulture;
        }
    }

    /// <summary>One line per category: the name, a colon, and the prefabs joined by commas. Used to compare catalogs.</summary>
    /// <param name="catalog">The catalog to describe.</param>
    /// <returns>The description lines in file order.</returns>
    internal static string[] Describe(CategoryCatalog catalog) =>
        catalog.Categories.Select(c => c.Name + ": " + string.Join(", ", c.Prefabs)).ToArray();

    /// <summary>The number of lines a reader sees in the text (the BCL TextReader.ReadLine rule).</summary>
    /// <param name="text">The text to count.</param>
    /// <returns>The line count.</returns>
    internal static int CountLines(string text)
    {
        using var reader = new System.IO.StringReader(text);
        int count = 0;
        while (reader.ReadLine() is not null)
        {
            count++;
        }

        return count;
    }
}
