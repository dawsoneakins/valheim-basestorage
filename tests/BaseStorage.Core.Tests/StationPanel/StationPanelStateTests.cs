namespace BaseStorage.Core.Tests.StationPanel;

using BaseStorage.Core.StationPanel;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-4 (R5, R6): StationPanelState value semantics, with each kind of difference giving inequality.</summary>
public sealed class StationPanelStateTests
{
    // GW-U-546 (AC-4): two results from equal inputs are equal on every path, with equal hash codes.
    [Fact]
    public void ResultsFromEqualInputsAreEqual()
    {
        ValueSemantics.AssertEqual(
            StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f)),
            StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f)));
    }

    // GW-U-546 (AC-4): a difference in the canvas gives inequality. Reference (1920x1080@1.0) and 1920x1080@1.2 differ only in the canvas, since
    // fixture A is right of the craft button at both. Note that 2560x1440@1.0 has the same canvas as the reference, so it cannot be used here.
    [Fact]
    public void ResultsThatDifferOnlyInTheCanvasAreUnequal()
    {
        StationPanelState reference = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f));
        StationPanelState scaled = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1.2f));
        Assert.Equal(reference.OrganizeButton, scaled.OrganizeButton);
        Assert.NotEqual(reference.CanvasWidth, scaled.CanvasWidth);
        ValueSemantics.AssertNotEqual(reference, scaled);
    }

    [Fact]
    public void ResultsThatDifferInPlacementAreUnequal()
    {
        // Same canvas (1920x1080@1.0) as the reference; only the craft button moves. At x=1700 the right candidate (status right edge
        // 2256) and the below candidate (deposit right edge 2068) run out of canvas, so the left candidate (SP5, contract 4.2) wins.
        StationPanelState right = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f));
        StationPanelState left = StationPanelLayout.Compute(StationPanelFixtures.In(1920, 1080, 1f, StationPanelFixtures.ARecipes, StationPanelFixtures.AReqs, new PanelRect(1700f, 244f, 180f, 46f)));
        Assert.Equal(StationPanelPlacement.LeftOfCraftButton, left.Placement);
        Assert.NotEqual(right.Placement, left.Placement);
        ValueSemantics.AssertNotEqual(right, left);
    }

    [Fact]
    public void ResultsThatDifferInDisabledReasonAreUnequal()
    {
        ValueSemantics.AssertNotEqual(
            StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f)),
            StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik")));
    }

    [Fact]
    public void ResultsThatDifferInHolderNameAreUnequal()
    {
        ValueSemantics.AssertNotEqual(
            StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik")),
            StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Bjorn")));
    }

    [Fact]
    public void ResultsThatDifferInChestCountAreUnequal()
    {
        ValueSemantics.AssertNotEqual(
            StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, chests: 41)),
            StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, chests: 40)));
    }

    [Fact]
    public void ResultsThatDifferInHiddenReasonAreUnequal()
    {
        ValueSemantics.AssertNotEqual(
            StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, on: false)),
            StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, stations: 0, chests: 0)));
    }

    [Fact]
    public void NullHandlingFollowsTheOperators()
    {
        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f));
        ValueSemantics.AssertNullHandling(typeof(StationPanelState), state);
    }
}
