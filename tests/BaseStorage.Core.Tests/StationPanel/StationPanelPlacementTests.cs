namespace BaseStorage.Core.Tests.StationPanel;

using BaseStorage.Core.StationPanel;

/// <summary>AC-1 (contracts 4.2, 4.3, 5.3): the other placements, the edge geometry, the first fitting candidate, and the row relations.</summary>
public sealed class StationPanelPlacementTests
{
    private static PanelRect ControlsRecipes => new PanelRect(40f, 100f, 300f, 950f);

    // GW-U-526 (AC-1, contract 5.3): LeftOfCraftButton, with the organize button at (1024,60).
    [Fact]
    public void LeftOfCraftButtonRowIsPlacedToTheLeft()
    {
        var input = StationPanelFixtures.In(1920, 1080, 1f, new PanelRect(1200f, 400f, 300f, 400f), new PanelRect(1200f, 320f, 380f, 60f), new PanelRect(1400f, 60f, 180f, 46f));
        StationPanelState state = StationPanelLayout.Compute(input);
        StationPanelFixtures.AssertPlaced(
            state,
            StationPanelPlacement.LeftOfCraftButton,
            new PanelRect(1024f, 60f, 180f, 46f),
            new PanelRect(1212f, 60f, 180f, 46f),
            new PanelRect(1024f, 20f, 368f, 24f));
        Sp5.AssertHolds(input, state);
    }

    // GW-U-526 (AC-1, contract 5.3): RightOfControls, with the organize button at (1368,150).
    [Fact]
    public void RightOfControlsRowIsPlacedAfterTheWholePanel()
    {
        var input = StationPanelFixtures.In(1920, 1080, 1f, ControlsRecipes, new PanelRect(360f, 40f, 1000f, 100f), new PanelRect(600f, 150f, 180f, 46f));
        StationPanelState state = StationPanelLayout.Compute(input);
        StationPanelFixtures.AssertPlaced(
            state,
            StationPanelPlacement.RightOfControls,
            new PanelRect(1368f, 150f, 180f, 46f),
            new PanelRect(1556f, 150f, 180f, 46f),
            new PanelRect(1368f, 110f, 368f, 24f));
        Sp5.AssertHolds(input, state);
    }

    // GW-U-526 (AC-1, contract 5.3): AboveControls at 1.0, with the organize button at (600,948).
    [Fact]
    public void AboveControlsRowIsPlacedAboveThePanelAtReferenceScale()
    {
        var input = StationPanelFixtures.In(1920, 1080, 1f, new PanelRect(0f, 0f, 1600f, 900f), StationPanelFixtures.AReqs, StationPanelFixtures.ACraft);
        StationPanelState state = StationPanelLayout.Compute(input);
        StationPanelFixtures.AssertPlaced(
            state,
            StationPanelPlacement.AboveControls,
            new PanelRect(600f, 948f, 180f, 46f),
            new PanelRect(788f, 948f, 180f, 46f),
            new PanelRect(600f, 908f, 368f, 24f));
        Sp5.AssertHolds(input, state);
    }

    // GW-U-526 (AC-1, SP6, contract 5.3): the same panel at 1.2 has no room, so it is hidden with NoRoom on a 1600 x 900 canvas.
    [Fact]
    public void SamePanelAtScale12HasNoRoomAndIsHidden()
    {
        var input = StationPanelFixtures.In(1920, 1080, 1.2f, new PanelRect(0f, 0f, 1600f, 900f), StationPanelFixtures.AReqs, StationPanelFixtures.ACraft);
        StationPanelState state = StationPanelLayout.Compute(input);
        Assert.False(state.Visible);
        Assert.Equal(StationPanelHiddenReason.NoRoom, state.HiddenReason);
        StationPanelFixtures.AssertCanvas(state, 1600f, 900f);
        Sp5.AssertHolds(input, state);
    }

    // GW-U-526 (AC-1, contract 5.3): a small craft button is widened to the minimum, 120 x 32, with the texts for 3 stations and 12 chests.
    [Fact]
    public void SmallCraftButtonGetsTheMinimumSizeAndItsOwnStatusText()
    {
        var craft = new PanelRect(600f, 244f, 100f, 20f);
        var input = StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, craft, 3, 12);
        StationPanelState state = StationPanelLayout.Compute(input);
        StationPanelFixtures.AssertPlaced(
            state,
            StationPanelPlacement.RightOfCraftButton,
            new PanelRect(708f, 244f, 120f, 32f),
            new PanelRect(836f, 244f, 120f, 32f),
            new PanelRect(708f, 204f, 248f, 24f));
        Assert.Equal(StationPanelFixtures.Status3Stations12Chests, state.StatusLineText);
        Sp5.AssertHolds(input, state);
    }

    // GW-U-526 (AC-1, contract 5.3): a collapsed requirements rectangle still gets the 8-unit gap, and the texts are for 2 stations and 5 chests.
    [Fact]
    public void CollapsedRequirementsStillGetTheGapAndTheirOwnStatusText()
    {
        var reqs = new PanelRect(500f, 300f, 0f, 0f);
        var input = StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, reqs, StationPanelFixtures.ACraft, 2, 5);
        StationPanelState state = StationPanelLayout.Compute(input);
        StationPanelFixtures.AssertPlaced(state, StationPanelPlacement.RightOfCraftButton, StationPanelFixtures.ARightOrganize, StationPanelFixtures.ARightDeposit, StationPanelFixtures.ARightStatus);
        Assert.Equal(StationPanelFixtures.Status2Stations5Chests, state.StatusLineText);
        Sp5.AssertHolds(input, state);
    }

    // GW-U-527 (AC-1, contract 4.2 note): at the reference scale the first candidate fits exactly, so deposit and status right edges are 1600.
    [Fact]
    public void RightOfCraftButtonFitsExactlyAtTheReferenceScale()
    {
        var craft = new PanelRect(1044f, 244f, 180f, 46f);
        var input = StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, craft);
        StationPanelState state = StationPanelLayout.Compute(input);
        Assert.Equal(StationPanelPlacement.RightOfCraftButton, state.Placement);
        Assert.Equal(1600f, state.DepositButton.Right);
        Assert.Equal(1600f, state.StatusLine.Right);
        Sp5.AssertHolds(input, state);
    }

    // GW-U-527 (AC-1, contract 4.2 note): at 1920x1080@1.2 and 1280x720@1.2 the canvas is just under 1600, so the first candidate loses and the
    // second one, BelowCraftButton, wins with the rectangles given by the contract.
    [Theory]
    [InlineData(1920, 1080, 1.2f)]
    [InlineData(1280, 720, 1.2f)]
    public void BelowCraftButtonWinsJustUnderTheCanvasEdge(int width, int height, float scale)
    {
        var craft = new PanelRect(1044f, 244f, 180f, 46f);
        var input = StationPanelFixtures.In(width, height, scale, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, craft);
        StationPanelState state = StationPanelLayout.Compute(input);
        StationPanelFixtures.AssertPlaced(
            state,
            StationPanelPlacement.BelowCraftButton,
            new PanelRect(1044f, 190f, 180f, 46f),
            new PanelRect(1232f, 190f, 180f, 46f),
            new PanelRect(1044f, 150f, 368f, 24f));
        Sp5.AssertHolds(input, state);
    }

    // GW-U-527 (AC-1, contract 5.3 RightOfControls row): candidates 1 to 4 are built by hand from 4.2 and each fails SP5 on a 1920 x 1080 canvas,
    // so candidate 5 wins because it is the first that fits.
    [Fact]
    public void FirstFittingCandidateWinsForTheControlsRow()
    {
        PanelRect craft = new PanelRect(600f, 150f, 180f, 46f);
        PanelRect reqs = new PanelRect(360f, 40f, 1000f, 100f);
        var input = StationPanelFixtures.In(1920, 1080, 1f, ControlsRecipes, reqs, craft);

        Assert.False(Sp5.Fits(input, 1920f, 1080f, new PanelRect(788f, 150f, 180f, 46f), new PanelRect(976f, 150f, 180f, 46f), new PanelRect(788f, 110f, 368f, 24f)));
        Assert.False(Sp5.Fits(input, 1920f, 1080f, new PanelRect(600f, 96f, 180f, 46f), new PanelRect(788f, 96f, 180f, 46f), new PanelRect(600f, 56f, 368f, 24f)));
        Assert.False(Sp5.Fits(input, 1920f, 1080f, new PanelRect(224f, 150f, 180f, 46f), new PanelRect(412f, 150f, 180f, 46f), new PanelRect(224f, 110f, 368f, 24f)));
        Assert.False(Sp5.Fits(input, 1920f, 1080f, new PanelRect(600f, 1098f, 180f, 46f), new PanelRect(788f, 1098f, 180f, 46f), new PanelRect(600f, 1058f, 368f, 24f)));
        Assert.True(Sp5.Fits(input, 1920f, 1080f, new PanelRect(1368f, 150f, 180f, 46f), new PanelRect(1556f, 150f, 180f, 46f), new PanelRect(1368f, 110f, 368f, 24f)));

        StationPanelState state = StationPanelLayout.Compute(input);
        Assert.Equal(StationPanelPlacement.RightOfControls, state.Placement);
    }

    public static TheoryData<string, StationPanelPlacement> PlacementCases()
    {
        return new TheoryData<string, StationPanelPlacement>
        {
            { "RightOfCraftButton", StationPanelPlacement.RightOfCraftButton },
            { "BelowCraftButton", StationPanelPlacement.BelowCraftButton },
            { "LeftOfCraftButton", StationPanelPlacement.LeftOfCraftButton },
            { "AboveControls", StationPanelPlacement.AboveControls },
            { "RightOfControls", StationPanelPlacement.RightOfControls },
        };
    }

    // GW-U-530 (AC-1): for one fixture per placement, organize is left of deposit, the gap is 8, both rectangles share Y, Width and Height,
    // the size is max(craft, minimum), and the status line geometry follows the row.
    [Theory]
    [MemberData(nameof(PlacementCases))]
    public void RowGeometryRelationsHoldForEveryPlacement(string name, StationPanelPlacement expected)
    {
        StationPanelInput input = InputFor(name);
        StationPanelState state = StationPanelLayout.Compute(input);
        Assert.True(state.Visible, name + " must be visible.");
        Assert.Equal(expected, state.Placement);

        PanelRect organize = state.OrganizeButton;
        PanelRect deposit = state.DepositButton;
        PanelRect status = state.StatusLine;
        Assert.True(organize.X < deposit.X, "Organize must be left of Deposit.");
        Assert.Equal(organize.Right + 8f, deposit.X);
        Assert.Equal(organize.Y, deposit.Y);
        Assert.Equal(organize.Width, deposit.Width);
        Assert.Equal(organize.Height, deposit.Height);
        Assert.Equal(System.Math.Max(input.CraftButton.Width, 120f), organize.Width);
        Assert.Equal(System.Math.Max(input.CraftButton.Height, 32f), organize.Height);
        Assert.Equal(organize.X, status.X);
        Assert.Equal(deposit.Right - organize.X, status.Width);
        Assert.Equal(24f, status.Height);
        Assert.Equal(organize.Y - 16f, status.Top);
        Sp5.AssertHolds(input, state);
    }

    private static StationPanelInput InputFor(string name)
    {
        switch (name)
        {
            case "RightOfCraftButton":
                return StationPanelFixtures.AIn(1920, 1080, 1f);
            case "BelowCraftButton":
                return StationPanelFixtures.BIn(1280, 720, 1.2f);
            case "LeftOfCraftButton":
                return StationPanelFixtures.In(1920, 1080, 1f, new PanelRect(1200f, 400f, 300f, 400f), new PanelRect(1200f, 320f, 380f, 60f), new PanelRect(1400f, 60f, 180f, 46f));
            case "AboveControls":
                return StationPanelFixtures.In(1920, 1080, 1f, new PanelRect(0f, 0f, 1600f, 900f), StationPanelFixtures.AReqs, StationPanelFixtures.ACraft);
            case "RightOfControls":
                return StationPanelFixtures.In(1920, 1080, 1f, ControlsRecipes, new PanelRect(360f, 40f, 1000f, 100f), new PanelRect(600f, 150f, 180f, 46f));
            default:
                throw new System.ArgumentOutOfRangeException(nameof(name), name, "Unknown placement case.");
        }
    }
}
