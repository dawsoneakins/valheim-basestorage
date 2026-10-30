namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Snapshots;

/// <summary>The candidate build-range rules that the S-E002-001 fixtures list in their [ADR] columns (contract 7.5).</summary>
internal enum RuleVariant
{
    /// <summary>3D Euclidean distance, strict less-than.</summary>
    Euclidean3DStrict,

    /// <summary>3D Euclidean distance, less-than-or-equal.</summary>
    Euclidean3DInclusive,

    /// <summary>Horizontal distance (X/Z plane), strict less-than.</summary>
    HorizontalStrict,

    /// <summary>Horizontal distance (X/Z plane), less-than-or-equal.</summary>
    HorizontalInclusive,
}

/// <summary>One contract row of S-E002-001 (contract 7.5): the inputs and the expected result.</summary>
internal sealed class Row
{
    private readonly (long UserId, uint Id)[]? _expected;
    private readonly IReadOnlyDictionary<RuleVariant, (long UserId, uint Id)[]>? _byRule;

    internal Row(StationSnapshot[] stations, StationId start, int maxBaseStations, (long UserId, uint Id)[] expected, bool isCapped)
    {
        Stations = stations;
        Start = start;
        MaxBaseStations = maxBaseStations;
        IsCapped = isCapped;
        _expected = expected;
    }

    internal Row(StationSnapshot[] stations, StationId start, int maxBaseStations, IReadOnlyDictionary<RuleVariant, (long UserId, uint Id)[]> byRule, bool isCapped)
    {
        Stations = stations;
        Start = start;
        MaxBaseStations = maxBaseStations;
        IsCapped = isCapped;
        _byRule = byRule;
    }

    internal StationSnapshot[] Stations { get; }

    internal StationId Start { get; }

    internal int MaxBaseStations { get; }

    internal bool IsCapped { get; }

    /// <summary>The expected (UserId, Id) pairs in fill order under <paramref name="rule"/>. Non-[ADR] rows ignore the rule.</summary>
    internal (long UserId, uint Id)[] ExpectedFor(RuleVariant rule) =>
        _byRule is null ? _expected! : _byRule[rule];
}

/// <summary>
/// Shared fixtures for the S-E002-001 Bases tests (ADR-GWIN-017). Every station and expected id is a literal copied from
/// contract section 7.5. Expected values are never derived from Core.
/// </summary>
internal static class StationGraphFixtures
{
    /// <summary>The creator peer id used by every row unless the row says otherwise.</summary>
    internal const long U = 3141592653L;

    /// <summary>The rule the tests use, chosen by ADR-GWIN-016 (vanilla: horizontal distance, strict less-than). The only place a variant is chosen.</summary>
    internal const RuleVariant VerifiedRule = RuleVariant.HorizontalStrict; // ADR-GWIN-016

    /// <summary>The default cap (SettingKeys.MaxBaseStations default 64).</summary>
    internal const int DefaultMax = 64;

    private static readonly Lazy<Dictionary<string, Row>> LazyRows = new(BuildRows);

    /// <summary>Every contract row, keyed by its ID (for example "G2a"), in ordinal order.</summary>
    internal static IReadOnlyDictionary<string, Row> Rows => LazyRows.Value;

    /// <summary>Builds a station as in contract 7.5 (default 31.25 m height, z -840, 20 m range, creator U).</summary>
    internal static StationSnapshot S(uint id, float x, float y = 31.25f, float z = -840f, float range = 20f, long userId = U) =>
        new(new StationId(userId, id), new WorldPosition(x, y, z), range);

    /// <summary>The station id (U, id).</summary>
    internal static StationId Id(uint id) => new(U, id);

    /// <summary>Expected (U, id) pairs for the given serial numbers.</summary>
    internal static (long UserId, uint Id)[] Ids(params uint[] ids) => ids.Select(i => (U, i)).ToArray();

    /// <summary>Runs <see cref="StationGraph.FindBase"/> for a row, optionally on the reversed list and with another cap.</summary>
    internal static StationGraphResult Run(string id, bool reversed = false, int? maxOverride = null)
    {
        Row row = Rows[id];
        StationSnapshot[] input = reversed ? Enumerable.Reverse(row.Stations).ToArray() : row.Stations;
        return StationGraph.FindBase(input, row.Start, maxOverride ?? row.MaxBaseStations);
    }

    /// <summary>Compares the (UserId, Id) pairs of a result, in order, with a row's expected pairs, then checks IsCapped.</summary>
    internal static void AssertRow(string id, bool reversed = false, int? maxOverride = null)
    {
        Row row = Rows[id];
        StationGraphResult result = Run(id, reversed, maxOverride);
        (long UserId, uint Id)[] actual = result.Stations.Select(s => (s.Id.UserId, s.Id.Id)).ToArray();

        Assert.Equal(row.ExpectedFor(VerifiedRule), actual);
        Assert.Equal(row.IsCapped, result.IsCapped);
    }

    private static Dictionary<RuleVariant, (long UserId, uint Id)[]> Variants(
        (long UserId, uint Id)[] threeDStrict,
        (long UserId, uint Id)[] threeDInclusive,
        (long UserId, uint Id)[] horizontalStrict,
        (long UserId, uint Id)[] horizontalInclusive) =>
        new()
        {
            [RuleVariant.Euclidean3DStrict] = threeDStrict,
            [RuleVariant.Euclidean3DInclusive] = threeDInclusive,
            [RuleVariant.HorizontalStrict] = horizontalStrict,
            [RuleVariant.HorizontalInclusive] = horizontalInclusive,
        };

    private static Dictionary<string, Row> BuildRows() => new(StringComparer.Ordinal)
    {
        // G1 single station
        ["G1"] = new([S(1, 0f)], Id(1), DefaultMax, Ids(1), false),

        // G2 chain of 5
        ["G2a"] = new([S(1, 0f), S(2, 15f), S(3, 30f), S(4, 45f), S(5, 60f)], Id(1), DefaultMax, Ids(1, 2, 3, 4, 5), false),
        ["G2b"] = new([S(1, 0f), S(2, 15f), S(3, 30f), S(4, 45f), S(5, 60f)], Id(3), DefaultMax, Ids(3, 2, 4, 1, 5), false),

        // G3 two separate bases
        ["G3a"] = new([S(1, 0f), S(2, 15f), S(10, 200f), S(11, 215f)], Id(1), DefaultMax, Ids(1, 2), false),
        ["G3b"] = new([S(1, 0f), S(2, 15f), S(10, 200f), S(11, 215f)], Id(11), DefaultMax, Ids(11, 10), false),

        // G4 broken chain (30 m gap between 2 and 4)
        ["G4a"] = new([S(1, 0f), S(2, 15f), S(4, 45f), S(5, 60f)], Id(1), DefaultMax, Ids(1, 2), false),
        ["G4b"] = new([S(1, 0f), S(2, 15f), S(4, 45f), S(5, 60f)], Id(5), DefaultMax, Ids(5, 4), false),

        // G5 overlapping ranges
        ["G5"] = new([S(1, 0f), S(2, 10f), S(3, 5f)], Id(2), DefaultMax, Ids(2, 1, 3), false),

        // G6 either-range rule
        ["G6a"] = new([S(1, 0f), S(2, 15f, range: 10f), S(3, 27f, range: 10f)], Id(1), DefaultMax, Ids(1, 2), false),
        ["G6b"] = new([S(1, 0f), S(2, 15f, range: 10f), S(3, 27f, range: 10f)], Id(2), DefaultMax, Ids(2, 1), false),
        ["G6c"] = new([S(1, 0f), S(2, 15f, range: 10f), S(3, 27f, range: 10f)], Id(3), DefaultMax, Ids(3), false),

        // G7 range 0 is never included and never bridges
        ["G7a"] = new([S(1, 0f), S(2, 15f, range: 0f), S(3, 30f)], Id(1), DefaultMax, Ids(1), false),
        ["G7b"] = new([S(1, 0f), S(2, 15f, range: 0f), S(3, 30f)], Id(2), DefaultMax, Ids(), false),
        ["G7c"] = new([S(1, 0f), S(2, 15f, range: 0f), S(3, 30f)], Id(3), DefaultMax, Ids(3), false),

        // G8 different heights (the non-[ADR] rows do not depend on the metric)
        ["G8a"] = new([S(1, 0f), S(2, 0f, 41.25f), S(3, 8f, 43.25f, -834f)], Id(1), DefaultMax, Ids(1, 2, 3), false),
        ["G8b"] = new([S(1, 0f), S(2, 0f, 41.25f), S(3, 8f, 43.25f, -834f)], Id(3), DefaultMax, Ids(3, 1, 2), false),
        ["G8c"] = new(
            [S(1, 0f), S(4, 0f, 71.25f)],
            Id(1),
            DefaultMax,
            Variants(Ids(1), Ids(1), Ids(1, 4), Ids(1, 4)),
            false),
        ["G8d"] = new(
            [S(1, 0f), S(5, 16f, 46.25f)],
            Id(1),
            DefaultMax,
            Variants(Ids(1), Ids(1), Ids(1, 5), Ids(1, 5)),
            false),

        // G9 boundary fixtures that pin the verified rule (ADR-GWIN-016); columns: 3D <, 3D <=, horizontal <, horizontal <=
        ["G9a"] = new(
            [S(1, 0f), S(2, 12f, 31.25f, -824f, range: 1f)],
            Id(1),
            DefaultMax,
            Variants(Ids(1), Ids(1, 2), Ids(1), Ids(1, 2)),
            false),
        ["G9b"] = new(
            [S(1, 0f), S(2, 12f, 31.25f, -824.01f, range: 1f)],
            Id(1),
            DefaultMax,
            Variants(Ids(1, 2), Ids(1, 2), Ids(1, 2), Ids(1, 2)),
            false),
        ["G9c"] = new(
            [S(1, 0f), S(2, 12f, 31.25f, -823.99f, range: 1f)],
            Id(1),
            DefaultMax,
            Variants(Ids(1), Ids(1), Ids(1), Ids(1)),
            false),
        ["G9d"] = new(
            [S(1, 0f), S(2, 0f, 51.25f, range: 1f)],
            Id(1),
            DefaultMax,
            Variants(Ids(1), Ids(1, 2), Ids(1, 2), Ids(1, 2)),
            false),
        ["G9e"] = new(
            [S(1, 0f), S(2, 0f, 51.26f, range: 1f)],
            Id(1),
            DefaultMax,
            Variants(Ids(1), Ids(1), Ids(1, 2), Ids(1, 2)),
            false),
        ["G9f"] = new(
            [S(1, 0f), S(2, 0f, 51.24f, range: 1f)],
            Id(1),
            DefaultMax,
            Variants(Ids(1, 2), Ids(1, 2), Ids(1, 2), Ids(1, 2)),
            false),

        // G10 traversal order and cap
        ["G10a"] = new([S(7, -24f), S(1, 24f), S(9, -12f), S(3, 12f), S(5, 0f)], Id(5), DefaultMax, Ids(5, 3, 9, 1, 7), false),
        ["G10b"] = new([S(7, -24f), S(1, 24f), S(9, -12f), S(3, 12f), S(5, 0f)], Id(5), 5, Ids(5, 3, 9, 1, 7), false),
        ["G10c"] = new([S(7, -24f), S(1, 24f), S(9, -12f), S(3, 12f), S(5, 0f)], Id(5), 4, Ids(5, 3, 9, 1), true),
        ["G10d"] = new([S(7, -24f), S(1, 24f), S(9, -12f), S(3, 12f), S(5, 0f)], Id(5), 3, Ids(5, 3, 9), true),
        ["G10e"] = new([S(7, -24f), S(1, 24f), S(9, -12f), S(3, 12f), S(5, 0f)], Id(5), 1, Ids(5), true),
        ["G10f"] = new([S(7, -24f), S(1, 24f), S(9, -12f), S(3, 12f), S(5, 0f)], Id(5), 0, Ids(5), true),
        ["G10g"] = new([S(7, -24f), S(1, 24f), S(9, -12f), S(3, 12f), S(5, 0f)], Id(1), 2, Ids(1, 3), true),
        ["G10h"] = new([S(7, -24f), S(1, 24f), S(9, -12f), S(3, 12f), S(5, 0f)], Id(1), DefaultMax, Ids(1, 3, 5, 9, 7), false),
        ["G10i"] = new([S(1, 0f)], Id(1), 1, Ids(1), false),

        // G11 neighbours ordered by StationId (UserId first)
        ["G11"] = new(
            [S(1, 0f), S(2, -12f), S(900, 12f, userId: 2718281828L)],
            Id(1),
            DefaultMax,
            [(U, 1u), (2718281828L, 900u), (U, 2u)],
            false),

        // G12 totality and duplicates
        ["G12a"] = new([S(1, 0f), S(2, 0f)], Id(1), DefaultMax, Ids(1, 2), false),
        ["G12b"] = new([S(1, 0f), S(1, 100f), S(2, 15f)], Id(1), DefaultMax, Ids(1, 2), false),
        ["G12c"] = new([S(1, 100f), S(1, 0f), S(2, 15f)], Id(1), DefaultMax, Ids(1), false),
        ["G12d"] = new([S(1, 0f), S(1, 0f), S(2, 15f)], Id(1), DefaultMax, Ids(1, 2), false),

        // G13 start not in list, none id, empty list
        ["G13a"] = new([S(1, 0f)], Id(99), DefaultMax, Ids(), false),
        ["G13b"] = new([S(1, 0f)], default, DefaultMax, Ids(), false),
        ["G13c"] = new(Array.Empty<StationSnapshot>(), Id(1), DefaultMax, Ids(), false),
    };
}
