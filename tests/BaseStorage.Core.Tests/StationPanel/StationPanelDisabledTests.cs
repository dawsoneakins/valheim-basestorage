namespace BaseStorage.Core.Tests.StationPanel;

using System.Collections.Generic;
using BaseStorage.Core.StationPanel;

/// <summary>AC-2 (contract 4.1 step 5, 4.4, 5.4 rows 2 to 5): the disabled reasons, their tooltips, the precedence and the geometry they keep.</summary>
public sealed class StationPanelDisabledTests
{
    // GW-U-531 (AC-2): an Organize running in this base disables both buttons, with the holder tooltip, at the reference canvas.
    [Fact]
    public void OrganizeRunningDisablesBothButtonsWithTheHolderTooltip()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik");
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.True(state.Visible);
        Assert.False(state.ButtonsEnabled);
        Assert.Equal(StationPanelDisabledReason.OrganizeInProgress, state.DisabledReason);
        Assert.Equal("station.actions.disabled-in-progress", state.TooltipKey);
        Assert.Equal("Organize in progress by Erik", state.TooltipText);
        Assert.Equal(StationPanelFixtures.Status6Stations41Chests, state.StatusLineText);
        StationPanelFixtures.AssertPlaced(
            state,
            StationPanelPlacement.RightOfCraftButton,
            StationPanelFixtures.ARightOrganize,
            StationPanelFixtures.ARightDeposit,
            StationPanelFixtures.ARightStatus);
        Sp5.AssertHolds(input, state);
    }

    [Fact]
    public void OrganizeRunningWithOneChestUsesTheSameTooltipAndTheSingularChestText()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f, chests: 1, holder: "Bjorn");
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.Equal(StationPanelDisabledReason.OrganizeInProgress, state.DisabledReason);
        Assert.Equal("station.actions.disabled-in-progress", state.TooltipKey);
        Assert.Equal("Organize in progress by Bjorn", state.TooltipText);
        Assert.Equal(StationPanelFixtures.Status6Stations1Chest, state.StatusLineText);
        Sp5.AssertHolds(input, state);
    }

    // GW-U-532 (AC-2): a base with no eligible chests disables both buttons, with the no-chests tooltip.
    [Fact]
    public void NoEligibleChestsDisablesBothButtonsWithTheNoChestsTooltip()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f, stations: 1, chests: 0);
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.True(state.Visible);
        Assert.False(state.ButtonsEnabled);
        Assert.Equal(StationPanelDisabledReason.NoEligibleChests, state.DisabledReason);
        Assert.Equal("station.actions.disabled-no-chests", state.TooltipKey);
        Assert.Equal("No chests in this base", state.TooltipText);
        Assert.Equal(StationPanelFixtures.Status1Station0Chests, state.StatusLineText);
        Sp5.AssertHolds(input, state);
    }

    [Fact]
    public void NoEligibleChestsWithSixStationsUsesThePluralStatusText()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f, stations: 6, chests: 0);
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.Equal(StationPanelDisabledReason.NoEligibleChests, state.DisabledReason);
        Assert.Equal("station.actions.disabled-no-chests", state.TooltipKey);
        Assert.Equal("No chests in this base", state.TooltipText);
        Assert.Equal(StationPanelFixtures.Status6Stations0Chests, state.StatusLineText);
        Sp5.AssertHolds(input, state);
    }

    // GW-U-533 (AC-2, contract 4.1 step 5): when both reasons hold, OrganizeInProgress wins, for six stations and for one station.
    [Fact]
    public void OrganizeInProgressTakesPrecedenceOverNoChestsWithSixStations()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f, stations: 6, chests: 0, holder: "Erik");
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.Equal(StationPanelDisabledReason.OrganizeInProgress, state.DisabledReason);
        Assert.Equal("station.actions.disabled-in-progress", state.TooltipKey);
        Assert.Equal("Organize in progress by Erik", state.TooltipText);
        Assert.Equal(StationPanelFixtures.Status6Stations0Chests, state.StatusLineText);
        Sp5.AssertHolds(input, state);
    }

    [Fact]
    public void OrganizeInProgressTakesPrecedenceOverNoChestsWithOneStation()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f, stations: 1, chests: 0, holder: "Astrid");
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.Equal(StationPanelDisabledReason.OrganizeInProgress, state.DisabledReason);
        Assert.Equal("station.actions.disabled-in-progress", state.TooltipKey);
        Assert.Equal("Organize in progress by Astrid", state.TooltipText);
        Assert.Equal(StationPanelFixtures.Status1Station0Chests, state.StatusLineText);
        Sp5.AssertHolds(input, state);
    }

    public static TheoryData<string, string> HolderNames()
    {
        return new TheoryData<string, string>
        {
            { "Erik\nthe Red", "Organize in progress by Erik the Red" },
            { "Erik\r\nthe Red", "Organize in progress by Erik the Red" },
            { "Erik\r\n\nthe Red", "Organize in progress by Erik the Red" },
            { "Ragnar;the Red", "Organize in progress by Ragnar;the Red" },
            { "{0}", "Organize in progress by {0}" },
            { "Ærik", "Organize in progress by Ærik" },
        };
    }

    // GW-U-534 (AC-2, contract 4.4): line breaks in the holder name are flattened to single spaces in the tooltip only. The stored holder is unchanged.
    [Theory]
    [MemberData(nameof(HolderNames))]
    public void HolderNamesAreFlattenedOnlyInTheTooltip(string holder, string expectedTooltip)
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f, holder: holder);
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.Equal(expectedTooltip, state.TooltipText);
        Assert.Equal(holder, input.OrganizeHolderName);
    }

    // GW-U-535 (AC-2): the enabled state has no tooltip, and a disabled reason never moves the buttons or the status line.
    [Fact]
    public void EnabledStateHasNoTooltip()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f);
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.True(state.ButtonsEnabled);
        Assert.Equal(StationPanelDisabledReason.None, state.DisabledReason);
        Assert.Null(state.TooltipKey);
        Assert.Null(state.TooltipText);
        Sp5.AssertHolds(input, state);
    }

    [Fact]
    public void DisabledReasonsNeverChangeTheGeometry()
    {
        StationPanelState baseline = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f));
        var disabledVariants = new List<StationPanelInput>
        {
            StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik"),
            StationPanelFixtures.AIn(1920, 1080, 1f, stations: 1, chests: 0),
            StationPanelFixtures.AIn(1920, 1080, 1f, stations: 6, chests: 0),
            StationPanelFixtures.AIn(1920, 1080, 1f, stations: 6, chests: 0, holder: "Erik"),
        };

        foreach (StationPanelInput input in disabledVariants)
        {
            StationPanelState state = StationPanelLayout.Compute(input);
            Assert.False(state.ButtonsEnabled);
            Assert.Equal(baseline.Placement, state.Placement);
            Assert.Equal(baseline.OrganizeButton, state.OrganizeButton);
            Assert.Equal(baseline.DepositButton, state.DepositButton);
            Assert.Equal(baseline.StatusLine, state.StatusLine);
        }
    }
}
