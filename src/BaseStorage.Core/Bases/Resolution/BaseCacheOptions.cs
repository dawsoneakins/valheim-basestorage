namespace BaseStorage.Core.Bases.Resolution;

using System;

/// <summary>
/// The two settings that <see cref="BaseCache"/> reads on each call (ADR-GWIN-024): the cache window in seconds and the station cap.
/// The constructor never throws. It normalises a negative window to 0 and a cap below 1 to 1, and equality compares the normalised values.
/// </summary>
public readonly struct BaseCacheOptions : IEquatable<BaseCacheOptions>
{
    /// <summary>Creates options. Never throws; out-of-range values are normalised.</summary>
    /// <param name="baseCacheSeconds">The window in seconds; below 0 is normalised to 0, which rebuilds on every resolve.</param>
    /// <param name="maxBaseStations">The station cap; below 1 is normalised to 1.</param>
    public BaseCacheOptions(int baseCacheSeconds, int maxBaseStations)
    {
        BaseCacheSeconds = baseCacheSeconds < 0 ? 0 : baseCacheSeconds;
        MaxBaseStations = maxBaseStations < 1 ? 1 : maxBaseStations;
    }

    /// <summary>Gets the window in seconds, never below 0.</summary>
    public int BaseCacheSeconds { get; }

    /// <summary>Gets the station cap, never below 1.</summary>
    public int MaxBaseStations { get; }

    /// <summary>Determines whether the normalised values are equal.</summary>
    /// <param name="other">The options to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(BaseCacheOptions other) =>
        BaseCacheSeconds == other.BaseCacheSeconds && MaxBaseStations == other.MaxBaseStations;

    /// <summary>Determines whether the object is equal options.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => obj is BaseCacheOptions other && Equals(other);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => unchecked((BaseCacheSeconds * 31) + MaxBaseStations);

    /// <summary>Determines whether two options are equal.</summary>
    /// <param name="left">The first options.</param>
    /// <param name="right">The second options.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(BaseCacheOptions left, BaseCacheOptions right) => left.Equals(right);

    /// <summary>Determines whether two options differ.</summary>
    /// <param name="left">The first options.</param>
    /// <param name="right">The second options.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(BaseCacheOptions left, BaseCacheOptions right) => !left.Equals(right);
}
