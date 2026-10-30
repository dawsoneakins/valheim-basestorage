namespace BaseStorage.Core.Locking;

/// <summary>The outcome of an organize lock request (contract section 3.1). The numeric values are contracted.</summary>
public enum OrganizeLockStatus
{
    /// <summary>The requester holds the lock now; the record was written to the lock station.</summary>
    Granted = 0,

    /// <summary>Another holder has a live lock in this base; nothing was written.</summary>
    Refused = 1,

    /// <summary>Renew only: no live lock of the requester and none of anyone else; nothing was written.</summary>
    NotHeld = 2,
}
