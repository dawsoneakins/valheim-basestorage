namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;

/// <summary>AC-5 (contract G8, 4.5-4.7, C4): the generated text parses with the S-E004-001 parser and resolves as written.</summary>
public sealed class DefaultCategoryFileRoundTripTests
{
    public static IEnumerable<object[]> EdgeRowIndices() => Enumerable.Range(0, 9).Select(i => new object[] { i });

    // GW-U-448
    [Fact]
    public void RoundTripOfSParsesWithoutIssuesAndKeepsTheNamesAndTheCount()
    {
        CategoryParseResult result = CategoryFile.Parse(DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems()));

        Assert.Empty(result.Issues);
        Assert.Equal(DefaultCategoryFixtures.Names, result.Catalog.CategoryNames);
        Assert.Equal(25, result.Catalog.Categories.Sum(category => category.Prefabs.Count));
    }

    // GW-U-448
    [Fact]
    public void EveryRowOfSResolvesToItsWrittenCategory()
    {
        CategoryCatalog catalog = CategoryFile.Parse(DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems())).Catalog;

        foreach (var row in DefaultCategoryFixtures.S)
        {
            Assert.Equal(row.WrittenUnder, catalog.Resolve(row.Prefab, row.Kind));
        }

        Assert.Equal("Raw Food", catalog.GetMappedCategory("Raspberry"));
        Assert.Null(catalog.GetMappedCategory("TinOre"));
        Assert.Equal(11, catalog.IndexOf("Gear"));
        Assert.Equal(
            new[] { "Hammer", "ArmorBronzeChest", "PickaxeAntler", "ShieldWood", "SwordBronze" },
            catalog.Categories[11].Prefabs);
    }

    // GW-U-449 (a)
    [Fact]
    public void EmptyInputParsesToTheFourteenEmptyCategories()
    {
        CategoryParseResult result = CategoryFile.Parse(DefaultCategoryFile.Generate(Array.Empty<CategoryItem>()));

        Assert.Empty(result.Issues);
        Assert.Equal(DefaultCategoryFixtures.Names, result.Catalog.CategoryNames);
        Assert.All(result.Catalog.Categories, category => Assert.Empty(category.Prefabs));
    }

    // GW-U-449 (b)
    [Fact]
    public void AllDefaultPrefabsAsOtherInReverseTableOrderRoundTripToTheFixtureTable()
    {
        var items = new List<CategoryItem>();
        for (int i = DefaultCategoryFixtures.Names.Length - 1; i >= 0; i--)
        {
            foreach (string prefab in DefaultCategoryFixtures.Prefabs[i])
            {
                items.Add(new CategoryItem(prefab, ItemKind.Other));
            }
        }

        CategoryParseResult result = CategoryFile.Parse(DefaultCategoryFile.Generate(items));

        Assert.Empty(result.Issues);
        Assert.Equal(DefaultCategoryFixtures.Names, result.Catalog.CategoryNames);
        Assert.Equal(DefaultCategoryFixtures.TotalCount, result.Catalog.Categories.Sum(category => category.Prefabs.Count));
        for (int i = 0; i < DefaultCategoryFixtures.Names.Length; i++)
        {
            Assert.Equal(DefaultCategoryFixtures.Prefabs[i], result.Catalog.Categories[i].Prefabs);
        }
    }

    // GW-U-449 (c)
    [Theory]
    [MemberData(nameof(EdgeRowIndices))]
    public void EachEdgeRowParsesWithoutIssuesAndResolvesEveryItemToItsLine(int row)
    {
        (CategoryItem[] items, string line) = DefaultCategoryFixtures.EdgeRows()[row];
        string expectedCategory = line.Substring(0, line.IndexOf(':'));

        CategoryParseResult result = CategoryFile.Parse(DefaultCategoryFile.Generate(items));

        Assert.Empty(result.Issues);
        foreach (CategoryItem item in items)
        {
            Assert.Equal(expectedCategory, result.Catalog.Resolve(item.PrefabName, item.Kind));
        }

        int distinct = items.Select(item => item.PrefabName).Distinct(StringComparer.Ordinal).Count();
        Assert.Equal(distinct, result.Catalog.Categories.Sum(category => category.Prefabs.Count));
    }

    // GW-U-450
    [Fact]
    public void CrlfVariantParsesIdenticallyToTheLfText()
    {
        string lf = DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems());
        string crlf = lf.Replace("\n", "\r\n");

        CategoryParseResult fromLf = CategoryFile.Parse(lf);
        CategoryParseResult fromCrlf = CategoryFile.Parse(crlf);

        Assert.Empty(fromCrlf.Issues);
        Assert.Equal(fromLf.Catalog.CategoryNames, fromCrlf.Catalog.CategoryNames);
        for (int i = 0; i < fromLf.Catalog.Categories.Count; i++)
        {
            Assert.Equal(fromLf.Catalog.Categories[i].Name, fromCrlf.Catalog.Categories[i].Name);
            Assert.Equal(fromLf.Catalog.Categories[i].Prefabs, fromCrlf.Catalog.Categories[i].Prefabs);
        }

        Assert.Equal(DefaultCategoryFixtures.Normalise(lf), DefaultCategoryFixtures.Normalise(crlf));
    }
}
