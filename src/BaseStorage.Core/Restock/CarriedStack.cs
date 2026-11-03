namespace BaseStorage.Core.Restock;

using System;
using BaseStorage.Core.Snapshots;

/// <summary>One stack the player carries, with the marks capture found for it (S-QR-001, contract 3.2).</summary>
public sealed class CarriedStack : IEquatable<CarriedStack>
{
    private readonly ItemStackSnapshot stack;
    private readonly CarriedMarks marks;

    /// <summary>Creates a carried stack.</summary>
    /// <param name="stack">The carried stack; not null. An over-full stack is representable.</param>
    /// <param name="marks">The marks; only bits 0 to 7 are defined.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stack"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="marks"/> has a bit outside 0 to 7.</exception>
    public CarriedStack(ItemStackSnapshot stack, CarriedMarks marks)
    {
        if (stack is null)
        {
            throw new ArgumentNullException(nameof(stack));
        }

        if (((int)marks & ~7) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(marks), "Only bits 0 to 7 are defined.");
        }

        this.stack = stack;
        this.marks = marks;
    }

    /// <summary>Gets the carried stack.</summary>
    public ItemStackSnapshot Stack => stack;

    /// <summary>Gets the marks capture found for the stack.</summary>
    public CarriedMarks Marks => marks;

    /// <summary>Determines whether two carried stacks are equal.</summary>
    /// <param name="left">The first carried stack; may be null.</param>
    /// <param name="right">The second carried stack; may be null.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(CarriedStack? left, CarriedStack? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two carried stacks differ.</summary>
    /// <param name="left">The first carried stack; may be null.</param>
    /// <param name="right">The second carried stack; may be null.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(CarriedStack? left, CarriedStack? right) => !(left == right);

    /// <summary>Determines whether the stack and the marks are equal.</summary>
    /// <param name="other">The carried stack to compare with; may be null.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(CarriedStack? other) => other is not null && stack.Equals(other.stack) && marks == other.marks;

    /// <summary>Determines whether the object is an equal carried stack.</summary>
    /// <param name="obj">The object to compare with; may be null.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as CarriedStack);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return (stack.GetHashCode() * 31) + (int)marks;
        }
    }
}
