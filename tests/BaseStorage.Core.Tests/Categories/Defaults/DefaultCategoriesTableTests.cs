namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;

/// <summary>AC-1 (contract D1, D2, D4, D6, 3.0-3.2): the exact default table and the guards of its accessors.</summary>
public sealed class DefaultCategoriesTableTests
{
    public static IEnumerable<object[]> CategoryIndices() => Enumerable.Range(0, 14).Select(i => new object[] { i });

    // GW-U-436
    [Theory]
    [MemberData(nameof(CategoryIndices))]
    public void GetPrefabsIsTheFixtureListInTableOrderWithThePinnedCount(int index)
    {
        IReadOnlyList<string> prefabs = DefaultCategories.GetPrefabs(DefaultCategoryFixtures.Names[index]);

        Assert.Equal(DefaultCategoryFixtures.Prefabs[index], prefabs);
        Assert.Equal(DefaultCategoryFixtures.Counts[index], prefabs.Count);
    }

    // GW-U-436
    [Fact]
    public void EveryCategoryHasAtLeastOneDefaultPrefab()
    {
        foreach (string name in DefaultCategoryFixtures.Names)
        {
            Assert.True(DefaultCategories.GetPrefabs(name).Count >= 1, $"'{name}' has no default prefab.");
        }
    }

    // GW-U-436
    [Fact]
    public void FibresAndMetalsSamplesMatchTheContract()
    {
        Assert.Equal(new[] { "Flax", "LinenThread", "JuteRed", "JuteBlue" }, DefaultCategories.GetPrefabs("Fibres & Cloth"));
        Assert.Equal(new[] { "CopperOre", "Copper", "TinOre", "Tin" }, DefaultCategories.GetPrefabs("Metals & Ores").Take(4));
    }

    // GW-U-437
    [Fact]
    public void GetPrefabsRejectsNullWithTheCategoryNameParameter()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => DefaultCategories.GetPrefabs(null!));
        Assert.Equal("categoryName", error.ParamName);
    }

    // GW-U-437
    [Theory]
    [InlineData("wood")]
    [InlineData("")]
    [InlineData("Metals & Ores ")]
    [InlineData("Fish")]
    public void GetPrefabsRejectsUnknownNamesWithTheCategoryNameParameter(string categoryName)
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() => DefaultCategories.GetPrefabs(categoryName));
        Assert.Equal("categoryName", error.ParamName);
    }

    // GW-U-437
    [Fact]
    public void NamesCannotBeWrittenThrough()
    {
        var names = (IList<string>)DefaultCategories.Names;

        Assert.Throws<NotSupportedException>(() => names.Add("Fish"));
        Assert.Throws<NotSupportedException>(() => { names[0] = "Fish"; });
        Assert.Throws<NotSupportedException>(() => names.Clear());
        Assert.Equal(14, DefaultCategories.Names.Count);
        Assert.Equal("Metals & Ores", DefaultCategories.Names[0]);
    }

    // GW-U-437
    [Fact]
    public void PrefabListsCannotBeWrittenThrough()
    {
        var prefabs = (IList<string>)DefaultCategories.GetPrefabs("Wood");
        int before = prefabs.Count;

        Assert.Throws<NotSupportedException>(() => prefabs.Add("Fish"));
        Assert.Throws<NotSupportedException>(() => { prefabs[0] = "Fish"; });
        Assert.Throws<NotSupportedException>(() => prefabs.Clear());
        Assert.Equal(before, DefaultCategories.GetPrefabs("Wood").Count);
        Assert.Equal(DefaultCategoryFixtures.Prefabs[1], DefaultCategories.GetPrefabs("Wood"));
    }

    // GW-U-437 (D2)
    [Fact]
    public void FallbackNamesAreInNamesAndTheFallbackConstantsMatchThem()
    {
        foreach (string literal in new[] { "Cooked Food", "Trophies", "Gear", "Ammo", "Misc" })
        {
            Assert.Contains(literal, DefaultCategories.Names);
        }

        Assert.Equal("Cooked Food", FallbackCategories.CookedFood);
        Assert.Equal("Trophies", FallbackCategories.Trophies);
        Assert.Equal("Gear", FallbackCategories.Gear);
        Assert.Equal("Ammo", FallbackCategories.Ammo);
        Assert.Equal("Misc", FallbackCategories.Misc);
    }

    // GW-U-437 (D4)
    [Fact]
    public void EveryDefaultPrefabIsAValidPrefabName()
    {
        foreach (string name in DefaultCategoryFixtures.Names)
        {
            foreach (string prefab in DefaultCategories.GetPrefabs(name))
            {
                Assert.True(CategoryItem.IsValidPrefabName(prefab), $"'{prefab}' in '{name}' violates rule P.");
            }
        }
    }
}
