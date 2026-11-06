namespace BaseStorage.Core.BaseMove;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// One ward read from world data. A far ward is not loaded, so the game's own <c>PrivateArea.CheckAccess</c> cannot see it. This mirrors
/// that check for one player: inside an enabled ward's radius, on the XZ plane, only the ward's creator and its permitted players have access.
/// </summary>
public sealed class MoveWard
{
    private readonly HashSet<long> permitted;

    /// <summary>Creates the ward.</summary>
    /// <param name="position">The ward's position.</param>
    /// <param name="radius">The ward's radius.</param>
    /// <param name="enabled">True when the ward is switched on.</param>
    /// <param name="creator">The player id of the ward's creator.</param>
    /// <param name="permittedPlayers">The player ids on the ward's list. Not null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="permittedPlayers"/> is null.</exception>
    public MoveWard(WorldPosition position, float radius, bool enabled, long creator, IEnumerable<long> permittedPlayers)
    {
        if (permittedPlayers is null)
        {
            throw new ArgumentNullException(nameof(permittedPlayers));
        }

        Position = position;
        Radius = radius;
        Enabled = enabled;
        Creator = creator;
        permitted = new HashSet<long>(permittedPlayers);
    }

    /// <summary>Gets the ward's position.</summary>
    public WorldPosition Position { get; }

    /// <summary>Gets the ward's radius.</summary>
    public float Radius { get; }

    /// <summary>Gets a value indicating whether the ward is switched on.</summary>
    public bool Enabled { get; }

    /// <summary>Gets the player id of the ward's creator.</summary>
    public long Creator { get; }

    /// <summary>Tells whether any ward keeps a player out of a point.</summary>
    /// <param name="wards">The wards. Not null.</param>
    /// <param name="point">The point.</param>
    /// <param name="playerId">The player id.</param>
    /// <returns>True when an enabled ward covers the point and the player is neither its creator nor on its list.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="wards"/> is null.</exception>
    public static bool Denies(IReadOnlyList<MoveWard> wards, WorldPosition point, long playerId)
    {
        if (wards is null)
        {
            throw new ArgumentNullException(nameof(wards));
        }

        for (var i = 0; i < wards.Count; i++)
        {
            var ward = wards[i];
            if (ward.Enabled && ward.Covers(point) && ward.Creator != playerId && !ward.permitted.Contains(playerId))
            {
                return true;
            }
        }

        return false;
    }

    private bool Covers(WorldPosition point)
    {
        var dx = (double)Position.X - point.X;
        var dz = (double)Position.Z - point.Z;
        return (dx * dx) + (dz * dz) < (double)Radius * Radius;
    }
}
