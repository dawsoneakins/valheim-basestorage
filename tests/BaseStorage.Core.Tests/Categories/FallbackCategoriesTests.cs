namespace BaseStorage.Core.Tests.Categories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Categories;

/// <summary>AC-3 (contract 2.1, 2.2): the fallback names by item kind, the five public constants and the item kind values.</summary>
public sealed class FallbackCategoriesTests
{
    // GW-U-151
    [Theory]
    [InlineData(ItemKind.Food, "Cooked Food")]
    [InlineData(ItemKind.Trophy, "Trophies")]
    [InlineData(ItemKind.Weapon, "Gear")]
    [InlineData(ItemKind.Armour, "Gear")]
    [InlineData(ItemKind.Tool, "Gear")]
    [InlineData(ItemKind.Shield, "Gear")]
    [InlineData(ItemKind.Ammo, "Ammo")]
    [InlineData(ItemKind.Other, "Misc")]
    [InlineData((ItemKind)99, "Misc")]
    [InlineData((ItemKind)(-1), "Misc")]
    public void ForGivesTheContractFallbackName(ItemKind kind, string expected)
    {
        Assert.Equal(expected, FallbackCategories.For(kind));
    }

    // GW-U-151
    [Fact]
    public void PublicConstantsAreExactlyTheFiveShippedNames()
    {
        Dictionary<string, string> constants = typeof(FallbackCategories)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .ToDictionary(field => field.Name, field => (string)field.GetRawConstantValue()!, StringComparer.Ordinal);

        Assert.Equal(new[] { "Ammo", "CookedFood", "Gear", "Misc", "Trophies" }, constants.Keys.OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal("Cooked Food", constants["CookedFood"]);
        Assert.Equal("Trophies", constants["Trophies"]);
        Assert.Equal("Gear", constants["Gear"]);
        Assert.Equal("Ammo", constants["Ammo"]);
        Assert.Equal("Misc", constants["Misc"]);
    }

    // GW-U-151
    [Theory]
    [InlineData(ItemKind.Other, 0)]
    [InlineData(ItemKind.Food, 1)]
    [InlineData(ItemKind.Trophy, 2)]
    [InlineData(ItemKind.Weapon, 3)]
    [InlineData(ItemKind.Armour, 4)]
    [InlineData(ItemKind.Tool, 5)]
    [InlineData(ItemKind.Shield, 6)]
    [InlineData(ItemKind.Ammo, 7)]
    public void ItemKindValuesAreTheContractedIntegers(ItemKind kind, int value)
    {
        Assert.Equal(value, (int)kind);
    }

    // GW-U-151
    [Fact]
    public void DefaultItemKindIsOtherAndTheUnderlyingTypeIsInt()
    {
        Assert.Equal(ItemKind.Other, default(ItemKind));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(ItemKind)));
    }
}
