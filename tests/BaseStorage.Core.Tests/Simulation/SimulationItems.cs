namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// The fixed item table, catalog, kinds and settings of the S-E005-004 multi-peer simulation (AC-5.5). Every prefab has one max
/// stack, as in the game, so the shared world can merge by prefab and quality exactly like the planners' item identity. Not a test row.
/// </summary>
internal static class SimulationItems
{
    /// <summary>The reservation timeout the peers run with, in milliseconds (the spec default).</summary>
    internal const int ReserveTimeoutMs = 1500;

    /// <summary>The organize lock duration the peers run with, in seconds (the spec default).</summary>
    internal const int OrganizeLockSeconds = 30;

    /// <summary>The milliseconds one simulated frame advances the shared clock (20 frames per second).</summary>
    internal const int TickMs = 50;

    /// <summary>The item table: prefab, max stack, and whether the item is gear (max stack 1, quality 1..3).</summary>
    internal static readonly ItemSpec[] Items =
    {
        new ItemSpec("Wood", 50, false),
        new ItemSpec("FineWood", 50, false),
        new ItemSpec("Stone", 50, false),
        new ItemSpec("Flint", 50, false),
        new ItemSpec("CopperOre", 30, false),
        new ItemSpec("Copper", 30, false),
        new ItemSpec("TinOre", 30, false),
        new ItemSpec("Resin", 50, false),
        new ItemSpec("CookedMeat", 20, false),
        new ItemSpec("TrophyDeer", 20, false),
        new ItemSpec("ArrowWood", 100, false),
        new ItemSpec("SwordIron", 1, true),
    };

    /// <summary>The prefabs a simulated recipe may require: stackable materials only, so a requirement never asks for gear.</summary>
    internal static readonly string[] CraftablePrefabs = { "Wood", "FineWood", "Stone", "Flint", "CopperOre", "Copper", "TinOre", "Resin" };

    /// <summary>The names the peers appear under, by peer index.</summary>
    internal static readonly string[] PeerNames = { "Erik", "Astrid", "Bjorn" };

    /// <summary>The catalog text the peers organize by (the S-E004-006 contract 6.1 catalog plus Misc and Ammo targets).</summary>
    internal const string CatalogText =
        "Metals & Ores: CopperOre, Copper, TinOre, Tin\n" +
        "Wood: Wood, FineWood, RoundLog\n" +
        "Stone: Stone, Flint\n" +
        "Cooked Food: CookedMeat, QueensJam\n" +
        "Trophies: TrophyDeer, TrophyBoar\n" +
        "Gear:\n" +
        "Ammo:\n" +
        "Misc:\n";

    /// <summary>One prefab of the item table.</summary>
    /// <param name="Prefab">The prefab name.</param>
    /// <param name="MaxStack">The item's max stack.</param>
    /// <param name="IsGear">True for gear: max stack 1 and a quality of 1..3.</param>
    internal sealed record ItemSpec(string Prefab, int MaxStack, bool IsGear);

    /// <summary>The parsed catalog.</summary>
    internal static CategoryCatalog Catalog() => CategoryFile.Parse(CatalogText).Catalog;

    /// <summary>The item kinds: gear, ammo, food and a plain Other.</summary>
    internal static Dictionary<string, ItemKind> Kinds() => new(StringComparer.Ordinal)
    {
        ["SwordIron"] = ItemKind.Weapon,
        ["ArrowWood"] = ItemKind.Ammo,
        ["CookedMeat"] = ItemKind.Food,
        ["TrophyDeer"] = ItemKind.Trophy,
        ["Resin"] = ItemKind.Other,
    };

    /// <summary>The host-synced settings every peer runs with.</summary>
    /// <param name="movesPerFrame">The MovesPerFrame value.</param>
    internal static BaseStorageSettings Settings(int movesPerFrame) => new BaseStorageSettings(
        new FakeSettingsSource()
            .WithInt(SettingKeys.MovesPerFrame, movesPerFrame)
            .WithInt(SettingKeys.ReserveTimeoutMs, ReserveTimeoutMs)
            .WithInt(SettingKeys.OrganizeLockSeconds, OrganizeLockSeconds)
            .WithInt(SettingKeys.AutoAssignThresholdPct, 50)
            .WithBool(SettingKeys.ExcludeVehicles, true)
            .WithBool(SettingKeys.KeepChestsCraftable, false));

    /// <summary>Finds the item spec of a prefab.</summary>
    /// <param name="prefab">The prefab name.</param>
    internal static ItemSpec SpecOf(string prefab)
    {
        foreach (ItemSpec item in Items)
        {
            if (string.Equals(item.Prefab, prefab, StringComparison.Ordinal))
            {
                return item;
            }
        }

        throw new ArgumentException("Unknown prefab: " + prefab, nameof(prefab));
    }

    /// <summary>A stack of a table item at its max stack.</summary>
    /// <param name="prefab">The prefab name.</param>
    /// <param name="count">The count, 1..max stack.</param>
    /// <param name="quality">The quality.</param>
    internal static ItemStackSnapshot Stack(string prefab, int count, int quality = 1) =>
        new ItemStackSnapshot(prefab, count, quality, SpecOf(prefab).MaxStack);

    /// <summary>The conservation key of a stack: prefab and quality, the identity the planners and the world share.</summary>
    /// <param name="stack">The stack.</param>
    internal static string KeyOf(ItemStackSnapshot stack) => stack.PrefabName + " q" + stack.Quality;

    /// <summary>Per-key totals of some stacks.</summary>
    /// <param name="stacks">The stacks.</param>
    internal static SortedDictionary<string, long> Totals(IEnumerable<ItemStackSnapshot> stacks)
    {
        var totals = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (ItemStackSnapshot stack in stacks)
        {
            string key = KeyOf(stack);
            totals.TryGetValue(key, out long current);
            totals[key] = current + stack.Count;
        }

        return totals;
    }
}
