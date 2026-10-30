namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The S-A-001 mixed-chest fixtures (mixed-chest-category-contracts.md 6.1 to 6.7). Every base is a method, so each call returns
/// fresh snapshot instances. Every expected value in the tests is a literal copied from section 6, never computed by the step.
/// </summary>
internal static class MixedChestFixtures
{
    /// <summary>The steam-id part shared by every fixture container id.</summary>
    internal const long UserId = 3141592653L;

    /// <summary>The threshold of every example in section 6.</summary>
    internal const int Threshold = 50;

    /// <summary>The contract 6.1 catalog text, copied verbatim.</summary>
    internal const string CatalogText =
        "Metals & Ores: CopperOre, Copper, TinOre, Tin\n" +
        "Wood: Wood, FineWood, RoundLog\n" +
        "Stone: Stone, Flint\n" +
        "Cooked Food: CookedMeat, QueensJam\n" +
        "Trophies: TrophyDeer, TrophyBoar\n" +
        "Gear:\n" +
        "Misc:\n";

    /// <summary>One row of a result: the id, the outcome, the assignment and the category name, every property of the entry.</summary>
    internal sealed record ChestRow(uint Id, AutoAssignOutcome Outcome, CategoryAssignment? Assignment, string? CategoryName);

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

    /// <summary>The container id of a fixture chest.</summary>
    internal static ContainerId Id(uint id) => new ContainerId(UserId, id);

    /// <summary>A stack factory. The default count is 10 and the default max stack is 50 (contract 6.1).</summary>
    internal static ItemStackSnapshot S(string prefab, int count = 10, int maxStack = 50) => new ItemStackSnapshot(prefab, count, 1, maxStack);

    /// <summary>A player-built chest with the given slot count (height 1).</summary>
    internal static ContainerSnapshot C(uint id, float x, int slots, params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(Id(id), new WorldPosition(x, 30f, 0f), slots, 1, stacks, ContainerFlags.PlayerBuilt);

    /// <summary>The stack notation of contract 6.1: Wood, Stone, Flint, Copper ore, Cooked meat, Trophy deer (count 1, max 20), Resin, Arrow.</summary>
    internal static ItemStackSnapshot W => S("Wood");

    /// <summary>Stone stack (see <see cref="W"/>).</summary>
    internal static ItemStackSnapshot St => S("Stone");

    /// <summary>Flint stack (see <see cref="W"/>).</summary>
    internal static ItemStackSnapshot Fl => S("Flint");

    /// <summary>Copper ore stack (see <see cref="W"/>).</summary>
    internal static ItemStackSnapshot Cu => S("CopperOre");

    /// <summary>Cooked meat stack (see <see cref="W"/>).</summary>
    internal static ItemStackSnapshot Ck => S("CookedMeat");

    /// <summary>Trophy deer stack, count 1, max 20 (see <see cref="W"/>).</summary>
    internal static ItemStackSnapshot Tr => S("TrophyDeer", 1, 20);

    /// <summary>Resin stack (see <see cref="W"/>).</summary>
    internal static ItemStackSnapshot Rs => S("Resin");

    /// <summary>Arrow stack, an ammo prefab that is not in the catalog (see <see cref="W"/>).</summary>
    internal static ItemStackSnapshot Ar => S("ArrowWood");

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

    /// <summary>The persisted values of base M (6.2): 501 Wood, 506 Keep.</summary>
    internal static Dictionary<ContainerId, CategoryAssignment> PersistedM() =>
        Assign((501, CategoryAssignment.ForCategory("Wood")), (506, CategoryAssignment.Keep));

    /// <summary>Base M of contract 6.2, in snapshot order. A fresh array on every call.</summary>
    internal static ContainerSnapshot[] M() => new[]
    {
        C(501, 0, 2, W, W),
        C(502, 2, 4, Cu, W, St, Ck),
        C(503, 4, 6, W, W, St, Cu, Tr),
        C(504, 6, 6, St, Fl, Cu, Ck, Tr),
        C(505, 8, 4),
        C(506, 10, 6, Ck, Ck, Ck, Ck, Ck, Ck),
    };

    /// <summary>Base M' of contract 6.3: M with 502 and 503 swapped in snapshot order. A fresh array on every call.</summary>
    internal static ContainerSnapshot[] MPrime()
    {
        ContainerSnapshot[] m = M();
        return new[] { m[0], m[2], m[1], m[3], m[4], m[5] };
    }

    /// <summary>Base N of contract 6.4, in snapshot order. A fresh array on every call.</summary>
    internal static ContainerSnapshot[] N() => new[]
    {
        C(601, 0, 10, Cu),
        C(602, 2, 10, St),
        C(603, 4, 10, W, W, Cu, St, Fl),
        C(604, 6, 4, W, St, Cu),
    };

    /// <summary>The persisted values of base N (6.4): 601 Metals & Ores, 602 Stone.</summary>
    internal static Dictionary<ContainerId, CategoryAssignment> PersistedN() =>
        Assign((601, CategoryAssignment.ForCategory("Metals & Ores")), (602, CategoryAssignment.ForCategory("Stone")));

    /// <summary>Base P of contract 6.5, in snapshot order. A fresh array on every call.</summary>
    internal static ContainerSnapshot[] P() => new[]
    {
        C(701, 0, 2, Ck, Ck),
        C(702, 2, 4, St, W, Cu),
    };

    /// <summary>The persisted values of base P (6.5): 701 Wood.</summary>
    internal static Dictionary<ContainerId, CategoryAssignment> PersistedP() =>
        Assign((701, CategoryAssignment.ForCategory("Wood")));

    /// <summary>Base R of contract 6.6, in snapshot order. A fresh array on every call.</summary>
    internal static ContainerSnapshot[] R() => new[]
    {
        C(900, 0, 10, W, W, W, W, W, W, W, W, W, W),
        C(901, 2, 4, W, St, Cu, Ck),
        C(902, 4, 4, W, St, Cu, Tr),
    };

    /// <summary>The persisted values of base R (6.6): 900 Stone.</summary>
    internal static Dictionary<ContainerId, CategoryAssignment> PersistedR() =>
        Assign((900, CategoryAssignment.ForCategory("Stone")));

    /// <summary>Base Q of contract 6.7, in snapshot order, with no persisted values. A fresh array on every call.</summary>
    internal static ContainerSnapshot[] Q() => new[]
    {
        C(801, 0, 4, Ar, Rs, W),
        C(802, 2, 4, Ar, Rs, St),
        C(803, 4, 4),
    };

    /// <summary>The threshold step over chests, with the 6.1 catalog and kinds and threshold 50.</summary>
    internal static AutoAssignResult Resolve(IReadOnlyList<ContainerSnapshot> chests, Dictionary<ContainerId, CategoryAssignment> persisted, int threshold = Threshold) =>
        AutoAssigner.Resolve(chests, persisted, Catalog(), Kinds(), threshold);

    /// <summary>The step under test, applied to the threshold step of the same chests (contract 6.1 <c>Mixed</c>).</summary>
    internal static AutoAssignResult Mixed(IReadOnlyList<ContainerSnapshot> chests, Dictionary<ContainerId, CategoryAssignment> persisted)
    {
        CategoryCatalog catalog = Catalog();
        IReadOnlyDictionary<string, ItemKind> kinds = Kinds();
        return AutoAssigner.AssignMixedChests(AutoAssigner.Resolve(chests, persisted, catalog, kinds, Threshold), catalog, kinds);
    }

    /// <summary>The rows of a result, in its chest order.</summary>
    internal static ChestRow[] RowsOf(AutoAssignResult result) =>
        result.Chests.Select(c => new ChestRow(c.Id.Id, c.Outcome, c.Assignment, c.CategoryName)).ToArray();

    /// <summary>An expected row of a chest given a category by the step or the threshold.</summary>
    internal static ChestRow Given(uint id, AutoAssignOutcome outcome, string category) =>
        new ChestRow(id, outcome, CategoryAssignment.ForCategory(category), category);

    /// <summary>An expected row of a chest with no category.</summary>
    internal static ChestRow Unset(uint id, AutoAssignOutcome outcome) => new ChestRow(id, outcome, null, null);

    /// <summary>Asserts that two results hold the same threshold, the same entries in order and the same overflow chests in order.</summary>
    internal static void AssertSameResult(AutoAssignResult expected, AutoAssignResult actual)
    {
        Assert.Equal(expected.AutoAssignThresholdPct, actual.AutoAssignThresholdPct);
        Assert.Equal(RowsOf(expected), RowsOf(actual));
        Assert.Equal(expected.OverflowChests.Select(c => c.Id.Id), actual.OverflowChests.Select(c => c.Id.Id));
    }
}
