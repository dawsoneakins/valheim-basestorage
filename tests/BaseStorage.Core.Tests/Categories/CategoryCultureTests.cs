namespace BaseStorage.Core.Tests.Categories;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;

/// <summary>
/// AC-1, AC-2 and AC-3 (contract C2, spec NFR): parsing and resolving give the same results under InvariantCulture, en-US,
/// de-DE, tr-TR and ar-SA. The culture is set for the current thread in try/finally (ADR-GWIN-019).
/// </summary>
public sealed class CategoryCultureTests
{
    // GW-U-156 (AC-1)
    [Theory]
    [MemberData(nameof(CultureRows))]
    public void ParsingFGivesTheContractCatalogUnderEveryCulture(string cultureName)
    {
        CategoryFixtures.WithCulture(cultureName, () =>
        {
            CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

            Assert.Equal(CategoryFixtures.FNames, catalog.CategoryNames);
            for (int i = 0; i < CategoryFixtures.FPrefabs.Length; i++)
            {
                Assert.Equal(CategoryFixtures.FPrefabs[i], catalog.Categories[i].Prefabs);
            }

            Assert.Equal("Metals & Ores", catalog.GetMappedCategory("TinOre"));
            Assert.Null(catalog.GetMappedCategory("copperore"));
        });
    }

    // GW-U-156 (AC-2)
    [Theory]
    [MemberData(nameof(CultureRows))]
    public void IssuesAreIdenticalUnderEveryCulture(string cultureName)
    {
        CategoryFixtures.WithCulture(cultureName, () =>
        {
            Assert.Equal(CategoryFixtures.FIssues(), CategoryFile.Parse(CategoryFixtures.F).Issues);
        });
    }

    // GW-U-156 (AC-2)
    [Theory]
    [MemberData(nameof(CultureRows))]
    public void CaseOnlyDuplicatesAreDetectedUnderEveryCulture(string cultureName)
    {
        CategoryFixtures.WithCulture(cultureName, () =>
        {
            CategoryParseResult result = CategoryFile.Parse("misc: Iron\nMISC: Ingot");

            CategoryParseIssue issue = Assert.Single(result.Issues);
            Assert.Equal(new CategoryParseIssue(2, CategoryParseIssueKind.DuplicateCategory, "MISC: Ingot", "MISC", null, "misc"), issue);
            Assert.Equal(LogSeverity.Warning, issue.Severity);

            CategoryDefinition merged = Assert.Single(CategoryFile.Parse("ITEM: a\nitem: b").Catalog.Categories);
            Assert.Equal("ITEM", merged.Name);
            Assert.Equal(new[] { "a", "b" }, merged.Prefabs);
        });
    }

    // GW-U-156 (AC-3)
    [Theory]
    [MemberData(nameof(CultureRows))]
    public void ResolverGivesTheContractCategoriesUnderEveryCulture(string cultureName)
    {
        CategoryFixtures.WithCulture(cultureName, () =>
        {
            CategoryCatalog catalog = CategoryFile.Parse(CategoryFixtures.F).Catalog;

            foreach (var (prefab, kind, expected) in CategoryFixtures.ResolverRows)
            {
                Assert.Equal(expected, catalog.Resolve(prefab, kind));
            }

            Assert.Equal("MISC", CategoryFile.Parse("MISC:").Catalog.Resolve("x", ItemKind.Other));
        });
    }

    /// <summary>The culture names of the NFR check as xunit member data.</summary>
    public static IEnumerable<object[]> CultureRows() => CategoryFixtures.CultureNames.Select(name => new object[] { name });
}
