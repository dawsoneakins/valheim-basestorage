namespace BaseStorage.Core.Tests.Categories;

using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;

/// <summary>
/// AC-2 (contract 3.2 rules 7 and 8, 3.3, 3.4, 5.3): a duplicate category merges into the first, and a duplicate prefab keeps
/// its first mapping. Both raise a warning.
/// </summary>
public sealed class CategoryFileDuplicateTests
{
    // GW-U-149
    [Fact]
    public void DuplicatePrefabWithinOneLineKeepsTheFirstAndWarns()
    {
        CategoryParseResult result = CategoryFile.Parse("Wood: Wood, Wood");

        CategoryDefinition category = Assert.Single(result.Catalog.Categories);
        Assert.Equal(new[] { "Wood" }, category.Prefabs);
        CategoryParseIssue issue = Assert.Single(result.Issues);
        Assert.Equal(new CategoryParseIssue(1, CategoryParseIssueKind.DuplicatePrefab, "Wood: Wood, Wood", "Wood", "Wood", "Wood"), issue);
        Assert.Equal(LogSeverity.Warning, issue.Severity);
    }

    // GW-U-149
    [Fact]
    public void DuplicatePrefabInAnotherCategoryStaysWithTheFirstCategory()
    {
        CategoryParseResult result = CategoryFile.Parse("Trophies: TrophyDeer\nDrops: TrophyDeer, Leather");

        Assert.Equal(new[] { "Leather" }, result.Catalog.Categories[1].Prefabs);
        Assert.Equal("Trophies", result.Catalog.GetMappedCategory("TrophyDeer"));
        CategoryParseIssue issue = Assert.Single(result.Issues);
        Assert.Equal(new CategoryParseIssue(2, CategoryParseIssueKind.DuplicatePrefab, "Drops: TrophyDeer, Leather", "Drops", "TrophyDeer", "Trophies"), issue);
        Assert.Equal(LogSeverity.Warning, issue.Severity);
    }

    // GW-U-149
    [Fact]
    public void PrefabNamesThatDifferOnlyInCaseAreBothKept()
    {
        CategoryParseResult result = CategoryFile.Parse("Metals: CopperOre, copperore");

        CategoryDefinition category = Assert.Single(result.Catalog.Categories);
        Assert.Equal(new[] { "CopperOre", "copperore" }, category.Prefabs);
        Assert.Empty(result.Issues);
    }

    // GW-U-150
    [Fact]
    public void DuplicateCategoryMergesIntoTheFirstWithItsSpelling()
    {
        CategoryParseResult result = CategoryFile.Parse("Wood: Wood\nWOOD: FineWood");

        CategoryDefinition category = Assert.Single(result.Catalog.Categories);
        Assert.Equal("Wood", category.Name);
        Assert.Equal(new[] { "Wood", "FineWood" }, category.Prefabs);
        CategoryParseIssue issue = Assert.Single(result.Issues);
        Assert.Equal(new CategoryParseIssue(2, CategoryParseIssueKind.DuplicateCategory, "WOOD: FineWood", "WOOD", null, "Wood"), issue);
        Assert.Equal(LogSeverity.Warning, issue.Severity);
    }

    // GW-U-150
    [Fact]
    public void DuplicateCategoryLineReportsTheCategoryBeforeItsPrefabsInTokenOrder()
    {
        CategoryParseResult result = CategoryFile.Parse("A: x, y\na: y, z, x");

        CategoryDefinition category = Assert.Single(result.Catalog.Categories);
        Assert.Equal(new[] { "x", "y", "z" }, category.Prefabs);
        Assert.Equal(
            new[]
            {
                new CategoryParseIssue(2, CategoryParseIssueKind.DuplicateCategory, "a: y, z, x", "a", null, "A"),
                new CategoryParseIssue(2, CategoryParseIssueKind.DuplicatePrefab, "a: y, z, x", "A", "y", "A"),
                new CategoryParseIssue(2, CategoryParseIssueKind.DuplicatePrefab, "a: y, z, x", "A", "x", "A"),
            },
            result.Issues);
    }

    // GW-U-150
    [Fact]
    public void FixtureFIssuesAreSortedByLineNumber()
    {
        CategoryParseResult result = CategoryFile.Parse(CategoryFixtures.F);

        int[] lines = result.Issues.Select(issue => issue.LineNumber).ToArray();
        Assert.Equal(lines.OrderBy(line => line).ToArray(), lines);
    }
}
