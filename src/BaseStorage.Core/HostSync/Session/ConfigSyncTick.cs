namespace BaseStorage.Core.HostSync.Session;

using System;

/// <summary>What one session tick changed (contract config-sync-contracts.md 3.4). Flags may combine.</summary>
[Flags]
public enum ConfigSyncTick
{
    /// <summary>Nothing changed.</summary>
    None = 0,

    /// <summary>The applied host payload was removed this tick (rule S3).</summary>
    HostCleared = 1,

    /// <summary>The mode just became LocalWorld or Client: the Plugin reads the local category file and calls LoadLocalCategories.</summary>
    ReloadLocalCategories = 2,
}
