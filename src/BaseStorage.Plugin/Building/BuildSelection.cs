namespace BaseStorage.Plugin.Building;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// The selected build piece at one moment (contract build-from-base 3.2). The piece is an opaque reference; equality compares it
/// by reference only, so no engine equality override can interfere.
/// </summary>
internal readonly struct BuildSelection : IEquatable<BuildSelection>
{
    /// <summary>Creates a selection.</summary>
    /// <param name="piece">The piece instance passed to the placement call; not null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="piece"/> is null.</exception>
    internal BuildSelection(object piece)
    {
        Piece = piece ?? throw new ArgumentNullException(nameof(piece));
    }

    /// <summary>Gets the piece instance.</summary>
    internal object Piece { get; }

    /// <summary>Tells whether two selections are the same.</summary>
    /// <param name="left">The first selection.</param>
    /// <param name="right">The second selection.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(BuildSelection left, BuildSelection right) => left.Equals(right);

    /// <summary>Tells whether two selections differ.</summary>
    /// <param name="left">The first selection.</param>
    /// <param name="right">The second selection.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(BuildSelection left, BuildSelection right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(BuildSelection other) => ReferenceEquals(Piece, other.Piece);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BuildSelection other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Piece is null ? 0 : RuntimeHelpers.GetHashCode(Piece);
}
