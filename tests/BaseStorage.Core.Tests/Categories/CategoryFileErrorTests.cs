namespace BaseStorage.Core.Tests.Categories;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;

/// <summary>AC-2 (contract 3.2 rules 3 and 4, 3.3, 5.3): error lines are reported and skipped, and the rest of the file survives.</summary>
public sealed class CategoryFileErrorTests
{
    // GW-U-148
    [Theory]
    [InlineData("Bad line\nWood: Wood", 1, CategoryParseIssueKind.MissingSeparator, "Bad line", null)]
    [InlineData(": Stone", 1, CategoryParseIssueKind.EmptyCategoryName, ": Stone", null)]
    [InlineData("Raw\tFood: RawMeat", 1, CategoryParseIssueKind.InvalidCategoryName, "Raw\tFood: RawMeat", "Raw\tFood")]
    [InlineData("auto: Wood", 1, CategoryParseIssueKind.ReservedCategoryName, "auto: Wood", "auto")]
    [InlineData("KEEP: Coins", 1, CategoryParseIssueKind.ReservedCategoryName, "KEEP: Coins", "KEEP")]
    [InlineData("x\ry: Stone", 1, CategoryParseIssueKind.MissingSeparator, "x", null)]
    public void ErrorLineGivesExactlyOneErrorIssueWithItsLineNumber(
        string text,
        int lineNumber,
        CategoryParseIssueKind kind,
        string lineText,
        string? categoryName)
    {
        CategoryParseResult result = CategoryFile.Parse(text);

        CategoryParseIssue issue = Assert.Single(result.Issues);
        Assert.Equal(new CategoryParseIssue(lineNumber, kind, lineText, categoryName, null, null), issue);
        Assert.Equal(LogSeverity.Error, issue.Severity);
    }

    // GW-U-148
    [Fact]
    public void ErrorLineDefinesNoCategoryButTheValidLineSurvives()
    {
        CategoryParseResult result = CategoryFile.Parse("Bad line\nWood: Wood");

        CategoryDefinition category = Assert.Single(result.Catalog.Categories);
        Assert.Equal("Wood", category.Name);
        Assert.Equal(new[] { "Wood" }, category.Prefabs);
    }

    // GW-U-148
    [Fact]
    public void ErrorLineInTheMiddleDoesNotBreakTheRestOfTheFile()
    {
        CategoryParseResult result = CategoryFile.Parse("x\ry: Stone");

        CategoryDefinition category = Assert.Single(result.Catalog.Categories);
        Assert.Equal("y", category.Name);
        Assert.Equal(new[] { "Stone" }, category.Prefabs);
    }

    // GW-U-148
    [Fact]
    public void ErrorLineMapsNoPrefabSoALaterLineMapsWithoutAWarning()
    {
        CategoryParseResult result = CategoryFile.Parse("Keep: Wood\nWood: Wood");

        CategoryParseIssue issue = Assert.Single(result.Issues);
        Assert.Equal(new CategoryParseIssue(1, CategoryParseIssueKind.ReservedCategoryName, "Keep: Wood", "Keep", null, null), issue);
        Assert.Equal("Wood", result.Catalog.GetMappedCategory("Wood"));
    }
}
