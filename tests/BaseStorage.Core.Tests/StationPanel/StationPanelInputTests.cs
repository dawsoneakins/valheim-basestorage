namespace BaseStorage.Core.Tests.StationPanel;

using System;
using BaseStorage.Core.StationPanel;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-1, AC-2, AC-4 (contract 3.1, 3.2, 5.5): input validation in parameter order, OrganizeRunning, count rules and value semantics.</summary>
public sealed class StationPanelInputTests
{
    // GW-U-536 (AC-1, contract 3.2, 5.5): screen and scale validation, with the exact exception type and ParamName.
    [Theory]
    [InlineData(0, 1080, 1f, "screenWidth")]
    [InlineData(-1, 1080, 1f, "screenWidth")]
    [InlineData(1920, 0, 1f, "screenHeight")]
    [InlineData(1920, 1080, 0f, "uiScale")]
    [InlineData(1920, 1080, -1f, "uiScale")]
    [InlineData(1920, 1080, float.NaN, "uiScale")]
    [InlineData(1920, 1080, float.PositiveInfinity, "uiScale")]
    [InlineData(1920, 1080, float.NegativeInfinity, "uiScale")]
    [InlineData(0, 0, 0f, "screenWidth")]
    [InlineData(1920, 0, 0f, "screenHeight")]
    public void InvalidScreenOrScaleThrowsWithTheParamNameInParameterOrder(int width, int height, float scale, string paramName)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => StationPanelFixtures.AIn(width, height, scale));
        Assert.Equal(paramName, ex.ParamName);
    }

    [Fact]
    public void OutOfRangeButValidScreenAndScaleIsAccepted()
    {
        Assert.Null(Record.Exception(() => StationPanelFixtures.AIn(1024, 768, 1.5f)));
    }

    [Fact]
    public void ComputeRejectsANullInputWithTheInputParamName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => StationPanelLayout.Compute(null!));
        Assert.Equal("input", ex.ParamName);
    }

    // GW-U-537 (AC-2, contract 3.2, 5.5): a blank holder name is rejected with the exact type ArgumentException, not a subclass.
    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    [InlineData("\t\n")]
    public void BlankHolderNameThrowsArgumentExceptionExactly(string holder)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => StationPanelFixtures.AIn(1920, 1080, 1f, holder: holder));
        Assert.Equal("organizeHolderName", ex.ParamName);
    }

    // GW-U-537 (AC-2, contract 3.1): OrganizeRunning is derived from the holder name, and null means no Organize or Deposit runs.
    [Fact]
    public void OrganizeRunningIsTrueForAHolderAndFalseForNull()
    {
        Assert.True(StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik").OrganizeRunning);
        Assert.False(StationPanelFixtures.AIn(1920, 1080, 1f).OrganizeRunning);
    }

    // GW-U-537 (AC-2, contract 3.2): with an invalid holder and a negative station count, the count is reported first.
    [Fact]
    public void StationCountIsCheckedBeforeTheHolderName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => StationPanelFixtures.AIn(1920, 1080, 1f, stations: -1, holder: " "));
        Assert.Equal("stationCount", ex.ParamName);
    }

    // GW-U-540 (AC-3, contract 3.2, 5.5): negative counts are rejected by name, and zero is accepted for both.
    [Fact]
    public void NegativeStationCountIsRejected()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => StationPanelFixtures.AIn(1920, 1080, 1f, stations: -1));
        Assert.Equal("stationCount", ex.ParamName);
    }

    [Fact]
    public void NegativeEligibleChestCountIsRejected()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => StationPanelFixtures.AIn(1920, 1080, 1f, chests: -1));
        Assert.Equal("eligibleChestCount", ex.ParamName);
    }

    [Fact]
    public void ZeroCountsAreAccepted()
    {
        Assert.Null(Record.Exception(() => StationPanelFixtures.AIn(1920, 1080, 1f, stations: 0, chests: 0)));
        Assert.Null(Record.Exception(() => StationPanelFixtures.AIn(1920, 1080, 1f, stations: 6, chests: 0)));
    }

    // GW-U-545 (AC-4, R5, R6): StationPanelInput value semantics. Each of the 10 stored values, when changed alone, gives inequality.
    [Fact]
    public void EqualInputsHaveValueSemantics()
    {
        ValueSemantics.AssertEqual(StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik"), StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik"));
        ValueSemantics.AssertEqual(StationPanelFixtures.AIn(1920, 1080, 1f), StationPanelFixtures.AIn(1920, 1080, 1f));
    }

    [Fact]
    public void EachStoredValueChangedAloneGivesInequality()
    {
        StationPanelInput baseInput = StationPanelFixtures.AIn(1920, 1080, 1f);
        ValueSemantics.AssertNotEqual(baseInput, StationPanelFixtures.AIn(1921, 1080, 1f));
        ValueSemantics.AssertNotEqual(baseInput, StationPanelFixtures.AIn(1920, 1081, 1f));
        ValueSemantics.AssertNotEqual(baseInput, StationPanelFixtures.AIn(1920, 1080, 1.1f));
        ValueSemantics.AssertNotEqual(baseInput, StationPanelFixtures.In(1920, 1080, 1f, new PanelRect(41f, 220f, 300f, 560f), StationPanelFixtures.AReqs, StationPanelFixtures.ACraft));
        ValueSemantics.AssertNotEqual(baseInput, StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, new PanelRect(361f, 310f, 420f, 80f), StationPanelFixtures.ACraft));
        ValueSemantics.AssertNotEqual(baseInput, StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, new PanelRect(601f, 244f, 180f, 46f)));
        ValueSemantics.AssertNotEqual(baseInput, StationPanelFixtures.AIn(1920, 1080, 1f, stations: 5));
        ValueSemantics.AssertNotEqual(baseInput, StationPanelFixtures.AIn(1920, 1080, 1f, chests: 40));
        ValueSemantics.AssertNotEqual(StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik"), StationPanelFixtures.AIn(1920, 1080, 1f, holder: "erik"));
        ValueSemantics.AssertNotEqual(baseInput, StationPanelFixtures.AIn(1920, 1080, 1f, on: false));
    }

    [Fact]
    public void NullHolderAndNamedHolderAreUnequal()
    {
        ValueSemantics.AssertNotEqual(StationPanelFixtures.AIn(1920, 1080, 1f), StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik"));
    }

    [Fact]
    public void NullHandlingFollowsTheOperators()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f);
        ValueSemantics.AssertNullHandling(typeof(StationPanelInput), input);
    }

    // GW-U-2255 (AC-1, station-panel v2 section 10 validation): a fill below 0 throws with the fillPercent name.
    [Fact]
    public void FillPercentBelowZeroThrowsWithItsName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StationPanelInput(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, 6, 41, -1, null, true));

        Assert.Equal("fillPercent", ex.ParamName);
    }

    // GW-U-2255 (AC-1, section 10 validation): a fill above 100 throws with the fillPercent name.
    [Fact]
    public void FillPercentAboveOneHundredThrowsWithItsName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StationPanelInput(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, 6, 41, 101, null, true));

        Assert.Equal("fillPercent", ex.ParamName);
    }

    // GW-U-2255 (AC-1, section 10 validation, parameter order): a negative chest count is reported before a bad fill.
    [Fact]
    public void EligibleChestCountIsCheckedBeforeFillPercent()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StationPanelInput(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, 6, -1, 101, null, true));

        Assert.Equal("eligibleChestCount", ex.ParamName);
    }

    // GW-U-2255 (AC-1, section 10 validation): 0 and 100 are both valid fills, and the value is returned as given.
    [Fact]
    public void ZeroAndOneHundredAreValidFillPercents()
    {
        StationPanelInput zero = new StationPanelInput(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, 6, 41, 0, null, true);
        StationPanelInput full = new StationPanelInput(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, 6, 41, 100, null, true);

        Assert.Equal(0, zero.FillPercent);
        Assert.Equal(100, full.FillPercent);
    }

    // GW-U-2255 (AC-1, section 10 equality): equal fills are equal with equal hash codes.
    [Fact]
    public void EqualFillPercentsAreEqualWithEqualHashCodes()
    {
        StationPanelInput a = StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, p: 64);
        StationPanelInput b = StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, p: 64);

        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // GW-U-2255 (AC-1, section 10 equality): a different fill is not equal.
    [Fact]
    public void DifferentFillPercentsAreNotEqual()
    {
        StationPanelInput a = StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, p: 64);
        StationPanelInput b = StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, p: 63);

        Assert.True(a != b);
        Assert.False(a == b);
    }

    // GW-U-2255 (AC-1, section 10): the FillPercent property returns the value given.
    [Fact]
    public void FillPercentReturnsTheValueGiven()
    {
        StationPanelInput input = StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, p: 64);

        Assert.Equal(64, input.FillPercent);
    }
}
