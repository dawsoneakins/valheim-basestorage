namespace BaseStorage.Core.HostSync.Session;

/// <summary>What the Plugin does after one config-sync decision (contract config-sync-contracts.md 3.2).</summary>
public enum ConfigSyncAction
{
    /// <summary>Nothing to do.</summary>
    None = 0,

    /// <summary>Host: build the payload and invoke ConfigSyncRpc.Name on the peer.</summary>
    Send = 1,

    /// <summary>Client: the host payload is now active.</summary>
    Applied = 2,

    /// <summary>Client: the payload was not applied; the active settings are unchanged.</summary>
    Rejected = 3,
}
