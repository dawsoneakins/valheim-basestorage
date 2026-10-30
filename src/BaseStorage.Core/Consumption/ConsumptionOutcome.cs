namespace BaseStorage.Core.Consumption;

/// <summary>The terminal outcome of one consumption execution. The numeric values are contracted.</summary>
public enum ConsumptionOutcome
{
    /// <summary>Every take succeeded and every reservation was released; the caller may create the item or place the piece.</summary>
    Success = 0,

    /// <summary>A chest reservation was denied, lost or timed out; nothing was consumed.</summary>
    ReservationFailed = 1,

    /// <summary>A container no longer holds what the plan expected; nothing was consumed.</summary>
    ContentsChanged = 2,

    /// <summary>A take failed after earlier takes succeeded; the earlier takes were rolled back.</summary>
    TakeFailed = 3,

    /// <summary>The caller cancelled the execution before it finished; nothing was consumed.</summary>
    Cancelled = 4,

    /// <summary>The container layer threw; reservations were released and any takes rolled back as far as possible.</summary>
    Faulted = 5,
}
