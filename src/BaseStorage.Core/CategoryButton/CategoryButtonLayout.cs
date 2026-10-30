namespace BaseStorage.Core.CategoryButton;

using System;

/// <summary>
/// Places the Category button of the open-chest panel beside the vanilla Take All / Stack All row (contract S-E004-004 section 2,
/// DESIGN-SYSTEM section 7). The clone keeps the template's anchors, pivot and size, so only the anchored x coordinate is decided here:
/// a right-anchored row grows to the left of its leftmost sibling, otherwise to the right of its rightmost sibling, with one gap.
/// Every member is pure.
/// </summary>
public static class CategoryButtonLayout
{
    /// <summary>The gap between the clone and its sibling, in UI units (DESIGN-SYSTEM spacing.2).</summary>
    public const float Gap = 8f;

    /// <summary>Tells whether the row grows to the left: a right-anchored sibling would push a clone placed to its right out of the panel.</summary>
    /// <param name="anchorMinX">The sibling's minimum anchor x, from 0 (left edge) to 1 (right edge).</param>
    /// <returns>True when the anchor is past the middle; false otherwise and for NaN.</returns>
    public static bool GrowsLeft(float anchorMinX) => anchorMinX > 0.5f;

    /// <summary>Picks the outermost of two sibling positions in the growth direction.</summary>
    /// <param name="firstX">The anchored x of one sibling.</param>
    /// <param name="secondX">The anchored x of the other sibling; NaN when that sibling is missing.</param>
    /// <param name="growsLeft">True to pick the smaller value, false to pick the larger.</param>
    /// <returns>The outermost x; the other argument when one is NaN.</returns>
    public static float OuterX(float firstX, float secondX, bool growsLeft)
    {
        if (float.IsNaN(firstX))
        {
            return secondX;
        }

        if (float.IsNaN(secondX))
        {
            return firstX;
        }

        return growsLeft ? Math.Min(firstX, secondX) : Math.Max(firstX, secondX);
    }

    /// <summary>Computes the anchored x of the clone next to the outermost sibling.</summary>
    /// <param name="outerX">The anchored x of the outermost sibling.</param>
    /// <param name="width">The width of one button (sibling and clone share it).</param>
    /// <param name="growsLeft">True to place the clone to the left, false to the right.</param>
    /// <param name="gap">The gap between the two buttons.</param>
    /// <returns><c>outerX - width - gap</c> when growing left, else <c>outerX + width + gap</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="outerX"/> is NaN or when <paramref name="width"/> or <paramref name="gap"/> is negative or NaN.</exception>
    public static float AnchoredX(float outerX, float width, bool growsLeft, float gap)
    {
        if (float.IsNaN(outerX))
        {
            throw new ArgumentOutOfRangeException(nameof(outerX));
        }

        if (float.IsNaN(width) || width < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (float.IsNaN(gap) || gap < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(gap));
        }

        return growsLeft ? outerX - width - gap : outerX + width + gap;
    }
}
