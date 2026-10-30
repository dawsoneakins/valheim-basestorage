namespace BaseStorage.Core.Snapshots;

using System;

/// <summary>Core-owned world position in metres, with Y up, matching the game's world space.</summary>
public readonly struct WorldPosition : IEquatable<WorldPosition>
{
    /// <summary>Creates a position.</summary>
    /// <param name="x">The X component; must be finite.</param>
    /// <param name="y">The Y (up) component; must be finite.</param>
    /// <param name="z">The Z component; must be finite.</param>
    public WorldPosition(float x, float y, float z)
    {
        X = SnapshotGuard.Finite(x, nameof(x));
        Y = SnapshotGuard.Finite(y, nameof(y));
        Z = SnapshotGuard.Finite(z, nameof(z));
    }

    /// <summary>Gets the X component.</summary>
    public float X { get; }

    /// <summary>Gets the Y (up) component.</summary>
    public float Y { get; }

    /// <summary>Gets the Z component.</summary>
    public float Z { get; }

    /// <summary>Determines whether all components are equal.</summary>
    /// <param name="other">The position to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(WorldPosition other) => X == other.X && Y == other.Y && Z == other.Z;

    /// <summary>Determines whether the object is an equal position.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => obj is WorldPosition other && Equals(other);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = X.GetHashCode();
            h = (h * 31) + Y.GetHashCode();
            return (h * 31) + Z.GetHashCode();
        }
    }

    /// <summary>Determines whether two positions are equal.</summary>
    /// <param name="left">The first position.</param>
    /// <param name="right">The second position.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(WorldPosition left, WorldPosition right) => left.Equals(right);

    /// <summary>Determines whether two positions differ.</summary>
    /// <param name="left">The first position.</param>
    /// <param name="right">The second position.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(WorldPosition left, WorldPosition right) => !left.Equals(right);
}
