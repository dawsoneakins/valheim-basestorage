namespace BaseStorage.Core.Settings;

/// <summary>Class tokens that an item list entry may use in place of a prefab name. A token starts with @.</summary>
public static class ItemListTokens
{
    /// <summary>Carried food: consumables that give health, stamina or eitr food.</summary>
    public const string Food = "@food";

    /// <summary>Meads and potions: consumables that apply an effect and are not food.</summary>
    public const string Mead = "@mead";

    /// <summary>Arrows and bolts (ammunition).</summary>
    public const string Ammo = "@ammo";

    /// <summary>Fishing bait of every kind.</summary>
    public const string Bait = "@bait";
}
