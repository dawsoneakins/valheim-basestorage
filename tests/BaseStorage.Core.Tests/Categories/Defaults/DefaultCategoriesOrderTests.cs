namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories.Defaults;

/// <summary>AC-2 (contract D1, 3.1): all 14 categories are present, in spec order.</summary>
public sealed class DefaultCategoriesOrderTests
{
    private static readonly string[] SpecOrder =
    {
        "Metals & Ores", "Wood", "Stone & Minerals", "Hides & Leather", "Fibres & Cloth", "Raw Food", "Cooked Food",
        "Meads & Potions", "Seeds & Saplings", "Monster Drops", "Trophies", "Gear", "Ammo", "Misc",
    };

    public static IEnumerable<object[]> Indices() => Enumerable.Range(0, 14).Select(i => new object[] { i });

    // GW-U-439
    [Fact]
    public void NamesHasFourteenEntriesInSpecOrder()
    {
        Assert.Equal(14, DefaultCategories.Names.Count);
        Assert.Equal(SpecOrder, DefaultCategories.Names);
    }

    // GW-U-439
    [Theory]
    [MemberData(nameof(Indices))]
    public void EachNameIsItsSpecPositionAndTheCatalogIndexOfItIsThatPosition(int i)
    {
        Assert.Equal(SpecOrder[i], DefaultCategories.Names[i]);
        Assert.Equal(i, DefaultCategories.Catalog.IndexOf(SpecOrder[i]));
    }

    // GW-U-439
    [Fact]
    public void NamesAreDistinctIgnoringCase()
    {
        Assert.Equal(14, DefaultCategories.Names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    // GW-U-439
    [Fact]
    public void FirstSeventhAndLastNamesAreTheSpecNames()
    {
        Assert.Equal("Metals & Ores", DefaultCategories.Names[0]);
        Assert.Equal("Cooked Food", DefaultCategories.Names[6]);
        Assert.Equal("Misc", DefaultCategories.Names[13]);
    }
}
