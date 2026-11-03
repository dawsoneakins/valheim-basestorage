namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize;

/// <summary>
/// The S-QF-001 fixtures (ADR-GWIN-180). The station position, the item sets, the selections, the eligibility options and the chest rows of
/// contract 7 are copied as literals. Expected values are never computed here. The chest helpers come from <c>OrganizeFixtures</c>.
/// </summary>
internal static class FeedFixtures
{
    /// <summary>The station position of every fixture (contract 7): (0, 30, 0).</summary>
    internal static readonly WorldPosition StationAt = new WorldPosition(0f, 30f, 0f);

    /// <summary>The empty skip list of every fixture.</summary>
    internal static readonly ContainerId[] NoSkip = Array.Empty<ContainerId>();

    /// <summary>The smelter inputs of contract 7: CopperOre and TinOre.</summary>
    internal static readonly string[] Ores = { "CopperOre", "TinOre" };

    /// <summary>The single-input inputs of contract 7: MeadBaseTasty and MeadBaseHealthMinor.</summary>
    internal static readonly string[] Mead = { "MeadBaseTasty", "MeadBaseHealthMinor" };

    /// <summary>The several-input inputs of contract 7: RawMeat, NeckTail and FishRaw.</summary>
    internal static readonly string[] Food = { "RawMeat", "NeckTail", "FishRaw" };

    /// <summary>Builds a selection with <c>With</c>, in the order listed.</summary>
    /// <param name="entries">The (prefab, keep) pairs.</param>
    /// <returns>The selection.</returns>
    internal static FeedSelection Sel(params (string Prefab, int Keep)[] entries)
    {
        FeedSelection selection = FeedSelection.Empty;
        foreach ((string prefab, int keep) in entries)
        {
            selection = selection.With(prefab, keep);
        }

        return selection;
    }

    /// <summary>Builds a selection of items that keep nothing, in the order listed.</summary>
    /// <param name="prefabs">The prefab names.</param>
    /// <returns>The selection.</returns>
    internal static FeedSelection Items(params string[] prefabs)
    {
        FeedSelection selection = FeedSelection.Empty;
        foreach (string prefab in prefabs)
        {
            selection = selection.With(prefab, 0);
        }

        return selection;
    }

    /// <summary>The eligibility options of contract 7 (<c>opt(k)</c>).</summary>
    /// <param name="keepChestsCraftable">The KeepChestsCraftable value.</param>
    /// <param name="excludeVehicles">The ExcludeVehicles value; true unless a row says otherwise.</param>
    /// <returns>The options.</returns>
    internal static EligibilityOptions Opt(bool keepChestsCraftable, bool excludeVehicles = true) =>
        new EligibilityOptions(excludeVehicles, keepChestsCraftable);

    /// <summary>Calls the 5-argument <c>Next</c> at the fixture position with no skip, as v0.4.0 did.</summary>
    /// <param name="station">The station.</param>
    /// <param name="selection">The selection.</param>
    /// <param name="chests">The chests, already filtered by the caller.</param>
    /// <returns>The order, or null.</returns>
    internal static FeedOrder? Next5(FeedStationState station, FeedSelection selection, IReadOnlyList<ContainerSnapshot> chests) =>
        FeedPlanner.Next(station, selection, chests, StationAt, NoSkip);

    /// <summary>Calls the 7-argument <c>Next</c> with defaults: no assignments, <c>Opt(true)</c> and no skip.</summary>
    /// <param name="station">The station.</param>
    /// <param name="selection">The selection.</param>
    /// <param name="chests">The chests, unfiltered.</param>
    /// <param name="assignments">The assignments, or null for none.</param>
    /// <param name="options">The options, or null for <c>Opt(true)</c>.</param>
    /// <param name="skip">The skip list, or null for none.</param>
    /// <returns>The order, or null.</returns>
    internal static FeedOrder? Next7(
        FeedStationState station,
        FeedSelection selection,
        IReadOnlyList<ContainerSnapshot> chests,
        IReadOnlyDictionary<ContainerId, CategoryAssignment>? assignments = null,
        EligibilityOptions? options = null,
        ICollection<ContainerId>? skip = null) =>
        FeedPlanner.Next(
            station,
            selection,
            chests,
            assignments ?? new Dictionary<ContainerId, CategoryAssignment>(),
            options ?? Opt(true),
            StationAt,
            skip ?? NoSkip);

    /// <summary>The chests the FM12 rule accepts as sources, in input order (contract 0.2 FM12).</summary>
    /// <param name="chests">The chests.</param>
    /// <param name="assignments">The assignments per chest id.</param>
    /// <param name="options">The options.</param>
    /// <returns>The source chests.</returns>
    internal static ContainerSnapshot[] SourcesOf(
        IReadOnlyList<ContainerSnapshot> chests,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        EligibilityOptions options)
    {
        var sources = new List<ContainerSnapshot>(chests.Count);
        for (int i = 0; i < chests.Count; i++)
        {
            bool isKeep = assignments.TryGetValue(chests[i].Id, out CategoryAssignment? assignment) && assignment is not null && assignment.IsKeep;
            if (ContainerEligibility.GetExclusionReason(chests[i], isKeep, ContainerPurpose.CraftAvailability, options) is null)
            {
                sources.Add(chests[i]);
            }
        }

        return sources.ToArray();
    }

    /// <summary>Compares two orders field by field.</summary>
    /// <param name="expected">The expected order, or null.</param>
    /// <param name="actual">The actual order, or null.</param>
    internal static void AssertSameOrder(FeedOrder? expected, FeedOrder? actual)
    {
        if (expected is null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.NotNull(actual);
        Assert.Equal(expected.Chest, actual!.Chest);
        Assert.Equal(expected.PrefabName, actual.PrefabName);
        Assert.Equal(expected.Amount, actual.Amount);
        Assert.Equal(expected.IsFuel, actual.IsFuel);
    }

    /// <summary>Compares two orders field by field without throwing.</summary>
    /// <param name="a">The first order, or null.</param>
    /// <param name="b">The second order, or null.</param>
    /// <returns>True when both are null, or both hold the same chest, prefab, amount and fuel flag.</returns>
    internal static bool SameOrder(FeedOrder? a, FeedOrder? b)
    {
        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        return a.Chest.Equals(b.Chest)
            && string.Equals(a.PrefabName, b.PrefabName, StringComparison.Ordinal)
            && a.Amount == b.Amount
            && a.IsFuel == b.IsFuel;
    }

    /// <summary>An order written as literal expectations. Tests never build a <c>FeedOrder</c>, whose constructor is internal to Core.</summary>
    /// <param name="Chest">The chest's number in the fixture (<c>Id(n)</c>).</param>
    /// <param name="PrefabName">The item's prefab name.</param>
    /// <param name="Amount">How many move.</param>
    /// <param name="IsFuel">True when the item goes in as fuel.</param>
    internal sealed record ExpectedOrder(uint Chest, string PrefabName, int Amount, bool IsFuel);

    /// <summary>Compares an actual order with literal expectations.</summary>
    /// <param name="expected">The expected order, or null for no order.</param>
    /// <param name="actual">The actual order, or null.</param>
    /// <returns>True when both are null, or both describe the same transfer.</returns>
    internal static bool Matches(ExpectedOrder? expected, FeedOrder? actual)
    {
        if (expected is null || actual is null)
        {
            return expected is null && actual is null;
        }

        return actual.Chest.Equals(OrganizeFixtures.Id(expected.Chest))
            && string.Equals(actual.PrefabName, expected.PrefabName, StringComparison.Ordinal)
            && actual.Amount == expected.Amount
            && actual.IsFuel == expected.IsFuel;
    }

    /// <summary>Describes an order for a failure message.</summary>
    /// <param name="order">The order, or null.</param>
    /// <returns>The text.</returns>
    internal static string Describe(FeedOrder? order) =>
        order is null
            ? "null"
            : $"chest {order.Chest}, {order.PrefabName}, {order.Amount}, IsFuel {order.IsFuel}";

    /// <summary>One row of contract 7.5: the smelter (or fuel-only) station, the selection, the chests, the assignments and the options.</summary>
    /// <param name="Id">The row id, KC1 to KC13.</param>
    /// <param name="Station">The station.</param>
    /// <param name="Selection">The selection.</param>
    /// <param name="Chests">The chests.</param>
    /// <param name="Assignments">The assignments.</param>
    /// <param name="Options">The options.</param>
    internal sealed record KeepRow(
        string Id,
        FeedStationState Station,
        FeedSelection Selection,
        ContainerSnapshot[] Chests,
        Dictionary<ContainerId, CategoryAssignment> Assignments,
        EligibilityOptions Options);

    /// <summary>
    /// Builds a contract 7.5 row. The row's own option value is used unless <paramref name="keepChestsCraftable"/> overrides it (KC1 to KC6,
    /// KC12 and KC13 only; KC9 to KC11 keep their own options).
    /// </summary>
    /// <param name="id">The row id, KC1 to KC13.</param>
    /// <param name="keepChestsCraftable">The override of KeepChestsCraftable, or null for the row's value.</param>
    /// <returns>The row.</returns>
    internal static KeepRow KeepRowOf(string id, bool? keepChestsCraftable = null)
    {
        FeedStationState smelter = FeedStationState.SmelterClass(0, 10, 20f, 20f, "Coal", Ores);
        Dictionary<ContainerId, CategoryAssignment> keep1 = OrganizeFixtures.Assign((1, CategoryAssignment.Keep));
        Dictionary<ContainerId, CategoryAssignment> none = new Dictionary<ContainerId, CategoryAssignment>();
        EligibilityOptions Opts(bool fallback) => Opt(keepChestsCraftable ?? fallback);

        return id switch
        {
            "KC1" => new KeepRow(id, smelter, Sel(("CopperOre", 0)), new[] { OrganizeFixtures.C(1, 1f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)) }, keep1, Opts(true)),
            "KC2" => new KeepRow(id, smelter, Sel(("CopperOre", 0)), new[] { OrganizeFixtures.C(1, 1f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)) }, keep1, Opts(false)),
            "KC3" => new KeepRow(id, smelter, Sel(("CopperOre", 0)), new[] { OrganizeFixtures.C(1, 1f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)) }, keep1, Opts(false)),
            "KC4" => new KeepRow(id, smelter, Sel(("CopperOre", 0)), new[] { OrganizeFixtures.C(1, 1f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)) }, keep1, Opts(true)),
            "KC5" => new KeepRow(id, smelter, Sel(("CopperOre", 25)), new[] { OrganizeFixtures.C(1, 1f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("CopperOre", 10)) }, keep1, Opts(true)),
            "KC6" => new KeepRow(id, smelter, Sel(("CopperOre", 25)), new[] { OrganizeFixtures.C(1, 1f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("CopperOre", 10)) }, keep1, Opts(false)),
            "KC7" => new KeepRow(id, smelter, Sel(("CopperOre", 0)), new[] { OrganizeFixtures.C(1, 1f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)) }, OrganizeFixtures.Assign((1, CategoryAssignment.ForCategory("Ores"))), Opts(false)),
            "KC8" => new KeepRow(id, smelter, Sel(("CopperOre", 0)), new[] { OrganizeFixtures.C(1, 1f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)) }, none, Opts(false)),
            "KC9" => new KeepRow(id, smelter, Sel(("CopperOre", 0)), new[] { OrganizeFixtures.CF(1, 1f, 0f, 4, ContainerFlags.PlayerBuilt | ContainerFlags.Tombstone, OrganizeFixtures.S("CopperOre", 30)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)) }, none, Opts(true)),
            "KC10" => new KeepRow(id, smelter, Sel(("CopperOre", 0)), new[] { OrganizeFixtures.CF(1, 1f, 0f, 4, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle, OrganizeFixtures.S("CopperOre", 30)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)) }, none, new EligibilityOptions(true, true)),
            "KC11" => new KeepRow(id, smelter, Sel(("CopperOre", 0)), new[] { OrganizeFixtures.CF(1, 1f, 0f, 4, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle, OrganizeFixtures.S("CopperOre", 30)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("CopperOre", 30)) }, none, new EligibilityOptions(false, true)),
            "KC12" => new KeepRow(id, FeedStationState.FuelOnly(0f, 10f, "Wood"), Sel(("Wood", 0)), new[] { OrganizeFixtures.C(1, 1f, 0f, 4, OrganizeFixtures.S("Wood", 50)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("Wood", 3)) }, keep1, Opts(false)),
            "KC13" => new KeepRow(id, FeedStationState.FuelOnly(0f, 10f, "Wood"), Sel(("Wood", 0)), new[] { OrganizeFixtures.C(1, 1f, 0f, 4, OrganizeFixtures.S("Wood", 50)), OrganizeFixtures.C(2, 9f, 0f, 4, OrganizeFixtures.S("Wood", 3)) }, keep1, Opts(true)),
            _ => throw new ArgumentException("Unknown contract 7.5 row: " + id, nameof(id)),
        };
    }
}
