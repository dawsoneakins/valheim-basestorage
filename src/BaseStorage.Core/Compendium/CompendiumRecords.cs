namespace BaseStorage.Core.Compendium;

using System.Collections.Generic;

/// <summary>The kinds of page.</summary>
public enum CompendiumKind
{
    /// <summary>An item.</summary>
    Item = 0,

    /// <summary>A creature.</summary>
    Creature = 1,

    /// <summary>A biome.</summary>
    Biome = 2,

    /// <summary>A building piece.</summary>
    Piece = 3,
}

/// <summary>The world's biomes, in the order a player meets them.</summary>
public enum CompendiumBiome
{
    /// <summary>The Meadows.</summary>
    Meadows = 0,

    /// <summary>The Black Forest.</summary>
    BlackForest = 1,

    /// <summary>The Swamp.</summary>
    Swamp = 2,

    /// <summary>The Mountain.</summary>
    Mountain = 3,

    /// <summary>The Plains.</summary>
    Plains = 4,

    /// <summary>The Mistlands.</summary>
    Mistlands = 5,

    /// <summary>The Ashlands.</summary>
    Ashlands = 6,

    /// <summary>The Deep North.</summary>
    DeepNorth = 7,

    /// <summary>The Ocean.</summary>
    Ocean = 8,
}

/// <summary>The groups items are listed under.</summary>
public enum ItemCategory
{
    /// <summary>Anything else.</summary>
    Misc = 0,

    /// <summary>A weapon.</summary>
    Weapon = 1,

    /// <summary>Worn gear.</summary>
    Armor = 2,

    /// <summary>A shield.</summary>
    Shield = 3,

    /// <summary>A tool.</summary>
    Tool = 4,

    /// <summary>Arrows, bolts and the like.</summary>
    Ammo = 5,

    /// <summary>Food.</summary>
    Food = 6,

    /// <summary>A consumable that isn't food.</summary>
    Potion = 7,

    /// <summary>A trophy.</summary>
    Trophy = 8,

    /// <summary>A crafting material.</summary>
    Material = 9,
}

/// <summary>The damage types.</summary>
public enum CompendiumDamage
{
    /// <summary>True damage.</summary>
    True = 0,

    /// <summary>Blunt.</summary>
    Blunt = 1,

    /// <summary>Slash.</summary>
    Slash = 2,

    /// <summary>Pierce.</summary>
    Pierce = 3,

    /// <summary>Chop.</summary>
    Chop = 4,

    /// <summary>Pickaxe.</summary>
    Pickaxe = 5,

    /// <summary>Fire.</summary>
    Fire = 6,

    /// <summary>Frost.</summary>
    Frost = 7,

    /// <summary>Lightning.</summary>
    Lightning = 8,

    /// <summary>Poison.</summary>
    Poison = 9,

    /// <summary>Spirit.</summary>
    Spirit = 10,
}

/// <summary>How a creature takes a damage type.</summary>
public enum DamageResponse
{
    /// <summary>Normally.</summary>
    Normal = 0,

    /// <summary>Very weak to it.</summary>
    VeryWeak = 1,

    /// <summary>Weak to it.</summary>
    Weak = 2,

    /// <summary>Slightly weak to it.</summary>
    SlightlyWeak = 3,

    /// <summary>Slightly resistant to it.</summary>
    SlightlyResistant = 4,

    /// <summary>Resistant to it.</summary>
    Resistant = 5,

    /// <summary>Very resistant to it.</summary>
    VeryResistant = 6,

    /// <summary>Immune to it.</summary>
    Immune = 7,
}

/// <summary>An amount of one damage type.</summary>
public sealed class DamageAmount
{
    /// <summary>Gets or sets the type.</summary>
    public CompendiumDamage Type { get; set; }

    /// <summary>Gets or sets the amount.</summary>
    public float Amount { get; set; }
}

/// <summary>A creature's response to one damage type.</summary>
public sealed class DamageResponseEntry
{
    /// <summary>Gets or sets the type.</summary>
    public CompendiumDamage Type { get; set; }

    /// <summary>Gets or sets the response.</summary>
    public DamageResponse Response { get; set; }
}

/// <summary>An item a recipe or piece needs.</summary>
public sealed class IngredientRecord
{
    /// <summary>Gets or sets the item's id.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount.</summary>
    public int Amount { get; set; }

    /// <summary>Gets or sets the extra amount for each quality level.</summary>
    public int AmountPerLevel { get; set; }
}

/// <summary>An item as the game defines it.</summary>
public sealed class ItemRecord
{
    /// <summary>Gets or sets the prefab name.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the group.</summary>
    public ItemCategory Category { get; set; }

    /// <summary>Gets or sets the weight.</summary>
    public float Weight { get; set; }

    /// <summary>Gets or sets the largest stack.</summary>
    public int MaxStack { get; set; }

    /// <summary>Gets or sets the highest quality.</summary>
    public int MaxQuality { get; set; }

    /// <summary>Gets or sets the trader's coin value.</summary>
    public int Value { get; set; }

    /// <summary>Gets or sets a value indicating whether it goes through portals.</summary>
    public bool Teleportable { get; set; } = true;

    /// <summary>Gets or sets the durability, zero when it has none.</summary>
    public float Durability { get; set; }

    /// <summary>Gets or sets the armor.</summary>
    public float Armor { get; set; }

    /// <summary>Gets or sets the armor gained per quality level.</summary>
    public float ArmorPerLevel { get; set; }

    /// <summary>Gets or sets the block power.</summary>
    public float BlockPower { get; set; }

    /// <summary>Gets or sets the movement modifier, where -0.05 is 5% slower.</summary>
    public float Movement { get; set; }

    /// <summary>Gets or sets the health food gives.</summary>
    public float FoodHealth { get; set; }

    /// <summary>Gets or sets the stamina food gives.</summary>
    public float FoodStamina { get; set; }

    /// <summary>Gets or sets the eitr food gives.</summary>
    public float FoodEitr { get; set; }

    /// <summary>Gets or sets the healing per tick food gives.</summary>
    public float FoodRegen { get; set; }

    /// <summary>Gets or sets how long food lasts, in seconds.</summary>
    public float FoodSeconds { get; set; }

    /// <summary>Gets or sets the armor set's name, or empty.</summary>
    public string SetName { get; set; } = string.Empty;

    /// <summary>Gets the damage it deals.</summary>
    public List<DamageAmount> Damage { get; } = new List<DamageAmount>();
}

/// <summary>A recipe.</summary>
public sealed class RecipeRecord
{
    /// <summary>Gets or sets the item made.</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>Gets or sets how many one craft makes.</summary>
    public int Amount { get; set; }

    /// <summary>Gets or sets the station's name, or empty when made by hand.</summary>
    public string Station { get; set; } = string.Empty;

    /// <summary>Gets or sets the station level needed.</summary>
    public int StationLevel { get; set; }

    /// <summary>Gets the items needed.</summary>
    public List<IngredientRecord> Ingredients { get; } = new List<IngredientRecord>();
}

/// <summary>An item a creature can drop.</summary>
public sealed class CreatureDropRecord
{
    /// <summary>Gets or sets the item's id.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>Gets or sets the fewest dropped.</summary>
    public int Min { get; set; }

    /// <summary>Gets or sets the most dropped.</summary>
    public int Max { get; set; }

    /// <summary>Gets or sets the chance, from 0 to 1.</summary>
    public float Chance { get; set; }
}

/// <summary>A creature as the game defines it.</summary>
public sealed class CreatureRecord
{
    /// <summary>Gets or sets the prefab name.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the group it's listed under.</summary>
    public string Faction { get; set; } = string.Empty;

    /// <summary>Gets or sets its base health.</summary>
    public float Health { get; set; }

    /// <summary>Gets or sets a value indicating whether it's a boss.</summary>
    public bool Boss { get; set; }

    /// <summary>Gets or sets the total damage of its strongest attack.</summary>
    public float StrongestAttack { get; set; }

    /// <summary>Gets its responses to damage.</summary>
    public List<DamageResponseEntry> Responses { get; } = new List<DamageResponseEntry>();

    /// <summary>Gets what it drops.</summary>
    public List<CreatureDropRecord> Drops { get; } = new List<CreatureDropRecord>();
}

/// <summary>One entry of the world's spawn list.</summary>
public sealed class SpawnRecord
{
    /// <summary>Gets or sets the creature's id.</summary>
    public string CreatureId { get; set; } = string.Empty;

    /// <summary>Gets or sets the biome.</summary>
    public CompendiumBiome Biome { get; set; }

    /// <summary>Gets or sets a value indicating whether it spawns by day.</summary>
    public bool Day { get; set; }

    /// <summary>Gets or sets a value indicating whether it spawns by night.</summary>
    public bool Night { get; set; }
}

/// <summary>Something in the world that gives items, such as a tree or an ore deposit.</summary>
public sealed class ResourceRecord
{
    /// <summary>Gets or sets its name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the biomes it grows in, empty when the world places it some other way.</summary>
    public List<CompendiumBiome> Biomes { get; } = new List<CompendiumBiome>();

    /// <summary>Gets the items it gives.</summary>
    public List<string> ItemIds { get; } = new List<string>();
}

/// <summary>A building piece as the game defines it.</summary>
public sealed class PieceRecord
{
    /// <summary>Gets or sets the prefab name.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the group it's listed under.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the comfort it gives.</summary>
    public int Comfort { get; set; }

    /// <summary>Gets or sets the station it needs nearby, or empty.</summary>
    public string Station { get; set; } = string.Empty;

    /// <summary>Gets the items it costs.</summary>
    public List<IngredientRecord> Cost { get; } = new List<IngredientRecord>();
}

/// <summary>Everything read from the game, in plain form.</summary>
public sealed class CompendiumData
{
    /// <summary>Gets the items.</summary>
    public List<ItemRecord> Items { get; } = new List<ItemRecord>();

    /// <summary>Gets the recipes.</summary>
    public List<RecipeRecord> Recipes { get; } = new List<RecipeRecord>();

    /// <summary>Gets the creatures.</summary>
    public List<CreatureRecord> Creatures { get; } = new List<CreatureRecord>();

    /// <summary>Gets the spawn entries.</summary>
    public List<SpawnRecord> Spawns { get; } = new List<SpawnRecord>();

    /// <summary>Gets the world's resources.</summary>
    public List<ResourceRecord> Resources { get; } = new List<ResourceRecord>();

    /// <summary>Gets the building pieces.</summary>
    public List<PieceRecord> Pieces { get; } = new List<PieceRecord>();
}
