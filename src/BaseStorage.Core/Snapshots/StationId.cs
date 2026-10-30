namespace BaseStorage.Core.Snapshots;

using System;

// Keep in sync with ContainerId.cs: the twin types are separate by contract so ids cannot be mixed up.
/// <summary>Opaque network-object id of a crafting station (creator peer id plus per-peer serial).</summary>
public readonly struct StationId : IEquatable<StationId>, IComparable<StationId>
{
    /// <summary>Creates an id from the game's two-part object id.</summary>
    /// <param name="userId">The creator peer id.</param>
    /// <param name="id">The per-peer serial number.</param>
    public StationId(long userId, uint id)
    {
        UserId = userId;
        Id = id;
    }

    /// <summary>Gets the creator peer id.</summary>
    public long UserId { get; }

    /// <summary>Gets the per-peer serial number.</summary>
    public uint Id { get; }

    /// <summary>Gets a value indicating whether both parts are zero.</summary>
    public bool IsNone => UserId == 0 && Id == 0;

    /// <summary>Compares by user id ascending, then id ascending.</summary>
    /// <param name="other">The id to compare with.</param>
    /// <returns>A negative, zero or positive value.</returns>
    public int CompareTo(StationId other)
    {
        int byUser = UserId.CompareTo(other.UserId);
        return byUser != 0 ? byUser : Id.CompareTo(other.Id);
    }

    /// <summary>Determines whether both parts are equal.</summary>
    /// <param name="other">The id to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(StationId other) => UserId == other.UserId && Id == other.Id;

    /// <summary>Determines whether the object is an equal id.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => obj is StationId other && Equals(other);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => unchecked((UserId.GetHashCode() * 31) + Id.GetHashCode());

    /// <summary>Determines whether two ids are equal.</summary>
    /// <param name="left">The first id.</param>
    /// <param name="right">The second id.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(StationId left, StationId right) => left.Equals(right);

    /// <summary>Determines whether two ids differ.</summary>
    /// <param name="left">The first id.</param>
    /// <param name="right">The second id.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(StationId left, StationId right) => !left.Equals(right);
}
