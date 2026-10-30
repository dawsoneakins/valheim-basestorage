namespace BaseStorage.Core.Categories;

/// <summary>The fallback category names for items that the category file does not map (spec AC-3, AC-4.1).</summary>
public static class FallbackCategories
{
    /// <summary>The fallback category for food items.</summary>
    public const string CookedFood = "Cooked Food";

    /// <summary>The fallback category for trophies.</summary>
    public const string Trophies = "Trophies";

    /// <summary>The fallback category for weapons, armour, tools and shields.</summary>
    public const string Gear = "Gear";

    /// <summary>The fallback category for arrows and bolts.</summary>
    public const string Ammo = "Ammo";

    /// <summary>The fallback category for everything else.</summary>
    public const string Misc = "Misc";

    /// <summary>Gets the fallback category name for an item kind. The result is never null.</summary>
    /// <param name="kind">The item kind; undefined values give <see cref="Misc"/>.</param>
    /// <returns>The fallback category name.</returns>
    public static string For(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.Food:
                return CookedFood;
            case ItemKind.Trophy:
                return Trophies;
            case ItemKind.Weapon:
            case ItemKind.Armour:
            case ItemKind.Tool:
            case ItemKind.Shield:
                return Gear;
            case ItemKind.Ammo:
                return Ammo;
            default:
                return Misc;
        }
    }
}
