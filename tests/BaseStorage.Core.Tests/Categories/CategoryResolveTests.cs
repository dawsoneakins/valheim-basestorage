namespace BaseStorage.Core.Tests.Categories;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;

/// <summary>AC-3 (contract 4.2, 5.2, 5.3): the resolver over fixture F, the fallback spelling and an empty catalog.</summary>
public sealed class CategoryResolveTests
{
    // GW-U-152
    [Theory]
    [MemberData(nameof(ResolverRowData))]
    public void ResolverGivesTheContractCategoryForEachRow(string prefabName, ItemKind kind, string expected)
    {
        CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

        string actual = catalog.Resolve(prefabName, kind);

        Assert.Equal(expected, actual);
        if (catalog.CategoryNames.Contains(actual, StringComparer.Ordinal))
        {
            Assert.True(catalog.IndexOf(actual) >= 0);
        }
    }

    // GW-U-152
    [Fact]
    public void NullPrefabNameIsAnArgumentNullException()
    {
        CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { _ = catalog.Resolve(null!, ItemKind.Other); });
        Assert.Equal("prefabName", ex.ParamName);
    }

    // GW-U-153
    [Fact]
    public void FallbackUsesTheCatalogSpellingOfTheCategory()
    {
        CategoryCatalog catalog = CategoryFile.Parse("cooked food: CookedMeat").Catalog;

        Assert.Equal("cooked food", catalog.Resolve("SerpentStew", ItemKind.Food));
    }

    // GW-U-153
    [Fact]
    public void EmptyCatalogHasNoCategoriesAndResolvesByFallback()
    {
        CategoryCatalog empty = CategoryCatalog.Empty;

        Assert.Empty(empty.Categories);
        Assert.Empty(empty.CategoryNames);
        Assert.Equal("Misc", empty.Resolve("CopperOre", ItemKind.Other));
        Assert.Equal("Trophies", empty.Resolve("TrophyDeer", ItemKind.Trophy));
    }

    // GW-U-153
    [Fact]
    public void UndefinedKindResolvesToMiscInTheEmptyCatalog()
    {
        Assert.Equal("Misc", CategoryCatalog.Empty.Resolve("Resin", (ItemKind)8));
    }

    // GW-U-153
    [Fact]
    public void ResolveNeverReturnsNullOrEmptyForAnyKindValue()
    {
        CategoryCatalog fixture = CategoryFile.Parse(CategoryFixtures.F).Catalog;
        var catalogs = new List<CategoryCatalog> { CategoryCatalog.Empty, fixture };

        foreach (CategoryCatalog catalog in catalogs)
        {
            for (int value = -1; value <= 8; value++)
            {
                string resolved = catalog.Resolve("SomePrefab", (ItemKind)value);
                Assert.False(string.IsNullOrEmpty(resolved), $"kind {value} resolved to an empty name.");
            }
        }
    }

    /// <summary>The contract 5.2 resolver rows as xunit member data.</summary>
    public static IEnumerable<object[]> ResolverRowData() => CategoryFixtures.ResolverRowData();
}
