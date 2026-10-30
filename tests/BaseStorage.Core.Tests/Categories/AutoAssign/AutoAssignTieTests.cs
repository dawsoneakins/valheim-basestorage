namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-2 (A6; contract 5.2 row 104, 5.3 and 5.4 tie rows): ties go to the catalog-first category, whatever the order.</summary>
public sealed class AutoAssignTieTests
{
    // GW-U-505
    [Fact]
    public void TieInChest104GoesToTheCatalogFirstCategoryForEveryContentOrder()
    {
        string[] prefabs = { "Stone", "Flint", "Wood", "FineWood" };

        foreach (string[] order in Permutations(prefabs))
        {
            ContainerSnapshot chest = C(104, 6, 0, order.Select(prefab => S(prefab, 10)).ToArray());

            AutoAssignResult r = ResolveWith(new[] { chest });

            AssertChest("order " + string.Join(",", order), AutoAssignOutcome.Assigned, "Wood", At(r, 104));
        }
    }

    // GW-U-506
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void TieRankingPicksTheBestRankedCategory(int row)
    {
        var (label, threshold, catalog, codes, expected) = Row(row);

        AutoAssignResult r = ResolveWith(new[] { C(201, 0, 0, Stacks(codes)) }, catalog: catalog, threshold: threshold);

        AssertChest(label, AutoAssignOutcome.Assigned, expected, At(r, 201));
    }

    private static (string Label, int Threshold, CategoryCatalog Catalog, string Codes, string Expected) Row(int row) => row switch
    {
        0 => ("50 St,W on the 5.1 catalog", 50, Catalog(), "St,W", "Wood"),
        1 => ("50 T,F,F,T on the 5.1 catalog", 50, Catalog(), "T,F,F,T", "Cooked Food"),
        2 => ("33 M,W,St on the 5.1 catalog", 33, Catalog(), "M,W,St", "Metals & Ores"),
        3 => ("50 SwordIron,Resin on the empty catalog", 50, CategoryCatalog.Empty, "SwordIron,Resin", "Gear"),
        4 => ("50 Resin,SwordIron on the empty catalog", 50, CategoryCatalog.Empty, "Resin,SwordIron", "Gear"),
        5 => ("50 ArrowWood,Wood on the Wood-only catalog", 50, WoodOnly(), "ArrowWood,Wood", "Wood"),
        6 => ("50 Wood,ArrowWood on the Wood-only catalog", 50, WoodOnly(), "Wood,ArrowWood", "Wood"),
        _ => ("50 SwordIron,Resin on the 5.1 catalog", 50, Catalog(), "SwordIron,Resin", "Gear"),
    };

    /// <summary>The catalog of the single line "Wood: Wood", parsed from text.</summary>
    private static CategoryCatalog WoodOnly() => CategoryFile.Parse("Wood: Wood\n").Catalog;

    /// <summary>Every ordering of the items, each as a new array.</summary>
    private static IEnumerable<string[]> Permutations(string[] items)
    {
        if (items.Length == 0)
        {
            yield return Array.Empty<string>();
            yield break;
        }

        for (int i = 0; i < items.Length; i++)
        {
            string[] rest = items.Where((_, index) => index != i).ToArray();
            foreach (string[] tail in Permutations(rest))
            {
                yield return new[] { items[i] }.Concat(tail).ToArray();
            }
        }
    }
}
