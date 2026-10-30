namespace BaseStorage.Core.Tests.StationActions;

using System;
using BaseStorage.Core.StationPanel;
using BaseStorage.Core.Tests.StationPanel;

/// <summary>
/// GW-U-1760..1766 (AC-1, contract 9.2): the UI scale of a screen, the canvas corner rectangle, the union of two rectangles and the
/// local size. The nine-corner replay (GW-U-1763) feeds the real Core layout the scale that the Plugin computes. Red until
/// StationPanelGeometry is implemented.
/// </summary>
public sealed class StationPanelGeometryTests
{
    // GW-U-1760 (AC-1, 9.2): the reference sizes give the canvas scale factor back.
    [Theory]
    [InlineData(1920, 1080, 1.2f, 1.2f)]
    // Edited by S-E004-012 (ADR-GWIN-136): contract GW-U-1760 gives UiScaleOf(3840, 2160, 2.4f) = 1.2f (the factor is divided by the screen ratio), so the expected value is 1.2f.
    [InlineData(3840, 2160, 2.4f, 1.2f)]
    [InlineData(2560, 1080, 1.2f, 1.2f)]
    public void UiScaleOfReferenceSizesGivesTheFactor(int width, int height, float canvasScaleFactor, float expected)
    {
        AssertNear(expected, UiScale(width, height, canvasScaleFactor));
    }

    // GW-U-1761 (AC-1, 9.2): the smallest supported size gives the 0.8 minimum.
    [Fact]
    public void UiScaleOfTheMinimumSizeGivesEightTenths()
    {
        AssertNear(0.8f, UiScale(1280, 720, 0.53333336f));
    }

    // GW-U-1762 (AC-1, 9.2): zero sizes and a zero, negative, NaN or infinite factor give 1.
    [Theory]
    [InlineData(0, 1080, 1f)]
    [InlineData(1920, 0, 1f)]
    [InlineData(1920, 1080, 0f)]
    [InlineData(1920, 1080, -1f)]
    [InlineData(1920, 1080, float.NaN)]
    [InlineData(1920, 1080, float.PositiveInfinity)]
    public void UiScaleOfInvalidInputGivesOne(int width, int height, float factor)
    {
        Assert.Equal(1f, UiScale(width, height, factor));
    }

    // GW-U-1763 (AC-1, NFR layout, 9.2): each of the nine corners of the 5.2 table, through UiScaleOf into Compute, reproduces fixture A
    // at the right of the craft button, and the canvas is within 0.01 of the contracted canvas size.
    [Theory]
    [InlineData(1280, 720, 0.8f, 2400f, 1350f)]
    [InlineData(1280, 720, 1.0f, 1920f, 1080f)]
    [InlineData(1280, 720, 1.2f, 1600f, 900f)]
    [InlineData(1920, 1080, 0.8f, 2400f, 1350f)]
    [InlineData(1920, 1080, 1.0f, 1920f, 1080f)]
    [InlineData(1920, 1080, 1.2f, 1600f, 900f)]
    [InlineData(3840, 2160, 0.8f, 2400f, 1350f)]
    [InlineData(3840, 2160, 1.0f, 1920f, 1080f)]
    [InlineData(3840, 2160, 1.2f, 1600f, 900f)]
    public void EveryCornerReplaysFixtureAAtTheRightOfTheCraftButton(int width, int height, float scale, float canvasWidth, float canvasHeight)
    {
        float uiScale = UiScale(width, height, Math.Min(width / 1920f, height / 1080f) * scale);

        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.AIn(width, height, uiScale));

        Assert.True(state.Visible);
        Assert.Equal(StationPanelPlacement.RightOfCraftButton, state.Placement);
        Assert.Equal(StationPanelFixtures.ARightOrganize, state.OrganizeButton);
        Assert.Equal(StationPanelFixtures.ARightStatus, state.StatusLine);
        Assert.True(Math.Abs(canvasWidth - state.CanvasWidth) <= 0.01f, "Canvas width " + state.CanvasWidth + " is not " + canvasWidth + ".");
        Assert.True(Math.Abs(canvasHeight - state.CanvasHeight) <= 0.01f, "Canvas height " + state.CanvasHeight + " is not " + canvasHeight + ".");
    }

    // GW-U-1764 (AC-1, 9.2): corners normalise in either order, and a NaN or infinite value gives the default rectangle.
    [Fact]
    public void FromCornersNormalisesAndRejectsNonFiniteValues()
    {
        Assert.Equal(new PanelRect(600f, 244f, 180f, 46f), FromCorners(600f, 244f, 780f, 290f));
        Assert.Equal(new PanelRect(600f, 244f, 180f, 46f), FromCorners(780f, 290f, 600f, 244f));
        Assert.Equal(default(PanelRect), FromCorners(float.NaN, 0f, 1f, 1f));
        Assert.Equal(default(PanelRect), FromCorners(0f, 0f, float.PositiveInfinity, 1f));
    }

    // GW-U-1765 (AC-1, 9.2): a default side yields the other rectangle, and overlapping and disjoint boxes give exact bounding boxes.
    [Fact]
    public void UnionIsTheExactBoundingBox()
    {
        var box = new PanelRect(600f, 244f, 180f, 46f);
        Assert.Equal(box, Union(default, box));
        Assert.Equal(box, Union(box, default));
        Assert.Equal(new PanelRect(360f, 310f, 420f, 80f), Union(new PanelRect(360f, 310f, 100f, 80f), new PanelRect(500f, 310f, 280f, 80f)));
        Assert.Equal(new PanelRect(360f, 244f, 420f, 146f), Union(new PanelRect(360f, 310f, 420f, 80f), box));
    }

    // GW-U-1766 (AC-1, 9.2): a canvas length becomes local units by the root and own scales; a zero own scale or a NaN root scale keeps the length.
    [Theory]
    [InlineData(180f, 1.2f, 1.2f, 180f)]
    [InlineData(180f, 0.5f, 1f, 90f)]
    [InlineData(180f, 1f, 0f, 180f)]
    [InlineData(180f, float.NaN, 1f, 180f)]
    public void ToLocalSizeScalesByRootOverOwn(float length, float rootScale, float ownScale, float expected)
    {
        AssertNear(expected, ToLocalSize(length, rootScale, ownScale));
    }

    private static void AssertNear(float expected, float actual) =>
        Assert.True(Math.Abs(expected - actual) <= 1e-5f, "Expected " + expected + " but was " + actual + ".");

    private static float UiScale(int width, int height, float canvasScaleFactor) =>
        (float)StationActionsTypes.CallStatic("StationPanelGeometry", "UiScaleOf", width, height, canvasScaleFactor)!;

    private static PanelRect FromCorners(float x0, float y0, float x1, float y1) =>
        (PanelRect)StationActionsTypes.CallStatic("StationPanelGeometry", "FromCorners", x0, y0, x1, y1)!;

    private static PanelRect Union(PanelRect a, PanelRect b) =>
        (PanelRect)StationActionsTypes.CallStatic("StationPanelGeometry", "Union", a, b)!;

    private static float ToLocalSize(float length, float rootScale, float ownScale) =>
        (float)StationActionsTypes.CallStatic("StationPanelGeometry", "ToLocalSize", length, rootScale, ownScale)!;
}
