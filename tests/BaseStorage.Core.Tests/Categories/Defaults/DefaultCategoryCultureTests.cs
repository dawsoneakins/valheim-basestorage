namespace BaseStorage.Core.Tests.Categories.Defaults;

using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;

/// <summary>AC-4 (contract C2, G6, ADR-GWIN-029): the generator output is the same under every culture the repo tests.</summary>
public sealed class DefaultCategoryCultureTests
{
    public static IEnumerable<object[]> Cultures()
    {
        yield return new object[] { string.Empty };
        yield return new object[] { "en-US" };
        yield return new object[] { "de-DE" };
        yield return new object[] { "tr-TR" };
        yield return new object[] { "ar-SA" };
    }

    // GW-U-455
    [Theory]
    [MemberData(nameof(Cultures))]
    public void GenerateOfSAndTheEdgeRowsIsIdenticalToTheInvariantResult(string cultureName)
    {
        string invariantS = string.Empty;
        var invariantEdges = new List<string>();
        CategoryFixtures.WithCulture(string.Empty, () =>
        {
            invariantS = DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems());
            invariantEdges = GenerateEdges();
        });

        CategoryFixtures.WithCulture(cultureName, () =>
        {
            Assert.Equal(invariantS, DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems()));
            Assert.Equal(invariantEdges, GenerateEdges());
        });
    }

    // GW-U-455
    [Fact]
    public void TurkishCultureKeepsOrdinalCaseRules()
    {
        CategoryFixtures.WithCulture("tr-TR", () =>
        {
            CategoryItem[] items =
            {
                new CategoryItem("copperore", ItemKind.Other),
                new CategoryItem("CopperOre", ItemKind.Other),
            };

            List<(string Category, string Token)> tokens = DefaultCategoryFixtures.ParseTokens(DefaultCategoryFile.Generate(items));

            Assert.Contains(("Misc", "copperore"), tokens);
            Assert.Contains(("Metals & Ores", "CopperOre"), tokens);
        });
    }

    private static List<string> GenerateEdges()
    {
        var generated = new List<string>();
        foreach ((CategoryItem[] items, string _) in DefaultCategoryFixtures.EdgeRows())
        {
            generated.Add(DefaultCategoryFile.Generate(items));
        }

        return generated;
    }
}
