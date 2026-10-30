namespace BaseStorage.Core.Tests.StationPanel;

using BaseStorage.Core.StationPanel;

/// <summary>
/// The panel as the game really lays it out, measured from a 2000 x 1120 playtest screenshot on 2026-10-30: the craft panel sits in
/// the right corner and the craft button is about 336 wide, so a row of two buttons is too wide for the five original candidates.
/// </summary>
public class StationPanelRealGeometryTests
{
    private static StationPanelInput RealPanel() => new StationPanelInput(
        2000, 1120, 1f,
        new PanelRect(1326f, 231f, 188f, 552f),
        new PanelRect(1533f, 304f, 336f, 64f),
        new PanelRect(1533f, 241f, 336f, 60f),
        1, 60, 70, null, true);

    [Fact]
    public void ButtonsAreShownUnderARightCornerPanel()
    {
        StationPanelState state = StationPanelLayout.Compute(RealPanel());

        Assert.True(state.Visible);
        Assert.Equal(StationPanelPlacement.BelowCraftButtonRightAligned, state.Placement);
    }
}
