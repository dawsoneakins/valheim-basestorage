namespace BaseStorage.Core.Menu;

using System;

/// <summary>
/// One button of the base menu as the layout sees it (S-QR-002 contract section 2.1): its measured width and whether it starts a row.
/// </summary>
public sealed class MenuButtonSpec : IEquatable<MenuButtonSpec>
{
    private readonly float width;
    private readonly bool startsRow;
    private readonly bool hasHeading;

    /// <summary>Creates a button spec.</summary>
    /// <param name="width">The button's measured width.</param>
    /// <param name="startsRow">True when this button begins a new row unless the current row is empty.</param>
    /// <param name="hasHeading">True when a group's heading sits above this button, which always starts a new row.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is not finite or not above 0.</exception>
    public MenuButtonSpec(float width, bool startsRow, bool hasHeading = false)
    {
        if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Must be finite and above 0.");
        }

        this.width = width;
        this.startsRow = startsRow;
        this.hasHeading = hasHeading;
    }

    /// <summary>Gets the measured width.</summary>
    public float Width => width;

    /// <summary>Gets a value that is true when this button begins a new row.</summary>
    public bool StartsRow => startsRow;

    /// <summary>Gets a value that is true when a group's heading sits above this button.</summary>
    public bool HasHeading => hasHeading;

    /// <summary>Compares two specs by value.</summary>
    /// <param name="left">The left spec.</param>
    /// <param name="right">The right spec.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(MenuButtonSpec? left, MenuButtonSpec? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Compares two specs by value.</summary>
    /// <param name="left">The left spec.</param>
    /// <param name="right">The right spec.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(MenuButtonSpec? left, MenuButtonSpec? right) => !(left == right);

    /// <summary>Compares the two values.</summary>
    /// <param name="other">The other spec.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(MenuButtonSpec? other) => other is not null && width == other.width && startsRow == other.startsRow && hasHeading == other.hasHeading;

    /// <summary>Compares with another object.</summary>
    /// <param name="obj">The other object.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as MenuButtonSpec);

    /// <summary>Gets the hash code of the two values.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return (width.GetHashCode() * 31) + (startsRow ? 1 : 0);
        }
    }
}
