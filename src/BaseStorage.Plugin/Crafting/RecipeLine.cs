namespace BaseStorage.Plugin.Crafting;

/// <summary>One material line of a recipe as the game reports it (contract craft-from-base 3.3).</summary>
internal readonly struct RecipeLine
{
    /// <summary>Creates a recipe line.</summary>
    /// <param name="prefabName">The item prefab name, or null when the line has no item.</param>
    /// <param name="amountPerCraft">The amount per craft as the game computes it.</param>
    /// <param name="upgraderResource">True when the line counts only at an upgrader station.</param>
    internal RecipeLine(string? prefabName, int amountPerCraft, bool upgraderResource)
    {
        PrefabName = prefabName;
        AmountPerCraft = amountPerCraft;
        UpgraderResource = upgraderResource;
    }

    /// <summary>Gets the item prefab name, or null.</summary>
    internal string? PrefabName { get; }

    /// <summary>Gets the amount per craft.</summary>
    internal int AmountPerCraft { get; }

    /// <summary>Gets a value indicating whether the line counts only at an upgrader station.</summary>
    internal bool UpgraderResource { get; }
}
