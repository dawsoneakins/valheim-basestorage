namespace BaseStorage.Core.Compendium;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BaseStorage.Core.Ui;

/// <summary>Turns the game's data into cross-linked pages, free of spoilers: only what the player has discovered gets a page, and pages name nothing else. An undiscovered thing a page needs to count, such as a recipe's ingredient, reads as undiscovered.</summary>
public static class CompendiumBuilder
{
    private const string ItemPrefix = "item:";
    private const string CreaturePrefix = "creature:";
    private const string BiomePrefix = "biome:";
    private const string PiecePrefix = "piece:";

    private static readonly CompendiumBiome[] AllBiomes =
    {
        CompendiumBiome.Meadows, CompendiumBiome.BlackForest, CompendiumBiome.Swamp, CompendiumBiome.Mountain, CompendiumBiome.Plains,
        CompendiumBiome.Mistlands, CompendiumBiome.Ashlands, CompendiumBiome.DeepNorth, CompendiumBiome.Ocean,
    };

    /// <summary>Gets an item's page id.</summary>
    /// <param name="id">The item's id.</param>
    /// <returns>The page id.</returns>
    public static string ItemPage(string id) => ItemPrefix + id;

    /// <summary>Gets a creature's page id.</summary>
    /// <param name="id">The creature's id.</param>
    /// <returns>The page id.</returns>
    public static string CreaturePage(string id) => CreaturePrefix + id;

    /// <summary>Gets a biome's page id.</summary>
    /// <param name="biome">The biome.</param>
    /// <returns>The page id.</returns>
    public static string BiomePage(CompendiumBiome biome) => BiomePrefix + biome.ToString();

    /// <summary>Gets a piece's page id.</summary>
    /// <param name="id">The piece's id.</param>
    /// <returns>The page id.</returns>
    public static string PiecePage(string id) => PiecePrefix + id;

    /// <summary>Makes a readable name from a prefab's name: Pickable_Barley1 becomes Barley, and MineRock_Copper becomes Copper.</summary>
    /// <param name="prefabName">The prefab's name.</param>
    /// <returns>The readable name, or empty when nothing is left.</returns>
    public static string Pretty(string? prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return string.Empty;
        }

        var words = new List<string>();
        foreach (string token in prefabName!.Split(new[] { '_', ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string bare = token.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            if (bare.Length > 0 && !IsPrefabWord(bare))
            {
                words.Add(SplitCamel(bare));
            }
        }

        return string.Join(" ", words);
    }

    /// <summary>Builds the book of what the player has discovered.</summary>
    /// <param name="data">The game's data.</param>
    /// <param name="knowledge">What the player has discovered.</param>
    /// <returns>The book.</returns>
    public static CompendiumBook Build(CompendiumData data, CompendiumKnowledge knowledge)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (knowledge == null)
        {
            throw new ArgumentNullException(nameof(knowledge));
        }

        var names = new Names(data, knowledge);
        var pages = new List<CompendiumPage>();
        foreach (CompendiumBiome biome in AllBiomes.Where(knowledge.Biomes.Contains))
        {
            pages.Add(BiomePageOf(biome, data, names));
        }

        foreach (ItemRecord item in data.Items.Where(i => names.HasItem(i.Id)))
        {
            pages.Add(ItemPageOf(item, data, names));
        }

        foreach (CreatureRecord creature in data.Creatures.Where(c => names.HasCreature(c.Id)))
        {
            pages.Add(CreaturePageOf(creature, data, names));
        }

        foreach (PieceRecord piece in data.Pieces.Where(names.HasPiece))
        {
            pages.Add(PiecePageOf(piece, names));
        }

        return new CompendiumBook(pages);
    }

    private static CompendiumPage ItemPageOf(ItemRecord item, CompendiumData data, Names names)
    {
        var sections = new List<CompendiumSection>();
        AddSection(sections, StringKeys.CompendiumSectionStats, ItemStats(item));
        AddSection(sections, StringKeys.CompendiumSectionCrafting, data.Recipes.Where(r => r.ResultId == item.Id).SelectMany(r => RecipeLines(r, names)).ToList());
        AddSection(sections, StringKeys.CompendiumSectionUsedIn, UsedIn(item.Id, data, names));
        AddSection(sections, StringKeys.CompendiumSectionDroppedBy, DroppedBy(item.Id, data, names));
        AddSection(sections, StringKeys.CompendiumSectionGathered, GatheredFrom(item.Id, data, names));
        return new CompendiumPage(ItemPage(item.Id), CompendiumKind.Item, item.Name, CategoryName(item.Category), item.Description, sections);
    }

    private static List<CompendiumLine> ItemStats(ItemRecord item)
    {
        var lines = new List<CompendiumLine>();
        AddIf(lines, item.Weight > 0f, StringKeys.CompendiumStatWeight, Number(item.Weight));
        AddIf(lines, item.MaxStack > 1, StringKeys.CompendiumStatStack, Number(item.MaxStack));
        AddIf(lines, item.MaxQuality > 1, StringKeys.CompendiumStatQuality, Number(item.MaxQuality));
        AddIf(lines, item.Value > 0, StringKeys.CompendiumStatValue, Number(item.Value));
        AddIf(lines, !item.Teleportable, StringKeys.CompendiumStatNoPortal);
        AddIf(lines, item.Durability > 0f, StringKeys.CompendiumStatDurability, Number(item.Durability));
        AddIf(lines, item.Armor > 0f && item.ArmorPerLevel <= 0f, StringKeys.CompendiumStatArmor, Number(item.Armor));
        AddIf(lines, item.Armor > 0f && item.ArmorPerLevel > 0f, StringKeys.CompendiumStatArmorLevels, Number(item.Armor), Number(item.ArmorPerLevel));
        AddIf(lines, item.BlockPower > 0f, StringKeys.CompendiumStatBlock, Number(item.BlockPower));
        AddIf(lines, Math.Abs(item.Movement) > 0.0001f, StringKeys.CompendiumStatMovement, Number(item.Movement * 100f));
        foreach (DamageAmount damage in item.Damage.Where(d => d.Amount > 0f).OrderBy(d => d.Type))
        {
            lines.Add(new CompendiumLine(Text(StringKeys.CompendiumStatDamage, DamageName(damage.Type), Number(damage.Amount))));
        }

        AddIf(lines, item.FoodHealth > 0f, StringKeys.CompendiumStatFoodHealth, Number(item.FoodHealth));
        AddIf(lines, item.FoodStamina > 0f, StringKeys.CompendiumStatFoodStamina, Number(item.FoodStamina));
        AddIf(lines, item.FoodEitr > 0f, StringKeys.CompendiumStatFoodEitr, Number(item.FoodEitr));
        AddIf(lines, item.FoodRegen > 0f, StringKeys.CompendiumStatFoodRegen, Number(item.FoodRegen));
        AddIf(lines, item.FoodSeconds > 0f, StringKeys.CompendiumStatFoodTime, Number(item.FoodSeconds / 60f));
        AddIf(lines, item.SetName.Length > 0, StringKeys.CompendiumStatSet, item.SetName);
        return lines;
    }

    private static IEnumerable<CompendiumLine> RecipeLines(RecipeRecord recipe, Names names)
    {
        if (recipe.Station.Length == 0)
        {
            yield return new CompendiumLine(Text(StringKeys.CompendiumRecipeByHand, Number(recipe.Amount)));
        }
        else if (recipe.StationLevel > 1)
        {
            yield return new CompendiumLine(Text(StringKeys.CompendiumRecipeStationLevel, Number(recipe.Amount), names.Station(recipe.Station), Number(recipe.StationLevel)));
        }
        else
        {
            yield return new CompendiumLine(Text(StringKeys.CompendiumRecipeStation, Number(recipe.Amount), names.Station(recipe.Station)));
        }

        foreach (IngredientRecord ingredient in recipe.Ingredients)
        {
            yield return IngredientLine(ingredient, names);
        }
    }

    private static CompendiumLine IngredientLine(IngredientRecord ingredient, Names names)
    {
        string name = names.Item(ingredient.ItemId);
        string text = ingredient.AmountPerLevel > 0
            ? Text(StringKeys.CompendiumIngredientLevels, name, Number(ingredient.Amount), Number(ingredient.AmountPerLevel))
            : Text(StringKeys.CompendiumIngredient, name, Number(ingredient.Amount));
        return new CompendiumLine(text, names.ItemLink(ingredient.ItemId));
    }

    private static List<CompendiumLine> UsedIn(string itemId, CompendiumData data, Names names)
    {
        IEnumerable<CompendiumLine> crafted = data.Recipes
            .Where(r => r.ResultId != itemId && names.HasItem(r.ResultId) && r.Ingredients.Any(i => i.ItemId == itemId))
            .Select(r => new CompendiumLine(names.Item(r.ResultId), names.ItemLink(r.ResultId)));
        IEnumerable<CompendiumLine> built = data.Pieces
            .Where(p => names.HasPiece(p) && p.Cost.Any(i => i.ItemId == itemId))
            .Select(p => new CompendiumLine(p.Name, PiecePage(p.Id)));
        return crafted.Concat(built).GroupBy(l => l.Text + l.Link).Select(g => g.First()).OrderBy(l => l.Text, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<CompendiumLine> DroppedBy(string itemId, CompendiumData data, Names names) =>
        data.Creatures
            .Where(c => names.HasCreature(c.Id))
            .SelectMany(c => c.Drops.Where(d => d.ItemId == itemId).Select(d => new CompendiumLine(DropText(c.Name, d), CreaturePage(c.Id))))
            .OrderBy(l => l.Text, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Lists only sources in biomes the player has been to, so a page can't send them somewhere new.</summary>
    private static List<CompendiumLine> GatheredFrom(string itemId, CompendiumData data, Names names) =>
        data.Resources
            .Where(r => r.Name.Length > 0 && r.ItemIds.Contains(itemId) && r.Biomes.Any(names.HasBiome))
            .Select(r => new CompendiumLine(Text(StringKeys.CompendiumGathered, r.Name, string.Join(", ", r.Biomes.Where(names.HasBiome).Distinct().OrderBy(b => b).Select(BiomeName)))))
            .GroupBy(l => l.Text)
            .Select(g => g.First())
            .OrderBy(l => l.Text, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string DropText(string name, CreatureDropRecord drop) =>
        Text(StringKeys.CompendiumDrop, name, Range(drop.Min, drop.Max), Number(drop.Chance * 100f));

    private static string Range(int min, int max) => min >= max ? Number(max) : Number(min) + "-" + Number(max);

    private static CompendiumPage CreaturePageOf(CreatureRecord creature, CompendiumData data, Names names)
    {
        var stats = new List<CompendiumLine>();
        AddIf(stats, creature.Boss, StringKeys.CompendiumStatBoss);
        AddIf(stats, creature.Health > 0f, StringKeys.CompendiumStatHealth, Number(creature.Health));
        AddIf(stats, creature.StrongestAttack > 0f, StringKeys.CompendiumStatStrongest, Number(creature.StrongestAttack));
        var sections = new List<CompendiumSection>();
        AddSection(sections, StringKeys.CompendiumSectionStats, stats);
        AddSection(
            sections,
            StringKeys.CompendiumSectionDamage,
            creature.Responses.Where(r => r.Response != DamageResponse.Normal).OrderBy(r => r.Type).Select(r => new CompendiumLine(Text(StringKeys.CompendiumResponse, DamageName(r.Type), ResponseName(r.Response)))).ToList());
        AddSection(
            sections,
            StringKeys.CompendiumSectionDrops,
            creature.Drops.Where(d => names.HasItem(d.ItemId)).Select(d => new CompendiumLine(DropText(names.Item(d.ItemId), d), names.ItemLink(d.ItemId))).ToList());
        AddSection(sections, StringKeys.CompendiumSectionAppears, Appears(creature.Id, data, names));
        return new CompendiumPage(CreaturePage(creature.Id), CompendiumKind.Creature, creature.Name, creature.Faction, string.Empty, sections);
    }

    private static List<CompendiumLine> Appears(string creatureId, CompendiumData data, Names names) =>
        data.Spawns
            .Where(s => s.CreatureId == creatureId && names.HasBiome(s.Biome))
            .GroupBy(s => s.Biome)
            .OrderBy(g => g.Key)
            .Select(g => new CompendiumLine(Text(StringKeys.CompendiumResponse, BiomeName(g.Key), TimeName(g.Any(s => s.Day), g.Any(s => s.Night))), BiomePage(g.Key)))
            .ToList();

    private static string TimeName(bool day, bool night)
    {
        if (day && night)
        {
            return StringTable.English.Get(StringKeys.CompendiumTimeAny);
        }

        return StringTable.English.Get(night ? StringKeys.CompendiumTimeNight : StringKeys.CompendiumTimeDay);
    }

    private static CompendiumPage BiomePageOf(CompendiumBiome biome, CompendiumData data, Names names)
    {
        var sections = new List<CompendiumSection>();
        AddSection(
            sections,
            StringKeys.CompendiumSectionCreatures,
            data.Spawns.Where(s => s.Biome == biome).Select(s => s.CreatureId).Distinct().Where(names.HasCreature)
                .Select(id => new CompendiumLine(names.Creature(id), CreaturePage(id))).OrderBy(l => l.Text, StringComparer.OrdinalIgnoreCase).ToList());
        AddSection(
            sections,
            StringKeys.CompendiumSectionResources,
            data.Resources.Where(r => r.Biomes.Contains(biome)).SelectMany(r => r.ItemIds).Distinct().Where(names.HasItem)
                .Select(id => new CompendiumLine(names.Item(id), names.ItemLink(id))).OrderBy(l => l.Text, StringComparer.OrdinalIgnoreCase).ToList());
        return new CompendiumPage(BiomePage(biome), CompendiumKind.Biome, BiomeName(biome), string.Empty, BiomeBlurb(biome), sections);
    }

    private static CompendiumPage PiecePageOf(PieceRecord piece, Names names)
    {
        var stats = new List<CompendiumLine>();
        AddIf(stats, piece.Comfort > 0, StringKeys.CompendiumStatComfort, Number(piece.Comfort));
        AddIf(stats, piece.Station.Length > 0, StringKeys.CompendiumStatStation, names.Station(piece.Station));
        var sections = new List<CompendiumSection>();
        AddSection(sections, StringKeys.CompendiumSectionStats, stats);
        AddSection(sections, StringKeys.CompendiumSectionCost, piece.Cost.Select(i => IngredientLine(i, names)).ToList());
        return new CompendiumPage(PiecePage(piece.Id), CompendiumKind.Piece, piece.Name, piece.Category, piece.Description, sections);
    }

    private static void AddSection(List<CompendiumSection> sections, string headingKey, List<CompendiumLine> lines)
    {
        if (lines.Count > 0)
        {
            sections.Add(new CompendiumSection(StringTable.English.Get(headingKey), lines));
        }
    }

    private static void AddIf(List<CompendiumLine> lines, bool condition, string key, params object?[] args)
    {
        if (condition)
        {
            lines.Add(new CompendiumLine(Text(key, args)));
        }
    }

    private static string Text(string key, params object?[] args) => StringTable.English.Format(key, args);

    private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static bool IsPrefabWord(string word) =>
        word.Equals("Pickable", StringComparison.OrdinalIgnoreCase)
        || word.Equals("MineRock", StringComparison.OrdinalIgnoreCase)
        || word.Equals("Clone", StringComparison.OrdinalIgnoreCase);

    private static string SplitCamel(string word)
    {
        var text = new StringBuilder(word.Length + 4);
        for (int i = 0; i < word.Length; i++)
        {
            if (i > 0 && char.IsUpper(word[i]) && char.IsLower(word[i - 1]))
            {
                text.Append(' ');
            }

            text.Append(word[i]);
        }

        return text.ToString();
    }

    private static string CategoryName(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Weapon:
                return StringTable.English.Get(StringKeys.CompendiumCategoryWeapon);
            case ItemCategory.Armor:
                return StringTable.English.Get(StringKeys.CompendiumCategoryArmor);
            case ItemCategory.Shield:
                return StringTable.English.Get(StringKeys.CompendiumCategoryShield);
            case ItemCategory.Tool:
                return StringTable.English.Get(StringKeys.CompendiumCategoryTool);
            case ItemCategory.Ammo:
                return StringTable.English.Get(StringKeys.CompendiumCategoryAmmo);
            case ItemCategory.Food:
                return StringTable.English.Get(StringKeys.CompendiumCategoryFood);
            case ItemCategory.Potion:
                return StringTable.English.Get(StringKeys.CompendiumCategoryPotion);
            case ItemCategory.Trophy:
                return StringTable.English.Get(StringKeys.CompendiumCategoryTrophy);
            case ItemCategory.Material:
                return StringTable.English.Get(StringKeys.CompendiumCategoryMaterial);
            default:
                return StringTable.English.Get(StringKeys.CompendiumCategoryMisc);
        }
    }

    private static string DamageName(CompendiumDamage type)
    {
        switch (type)
        {
            case CompendiumDamage.Blunt:
                return StringTable.English.Get(StringKeys.CompendiumDamageBlunt);
            case CompendiumDamage.Slash:
                return StringTable.English.Get(StringKeys.CompendiumDamageSlash);
            case CompendiumDamage.Pierce:
                return StringTable.English.Get(StringKeys.CompendiumDamagePierce);
            case CompendiumDamage.Chop:
                return StringTable.English.Get(StringKeys.CompendiumDamageChop);
            case CompendiumDamage.Pickaxe:
                return StringTable.English.Get(StringKeys.CompendiumDamagePickaxe);
            case CompendiumDamage.Fire:
                return StringTable.English.Get(StringKeys.CompendiumDamageFire);
            case CompendiumDamage.Frost:
                return StringTable.English.Get(StringKeys.CompendiumDamageFrost);
            case CompendiumDamage.Lightning:
                return StringTable.English.Get(StringKeys.CompendiumDamageLightning);
            case CompendiumDamage.Poison:
                return StringTable.English.Get(StringKeys.CompendiumDamagePoison);
            case CompendiumDamage.Spirit:
                return StringTable.English.Get(StringKeys.CompendiumDamageSpirit);
            default:
                return StringTable.English.Get(StringKeys.CompendiumDamageTrue);
        }
    }

    private static string ResponseName(DamageResponse response)
    {
        switch (response)
        {
            case DamageResponse.VeryWeak:
                return StringTable.English.Get(StringKeys.CompendiumResponseVeryWeak);
            case DamageResponse.Weak:
                return StringTable.English.Get(StringKeys.CompendiumResponseWeak);
            case DamageResponse.SlightlyWeak:
                return StringTable.English.Get(StringKeys.CompendiumResponseSlightlyWeak);
            case DamageResponse.SlightlyResistant:
                return StringTable.English.Get(StringKeys.CompendiumResponseSlightlyResistant);
            case DamageResponse.Resistant:
                return StringTable.English.Get(StringKeys.CompendiumResponseResistant);
            case DamageResponse.VeryResistant:
                return StringTable.English.Get(StringKeys.CompendiumResponseVeryResistant);
            case DamageResponse.Immune:
                return StringTable.English.Get(StringKeys.CompendiumResponseImmune);
            default:
                return StringTable.English.Get(StringKeys.CompendiumResponseNormal);
        }
    }

    /// <summary>Gets a biome's display name.</summary>
    /// <param name="biome">The biome.</param>
    /// <returns>The name.</returns>
    public static string BiomeName(CompendiumBiome biome)
    {
        switch (biome)
        {
            case CompendiumBiome.BlackForest:
                return StringTable.English.Get(StringKeys.CompendiumBiomeBlackForest);
            case CompendiumBiome.Swamp:
                return StringTable.English.Get(StringKeys.CompendiumBiomeSwamp);
            case CompendiumBiome.Mountain:
                return StringTable.English.Get(StringKeys.CompendiumBiomeMountain);
            case CompendiumBiome.Plains:
                return StringTable.English.Get(StringKeys.CompendiumBiomePlains);
            case CompendiumBiome.Mistlands:
                return StringTable.English.Get(StringKeys.CompendiumBiomeMistlands);
            case CompendiumBiome.Ashlands:
                return StringTable.English.Get(StringKeys.CompendiumBiomeAshlands);
            case CompendiumBiome.DeepNorth:
                return StringTable.English.Get(StringKeys.CompendiumBiomeDeepNorth);
            case CompendiumBiome.Ocean:
                return StringTable.English.Get(StringKeys.CompendiumBiomeOcean);
            default:
                return StringTable.English.Get(StringKeys.CompendiumBiomeMeadows);
        }
    }

    private static string BiomeBlurb(CompendiumBiome biome)
    {
        switch (biome)
        {
            case CompendiumBiome.BlackForest:
                return StringTable.English.Get(StringKeys.CompendiumBlurbBlackForest);
            case CompendiumBiome.Swamp:
                return StringTable.English.Get(StringKeys.CompendiumBlurbSwamp);
            case CompendiumBiome.Mountain:
                return StringTable.English.Get(StringKeys.CompendiumBlurbMountain);
            case CompendiumBiome.Plains:
                return StringTable.English.Get(StringKeys.CompendiumBlurbPlains);
            case CompendiumBiome.Mistlands:
                return StringTable.English.Get(StringKeys.CompendiumBlurbMistlands);
            case CompendiumBiome.Ashlands:
                return StringTable.English.Get(StringKeys.CompendiumBlurbAshlands);
            case CompendiumBiome.DeepNorth:
                return StringTable.English.Get(StringKeys.CompendiumBlurbDeepNorth);
            case CompendiumBiome.Ocean:
                return StringTable.English.Get(StringKeys.CompendiumBlurbOcean);
            default:
                return StringTable.English.Get(StringKeys.CompendiumBlurbMeadows);
        }
    }

    /// <summary>Answers what's discovered, and names only that. A creature also counts as discovered when the player holds its trophy, which covers saves from before the mod kept sightings.</summary>
    private sealed class Names
    {
        private readonly Dictionary<string, string> items = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> creatures = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly CompendiumKnowledge knowledge;

        internal Names(CompendiumData data, CompendiumKnowledge knowledge)
        {
            this.knowledge = knowledge;
            var trophies = new HashSet<string>(StringComparer.Ordinal);
            foreach (ItemRecord item in data.Items.Where(i => i.Name.Length > 0 && knowledge.Items.Contains(i.Id)))
            {
                items[item.Id] = item.Name;
                if (item.Category == ItemCategory.Trophy)
                {
                    trophies.Add(item.Id);
                }
            }

            foreach (CreatureRecord creature in data.Creatures.Where(c => c.Name.Length > 0))
            {
                if (knowledge.Creatures.Contains(creature.Id) || creature.Drops.Any(d => trophies.Contains(d.ItemId)))
                {
                    creatures[creature.Id] = creature.Name;
                }
            }
        }

        internal bool HasItem(string id) => items.ContainsKey(id);

        internal bool HasCreature(string id) => creatures.ContainsKey(id);

        internal bool HasBiome(CompendiumBiome biome) => knowledge.Biomes.Contains(biome);

        internal bool HasPiece(PieceRecord piece) => piece.Name.Length > 0 && knowledge.Pieces.Contains(piece.Id);

        internal string Item(string id) => items.TryGetValue(id, out string? name) ? name : StringTable.English.Get(StringKeys.CompendiumUndiscoveredItem);

        internal string Creature(string id) => creatures[id];

        internal string Station(string name) => knowledge.Stations.Contains(name) ? name : StringTable.English.Get(StringKeys.CompendiumUndiscoveredStation);

        internal string? ItemLink(string id) => HasItem(id) ? ItemPage(id) : null;
    }
}
