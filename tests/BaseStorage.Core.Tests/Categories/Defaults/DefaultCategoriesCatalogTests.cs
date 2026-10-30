namespace BaseStorage.Core.Tests.Categories.Defaults;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;

/// <summary>AC-1 (contract D5, 3.3): the catalog mirrors the default table and resolves the example rows.</summary>
public sealed class DefaultCategoriesCatalogTests
{
    public static IEnumerable<object[]> UndefinedAndEdgeKinds() => Enumerable.Range(-1, 10).Select(k => new object[] { k });

    // GW-U-438
    [Fact]
    public void CatalogCategoryNamesAreTheFourteenLiteralNames()
    {
        Assert.Equal(DefaultCategoryFixtures.Names, DefaultCategories.Catalog.CategoryNames);
    }

    // GW-U-438
    [Fact]
    public void EachCatalogCategoryHoldsItsFixtureListInOrder()
    {
        CategoryCatalog catalog = DefaultCategories.Catalog;

        Assert.Equal(DefaultCategoryFixtures.Names.Length, catalog.Categories.Count);
        for (int i = 0; i < DefaultCategoryFixtures.Names.Length; i++)
        {
            Assert.Equal(DefaultCategoryFixtures.Prefabs[i], catalog.Categories[i].Prefabs);
        }
    }

    // GW-U-438
    [Fact]
    public void TwoReadsOfTheCatalogHaveEqualContent()
    {
        CategoryCatalog first = DefaultCategories.Catalog;
        CategoryCatalog second = DefaultCategories.Catalog;

        Assert.Equal(first.CategoryNames, second.CategoryNames);
        for (int i = 0; i < first.Categories.Count; i++)
        {
            Assert.Equal(first.Categories[i].Prefabs, second.Categories[i].Prefabs);
        }
    }

    // GW-U-438
    [Fact]
    public void MappingAndResolutionExamplesMatchSection33()
    {
        CategoryCatalog catalog = DefaultCategories.Catalog;

        Assert.Equal("Raw Food", catalog.GetMappedCategory("Raspberry"));
        Assert.Equal("Raw Food", catalog.Resolve("Raspberry", ItemKind.Food));
        Assert.Equal("Meads & Potions", catalog.Resolve("MeadHealthMinor", ItemKind.Other));
        Assert.Equal("Gear", catalog.Resolve("SwordBronze", ItemKind.Weapon));
        Assert.Equal("Misc", catalog.Resolve("ModdedGem", ItemKind.Other));
        Assert.Equal(12, catalog.IndexOf("Ammo"));
    }

    // GW-U-438 (D2)
    [Theory]
    [MemberData(nameof(UndefinedAndEdgeKinds))]
    public void UnmappedPrefabsResolveToADefaultCategoryForEveryKind(int kind)
    {
        string resolved = DefaultCategories.Catalog.Resolve("Unmapped_X", (ItemKind)kind);

        Assert.Contains(resolved, DefaultCategoryFixtures.Names);
    }
}
