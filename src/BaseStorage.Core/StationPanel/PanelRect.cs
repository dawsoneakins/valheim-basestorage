namespace BaseStorage.Core.StationPanel;

using System;

/// <summary>
/// An axis-aligned rectangle in root-canvas units (SP3): the origin is the bottom-left corner, X grows to the right and Y grows up.
/// Width and height are never negative, and every argument is finite. Value equality uses X, Y, Width and Height.
/// </summary>
public readonly struct PanelRect : IEquatable<PanelRect>
{
    /// <summary>Creates a rectangle from its bottom-left corner and its size.</summary>
    /// <param name="x">The left edge. Must be finite.</param>
    /// <param name="y">The bottom edge. Must be finite.</param>
    /// <param name="width">The width. Must be finite and not negative.</param>
    /// <param name="height">The height. Must be finite and not negative.</param>
    public PanelRect(float x, float y, float width, float height)
    {
        Require(x, nameof(x), allowNegative: true);
        Require(y, nameof(y), allowNegative: true);
        Require(width, nameof(width), allowNegative: false);
        Require(height, nameof(height), allowNegative: false);
        // Adding 0f turns negative zero into positive zero (-0f + 0f == +0f) so equal rectangles have equal hash codes and text.
        this.x = x + 0f;
        this.y = y + 0f;
        this.width = width + 0f;
        this.height = height + 0f;
    }

    private readonly float x;
    private readonly float y;
    private readonly float width;
    private readonly float height;

    /// <summary>Gets the left edge.</summary>
    public float X => x;

    /// <summary>Gets the bottom edge.</summary>
    public float Y => y;

    /// <summary>Gets the width, which is never negative.</summary>
    public float Width => width;

    /// <summary>Gets the height, which is never negative.</summary>
    public float Height => height;

    /// <summary>Gets the right edge, which is X plus Width.</summary>
    public float Right => x + width;

    /// <summary>Gets the top edge, which is Y plus Height.</summary>
    public float Top => y + height;

    /// <summary>Tests whether two rectangles overlap with strict comparisons, so rectangles that only share an edge do not overlap.</summary>
    /// <param name="other">The rectangle to test against.</param>
    /// <returns>True when the interiors intersect; otherwise false.</returns>
    public bool Overlaps(PanelRect other) => X < other.Right && other.X < Right && Y < other.Top && other.Y < Top;

    /// <summary>Grows the rectangle by the amount on every side, keeping its centre.</summary>
    /// <param name="amount">The growth on each side. Must be finite and not negative.</param>
    /// <returns>The grown rectangle.</returns>
    public PanelRect Inflate(float amount)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be finite and not negative.");
        }

        return new PanelRect(x - amount, y - amount, width + (2 * amount), height + (2 * amount));
    }

    /// <summary>Compares the four stored values with another rectangle.</summary>
    /// <param name="other">The rectangle to compare with.</param>
    /// <returns>True when X, Y, Width and Height are all equal.</returns>
    public bool Equals(PanelRect other) => x == other.x && y == other.y && width == other.width && height == other.height;

    /// <summary>Compares this rectangle with an object.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when the object is a PanelRect equal to this one.</returns>
    public override bool Equals(object? obj) => obj is PanelRect other && Equals(other);

    /// <summary>Gets a hash code that is equal for equal rectangles.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = x.GetHashCode();
            h = (h * 31) + y.GetHashCode();
            h = (h * 31) + width.GetHashCode();
            h = (h * 31) + height.GetHashCode();
            return h;
        }
    }

    /// <summary>Compares two rectangles for equality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when the rectangles are equal.</returns>
    public static bool operator ==(PanelRect left, PanelRect right) => left.Equals(right);

    /// <summary>Compares two rectangles for inequality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when the rectangles differ.</returns>
    public static bool operator !=(PanelRect left, PanelRect right) => !left.Equals(right);

    private static void Require(float value, string name, bool allowNegative)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || (!allowNegative && value < 0f))
        {
            throw new ArgumentOutOfRangeException(name, value, "Value must be finite" + (allowNegative ? "." : " and not negative."));
        }
    }
}
