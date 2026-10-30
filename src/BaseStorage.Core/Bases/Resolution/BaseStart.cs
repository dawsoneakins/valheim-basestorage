namespace BaseStorage.Core.Bases.Resolution;

using System;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Where a base lookup starts (ADR-GWIN-024): a crafting station or a world position. Create one with <see cref="FromStation"/> or
/// <see cref="FromPosition"/>. <c>default(BaseStart)</c> has no defined <see cref="Kind"/> and is rejected by the resolver and the cache.
/// </summary>
public readonly struct BaseStart : IEquatable<BaseStart>
{
    private BaseStart(BaseStartKind kind, StationId station, WorldPosition position)
    {
        Kind = kind;
        Station = station;
        Position = position;
    }

    /// <summary>Starts a lookup from a crafting station.</summary>
    /// <param name="station">The id of the station; must not be none.</param>
    /// <returns>A station start.</returns>
    /// <exception cref="ArgumentException"><paramref name="station"/> is none.</exception>
    public static BaseStart FromStation(StationId station)
    {
        if (station.IsNone)
        {
            throw new ArgumentException("The station id must not be none.", nameof(station));
        }

        return new BaseStart(BaseStartKind.Station, station, default);
    }

    /// <summary>Starts a lookup from a world position. Only the horizontal (X and Z) components are used.</summary>
    /// <param name="position">The world position to look up.</param>
    /// <returns>A position start.</returns>
    public static BaseStart FromPosition(WorldPosition position) => new BaseStart(BaseStartKind.Position, default, position);

    /// <summary>Gets the kind of start.</summary>
    public BaseStartKind Kind { get; }

    /// <summary>Gets the station id of a station start; none for a position start.</summary>
    public StationId Station { get; }

    /// <summary>Gets the world position of a position start; the default position for a station start.</summary>
    public WorldPosition Position { get; }

    /// <summary>Determines whether two starts have the same kind and the same station or position.</summary>
    /// <param name="other">The start to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(BaseStart other) =>
        Kind == other.Kind && Station == other.Station && Position == other.Position;

    /// <summary>Determines whether the object is an equal start.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => obj is BaseStart other && Equals(other);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = (int)Kind;
            h = (h * 31) + Station.GetHashCode();
            return (h * 31) + Position.GetHashCode();
        }
    }

    /// <summary>Determines whether two starts are equal.</summary>
    /// <param name="left">The first start.</param>
    /// <param name="right">The second start.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(BaseStart left, BaseStart right) => left.Equals(right);

    /// <summary>Determines whether two starts differ.</summary>
    /// <param name="left">The first start.</param>
    /// <param name="right">The second start.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(BaseStart left, BaseStart right) => !left.Equals(right);
}
