namespace BaseStorage.Core.ChestAccess;

/// <summary>
/// The path a reservation takes for one chest (contract S-E005-002 section 2.2). Callers use the names. The numeric values
/// are contracted, and the underlying type is <see cref="int"/>.
/// </summary>
public enum ReservationRoute
{
    /// <summary>Send the open request to the owner and wait (Requesting).</summary>
    SendRequest = 0,

    /// <summary>The local peer already owns the chest: no network, grant now.</summary>
    LocalOwner = 1,

    /// <summary>The local player has the chest open in the vanilla UI: no network, grant now, never touch the in-use marker.</summary>
    SelfOpen = 2,

    /// <summary>The local peer is the server and the chest has no connected owner: claim locally, no RPC, grant now.</summary>
    HostClaim = 3,

    /// <summary>The view is invalid or there is no world: nothing is sent and the reservation fails with OwnerLost.</summary>
    SkipInvalid = 4,

    /// <summary>Vanilla access denies the local player: nothing is sent and the reservation fails with Denied.</summary>
    SkipAccessDenied = 5,

    /// <summary>Another listed player has the chest open: nothing is sent and the reservation fails with InUseByOther.</summary>
    SkipInUse = 6,

    /// <summary>A client, and the owner is 0 or not connected: nothing is sent and the reservation fails with OwnerLost.</summary>
    SkipNoOwner = 7,
}
