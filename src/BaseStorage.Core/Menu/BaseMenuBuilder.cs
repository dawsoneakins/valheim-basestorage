namespace BaseStorage.Core.Menu;

using System;
using System.Collections.Generic;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;

/// <summary>
/// Builds the BaseStorage menu's chest list from a base snapshot (S-C-002). Pure: rows are sorted by rule label, then distance from the
/// player, then chest id, so the list does not reshuffle between refreshes.
/// </summary>
public static class BaseMenuBuilder
{
    /// <summary>Builds the rows of a base.</summary>
    /// <param name="containers">The chests of the base.</param>
    /// <param name="assignments">The stored assignment per chest id; a missing id is Auto.</param>
    /// <param name="player">The player's position.</param>
    /// <param name="filter">A rule label to keep, or null for every chest.</param>
    /// <param name="strings">The strings table.</param>
    /// <returns>The sorted rows.</returns>
    public static IReadOnlyList<BaseMenuRow> Rows(
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        WorldPosition player,
        string? filter,
        StringTable strings)
    {
        if (containers is null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        if (assignments is null)
        {
            throw new ArgumentNullException(nameof(assignments));
        }

        if (strings is null)
        {
            throw new ArgumentNullException(nameof(strings));
        }

        var rows = new List<BaseMenuRow>(containers.Count);
        for (var i = 0; i < containers.Count; i++)
        {
            var chest = containers[i];
            assignments.TryGetValue(chest.Id, out var assignment);
            var label = ChestCategoryReader.Label(assignment, strings);
            if (filter is null || string.Equals(label, filter, StringComparison.Ordinal))
            {
                rows.Add(new BaseMenuRow(chest, assignment, label, FillPercent(chest), DistanceSquared(chest.Position, player)));
            }
        }

        rows.Sort(Compare);
        return rows;
    }

    /// <summary>Gets the percent of a base's slots in use, rounded down.</summary>
    /// <param name="containers">The chests of the base.</param>
    /// <returns>The percent; 0 for no chests.</returns>
    public static int FillPercent(IReadOnlyList<ContainerSnapshot> containers)
    {
        if (containers is null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        long used = 0;
        long slots = 0;
        for (var i = 0; i < containers.Count; i++)
        {
            used += containers[i].Contents.Count;
            slots += containers[i].SlotCount;
        }

        return slots == 0 ? 0 : (int)(used * 100 / slots);
    }

    private static int FillPercent(ContainerSnapshot chest) =>
        chest.SlotCount == 0 ? 0 : chest.Contents.Count * 100 / chest.SlotCount;

    private static float DistanceSquared(WorldPosition a, WorldPosition b)
    {
        var x = a.X - b.X;
        var z = a.Z - b.Z;
        return (x * x) + (z * z);
    }

    private static int Compare(BaseMenuRow a, BaseMenuRow b)
    {
        var byLabel = string.CompareOrdinal(a.RuleLabel, b.RuleLabel);
        if (byLabel != 0)
        {
            return byLabel;
        }

        var byDistance = a.DistanceSquared.CompareTo(b.DistanceSquared);
        return byDistance != 0 ? byDistance : a.Chest.Id.CompareTo(b.Chest.Id);
    }
}
