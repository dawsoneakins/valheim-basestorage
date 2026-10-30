namespace BaseStorage.Core.Reservations;

using System;

/// <summary>
/// One row of the reservation transition table: a from state, a trigger event, the state it leads to and the failure recorded
/// on entry. This is an immutable value. It does not check that the row is in the table; ReservationTransitions decides legality.
/// </summary>
public readonly struct ReservationTransition : IEquatable<ReservationTransition>
{
    /// <summary>Creates a transition row.</summary>
    /// <param name="from">The state the row leaves.</param>
    /// <param name="trigger">The event that fires the row.</param>
    /// <param name="to">The state the row enters.</param>
    /// <param name="failure">The failure recorded on entry. It is None unless to is Failed.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when from, trigger, to or failure is not a defined value. ParamName names that argument.</exception>
    /// <exception cref="ArgumentException">Thrown when to is Failed and failure is None, or when to is not Failed and failure is not None. ParamName is "failure".</exception>
    public ReservationTransition(ReservationState from, ReservationEvent trigger, ReservationState to, ReservationFailure failure)
    {
        if (!ReservationTransitions.IsDefined(from))
        {
            throw new ArgumentOutOfRangeException(nameof(from));
        }

        if (!ReservationTransitions.IsDefined(trigger))
        {
            throw new ArgumentOutOfRangeException(nameof(trigger));
        }

        if (!ReservationTransitions.IsDefined(to))
        {
            throw new ArgumentOutOfRangeException(nameof(to));
        }

        if (!ReservationTransitions.IsDefined(failure))
        {
            throw new ArgumentOutOfRangeException(nameof(failure));
        }

        if ((to == ReservationState.Failed) == (failure == ReservationFailure.None))
        {
            throw new ArgumentException("Failure must be set exactly when the target state is Failed.", nameof(failure));
        }

        From = from;
        Trigger = trigger;
        To = to;
        Failure = failure;
    }

    /// <summary>Gets the state the row leaves.</summary>
    public ReservationState From { get; }

    /// <summary>Gets the event that fires the row.</summary>
    public ReservationEvent Trigger { get; }

    /// <summary>Gets the state the row enters.</summary>
    public ReservationState To { get; }

    /// <summary>Gets the failure recorded on entry. None unless To is Failed.</summary>
    public ReservationFailure Failure { get; }

    /// <summary>Determines whether all four members are equal to those of another row.</summary>
    /// <param name="other">The row to compare with.</param>
    /// <returns>True when all four members are equal; otherwise false.</returns>
    public bool Equals(ReservationTransition other) =>
        From == other.From && Trigger == other.Trigger && To == other.To && Failure == other.Failure;

    /// <summary>Determines whether the object is a row with the same four members.</summary>
    /// <param name="obj">The object to compare with. A null or other type gives false.</param>
    /// <returns>True when obj is an equal row; otherwise false.</returns>
    public override bool Equals(object? obj) => obj is ReservationTransition other && Equals(other);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => ((((((int)From * 31) + (int)Trigger) * 31) + (int)To) * 31) + (int)Failure;

    /// <summary>Determines whether two rows are equal.</summary>
    /// <param name="left">The first row.</param>
    /// <param name="right">The second row.</param>
    /// <returns>True when equal; otherwise false.</returns>
    public static bool operator ==(ReservationTransition left, ReservationTransition right) => left.Equals(right);

    /// <summary>Determines whether two rows differ.</summary>
    /// <param name="left">The first row.</param>
    /// <param name="right">The second row.</param>
    /// <returns>True when different; otherwise false.</returns>
    public static bool operator !=(ReservationTransition left, ReservationTransition right) => !left.Equals(right);
}
