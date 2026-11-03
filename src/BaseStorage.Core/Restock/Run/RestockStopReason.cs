namespace BaseStorage.Core.Restock.Run;

/// <summary>
/// Why a Restock run stopped early (S-QR-002 contract section 3.2). The underlying type is int and the values are contracted.
/// </summary>
public enum RestockStopReason
{
    /// <summary>Not stopped.</summary>
    None = 0,

    /// <summary>The re-read after an add was doubtful (RR8).</summary>
    ConfirmFailed = 1,

    /// <summary>A remove failed and the rollback could not be confirmed (RR9).</summary>
    RollbackFailed = 2,

    /// <summary>The base lock renewal was refused (coordinator).</summary>
    LockLost = 3,

    /// <summary>Abort() was called, because the world went away or stepping threw.</summary>
    Aborted = 4,
}
