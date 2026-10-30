namespace BaseStorage.Plugin.Reservations;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Snapshots;
using BaseStorage.Shared;

/// <summary>
/// The game adapter of the reservation driver for vanilla <see cref="Container"/> (contract S-E005-002 section 4.1 and 4.3).
/// Game-bound, so it is not linked into the tests; its facts are proven by source scans. The access and in-use expressions
/// are the same as <c>GameSnapshotSource</c>, so the snapshot flag and <see cref="ChestAccessRules.IsInUseByOther"/> agree.
/// </summary>
internal sealed class GameReservationPort : IReservationGame<Container>
{
    /// <summary>Gets a value that is true when a world exists and a local session is known.</summary>
    public bool InWorld => ZNet.instance != null && ZDOMan.GetSessionID() != 0;

    /// <summary>Gets the id of a chest from its ZDO uid, when the view is valid.</summary>
    /// <param name="c">The chest.</param>
    /// <param name="id">The id, when the call returns true.</param>
    /// <returns>False when the view is invalid or has no ZDO.</returns>
    public bool TryGetId(Container c, out ContainerId id)
    {
        if (!IsViewValid(c))
        {
            id = default;
            return false;
        }

        ZDOID uid = c.m_nview.GetZDO().m_uid;
        id = new ContainerId(uid.UserID, uid.ID);
        return true;
    }

    /// <summary>Tells whether the view of a chest is valid.</summary>
    /// <param name="c">The chest.</param>
    /// <returns>True when the view is valid and has a ZDO.</returns>
    public bool IsViewValid(Container c) =>
        c != null && c.m_nview != null && c.m_nview.IsValid() && c.m_nview.GetZDO() != null;

    /// <summary>Observes the facts of one chest (contract 4.3).</summary>
    /// <param name="c">The chest.</param>
    /// <returns>The observation.</returns>
    public ChestObservation Observe(Container c)
    {
        if (!IsViewValid(c))
        {
            return new ChestObservation(false, false, false, false, 0L, 0L, false, false, null, false);
        }

        ZDO zdo = c.m_nview.GetZDO();
        long localSessionId = ZDOMan.GetSessionID();
        long owner = zdo.GetOwner();
        bool isServer = ZNet.instance != null && ZNet.instance.IsServer();

        bool hasPlayer = false;
        long playerId = 0L;
        if (Game.instance != null)
        {
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            if (profile != null)
            {
                hasPlayer = true;
                playerId = profile.GetPlayerID();
            }
        }

        bool accessAllowed = hasPlayer
            && c.CheckAccess(playerId)
            && (!c.m_checkGuardStone || PrivateArea.CheckAccess(c.transform.position, 0f, false, false));
        bool openLocally = InventoryGui.instance != null && InventoryGui.instance.m_currentContainer == c;
        bool inUseFlag = zdo.GetInt(ZDOVars.s_inUse, 0) == 1;

        bool ownerListed = false;
        string? ownerName = null;
        if (owner != 0L && ZNet.instance != null)
        {
            var players = ZNet.instance.GetPlayerList();
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].m_characterID.UserID == owner)
                {
                    ownerListed = true;
                    ownerName = players[i].m_name;
                    break;
                }
            }
        }

        bool ownerConnected = ownerListed;
        if (owner == 0L)
        {
            ownerConnected = false;
        }
        else if (owner == localSessionId)
        {
            ownerConnected = true;
        }
        else if (ZNet.instance != null && ZNet.instance.IsServer())
        {
            ownerConnected = ZNet.instance.GetPeer(owner) != null;
        }

        return new ChestObservation(
            true,
            accessAllowed,
            openLocally,
            inUseFlag,
            localSessionId,
            owner,
            ownerListed,
            ownerConnected,
            ownerName,
            isServer);
    }

    /// <summary>Tells whether the local peer owns a chest.</summary>
    /// <param name="c">The chest.</param>
    /// <returns>True when the view is valid and the local peer owns it.</returns>
    public bool IsOwner(Container c) => IsViewValid(c) && c.m_nview.IsOwner();

    /// <summary>Tells whether the local vanilla UI has a chest open.</summary>
    /// <param name="c">The chest.</param>
    /// <returns>True when the local UI has this chest open.</returns>
    public bool IsOpenLocally(Container c) =>
        InventoryGui.instance != null && InventoryGui.instance.m_currentContainer == c;

    /// <summary>Sends the one open request of the handshake to the owner (RA3).</summary>
    /// <param name="c">The chest.</param>
    public void SendOpenRequest(Container c)
    {
        if (IsViewValid(c))
        {
            c.m_nview.InvokeRPC(PatchTargetNames.Container.RpcRequestOpen, Game.instance.GetPlayerProfile().GetPlayerID());
        }
    }

    /// <summary>Claims the ownership of a chest for the local peer (host route only, RA4).</summary>
    /// <param name="c">The chest.</param>
    public void ClaimOwnership(Container c)
    {
        if (IsViewValid(c))
        {
            c.m_nview.ClaimOwnership();
        }
    }

    /// <summary>Sets the in-use marker of a chest.</summary>
    /// <param name="c">The chest.</param>
    /// <param name="inUse">The new value of the marker.</param>
    public void SetInUse(Container c, bool inUse)
    {
        if (IsViewValid(c))
        {
            c.SetInUse(inUse);
        }
    }
}
