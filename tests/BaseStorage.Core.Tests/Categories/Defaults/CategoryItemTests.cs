namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-4 (contract 2.1-2.3, rule P, R5, R6, R8): the generator input validates its prefab name and has value semantics.</summary>
public sealed class CategoryItemTests
{
    // GW-U-447
    [Fact]
    public void NullPrefabNameIsAnArgumentNullExceptionOnTheParameter()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => { _ = new CategoryItem(null!, ItemKind.Other); });
        Assert.Equal("prefabName", error.ParamName);
    }

    // GW-U-447
    [Theory]
    [InlineData("")]
    [InlineData(" Wood")]
    [InlineData("Wood ")]
    [InlineData("Wood\n")]
    [InlineData("Copper,Tin")]
    [InlineData("Iron\tOre")]
    public void InvalidPrefabNamesAreArgumentExceptionsOnTheParameter(string prefabName)
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() => { _ = new CategoryItem(prefabName, ItemKind.Other); });
        Assert.Equal("prefabName", error.ParamName);
    }

    // GW-U-447
    [Fact]
    public void AnUndefinedKindIsStoredAsGiven()
    {
        Assert.Equal((ItemKind)99, new CategoryItem("CopperOre", (ItemKind)99).Kind);
    }

    // GW-U-447
    [Theory]
    [InlineData("CopperOre")]
    [InlineData("MyMod_HoneyCake")]
    [InlineData("Ores: Iron")]
    [InlineData("#Gem")]
    [InlineData("Fish 1")]
    public void ValidPrefabNamesPassRuleP(string prefabName)
    {
        Assert.True(CategoryItem.IsValidPrefabName(prefabName));
    }

    // GW-U-447
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" Wood")]
    [InlineData("Wood ")]
    [InlineData("Copper,Tin")]
    [InlineData("Iron\tOre")]
    [InlineData("A\r\nB")]
    [InlineData("A\u0085")]
    public void InvalidPrefabNamesFailRuleP(string? prefabName)
    {
        Assert.False(CategoryItem.IsValidPrefabName(prefabName));
    }

    // GW-U-447
    [Fact]
    public void EqualItemsAgreeOnEveryEqualityPath()
    {
        ValueSemantics.AssertEqual(new CategoryItem("CopperOre", ItemKind.Other), new CategoryItem("CopperOre", ItemKind.Other));
    }

    // GW-U-447
    [Fact]
    public void UnequalItemsDisagreeOnEveryEqualityPath()
    {
        ValueSemantics.AssertNotEqual(new CategoryItem("CopperOre", ItemKind.Other), new CategoryItem("copperore", ItemKind.Other));
        ValueSemantics.AssertNotEqual(new CategoryItem("CopperOre", ItemKind.Other), new CategoryItem("CopperOre", ItemKind.Food));
        ValueSemantics.AssertNotEqual(new CategoryItem("Resin", (ItemKind)99), new CategoryItem("Resin", ItemKind.Other));
    }

    // GW-U-447
    [Fact]
    public void NullOnEitherSideFollowsRuleR5()
    {
        CategoryItem item = new CategoryItem("CopperOre", ItemKind.Other);
        CategoryItem? none = null;

        Assert.False(item == null);
        Assert.True(item != null);
        Assert.False(item.Equals(null));
        Assert.True(none == null);
        Assert.False(none != null);
    }
}
