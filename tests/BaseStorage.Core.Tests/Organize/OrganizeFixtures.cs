namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The S-E004-006 plan fixtures (ADR-GWIN-039, contract 6.1 to 6.7 and 6.9). The catalog, kinds, options, factories and bases are
/// copied from the contract as literals. Every expected value in the tests is a literal too, never computed by the planner.
/// </summary>
internal static class OrganizeFixtures
{
    /// <summary>The steam-id part shared by every fixture container id.</summary>
    internal const long UserId = 3141592653L;

    /// <summary>The threshold of most fixtures (contract 6.1 and 6.2).</summary>
    internal const int DefaultThreshold = 50;

    /// <summary>The contract 6.1 catalog text, copied verbatim.</summary>
    internal const string CatalogText =
        "Metals & Ores: CopperOre, Copper, TinOre, Tin\n" +
        "Wood: Wood, FineWood, RoundLog\n" +
        "Stone: Stone, Flint\n" +
        "Cooked Food: CookedMeat, QueensJam\n" +
        "Trophies: TrophyDeer, TrophyBoar\n" +
        "Gear:\n" +
        "Misc:\n";

    /// <summary>The base names that the invariant rows iterate over.</summary>
    internal static readonly string[] AllFixtureNames =
    {
        "E", "Merge", "Overflow", "OverflowNo405", "Solo702", "Pair702", "Iron", "Chained", "ChainedNoB", "SkipCombined", "Perf",
    };

    /// <summary>The arguments of one <c>OrganizePlanner.Plan</c> call, in contract order.</summary>
    internal sealed record PlanInput(
        ContainerSnapshot[] Containers,
        Dictionary<ContainerId, CategoryAssignment> Assignments,
        Dictionary<ContainerId, string> InUseBy,
        CategoryCatalog Catalog,
        Dictionary<string, ItemKind> Kinds,
        EligibilityOptions Options,
        int Threshold);

    /// <summary>The contract 6.1 catalog, parsed from its text.</summary>
    internal static CategoryCatalog Catalog() => CategoryFile.Parse(CatalogText).Catalog;

    /// <summary>The contract 6.1 item kinds.</summary>
    internal static Dictionary<string, ItemKind> Kinds() => new(StringComparer.Ordinal)
    {
        ["SwordIron"] = ItemKind.Weapon,
        ["AxeBronze"] = ItemKind.Tool,
        ["ArrowWood"] = ItemKind.Ammo,
        ["SerpentStew"] = ItemKind.Food,
        ["Resin"] = ItemKind.Other,
    };

    /// <summary>The contract 6.1 eligibility options: vehicles excluded, Keep chests not craftable.</summary>
    internal static EligibilityOptions Options() => new EligibilityOptions(excludeVehicles: true, keepChestsCraftable: false);

    /// <summary>The container id of a fixture chest.</summary>
    internal static ContainerId Id(uint id) => new ContainerId(UserId, id);

    /// <summary>A stack factory. The default max stack is 50 and the default quality is 1 (contract 6.1).</summary>
    internal static ItemStackSnapshot S(string prefab, int count, int maxStack = 50, int quality = 1) =>
        new ItemStackSnapshot(prefab, count, quality, maxStack);

    /// <summary>A player-built chest with the given slot count (height 1).</summary>
    internal static ContainerSnapshot C(uint id, float x, float z, int slots, params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(Id(id), new WorldPosition(x, 30f, z), slots, 1, stacks, ContainerFlags.PlayerBuilt);

    /// <summary>A chest with explicit flags (height 1).</summary>
    internal static ContainerSnapshot CF(uint id, float x, float z, int slots, ContainerFlags flags, params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(Id(id), new WorldPosition(x, 30f, z), slots, 1, stacks, flags);

    /// <summary>Builds a persisted-assignment dictionary from (chest, assignment) pairs.</summary>
    internal static Dictionary<ContainerId, CategoryAssignment> Assign(params (uint Chest, CategoryAssignment Value)[] pairs)
    {
        var map = new Dictionary<ContainerId, CategoryAssignment>();
        foreach ((uint chest, CategoryAssignment value) in pairs)
        {
            map[Id(chest)] = value;
        }

        return map;
    }

    /// <summary>Builds an in-use dictionary from (chest, player name) pairs.</summary>
    internal static Dictionary<ContainerId, string> InUse(params (uint Chest, string Name)[] pairs)
    {
        var map = new Dictionary<ContainerId, string>();
        foreach ((uint chest, string name) in pairs)
        {
            map[Id(chest)] = name;
        }

        return map;
    }

    /// <summary>Builds one plan input with the 6.1 catalog and kinds, unless overridden.</summary>
    internal static PlanInput Make(
        ContainerSnapshot[] containers,
        Dictionary<ContainerId, CategoryAssignment>? assignments = null,
        Dictionary<ContainerId, string>? inUseBy = null,
        int threshold = DefaultThreshold,
        EligibilityOptions? options = null,
        CategoryCatalog? catalog = null) =>
        new PlanInput(
            containers,
            assignments ?? new Dictionary<ContainerId, CategoryAssignment>(),
            inUseBy ?? new Dictionary<ContainerId, string>(),
            catalog ?? Catalog(),
            Kinds(),
            options ?? Options(),
            threshold);

    /// <summary>
    /// Calls <c>OrganizePlanner.Plan</c> with the arguments of one input. S-A-001: <paramref name="autoAssignMixedChests"/> true calls the
    /// 8-parameter overload with the mixed-chest step; false (the default) calls the 7-parameter overload, which is v0.1, so every existing
    /// caller keeps its behaviour.
    /// </summary>
    /// <param name="input">The planner arguments.</param>
    /// <param name="autoAssignMixedChests">The AutoAssignMixedChests value of the call; false by default.</param>
    internal static OrganizePlan Run(PlanInput input, bool autoAssignMixedChests = false) =>
        autoAssignMixedChests
            ? OrganizePlanner.Plan(input.Containers, input.Assignments, input.InUseBy, input.Catalog, input.Kinds, input.Options, input.Threshold, true)
            : OrganizePlanner.Plan(input.Containers, input.Assignments, input.InUseBy, input.Catalog, input.Kinds, input.Options, input.Threshold);

    /// <summary>The 6.2 chests of base E, in snapshot order. A fresh array on every call.</summary>
    internal static ContainerSnapshot[] EChests() => new[]
    {
        C(201, 0, 0, 2, S("Wood", 30), S("Stone", 10)),
        C(202, 2, 0, 4, S("Stone", 20), S("Flint", 5), S("Stone", 25)),
        C(203, 4, 0, 3, S("Wood", 20), S("Stone", 40), S("Flint", 7)),
        C(204, 6, 0, 2),
        C(205, 8, 0, 2, S("Wood", 10)),
        CF(206, 10, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Stone", 5)),
        CF(207, 12, 0, 2, ContainerFlags.Tombstone, S("Wood", 3)),
        C(208, -4, 0, 1),
        C(209, 14, 0, 5, S("Wood", 40), S("Wood", 45), S("Resin", 12), S("CookedMeat", 5), S("TrophyDeer", 1, 20)),
    };

    /// <summary>The base E of contract 6.2 with its assignments and in-use names. Threshold 50.</summary>
    internal static PlanInput E() => Make(
        EChests(),
        Assign(
            (201, CategoryAssignment.ForCategory("Wood")),
            (202, CategoryAssignment.ForCategory("Stone")),
            (205, CategoryAssignment.Keep),
            (206, CategoryAssignment.ForCategory("Wood"))),
        InUse((206, "Erik")));

    /// <summary>The 6.3 base (merge first, partial no room). Threshold 50 and no in-use names.</summary>
    internal static PlanInput Merge() => Make(
        new[]
        {
            C(301, 0, 0, 2, S("Stone", 50)),
            C(302, 1, 0, 2, S("Stone", 45)),
            C(303, 2, 0, 3, S("Stone", 12), S("Wood", 3), S("CopperOre", 1)),
            C(304, 3, 0, 1, S("Wood", 48)),
        },
        Assign((301, CategoryAssignment.ForCategory("Stone")), (302, CategoryAssignment.ForCategory("Stone")), (304, CategoryAssignment.ForCategory("Wood"))));

    /// <summary>The 6.4 overflow base, threshold 60. Pass false for the variant without chest 405.</summary>
    internal static PlanInput Overflow(bool includeChest405 = true)
    {
        var chests = new List<ContainerSnapshot>
        {
            C(401, 0, 0, 1, S("Wood", 50)),
            C(402, 10, 0, 4, S("Wood", 50), S("Wood", 50), S("Stone", 1), S("Flint", 1)),
            C(403, 5, 0, 1),
            C(404, 2, 0, 1),
        };
        if (includeChest405)
        {
            chests.Add(C(405, 20, 0, 1));
        }

        return Make(chests.ToArray(), Assign((401, CategoryAssignment.ForCategory("Wood"))), threshold: 60);
    }

    /// <summary>The 6.7 base with one unassigned chest holding SwordIron, Resin and Wood, and no other chest.</summary>
    internal static PlanInput Solo702() => Make(new[]
    {
        C(702, 0, 0, 3, S("SwordIron", 1, 1), S("Resin", 3), S("Wood", 2)),
    });

    /// <summary>The 6.7 base 702 plus an empty unassigned chest 703 that can be claimed for Gear.</summary>
    internal static PlanInput Pair702() => Make(new[]
    {
        C(702, 0, 0, 3, S("SwordIron", 1, 1), S("Resin", 3), S("Wood", 2)),
        C(703, 5, 0, 2),
    });

    /// <summary>A Wood-persisted chest and a chest persisted as the unknown category Iron, which must receive nothing.</summary>
    internal static PlanInput Iron() => Make(
        new[]
        {
            C(801, 0, 0, 2, S("Wood", 5)),
            C(802, 2, 0, 2, S("Wood", 10)),
        },
        Assign((801, CategoryAssignment.ForCategory("Iron")), (802, CategoryAssignment.ForCategory("Wood"))));

    /// <summary>The GW-U-550 chain: one Wood chest, a Stone source with three misplaced Wood stacks, and two empty pool chests.</summary>
    internal static PlanInput Chained(bool includeChestB = true)
    {
        var chests = new List<ContainerSnapshot>
        {
            C(1401, 0, 0, 1, S("Wood", 50)),
            C(1402, 10, 0, 4, S("Wood", 50), S("Wood", 50), S("Wood", 50), S("Stone", 1)),
            C(1403, 1, 0, 2),
        };
        if (includeChestB)
        {
            chests.Add(C(1404, 2, 0, 1));
        }

        return Make(
            chests.ToArray(),
            Assign((1401, CategoryAssignment.ForCategory("Wood")), (1402, CategoryAssignment.ForCategory("Stone"))));
    }

    /// <summary>The 6.5 skip rows: a base with one chest, named by case. The name selects the chest, its assignment, in-use names and options.</summary>
    internal static PlanInput SkipCase(string name) => name switch
    {
        "501-keep" => Make(new[] { C(501, 0, 0, 2, S("Wood", 10)) }, Assign((501, CategoryAssignment.Keep))),
        "502-keep-empty" => Make(new[] { C(502, 0, 0, 2) }, Assign((502, CategoryAssignment.Keep))),
        "501-keep-craftable" => Make(
            new[] { C(501, 0, 0, 2, S("Wood", 10)) },
            Assign((501, CategoryAssignment.Keep)),
            options: new EligibilityOptions(excludeVehicles: true, keepChestsCraftable: true)),
        "513-keep-in-use" => Make(
            new[] { CF(513, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 3)) },
            Assign((513, CategoryAssignment.Keep)),
            InUse((513, "Erik"))),
        "503-named" => Make(
            new[] { CF(503, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Stone", 5)) },
            inUseBy: InUse((503, "Erik"))),
        "504-flag-only" => Make(new[] { CF(504, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Stone", 5)) }),
        "505-dictionary-only" => Make(new[] { C(505, 0, 0, 2, S("Stone", 5)) }, inUseBy: InUse((505, "Astrid"))),
        "506-blank-name" => Make(
            new[] { CF(506, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Stone", 5)) },
            inUseBy: InUse((506, "   "))),
        "507-empty" => Make(new[] { CF(507, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther) }),
        "508-tombstone" => Make(new[] { CF(508, 0, 0, 2, ContainerFlags.Tombstone, S("Wood", 3)) }),
        "509-vehicle" => Make(new[] { CF(509, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle, S("Wood", 3)) }),
        "509-vehicle-allowed" => Make(
            new[] { CF(509, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle, S("Wood", 3)) },
            options: new EligibilityOptions(excludeVehicles: false, keepChestsCraftable: false)),
        "510-not-player-built" => Make(new[] { CF(510, 0, 0, 2, ContainerFlags.None, S("Wood", 3)) }),
        "511-access-denied" => Make(new[] { CF(511, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied, S("Wood", 3)) }),
        "512-keep-tombstone" => Make(new[] { CF(512, 0, 0, 2, ContainerFlags.Tombstone, S("Wood", 3)) }, Assign((512, CategoryAssignment.Keep))),
        "514-tombstone-in-use" => Make(
            new[] { CF(514, 0, 0, 2, ContainerFlags.Tombstone | ContainerFlags.InUseByOther, S("Wood", 3)) },
            inUseBy: InUse((514, "Erik"))),
        "515-too-many-stacks" => Make(new[]
        {
            new ContainerSnapshot(Id(515), new WorldPosition(0f, 30f, 0f), 1, 1, new[] { S("Wood", 1), S("Stone", 1) }, ContainerFlags.PlayerBuilt),
        }),
        "516-above-max" => Make(new[] { C(516, 0, 0, 2, S("Wood", 60)) }, Assign((516, CategoryAssignment.ForCategory("Wood")))),
        "precedence-both-problems" => Make(new[]
        {
            new ContainerSnapshot(Id(1201), new WorldPosition(0f, 30f, 0f), 1, 1, new[] { S("Wood", 60), S("Stone", 1) }, ContainerFlags.PlayerBuilt),
        }),
        "combined" => SkipCombined(),
        _ => throw new ArgumentException("Unknown skip case: " + name, nameof(name)),
    };

    /// <summary>The AC-10 base: the 16 distinct chests 501 to 516 with their assignments and in-use names merged. Options from 6.1.</summary>
    internal static PlanInput SkipCombined() => Make(
        new[]
        {
            C(501, 0, 0, 2, S("Wood", 10)),
            C(502, 0, 0, 2),
            CF(503, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Stone", 5)),
            CF(504, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Stone", 5)),
            C(505, 0, 0, 2, S("Stone", 5)),
            CF(506, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Stone", 5)),
            CF(507, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther),
            CF(508, 0, 0, 2, ContainerFlags.Tombstone, S("Wood", 3)),
            CF(509, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle, S("Wood", 3)),
            CF(510, 0, 0, 2, ContainerFlags.None, S("Wood", 3)),
            CF(511, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied, S("Wood", 3)),
            CF(512, 0, 0, 2, ContainerFlags.Tombstone, S("Wood", 3)),
            CF(513, 0, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 3)),
            CF(514, 0, 0, 2, ContainerFlags.Tombstone | ContainerFlags.InUseByOther, S("Wood", 3)),
            new ContainerSnapshot(Id(515), new WorldPosition(0f, 30f, 0f), 1, 1, new[] { S("Wood", 1), S("Stone", 1) }, ContainerFlags.PlayerBuilt),
            C(516, 0, 0, 2, S("Wood", 60)),
        },
        Assign(
            (501, CategoryAssignment.Keep),
            (502, CategoryAssignment.Keep),
            (512, CategoryAssignment.Keep),
            (513, CategoryAssignment.Keep),
            (516, CategoryAssignment.ForCategory("Wood"))),
        InUse((503, "Erik"), (505, "Astrid"), (506, "   "), (513, "Erik"), (514, "Erik")));

    /// <summary>The 6.6 compaction rows: one category chest per row, named by its chest id.</summary>
    internal static PlanInput Compaction(string name) => name switch
    {
        "601" => Make(
            new[] { C(601, 0, 0, 4, S("Stone", 20), S("Flint", 3), S("Stone", 40), S("Flint", 4)) },
            Assign((601, CategoryAssignment.ForCategory("Stone")))),
        "602" => Make(
            new[] { C(602, 0, 0, 3, S("Wood", 10), S("RoundLog", 5), S("FineWood", 5)) },
            Assign((602, CategoryAssignment.ForCategory("Wood")))),
        "603" => Make(
            new[] { C(603, 0, 0, 3, S("SwordIron", 1, 1, 2), S("AxeBronze", 1, 1), S("SwordIron", 1, 1)) },
            Assign((603, CategoryAssignment.ForCategory("Gear")))),
        "604" => Make(
            new[] { C(604, 0, 0, 3, S("Stone", 20, 50), S("Stone", 10, 30), S("Stone", 15, 50)) },
            Assign((604, CategoryAssignment.ForCategory("Stone")))),
        "605" => Make(
            new[] { C(605, 0, 0, 3, S("Wood", 50), S("Wood", 50), S("Wood", 50)) },
            Assign((605, CategoryAssignment.ForCategory("Wood")))),
        _ => throw new ArgumentException("Unknown compaction case: " + name, nameof(name)),
    };

    /// <summary>The GW-U-563 AC-11 derived base: a Wood chest with two 2000000000 stacks, and a Stone source with one Wood1 stack.</summary>
    internal static PlanInput Huge() => Make(
        new[]
        {
            C(1101, 0, 0, 3, S("Wood", 2000000000, int.MaxValue), S("Wood", 2000000000, int.MaxValue)),
            C(1102, 2, 0, 1, S("Wood", 1, int.MaxValue)),
        },
        Assign((1101, CategoryAssignment.ForCategory("Wood")), (1102, CategoryAssignment.ForCategory("Stone"))));

    /// <summary>The 6.9 performance base: 60 chests of 32 slots, built from the normative shape.</summary>
    internal static PlanInput PerfBase()
    {
        string[] ownPrefab = { "Wood", "Stone", "CopperOre", "CookedMeat" };
        string[] ownCategory = { "Wood", "Stone", "Metals & Ores", "Cooked Food" };
        string[] cycle = { "CopperOre", "Wood", "Stone", "CookedMeat", "Resin", "SwordIron" };
        var chests = new ContainerSnapshot[60];
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        for (int k = 0; k < 60; k++)
        {
            var stacks = new List<ItemStackSnapshot>(32);
            if (k % 3 == 0)
            {
                int j = (k / 3) % 4;
                for (int i = 0; i < 16; i++)
                {
                    stacks.Add(S(i < 8 ? ownPrefab[j] : ownPrefab[(j + 1) % 4], 25));
                }

                assignments[Id((uint)(1000 + k))] = CategoryAssignment.ForCategory(ownCategory[j]);
            }
            else if (k % 3 == 1)
            {
                for (int i = 0; i < 32; i++)
                {
                    string prefab = cycle[i % 6];
                    stacks.Add(prefab == "SwordIron" ? S(prefab, 1, 1) : S(prefab, 1 + ((k + i) % 49)));
                }
            }

            chests[k] = new ContainerSnapshot(
                Id((uint)(1000 + k)),
                new WorldPosition((k % 10) * 2f, 30f, (k / 10) * 2f),
                8,
                4,
                stacks,
                ContainerFlags.PlayerBuilt);
        }

        return Make(chests, assignments);
    }

    /// <summary>Looks up a named base from <see cref="AllFixtureNames"/>.</summary>
    internal static PlanInput Named(string name) => name switch
    {
        "E" => E(),
        "Merge" => Merge(),
        "Overflow" => Overflow(),
        "OverflowNo405" => Overflow(includeChest405: false),
        "Solo702" => Solo702(),
        "Pair702" => Pair702(),
        "Iron" => Iron(),
        "Chained" => Chained(),
        "ChainedNoB" => Chained(includeChestB: false),
        "SkipCombined" => SkipCombined(),
        "Perf" => PerfBase(),
        _ => throw new ArgumentException("Unknown fixture: " + name, nameof(name)),
    };
}
