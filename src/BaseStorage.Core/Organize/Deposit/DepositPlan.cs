namespace BaseStorage.Core.Organize.Deposit;

using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// The result of one Deposit planning (deposit contract 4.1): the move plan, whose only source is the player inventory and whose
/// first layout is the inventory, and the stacks the Deposit keeps where they are. Immutable and safe to share across threads.
/// </summary>
public sealed class DepositPlan
{
    private readonly Dictionary<int, ExcludedStack> excludedByIndex;

    /// <summary>Creates a plan. Only the planner creates plans.</summary>
    /// <param name="plan">The move plan.</param>
    /// <param name="player">The inventory the plan was made from.</param>
    /// <param name="excluded">The excluded stacks in stack-index order.</param>
    internal DepositPlan(OrganizePlan plan, PlayerInventorySnapshot player, ExcludedStack[] excluded)
    {
        Plan = plan;
        Player = player;
        Excluded = new ReadOnlyCollection<ExcludedStack>(excluded);
        excludedByIndex = new Dictionary<int, ExcludedStack>(excluded.Length);
        for (var i = 0; i < excluded.Length; i++)
        {
            excludedByIndex[excluded[i].StackIndex] = excluded[i];
        }
    }

    /// <summary>Gets the move plan to hand to the executor. Every move's Source is the inventory; Layouts[0] is the inventory's layout.</summary>
    public OrganizePlan Plan { get; }

    /// <summary>Gets the inventory the plan was made from, the same instance as passed in.</summary>
    public PlayerInventorySnapshot Player { get; }

    /// <summary>Gets the stacks the Deposit keeps where they are, in stack-index order, read-only.</summary>
    public IReadOnlyList<ExcludedStack> Excluded { get; }

    /// <summary>Gets the number of inventory stacks the Deposit may move: the stack count minus the excluded count.</summary>
    public int DepositableStacks => Player.Stacks.Count - Excluded.Count;

    /// <summary>Returns the exclusion entry of an inventory stack.</summary>
    /// <param name="stackIndex">The index of the stack in the snapshot's Stacks.</param>
    /// <returns>The entry, or null when the stack is depositable or the index is unknown.</returns>
    public ExcludedStack? FindExcluded(int stackIndex) => excludedByIndex.TryGetValue(stackIndex, out var entry) ? entry : null;
}
