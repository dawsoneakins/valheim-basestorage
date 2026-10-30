namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;

/// <summary>AC-3 (contract D3, D5, 3.2): no default prefab is listed twice, and duplicates cannot be silently repaired.</summary>
public sealed class DefaultCategoriesNoDuplicateTests
{
    // GW-U-440
    [Fact]
    public void NoPrefabIsListedTwiceAcrossTheFourteenLists()
    {
        var all = new List<string>();
        foreach (string name in DefaultCategoryFixtures.Names)
        {
            all.AddRange(DefaultCategories.GetPrefabs(name));
        }

        Assert.Equal(DefaultCategoryFixtures.TotalCount, all.Count);

        List<string> duplicates = all.GroupBy(prefab => prefab, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key + " in " + string.Join(", ", DefaultCategoryFixtures.Names.Where(
                name => DefaultCategories.GetPrefabs(name).Contains(group.Key, StringComparer.Ordinal))))
            .ToList();
        Assert.True(duplicates.Count == 0, "Duplicated default prefabs: " + string.Join("; ", duplicates));

        Assert.Equal(DefaultCategoryFixtures.TotalCount, all.Distinct(StringComparer.Ordinal).Count());
        foreach (string name in DefaultCategoryFixtures.Names)
        {
            IReadOnlyList<string> list = DefaultCategories.GetPrefabs(name);
            Assert.Equal(list.Count, list.Distinct(StringComparer.Ordinal).Count());
        }
    }

    // GW-U-441
    [Fact]
    public void CatalogPrefabTotalIsTheFixtureTotal()
    {
        Assert.Equal(DefaultCategoryFixtures.TotalCount, DefaultCategories.Catalog.Categories.Sum(category => category.Prefabs.Count));
    }

    // GW-U-441
    [Fact]
    public void ParsingTheFourteenTableLinesGivesNoIssuesAndTheSameCatalog()
    {
        CategoryParseResult result = CategoryFile.Parse(DefaultCategoryFixtures.TableLines());

        Assert.Empty(result.Issues);
        Assert.Equal(DefaultCategoryFixtures.Names, result.Catalog.CategoryNames);
        Assert.Equal(DefaultCategories.Catalog.CategoryNames, result.Catalog.CategoryNames);
        for (int i = 0; i < DefaultCategoryFixtures.Names.Length; i++)
        {
            Assert.Equal(DefaultCategories.Catalog.Categories[i].Name, result.Catalog.Categories[i].Name);
            Assert.Equal(DefaultCategories.Catalog.Categories[i].Prefabs, result.Catalog.Categories[i].Prefabs);
        }
    }
}
