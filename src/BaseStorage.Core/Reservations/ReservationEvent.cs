namespace BaseStorage.Core.Reservations;

/// <summary>
/// An event that the caller reports to a reservation (contract S-E005-001 section 2.2). Every event is supplied by the caller;
/// the timeout is not an event and is evaluated by the reservation's CheckTimeout. The numeric values are contracted.
/// </summary>
public enum ReservationEvent
{
    /// <summary>The owner granted access.</summary>
    Grant = 0,

    /// <summary>The owner refused access.</summary>
    Deny = 1,

    /// <summary>The caller abandons a request before any answer.</summary>
    Cancel = 2,

    /// <summary>The caller starts writing.</summary>
    BeginWrite = 3,

    /// <summary>The caller is done, or writes nothing, and starts giving the chest back.</summary>
    BeginRelease = 4,

    /// <summary>The chest has been given back.</summary>
    CompleteRelease = 5,

    /// <summary>The requesting session vanished (disconnect, logout or world unload).</summary>
    RequesterDisconnected = 6,

    /// <summary>The chest's owner disconnected, ownership moved away, or the chest unloaded.</summary>
    OwnerLost = 7,

    /// <summary>Another player was observed to have the chest open.</summary>
    InUseByOther = 8,
}
