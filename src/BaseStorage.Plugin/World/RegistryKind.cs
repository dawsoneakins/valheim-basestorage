namespace BaseStorage.Plugin.World;

/// <summary>
/// Which loaded-object registry a debug line describes (contract section 8). The numeric values are part of the contract
/// and are checked by test. Game-free (W1).
/// </summary>
internal enum RegistryKind
{
    /// <summary>The registry of loaded containers.</summary>
    Container = 0,

    /// <summary>The registry of loaded crafting stations.</summary>
    Station = 1,
}
