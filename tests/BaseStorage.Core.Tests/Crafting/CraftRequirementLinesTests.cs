namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Core.Availability;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1101 (AC-2, contract 3.3): the recipe lines to the requirement list, the skip rules and the argument checks.</summary>
public sealed class CraftRequirementLinesTests
{
    private static RecipeLine Line(string? prefab, int amount, bool upgrader = false) => new RecipeLine(prefab, amount, upgrader);

    private static string[]? Describe(IReadOnlyList<CraftRequirement>? result) =>
        result?.Select(requirement => requirement.PrefabName + " " + requirement.Amount).ToArray();

    // GW-U-1101 (3.3 table row 1): two lines at multiplier 1.
    [Fact]
    public void TwoLinesAtMultiplierOneKeepTheirAmounts()
    {
        var result = CraftRequirementLines.Build(new[] { Line("Wood", 10), Line("Resin", 4) }, 1);

        Assert.Equal(new[] { "Wood 10", "Resin 4" }, Describe(result));
    }

    // GW-U-1101 (3.3 table row 2): the same lines at multiplier 5.
    [Fact]
    public void TwoLinesAtMultiplierFiveAreScaled()
    {
        var result = CraftRequirementLines.Build(new[] { Line("Wood", 10), Line("Resin", 4) }, 5);

        Assert.Equal(new[] { "Wood 50", "Resin 20" }, Describe(result));
    }

    // GW-U-1101 (3.3 table row 3): null prefabs, upgrader lines and zero lines are skipped.
    [Fact]
    public void NullUpgraderAndZeroLinesAreSkipped()
    {
        var result = CraftRequirementLines.Build(new[] { Line("Wood", 10), Line(null, 3), Line("Bronze", 2, true), Line("Resin", 0) }, 1);

        Assert.Equal(new[] { "Wood 10" }, Describe(result));
    }

    // GW-U-1101 (3.3 table row 4): the level 3 upgrade lines keep the amounts vanilla already computed.
    [Fact]
    public void UpgradeLinesKeepTheVanillaAmounts()
    {
        var result = CraftRequirementLines.Build(new[] { Line("FineWood", 6), Line("Iron", 15) }, 1);

        Assert.Equal(new[] { "FineWood 6", "Iron 15" }, Describe(result));
    }

    // GW-U-1101 (3.3 table row 5): a product above int.MaxValue makes the whole result null.
    [Fact]
    public void AProductAboveIntMaxValueGivesNull()
    {
        var result = CraftRequirementLines.Build(new[] { Line("Wood", int.MaxValue) }, 2);

        Assert.Null(result);
    }

    // GW-U-1101 (3.3 table row 6): an empty list gives an empty result.
    [Fact]
    public void AnEmptyListGivesAnEmptyResult()
    {
        var result = CraftRequirementLines.Build(Array.Empty<RecipeLine>(), 1);

        Assert.Empty(result!);
    }

    // GW-U-1101: duplicate prefabs stay separate entries.
    [Fact]
    public void DuplicatePrefabsStaySeparate()
    {
        var result = CraftRequirementLines.Build(new[] { Line("Wood", 3), Line("Wood", 4) }, 1);

        Assert.Equal(new[] { "Wood 3", "Wood 4" }, Describe(result));
    }

    // GW-U-1101: a whitespace prefab is skipped.
    [Fact]
    public void AWhitespacePrefabIsSkipped()
    {
        var result = CraftRequirementLines.Build(new[] { Line("   ", 5), Line("Wood", 2) }, 1);

        Assert.Equal(new[] { "Wood 2" }, Describe(result));
    }

    // GW-U-1101: a null lines argument throws by name, and a multiplier of 0 throws by name.
    [Fact]
    public void NullLinesAndZeroMultiplierThrowByName()
    {
        ArgumentNullException nullLines = Assert.Throws<ArgumentNullException>(() => CraftRequirementLines.Build(null!, 1));
        Assert.Equal("lines", nullLines.ParamName);

        ArgumentOutOfRangeException zero = Assert.Throws<ArgumentOutOfRangeException>(() => CraftRequirementLines.Build(new[] { Line("Wood", 1) }, 0));
        Assert.Equal("multiplier", zero.ParamName);
    }

    // GW-U-1101: the result is read-only.
    [Fact]
    public void TheResultIsReadOnly()
    {
        var result = CraftRequirementLines.Build(new[] { Line("Wood", 10) }, 1);

        Assert.NotNull(result);
        Assert.False(result is IList<CraftRequirement>);
    }
}
