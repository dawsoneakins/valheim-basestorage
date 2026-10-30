namespace BaseStorage.Core.Categories;

/// <summary>
/// Core's game-free view of an item's type. The Plugin supplies it per item. Any value outside 0..7 is treated as
/// <see cref="Other"/> everywhere.
/// </summary>
public enum ItemKind
{
    /// <summary>Anything not listed below: materials, meads, potions, bait, fish and miscellaneous items.</summary>
    Other = 0,

    /// <summary>Food consumables that are eaten for health, stamina or eitr.</summary>
    Food = 1,

    /// <summary>Trophies.</summary>
    Trophy = 2,

    /// <summary>Weapons, bows and torches.</summary>
    Weapon = 3,

    /// <summary>Helmets, chest, legs, shoulder and utility armour.</summary>
    Armour = 4,

    /// <summary>Tools.</summary>
    Tool = 5,

    /// <summary>Shields.</summary>
    Shield = 6,

    /// <summary>Arrows and bolts.</summary>
    Ammo = 7,
}
