namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;

/// <summary>
/// Literal oracles for the default categories (ADR-GWIN-029). Not a test class. Every value is copied from
/// contracts/default-categories-contracts.md v1 (3.1, 3.2, 4.4, 4.5, 4.6, 4.7). Expected values are never read from
/// <see cref="DefaultCategories"/>, <see cref="FallbackCategories"/> or the parser.
/// </summary>
internal static class DefaultCategoryFixtures
{
    /// <summary>The number of default prefabs over all 14 categories (contract 3.2).</summary>
    internal const int TotalCount = 273;

    /// <summary>The 14 category names in spec order (contract 3.1).</summary>
    internal static readonly string[] Names =
    {
        "Metals & Ores", "Wood", "Stone & Minerals", "Hides & Leather", "Fibres & Cloth", "Raw Food", "Cooked Food",
        "Meads & Potions", "Seeds & Saplings", "Monster Drops", "Trophies", "Gear", "Ammo", "Misc",
    };

    /// <summary>The pinned count of each category, in <see cref="Names"/> order (contract 3.2).</summary>
    internal static readonly int[] Counts = { 21, 8, 8, 8, 4, 54, 33, 33, 9, 34, 24, 8, 15, 14 };

    /// <summary>The default prefab list of each category, in <see cref="Names"/> order and table order (contract 3.2).</summary>
    internal static readonly string[][] Prefabs =
    {
        new[]
        {
            "CopperOre", "Copper", "TinOre", "Tin", "Bronze", "BronzeNails", "CopperScrap", "IronScrap", "IronOre", "Iron",
            "IronNails", "SilverOre", "Silver", "BlackMetalScrap", "BlackMetal", "FlametalOre", "Flametal", "FlametalOreNew",
            "FlametalNew", "Chain", "MechanicalSpring",
        },
        new[] { "Wood", "FineWood", "RoundLog", "ElderBark", "YggdrasilWood", "Blackwood", "Resin", "Coal" },
        new[] { "Stone", "Flint", "Obsidian", "Crystal", "BlackMarble", "Grausten", "SulfurStone", "Tar" },
        new[] { "DeerHide", "LeatherScraps", "TrollHide", "WolfPelt", "LoxPelt", "ScaleHide", "BjornHide", "AskHide" },
        new[] { "Flax", "LinenThread", "JuteRed", "JuteBlue" },
        new[]
        {
            "RawMeat", "DeerMeat", "NeckTail", "WolfMeat", "LoxMeat", "SerpentMeat", "FishRaw", "ChickenMeat", "HareMeat",
            "BugMeat", "AsksvinMeat", "VoltureMeat", "BoneMawSerpentMeat", "Egg", "ChickenEgg", "Raspberry", "Blueberries",
            "Cloudberry", "Vineberry", "Mushroom", "MushroomYellow", "MushroomBlue", "MushroomJotunPuffs", "MushroomMagecap",
            "MushroomSmokePuff", "Carrot", "Turnip", "Onion", "Fiddleheadfern", "Barley", "BarleyFlour", "Thistle", "Dandelion",
            "Honey", "Sap", "RoyalJelly", "BreadDough", "LoxPieUncooked", "FishAndBreadUncooked", "MeatPlatterUncooked",
            "HoneyGlazedChickenUncooked", "MagicallyStuffedShroomUncooked", "Fish1", "Fish2", "Fish3", "Fish4", "Fish5", "Fish6",
            "Fish7", "Fish8", "Fish9", "Fish10", "Fish11", "Fish12",
        },
        new[]
        {
            "CookedMeat", "CookedDeerMeat", "NeckTailGrilled", "CookedWolfMeat", "CookedLoxMeat", "SerpentMeatCooked", "FishCooked",
            "CookedChickenMeat", "CookedHareMeat", "CookedBugMeat", "CookedEgg", "QueensJam", "CarrotSoup", "TurnipStew", "Sausages",
            "BlackSoup", "ShocklateSmoothie", "SerpentStew", "FishWraps", "LoxPie", "Bread", "BloodPudding", "OnionSoup", "Eyescream",
            "FishAndBread", "MeatPlatter", "HoneyGlazedChicken", "MisthareSupreme", "Salad", "MushroomOmelette",
            "MagicallyStuffedShroom", "YggdrasilPorridge", "SeekerAspic",
        },
        new[]
        {
            "MeadBaseHealthMinor", "MeadHealthMinor", "MeadBaseHealthMedium", "MeadHealthMedium", "MeadBaseHealthMajor",
            "MeadHealthMajor", "MeadBaseHealthLingering", "MeadHealthLingering", "MeadBaseStaminaMinor", "MeadStaminaMinor",
            "MeadBaseStaminaMedium", "MeadStaminaMedium", "MeadBaseStaminaLingering", "MeadStaminaLingering", "MeadBaseEitrMinor",
            "MeadEitrMinor", "MeadBaseEitrLingering", "MeadEitrLingering", "MeadBasePoisonResist", "MeadPoisonResist",
            "MeadBaseFrostResist", "MeadFrostResist", "MeadBaseTasty", "MeadTasty", "BarleyWineBase", "BarleyWine",
            "MeadBugRepellent", "MeadBzerker", "MeadHasty", "MeadLightfoot", "MeadStrength", "MeadSwimmer", "MeadTrollPheromones",
        },
        new[]
        {
            "BeechSeeds", "PineCone", "FirCone", "BirchSeeds", "Acorn", "CarrotSeeds", "TurnipSeeds", "OnionSeeds", "VineberrySeeds",
        },
        new[]
        {
            "BoneFragments", "Feathers", "GreydwarfEye", "SurtlingCore", "AncientSeed", "HardAntler", "Guck", "Ooze", "Entrails",
            "WitheredBone", "Bloodbag", "Chitin", "SerpentScale", "WolfFang", "WolfClaw", "FreezeGland", "DragonEgg", "DragonTear",
            "Needle", "YmirRemains", "BlackCore", "Carapace", "Mandible", "Softtissue", "GiantBloodSack", "Wisp", "DvergrNeedle",
            "DvergrKeyFragment", "CelestialFeather", "MorgenSinew", "MorgenHeart", "AskBladder", "MoltenCore", "CharredBone",
        },
        new[]
        {
            "TrophyBoar", "TrophyDeer", "TrophyNeck", "TrophyEikthyr", "TrophyGreydwarf", "TrophyGreydwarfBrute",
            "TrophyGreydwarfShaman", "TrophySkeleton", "TrophyFrostTroll", "TrophyTheElder", "TrophyDraugr", "TrophyLeech",
            "TrophyBlob", "TrophyBonemass", "TrophyWolf", "TrophyHatchling", "TrophyDragonQueen", "TrophyLox", "TrophyDeathsquito",
            "TrophyGoblin", "TrophyGoblinKing", "TrophySeeker", "TrophySeekerQueen", "TrophyFader",
        },
        new[] { "Hammer", "Hoe", "Cultivator", "FishingRod", "Torch", "BeltStrength", "Wishbone", "Demister" },
        new[]
        {
            "ArrowWood", "ArrowFlint", "ArrowBronze", "ArrowIron", "ArrowSilver", "ArrowObsidian", "ArrowPoison", "ArrowFire",
            "ArrowFrost", "ArrowNeedle", "ArrowCarapace", "BoltBone", "BoltIron", "BoltBlackmetal", "BoltCarapace",
        },
        new[]
        {
            "Coins", "Ruby", "Amber", "AmberPearl", "SilverNecklace", "FishingBait", "FishingBaitForest", "FishingBaitSwamp",
            "FishingBaitCave", "FishingBaitPlains", "FishingBaitOcean", "FishingBaitMistlands", "FishingBaitAshlands",
            "FishingBaitDeepNorth",
        },
    };

    /// <summary>The 4.4 header, 7 lines, each ending in a line feed (line 7 is empty).</summary>
    internal const string Header =
        "# BaseStorage categories file (BaseStorage.categories.txt)\n" +
        "# One line per category: Category Name: PrefabA, PrefabB, ...\n" +
        "# Lines starting with # and blank lines are ignored. A prefab belongs to the first category that lists it.\n" +
        "# Items not listed here fall back by type: food to Cooked Food, trophies to Trophies,\n" +
        "# weapons, armour, tools and shields to Gear, ammo to Ammo, everything else to Misc.\n" +
        "# BaseStorage wrote this file on first run and never overwrites it, so your edits are kept.\n" +
        "\n";

    /// <summary>The 4.5 body: the 14 category lines with no prefabs (no trailing space after the colon).</summary>
    internal const string EmptyBody =
        "Metals & Ores:\n" +
        "Wood:\n" +
        "Stone & Minerals:\n" +
        "Hides & Leather:\n" +
        "Fibres & Cloth:\n" +
        "Raw Food:\n" +
        "Cooked Food:\n" +
        "Meads & Potions:\n" +
        "Seeds & Saplings:\n" +
        "Monster Drops:\n" +
        "Trophies:\n" +
        "Gear:\n" +
        "Ammo:\n" +
        "Misc:\n";

    /// <summary>The 4.6 body for fixture S: the 14 category lines.</summary>
    internal const string SBody =
        "Metals & Ores: CopperOre, Copper\n" +
        "Wood: Wood, Resin\n" +
        "Stone & Minerals: Stone\n" +
        "Hides & Leather: DeerHide\n" +
        "Fibres & Cloth: Flax\n" +
        "Raw Food: Raspberry\n" +
        "Cooked Food: CookedMeat, SerpentStew, MyMod_HoneyCake\n" +
        "Meads & Potions: MeadHealthMinor\n" +
        "Seeds & Saplings: BeechSeeds\n" +
        "Monster Drops: BoneFragments\n" +
        "Trophies: TrophyDeer, TrophySerpent\n" +
        "Gear: Hammer, ArmorBronzeChest, PickaxeAntler, ShieldWood, SwordBronze\n" +
        "Ammo: ArrowWood, ArrowCharred\n" +
        "Misc: Coins, ModdedGem\n";

    /// <summary>The full expected text for an empty item list (header plus the 4.5 body).</summary>
    internal const string ExpectedEmpty = Header + EmptyBody;

    /// <summary>The full expected text for fixture S (header plus the 4.6 body).</summary>
    internal const string ExpectedS = Header + SBody;

    /// <summary>
    /// Fixture S of contract 4.6, in contract order: prefab, kind, whether the default table maps it, and the category it is
    /// written under. Row 26 repeats row 14.
    /// </summary>
    internal static readonly (string Prefab, ItemKind Kind, bool Mapped, string WrittenUnder)[] S =
    {
        ("SwordBronze", ItemKind.Weapon, false, "Gear"),
        ("CopperOre", ItemKind.Other, true, "Metals & Ores"),
        ("TrophySerpent", ItemKind.Trophy, false, "Trophies"),
        ("Copper", ItemKind.Other, true, "Metals & Ores"),
        ("MyMod_HoneyCake", ItemKind.Food, false, "Cooked Food"),
        ("Raspberry", ItemKind.Food, true, "Raw Food"),
        ("ArmorBronzeChest", ItemKind.Armour, false, "Gear"),
        ("PickaxeAntler", ItemKind.Tool, false, "Gear"),
        ("ShieldWood", ItemKind.Shield, false, "Gear"),
        ("ArrowCharred", ItemKind.Ammo, false, "Ammo"),
        ("ArrowWood", ItemKind.Ammo, true, "Ammo"),
        ("MeadHealthMinor", ItemKind.Other, true, "Meads & Potions"),
        ("Resin", ItemKind.Other, true, "Wood"),
        ("Wood", ItemKind.Other, true, "Wood"),
        ("ModdedGem", ItemKind.Other, false, "Misc"),
        ("CookedMeat", ItemKind.Food, true, "Cooked Food"),
        ("TrophyDeer", ItemKind.Trophy, true, "Trophies"),
        ("Coins", ItemKind.Other, true, "Misc"),
        ("Hammer", ItemKind.Tool, true, "Gear"),
        ("DeerHide", ItemKind.Other, true, "Hides & Leather"),
        ("Flax", ItemKind.Other, true, "Fibres & Cloth"),
        ("BeechSeeds", ItemKind.Other, true, "Seeds & Saplings"),
        ("BoneFragments", ItemKind.Other, true, "Monster Drops"),
        ("Stone", ItemKind.Other, true, "Stone & Minerals"),
        ("SerpentStew", ItemKind.Food, true, "Cooked Food"),
        ("Wood", ItemKind.Other, true, "Wood"),
    };

    /// <summary>Normalises CRLF to LF before any text comparison (spec AC-9).</summary>
    internal static string Normalise(string text) => BaseStorage.Core.Tests.Repo.RepoRoot.Normalise(text);

    /// <summary>The 26 items of fixture S as generator input, in contract order.</summary>
    internal static CategoryItem[] SItems() => S.Select(row => new CategoryItem(row.Prefab, row.Kind)).ToArray();

    /// <summary>The category the default table maps a prefab to, or null when the table does not list it (literal lookup).</summary>
    internal static string? LiteralCategoryOf(string prefab)
    {
        for (int i = 0; i < Prefabs.Length; i++)
        {
            if (Array.IndexOf(Prefabs[i], prefab) >= 0)
            {
                return Names[i];
            }
        }

        return null;
    }

    /// <summary>The literal fallback category of a kind (contract 4.4 header and 2.2 of category-contracts; undefined kinds are Other).</summary>
    internal static string LiteralFallback(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.Food:
                return "Cooked Food";
            case ItemKind.Trophy:
                return "Trophies";
            case ItemKind.Weapon:
            case ItemKind.Armour:
            case ItemKind.Tool:
            case ItemKind.Shield:
                return "Gear";
            case ItemKind.Ammo:
                return "Ammo";
            default:
                return "Misc";
        }
    }

    /// <summary>The G3 category of a prefab from literal data only: the mapping, else the earliest-listed fallback of its kinds.</summary>
    internal static string LiteralG3(string prefab, IEnumerable<ItemKind> kinds)
    {
        string? mapped = LiteralCategoryOf(prefab);
        if (mapped is not null)
        {
            return mapped;
        }

        int best = kinds.Min(kind => Array.IndexOf(Names, LiteralFallback(kind)));
        return Names[best];
    }

    /// <summary>The 9 duplicate and edge-kind rows of contract 4.7: the items and the one non-empty category line they produce.</summary>
    internal static IReadOnlyList<(CategoryItem[] Items, string Line)> EdgeRows() => new (CategoryItem[], string)[]
    {
        (new[] { Item("Wood", ItemKind.Other), Item("Wood", ItemKind.Other) }, "Wood: Wood"),
        (new[] { Item("Raspberry", ItemKind.Food), Item("Raspberry", ItemKind.Other) }, "Raw Food: Raspberry"),
        (new[] { Item("Lantern_Mod", ItemKind.Other), Item("Lantern_Mod", ItemKind.Weapon) }, "Gear: Lantern_Mod"),
        (new[] { Item("Lantern_Mod", ItemKind.Weapon), Item("Lantern_Mod", ItemKind.Other) }, "Gear: Lantern_Mod"),
        (new[] { Item("Spice_Mod", ItemKind.Food), Item("Spice_Mod", ItemKind.Trophy) }, "Cooked Food: Spice_Mod"),
        (new[] { Item("Resin", (ItemKind)99) }, "Wood: Resin"),
        (new[] { Item("Glowstone_Mod", (ItemKind)99) }, "Misc: Glowstone_Mod"),
        (new[] { Item("copperore", ItemKind.Other) }, "Misc: copperore"),
        (new[] { Item("Gem: Blue", ItemKind.Other), Item("#Gem", ItemKind.Other) }, "Misc: #Gem, Gem: Blue"),
    };

    /// <summary>
    /// The ObjectDB-sized list of GW-U-452 (about 650 items, deterministic): every default prefab with a random kind, 40 unmapped
    /// items per defined kind, 10 with kind 99, and 30 repeats.
    /// </summary>
    internal static List<CategoryItem> ObjectDbSized()
    {
        var random = new Random(20261026);
        var items = new List<CategoryItem>();
        foreach (string[] list in Prefabs)
        {
            foreach (string prefab in list)
            {
                items.Add(new CategoryItem(prefab, (ItemKind)random.Next(0, 8)));
            }
        }

        for (int kind = 0; kind < 8; kind++)
        {
            for (int n = 0; n < 40; n++)
            {
                items.Add(new CategoryItem("Mod_" + ((ItemKind)kind).ToString() + "_" + n.ToString(CultureInfo.InvariantCulture), (ItemKind)kind));
            }
        }

        for (int n = 0; n < 10; n++)
        {
            items.Add(new CategoryItem("Mod_99_" + n.ToString(CultureInfo.InvariantCulture), (ItemKind)99));
        }

        int pool = items.Count;
        for (int r = 0; r < 30; r++)
        {
            items.Add(items[random.Next(pool)]);
        }

        return items;
    }

    /// <summary>
    /// The (category, token) pairs of a generated text: skips lines that start with '#' and blank lines, splits each category line at
    /// its first ':' and each list at ',' and trims the tokens. Empty tokens are dropped.
    /// </summary>
    internal static List<(string Category, string Token)> ParseTokens(string text)
    {
        var result = new List<(string Category, string Token)>();
        foreach (string line in Normalise(text).Split('\n'))
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            int colon = line.IndexOf(':');
            if (colon < 0)
            {
                result.Add((string.Empty, line));
                continue;
            }

            string category = line.Substring(0, colon).Trim();
            foreach (string token in line.Substring(colon + 1).Split(','))
            {
                string trimmed = token.Trim();
                if (trimmed.Length > 0)
                {
                    result.Add((category, trimmed));
                }
            }
        }

        return result;
    }

    /// <summary>The 14 table lines of contract 3.2 in the file format: "Name: p1, p2, ...", joined by line feeds.</summary>
    internal static string TableLines() =>
        string.Join("\n", Names.Select((name, i) => name + ": " + string.Join(", ", Prefabs[i])));

    private static CategoryItem Item(string prefabName, ItemKind kind) => new CategoryItem(prefabName, kind);
}
