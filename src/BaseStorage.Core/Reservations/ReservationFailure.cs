namespace BaseStorage.Core.Reservations;

/// <summary>
/// The reason a reservation failed (contract S-E005-001 section 2.3). These are return values, not log or build codes.
/// The numeric values are contracted.
/// </summary>
public enum ReservationFailure
{
    /// <summary>Not failed: every state except Failed.</summary>
    None = 0,

    /// <summary>The deadline passed before a terminal state was reached.</summary>
    Timeout = 1,

    /// <summary>The owner denied the request while it was Requesting.</summary>
    Denied = 2,

    /// <summary>The caller cancelled the request while it was Requesting.</summary>
    Cancelled = 3,

    /// <summary>The requesting session vanished in a non-terminal state.</summary>
    RequesterDisconnected = 4,

    /// <summary>The owner was lost in a non-terminal state.</summary>
    OwnerLost = 5,

    /// <summary>Another player was observed to have the chest open in a non-terminal state.</summary>
    InUseByOther = 6,
}
