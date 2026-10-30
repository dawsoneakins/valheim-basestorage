namespace BaseStorage.Core.Snapshots;

using System;

/// <summary>A crafting station and its build range.</summary>
public sealed class StationSnapshot : IEquatable<StationSnapshot>
{
    /// <summary>Creates a station snapshot.</summary>
    /// <param name="id">The station id; must not be none.</param>
    /// <param name="position">The station's world position.</param>
    /// <param name="buildRange">The build range in metres; finite and at least 0.</param>
    public StationSnapshot(StationId id, WorldPosition position, float buildRange)
    {
        if (id.IsNone)
        {
            throw new ArgumentException("Must not be none.", nameof(id));
        }

        float range = SnapshotGuard.Finite(buildRange, nameof(buildRange));
        if (range < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(buildRange), "Must be at least 0.");
        }

        Id = id;
        Position = position;
        BuildRange = range;
    }

    /// <summary>Gets the station id.</summary>
    public StationId Id { get; }

    /// <summary>Gets the station's world position.</summary>
    public WorldPosition Position { get; }

    /// <summary>Gets the build range in metres; 0 means the station has no build range.</summary>
    public float BuildRange { get; }

    /// <summary>Determines whether two snapshots are equal.</summary>
    /// <param name="left">The first snapshot.</param>
    /// <param name="right">The second snapshot.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(StationSnapshot? left, StationSnapshot? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two snapshots differ.</summary>
    /// <param name="left">The first snapshot.</param>
    /// <param name="right">The second snapshot.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(StationSnapshot? left, StationSnapshot? right) => !(left == right);

    /// <summary>Determines whether id, position and build range are equal.</summary>
    /// <param name="other">The snapshot to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(StationSnapshot? other) =>
        other is not null && Id == other.Id && Position == other.Position && BuildRange == other.BuildRange;

    /// <summary>Determines whether the object is an equal snapshot.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as StationSnapshot);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = Id.GetHashCode();
            h = (h * 31) + Position.GetHashCode();
            return (h * 31) + BuildRange.GetHashCode();
        }
    }
}
