namespace BaseStorage.Core.Menu;

using System;

/// <summary>
/// A panel-local rectangle of the base menu (S-QR-002 contract section 2.1). Coordinates are top-anchored and grow down.
/// </summary>
public sealed class MenuRect : IEquatable<MenuRect>
{
    private readonly float x;
    private readonly float top;
    private readonly float width;
    private readonly float height;

    /// <summary>Creates a rectangle.</summary>
    /// <param name="x">The left edge.</param>
    /// <param name="top">The top edge.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is not finite, or width or height is negative; ParamName names it.</exception>
    public MenuRect(float x, float top, float width, float height)
    {
        if (!IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Must be finite.");
        }

        if (!IsFinite(top))
        {
            throw new ArgumentOutOfRangeException(nameof(top), "Must be finite.");
        }

        if (!IsFinite(width) || width < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Must be finite and not negative.");
        }

        if (!IsFinite(height) || height < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Must be finite and not negative.");
        }

        this.x = x == 0f ? 0f : x;
        this.top = top == 0f ? 0f : top;
        this.width = width == 0f ? 0f : width;
        this.height = height == 0f ? 0f : height;
    }

    /// <summary>Gets the left edge.</summary>
    public float X => x;

    /// <summary>Gets the top edge.</summary>
    public float Top => top;

    /// <summary>Gets the width.</summary>
    public float Width => width;

    /// <summary>Gets the height.</summary>
    public float Height => height;

    /// <summary>Gets the right edge (X + Width).</summary>
    public float Right => x + width;

    /// <summary>Gets the bottom edge (Top + Height).</summary>
    public float Bottom => top + height;

    /// <summary>Compares two rectangles by value.</summary>
    /// <param name="left">The left rectangle.</param>
    /// <param name="right">The right rectangle.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(MenuRect? left, MenuRect? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Compares two rectangles by value.</summary>
    /// <param name="left">The left rectangle.</param>
    /// <param name="right">The right rectangle.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(MenuRect? left, MenuRect? right) => !(left == right);

    /// <summary>Returns true when the open interiors intersect; rectangles that share only an edge do not overlap.</summary>
    /// <param name="other">The rectangle to compare with.</param>
    /// <returns>True when the interiors intersect.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
    public bool Overlaps(MenuRect other)
    {
        if (other is null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        return x < other.Right && other.x < Right && top < other.Bottom && other.top < Bottom;
    }

    /// <summary>Compares the four values.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>True when all four values are equal.</returns>
    public bool Equals(MenuRect? other) =>
        other is not null && x == other.x && top == other.top && width == other.width && height == other.height;

    /// <summary>Compares with another object.</summary>
    /// <param name="obj">The other object.</param>
    /// <returns>True when it is an equal rectangle.</returns>
    public override bool Equals(object? obj) => Equals(obj as MenuRect);

    /// <summary>Gets the hash code of the four values.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = x.GetHashCode();
            h = (h * 31) + top.GetHashCode();
            h = (h * 31) + width.GetHashCode();
            return (h * 31) + height.GetHashCode();
        }
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
