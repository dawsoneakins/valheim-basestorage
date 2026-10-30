namespace BaseStorage.Core.Tests.StationPanel;

using System;
using System.Collections.Generic;
using BaseStorage.Core.StationPanel;

/// <summary>AC-1 (ADR-GWIN-041): an SP5 sweep over the supported grid, collecting every failure rather than stopping at the first.</summary>
public sealed class StationPanelInvariantTests
{
    private static readonly int[] Widths = { 1280, 1366, 1600, 1920, 2560, 3440, 3840 };
    private static readonly int[] Heights = { 720, 768, 900, 1080, 1440, 1600, 2160 };

    private static float ScaleAt(int index) => 0.8f + index * 0.05f;

    // GW-U-528 (AC-1): fixture A, 441 grid points. Every point is visible with RightOfCraftButton and A-right, and holds SP5.
    [Fact]
    public void FixtureAIsRightOfTheCraftButtonAtEveryGridPoint()
    {
        var failures = new List<string>();
        foreach (int width in Widths)
        {
            foreach (int height in Heights)
            {
                for (int i = 0; i <= 8; i++)
                {
                    StationPanelInput input = StationPanelFixtures.AIn(width, height, ScaleAt(i));
                    StationPanelState state = StationPanelLayout.Compute(input);
                    string where = $"{width}x{height}@{ScaleAt(i)}";
                    if (!state.Visible || state.Placement != StationPanelPlacement.RightOfCraftButton)
                    {
                        failures.Add(where + ": not visible on the right of the craft button.");
                        continue;
                    }

                    if (state.OrganizeButton != StationPanelFixtures.ARightOrganize
                        || state.DepositButton != StationPanelFixtures.ARightDeposit
                        || state.StatusLine != StationPanelFixtures.ARightStatus)
                    {
                        failures.Add(where + ": rectangles differ from A-right.");
                        continue;
                    }

                    Exception? sp5 = Record.Exception(() => Sp5.AssertHolds(input, state));
                    if (sp5 is not null)
                    {
                        failures.Add(where + ": " + sp5.Message);
                    }
                }
            }
        }

        Assert.Empty(failures);
    }

    // GW-U-528 (AC-1): fixture B, 441 grid points. Every point is visible and holds SP5 (the placement may be either candidate).
    [Fact]
    public void FixtureBIsVisibleAndHoldsSp5AtEveryGridPoint()
    {
        var failures = new List<string>();
        foreach (int width in Widths)
        {
            foreach (int height in Heights)
            {
                for (int i = 0; i <= 8; i++)
                {
                    StationPanelInput input = StationPanelFixtures.BIn(width, height, ScaleAt(i));
                    StationPanelState state = StationPanelLayout.Compute(input);
                    string where = $"{width}x{height}@{ScaleAt(i)}";
                    if (!state.Visible)
                    {
                        failures.Add(where + ": hidden with " + state.HiddenReason + ".");
                        continue;
                    }

                    Exception? sp5 = Record.Exception(() => Sp5.AssertHolds(input, state));
                    if (sp5 is not null)
                    {
                        failures.Add(where + ": " + sp5.Message);
                    }
                }
            }
        }

        Assert.Empty(failures);
    }
}
