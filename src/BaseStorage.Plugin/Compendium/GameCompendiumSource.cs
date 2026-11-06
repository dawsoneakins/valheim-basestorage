namespace BaseStorage.Plugin.Compendium;

using System.Collections.Generic;
using BaseStorage.Core.Compendium;
using UnityEngine;

/// <summary>Reads the live game's items, recipes, creatures, spawns, resources, places and pieces into plain records, so mods' additions are included. Main thread only.</summary>
internal sealed class GameCompendiumSource
{
    private static readonly Heightmap.Biome[] GameBiomes =
    {
        Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp, Heightmap.Biome.Mountain, Heightmap.Biome.Plains,
        Heightmap.Biome.Mistlands, Heightmap.Biome.AshLands, Heightmap.Biome.DeepNorth, Heightmap.Biome.Ocean,
    };

    private static readonly CompendiumBiome[] CoreBiomes =
    {
        CompendiumBiome.Meadows, CompendiumBiome.BlackForest, CompendiumBiome.Swamp, CompendiumBiome.Mountain, CompendiumBiome.Plains,
        CompendiumBiome.Mistlands, CompendiumBiome.Ashlands, CompendiumBiome.DeepNorth, CompendiumBiome.Ocean,
    };

    private readonly CompendiumData data = new CompendiumData();
    private readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
    private readonly HashSet<string> spawnSeen = new HashSet<string>();
    private readonly Dictionary<string, string> itemTokens = new Dictionary<string, string>();
    private readonly Dictionary<string, string> pieceTokens = new Dictionary<string, string>();
    private readonly Dictionary<string, string> stationTokens = new Dictionary<string, string>();

    /// <summary>Gets the icon of each page that has one.</summary>
    internal IReadOnlyDictionary<string, Sprite> Icons => icons;

    /// <summary>Reads everything.</summary>
    /// <returns>The data.</returns>
    internal CompendiumData Read()
    {
        ReadItems();
        ReadRecipes();
        ReadPrefabs();
        ReadWorld();
        return data;
    }

    /// <summary>Maps one of the game's biomes.</summary>
    /// <param name="biome">The game's biome.</param>
    /// <param name="mapped">The mapped biome.</param>
    /// <returns>False when it isn't one of the listed biomes.</returns>
    internal static bool TryMapBiome(Heightmap.Biome biome, out CompendiumBiome mapped)
    {
        for (int i = 0; i < GameBiomes.Length; i++)
        {
            if (GameBiomes[i] == biome)
            {
                mapped = CoreBiomes[i];
                return true;
            }
        }

        mapped = default;
        return false;
    }

    /// <summary>Reads what a player has discovered: the game's own records, plus the mod's sightings.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The knowledge.</returns>
    internal CompendiumKnowledge ReadKnowledge(Player player)
    {
        var knowledge = new CompendiumKnowledge();
        foreach (KeyValuePair<string, string> item in itemTokens)
        {
            if (player.m_knownMaterial.Contains(item.Value) || player.m_knownRecipes.Contains(item.Value))
            {
                knowledge.Items.Add(item.Key);
            }
        }

        foreach (KeyValuePair<string, string> piece in pieceTokens)
        {
            if (player.m_knownRecipes.Contains(piece.Value))
            {
                knowledge.Pieces.Add(piece.Key);
            }
        }

        foreach (KeyValuePair<string, string> station in stationTokens)
        {
            if (player.m_knownStations.ContainsKey(station.Value))
            {
                knowledge.Stations.Add(station.Key);
            }
        }

        // A known biome's entry can carry a variant's prefix, so the check looks for the biome's name inside it.
        for (int i = 0; i < GameBiomes.Length; i++)
        {
            string name = BiomeSector.GetBiomeName(GameBiomes[i]);
            foreach (string known in player.m_knownBiome)
            {
                if (known.IndexOf(name, System.StringComparison.Ordinal) >= 0)
                {
                    knowledge.Biomes.Add(CoreBiomes[i]);
                    break;
                }
            }
        }

        CompendiumSightingTracker.For(player).CopyTo(knowledge);
        return knowledge;
    }

    private static string Localized(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return string.Empty;
        }

        string text = Localization.instance != null ? Localization.instance.Localize(token) : token!;

        // A token with no translation comes back in brackets or as itself; those are the game's unfinished and test entries.
        return text.Length == 0 || text[0] == '[' || text[0] == '$' ? string.Empty : text;
    }

    private static ItemCategory CategoryOf(ItemDrop.ItemData.SharedData shared)
    {
        switch (shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
            case ItemDrop.ItemData.ItemType.Bow:
                return ItemCategory.Weapon;
            case ItemDrop.ItemData.ItemType.Shield:
                return ItemCategory.Shield;
            case ItemDrop.ItemData.ItemType.Helmet:
            case ItemDrop.ItemData.ItemType.Chest:
            case ItemDrop.ItemData.ItemType.Legs:
            case ItemDrop.ItemData.ItemType.Hands:
            case ItemDrop.ItemData.ItemType.Shoulder:
            case ItemDrop.ItemData.ItemType.Utility:
            case ItemDrop.ItemData.ItemType.Trinket:
                return ItemCategory.Armor;
            case ItemDrop.ItemData.ItemType.Tool:
            case ItemDrop.ItemData.ItemType.Torch:
                return ItemCategory.Tool;
            case ItemDrop.ItemData.ItemType.Ammo:
            case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                return ItemCategory.Ammo;
            case ItemDrop.ItemData.ItemType.Consumable:
                return shared.m_food > 0f ? ItemCategory.Food : ItemCategory.Potion;
            case ItemDrop.ItemData.ItemType.Trophy:
                return ItemCategory.Trophy;
            case ItemDrop.ItemData.ItemType.Material:
                return ItemCategory.Material;
            default:
                return ItemCategory.Misc;
        }
    }

    private static void AddDamage(List<DamageAmount> list, HitData.DamageTypes damages)
    {
        Add(list, CompendiumDamage.True, damages.m_damage);
        Add(list, CompendiumDamage.Blunt, damages.m_blunt);
        Add(list, CompendiumDamage.Slash, damages.m_slash);
        Add(list, CompendiumDamage.Pierce, damages.m_pierce);
        Add(list, CompendiumDamage.Chop, damages.m_chop);
        Add(list, CompendiumDamage.Pickaxe, damages.m_pickaxe);
        Add(list, CompendiumDamage.Fire, damages.m_fire);
        Add(list, CompendiumDamage.Frost, damages.m_frost);
        Add(list, CompendiumDamage.Lightning, damages.m_lightning);
        Add(list, CompendiumDamage.Poison, damages.m_poison);
        Add(list, CompendiumDamage.Spirit, damages.m_spirit);
    }

    private static void Add(List<DamageAmount> list, CompendiumDamage type, float amount) =>
        list.Add(new DamageAmount { Type = type, Amount = amount });

    private static float TotalCombatDamage(HitData.DamageTypes d) =>
        d.m_damage + d.m_blunt + d.m_slash + d.m_pierce + d.m_fire + d.m_frost + d.m_lightning + d.m_poison + d.m_spirit;

    private static DamageResponse ResponseOf(HitData.DamageModifier modifier)
    {
        switch (modifier)
        {
            case HitData.DamageModifier.VeryWeak:
                return DamageResponse.VeryWeak;
            case HitData.DamageModifier.Weak:
                return DamageResponse.Weak;
            case HitData.DamageModifier.SlightlyWeak:
                return DamageResponse.SlightlyWeak;
            case HitData.DamageModifier.SlightlyResistant:
                return DamageResponse.SlightlyResistant;
            case HitData.DamageModifier.Resistant:
                return DamageResponse.Resistant;
            case HitData.DamageModifier.VeryResistant:
                return DamageResponse.VeryResistant;
            case HitData.DamageModifier.Immune:
            case HitData.DamageModifier.Ignore:
                return DamageResponse.Immune;
            default:
                return DamageResponse.Normal;
        }
    }

    private static void AddResponse(List<DamageResponseEntry> list, CompendiumDamage type, HitData.DamageModifier modifier) =>
        list.Add(new DamageResponseEntry { Type = type, Response = ResponseOf(modifier) });

    private static void AddBiomes(List<CompendiumBiome> list, Heightmap.Biome flags)
    {
        for (int i = 0; i < GameBiomes.Length; i++)
        {
            if ((flags & GameBiomes[i]) != 0 && !list.Contains(CoreBiomes[i]))
            {
                list.Add(CoreBiomes[i]);
            }
        }
    }

    private static void AddTable(List<string> itemIds, DropTable? table)
    {
        if (table?.m_drops == null)
        {
            return;
        }

        foreach (DropTable.DropData drop in table.m_drops)
        {
            if (drop.m_item != null && !itemIds.Contains(drop.m_item.name))
            {
                itemIds.Add(drop.m_item.name);
            }
        }
    }

    private static void CollectDrops(GameObject prefab, List<string> itemIds, int depth)
    {
        Pickable pickable = prefab.GetComponent<Pickable>();
        if (pickable != null)
        {
            if (pickable.m_itemPrefab != null && !itemIds.Contains(pickable.m_itemPrefab.name))
            {
                itemIds.Add(pickable.m_itemPrefab.name);
            }

            AddTable(itemIds, pickable.m_extraDrops);
        }

        AddTable(itemIds, prefab.GetComponent<MineRock>()?.m_dropItems);
        AddTable(itemIds, prefab.GetComponent<MineRock5>()?.m_dropItems);
        AddTable(itemIds, prefab.GetComponent<DropOnDestroyed>()?.m_dropWhenDestroyed);
        AddTable(itemIds, prefab.GetComponent<TreeLog>()?.m_dropWhenDestroyed);
        TreeBase tree = prefab.GetComponent<TreeBase>();
        if (tree != null)
        {
            AddTable(itemIds, tree.m_dropWhenDestroyed);
            if (depth > 0 && tree.m_logPrefab != null)
            {
                CollectDrops(tree.m_logPrefab, itemIds, depth - 1);
            }
        }

        Destructible destructible = prefab.GetComponent<Destructible>();
        if (depth > 0 && destructible != null && destructible.m_spawnWhenDestroyed != null)
        {
            CollectDrops(destructible.m_spawnWhenDestroyed, itemIds, depth - 1);
        }
    }

    private void ReadItems()
    {
        foreach (GameObject prefab in ObjectDB.instance.m_items)
        {
            ItemDrop? drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
            ItemDrop.ItemData.SharedData? shared = drop?.m_itemData?.m_shared;
            if (shared == null)
            {
                continue;
            }

            var item = new ItemRecord
            {
                Id = prefab!.name,
                Name = Localized(shared.m_name),
                Description = Localized(shared.m_description),
                Category = CategoryOf(shared),
                Weight = shared.m_weight,
                MaxStack = shared.m_maxStackSize,
                MaxQuality = shared.m_maxQuality,
                Value = shared.m_value,
                Teleportable = shared.m_teleportable,
                Durability = shared.m_useDurability ? shared.m_maxDurability : 0f,
                Armor = shared.m_armor,
                ArmorPerLevel = shared.m_armorPerLevel,
                BlockPower = shared.m_blockPower,
                Movement = shared.m_movementModifier,
                FoodHealth = shared.m_food,
                FoodStamina = shared.m_foodStamina,
                FoodEitr = shared.m_foodEitr,
                FoodRegen = shared.m_foodRegen,
                FoodSeconds = shared.m_foodBurnTime,
                SetName = shared.m_setName ?? string.Empty,
            };
            AddDamage(item.Damage, shared.m_damages);
            data.Items.Add(item);
            itemTokens[item.Id] = shared.m_name;
            if (shared.m_icons != null && shared.m_icons.Length > 0 && shared.m_icons[0] != null)
            {
                icons[CompendiumBuilder.ItemPage(item.Id)] = shared.m_icons[0];
            }
        }
    }

    private string StationName(CraftingStation? station)
    {
        if (station == null)
        {
            return string.Empty;
        }

        string name = Localized(station.m_name);
        if (name.Length > 0)
        {
            stationTokens[name] = station.m_name;
        }

        return name;
    }

    private void ReadRecipes()
    {
        foreach (Recipe recipe in ObjectDB.instance.m_recipes)
        {
            if (recipe == null || recipe.m_item == null || !recipe.m_enabled)
            {
                continue;
            }

            var record = new RecipeRecord
            {
                ResultId = recipe.m_item.name,
                Amount = recipe.m_amount,
                Station = StationName(recipe.m_craftingStation),
                StationLevel = recipe.m_minStationLevel,
            };
            AddIngredients(record.Ingredients, recipe.m_resources);
            data.Recipes.Add(record);
        }
    }

    private void AddIngredients(List<IngredientRecord> list, Piece.Requirement[]? requirements)
    {
        if (requirements == null)
        {
            return;
        }

        foreach (Piece.Requirement requirement in requirements)
        {
            if (requirement?.m_resItem != null)
            {
                list.Add(new IngredientRecord { ItemId = requirement.m_resItem.name, Amount = requirement.m_amount, AmountPerLevel = requirement.m_amountPerLevel });
            }
        }
    }

    private void ReadPrefabs()
    {
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            ReadCreature(prefab);
            ReadPiece(prefab);
            ReadSpawnSystem(prefab.GetComponent<SpawnSystem>());
        }

        foreach (SpawnSystem system in SpawnSystem.m_instances)
        {
            ReadSpawnSystem(system);
        }
    }

    private void ReadCreature(GameObject prefab)
    {
        Character character = prefab.GetComponent<Character>();
        if (character == null || character is Player)
        {
            return;
        }

        var creature = new CreatureRecord
        {
            Id = prefab.name,
            Name = Localized(character.m_name),
            Faction = CompendiumBuilder.Pretty(character.m_faction.ToString()),
            Health = character.m_health,
            Boss = character.m_boss,
        };
        HitData.DamageModifiers modifiers = character.m_damageModifiers;
        AddResponse(creature.Responses, CompendiumDamage.Blunt, modifiers.m_blunt);
        AddResponse(creature.Responses, CompendiumDamage.Slash, modifiers.m_slash);
        AddResponse(creature.Responses, CompendiumDamage.Pierce, modifiers.m_pierce);
        AddResponse(creature.Responses, CompendiumDamage.Fire, modifiers.m_fire);
        AddResponse(creature.Responses, CompendiumDamage.Frost, modifiers.m_frost);
        AddResponse(creature.Responses, CompendiumDamage.Lightning, modifiers.m_lightning);
        AddResponse(creature.Responses, CompendiumDamage.Poison, modifiers.m_poison);
        AddResponse(creature.Responses, CompendiumDamage.Spirit, modifiers.m_spirit);
        creature.StrongestAttack = StrongestAttack(character as Humanoid);
        ReadDrops(creature, prefab.GetComponent<CharacterDrop>());
        data.Creatures.Add(creature);
    }

    private float StrongestAttack(Humanoid? humanoid)
    {
        float strongest = 0f;
        if (humanoid?.m_defaultItems == null)
        {
            return strongest;
        }

        foreach (GameObject weapon in humanoid.m_defaultItems)
        {
            ItemDrop? drop = weapon == null ? null : weapon.GetComponent<ItemDrop>();
            if (drop?.m_itemData?.m_shared != null)
            {
                strongest = Mathf.Max(strongest, TotalCombatDamage(drop.m_itemData.m_shared.m_damages));
            }
        }

        return strongest;
    }

    private void ReadDrops(CreatureRecord creature, CharacterDrop? drops)
    {
        if (drops?.m_drops == null)
        {
            return;
        }

        foreach (CharacterDrop.Drop drop in drops.m_drops)
        {
            if (drop.m_prefab != null)
            {
                creature.Drops.Add(new CreatureDropRecord { ItemId = drop.m_prefab.name, Min = drop.m_amountMin, Max = drop.m_amountMax, Chance = drop.m_chance });
            }
        }
    }

    private void ReadPiece(GameObject prefab)
    {
        Piece piece = prefab.GetComponent<Piece>();
        if (piece == null || !piece.m_enabled || piece.m_resources == null || piece.m_resources.Length == 0)
        {
            return;
        }

        var record = new PieceRecord
        {
            Id = prefab.name,
            Name = Localized(piece.m_name),
            Description = Localized(piece.m_description),
            Category = CompendiumBuilder.Pretty(piece.m_category.ToString()),
            Comfort = piece.m_comfort,
            Station = StationName(piece.m_craftingStation),
        };
        AddIngredients(record.Cost, piece.m_resources);
        data.Pieces.Add(record);
        pieceTokens[record.Id] = piece.m_name;
        if (piece.m_icon != null)
        {
            icons[CompendiumBuilder.PiecePage(record.Id)] = piece.m_icon;
        }
    }

    private void ReadSpawnSystem(SpawnSystem? system)
    {
        if (system?.m_spawnLists == null)
        {
            return;
        }

        foreach (SpawnSystemList list in system.m_spawnLists)
        {
            if (list?.m_spawners == null)
            {
                continue;
            }

            foreach (SpawnSystem.SpawnData spawn in list.m_spawners)
            {
                if (spawn.m_enabled && spawn.m_prefab != null)
                {
                    AddSpawn(spawn);
                }
            }
        }
    }

    private void AddSpawn(SpawnSystem.SpawnData spawn)
    {
        var biomes = new List<CompendiumBiome>();
        AddBiomes(biomes, spawn.m_biome);
        foreach (CompendiumBiome biome in biomes)
        {
            // Several spawn systems share the same lists, so each entry is kept once.
            string key = spawn.m_prefab.name + "|" + biome + "|" + spawn.m_spawnAtDay + "|" + spawn.m_spawnAtNight;
            if (spawnSeen.Add(key))
            {
                data.Spawns.Add(new SpawnRecord { CreatureId = spawn.m_prefab.name, Biome = biome, Day = spawn.m_spawnAtDay, Night = spawn.m_spawnAtNight });
            }
        }
    }

    private void ReadWorld()
    {
        var biomesByPrefab = new Dictionary<string, List<CompendiumBiome>>();
        var vegetationPrefabs = new List<GameObject>();
        ZoneSystem? zones = ZoneSystem.instance;
        if (zones?.m_vegetation != null)
        {
            foreach (ZoneSystem.ZoneVegetation vegetation in zones.m_vegetation)
            {
                if (vegetation.m_enable && vegetation.m_prefab != null)
                {
                    if (!biomesByPrefab.TryGetValue(vegetation.m_prefab.name, out List<CompendiumBiome> list))
                    {
                        list = new List<CompendiumBiome>();
                        biomesByPrefab[vegetation.m_prefab.name] = list;
                        vegetationPrefabs.Add(vegetation.m_prefab);
                    }

                    AddBiomes(list, vegetation.m_biome);
                }
            }
        }

        foreach (GameObject prefab in vegetationPrefabs)
        {
            ReadResource(prefab, biomesByPrefab[prefab.name]);
        }

        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            if (prefab != null && !biomesByPrefab.ContainsKey(prefab.name))
            {
                ReadResource(prefab, new List<CompendiumBiome>());
            }
        }
    }

    private void ReadResource(GameObject prefab, List<CompendiumBiome> biomes)
    {
        var items = new List<string>();
        CollectDrops(prefab, items, 2);
        if (items.Count == 0)
        {
            return;
        }

        var record = new ResourceRecord { Name = CompendiumBuilder.Pretty(prefab.name) };
        record.ItemIds.AddRange(items);
        record.Biomes.AddRange(biomes);
        data.Resources.Add(record);
    }
}
