namespace BaseStorage.Core.Tests.Snapshots;

using System;

/// <summary>AC-3 and AC-5: ItemStackSnapshot construction, validation and value equality.</summary>
public sealed class ItemStackSnapshotTests
{
    private const string TypeName = "ItemStackSnapshot";

    // GW-U-028
    [Fact]
    public void ExposesPrefabNameCountQualityAndMaxStack()
    {
        dynamic stack = Fixtures.StackOf("CopperOre", 30, 1, 30);

        Assert.Equal("CopperOre", (string)stack.PrefabName);
        Assert.Equal(30, (int)stack.Count);
        Assert.Equal(1, (int)stack.Quality);
        Assert.Equal(30, (int)stack.MaxStack);
    }

    // GW-U-028
    [Theory]
    [InlineData("SwordIron", 1, 3, 1)]
    [InlineData("Wood", 75, 1, 50)]
    public void AcceptsAnUpgradedStackAndAnOverFullStack(string prefabName, int count, int quality, int maxStack)
    {
        dynamic stack = Fixtures.StackOf(prefabName, count, quality, maxStack);

        Assert.Equal(prefabName, (string)stack.PrefabName);
        Assert.Equal(count, (int)stack.Count);
        Assert.Equal(quality, (int)stack.Quality);
        Assert.Equal(maxStack, (int)stack.MaxStack);
    }

    // GW-U-028
    [Fact]
    public void NullPrefabNameIsArgumentNull()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            Fixtures.StackOf(null, 1, 1, 50);
        });

        Assert.Equal("prefabName", ex.ParamName);
    }

    // GW-U-028
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespacePrefabNameIsArgumentException(string prefabName)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            Fixtures.StackOf(prefabName, 1, 1, 50);
        });

        Assert.Equal("prefabName", ex.ParamName);
    }

    // GW-U-028
    [Theory]
    [InlineData("count", 0, 1, 50)]
    [InlineData("count", -1, 1, 50)]
    [InlineData("quality", 17, 0, 50)]
    [InlineData("maxStack", 17, 1, 0)]
    public void ZeroOrNegativeNumericFieldIsOutOfRangeForItsParameter(string paramName, int count, int quality, int maxStack)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Fixtures.StackOf("Wood", count, quality, maxStack);
        });

        Assert.Equal(paramName, ex.ParamName);
    }

    // GW-U-034
    [Fact]
    public void EqualStacksAreEqualWithEqualHashCodes()
    {
        ValueSemantics.AssertEqual(Fixtures.StackOf("CopperOre", 30, 1, 30), Fixtures.StackOf("CopperOre", 30, 1, 30));
    }

    // GW-U-034
    [Theory]
    [InlineData("TinOre", 30, 1, 30)]
    [InlineData("copperore", 30, 1, 30)]
    [InlineData("CopperOre", 29, 1, 30)]
    [InlineData("CopperOre", 30, 2, 30)]
    [InlineData("CopperOre", 30, 1, 50)]
    public void AnyDifferingFieldIsUnequal(string prefabName, int count, int quality, int maxStack)
    {
        ValueSemantics.AssertNotEqual(
            Fixtures.StackOf("CopperOre", 30, 1, 30),
            Fixtures.StackOf(prefabName, count, quality, maxStack));
    }

    // GW-U-034
    [Fact]
    public void NullHandlingFollowsClassEquality()
    {
        ValueSemantics.AssertNullHandling(ContractTypes.Snapshot(TypeName), Fixtures.StackOf("CopperOre", 30, 1, 30));
    }
}
