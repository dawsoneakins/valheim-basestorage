namespace BaseStorage.Core.Reservations;

/// <summary>The state of one chest reservation (contract S-E005-001 section 2.1). The numeric values are contracted.</summary>
public enum ReservationState
{
    /// <summary>Asked the chest's owner for access; holds nothing yet.</summary>
    Requesting = 0,

    /// <summary>Access granted; the requester holds the chest but has not started writing.</summary>
    Granted = 1,

    /// <summary>The operation is writing to the chest.</summary>
    Writing = 2,

    /// <summary>Writing is over; the chest is being given back.</summary>
    Releasing = 3,

    /// <summary>Terminal: released normally.</summary>
    Completed = 4,

    /// <summary>Terminal: ended abnormally; the failure and the state it failed from are kept by the reservation.</summary>
    Failed = 5,
}
