namespace BaseStorage.Core.Tests.StationPanel;

using BaseStorage.Core.StationPanel;

/// <summary>AC-1 (SP4, contract 4.1 step 1, 5.2): the canvas model is computed for every input, including hidden ones.</summary>
public sealed class StationPanelCanvasTests
{
    // GW-U-522 (AC-1): the 13 contract 5.2 inputs plus 1024x768@1.5. Each canvas equals the contract value within 0.01, for a visible result
    // and for a hidden one (on = false), because the canvas is always computed.
    [Theory]
    [InlineData("1280x720@0.8", 1280, 720, 0.8f, 2400f, 1350f)]
    [InlineData("1280x720@1.0", 1280, 720, 1.0f, 1920f, 1080f)]
    [InlineData("1280x720@1.2", 1280, 720, 1.2f, 1600f, 900f)]
    [InlineData("1920x1080@0.8", 1920, 1080, 0.8f, 2400f, 1350f)]
    [InlineData("1920x1080@1.0", 1920, 1080, 1.0f, 1920f, 1080f)]
    [InlineData("1920x1080@1.2", 1920, 1080, 1.2f, 1600f, 900f)]
    [InlineData("3840x2160@0.8", 3840, 2160, 0.8f, 2400f, 1350f)]
    [InlineData("3840x2160@1.0", 3840, 2160, 1.0f, 1920f, 1080f)]
    [InlineData("3840x2160@1.2", 3840, 2160, 1.2f, 1600f, 900f)]
    [InlineData("2560x1080@1.2 ultrawide", 2560, 1080, 1.2f, 2133.33f, 900f)]
    [InlineData("1280x1024@1.2 5:4", 1280, 1024, 1.2f, 1600f, 1280f)]
    [InlineData("3840x720@1.2", 3840, 720, 1.2f, 4800f, 900f)]
    [InlineData("1280x2160@0.8", 1280, 2160, 0.8f, 2400f, 4050f)]
    [InlineData("1024x768@1.5 out of range", 1024, 768, 1.5f, 1280f, 960f)]
    public void CanvasMatchesTheContractForVisibleAndHiddenResults(string name, int width, int height, float scale, float canvasWidth, float canvasHeight)
    {
        StationPanelState visible = StationPanelLayout.Compute(StationPanelFixtures.AIn(width, height, scale));
        StationPanelFixtures.AssertCanvas(visible, canvasWidth, canvasHeight);

        StationPanelState hidden = StationPanelLayout.Compute(StationPanelFixtures.AIn(width, height, scale, on: false));
        StationPanelFixtures.AssertCanvas(hidden, canvasWidth, canvasHeight);
        Assert.False(hidden.Visible, name + " must be hidden when StationButtons is off.");
    }

    // GW-U-522 (AC-1, SP4 exact float): the reference canvas at 1920x1080@1.2 is a little under 1600 wide, and at 1280x720@0.8 a little under 2400.
    [Fact]
    public void ExactFloatCanvasesAreJustUnderTheRoundNumbers()
    {
        StationPanelState atReferenceScale = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1.2f));
        Assert.True(atReferenceScale.CanvasWidth < 1600f, "The canvas width at 1920x1080@1.2 must be under 1600.");

        StationPanelState smallScreen = StationPanelLayout.Compute(StationPanelFixtures.AIn(1280, 720, 0.8f));
        Assert.True(smallScreen.CanvasWidth < 2400f, "The canvas width at 1280x720@0.8 must be under 2400.");
    }
}
