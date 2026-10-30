namespace BaseStorage.Plugin.StationActions;

using System;
using BaseStorage.Core.StationPanel;

/// <summary>The pure canvas geometry helpers of the station panel (contract S-E004-012 section 3.2). No member throws.</summary>
internal static class StationPanelGeometry
{
    /// <summary>The reference canvas width; equals the Core layout reference.</summary>
    internal const float ReferenceWidth = 1920f;

    /// <summary>The reference canvas height; equals the Core layout reference.</summary>
    internal const float ReferenceHeight = 1080f;

    /// <summary>Computes the UI scale that makes the Core canvas equal the real root canvas.</summary>
    /// <param name="screenWidth">The screen width in pixels.</param>
    /// <param name="screenHeight">The screen height in pixels.</param>
    /// <param name="canvasScaleFactor">The scale factor of the root canvas.</param>
    /// <returns>The scale; 1 for invalid input.</returns>
    internal static float UiScaleOf(int screenWidth, int screenHeight, float canvasScaleFactor)
    {
        if (screenWidth < 1 || screenHeight < 1 || !IsFinite(canvasScaleFactor) || canvasScaleFactor <= 0f)
        {
            return 1f;
        }

        return canvasScaleFactor / Math.Min(screenWidth / ReferenceWidth, screenHeight / ReferenceHeight);
    }

    /// <summary>Builds the rect spanned by two corners, normalised per axis.</summary>
    /// <param name="x0">The first corner x.</param>
    /// <param name="y0">The first corner y.</param>
    /// <param name="x1">The second corner x.</param>
    /// <param name="y1">The second corner y.</param>
    /// <returns>The rect; default when any value is NaN or infinite.</returns>
    internal static PanelRect FromCorners(float x0, float y0, float x1, float y1)
    {
        if (!IsFinite(x0) || !IsFinite(y0) || !IsFinite(x1) || !IsFinite(y1))
        {
            return default;
        }

        float left = Math.Min(x0, x1);
        float bottom = Math.Min(y0, y1);
        return new PanelRect(left, bottom, Math.Max(x0, x1) - left, Math.Max(y0, y1) - bottom);
    }

    /// <summary>Builds the bounding box of two rects; a default rect counts as empty.</summary>
    /// <param name="a">The first rect.</param>
    /// <param name="b">The second rect.</param>
    /// <returns>The bounding box.</returns>
    internal static PanelRect Union(PanelRect a, PanelRect b)
    {
        if (a == default)
        {
            return b;
        }

        if (b == default)
        {
            return a;
        }

        float left = Math.Min(a.X, b.X);
        float bottom = Math.Min(a.Y, b.Y);
        return new PanelRect(left, bottom, Math.Max(a.Right, b.Right) - left, Math.Max(a.Top, b.Top) - bottom);
    }

    /// <summary>Converts a canvas-unit length to the local units of a control.</summary>
    /// <param name="length">The length in canvas units.</param>
    /// <param name="rootScale">The lossy scale of the root canvas.</param>
    /// <param name="ownScale">The lossy scale of the control.</param>
    /// <returns>length * rootScale / ownScale; the length itself when a scale is 0, NaN or infinite.</returns>
    internal static float ToLocalSize(float length, float rootScale, float ownScale)
    {
        if (rootScale == 0f || ownScale == 0f || !IsFinite(rootScale) || !IsFinite(ownScale))
        {
            return length;
        }

        return length * rootScale / ownScale;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
