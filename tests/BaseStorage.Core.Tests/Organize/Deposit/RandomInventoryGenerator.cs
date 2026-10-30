namespace BaseStorage.Core.Tests.Organize.Deposit;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// The deterministic random-Deposit generator of the S-E004-013 fuzzer (AC-4.9). Case <c>index</c> pairs base <c>index</c> of the
/// S-E004-007 corpus (<see cref="RandomBaseGenerator"/>) with an inventory, favourites and a never list drawn from a
/// <see cref="FuzzRandom"/> seeded with <c>Mix(Seed, index)</c>, so a logged seed and index reproduce the case exactly.
/// </summary>
internal static class RandomInventoryGenerator
{
    /// <summary>The fixed seed of the inventory corpus. Change it only on purpose.</summary>
    internal const int Seed = 20261027;

    /// <summary>The number of cases in the corpus, split into shards by the test.</summary>
    internal const int CorpusSize = 600;

    /// <summary>The never-list variants: the default list, an empty list, a random subset plus a random prefab, and odd-cased tokens.</summary>
    internal const int NeverListVariants = 4;

    /// <summary>The player inventory id: a peer id no chest of the base corpus uses.</summary>
    internal static readonly ContainerId PlayerId = new ContainerId(99L, 1u);

    /// <summary>The grid heights an inventory can have; 4 is vanilla.</summary>
    internal static readonly int[] Heights = { 4, 4, 4, 2, 3, 5 };

    /// <summary>The items of the default never list plus a food without a kind entry.</summary>
    internal static readonly NeverItemSpec[] NeverItems =
    {
        new NeverItemSpec("Hammer", ItemKind.Tool, InventoryItemTraits.None, 1),
        new NeverItemSpec("Hoe", ItemKind.Tool, InventoryItemTraits.None, 1),
        new NeverItemSpec("Cultivator", ItemKind.Tool, InventoryItemTraits.None, 1),
        new NeverItemSpec("FishingRod", ItemKind.Tool, InventoryItemTraits.None, 1),
        new NeverItemSpec("MeadHealthMinor", ItemKind.Other, InventoryItemTraits.Mead, 10),
        new NeverItemSpec("FishingBait", ItemKind.Other, InventoryItemTraits.Bait, 20),
        new NeverItemSpec("BoltIron", ItemKind.Ammo, InventoryItemTraits.Ammo, 20),
        new NeverItemSpec("Honey", null, InventoryItemTraits.Food, 50),
    };

    /// <summary>One never-list item of the table.</summary>
    /// <param name="Prefab">The prefab name.</param>
    /// <param name="Kind">The kind in the kinds dictionary, or null when the prefab has no kind entry.</param>
    /// <param name="Traits">The traits the Plugin would report; sometimes dropped by the generator.</param>
    /// <param name="MaxStack">The max stack.</param>
    internal sealed record NeverItemSpec(string Prefab, ItemKind? Kind, InventoryItemTraits Traits, int MaxStack);

    /// <summary>One generated case.</summary>
    /// <param name="Index">The case index in the corpus.</param>
    /// <param name="CaseSeed">The 64-bit seed the inventory was drawn from.</param>
    /// <param name="Player">The inventory.</param>
    /// <param name="Exclusions">The favourites and never list.</param>
    /// <param name="Input">The base arguments, from the S-E004-007 corpus with the never-list kinds added.</param>
    /// <param name="NeverListVariant">Which never-list variant was drawn.</param>
    internal sealed record GeneratedDeposit(int Index, ulong CaseSeed, PlayerInventorySnapshot Player, DepositExclusions Exclusions, PlanInput Input, int NeverListVariant);

    /// <summary>Generates case <paramref name="index"/> of the corpus.</summary>
    /// <param name="index">The case index, 0..<see cref="CorpusSize"/>-1.</param>
    internal static GeneratedDeposit Generate(int index) => Generate(Seed, index);

    /// <summary>Generates case <paramref name="index"/> of the corpus seeded by <paramref name="seed"/>.</summary>
    /// <param name="seed">The corpus seed.</param>
    /// <param name="index">The case index.</param>
    internal static GeneratedDeposit Generate(int seed, int index)
    {
        RandomBaseGenerator.GeneratedBase generated = RandomBaseGenerator.Generate(index);
        PlanInput baseInput = generated.Input;
        var kinds = new Dictionary<string, ItemKind>(baseInput.Kinds, StringComparer.Ordinal);
        foreach (NeverItemSpec item in NeverItems)
        {
            if (item.Kind is ItemKind kind)
            {
                kinds[item.Prefab] = kind;
            }
        }

        PlanInput input = new PlanInput(baseInput.Containers, baseInput.Assignments, baseInput.InUseBy, baseInput.Catalog, kinds, baseInput.Options, baseInput.Threshold);

        ulong caseSeed = RandomBaseGenerator.Mix(seed, index);
        var random = new FuzzRandom(caseSeed);
        int height = random.Pick(Heights);
        const int width = 8;

        int mode = random.Next(0, 100);
        int occupancy = mode < 5 ? 0 : mode < 15 ? 100 : random.Next(25, 80);
        var stacks = new List<PlayerInventoryStack>();
        var empty = new List<InventorySlot>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var slot = new InventorySlot(x, y);
                if (random.Chance(occupancy))
                {
                    stacks.Add(new PlayerInventoryStack(slot, GenerateStack(random, out InventoryItemTraits traits), traits));
                }
                else
                {
                    empty.Add(slot);
                }
            }
        }

        if (random.Chance(30))
        {
            stacks.Reverse();
        }

        var favourites = new List<InventorySlot>();
        foreach (PlayerInventoryStack stack in stacks)
        {
            if (random.Chance(10))
            {
                favourites.Add(stack.Slot);
            }
        }

        if (empty.Count > 0 && random.Chance(20))
        {
            favourites.Add(random.Pick(empty));
        }

        int variant = random.Next(0, 100) switch
        {
            < 40 => 0,
            < 60 => 1,
            < 90 => 2,
            _ => 3,
        };
        var exclusions = new DepositExclusions(new FavouriteSlots(favourites), NeverListOf(random, variant));
        var player = new PlayerInventorySnapshot(PlayerId, width, height, stacks);
        return new GeneratedDeposit(index, caseSeed, player, exclusions, input, variant);
    }

    /// <summary>Describes a case compactly: the base line, then one line per inventory stack and the exclusions.</summary>
    /// <param name="generated">The case.</param>
    internal static string Describe(GeneratedDeposit generated)
    {
        var text = new StringBuilder();
        text.Append("deposit case ").Append(generated.Index.ToString(CultureInfo.InvariantCulture))
            .Append(" seed=0x").Append(generated.CaseSeed.ToString("X16", CultureInfo.InvariantCulture))
            .Append(" grid=").Append(generated.Player.Width.ToString(CultureInfo.InvariantCulture))
            .Append('x').Append(generated.Player.Height.ToString(CultureInfo.InvariantCulture))
            .Append(" neverList=").Append(string.Join(",", generated.Exclusions.NeverItems))
            .Append(" favourites=");
        foreach (InventorySlot slot in generated.Exclusions.Favourites.Slots)
        {
            text.Append('(').Append(slot.X.ToString(CultureInfo.InvariantCulture)).Append(',').Append(slot.Y.ToString(CultureInfo.InvariantCulture)).Append(')');
        }

        text.Append('\n');
        for (int i = 0; i < generated.Player.Stacks.Count; i++)
        {
            PlayerInventoryStack stack = generated.Player.Stacks[i];
            text.Append("  [").Append(i.ToString(CultureInfo.InvariantCulture)).Append("] (")
                .Append(stack.Slot.X.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stack.Slot.Y.ToString(CultureInfo.InvariantCulture)).Append(") ")
                .Append(PlanAssert.Describe(stack.Stack)).Append(' ').Append(stack.Traits).Append('\n');
        }

        text.Append(RandomBaseGenerator.Describe(RandomBaseGenerator.Generate(generated.Index)));
        return text.ToString();
    }

    private static IReadOnlyList<string> NeverListOf(FuzzRandom random, int variant)
    {
        switch (variant)
        {
            case 0:
                return SettingsCatalog.DepositNeverItemsDefault;
            case 1:
                return Array.Empty<string>();
            case 2:
                var subset = new List<string>();
                foreach (string entry in SettingsCatalog.DepositNeverItemsDefault)
                {
                    if (random.Chance(50))
                    {
                        subset.Add(entry);
                    }
                }

                subset.Add(random.Pick(RandomBaseGenerator.Items).Prefab);
                return subset;
            default:
                return new[] { "@FOOD", "@Mead", "@Ammo", "@BAIT", "hammer", "Hoe", random.Pick(RandomBaseGenerator.Items).Prefab };
        }
    }

    private static ItemStackSnapshot GenerateStack(FuzzRandom random, out InventoryItemTraits traits)
    {
        string prefab;
        int maxStack;
        int quality = 1;
        traits = InventoryItemTraits.None;
        if (random.Chance(30))
        {
            NeverItemSpec never = random.Pick(NeverItems);
            prefab = never.Prefab;
            maxStack = never.MaxStack;
            traits = random.Chance(80) ? never.Traits : InventoryItemTraits.None;
            if (never.MaxStack == 1)
            {
                quality = random.Next(1, 4);
                if (random.Chance(30))
                {
                    traits |= InventoryItemTraits.Equipped;
                }
            }
        }
        else
        {
            RandomBaseGenerator.ItemSpec item = random.Pick(RandomBaseGenerator.Items);
            prefab = item.Prefab;
            if (item.IsGear)
            {
                maxStack = 1;
                quality = random.Next(1, 5);
                if (random.Chance(25))
                {
                    traits |= InventoryItemTraits.Equipped;
                }
            }
            else
            {
                maxStack = random.Pick(RandomBaseGenerator.MaxStacks);
                if (random.Chance(10))
                {
                    quality = random.Next(2, 4);
                }
            }

            if ((item.Prefab == "CookedMeat" || item.Prefab == "QueensJam" || item.Prefab == "SerpentStew") && random.Chance(60))
            {
                traits |= InventoryItemTraits.Food;
            }

            if (item.Prefab == "ArrowWood" && random.Chance(60))
            {
                traits |= InventoryItemTraits.Ammo;
            }
        }

        if (random.Chance(3))
        {
            traits |= random.Pick(new[] { InventoryItemTraits.Food, InventoryItemTraits.Mead, InventoryItemTraits.Ammo, InventoryItemTraits.Bait });
        }

        int count;
        if (random.Chance(3) && maxStack > 1)
        {
            count = maxStack + random.Next(1, 10);
        }
        else if (random.Chance(40))
        {
            count = maxStack;
        }
        else
        {
            count = random.Next(1, maxStack + 1);
        }

        return new ItemStackSnapshot(prefab, count, quality, maxStack);
    }
}
