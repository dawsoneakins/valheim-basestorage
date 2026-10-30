namespace BaseStorage.Core.Tests.StationPanel;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.StationPanel;

/// <summary>AC-4 (contract 4.1, 5.4 rows 7 to 10, spec assumption 3): StationButtons off hides everything, zero stations, and the coverage of every reason.</summary>
public sealed class StationPanelHiddenTests
{
    private static (StationPanelInput Input, float Width, float Height) HiddenCase(int index)
    {
        switch (index)
        {
            case 0:
                return (StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik", on: false), 1920f, 1080f);
            case 1:
                return (StationPanelFixtures.AIn(1920, 1080, 1f, on: false), 1920f, 1080f);
            case 2:
                return (StationPanelFixtures.AIn(1920, 1080, 1f, chests: 0, on: false), 1920f, 1080f);
            case 3:
                return (StationPanelFixtures.AIn(1920, 1080, 1f, stations: 0, chests: 0, on: false), 1920f, 1080f);
            case 4:
                return (StationPanelFixtures.In(1920, 1080, 1.2f, new PanelRect(0f, 0f, 1600f, 900f), StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, on: false), 1600f, 900f);
            default:
                throw new ArgumentOutOfRangeException(nameof(index), index, "Unknown hidden case.");
        }
    }

    // GW-U-541 (AC-4, contract 4.1 step 2, 5.4 rows 7, 8, 10): StationButtons false hides both buttons and the status line, and wins over every
    // other hidden reason. No disabled reason or tooltip is reported, and the canvas is set.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void StationButtonsOffHidesBothButtonsAndTheStatusLine(int index)
    {
        (StationPanelInput input, float width, float height) = HiddenCase(index);
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.Equal(StationPanelHiddenReason.StationButtonsOff, state.HiddenReason);
        StationPanelFixtures.AssertCanvas(state, width, height);
        Sp5.AssertHolds(input, state);
        Assert.Equal(StationPanelDisabledReason.None, state.DisabledReason);
        Assert.Null(state.TooltipKey);
        Assert.Null(state.TooltipText);
    }

    // GW-U-542 (AC-4, contract 5.4 row 9): zero stations and zero chests, and hidden before disabled.
    [Fact]
    public void ZeroStationsAndZeroChestsIsNotInBaseAndHidden()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f, stations: 0, chests: 0);
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.Equal(StationPanelHiddenReason.NotInBase, state.HiddenReason);
        Sp5.AssertHolds(input, state);
    }

    [Fact]
    public void ZeroStationsWithChestsAndAHolderIsNotInBaseNotInProgress()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f, stations: 0, chests: 41, holder: "Erik");
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.Equal(StationPanelHiddenReason.NotInBase, state.HiddenReason);
        Assert.NotEqual(StationPanelDisabledReason.OrganizeInProgress, state.DisabledReason);
        Sp5.AssertHolds(input, state);
    }

    [Fact]
    public void OneStationAndZeroChestsIsVisibleWithNoEligibleChests()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1920, 1080, 1f, stations: 1, chests: 0);
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.True(state.Visible);
        Assert.Equal(StationPanelDisabledReason.NoEligibleChests, state.DisabledReason);
        Sp5.AssertHolds(input, state);
    }

    // GW-U-543 (AC-4, spec AC-4 meta): every member of the three enums is produced by at least one contract fixture input.
    [Fact]
    public void EveryHiddenReasonDisabledReasonAndPlacementIsProduced()
    {
        StationPanelInput[] fixtures =
        {
            StationPanelFixtures.AIn(1920, 1080, 1f),
            StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik"),
            StationPanelFixtures.AIn(1920, 1080, 1f, stations: 1, chests: 0),
            StationPanelFixtures.AIn(1920, 1080, 1f, on: false),
            StationPanelFixtures.AIn(1920, 1080, 1f, stations: 0, chests: 0),
            StationPanelFixtures.In(1920, 1080, 1.2f, new PanelRect(0f, 0f, 1600f, 900f), StationPanelFixtures.AReqs, StationPanelFixtures.ACraft),
            StationPanelFixtures.BIn(1280, 720, 1.2f),
            StationPanelFixtures.In(1920, 1080, 1f, new PanelRect(1200f, 400f, 300f, 400f), new PanelRect(1200f, 320f, 380f, 60f), new PanelRect(1400f, 60f, 180f, 46f)),
            StationPanelFixtures.In(1920, 1080, 1f, new PanelRect(0f, 0f, 1600f, 900f), StationPanelFixtures.AReqs, StationPanelFixtures.ACraft, 6, 41, 0, null, true),
            StationPanelFixtures.In(1920, 1080, 1f, new PanelRect(40f, 100f, 300f, 950f), new PanelRect(360f, 40f, 1000f, 100f), new PanelRect(600f, 150f, 180f, 46f)),
            StationPanelFixtures.In(2000, 1120, 1f, new PanelRect(1326f, 231f, 188f, 552f), new PanelRect(1533f, 304f, 336f, 64f), new PanelRect(1533f, 241f, 336f, 60f)),
        };

        var hidden = new HashSet<StationPanelHiddenReason>();
        var disabled = new HashSet<StationPanelDisabledReason>();
        var placements = new HashSet<StationPanelPlacement>();
        foreach (StationPanelInput input in fixtures)
        {
            StationPanelState state = StationPanelLayout.Compute(input);
            hidden.Add(state.HiddenReason);
            disabled.Add(state.DisabledReason);
            placements.Add(state.Placement);
        }

        Assert.Equal(Enum.GetValues<StationPanelHiddenReason>().OrderBy(value => (int)value).ToArray(), hidden.OrderBy(value => (int)value).ToArray());
        Assert.Equal(Enum.GetValues<StationPanelDisabledReason>().OrderBy(value => (int)value).ToArray(), disabled.OrderBy(value => (int)value).ToArray());
        Assert.Equal(Enum.GetValues<StationPanelPlacement>().OrderBy(value => (int)value).ToArray(), placements.OrderBy(value => (int)value).ToArray());
    }
}
