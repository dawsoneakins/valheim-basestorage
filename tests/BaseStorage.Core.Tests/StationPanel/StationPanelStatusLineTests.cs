namespace BaseStorage.Core.Tests.StationPanel;

using System.Text.RegularExpressions;
using BaseStorage.Core.StationPanel;

/// <summary>AC-3 (contract 4.4, 5.4 status column): the status line text, its singular forms, its presence rules and the eligible-chest count.</summary>
public sealed class StationPanelStatusLineTests
{
    // GW-U-538 (AC-3, contract 4.4; v2 edit in place for S-E010-001 section 10): the tagged literal, and the same text with the tags removed,
    // which is "Base: N stations, M chests, P% full" with P 0 (the default fill) and singular forms when a count is 1. The tag stripping is done only here.
    [Theory]
    [InlineData(6, 41, "Base: <color=orange>6</color> stations, <color=orange>41</color> chests, <color=orange>0%</color> full", "Base: 6 stations, 41 chests, 0% full")]
    [InlineData(1, 0, "Base: <color=orange>1</color> station, <color=orange>0</color> chests, <color=orange>0%</color> full", "Base: 1 station, 0 chests, 0% full")]
    [InlineData(1, 1, "Base: <color=orange>1</color> station, <color=orange>1</color> chest, <color=orange>0%</color> full", "Base: 1 station, 1 chest, 0% full")]
    [InlineData(1, 2, "Base: <color=orange>1</color> station, <color=orange>2</color> chests, <color=orange>0%</color> full", "Base: 1 station, 2 chests, 0% full")]
    [InlineData(2, 1, "Base: <color=orange>2</color> stations, <color=orange>1</color> chest, <color=orange>0%</color> full", "Base: 2 stations, 1 chest, 0% full")]
    [InlineData(6, 0, "Base: <color=orange>6</color> stations, <color=orange>0</color> chests, <color=orange>0%</color> full", "Base: 6 stations, 0 chests, 0% full")]
    [InlineData(3, 12, "Base: <color=orange>3</color> stations, <color=orange>12</color> chests, <color=orange>0%</color> full", "Base: 3 stations, 12 chests, 0% full")]
    [InlineData(2, 5, "Base: <color=orange>2</color> stations, <color=orange>5</color> chests, <color=orange>0%</color> full", "Base: 2 stations, 5 chests, 0% full")]
    public void StatusLineReadsTheBaseCountsWithSingularForms(int stations, int chests, string tagged, string plain)
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, stations, chests));

        Assert.Equal(tagged, state.StatusLineText);
        Assert.NotNull(state.StatusLineText);
        Assert.Equal(plain, Regex.Replace(state.StatusLineText!, "<[^>]+>", string.Empty));
    }

    // GW-U-539 (AC-3): the status line is present in every visible state (enabled, in progress, no chests) and absent in every hidden one.
    [Fact]
    public void StatusLineIsPresentInEveryVisibleState()
    {
        StationPanelState enabled = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f));
        StationPanelState inProgress = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik"));
        StationPanelState noChests = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, stations: 1, chests: 0));

        Assert.NotNull(enabled.StatusLineText);
        Assert.NotNull(inProgress.StatusLineText);
        Assert.NotNull(noChests.StatusLineText);
        Assert.Equal(enabled.StatusLineText, inProgress.StatusLineText);
    }

    [Fact]
    public void StatusLineIsAbsentInEveryHiddenState()
    {
        StationPanelState buttonsOff = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, on: false));
        StationPanelState notInBase = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, stations: 0, chests: 0));
        StationPanelState noRoom = StationPanelLayout.Compute(
            StationPanelFixtures.In(1920, 1080, 1.2f, new PanelRect(0f, 0f, 1600f, 900f), StationPanelFixtures.AReqs, StationPanelFixtures.ACraft));

        Assert.Null(buttonsOff.StatusLineText);
        Assert.Null(notInBase.StatusLineText);
        Assert.Equal(StationPanelHiddenReason.NoRoom, noRoom.HiddenReason);
        Assert.Null(noRoom.StatusLineText);
    }

    // GW-U-539 (AC-3, contract 4.4): the status line counts the eligible chests, with no digit grouping, even at the int limits.
    [Fact]
    public void StatusLineCountsEligibleChestsWithoutDigitGrouping()
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, stations: 1234567, chests: int.MaxValue));
        Assert.Equal(StationPanelFixtures.StatusLargeCounts, state.StatusLineText);
    }

    // GW-U-2254 (AC-1, station-panel v2 4.4 and section 10): the line reads "Base: N stations, M chests, P% full", with singular forms when a count is 1.
    [Theory]
    [InlineData(6, 41, 64, "Base: <color=orange>6</color> stations, <color=orange>41</color> chests, <color=orange>64%</color> full", "Base: 6 stations, 41 chests, 64% full")]
    [InlineData(1, 3, 33, "Base: <color=orange>1</color> station, <color=orange>3</color> chests, <color=orange>33%</color> full", "Base: 1 station, 3 chests, 33% full")]
    [InlineData(1, 1, 100, "Base: <color=orange>1</color> station, <color=orange>1</color> chest, <color=orange>100%</color> full", "Base: 1 station, 1 chest, 100% full")]
    public void StatusLineReadsTheBaseCountsAndTheFillPercent(int stations, int chests, int percent, string tagged, string plain)
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.In(
            1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, stations, chests, percent));

        Assert.Equal(tagged, state.StatusLineText);
        Assert.Equal(plain, Regex.Replace(state.StatusLineText!, "<[^>]+>", string.Empty));
    }

    // GW-U-2254 (AC-1, section 10 row 6): an Organize in progress keeps the same text and disables the button.
    [Fact]
    public void StatusLineKeepsTheSameTextWhileAnOrganizeRunsAndIsDisabled()
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.In(
            1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, 6, 41, 64, "Erik"));

        Assert.Equal("Base: <color=orange>6</color> stations, <color=orange>41</color> chests, <color=orange>64%</color> full", state.StatusLineText);
        Assert.False(state.ButtonsEnabled);
        Assert.Equal(StationPanelDisabledReason.OrganizeInProgress, state.DisabledReason);
    }

    // GW-U-2254 (AC-1, section 10 row 7): with StationButtons off the line is hidden, so its text is null.
    [Fact]
    public void StatusLineIsNullWhenStationButtonsIsOff()
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.In(
            1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, 6, 41, 64, null, false));

        Assert.Null(state.StatusLineText);
        Assert.Equal(StationPanelHiddenReason.StationButtonsOff, state.HiddenReason);
    }

    // GW-U-2254 (AC-1, section 10 row 8, U2): the digits stay invariant under de-DE, tr-TR and ar-SA.
    [Theory]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    public void StatusLineHasInvariantDigitsUnderOtherCultures(string cultureName)
    {
        StationPanelFixtures.WithCulture(cultureName, () =>
        {
            StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.In(
                1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, 6, 41, 64));

            Assert.Equal("Base: <color=orange>6</color> stations, <color=orange>41</color> chests, <color=orange>64%</color> full", state.StatusLineText);
        });
    }

    // GW-U-2257 (AC-6, section 10 row 4): one station with no eligible chest reads 0 chests and 0% full, and the button is disabled for NoEligibleChests.
    [Fact]
    public void NoEligibleChestGivesZeroPercentAndTheNoChestsReason()
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.In(
            1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, 1, 0, 0));

        Assert.Equal("Base: <color=orange>1</color> station, <color=orange>0</color> chests, <color=orange>0%</color> full", state.StatusLineText);
        Assert.False(state.ButtonsEnabled);
        Assert.Equal(StationPanelDisabledReason.NoEligibleChests, state.DisabledReason);
        Assert.Equal(StationPanelFixtures.NoChestsKey, state.TooltipKey);
    }
}
