namespace BaseStorage.Core.Abstractions;

/// <summary>Write-access state of a container reservation.</summary>
public enum ReservationStatus
{
    /// <summary>Not requested, or released.</summary>
    None = 0,

    /// <summary>Requested, waiting for the owner's answer.</summary>
    Pending = 1,

    /// <summary>The local peer may write to the container.</summary>
    Granted = 2,

    /// <summary>The owner refused, for example in use by another player or access denied.</summary>
    Denied = 3,

    /// <summary>Was pending or granted, then ownership was lost, the owner disconnected or the container unloaded.</summary>
    Lost = 4,
}
