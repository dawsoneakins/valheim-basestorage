namespace BaseStorage.Core.Organize.Deposit;

/// <summary>One player-inventory stack a Deposit keeps where it is, with every reason that applies (deposit contract 4.2). Immutable.</summary>
public sealed class ExcludedStack
{
    /// <summary>Creates an entry. Only the planner creates entries.</summary>
    /// <param name="stackIndex">The index of the stack in the snapshot's Stacks.</param>
    /// <param name="source">The stack.</param>
    /// <param name="reasons">The reasons, never None.</param>
    internal ExcludedStack(int stackIndex, PlayerInventoryStack source, DepositExclusion reasons)
    {
        StackIndex = stackIndex;
        Source = source;
        Reasons = reasons;
    }

    /// <summary>Gets the index of the stack in the snapshot's Stacks, which is its stack index in the inventory container.</summary>
    public int StackIndex { get; }

    /// <summary>Gets the stack, the same instance as in the snapshot.</summary>
    public PlayerInventoryStack Source { get; }

    /// <summary>Gets every reason that applies; never None.</summary>
    public DepositExclusion Reasons { get; }
}
