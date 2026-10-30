namespace BaseStorage.Core.Tests.Categories;

using BaseStorage.Core.Categories;

/// <summary>AC-1 (contract 3.2 rules 1, 2, 4 to 6, and 5.3): the line form of the category file.</summary>
public sealed class CategoryFileLineFormTests
{
    // GW-U-144
    [Theory]
    [InlineData("Wood:Wood,FineWood", "Wood", new[] { "Wood", "FineWood" })]
    [InlineData("Wood: , ,Wood", "Wood", new[] { "Wood" })]
    [InlineData("\tWood\t: Wood ", "Wood", new[] { "Wood" })]
    [InlineData("Ores: Iron: Scrap", "Ores", new[] { "Iron: Scrap" })]
    [InlineData("Wood: Wood # logs", "Wood", new[] { "Wood # logs" })]
    public void ValidLineGivesOneCategoryWithTrimmedPrefabsAndNoIssue(string text, string name, string[] prefabs)
    {
        CategoryParseResult result = CategoryFile.Parse(text);

        Assert.Empty(result.Issues);
        CategoryDefinition category = Assert.Single(result.Catalog.Categories);
        Assert.Equal(name, category.Name);
        Assert.Equal(prefabs, category.Prefabs);
    }

    // GW-U-144
    [Fact]
    public void LineWithNoPrefabsDefinesACategoryWithEmptyPrefabsAndNoIssue()
    {
        CategoryParseResult result = CategoryFile.Parse("Meads & Potions:");

        Assert.Empty(result.Issues);
        CategoryDefinition category = Assert.Single(result.Catalog.Categories);
        Assert.Equal("Meads & Potions", category.Name);
        Assert.Empty(category.Prefabs);
    }

    // GW-U-145
    [Theory]
    [InlineData("")]
    [InlineData("\n\n   \n")]
    [InlineData("   # indented comment")]
    public void BlankAndCommentOnlyTextGivesNoCategoriesAndNoIssues(string text)
    {
        CategoryParseResult result = CategoryFile.Parse(text);

        Assert.Empty(result.Catalog.Categories);
        Assert.Empty(result.Issues);
    }

    // GW-U-145
    [Fact]
    public void CommentLineContainingAColonAddsNoCategory()
    {
        CategoryParseResult result = CategoryFile.Parse("# BaseStorage categories: one line per category\n");

        Assert.Empty(result.Catalog.Categories);
        Assert.Empty(result.Issues);
    }

    // GW-U-145
    [Fact]
    public void CommentLineIsIgnoredAndTheNextCategoryIsKept()
    {
        CategoryParseResult result = CategoryFile.Parse("#Wood: Wood\nStone: Stone");

        Assert.Empty(result.Issues);
        Assert.Equal(new[] { "Stone" }, result.Catalog.CategoryNames);
    }
}
