namespace BaseStorage.Core.Tests.StationPanel;

using BaseStorage.Core.StationPanel;

/// <summary>AC-1 (contract 4.3): fixture A is visible, on the right of the craft button, and holds SP5 across the supported grid.</summary>
public sealed class StationPanelRangeTests
{
    public static TheoryData<int, int, float> SupportedGrid()
    {
        var data = new TheoryData<int, int, float>();
        foreach (int width in new[] { 1280, 1920, 3840 })
        {
            foreach (int height in new[] { 720, 1080, 2160 })
            {
                foreach (float scale in new[] { 0.8f, 1.0f, 1.2f })
                {
                    data.Add(width, height, scale);
                }
            }
        }

        return data;
    }

    // GW-U-523 (AC-1, contract 4.3): every combination of screen width {1280, 1920, 3840}, screen height {720, 1080, 2160} and UI scale {0.8, 1.0, 1.2}.
    // Fixture A gives a visible result, RightOfCraftButton with A-right, enabled, no tooltip and the status text for 6 stations and 41 chests.
    [Theory]
    [MemberData(nameof(SupportedGrid))]
    public void FixtureAIsVisibleOnTheRightAcrossTheSupportedRange(int width, int height, float scale)
    {
        StationPanelInput input = StationPanelFixtures.AIn(width, height, scale);
        StationPanelState state = StationPanelLayout.Compute(input);

        Assert.True(state.Visible, $"{width}x{height}@{scale} must be visible.");
        Assert.Equal(StationPanelPlacement.RightOfCraftButton, state.Placement);
        Assert.Equal(StationPanelFixtures.ARightOrganize, state.OrganizeButton);
        Assert.Equal(StationPanelFixtures.ARightDeposit, state.DepositButton);
        Assert.Equal(StationPanelFixtures.ARightStatus, state.StatusLine);
        Assert.True(state.ButtonsEnabled);
        Assert.Equal(StationPanelDisabledReason.None, state.DisabledReason);
        Assert.Null(state.TooltipKey);
        Assert.Null(state.TooltipText);
        Assert.Equal(StationPanelFixtures.Status6Stations41Chests, state.StatusLineText);
        Sp5.AssertHolds(input, state);
    }
}
