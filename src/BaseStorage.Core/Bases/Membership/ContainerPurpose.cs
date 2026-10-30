namespace BaseStorage.Core.Bases.Membership;

/// <summary>What a feature wants to use a base's containers for; the Keep rule differs between purposes.</summary>
public enum ContainerPurpose
{
    /// <summary>Moving items between chests (organize). Deposit uses this purpose too: Keep is never a source or destination.</summary>
    Organize = 0,

    /// <summary>Counting and taking materials for crafting and building from the base.</summary>
    CraftAvailability = 1,
}
