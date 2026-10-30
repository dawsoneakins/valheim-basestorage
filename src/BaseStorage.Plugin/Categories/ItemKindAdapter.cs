namespace BaseStorage.Plugin.Categories;

using BaseStorage.Core.Categories;

/// <summary>Maps the game item type to Core ItemKind (category-contracts.md section 2.1).</summary>
internal static class ItemKindAdapter
{
    /// <summary>Maps the shared item data to an item kind. Null and unlisted types give Other.</summary>
    /// <param name="shared">The shared item data, or null.</param>
    /// <returns>The item kind.</returns>
    internal static ItemKind From(ItemDrop.ItemData.SharedData? shared)
    {
        if (shared is null)
        {
            return ItemKind.Other;
        }

        switch (shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.Consumable:
                return shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f ? ItemKind.Food : ItemKind.Other;
            case ItemDrop.ItemData.ItemType.Trophy:
                return ItemKind.Trophy;
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
            case ItemDrop.ItemData.ItemType.Bow:
            case ItemDrop.ItemData.ItemType.Torch:
                return ItemKind.Weapon;
            case ItemDrop.ItemData.ItemType.Helmet:
            case ItemDrop.ItemData.ItemType.Chest:
            case ItemDrop.ItemData.ItemType.Legs:
            case ItemDrop.ItemData.ItemType.Shoulder:
            case ItemDrop.ItemData.ItemType.Utility:
                return ItemKind.Armour;
            case ItemDrop.ItemData.ItemType.Tool:
                return ItemKind.Tool;
            case ItemDrop.ItemData.ItemType.Shield:
                return ItemKind.Shield;
            case ItemDrop.ItemData.ItemType.Ammo:
                return ItemKind.Ammo;
            default:
                return ItemKind.Other;
        }
    }
}
