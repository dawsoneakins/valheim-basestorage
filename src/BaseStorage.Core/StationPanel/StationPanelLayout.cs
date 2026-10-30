namespace BaseStorage.Core.StationPanel;

using System;
using BaseStorage.Core.Ui;

/// <summary>
/// Computes where the station buttons go, whether they are enabled, the tooltip and the status line (contract section 4). The computation
/// is pure and deterministic, and it is the only public behaviour of the StationPanel namespace.
/// </summary>
public static class StationPanelLayout
{
    /// <summary>The reference screen width in pixels (breakpoint.ref-width).</summary>
    public const float ReferenceWidth = 1920f;

    /// <summary>The reference screen height in pixels (breakpoint.ref-height).</summary>
    public const float ReferenceHeight = 1080f;

    /// <summary>The gap between the two buttons, in canvas units (spacing.2).</summary>
    public const float ButtonGap = 8f;

    /// <summary>The gap between a button and any vanilla rectangle, in canvas units (a11y.min-target-gap).</summary>
    public const float VanillaGap = 8f;

    /// <summary>The gap between the button row and the status line, in canvas units (spacing.4).</summary>
    public const float StatusLineGap = 16f;

    /// <summary>The height of the status line, in canvas units (spacing.6).</summary>
    public const float StatusLineHeight = 24f;

    /// <summary>The minimum button width, in canvas units (a11y.min-hit-width).</summary>
    public const float MinButtonWidth = 120f;

    /// <summary>The minimum button height, in canvas units (a11y.min-hit-height).</summary>
    public const float MinButtonHeight = 32f;

    /// <summary>Gets the candidate that follows a placement in the fixed order of contract 4.2, or None after the last one.</summary>
    private static StationPanelPlacement NextCandidate(StationPanelPlacement placement) => placement switch
    {
        StationPanelPlacement.RightOfCraftButton => StationPanelPlacement.BelowCraftButton,
        StationPanelPlacement.BelowCraftButton => StationPanelPlacement.LeftOfCraftButton,
        StationPanelPlacement.LeftOfCraftButton => StationPanelPlacement.AboveControls,
        StationPanelPlacement.AboveControls => StationPanelPlacement.RightOfControls,
        StationPanelPlacement.RightOfControls => StationPanelPlacement.BelowCraftButtonRightAligned,
        _ => StationPanelPlacement.None,
    };

    /// <summary>Computes the state of the buttons and the status line for an input.</summary>
    /// <param name="input">The panel and base state. Must not be null.</param>
    /// <returns>The computed state. It never throws for a constructed input.</returns>
    public static StationPanelState Compute(StationPanelInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        float scale = Math.Min(input.ScreenWidth / ReferenceWidth, input.ScreenHeight / ReferenceHeight) * input.UiScale;
        float canvasWidth = input.ScreenWidth / scale;
        float canvasHeight = input.ScreenHeight / scale;

        if (!input.StationButtons)
        {
            return Hidden(StationPanelHiddenReason.StationButtonsOff, canvasWidth, canvasHeight);
        }

        if (input.StationCount == 0)
        {
            return Hidden(StationPanelHiddenReason.NotInBase, canvasWidth, canvasHeight);
        }

        PanelRect c = input.CraftButton;
        PanelRect q = input.Requirements;
        PanelRect l = input.RecipeList;
        float uRight = Math.Max(l.Right, Math.Max(q.Right, c.Right));
        float uTop = Math.Max(l.Top, Math.Max(q.Top, c.Top));

        float bw = Math.Max(c.Width, MinButtonWidth);
        float bh = Math.Max(c.Height, MinButtonHeight);
        float rowW = (2 * bw) + ButtonGap;
        float statusDrop = StatusLineGap + StatusLineHeight;

        PanelRect[] avoid = { l, q, c };

        for (StationPanelPlacement placement = StationPanelPlacement.RightOfCraftButton; placement != StationPanelPlacement.None; placement = NextCandidate(placement))
        {
            float rx;
            float ry;
            switch (placement)
            {
                case StationPanelPlacement.RightOfCraftButton:
                    rx = c.Right + VanillaGap;
                    ry = c.Y;
                    break;
                case StationPanelPlacement.BelowCraftButton:
                    rx = c.X;
                    ry = c.Y - VanillaGap - bh;
                    break;
                case StationPanelPlacement.LeftOfCraftButton:
                    rx = c.X - VanillaGap - rowW;
                    ry = c.Y;
                    break;
                case StationPanelPlacement.AboveControls:
                    rx = c.X;
                    ry = uTop + VanillaGap + statusDrop;
                    break;
                case StationPanelPlacement.RightOfControls:
                    rx = uRight + VanillaGap;
                    ry = c.Y;
                    break;
                default:
                    rx = c.Right - rowW;
                    ry = (l.Height > 0f ? Math.Min(c.Y, l.Y) : c.Y) - VanillaGap - bh;
                    break;
            }

            if (TryCandidate(rx, ry, bw, bh, rowW, statusDrop, out PanelRect organize, out PanelRect deposit, out PanelRect status)
                && Fits(organize, canvasWidth, canvasHeight, avoid)
                && Fits(deposit, canvasWidth, canvasHeight, avoid)
                && Fits(status, canvasWidth, canvasHeight, avoid))
            {
                return Shown(input, placement, canvasWidth, canvasHeight, organize, deposit, status);
            }
        }

        return Hidden(StationPanelHiddenReason.NoRoom, canvasWidth, canvasHeight);
    }

    /// <summary>
    /// Builds the three rectangles for one candidate origin. Every value that feeds the PanelRect constructor, and every edge derived from
    /// the rectangles (Right and Top), must be finite; otherwise the candidate is skipped so that Compute never throws.
    /// </summary>
    private static bool TryCandidate(
        float rx,
        float ry,
        float bw,
        float bh,
        float rowW,
        float statusDrop,
        out PanelRect organize,
        out PanelRect deposit,
        out PanelRect status)
    {
        organize = default;
        deposit = default;
        status = default;
        float depositX = rx + bw + ButtonGap;
        float statusY = ry - statusDrop;
        if (!IsFinite(rx) || !IsFinite(ry) || !IsFinite(bw) || !IsFinite(bh) || !IsFinite(rowW)
            || !IsFinite(depositX) || !IsFinite(statusY)
            || !IsFinite(rx + bw) || !IsFinite(depositX + bw) || !IsFinite(rx + rowW)
            || !IsFinite(ry + bh) || !IsFinite(statusY + StatusLineHeight))
        {
            return false;
        }

        organize = new PanelRect(rx, ry, bw, bh);
        deposit = new PanelRect(depositX, ry, bw, bh);
        status = new PanelRect(rx, statusY, rowW, StatusLineHeight);
        return IsFinite(organize.Right) && IsFinite(organize.Top)
            && IsFinite(deposit.Right) && IsFinite(deposit.Top)
            && IsFinite(status.Right) && IsFinite(status.Top);
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    // Double arithmetic is deliberate, for exactness rather than overflow: PanelRect.Inflate rounds x - 8, y - 8 and width + 16 to float, so the
    // inflated edge can land one ulp inside the true 8-unit gap and the SP5 gap check (which is exact) would then fail. The strict overlap
    // test mirrors PanelRect.Overlaps; keep the two in step.
    private static bool Fits(PanelRect rect, float canvasWidth, float canvasHeight, PanelRect[] vanilla)
    {
        double right = (double)rect.X + rect.Width;
        double top = (double)rect.Y + rect.Height;
        if (rect.X < 0f || rect.Y < 0f || right > canvasWidth || top > canvasHeight)
        {
            return false;
        }

        for (int i = 0; i < vanilla.Length; i++)
        {
            PanelRect v = vanilla[i];
            double vLeft = (double)v.X - VanillaGap;
            double vRight = (double)v.X + v.Width + VanillaGap;
            double vBottom = (double)v.Y - VanillaGap;
            double vTop = (double)v.Y + v.Height + VanillaGap;
            if (rect.X < vRight && vLeft < right && rect.Y < vTop && vBottom < top)
            {
                return false;
            }
        }

        return true;
    }

    private static StationPanelState Hidden(StationPanelHiddenReason reason, float canvasWidth, float canvasHeight) =>
        new StationPanelState(
            false,
            reason,
            StationPanelPlacement.None,
            canvasWidth,
            canvasHeight,
            default,
            default,
            default,
            false,
            StationPanelDisabledReason.None,
            null,
            null,
            null);

    private static StationPanelState Shown(
        StationPanelInput input,
        StationPanelPlacement placement,
        float canvasWidth,
        float canvasHeight,
        PanelRect organize,
        PanelRect deposit,
        PanelRect status)
    {
        StringTable t = StringTable.English;
        int n = input.StationCount;
        int m = input.EligibleChestCount;
        string statusText = t.Format(StringKeys.StationActionsStatusLine + "." + Plural.Form(n) + "." + Plural.Form(m), n, m)
            + t.Format(StringKeys.StationActionsStatusLineFill, input.FillPercent);

        StationPanelDisabledReason reason = StationPanelDisabledReason.None;
        string? key = null;
        string? tip = null;
        if (input.OrganizeRunning)
        {
            reason = StationPanelDisabledReason.OrganizeInProgress;
            key = StringKeys.StationActionsDisabledInProgress;
            tip = t.Format(key, SingleLine.Of(input.OrganizeHolderName!));
        }
        else if (m == 0)
        {
            reason = StationPanelDisabledReason.NoEligibleChests;
            key = StringKeys.StationActionsDisabledNoChests;
            tip = t.Get(key);
        }

        return new StationPanelState(
            true,
            StationPanelHiddenReason.None,
            placement,
            canvasWidth,
            canvasHeight,
            organize,
            deposit,
            status,
            reason == StationPanelDisabledReason.None,
            reason,
            key,
            tip,
            statusText);
    }
}
