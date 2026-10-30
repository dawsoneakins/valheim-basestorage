namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// The deterministic random-base generator of the S-E004-007 fuzzer (AC-4.5). Base <c>index</c> is generated from
/// <c>Mix(Seed, index)</c> alone, so a logged seed and index reproduce it. The base-level choices (catalog, threshold, options,
/// chest count) are drawn first and the chests then one after another, so <c>Generate(index, cap)</c> returns the first
/// <c>cap</c> chests of the full base unchanged: the shrinker relies on this prefix property.
/// </summary>
internal static class RandomBaseGenerator
{
    /// <summary>The fixed seed of the corpus. Change it only on purpose; the coverage test pins facts about this corpus.</summary>
    internal const int Seed = 20261026;

    /// <summary>The largest chest count of a base.</summary>
    internal const int MaxChests = 60;

    /// <summary>The number of bases in the corpus, split into shards by the test.</summary>
    internal const int CorpusSize = 1000;

    /// <summary>Every 50th base has exactly <see cref="MaxChests"/> chests, so the corpus always exercises the largest size.</summary>
    internal const int LargeBaseEvery = 50;

    /// <summary>The grid sizes a chest can have: the vanilla 8/16/24/32 slot grids and odd sizes.</summary>
    internal static readonly (int Width, int Height)[] Sizes =
    {
        (1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (7, 1), (4, 2), (3, 3), (13, 1), (8, 2), (8, 3), (9, 3), (8, 4),
    };

    /// <summary>The thresholds a base can use.</summary>
    internal static readonly int[] Thresholds = { 0, 34, 50, 60, 75, 100 };

    /// <summary>The usual max stacks of a non-gear item.</summary>
    internal static readonly int[] MaxStacks = { 1, 10, 20, 30, 50, 100 };

    /// <summary>The player names an in-use entry can carry; the blank one gives the generic reason.</summary>
    internal static readonly string[] PlayerNames = { "Erik", "Astrid", "   " };

    /// <summary>The flag combinations other than plain player-built.</summary>
    internal static readonly ContainerFlags[] OddFlags =
    {
        ContainerFlags.None,
        ContainerFlags.Tombstone,
        ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle,
        ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied,
        ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther,
        ContainerFlags.Tombstone | ContainerFlags.InUseByOther,
    };

    /// <summary>The catalog texts a base can use: the contract 6.1 catalog, a wider one with fallback names spelled by the file, and the empty catalog.</summary>
    internal static readonly string?[] CatalogTexts =
    {
        CatalogText,
        "Metals & Ores: CopperOre, Copper, TinOre, Tin\n" +
        "Wood: Wood, FineWood, RoundLog\n" +
        "Stone: Stone, Flint\n" +
        "Cooked Food: CookedMeat, QueensJam, SerpentStew\n" +
        "Trophies: TrophyDeer, TrophyBoar\n" +
        "Gear: SwordIron\n" +
        "Ammo: ArrowWood\n" +
        "misc: Resin, Coal\n" +
        "Valuables: Feathers\n",
        null,
    };

    /// <summary>Category names a persisted assignment can name beyond the catalog: an unknown one, a fallback the 6.1 catalog lacks, and fallbacks it has.</summary>
    internal static readonly string[] ExtraAssignmentNames = { "Iron", "Ammo", "Misc", "Gear" };

    /// <summary>The item table: mapped prefabs, kind-only prefabs (fallback categories) and prefabs with neither (Misc).</summary>
    internal static readonly ItemSpec[] Items =
    {
        new ItemSpec("CopperOre", null, false),
        new ItemSpec("Copper", null, false),
        new ItemSpec("TinOre", null, false),
        new ItemSpec("Tin", null, false),
        new ItemSpec("Wood", null, false),
        new ItemSpec("FineWood", null, false),
        new ItemSpec("RoundLog", null, false),
        new ItemSpec("Stone", null, false),
        new ItemSpec("Flint", null, false),
        new ItemSpec("CookedMeat", null, false),
        new ItemSpec("QueensJam", null, false),
        new ItemSpec("TrophyDeer", null, false),
        new ItemSpec("TrophyBoar", null, false),
        new ItemSpec("SwordIron", ItemKind.Weapon, true),
        new ItemSpec("AxeBronze", ItemKind.Tool, true),
        new ItemSpec("HelmetBronze", ItemKind.Armour, true),
        new ItemSpec("ShieldWood", ItemKind.Shield, true),
        new ItemSpec("ArrowWood", ItemKind.Ammo, false),
        new ItemSpec("SerpentStew", ItemKind.Food, false),
        new ItemSpec("TrophyGreydwarf", ItemKind.Trophy, false),
        new ItemSpec("Resin", ItemKind.Other, false),
        new ItemSpec("Coal", null, false),
        new ItemSpec("Feathers", null, false),
        new ItemSpec("GreydwarfEye", null, false),
    };

    /// <summary>One prefab of the item table.</summary>
    /// <param name="Prefab">The prefab name.</param>
    /// <param name="Kind">The kind in the kinds dictionary, or null when the prefab has no kind entry.</param>
    /// <param name="IsGear">True for an equipment item: max stack 1 and a quality of 1..4.</param>
    internal sealed record ItemSpec(string Prefab, ItemKind? Kind, bool IsGear);

    /// <summary>One generated base.</summary>
    /// <param name="Index">The base index in the corpus.</param>
    /// <param name="BaseSeed">The 64-bit seed the base was drawn from.</param>
    /// <param name="FullChestCount">The chest count of the full base, before any cap.</param>
    /// <param name="CatalogVariant">The index into <see cref="CatalogTexts"/>.</param>
    /// <param name="Input">The planner arguments.</param>
    internal sealed record GeneratedBase(int Index, ulong BaseSeed, int FullChestCount, int CatalogVariant, PlanInput Input);

    /// <summary>The item kinds of the fuzzer: every item of the table with a kind.</summary>
    internal static Dictionary<string, ItemKind> FuzzKinds()
    {
        var kinds = new Dictionary<string, ItemKind>(StringComparer.Ordinal);
        foreach (ItemSpec item in Items)
        {
            if (item.Kind is ItemKind kind)
            {
                kinds[item.Prefab] = kind;
            }
        }

        return kinds;
    }

    /// <summary>The catalog of a variant.</summary>
    /// <param name="variant">The index into <see cref="CatalogTexts"/>.</param>
    internal static CategoryCatalog CatalogOf(int variant)
    {
        string? text = CatalogTexts[variant];
        return text is null ? CategoryCatalog.Empty : CategoryFile.Parse(text).Catalog;
    }

    /// <summary>The seed of one base: the corpus seed in the high half and the index in the low half.</summary>
    /// <param name="seed">The corpus seed.</param>
    /// <param name="index">The base index.</param>
    internal static ulong Mix(int seed, int index) => ((ulong)(uint)seed << 32) | (uint)index;

    /// <summary>Generates base <paramref name="index"/> of the corpus, with at most <paramref name="chestCap"/> chests.</summary>
    /// <param name="index">The base index, 0..<see cref="CorpusSize"/>-1.</param>
    /// <param name="chestCap">The largest chest count to keep; the full base when larger than its count.</param>
    internal static GeneratedBase Generate(int index, int chestCap = MaxChests) => Generate(Seed, index, chestCap);

    /// <summary>Generates base <paramref name="index"/> of the corpus seeded by <paramref name="seed"/>.</summary>
    /// <param name="seed">The corpus seed.</param>
    /// <param name="index">The base index.</param>
    /// <param name="chestCap">The largest chest count to keep.</param>
    internal static GeneratedBase Generate(int seed, int index, int chestCap)
    {
        if (chestCap < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(chestCap), "Must be at least 1.");
        }

        ulong baseSeed = Mix(seed, index);
        var random = new FuzzRandom(baseSeed);

        int catalogVariant = random.Next(0, CatalogTexts.Length);
        CategoryCatalog catalog = CatalogOf(catalogVariant);
        int threshold = random.Pick(Thresholds);
        var options = new EligibilityOptions(excludeVehicles: random.Chance(70), keepChestsCraftable: random.Chance(50));
        int fullCount = ChestCount(random, index);

        var usedIds = new HashSet<uint>();
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        var inUseBy = new Dictionary<ContainerId, string>();
        var containers = new List<ContainerSnapshot>();
        int keep = Math.Min(fullCount, chestCap);
        WorldPosition previous = new WorldPosition(0f, 30f, 0f);
        for (int c = 0; c < keep; c++)
        {
            ContainerSnapshot chest = GenerateChest(random, catalog, usedIds, assignments, inUseBy, previous);
            previous = chest.Position;
            containers.Add(chest);
        }

        PlanInput input = new PlanInput(containers.ToArray(), assignments, inUseBy, catalog, FuzzKinds(), options, threshold);
        return new GeneratedBase(index, baseSeed, fullCount, catalogVariant, input);
    }

    /// <summary>Describes a base compactly: its base-level choices and one line per chest, so a failing base can become a fixture.</summary>
    /// <param name="generated">The base.</param>
    internal static string Describe(GeneratedBase generated)
    {
        PlanInput input = generated.Input;
        var text = new StringBuilder();
        text.Append("base ").Append(generated.Index.ToString(CultureInfo.InvariantCulture))
            .Append(" seed=0x").Append(generated.BaseSeed.ToString("X16", CultureInfo.InvariantCulture))
            .Append(" catalog=").Append(generated.CatalogVariant.ToString(CultureInfo.InvariantCulture))
            .Append(" threshold=").Append(input.Threshold.ToString(CultureInfo.InvariantCulture))
            .Append(" excludeVehicles=").Append(input.Options.ExcludeVehicles)
            .Append(" keepCraftable=").Append(input.Options.KeepChestsCraftable)
            .Append(" chests=").Append(input.Containers.Length.ToString(CultureInfo.InvariantCulture))
            .Append('/').Append(generated.FullChestCount.ToString(CultureInfo.InvariantCulture))
            .Append('\n');
        foreach (ContainerSnapshot chest in input.Containers)
        {
            text.Append("  #").Append(chest.Id.Id.ToString(CultureInfo.InvariantCulture))
                .Append(' ').Append(chest.Width.ToString(CultureInfo.InvariantCulture))
                .Append('x').Append(chest.Height.ToString(CultureInfo.InvariantCulture))
                .Append(" @(").Append(chest.Position.X.ToString("F1", CultureInfo.InvariantCulture))
                .Append(',').Append(chest.Position.Z.ToString("F1", CultureInfo.InvariantCulture))
                .Append(") ").Append(chest.Flags);
            if (input.Assignments.TryGetValue(chest.Id, out CategoryAssignment? assignment))
            {
                text.Append(assignment!.IsKeep ? " Keep" : " =" + assignment.CategoryName);
            }

            if (input.InUseBy.TryGetValue(chest.Id, out string? name))
            {
                text.Append(" inUse=\"").Append(name).Append('"');
            }

            text.Append(" [").Append(PlanAssert.Join(chest.Contents)).Append("]\n");
        }

        return text.ToString();
    }

    /// <summary>The chest count: every 50th base is the largest, most bases are small, and the rest spread to the maximum.</summary>
    private static int ChestCount(FuzzRandom random, int index)
    {
        if (index % LargeBaseEvery == LargeBaseEvery - 1)
        {
            return MaxChests;
        }

        return random.Chance(60) ? random.Next(1, 13) : random.Next(1, MaxChests + 1);
    }

    private static ContainerSnapshot GenerateChest(
        FuzzRandom random,
        CategoryCatalog catalog,
        HashSet<uint> usedIds,
        Dictionary<ContainerId, CategoryAssignment> assignments,
        Dictionary<ContainerId, string> inUseBy,
        WorldPosition previous)
    {
        uint rawId;
        do
        {
            rawId = (uint)random.Next(1, 50001);
        }
        while (!usedIds.Add(rawId));

        ContainerId id = Id(rawId);
        WorldPosition position = random.Chance(15)
            ? previous
            : new WorldPosition(random.Next(-40, 41) * 0.5f, 30f, random.Next(-40, 41) * 0.5f);
        (int width, int height) = random.Pick(Sizes);
        int slots = width * height;

        ContainerFlags flags = random.Chance(85) ? ContainerFlags.PlayerBuilt : random.Pick(OddFlags);
        if (random.Chance(6))
        {
            inUseBy[id] = random.Pick(PlayerNames);
        }

        string? assignedCategory = null;
        int assignmentRoll = random.Next(0, 100);
        if (assignmentRoll >= 80)
        {
            assignments[id] = CategoryAssignment.Keep;
        }
        else if (assignmentRoll >= 45)
        {
            assignedCategory = PickAssignmentName(random, catalog);
            assignments[id] = CategoryAssignment.ForCategory(assignedCategory);
        }

        ItemStackSnapshot[] contents = GenerateContents(random, catalog, slots, assignedCategory);
        return new ContainerSnapshot(id, position, width, height, contents, flags);
    }

    private static string PickAssignmentName(FuzzRandom random, CategoryCatalog catalog)
    {
        if (catalog.CategoryNames.Count == 0 || random.Chance(15))
        {
            return random.Pick(ExtraAssignmentNames);
        }

        return random.Pick(catalog.CategoryNames);
    }

    /// <summary>Draws the contents: empty, partial, full, or (rarely) invalid with too many stacks or an over-max stack.</summary>
    private static ItemStackSnapshot[] GenerateContents(FuzzRandom random, CategoryCatalog catalog, int slots, string? assignedCategory)
    {
        int mode = random.Next(0, 100);
        if (mode < 25)
        {
            return Array.Empty<ItemStackSnapshot>();
        }

        int stackCount;
        bool overMax = false;
        if (mode < 65)
        {
            stackCount = random.Next(1, slots + 1);
        }
        else if (mode < 90)
        {
            stackCount = slots;
        }
        else if (mode < 95)
        {
            stackCount = slots + random.Next(1, 4);
        }
        else
        {
            stackCount = random.Next(1, slots + 1);
            overMax = true;
        }

        // A dominant item makes auto-assign thresholds bite and gives merges something to merge into.
        IReadOnlyList<string> own = assignedCategory is null ? Array.Empty<string>() : OwnPrefabs(catalog, assignedCategory);
        ItemSpec? dominant = random.Chance(50) ? random.Pick(Items) : null;
        int ownPercent = own.Count == 0 ? 0 : random.Next(30, 90);

        var stacks = new ItemStackSnapshot[stackCount];
        for (int i = 0; i < stackCount; i++)
        {
            ItemSpec item;
            if (own.Count > 0 && random.Chance(ownPercent))
            {
                item = ItemOf(random.Pick(own));
            }
            else if (dominant is not null && random.Chance(60))
            {
                item = dominant;
            }
            else
            {
                item = random.Pick(Items);
            }

            stacks[i] = GenerateStack(random, item, overMax && i == stackCount - 1);
        }

        return stacks;
    }

    private static IReadOnlyList<string> OwnPrefabs(CategoryCatalog catalog, string categoryName)
    {
        int index = catalog.IndexOf(categoryName);
        return index < 0 ? Array.Empty<string>() : catalog.Categories[index].Prefabs;
    }

    private static ItemSpec ItemOf(string prefab)
    {
        foreach (ItemSpec item in Items)
        {
            if (item.Prefab == prefab)
            {
                return item;
            }
        }

        return new ItemSpec(prefab, null, false);
    }

    private static ItemStackSnapshot GenerateStack(FuzzRandom random, ItemSpec item, bool forceOverMax)
    {
        int maxStack;
        int quality;
        if (item.IsGear)
        {
            maxStack = random.Chance(90) ? 1 : random.Pick(MaxStacks);
            quality = random.Next(1, 5);
        }
        else
        {
            maxStack = random.Chance(2) ? int.MaxValue : random.Pick(MaxStacks);
            quality = random.Chance(10) ? random.Next(2, 4) : 1;
        }

        int count;
        if (forceOverMax)
        {
            if (maxStack == int.MaxValue)
            {
                maxStack = random.Pick(MaxStacks);
            }

            count = maxStack + random.Next(1, 10);
        }
        else
        {
            int fill = random.Next(0, 100);
            if (fill < 45)
            {
                count = maxStack;
            }
            else if (fill < 55)
            {
                count = 1;
            }
            else
            {
                count = maxStack == int.MaxValue ? random.Next(1, int.MaxValue) : random.Next(1, maxStack + 1);
            }
        }

        return new ItemStackSnapshot(item.Prefab, count, quality, maxStack);
    }
}
