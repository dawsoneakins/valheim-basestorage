namespace BaseStorage.Core.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The immutable result of <see cref="AutoAssigner.Resolve"/>: one entry per chest in snapshot order, the overflow chests and
/// the category lookups. It is safe to share across threads. Overflow pools are created from it, never inside it.
/// </summary>
public sealed class AutoAssignResult
{
    private readonly int threshold;
    private readonly ChestAutoAssignment[] chests;
    private readonly ContainerSnapshot[] overflow;
    private readonly ReadOnlyCollection<ChestAutoAssignment> chestsView;
    private readonly ReadOnlyCollection<ContainerSnapshot> overflowView;
    private readonly Dictionary<ContainerId, ChestAutoAssignment> byId;

    /// <summary>Creates a result. Only the resolver creates results.</summary>
    /// <param name="threshold">The threshold the result was computed with.</param>
    /// <param name="chests">One entry per input container, in snapshot order.</param>
    /// <param name="overflow">The containers whose outcome is Overflow, in snapshot order.</param>
    /// <param name="mixedAssigned">The entries the mixed-chest step gave a category (S-A-001); these are the only ones Organize saves.</param>
    internal AutoAssignResult(int threshold, ChestAutoAssignment[] chests, ContainerSnapshot[] overflow, ChestAutoAssignment[]? mixedAssigned = null)
    {
        MixedAssigned = new ReadOnlyCollection<ChestAutoAssignment>(mixedAssigned ?? Array.Empty<ChestAutoAssignment>());
        this.threshold = threshold;
        this.chests = chests;
        this.overflow = overflow;
        chestsView = new ReadOnlyCollection<ChestAutoAssignment>(chests);
        overflowView = new ReadOnlyCollection<ContainerSnapshot>(overflow);
        byId = new Dictionary<ContainerId, ChestAutoAssignment>(chests.Length);
        foreach (var chest in chests)
        {
            byId[chest.Id] = chest;
        }
    }

    /// <summary>Gets the entries the mixed-chest step gave a category, in snapshot order. Organize saves these after a successful run (S-A-003).</summary>
    public IReadOnlyList<ChestAutoAssignment> MixedAssigned { get; }

    /// <summary>Gets the threshold this result was computed with, exactly as it was passed in.</summary>
    public int AutoAssignThresholdPct => threshold;

    /// <summary>Gets one entry per input container, in snapshot order. The list is read-only.</summary>
    public IReadOnlyList<ChestAutoAssignment> Chests => chestsView;

    /// <summary>Gets the entry for an id.</summary>
    /// <param name="id">The container id to look up.</param>
    /// <returns>The entry, or null when the id was not in the input.</returns>
    public ChestAutoAssignment? Find(ContainerId id) => byId.TryGetValue(id, out var found) ? found : null;

    /// <summary>Gets the containers whose outcome is Overflow, in snapshot order. The list is read-only and is the initial pool.</summary>
    public IReadOnlyList<ContainerSnapshot> OverflowChests => overflowView;

    /// <summary>Gets the category's existing chests: AlreadyAssigned or Assigned with that category name, in snapshot order.</summary>
    /// <param name="categoryName">The category name, matched ordinally; not null.</param>
    /// <returns>A new read-only list, which is empty when there is no such chest.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="categoryName"/> is null.</exception>
    public IReadOnlyList<ContainerSnapshot> GetCategoryChests(string categoryName)
    {
        if (categoryName is null)
        {
            throw new ArgumentNullException(nameof(categoryName));
        }

        var list = new List<ContainerSnapshot>();
        foreach (var chest in chests)
        {
            if ((chest.Outcome == AutoAssignOutcome.AlreadyAssigned || chest.Outcome == AutoAssignOutcome.Assigned)
                && string.Equals(chest.CategoryName, categoryName, StringComparison.Ordinal))
            {
                list.Add(chest.Container);
            }
        }

        return list.AsReadOnly();
    }

    /// <summary>Creates a new, independent overflow pool holding <see cref="OverflowChests"/>.</summary>
    /// <returns>A new pool. Claiming from it never changes this result or any other pool.</returns>
    public OverflowPool CreateOverflowPool() => new OverflowPool(this, (ContainerSnapshot[])overflow.Clone());
}
