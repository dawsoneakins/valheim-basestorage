namespace BaseStorage.Core.Menu;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>One chest in the BaseStorage menu's list (S-C-002).</summary>
public sealed class BaseMenuRow
{
    /// <summary>Creates a row. Only the builder creates rows.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="assignment">The stored assignment; null for Auto.</param>
    /// <param name="ruleLabel">The display label of the assignment.</param>
    /// <param name="fillPercent">The percent of the chest's slots in use.</param>
    /// <param name="distanceSquared">The squared distance from the player, in metres.</param>
    internal BaseMenuRow(ContainerSnapshot chest, CategoryAssignment? assignment, string ruleLabel, int fillPercent, float distanceSquared)
    {
        Chest = chest;
        Assignment = assignment;
        RuleLabel = ruleLabel;
        FillPercent = fillPercent;
        DistanceSquared = distanceSquared;
    }

    /// <summary>Gets the chest.</summary>
    public ContainerSnapshot Chest { get; }

    /// <summary>Gets the stored assignment; null for Auto.</summary>
    public CategoryAssignment? Assignment { get; }

    /// <summary>Gets the display label of the assignment.</summary>
    public string RuleLabel { get; }

    /// <summary>Gets the percent of the chest's slots in use.</summary>
    public int FillPercent { get; }

    /// <summary>Gets the squared distance from the player, in metres.</summary>
    public float DistanceSquared { get; }

    /// <summary>Gets the items in this chest that match the current search, most first.</summary>
    public IReadOnlyList<BaseSearchHit> Hits { get; private set; } = Array.Empty<BaseSearchHit>();

    /// <summary>Gets how well the chest matched the current search; lower is better.</summary>
    internal int SearchRank { get; private set; }

    /// <summary>Gets a value indicating whether the local player may not open this chest.</summary>
    public bool AccessDenied => (Chest.Flags & ContainerFlags.AccessDenied) != 0;

    /// <summary>Gets a value indicating whether another player has this chest open.</summary>
    public bool InUse => (Chest.Flags & ContainerFlags.InUseByOther) != 0;

    /// <summary>Copies the row with search hits.</summary>
    /// <param name="hits">The hits; empty when only the chest's label matched.</param>
    /// <param name="rank">How well the chest matched.</param>
    /// <returns>The copy.</returns>
    internal BaseMenuRow WithHits(IReadOnlyList<BaseSearchHit> hits, int rank) =>
        new BaseMenuRow(Chest, Assignment, RuleLabel, FillPercent, DistanceSquared) { Hits = hits, SearchRank = rank };
}
