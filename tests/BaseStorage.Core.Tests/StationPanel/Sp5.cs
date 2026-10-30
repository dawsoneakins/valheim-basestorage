namespace BaseStorage.Core.Tests.StationPanel;

using BaseStorage.Core.StationPanel;

/// <summary>
/// An independent re-implementation of contract SP5 (non-overlap) and of the 4.1 hidden shape, on raw floats (ADR-GWIN-041).
/// It never calls PanelRect.Overlaps or PanelRect.Inflate, so a fault in those two members cannot hide itself. It works in double
/// arithmetic on the rectangle parts, and it reads only X, Y, Width and Height of each rectangle.
/// </summary>
internal static class Sp5
{
    private const double Gap = 8.0;
    private const float Tolerance = 0.01f;

    /// <summary>Asserts SP5 for a visible state, or the full hidden shape of 4.1 for a hidden state.</summary>
    /// <param name="input">The input that produced the state.</param>
    /// <param name="state">The state to check.</param>
    public static void AssertHolds(StationPanelInput input, StationPanelState state)
    {
        if (!state.Visible)
        {
            AssertHiddenShape(state);
            return;
        }

        Assert.Equal(StationPanelHiddenReason.None, state.HiddenReason);
        Assert.NotEqual(StationPanelPlacement.None, state.Placement);

        PanelRect organize = state.OrganizeButton;
        PanelRect deposit = state.DepositButton;
        PanelRect status = state.StatusLine;
        PanelRect[] outputs = { organize, deposit, status };

        foreach (PanelRect vanilla in new[] { input.RecipeList, input.Requirements, input.CraftButton })
        {
            foreach (PanelRect output in outputs)
            {
                Assert.False(Collides(output, vanilla), $"Output {Describe(output)} collides with vanilla {Describe(vanilla)} inflated by 8.");
            }
        }

        foreach (PanelRect output in outputs)
        {
            Assert.True(InsideCanvas(output, state.CanvasWidth, state.CanvasHeight), $"Output {Describe(output)} leaves the canvas.");
        }

        Assert.Equal(organize.Y, deposit.Y);
        Assert.Equal(organize.Width, deposit.Width);
        Assert.Equal(organize.Height, deposit.Height);
        Assert.InRange((double)deposit.X, (double)organize.X + organize.Width + Gap - Tolerance, (double)organize.X + organize.Width + Gap + Tolerance);
        Assert.Equal(24f, status.Height);
        Assert.InRange((double)status.Y + status.Height, (double)organize.Y - 16.0 - Tolerance, (double)organize.Y - 16.0 + Tolerance);
        Assert.Equal(organize.X, status.X);
    }

    /// <summary>Asserts the full hidden shape of contract 4.1: the flags, the default rectangles, the null texts and the canvas is set.</summary>
    /// <param name="state">The hidden state to check.</param>
    public static void AssertHiddenShape(StationPanelState state)
    {
        Assert.False(state.Visible);
        Assert.NotEqual(StationPanelHiddenReason.None, state.HiddenReason);
        Assert.Equal(StationPanelPlacement.None, state.Placement);
        Assert.Equal(default(PanelRect), state.OrganizeButton);
        Assert.Equal(default(PanelRect), state.DepositButton);
        Assert.Equal(default(PanelRect), state.StatusLine);
        Assert.False(state.ButtonsEnabled);
        Assert.Equal(StationPanelDisabledReason.None, state.DisabledReason);
        Assert.Null(state.TooltipKey);
        Assert.Null(state.TooltipText);
        Assert.Null(state.StatusLineText);
    }

    /// <summary>Tests the SP5 overlap of an output rectangle with a vanilla rectangle inflated by 8, using strict comparisons on raw floats.</summary>
    /// <param name="output">The output rectangle.</param>
    /// <param name="vanilla">The vanilla rectangle, before inflation.</param>
    /// <returns>True when the output overlaps the inflated vanilla rectangle.</returns>
    public static bool Collides(PanelRect output, PanelRect vanilla)
    {
        double left = (double)vanilla.X - Gap;
        double right = (double)vanilla.X + vanilla.Width + Gap;
        double bottom = (double)vanilla.Y - Gap;
        double top = (double)vanilla.Y + vanilla.Height + Gap;

        double outLeft = output.X;
        double outRight = (double)output.X + output.Width;
        double outBottom = output.Y;
        double outTop = (double)output.Y + output.Height;

        return outLeft < right && left < outRight && outBottom < top && bottom < outTop;
    }

    /// <summary>Tests that a rectangle lies inside the canvas (SP5 part a).</summary>
    /// <param name="rect">The rectangle.</param>
    /// <param name="canvasWidth">The canvas width.</param>
    /// <param name="canvasHeight">The canvas height.</param>
    /// <returns>True when the rectangle is inside the canvas.</returns>
    public static bool InsideCanvas(PanelRect rect, float canvasWidth, float canvasHeight)
    {
        return rect.X >= 0f && rect.Y >= 0f && (double)rect.X + rect.Width <= canvasWidth && (double)rect.Y + rect.Height <= canvasHeight;
    }

    /// <summary>Tests whether a set of three output rectangles satisfies SP5 against the vanilla rectangles of an input.</summary>
    /// <param name="input">The input whose vanilla rectangles are checked.</param>
    /// <param name="canvasWidth">The canvas width.</param>
    /// <param name="canvasHeight">The canvas height.</param>
    /// <param name="organize">The Organize rectangle.</param>
    /// <param name="deposit">The Deposit rectangle.</param>
    /// <param name="status">The status line rectangle.</param>
    /// <returns>True when every output is inside the canvas and clear of every vanilla rectangle.</returns>
    public static bool Fits(StationPanelInput input, float canvasWidth, float canvasHeight, PanelRect organize, PanelRect deposit, PanelRect status)
    {
        PanelRect[] outputs = { organize, deposit, status };
        foreach (PanelRect output in outputs)
        {
            if (!InsideCanvas(output, canvasWidth, canvasHeight))
            {
                return false;
            }

            foreach (PanelRect vanilla in new[] { input.RecipeList, input.Requirements, input.CraftButton })
            {
                if (Collides(output, vanilla))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static string Describe(PanelRect rect) => "(" + rect.X + "," + rect.Y + "," + rect.Width + "," + rect.Height + ")";
}
