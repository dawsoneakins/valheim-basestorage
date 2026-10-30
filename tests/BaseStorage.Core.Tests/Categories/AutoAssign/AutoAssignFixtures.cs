namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Contract 5.1 to 5.6 fixtures for S-E004-005 (ADR-GWIN-037). The catalog, base B, assignments and set T are copied from the
/// contract as literals. Expected values in the tests are literals too, never read from the code under test.
/// </summary>
internal static class AutoAssignFixtures
{
    /// <summary>The steam-id part shared by every fixture container id.</summary>
    internal const long UserId = 3141592653L;

    /// <summary>The contract 5.1 catalog text, copied verbatim.</summary>
    internal const string CatalogText =
        "Metals & Ores: CopperOre, Copper, TinOre, Tin\n" +
        "Wood: Wood, FineWood, RoundLog\n" +
        "Stone: Stone, Flint\n" +
        "Cooked Food: CookedMeat, QueensJam\n" +
        "Trophies: TrophyDeer, TrophyBoar\n" +
        "Gear:\n" +
        "Misc:\n";

    /// <summary>The contract 5.2 table at threshold 50, one row per chest of B in snapshot order (chest 106 is Keep).</summary>
    internal static readonly (uint Id, AutoAssignOutcome Outcome, string? Name)[] BaseTable50 =
    {
        (101, AutoAssignOutcome.AlreadyAssigned, "Wood"),
        (102, AutoAssignOutcome.Assigned, "Metals & Ores"),
        (103, AutoAssignOutcome.SourceOnly, null),
        (104, AutoAssignOutcome.Assigned, "Wood"),
        (105, AutoAssignOutcome.Overflow, null),
        (106, AutoAssignOutcome.Keep, null),
        (107, AutoAssignOutcome.AlreadyAssigned, "Wood"),
        (108, AutoAssignOutcome.Overflow, null),
        (109, AutoAssignOutcome.Overflow, null),
        (110, AutoAssignOutcome.Assigned, "Gear"),
        (111, AutoAssignOutcome.Assigned, "Wood"),
    };

    /// <summary>Builds the 5.1 catalog, parsed from its text.</summary>
    internal static CategoryCatalog Catalog() => CategoryFile.Parse(CatalogText).Catalog;

    /// <summary>Builds the 5.1 item kinds with ordinal comparison. A fresh dictionary per call.</summary>
    internal static Dictionary<string, ItemKind> Kinds() => new Dictionary<string, ItemKind>(StringComparer.Ordinal)
    {
        ["SwordIron"] = ItemKind.Weapon,
        ["AxeBronze"] = ItemKind.Tool,
        ["ArrowWood"] = ItemKind.Ammo,
        ["SerpentStew"] = ItemKind.Food,
        ["Resin"] = ItemKind.Other,
    };

    /// <summary>The 5.1 item kinds with one prefab set to another kind.</summary>
    internal static Dictionary<string, ItemKind> KindsWith(string prefab, ItemKind kind)
    {
        Dictionary<string, ItemKind> kinds = Kinds();
        kinds[prefab] = kind;
        return kinds;
    }

    /// <summary>Maps the short codes of the contract tables to prefab names. Any other text is returned unchanged.</summary>
    internal static string Prefab(string code) => code switch
    {
        "M" => "CopperOre",
        "W" => "Wood",
        "St" => "Stone",
        "F" => "CookedMeat",
        "T" => "TrophyDeer",
        _ => code,
    };

    /// <summary>Splits a comma list of codes into one stack per code, each of one item.</summary>
    internal static ItemStackSnapshot[] Stacks(string codes) =>
        codes.Split(',').Select(code => S(Prefab(code.Trim()), 1)).ToArray();

    /// <summary>A stack of the given prefab. Quality 1 and max stack 50.</summary>
    internal static ItemStackSnapshot S(string prefab, int count) => new ItemStackSnapshot(prefab, count, 1, 50);

    /// <summary>The fixture container id for a serial number.</summary>
    internal static ContainerId Id(uint id) => new ContainerId(UserId, id);

    /// <summary>A 5 x 2 player-built chest at height 30.</summary>
    internal static ContainerSnapshot C(uint id, float x, float z, params ItemStackSnapshot[] stacks) =>
        CY(id, x, 30f, z, stacks);

    /// <summary>A 5 x 2 player-built chest at an explicit height.</summary>
    internal static ContainerSnapshot CY(uint id, float x, float y, float z, params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(Id(id), new WorldPosition(x, y, z), 5, 2, stacks, ContainerFlags.PlayerBuilt);

    /// <summary>A 8 x 4 player-built chest at height 30, for the large-base fixture.</summary>
    internal static ContainerSnapshot Big(uint id, float x, float z, params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(Id(id), new WorldPosition(x, 30f, z), 8, 4, stacks, ContainerFlags.PlayerBuilt);

    /// <summary>A 5 x 2 chest with the given flags and a position at the id, for the flag-independence rows.</summary>
    internal static ContainerSnapshot Flagged(uint id, ContainerFlags flags, params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(Id(id), new WorldPosition(id, 30f, 0f), 5, 2, stacks, flags);

    /// <summary>Contract 5.1 base B: 11 chests in snapshot order. A fresh array per call.</summary>
    internal static ContainerSnapshot[] B() => new[]
    {
        C(101, 0, 0, S("Wood", 50), S("Wood", 50)),
        C(102, 2, 0, S("CopperOre", 30), S("CopperOre", 30), S("Tin", 10), S("Wood", 20)),
        C(103, 4, 0, S("CopperOre", 5), S("Wood", 5), S("Stone", 5), S("CookedMeat", 5)),
        C(104, 6, 0, S("Stone", 10), S("Flint", 10), S("Wood", 10), S("FineWood", 10)),
        C(105, 8, 0),
        C(106, 10, 0),
        C(107, 20, 0),
        C(108, 1, 5),
        C(109, 30, 0),
        C(110, -3, 0, S("SwordIron", 1), S("AxeBronze", 1), S("ArrowWood", 20)),
        C(111, 12, 0, S("Wood", 1), S("Wood", 1), S("Wood", 1), S("Stone", 50)),
    };

    /// <summary>Contract 5.1 persisted assignments of B. A fresh dictionary per call. Id 999 is not in B.</summary>
    internal static Dictionary<ContainerId, CategoryAssignment> Assignments() => new Dictionary<ContainerId, CategoryAssignment>
    {
        [Id(101)] = CategoryAssignment.ForCategory("Wood"),
        [Id(106)] = CategoryAssignment.Keep,
        [Id(107)] = CategoryAssignment.ForCategory("Wood"),
        [Id(999)] = CategoryAssignment.ForCategory("Stone"),
    };

    /// <summary>Contract 5.6 set T: an assigned Wood chest 301 and three empty chests, in snapshot order 301, 303, 302, 304.</summary>
    internal static ContainerSnapshot[] T() => new[]
    {
        C(301, 0, 0, S("Wood", 50)),
        C(303, 3, 0),
        C(302, -3, 0),
        C(304, 0, 4),
    };

    /// <summary>Contract 5.6 persisted assignments: chest 301 is Wood.</summary>
    internal static Dictionary<ContainerId, CategoryAssignment> TAssignments() => new Dictionary<ContainerId, CategoryAssignment>
    {
        [Id(301)] = CategoryAssignment.ForCategory("Wood"),
    };

    /// <summary>Resolves containers with the 5.1 catalog and kinds unless other values are given.</summary>
    internal static AutoAssignResult ResolveWith(
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment>? assignments = null,
        CategoryCatalog? catalog = null,
        IReadOnlyDictionary<string, ItemKind>? kinds = null,
        int threshold = 50) =>
        AutoAssigner.Resolve(
            containers,
            assignments ?? new Dictionary<ContainerId, CategoryAssignment>(),
            catalog ?? Catalog(),
            kinds ?? Kinds(),
            threshold);

    /// <summary>Resolves a fresh B with the given threshold, using a fresh <see cref="Assignments"/> set.</summary>
    internal static AutoAssignResult ResolveB(int threshold = 50) => ResolveWith(B(), Assignments(), threshold: threshold);

    /// <summary>Resolves one chest alone with empty assignments and the 5.1 catalog and kinds.</summary>
    internal static AutoAssignResult ResolveOne(ContainerSnapshot chest, int threshold = 50) =>
        ResolveWith(new[] { chest }, threshold: threshold);

    /// <summary>Resolves the single chest 201 whose stacks are the codes of a comma list (see <see cref="Stacks"/>).</summary>
    internal static AutoAssignResult ResolveCodes(int threshold, string codes) =>
        ResolveOne(C(201, 0, 0, Stacks(codes)), threshold);

    /// <summary>The entry of a chest in a result. Fails the test with a clear message when the id is not in the result.</summary>
    internal static ChestAutoAssignment At(AutoAssignResult result, uint id) =>
        result.Find(Id(id)) ?? throw new InvalidOperationException("Chest " + id + " is not in the result.");

    /// <summary>The serial numbers of containers, in order.</summary>
    internal static uint[] ContainerIds(IEnumerable<ContainerSnapshot> chests) => chests.Select(c => c.Id.Id).ToArray();

    /// <summary>The serial numbers of result entries, in order.</summary>
    internal static uint[] EntryIds(IEnumerable<ChestAutoAssignment> entries) => entries.Select(e => e.Id.Id).ToArray();

    /// <summary>Claims in order on one pool. Each entry is the claimed serial number, or null when nothing was returned.</summary>
    internal static uint?[] ClaimIds(OverflowPool pool, params string[] categories) =>
        categories.Select(name => pool.Claim(name)?.Id.Id).ToArray();

    /// <summary>
    /// Compares one entry with an expected outcome and category. The label is part of the failure message. The assignment is
    /// Keep for Keep, null for an unassigned chest, and ForCategory(name) otherwise.
    /// </summary>
    internal static void AssertChest(string label, AutoAssignOutcome outcome, string? categoryName, ChestAutoAssignment actual)
    {
        Assert.Equal((label, outcome, categoryName), (label, actual.Outcome, actual.CategoryName));

        CategoryAssignment? expected = outcome switch
        {
            AutoAssignOutcome.Keep => CategoryAssignment.Keep,
            _ => categoryName is null ? null : CategoryAssignment.ForCategory(categoryName),
        };
        Assert.Equal(expected, actual.Assignment);
    }

    /// <summary>Checks every row of a 5.2 style table against a result.</summary>
    internal static void AssertTable(string label, AutoAssignResult result, (uint Id, AutoAssignOutcome Outcome, string? Name)[] table)
    {
        foreach ((uint id, AutoAssignOutcome outcome, string? name) in table)
        {
            AssertChest(label + " chest " + id, outcome, name, At(result, id));
        }
    }

    /// <summary>The (id, outcome, assignment) of every entry of a result, in order. Used to compare two results.</summary>
    internal static (uint Id, AutoAssignOutcome Outcome, CategoryAssignment? Assignment)[] Summary(AutoAssignResult result) =>
        result.Chests.Select(c => (c.Id.Id, c.Outcome, c.Assignment)).ToArray();

    /// <summary>Checks that two results agree on every entry and on the overflow ids.</summary>
    internal static void AssertSameResult(AutoAssignResult expected, AutoAssignResult actual)
    {
        Assert.Equal(Summary(expected), Summary(actual));
        Assert.Equal(ContainerIds(expected.OverflowChests), ContainerIds(actual.OverflowChests));
    }

    /// <summary>
    /// A test-only read-only dictionary that enumerates its entries in reverse insertion order. Lookups behave as usual.
    /// </summary>
    internal sealed class ReversedDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> lookup;
        private readonly List<KeyValuePair<TKey, TValue>> entries;

        /// <summary>Wraps a dictionary. Its entries are enumerated in the reverse of the source order.</summary>
        /// <param name="source">The dictionary to wrap.</param>
        internal ReversedDictionary(IDictionary<TKey, TValue> source)
        {
            lookup = new Dictionary<TKey, TValue>(source);
            entries = new List<KeyValuePair<TKey, TValue>>(source);
            entries.Reverse();
        }

        /// <summary>Gets the value for a key.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        public TValue this[TKey key] => lookup[key];

        /// <summary>Gets the keys in reverse order.</summary>
        public IEnumerable<TKey> Keys => entries.Select(entry => entry.Key);

        /// <summary>Gets the values in reverse order.</summary>
        public IEnumerable<TValue> Values => entries.Select(entry => entry.Value);

        /// <summary>Gets the number of entries.</summary>
        public int Count => entries.Count;

        /// <summary>Determines whether the key is present.</summary>
        /// <param name="key">The key.</param>
        /// <returns>True when present.</returns>
        public bool ContainsKey(TKey key) => lookup.ContainsKey(key);

        /// <summary>Looks up a key.</summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value when found.</param>
        /// <returns>True when found.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => lookup.TryGetValue(key, out value);

        /// <summary>Enumerates the entries in reverse order.</summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => entries.GetEnumerator();

        /// <summary>Enumerates the entries in reverse order.</summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
