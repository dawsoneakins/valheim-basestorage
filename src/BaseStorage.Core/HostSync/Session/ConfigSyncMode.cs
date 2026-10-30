namespace BaseStorage.Core.HostSync.Session;

/// <summary>The mode of the session as the Plugin computes it (contract config-sync-contracts.md 3.1, rule S2).</summary>
public enum ConfigSyncMode
{
    /// <summary>No ZNet: the main menu or a loading screen.</summary>
    Menu = 0,

    /// <summary>ZNet.instance.IsServer(): single-player, a hosted world or a dedicated server.</summary>
    LocalWorld = 1,

    /// <summary>ZNet.instance present and not the server: connected to a remote host.</summary>
    Client = 2,
}
