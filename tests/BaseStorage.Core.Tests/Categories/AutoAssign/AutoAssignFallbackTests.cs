namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-1 (contract 5.4, non-tie rows): the category of each stack comes from the catalog resolver with kind fallback.</summary>
public sealed class AutoAssignFallbackTests
{
    // GW-U-501
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void StackCategoryComesFromTheCatalogResolverWithKindFallback(int row)
    {
        var (label, catalog, kinds, prefabs, expected) = Row(row);
        ContainerSnapshot chest = C(201, 0, 0, prefabs.Select(prefab => S(prefab, 1)).ToArray());

        AutoAssignResult r = ResolveWith(new[] { chest }, catalog: catalog, kinds: kinds);

        AssertChest(label, AutoAssignOutcome.Assigned, expected, At(r, 201));
    }

    private static (string Label, CategoryCatalog Catalog, Dictionary<string, ItemKind> Kinds, string[] Prefabs, string Expected) Row(int row) =>
        row switch
        {
            0 => ("empty catalog: SwordIron, AxeBronze, Resin", CategoryCatalog.Empty, Kinds(), new[] { "SwordIron", "AxeBronze", "Resin" }, "Gear"),
            1 => ("5.1 catalog: SerpentStew x2, CopperOre", Catalog(), Kinds(), new[] { "SerpentStew", "SerpentStew", "CopperOre" }, "Cooked Food"),
            2 => ("5.1 catalog: UnknownThing x2, Wood", Catalog(), Kinds(), new[] { "UnknownThing", "UnknownThing", "Wood" }, "Misc"),
            3 => ("5.1 catalog: CopperOre with kind Trophy", Catalog(), KindsWith("CopperOre", ItemKind.Trophy), new[] { "CopperOre" }, "Metals & Ores"),
            4 => ("5.1 catalog: copperore (lower case)", Catalog(), Kinds(), new[] { "copperore" }, "Misc"),
            _ => ("5.1 catalog: Resin with kind 99", Catalog(), KindsWith("Resin", (ItemKind)99), new[] { "Resin" }, "Misc"),
        };
}
