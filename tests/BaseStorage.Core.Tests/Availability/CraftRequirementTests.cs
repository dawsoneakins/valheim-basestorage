namespace BaseStorage.Core.Tests.Availability;

using System;
using BaseStorage.Core.Availability;

/// <summary>AC-3 (contract 9.2, 9.3, R5, R8): ForLevel rows L1-L8, CraftRequirement validation, and value equality.</summary>
public sealed class CraftRequirementTests
{
    // GW-U-422 (AC-3, 9.3 L1-L6): ForLevel gives the vanilla amount, always with QualityMatch.Any and quality 1.
    [Theory]
    [InlineData("Bronze", 8, 4, 1, 1, 8)]
    [InlineData("Bronze", 8, 4, 2, 1, 4)]
    [InlineData("Bronze", 8, 4, 3, 1, 8)]
    [InlineData("Wood", 2, 0, 2, 1, 0)]
    [InlineData("Wood", 2, 0, 1, 5, 10)]
    [InlineData("LeatherScraps", 2, 1, 4, 3, 9)]
    public void ForLevelGivesTheVanillaAmount(string prefab, int amount, int amountPerLevel, int qualityLevel, int craftCount, int expected)
    {
        CraftRequirement requirement = CraftRequirement.ForLevel(prefab, amount, amountPerLevel, qualityLevel, craftCount);

        Assert.Equal(expected, requirement.Amount);
        Assert.Equal(QualityMatch.Any, requirement.QualityMatch);
        Assert.Equal(1, requirement.Quality);
    }

    // GW-U-422 (AC-3, 9.3 L7): a product over int.MaxValue throws ArgumentOutOfRangeException with ParamName "craftCount".
    [Fact]
    public void ForLevelOverflowThrowsForCraftCount()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CraftRequirement.ForLevel("Wood", 2, 0, 1, int.MaxValue));

        Assert.Equal("craftCount", ex.ParamName);
    }

    // GW-U-422 (AC-3, 9.3 L8 and validation): out-of-range arguments throw ArgumentOutOfRangeException with the matching ParamName.
    [Theory]
    [InlineData(-1, 0, 1, 1, "amount")]
    [InlineData(2, -1, 1, 1, "amountPerLevel")]
    [InlineData(2, 0, 0, 1, "qualityLevel")]
    [InlineData(2, 0, 1, 0, "craftCount")]
    public void ForLevelRejectsOutOfRangeArguments(int amount, int amountPerLevel, int qualityLevel, int craftCount, string paramName)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CraftRequirement.ForLevel("Wood", amount, amountPerLevel, qualityLevel, craftCount));

        Assert.Equal(paramName, ex.ParamName);
    }

    // GW-U-422 (AC-3, 9.2 R8): a null prefab gives ArgumentNullException, and a blank prefab gives ArgumentException, both with ParamName "prefabName".
    [Fact]
    public void ForLevelRejectsNullAndBlankPrefab()
    {
        ArgumentNullException nullEx = Assert.Throws<ArgumentNullException>(() => CraftRequirement.ForLevel(null!, 2, 0, 1, 1));
        ArgumentException blankEx = Assert.Throws<ArgumentException>(() => CraftRequirement.ForLevel(" ", 2, 0, 1, 1));

        Assert.Equal("prefabName", nullEx.ParamName);
        Assert.Equal("prefabName", blankEx.ParamName);
    }

    // GW-U-425 (AC-3, 9.2 V5): a negative amount gives ArgumentOutOfRangeException with ParamName "amount".
    [Fact]
    public void NegativeAmountIsRejected()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => new CraftRequirement("Wood", -1));

        Assert.Equal("amount", ex.ParamName);
    }

    // GW-U-425 (AC-3, 9.2 V6): a whitespace prefab gives ArgumentException with ParamName "prefabName".
    [Fact]
    public void WhitespacePrefabIsRejected()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new CraftRequirement(" ", 1));

        Assert.Equal("prefabName", ex.ParamName);
    }

    // GW-U-425 (AC-3, 9.2 R8): a null prefab gives ArgumentNullException with ParamName "prefabName".
    [Fact]
    public void NullPrefabIsRejected()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new CraftRequirement(null!, 1));

        Assert.Equal("prefabName", ex.ParamName);
    }

    // GW-U-425 (AC-3, 9.2 R8): a quality rule that is not defined gives ArgumentOutOfRangeException with ParamName "qualityMatch".
    [Fact]
    public void UndefinedQualityMatchIsRejected()
    {
        ArgumentOutOfRangeException above = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CraftRequirement("Wood", 1, (QualityMatch)3, 1));
        ArgumentOutOfRangeException below = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CraftRequirement("Wood", 1, (QualityMatch)(-1), 1));

        Assert.Equal("qualityMatch", above.ParamName);
        Assert.Equal("qualityMatch", below.ParamName);
    }

    // GW-U-425 (AC-3, 9.2 R8): quality 0 with Exact gives ArgumentOutOfRangeException with ParamName "quality".
    [Fact]
    public void QualityBelowOneIsRejected()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CraftRequirement("Wood", 1, QualityMatch.Exact, 0));

        Assert.Equal("quality", ex.ParamName);
    }

    // GW-U-425 (AC-3, 9.2): a zero amount is valid, with Amount 0.
    [Fact]
    public void ZeroAmountIsValid()
    {
        var requirement = new CraftRequirement("Wood", 0);

        Assert.Equal(0, requirement.Amount);
    }

    // GW-U-425 (AC-3, 9.2 equality): new CraftRequirement("Wood", 10) equals ("Wood", 10, Any, 1), with an equal hash.
    [Fact]
    public void DefaultQualityFormEqualsTheExplicitAnyForm()
    {
        var shortForm = new CraftRequirement("Wood", 10);
        var longForm = new CraftRequirement("Wood", 10, QualityMatch.Any, 1);

        Assert.Equal(longForm, shortForm);
        Assert.Equal(longForm.GetHashCode(), shortForm.GetHashCode());
    }

    // GW-U-425 (AC-3, 9.2 equality): each field difference makes two requirements unequal (prefab case, amount, quality rule, quality).
    [Fact]
    public void EachFieldDifferenceMakesRequirementsUnequal()
    {
        var baseline = new CraftRequirement("Wood", 10, QualityMatch.Exact, 2);

        Assert.NotEqual(baseline, new CraftRequirement("wood", 10, QualityMatch.Exact, 2));
        Assert.NotEqual(baseline, new CraftRequirement("Wood", 11, QualityMatch.Exact, 2));
        Assert.NotEqual(baseline, new CraftRequirement("Wood", 10, QualityMatch.AtLeast, 2));
        Assert.NotEqual(baseline, new CraftRequirement("Wood", 10, QualityMatch.Exact, 3));
        Assert.True(baseline != new CraftRequirement("wood", 10, QualityMatch.Exact, 2));
    }

    // GW-U-425 (AC-3, R5): == and != are null-safe on both sides, and Equals(null) is false.
    [Fact]
    public void EqualityOperatorsAreNullSafeOnBothSides()
    {
        CraftRequirement? none = null;
        CraftRequirement? alsoNone = null;
        CraftRequirement value = new CraftRequirement("Wood", 10);

        Assert.True(none == alsoNone);
        Assert.False(none != alsoNone);
        Assert.False(value == none);
        Assert.False(none == value);
        Assert.True(value != none);
        Assert.False(value.Equals((CraftRequirement?)null));
    }
}
