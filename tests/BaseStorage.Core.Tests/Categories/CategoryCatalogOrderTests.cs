namespace BaseStorage.Core.Tests.Categories;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BaseStorage.Core.Categories;

/// <summary>AC-4 (contract 4.1, 5.1, 5.5): file order is kept, and the catalog views are read-only.</summary>
public sealed class CategoryCatalogOrderTests
{
    // GW-U-154
    [Fact]
    public void FixtureFNamesAreInFileOrderWithTheMergedWoodAtIndexOne()
    {
        CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

        Assert.Equal(new[] { "Metals & Ores", "Wood", "Cooked Food", "Trophies", "Monster Drops", "Misc" }, catalog.CategoryNames);
        Assert.Equal(new[] { "Metals & Ores", "Wood", "Cooked Food", "Trophies", "Monster Drops", "Misc" }, catalog.Categories.Select(c => c.Name));
    }

    // GW-U-154
    [Fact]
    public void IndexOfUsesTheOrdinalPositionAndMinusOneWhenNothingMatches()
    {
        CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

        Assert.Equal(1, catalog.IndexOf("Wood"));
        Assert.Equal(5, catalog.IndexOf("Misc"));
        Assert.Equal(-1, catalog.IndexOf("wood"));
        Assert.Equal(-1, catalog.IndexOf("Keep"));
        Assert.Equal(-1, catalog.IndexOf(""));
        Assert.Equal(-1, catalog.IndexOf("Stone & Minerals"));
    }

    // GW-U-154
    [Fact]
    public void NullCategoryNameIsAnArgumentNullException()
    {
        CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { _ = catalog.IndexOf(null!); });
        Assert.Equal("categoryName", ex.ParamName);
    }

    // GW-U-154
    [Fact]
    public void ReversedFileGivesReversedOrderBecauseNothingIsSorted()
    {
        CategoryCatalog catalog = CategoryFile.Parse("B: b\nA: a").Catalog;

        Assert.Equal(new[] { "B", "A" }, catalog.CategoryNames);
    }

    // GW-U-162
    [Fact]
    public void CategoryNamesRejectsWrites()
    {
        CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

        Assert.Throws<NotSupportedException>(() => { ((IList<string>)catalog.CategoryNames).Add("Ammo"); });
        Assert.Equal(CategoryFixtures.FNames, catalog.CategoryNames);
    }

    // GW-U-162
    [Fact]
    public void PrefabListsRejectWrites()
    {
        CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

        Assert.Throws<NotSupportedException>(() => { ((IList<string>)catalog.Categories[0].Prefabs)[0] = "Iron"; });
        Assert.Equal(new[] { "CopperOre", "Copper", "TinOre", "Tin" }, catalog.Categories[0].Prefabs);
    }

    // GW-U-162
    [Fact]
    public void IssuesRejectWrites()
    {
        CategoryParseResult result = CategoryFile.Parse(CategoryFixtures.F);

        Assert.Throws<NotSupportedException>(() => { ((IList<CategoryParseIssue>)result.Issues).Clear(); });
        Assert.Equal(CategoryFixtures.FIssues(), result.Issues);
    }

    // GW-U-162
    [Fact]
    public void ViewsAreReadOnlyCollectionsAndTwoReadsGiveEqualContent()
    {
        CategoryParseResult result = CategoryFile.Parse(CategoryFixtures.F);
        CategoryCatalog catalog = result.Catalog;

        Assert.IsType<ReadOnlyCollection<CategoryDefinition>>(catalog.Categories);
        Assert.IsType<ReadOnlyCollection<string>>(catalog.CategoryNames);
        Assert.IsType<ReadOnlyCollection<string>>(catalog.Categories[0].Prefabs);
        Assert.IsType<ReadOnlyCollection<CategoryParseIssue>>(result.Issues);
        Assert.Equal(CategoryFixtures.Describe(catalog), CategoryFixtures.Describe(catalog));
    }
}
