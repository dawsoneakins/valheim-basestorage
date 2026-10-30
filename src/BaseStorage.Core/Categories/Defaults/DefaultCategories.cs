namespace BaseStorage.Core.Categories.Defaults;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Categories;

/// <summary>
/// The 14 shipped default categories in spec order, with their default prefab mapping (spec AC-1, AC-2, AC-3).
/// Every collection it returns is read-only.
/// </summary>
public static class DefaultCategories
{
    private static readonly string[] NameArray =
    {
        "Metals & Ores",
        "Wood",
        "Stone & Minerals",
        "Hides & Leather",
        "Fibres & Cloth",
        "Raw Food",
        "Cooked Food",
        "Meads & Potions",
        "Seeds & Saplings",
        "Monster Drops",
        "Trophies",
        "Gear",
        "Ammo",
        "Misc",
    };

    private static readonly string[][] PrefabTable =
    {
        new[] { "CopperOre", "Copper", "TinOre", "Tin", "Bronze", "BronzeNails", "CopperScrap", "IronScrap", "IronOre", "Iron", "IronNails", "SilverOre", "Silver", "BlackMetalScrap", "BlackMetal", "FlametalOre", "Flametal", "FlametalOreNew", "FlametalNew", "Chain", "MechanicalSpring" },
        new[] { "Wood", "FineWood", "RoundLog", "ElderBark", "YggdrasilWood", "Blackwood", "Resin", "Coal" },
        new[] { "Stone", "Flint", "Obsidian", "Crystal", "BlackMarble", "Grausten", "SulfurStone", "Tar" },
        new[] { "DeerHide", "LeatherScraps", "TrollHide", "WolfPelt", "LoxPelt", "ScaleHide", "BjornHide", "AskHide" },
        new[] { "Flax", "LinenThread", "JuteRed", "JuteBlue" },
        new[] { "RawMeat", "DeerMeat", "NeckTail", "WolfMeat", "LoxMeat", "SerpentMeat", "FishRaw", "ChickenMeat", "HareMeat", "BugMeat", "AsksvinMeat", "VoltureMeat", "BoneMawSerpentMeat", "Egg", "ChickenEgg", "Raspberry", "Blueberries", "Cloudberry", "Vineberry", "Mushroom", "MushroomYellow", "MushroomBlue", "MushroomJotunPuffs", "MushroomMagecap", "MushroomSmokePuff", "Carrot", "Turnip", "Onion", "Fiddleheadfern", "Barley", "BarleyFlour", "Thistle", "Dandelion", "Honey", "Sap", "RoyalJelly", "BreadDough", "LoxPieUncooked", "FishAndBreadUncooked", "MeatPlatterUncooked", "HoneyGlazedChickenUncooked", "MagicallyStuffedShroomUncooked", "Fish1", "Fish2", "Fish3", "Fish4", "Fish5", "Fish6", "Fish7", "Fish8", "Fish9", "Fish10", "Fish11", "Fish12" },
        new[] { "CookedMeat", "CookedDeerMeat", "NeckTailGrilled", "CookedWolfMeat", "CookedLoxMeat", "SerpentMeatCooked", "FishCooked", "CookedChickenMeat", "CookedHareMeat", "CookedBugMeat", "CookedEgg", "QueensJam", "CarrotSoup", "TurnipStew", "Sausages", "BlackSoup", "ShocklateSmoothie", "SerpentStew", "FishWraps", "LoxPie", "Bread", "BloodPudding", "OnionSoup", "Eyescream", "FishAndBread", "MeatPlatter", "HoneyGlazedChicken", "MisthareSupreme", "Salad", "MushroomOmelette", "MagicallyStuffedShroom", "YggdrasilPorridge", "SeekerAspic" },
        new[] { "MeadBaseHealthMinor", "MeadHealthMinor", "MeadBaseHealthMedium", "MeadHealthMedium", "MeadBaseHealthMajor", "MeadHealthMajor", "MeadBaseHealthLingering", "MeadHealthLingering", "MeadBaseStaminaMinor", "MeadStaminaMinor", "MeadBaseStaminaMedium", "MeadStaminaMedium", "MeadBaseStaminaLingering", "MeadStaminaLingering", "MeadBaseEitrMinor", "MeadEitrMinor", "MeadBaseEitrLingering", "MeadEitrLingering", "MeadBasePoisonResist", "MeadPoisonResist", "MeadBaseFrostResist", "MeadFrostResist", "MeadBaseTasty", "MeadTasty", "BarleyWineBase", "BarleyWine", "MeadBugRepellent", "MeadBzerker", "MeadHasty", "MeadLightfoot", "MeadStrength", "MeadSwimmer", "MeadTrollPheromones" },
        new[] { "BeechSeeds", "PineCone", "FirCone", "BirchSeeds", "Acorn", "CarrotSeeds", "TurnipSeeds", "OnionSeeds", "VineberrySeeds" },
        new[] { "BoneFragments", "Feathers", "GreydwarfEye", "SurtlingCore", "AncientSeed", "HardAntler", "Guck", "Ooze", "Entrails", "WitheredBone", "Bloodbag", "Chitin", "SerpentScale", "WolfFang", "WolfClaw", "FreezeGland", "DragonEgg", "DragonTear", "Needle", "YmirRemains", "BlackCore", "Carapace", "Mandible", "Softtissue", "GiantBloodSack", "Wisp", "DvergrNeedle", "DvergrKeyFragment", "CelestialFeather", "MorgenSinew", "MorgenHeart", "AskBladder", "MoltenCore", "CharredBone" },
        new[] { "TrophyBoar", "TrophyDeer", "TrophyNeck", "TrophyEikthyr", "TrophyGreydwarf", "TrophyGreydwarfBrute", "TrophyGreydwarfShaman", "TrophySkeleton", "TrophyFrostTroll", "TrophyTheElder", "TrophyDraugr", "TrophyLeech", "TrophyBlob", "TrophyBonemass", "TrophyWolf", "TrophyHatchling", "TrophyDragonQueen", "TrophyLox", "TrophyDeathsquito", "TrophyGoblin", "TrophyGoblinKing", "TrophySeeker", "TrophySeekerQueen", "TrophyFader" },
        new[] { "Hammer", "Hoe", "Cultivator", "FishingRod", "Torch", "BeltStrength", "Wishbone", "Demister" },
        new[] { "ArrowWood", "ArrowFlint", "ArrowBronze", "ArrowIron", "ArrowSilver", "ArrowObsidian", "ArrowPoison", "ArrowFire", "ArrowFrost", "ArrowNeedle", "ArrowCarapace", "BoltBone", "BoltIron", "BoltBlackmetal", "BoltCarapace" },
        new[] { "Coins", "Ruby", "Amber", "AmberPearl", "SilverNecklace", "FishingBait", "FishingBaitForest", "FishingBaitSwamp", "FishingBaitCave", "FishingBaitPlains", "FishingBaitOcean", "FishingBaitMistlands", "FishingBaitAshlands", "FishingBaitDeepNorth" },
    };

    private static readonly ReadOnlyCollection<string> NameList = new ReadOnlyCollection<string>(NameArray);

    private static readonly ReadOnlyCollection<string>[] PrefabLists = BuildLists();

    private static readonly CategoryCatalog SharedCatalog = BuildCatalog();

    /// <summary>The 14 shipped category names in spec order. Read-only.</summary>
    public static IReadOnlyList<string> Names => NameList;

    /// <summary>The 14 categories with the default mapping as a catalog. Immutable and shared.</summary>
    public static CategoryCatalog Catalog => SharedCatalog;

    /// <summary>The default prefab list of one shipped category, in table order. Read-only.</summary>
    /// <param name="categoryName">One of the 14 names of <see cref="Names"/>, ordinal-equal.</param>
    /// <returns>The default prefab names of that category, in table order.</returns>
    /// <exception cref="ArgumentNullException">categoryName is null.</exception>
    /// <exception cref="ArgumentException">categoryName is not one of the 14 names.</exception>
    public static IReadOnlyList<string> GetPrefabs(string categoryName)
    {
        if (categoryName is null)
        {
            throw new ArgumentNullException(nameof(categoryName));
        }

        for (int i = 0; i < NameArray.Length; i++)
        {
            if (string.Equals(NameArray[i], categoryName, StringComparison.Ordinal))
            {
                return PrefabLists[i];
            }
        }

        throw new ArgumentException("Not one of the shipped category names.", nameof(categoryName));
    }

    private static ReadOnlyCollection<string>[] BuildLists()
    {
        var lists = new ReadOnlyCollection<string>[PrefabTable.Length];
        for (int i = 0; i < lists.Length; i++)
        {
            lists[i] = new ReadOnlyCollection<string>(PrefabTable[i]);
        }

        return lists;
    }

    private static CategoryCatalog BuildCatalog()
    {
        var builder = new CategoryCatalogBuilder();
        for (int i = 0; i < NameArray.Length; i++)
        {
            builder.AddCategory(NameArray[i]);
        }

        for (int i = 0; i < PrefabTable.Length; i++)
        {
            foreach (string prefab in PrefabTable[i])
            {
                if (builder.FindOwner(prefab) < 0)
                {
                    builder.AddPrefab(i, prefab);
                }
            }
        }

        return builder.Build();
    }
}
