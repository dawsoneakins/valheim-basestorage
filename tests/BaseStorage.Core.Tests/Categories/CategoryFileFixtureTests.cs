namespace BaseStorage.Core.Tests.Categories;

using System;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;

/// <summary>AC-1 and AC-2 (contract 5.1): the catalog and the issues of fixture F, verbatim from the contract.</summary>
public sealed class CategoryFileFixtureTests
{
    // GW-U-146
    [Fact]
    public void FixtureFCategoriesHoldTheContractPrefabLists()
    {
        CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

        Assert.Equal(new[] { "Metals & Ores", "Wood", "Cooked Food", "Trophies", "Monster Drops", "Misc" }, catalog.CategoryNames);
        Assert.Equal(new[] { "CopperOre", "Copper", "TinOre", "Tin" }, catalog.Categories[0].Prefabs);
        Assert.Equal(new[] { "Wood", "FineWood", "RoundLog", "ElderBark" }, catalog.Categories[1].Prefabs);
        Assert.Equal(new[] { "CookedMeat", "QueensJam" }, catalog.Categories[2].Prefabs);
        Assert.Equal(new[] { "TrophyDeer", "TrophyBoar" }, catalog.Categories[3].Prefabs);
        Assert.Equal(new[] { "LeatherScraps" }, catalog.Categories[4].Prefabs);
        Assert.Empty(catalog.Categories[5].Prefabs);
    }

    // GW-U-146
    [Fact]
    public void FixtureFMappedAndUnmappedPrefabsMatchTheContract()
    {
        CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

        Assert.Equal("Metals & Ores", catalog.GetMappedCategory("TinOre"));
        Assert.Null(catalog.GetMappedCategory("Coins"));
        Assert.Null(catalog.GetMappedCategory("Stone"));
        Assert.Null(catalog.GetMappedCategory("copperore"));
        Assert.Null(catalog.GetMappedCategory(""));
        Assert.Null(catalog.GetMappedCategory("   "));
    }

    // GW-U-146
    [Fact]
    public void NullPrefabNameIsAnArgumentNullException()
    {
        CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { _ = catalog.GetMappedCategory(null!); });
        Assert.Equal("prefabName", ex.ParamName);
    }

    // GW-U-146
    [Fact]
    public void NullTextIsAnArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { _ = CategoryFile.Parse(null!); });
        Assert.Equal("text", ex.ParamName);
    }

    // GW-U-147
    [Fact]
    public void FixtureFIssuesAreTheSixContractIssuesInOrder()
    {
        CategoryParseResult result = CategoryFile.Parse(CategoryFixtures.F);

        Assert.Equal(CategoryFixtures.FIssues(), result.Issues);
    }

    // GW-U-147
    [Fact]
    public void FixtureFIssueLineNumbersCountBlankAndCommentLines()
    {
        CategoryParseResult result = CategoryFile.Parse(CategoryFixtures.F);

        Assert.Equal(new[] { 6, 7, 9, 10, 10, 12 }, result.Issues.Select(issue => issue.LineNumber));
    }

    // GW-U-147
    [Fact]
    public void FixtureFIssueSeveritiesMatchTheContract()
    {
        CategoryParseResult result = CategoryFile.Parse(CategoryFixtures.F);

        Assert.Equal(
            new[]
            {
                LogSeverity.Error,
                LogSeverity.Error,
                LogSeverity.Warning,
                LogSeverity.Warning,
                LogSeverity.Warning,
                LogSeverity.Error,
            },
            result.Issues.Select(issue => issue.Severity));
    }
}
