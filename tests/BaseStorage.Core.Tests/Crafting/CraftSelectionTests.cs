namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1100 (AC-2, contract 3.2): the craft selection value, its argument checks and its reference equality.</summary>
public sealed class CraftSelectionTests
{
    private static readonly object Recipe = new object();
    private static readonly object Station = new object();

    // GW-U-1100 (3.2 row 1): equal selections are equal and hash equally.
    [Fact]
    public void EqualSelectionsAreEqualAndHashEqually()
    {
        var a = new CraftSelection(Recipe, null, Station, 1, 1);
        var b = new CraftSelection(Recipe, null, Station, 1, 1);

        Assert.True(a == b);
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // GW-U-1100 (3.2 row 2): a multiplier of 5 on one side differs.
    [Fact]
    public void DifferentMultiplierIsNotEqual()
    {
        var a = new CraftSelection(Recipe, null, Station, 1, 1);
        var b = new CraftSelection(Recipe, null, Station, 1, 5);

        Assert.True(a != b);
    }

    // GW-U-1100 (3.2 row 3): an upgrade item against no upgrade item differs.
    [Fact]
    public void UpgradeItemVersusNullIsNotEqual()
    {
        var upgrade = new object();
        var a = new CraftSelection(Recipe, upgrade, Station, 3, 1);
        var b = new CraftSelection(Recipe, null, Station, 3, 1);

        Assert.True(a != b);
    }

    // GW-U-1100 (3.2 row 4): a null recipe throws by name.
    [Fact]
    public void NullRecipeThrowsByName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new CraftSelection(null!, null, Station, 1, 1));
        Assert.Equal("recipe", ex.ParamName);
    }

    // GW-U-1100 (3.2 row 5): a quality level below 1 throws by name.
    [Fact]
    public void QualityBelowOneThrowsByName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => new CraftSelection(Recipe, null, Station, 0, 1));
        Assert.Equal("qualityLevel", ex.ParamName);
    }

    // GW-U-1100: a null station throws by name.
    [Fact]
    public void NullStationThrowsByName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new CraftSelection(Recipe, null, null!, 1, 1));
        Assert.Equal("station", ex.ParamName);
    }

    // GW-U-1100: a multiplier below 1 throws by name.
    [Fact]
    public void MultiplierBelowOneThrowsByName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => new CraftSelection(Recipe, null, Station, 1, 0));
        Assert.Equal("multiplier", ex.ParamName);
    }

    // GW-U-1100 (3.2 equality rule): equality is by reference, so an object whose Equals always returns true does not make two selections equal.
    [Fact]
    public void AnEqualsOverrideDoesNotMakeSelectionsEqual()
    {
        var first = new AlwaysEqual();
        var second = new AlwaysEqual();
        var a = new CraftSelection(first, null, Station, 1, 1);
        var b = new CraftSelection(second, null, Station, 1, 1);

        Assert.False(a == b);
        Assert.False(a.Equals(b));
    }

    private sealed class AlwaysEqual
    {
        public override bool Equals(object? obj) => true;

        public override int GetHashCode() => 0;
    }
}
