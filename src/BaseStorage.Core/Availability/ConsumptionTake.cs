namespace BaseStorage.Core.Availability;

using System;
using BaseStorage.Core.Snapshots;

/// <summary>One removal of <see cref="Count"/> items from one stack of one container, paid toward one requirement (ADR-GWIN-028).</summary>
/// <remarks>
/// Equality compares all five fields, with the stack compared by <see cref="ItemStackSnapshot"/> equality. The type is immutable.
/// </remarks>
public sealed class ConsumptionTake : IEquatable<ConsumptionTake>
{
    /// <summary>Creates a take.</summary>
    /// <param name="containerId">The container to take from, which is the player inventory or a chest; must not be none.</param>
    /// <param name="stackIndex">The index of the stack in that container's contents; at least 0.</param>
    /// <param name="stack">The stack as it was in the snapshot; must not be null.</param>
    /// <param name="count">The number of items to take; from 1 to the stack's count.</param>
    /// <param name="requirementIndex">The index of the requirement this take pays toward in the plan's input; at least 0.</param>
    /// <exception cref="ArgumentException"><paramref name="containerId"/> is none.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="stack"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="stackIndex"/> or <paramref name="requirementIndex"/> is negative, or <paramref name="count"/> is outside 1 to the stack's count.
    /// </exception>
    public ConsumptionTake(ContainerId containerId, int stackIndex, ItemStackSnapshot stack, int count, int requirementIndex)
    {
        if (containerId.IsNone)
        {
            throw new ArgumentException("Must not be none.", nameof(containerId));
        }

        if (stackIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stackIndex), "Must be at least 0.");
        }

        if (stack is null)
        {
            throw new ArgumentNullException(nameof(stack));
        }

        if (count < 1 || count > stack.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Must be from 1 to the stack's count.");
        }

        if (requirementIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requirementIndex), "Must be at least 0.");
        }

        ContainerId = containerId;
        StackIndex = stackIndex;
        Stack = stack;
        Count = count;
        RequirementIndex = requirementIndex;
    }

    /// <summary>Gets the container to take from.</summary>
    public ContainerId ContainerId { get; }

    /// <summary>Gets the index of the stack in the container's contents.</summary>
    public int StackIndex { get; }

    /// <summary>Gets the stack as it was in the snapshot, which is the executor's expected stack.</summary>
    public ItemStackSnapshot Stack { get; }

    /// <summary>Gets the number of items to take from the stack.</summary>
    public int Count { get; }

    /// <summary>Gets the index of the requirement this take pays toward.</summary>
    public int RequirementIndex { get; }

    /// <summary>Determines whether two takes are equal.</summary>
    /// <param name="left">The first take, or null.</param>
    /// <param name="right">The second take, or null.</param>
    /// <returns>True when both are null or both are equal.</returns>
    public static bool operator ==(ConsumptionTake? left, ConsumptionTake? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two takes differ.</summary>
    /// <param name="left">The first take, or null.</param>
    /// <param name="right">The second take, or null.</param>
    /// <returns>True when they are not equal.</returns>
    public static bool operator !=(ConsumptionTake? left, ConsumptionTake? right) => !(left == right);

    /// <summary>Determines whether all five fields are equal.</summary>
    /// <param name="other">The take to compare with, or null.</param>
    /// <returns>True when equal; false for null.</returns>
    public bool Equals(ConsumptionTake? other) =>
        other is not null
        && ContainerId == other.ContainerId
        && StackIndex == other.StackIndex
        && Stack == other.Stack
        && Count == other.Count
        && RequirementIndex == other.RequirementIndex;

    /// <summary>Determines whether the object is an equal take.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as ConsumptionTake);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = ContainerId.GetHashCode();
            h = (h * 31) + StackIndex;
            h = (h * 31) + Stack.GetHashCode();
            h = (h * 31) + Count;
            return (h * 31) + RequirementIndex;
        }
    }
}
