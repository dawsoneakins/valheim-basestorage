namespace BaseStorage.Plugin.Deposit;

using System;
using BaseStorage.Core.Organize.Deposit;

/// <summary>
/// Maps the plain facts of one inventory item to its Deposit traits (contract deposit-button-contracts.md section 3.1). Pure and
/// game-free: the game-bound capture reads the facts and passes them in. Extra traits only add exclusions, so they are safe.
/// </summary>
internal static class DepositTraitRules
{
    /// <summary>The prefab name prefix of the fishing bait items.</summary>
    internal const string BaitPrefabPrefix = "FishingBait";

    /// <summary>Builds the traits of one item.</summary>
    /// <param name="equipped">True when the item is equipped.</param>
    /// <param name="consumable">True when the item type is a consumable.</param>
    /// <param name="ammo">True when the item type is ammo.</param>
    /// <param name="food">True when the item is food.</param>
    /// <param name="prefabName">The prefab name of the item.</param>
    /// <returns>The OR of every trait that applies.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prefabName"/> is null.</exception>
    internal static InventoryItemTraits Of(bool equipped, bool consumable, bool ammo, bool food, string prefabName)
    {
        if (prefabName is null)
        {
            throw new ArgumentNullException(nameof(prefabName));
        }

        InventoryItemTraits traits = InventoryItemTraits.None;
        if (equipped)
        {
            traits |= InventoryItemTraits.Equipped;
        }

        if (food)
        {
            traits |= InventoryItemTraits.Food;
        }

        if (consumable && !food)
        {
            traits |= InventoryItemTraits.Mead;
        }

        if (ammo)
        {
            traits |= InventoryItemTraits.Ammo;
        }

        if (prefabName.StartsWith(BaitPrefabPrefix, StringComparison.Ordinal))
        {
            traits |= InventoryItemTraits.Bait;
        }

        return traits;
    }
}
