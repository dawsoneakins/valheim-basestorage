namespace BaseStorage.Core.Reservations;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The reservation of one chest for one operation (contract S-E005-001 section 4). It is a state machine whose state changes only
/// through Apply and CheckTimeout. It has reference equality. It is not thread-safe; use it from one thread.
/// </summary>
/// <typeparam name="TOperation">The operation the reservation serves. It is carried unchanged and never inspected.</typeparam>
public sealed class Reservation<TOperation>
    where TOperation : notnull
{
    private readonly ContainerId chest;
    private readonly TOperation operation;
    private readonly IClock clock;
    private readonly TimeSpan timeout;
    private readonly TimeSpan startedAt;
    private ReservationState state;
    private ReservationFailure failure;
    private ReservationState? failedFrom;

    /// <summary>Starts a reservation in Requesting. Reads the clock once, which fixes the deadline.</summary>
    /// <param name="chest">The chest to reserve. It must not be the none id.</param>
    /// <param name="operation">The operation the reservation serves. It must not be null.</param>
    /// <param name="clock">The only time source. It must not be null.</param>
    /// <param name="reserveTimeoutMs">The timeout in milliseconds. It must be at least 1.</param>
    /// <exception cref="ArgumentException">Thrown when chest is the none id. ParamName is "chest".</exception>
    /// <exception cref="ArgumentNullException">Thrown when operation or clock is null. ParamName names that argument.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when reserveTimeoutMs is less than 1. ParamName is "reserveTimeoutMs".</exception>
    public Reservation(ContainerId chest, TOperation operation, IClock clock, int reserveTimeoutMs)
    {
        if (chest.IsNone)
        {
            throw new ArgumentException("The chest must not be the none id.", nameof(chest));
        }

        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (clock is null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (reserveTimeoutMs < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(reserveTimeoutMs));
        }

        this.chest = chest;
        this.operation = operation;
        this.clock = clock;
        timeout = TimeSpan.FromMilliseconds(reserveTimeoutMs);
        startedAt = clock.Elapsed;
        state = ReservationState.Requesting;
    }

    /// <summary>Gets the chest this reservation is about.</summary>
    public ContainerId Chest => chest;

    /// <summary>Gets the operation the reservation serves, as it was passed in.</summary>
    public TOperation Operation => operation;

    /// <summary>Gets the current state.</summary>
    public ReservationState State => state;

    /// <summary>Gets the failure. It is None unless the state is Failed.</summary>
    public ReservationFailure Failure => failure;

    /// <summary>Gets the state the reservation failed from. It is null unless the state is Failed.</summary>
    public ReservationState? FailedFrom => failedFrom;

    /// <summary>Gets the clock reading taken in the constructor. It is the start of the deadline.</summary>
    public TimeSpan StartedAt => startedAt;

    /// <summary>Gets the timeout, which is TimeSpan.FromMilliseconds(reserveTimeoutMs).</summary>
    public TimeSpan Timeout => timeout;

    /// <summary>Gets a value indicating whether the state is terminal (Completed or Failed).</summary>
    public bool IsTerminal => ReservationTransitions.IsTerminal(state);

    /// <summary>Gets a value indicating whether the state holds the chest (Granted, Writing or Releasing).</summary>
    public bool HoldsChest => ReservationTransitions.HoldsChest(state);

    /// <summary>
    /// Applies an event by looking it up in the transition table. The clock is not read. An illegal event, a terminal state or an
    /// undefined value changes nothing. Never throws.
    /// </summary>
    /// <param name="trigger">The event to apply.</param>
    /// <returns>True when the transition was accepted; otherwise false, with nothing changed.</returns>
    public bool Apply(ReservationEvent trigger)
    {
        if (!ReservationTransitions.TryGet(state, trigger, out ReservationTransition row))
        {
            return false;
        }

        if (row.To == ReservationState.Failed)
        {
            failedFrom = state;
        }

        state = row.To;
        failure = row.Failure;
        return true;
    }

    /// <summary>
    /// Fails a non-terminal reservation with Timeout once the deadline has passed, or when the clock has gone backwards. It reads the
    /// clock once when the state is non-terminal, and never throws.
    /// </summary>
    /// <returns>True when this call moved the reservation to Failed; otherwise false.</returns>
    public bool CheckTimeout()
    {
        if (IsTerminal)
        {
            return false;
        }

        TimeSpan now = clock.Elapsed;
        if (now < startedAt || unchecked((ulong)now.Ticks - (ulong)startedAt.Ticks) >= (ulong)timeout.Ticks)
        {
            failedFrom = state;
            state = ReservationState.Failed;
            failure = ReservationFailure.Timeout;
            return true;
        }

        return false;
    }
}
