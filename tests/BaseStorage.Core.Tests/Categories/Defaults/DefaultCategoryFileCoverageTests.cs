namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;

/// <summary>AC-6 (contract 4.6, G2, G3, G8): the sample list covers every fallback type, and an ObjectDB-sized list is covered once each.</summary>
public sealed class DefaultCategoryFileCoverageTests
{
    private static readonly ItemKind[] FallbackKinds =
    {
        ItemKind.Food, ItemKind.Trophy, ItemKind.Weapon, ItemKind.Armour, ItemKind.Tool, ItemKind.Shield, ItemKind.Ammo, ItemKind.Other,
    };

    // GW-U-451
    [Fact]
    public void SampleListHasMappedAndUnmappedItemsOfEveryFallbackTypeAndIsFullyCovered()
    {
        foreach (var row in DefaultCategoryFixtures.S)
        {
            Assert.Equal(row.Mapped, DefaultCategoryFixtures.LiteralCategoryOf(row.Prefab) is not null);
        }

        foreach (ItemKind kind in FallbackKinds)
        {
            Assert.True(DefaultCategoryFixtures.S.Any(row => row.Kind == kind && !row.Mapped), $"S has no unmapped {kind} item.");
        }

        string text = DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems());
        List<(string Category, string Token)> tokens = DefaultCategoryFixtures.ParseTokens(text);

        string[] expectedTokens = DefaultCategoryFixtures.S.Select(row => row.Prefab)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(prefab => prefab, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedTokens, tokens.Select(token => token.Token).OrderBy(token => token, StringComparer.Ordinal).ToArray());

        Assert.Contains(("Cooked Food", "MyMod_HoneyCake"), tokens);
        Assert.Contains(("Trophies", "TrophySerpent"), tokens);
        Assert.Contains(("Ammo", "ArrowCharred"), tokens);
        Assert.Contains(("Misc", "ModdedGem"), tokens);
        foreach (string gear in new[] { "ArmorBronzeChest", "PickaxeAntler", "ShieldWood", "SwordBronze" })
        {
            Assert.Contains(("Gear", gear), tokens);
        }
    }

    // GW-U-452
    [Fact]
    public void ObjectDbSizedListIsCoveredOnceEachAndResolvesToItsG3Category()
    {
        List<CategoryItem> items = DefaultCategoryFixtures.ObjectDbSized();
        string text = DefaultCategoryFile.Generate(items);
        List<(string Category, string Token)> tokens = DefaultCategoryFixtures.ParseTokens(text);

        string[] distinct = items.Select(item => item.PrefabName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(prefab => prefab, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(distinct.Length, tokens.Count);
        Assert.Equal(distinct.Length, tokens.Select(token => token.Token).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(distinct, tokens.Select(token => token.Token).OrderBy(token => token, StringComparer.Ordinal).ToArray());

        CategoryParseResult parsed = CategoryFile.Parse(text);
        Assert.Empty(parsed.Issues);
        Assert.Equal(DefaultCategoryFixtures.Names, parsed.Catalog.CategoryNames);

        foreach (IGrouping<string, CategoryItem> group in items.GroupBy(item => item.PrefabName, StringComparer.Ordinal))
        {
            string expected = DefaultCategoryFixtures.LiteralG3(group.Key, group.Select(item => item.Kind));
            foreach (CategoryItem item in group)
            {
                Assert.Equal(expected, parsed.Catalog.Resolve(item.PrefabName, item.Kind));
            }
        }
    }
}
