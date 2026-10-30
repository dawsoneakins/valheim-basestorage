namespace BaseStorage.Core.Abstractions;

/// <summary>Outcome of a container mutation.</summary>
public enum ContainerOperationResult
{
    /// <summary>The operation was applied.</summary>
    Success = 0,

    /// <summary>The container is unknown or no longer loaded.</summary>
    NotFound = 1,

    /// <summary>The reservation status is not Granted.</summary>
    NotReserved = 2,

    /// <summary>The stack at the given index is not the expected one, or the index is out of range.</summary>
    ContainerChanged = 3,

    /// <summary>The container cannot accept the whole amount; nothing was added.</summary>
    DestinationFull = 4,
}
