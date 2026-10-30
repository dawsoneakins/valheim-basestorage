namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Plan comparison and row helpers for the S-E004-006 tests (ADR-GWIN-039). The tests write expected rows as literals with these
/// helpers. Each row carries every property of its contract type, so an element-wise comparison checks every field.
/// </summary>
internal static class PlanAssert
{
    /// <summary>A planned move, with every property of <c>PlannedMove</c>.</summary>
    internal sealed record MoveRow(
        uint Source,
        int SourceStackIndex,
        uint Destination,
        string PrefabName,
        int Quality,
        int MaxStack,
        int Count,
        string CategoryName);

    /// <summary>A no-room entry, with every property of <c>NoRoomEntry</c>.</summary>
    internal sealed record NoRoomRow(
        uint Source,
        int SourceStackIndex,
        string PrefabName,
        int Quality,
        int MaxStack,
        int Count,
        string CategoryName);

    /// <summary>A skipped chest, with every property of <c>SkippedChest</c> except the snapshot itself.</summary>
    internal sealed record SkipRow(uint Id, SkipReason Kind, string Reason, string? PlayerName, ExclusionReason? Exclusion);

    /// <summary>A chest layout. The stacks are the describe-strings of the layout, in order.</summary>
    internal sealed record LayoutRow(uint Id, ChestRole Role, string? CategoryName, string Stacks);

    /// <summary>The expected move (source, index) to destination, with the item and the category.</summary>
    internal static MoveRow Move(uint source, int stackIndex, uint destination, string prefab, int quality, int maxStack, int count, string category) =>
        new MoveRow(source, stackIndex, destination, prefab, quality, maxStack, count, category);

    /// <summary>The expected no-room entry of a source stack.</summary>
    internal static NoRoomRow NoRoom(uint source, int stackIndex, string prefab, int quality, int maxStack, int count, string category) =>
        new NoRoomRow(source, stackIndex, prefab, quality, maxStack, count, category);

    /// <summary>The expected skipped entry of a chest.</summary>
    internal static SkipRow Skip(uint id, SkipReason kind, string reason, string? playerName, ExclusionReason? exclusion) =>
        new SkipRow(id, kind, reason, playerName, exclusion);

    /// <summary>The expected layout of a chest, with its stacks in order.</summary>
    internal static LayoutRow Layout(uint id, ChestRole role, string? category, params ItemStackSnapshot[] stacks) =>
        new LayoutRow(id, role, category, Join(stacks));

    /// <summary>Describes one stack with every identity field: prefab, count, quality and max stack.</summary>
    internal static string Describe(ItemStackSnapshot stack) => Describe(stack.PrefabName, stack.Count, stack.Quality, stack.MaxStack);

    /// <summary>Describes one stack from its fields.</summary>
    internal static string Describe(string prefab, int count, int quality, int maxStack) => prefab + " x" + count + " q" + quality + " max" + maxStack;

    /// <summary>Joins the descriptions of stacks, in order.</summary>
    internal static string Join(IEnumerable<ItemStackSnapshot> stacks) => string.Join("; ", stacks.Select(stack => Describe(stack)));

    /// <summary>The moves of a plan, in order, as rows.</summary>
    internal static MoveRow[] MovesOf(OrganizePlan plan) =>
        plan.Moves.Select(m => new MoveRow(m.Source.Id, m.SourceStackIndex, m.Destination.Id, m.PrefabName, m.Quality, m.MaxStack, m.Count, m.CategoryName))
            .ToArray();

    /// <summary>The no-room entries of a plan, in order, as rows.</summary>
    internal static NoRoomRow[] NoRoomOf(OrganizePlan plan) =>
        plan.NoRoom.Select(e => new NoRoomRow(e.Source.Id, e.SourceStackIndex, e.PrefabName, e.Quality, e.MaxStack, e.Count, e.CategoryName))
            .ToArray();

    /// <summary>The skipped entries of a plan, in order, as rows.</summary>
    internal static SkipRow[] SkipsOf(OrganizePlan plan) =>
        plan.Skipped.Select(s => new SkipRow(s.Id.Id, s.Kind, s.Reason, s.PlayerName, s.Exclusion)).ToArray();

    /// <summary>The layout of one chest as a row.</summary>
    internal static LayoutRow LayoutRowOf(ChestLayout layout) =>
        new LayoutRow(layout.Id.Id, layout.Role, layout.CategoryName, Join(layout.Stacks));

    /// <summary>The layouts of a plan, in snapshot order, as rows.</summary>
    internal static LayoutRow[] LayoutsOf(OrganizePlan plan) => plan.Layouts.Select(LayoutRowOf).ToArray();

    /// <summary>The claimed overflow layouts, in claim order, as rows.</summary>
    internal static LayoutRow[] ClaimedOf(OrganizePlan plan) => plan.ClaimedOverflow.Select(LayoutRowOf).ToArray();

    /// <summary>Asserts that two plans are equal on every property of every list, in order (AC-12).</summary>
    internal static void AssertSamePlan(OrganizePlan expected, OrganizePlan actual)
    {
        Assert.Equal(MovesOf(expected), MovesOf(actual));
        Assert.Equal(NoRoomOf(expected), NoRoomOf(actual));
        Assert.Equal(SkipsOf(expected), SkipsOf(actual));
        Assert.Equal(LayoutsOf(expected), LayoutsOf(actual));
        Assert.Equal(ClaimedOf(expected), ClaimedOf(actual));
        Assert.Equal(expected.Skipped.Select(s => s.Container), actual.Skipped.Select(s => s.Container));
        Assert.Equal(expected.Layouts.Select(l => l.Container), actual.Layouts.Select(l => l.Container));
        Assert.Equal(expected.AutoAssign.Chests.Select(c => c.Id), actual.AutoAssign.Chests.Select(c => c.Id));
    }

    /// <summary>The item totals of stacks, keyed by the full identity (prefab, quality, max stack, P5).</summary>
    internal static Dictionary<string, long> ItemTotals(IEnumerable<ItemStackSnapshot> stacks)
    {
        var totals = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (ItemStackSnapshot stack in stacks)
        {
            string key = stack.PrefabName + "|" + stack.Quality + "|" + stack.MaxStack;
            totals[key] = (totals.TryGetValue(key, out long sum) ? sum : 0L) + stack.Count;
        }

        return totals;
    }

    /// <summary>The item totals of stacks, keyed by the prefab name only.</summary>
    internal static Dictionary<string, long> PrefabTotals(IEnumerable<ItemStackSnapshot> stacks)
    {
        var totals = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (ItemStackSnapshot stack in stacks)
        {
            totals[stack.PrefabName] = (totals.TryGetValue(stack.PrefabName, out long sum) ? sum : 0L) + stack.Count;
        }

        return totals;
    }

    /// <summary>Asserts that two total maps hold the same keys and values, whatever their order.</summary>
    internal static void AssertTotals(IReadOnlyDictionary<string, long> expected, IReadOnlyDictionary<string, long> actual) =>
        Assert.Equal(Sorted(expected), Sorted(actual));

    /// <summary>Asserts that every move of each claimed overflow chest has the category of that chest's layout (P11).</summary>
    internal static void AssertClaimedMovesMatchLayouts(OrganizePlan plan)
    {
        foreach (ChestLayout claimed in plan.ClaimedOverflow)
        {
            foreach (PlannedMove move in plan.Moves.Where(m => m.Destination == claimed.Id))
            {
                Assert.Equal(claimed.CategoryName, move.CategoryName);
            }
        }
    }

    private static string[] Sorted(IReadOnlyDictionary<string, long> totals) =>
        totals.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "=" + pair.Value).ToArray();
}
