namespace BaseStorage.Core.BaseMove;

using BaseStorage.Core.Snapshots;

/// <summary>A source stack the portal rule keeps in its chest.</summary>
public sealed class StayedStack
{
    /// <summary>Creates the entry.</summary>
    /// <param name="source">The chest that keeps the stack.</param>
    /// <param name="stackIndex">The stack's index in the chest's snapshot contents.</param>
    /// <param name="stack">The stack.</param>
    internal StayedStack(ContainerId source, int stackIndex, ItemStackSnapshot stack)
    {
        Source = source;
        StackIndex = stackIndex;
        Stack = stack;
    }

    /// <summary>Gets the chest that keeps the stack.</summary>
    public ContainerId Source { get; }

    /// <summary>Gets the stack's index in the chest's snapshot contents.</summary>
    public int StackIndex { get; }

    /// <summary>Gets the stack.</summary>
    public ItemStackSnapshot Stack { get; }
}
