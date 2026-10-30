namespace BaseStorage.Core.Organize.Execution;

/// <summary>Why an execution stopped early (contract 2.2). The numeric values are contracted; an unknown value means "stopped".</summary>
public enum ExecutionStopReason
{
    /// <summary>Not stopped: the status is NotStarted, Running or Completed.</summary>
    None = 0,

    /// <summary>A chest of the batch reported a Denied reservation.</summary>
    ReservationRefused = 1,

    /// <summary>A chest stayed Pending past ReserveTimeoutMs.</summary>
    ReservationTimedOut = 2,

    /// <summary>A chest reported a Lost reservation, or a write returned NotReserved.</summary>
    ReservationLost = 3,

    /// <summary>The chest could not be fetched, a write returned NotFound, or the status was None after a request.</summary>
    ChestUnavailable = 4,

    /// <summary>The live contents differ from the expected state, the expected source stack was not found, or a write returned ContainerChanged.</summary>
    ChestChanged = 5,

    /// <summary>AddItems returned DestinationFull; nothing of that move was added.</summary>
    DestinationFull = 6,

    /// <summary>The remove failed after a successful add and the add could not be fully taken back (contract 4.3).</summary>
    RollbackFailed = 7,

    /// <summary><see cref="OrganizeExecutor.Abort"/> was called before the run finished.</summary>
    Aborted = 8,
}
