namespace BaseStorage.Core.Persistence;

using System;

/// <summary>A slot of the player inventory grid: X is the column (0 = left), Y the row (0 = top).</summary>
public readonly struct InventorySlot : IEquatable<InventorySlot>, IComparable<InventorySlot>
{
    /// <summary>Creates a slot.</summary>
    /// <param name="x">The column, 0 to 255.</param>
    /// <param name="y">The row, 0 to 255.</param>
    public InventorySlot(int x, int y)
    {
        if (x < 0 || x > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "The column must be 0 to 255.");
        }

        if (y < 0 || y > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(y), "The row must be 0 to 255.");
        }

        X = x;
        Y = y;
    }

    /// <summary>Gets the column.</summary>
    public int X { get; }

    /// <summary>Gets the row.</summary>
    public int Y { get; }

    /// <summary>Compares by row ascending, then column ascending.</summary>
    /// <param name="other">The slot to compare with.</param>
    /// <returns>A negative, zero or positive value.</returns>
    public int CompareTo(InventorySlot other)
    {
        int byRow = Y.CompareTo(other.Y);
        return byRow != 0 ? byRow : X.CompareTo(other.X);
    }

    /// <summary>Determines whether both coordinates are equal.</summary>
    /// <param name="other">The slot to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(InventorySlot other) => X == other.X && Y == other.Y;

    /// <summary>Determines whether the object is an equal slot.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => obj is InventorySlot other && Equals(other);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => (Y * 256) + X;

    /// <summary>Determines whether two slots are equal.</summary>
    /// <param name="left">The first slot.</param>
    /// <param name="right">The second slot.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(InventorySlot left, InventorySlot right) => left.Equals(right);

    /// <summary>Determines whether two slots differ.</summary>
    /// <param name="left">The first slot.</param>
    /// <param name="right">The second slot.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(InventorySlot left, InventorySlot right) => !left.Equals(right);
}
