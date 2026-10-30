namespace BaseStorage.Core.Tests.StationPanel;

using BaseStorage.Core.StationPanel;

/// <summary>AC-1 and AC-4 (contract 5.2): each resolution and scale corner as a named row, and every row of the 5.2 table for fixture B.</summary>
public sealed class StationPanelCornerTests
{
    // GW-U-524 (AC-4): the four corners and the reference, each for fixture A and fixture B, with the complete expected state from the 5.2 table.
    [Theory]
    [InlineData("1280x720@0.8/A", false, 1280, 720, 0.8f, 2400f, 1350f, StationPanelPlacement.RightOfCraftButton, "A-right")]
    [InlineData("1280x720@0.8/B", true, 1280, 720, 0.8f, 2400f, 1350f, StationPanelPlacement.RightOfCraftButton, "B-right")]
    [InlineData("1280x720@1.2/A", false, 1280, 720, 1.2f, 1600f, 900f, StationPanelPlacement.RightOfCraftButton, "A-right")]
    [InlineData("1280x720@1.2/B", true, 1280, 720, 1.2f, 1600f, 900f, StationPanelPlacement.BelowCraftButton, "B-below")]
    [InlineData("3840x2160@0.8/A", false, 3840, 2160, 0.8f, 2400f, 1350f, StationPanelPlacement.RightOfCraftButton, "A-right")]
    [InlineData("3840x2160@0.8/B", true, 3840, 2160, 0.8f, 2400f, 1350f, StationPanelPlacement.RightOfCraftButton, "B-right")]
    [InlineData("3840x2160@1.2/A", false, 3840, 2160, 1.2f, 1600f, 900f, StationPanelPlacement.RightOfCraftButton, "A-right")]
    [InlineData("3840x2160@1.2/B", true, 3840, 2160, 1.2f, 1600f, 900f, StationPanelPlacement.BelowCraftButton, "B-below")]
    [InlineData("1920x1080@1.0/A", false, 1920, 1080, 1.0f, 1920f, 1080f, StationPanelPlacement.RightOfCraftButton, "A-right")]
    [InlineData("1920x1080@1.0/B", true, 1920, 1080, 1.0f, 1920f, 1080f, StationPanelPlacement.RightOfCraftButton, "B-right")]
    public void EachCornerAndReferenceGivesTheCompleteExpectedState(
        string name,
        bool fixtureB,
        int width,
        int height,
        float scale,
        float canvasWidth,
        float canvasHeight,
        StationPanelPlacement placement,
        string rects)
    {
        StationPanelInput input = fixtureB ? StationPanelFixtures.BIn(width, height, scale) : StationPanelFixtures.AIn(width, height, scale);
        StationPanelState state = StationPanelLayout.Compute(input);

        StationPanelFixtures.AssertCanvas(state, canvasWidth, canvasHeight);
        (PanelRect organize, PanelRect deposit, PanelRect status) = StationPanelFixtures.Rects(rects);
        StationPanelFixtures.AssertPlaced(state, placement, organize, deposit, status);
        Assert.True(state.ButtonsEnabled, name + " must be enabled.");
        Assert.Equal(StationPanelDisabledReason.None, state.DisabledReason);
        Assert.Null(state.TooltipKey);
        Assert.Null(state.TooltipText);
        Assert.Equal(StationPanelFixtures.Status6Stations41Chests, state.StatusLineText);
        Sp5.AssertHolds(input, state);
    }

    public static TheoryData<int, int, float, float, float, StationPanelPlacement, string> ContractTable()
    {
        var data = new TheoryData<int, int, float, float, float, StationPanelPlacement, string>();
        data.Add(1280, 720, 0.8f, 2400f, 1350f, StationPanelPlacement.RightOfCraftButton, "B-right");
        data.Add(1280, 720, 1.0f, 1920f, 1080f, StationPanelPlacement.RightOfCraftButton, "B-right");
        data.Add(1280, 720, 1.2f, 1600f, 900f, StationPanelPlacement.BelowCraftButton, "B-below");
        data.Add(1920, 1080, 0.8f, 2400f, 1350f, StationPanelPlacement.RightOfCraftButton, "B-right");
        data.Add(1920, 1080, 1.0f, 1920f, 1080f, StationPanelPlacement.RightOfCraftButton, "B-right");
        data.Add(1920, 1080, 1.2f, 1600f, 900f, StationPanelPlacement.BelowCraftButton, "B-below");
        data.Add(3840, 2160, 0.8f, 2400f, 1350f, StationPanelPlacement.RightOfCraftButton, "B-right");
        data.Add(3840, 2160, 1.0f, 1920f, 1080f, StationPanelPlacement.RightOfCraftButton, "B-right");
        data.Add(3840, 2160, 1.2f, 1600f, 900f, StationPanelPlacement.BelowCraftButton, "B-below");
        data.Add(2560, 1080, 1.2f, 2133.33f, 900f, StationPanelPlacement.RightOfCraftButton, "B-right");
        data.Add(1280, 1024, 1.2f, 1600f, 1280f, StationPanelPlacement.BelowCraftButton, "B-below");
        data.Add(3840, 720, 1.2f, 4800f, 900f, StationPanelPlacement.RightOfCraftButton, "B-right");
        data.Add(1280, 2160, 0.8f, 2400f, 4050f, StationPanelPlacement.RightOfCraftButton, "B-right");
        return data;
    }

    // GW-U-525 (AC-1): every row of the contract 5.2 table for fixture B. The matching fixture A rows are in the second theory and are all A-right.
    [Theory]
    [MemberData(nameof(ContractTable))]
    public void EveryFixtureBRowOfTheTableMatches(
        int width,
        int height,
        float scale,
        float canvasWidth,
        float canvasHeight,
        StationPanelPlacement placement,
        string rects)
    {
        StationPanelInput input = StationPanelFixtures.BIn(width, height, scale);
        StationPanelState state = StationPanelLayout.Compute(input);

        StationPanelFixtures.AssertCanvas(state, canvasWidth, canvasHeight);
        (PanelRect organize, PanelRect deposit, PanelRect status) = StationPanelFixtures.Rects(rects);
        StationPanelFixtures.AssertPlaced(state, placement, organize, deposit, status);
        Assert.True(state.ButtonsEnabled);
        Assert.Equal(StationPanelFixtures.Status6Stations41Chests, state.StatusLineText);
        Sp5.AssertHolds(input, state);
    }

    [Theory]
    [MemberData(nameof(ContractTable))]
    public void EveryFixtureARowOfTheTableIsRightOfTheCraftButton(
        int width,
        int height,
        float scale,
        float canvasWidth,
        float canvasHeight,
        StationPanelPlacement placement,
        string rects)
    {
        _ = placement;
        _ = rects;
        StationPanelInput input = StationPanelFixtures.AIn(width, height, scale);
        StationPanelState state = StationPanelLayout.Compute(input);

        StationPanelFixtures.AssertCanvas(state, canvasWidth, canvasHeight);
        StationPanelFixtures.AssertPlaced(
            state,
            StationPanelPlacement.RightOfCraftButton,
            StationPanelFixtures.ARightOrganize,
            StationPanelFixtures.ARightDeposit,
            StationPanelFixtures.ARightStatus);
        Sp5.AssertHolds(input, state);
    }
}
