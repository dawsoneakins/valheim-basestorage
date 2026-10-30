namespace BaseStorage.Core.Tests.StationActions;

using BaseStorage.Core.StationPanel;
using BaseStorage.Core.Tests.StationPanel;

/// <summary>
/// GW-U-1924 (AC-1, AC-4.10; deposit-button contract 4.4 and 3.6): the Deposit button state, read from the existing
/// <see cref="StationPanelLayout"/> over fixture A at 1920x1080 and UI scale 1.0. The layout already computes DepositButton, so this
/// row is green on arrival. It is kept green by the implement phase, which applies the same state to the second clone.
/// </summary>
public sealed class DepositButtonStateTests
{
    // GW-U-1924 (row 1): no holder, enabled, no tooltip, at the contracted rectangle.
    [Fact]
    public void WithNoHolderTheDepositButtonIsEnabledAtItsRectangleWithNoTooltip()
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f));

        Assert.True(state.Visible);
        Assert.Equal(new PanelRect(976f, 244f, 180f, 46f), state.DepositButton);
        Assert.True(state.ButtonsEnabled);
        Assert.Null(state.TooltipText);
    }

    // GW-U-1924 (row 2): another holder disables the button with the in-progress tooltip.
    [Fact]
    public void WhileErikHoldsTheBaseTheDepositButtonIsDisabledWithTheHolderTooltip()
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik"));

        Assert.True(state.Visible);
        Assert.False(state.ButtonsEnabled);
        Assert.Equal("Organize in progress by Erik", state.TooltipText);
    }

    // GW-U-1924 (row 3): a base without an eligible chest disables the button with the no-chests tooltip.
    [Fact]
    public void WithNoChestsTheDepositButtonIsDisabledWithTheNoChestsTooltip()
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, chests: 0));

        Assert.True(state.Visible);
        Assert.False(state.ButtonsEnabled);
        Assert.Equal("No chests in this base", state.TooltipText);
    }

    // GW-U-1924 (row 4): StationButtons off hides the panel, and the Deposit button has no rectangle.
    [Fact]
    public void StationButtonsOffHidesTheDepositButton()
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, on: false));

        Assert.False(state.Visible);
        Assert.Equal(default(PanelRect), state.DepositButton);
    }

    // GW-U-1924 (row 5, AC-4.10): the Deposit button starts 8 units to the right of the Organize button, with no overlap.
    [Fact]
    public void TheDepositButtonStartsEightUnitsRightOfTheOrganizeButton()
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f));

        Assert.Equal(8f, state.DepositButton.X - state.OrganizeButton.Right);
    }
}
