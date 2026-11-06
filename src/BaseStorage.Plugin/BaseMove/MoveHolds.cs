namespace BaseStorage.Plugin.BaseMove;

using System.Collections.Generic;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Reservations;
using BaseStorage.Plugin.World;

/// <summary>
/// Holds a move's loaded chests through the reservation protocol. A hold has to last until the host's write has arrived, far past the usual
/// reservation timeout, and a reservation that times out lets go of its chest. So holds use their own long timeout. Main thread only.
/// </summary>
internal sealed class MoveHolds
{
    private const string Operation = "move";
    private const int HoldTimeoutMs = 3600000;

    private readonly PluginWorld world;
    private readonly ReservationDriver<Container> reservations;
    private readonly List<ContainerId> held = new List<ContainerId>();
    private readonly List<Container> scratch = new List<Container>();

    internal MoveHolds(PluginWorld world, ReservationDriver<Container> reservations)
    {
        this.world = world;
        this.reservations = reservations;
    }

    /// <summary>Gets the chests held or being held.</summary>
    internal IReadOnlyList<ContainerId> Held => held;

    /// <summary>Starts holding every listed chest this game has loaded, except the skipped ones.</summary>
    /// <param name="chests">The chests.</param>
    /// <param name="skip">The chests somebody else holds, or null.</param>
    /// <returns>False when a chest is already reserved by something else.</returns>
    internal bool Begin(IReadOnlyList<ContainerId> chests, HashSet<ContainerId>? skip)
    {
        Dictionary<ContainerId, Container> loaded = Loaded();
        for (int i = 0; i < chests.Count; i++)
        {
            ContainerId id = chests[i];
            if (held.Contains(id) || (skip != null && skip.Contains(id)) || !loaded.TryGetValue(id, out Container chest))
            {
                continue;
            }

            ReservationRoute? route = reservations.Begin(chest, Operation, HoldTimeoutMs);
            if (route == null)
            {
                return false;
            }

            held.Add(id);

            // A chest this player has open is not held still: a change in its window would save the old contents over the host's write.
            if (route.Value == ReservationRoute.SelfOpen)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Reads the holds' progress.</summary>
    /// <returns>True when all are granted, false when any failed, null while waiting.</returns>
    internal bool? Poll()
    {
        bool waiting = false;
        for (int i = 0; i < held.Count; i++)
        {
            Reservation<string>? reservation = reservations.Find(held[i]);
            if (reservation == null || reservation.State == ReservationState.Failed || reservation.IsTerminal)
            {
                return false;
            }

            waiting |= reservation.State != ReservationState.Granted;
        }

        return waiting ? (bool?)null : true;
    }

    /// <summary>Reads each held chest's current revision. Read it after the last hold, since a hold may raise it.</summary>
    /// <returns>The revisions, or null when a chest's record is gone.</returns>
    internal List<MoveChestRevision>? Revisions()
    {
        var revisions = new List<MoveChestRevision>(held.Count);
        for (int i = 0; i < held.Count; i++)
        {
            uint? revision = RevisionOf(held[i]);
            if (!revision.HasValue)
            {
                return null;
            }

            revisions.Add(new MoveChestRevision(held[i], revision.Value));
        }

        return revisions;
    }

    /// <summary>Releases every hold.</summary>
    internal void Release()
    {
        for (int i = 0; i < held.Count; i++)
        {
            reservations.Release(held[i]);
        }

        held.Clear();
    }

    /// <summary>Reads a chest's revision in this game's copy of the world.</summary>
    /// <param name="id">The chest id.</param>
    /// <returns>The revision, or null when this game has no record.</returns>
    internal static uint? RevisionOf(ContainerId id)
    {
        ZDO? zdo = ZDOMan.instance == null ? null : ZDOMan.instance.GetZDO(new ZDOID(id.UserId, id.Id));
        return zdo == null ? (uint?)null : zdo.DataRevision;
    }

    private Dictionary<ContainerId, Container> Loaded()
    {
        var loaded = new Dictionary<ContainerId, Container>();
        world.Containers.CopyItemsTo(scratch);
        for (int i = 0; i < scratch.Count; i++)
        {
            Container chest = scratch[i];
            if (chest != null && chest.m_nview != null && chest.m_nview.IsValid() && chest.m_nview.GetZDO() != null)
            {
                ZDOID uid = chest.m_nview.GetZDO().m_uid;
                loaded[new ContainerId(uid.UserID, uid.ID)] = chest;
            }
        }

        scratch.Clear();
        return loaded;
    }
}
