namespace BaseStorage.Core.HostSync.Session;

/// <summary>
/// The peer RPC that carries the host payload (contract config-sync-contracts.md 2.1). The mod owns this name; it is not a game member.
/// </summary>
public static class ConfigSyncRpc
{
    /// <summary>The RPC name "BaseStorage_ConfigSync": registered by the holder on every new connection, invoked by the host once per accepted client.</summary>
    public const string Name = "BaseStorage_ConfigSync";
}
