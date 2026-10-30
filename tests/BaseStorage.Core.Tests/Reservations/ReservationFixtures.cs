namespace BaseStorage.Core.Tests.Reservations;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;

/// <summary>One row of contract 3.1, copied literally from the contract. Never read from Core (ADR-GWIN-023).</summary>
internal readonly record struct TableRow(ReservationState From, ReservationEvent Trigger, ReservationState To, ReservationFailure Failure);

/// <summary>Shared fixtures of the reservation tests: chests, timeout, start time, the table copy, Drive and the script replay.</summary>
internal static class ReservationFixtures
{
    /// <summary>The chest "a" of contract section 8 (ContainerId 3141592653, 42).</summary>
    public static readonly ContainerId ChestA = new ContainerId(3141592653L, 42u);

    /// <summary>The chest "b" of contract section 8 (ContainerId 3141592653, 57).</summary>
    public static readonly ContainerId ChestB = new ContainerId(3141592653L, 57u);

    /// <summary>A chest that no registry in the tests holds (ContainerId 3141592653, 99).</summary>
    public static readonly ContainerId ChestUnknown = new ContainerId(3141592653L, 99u);

    /// <summary>The ReserveTimeoutMs default.</summary>
    public const int TimeoutMs = 1500;

    /// <summary>The clock reading at which the tests start, 3600 seconds.</summary>
    public static readonly TimeSpan Start = TimeSpan.FromSeconds(3600);

    /// <summary>A new OrganizeRun for the default tests.</summary>
    /// <returns>A new operation instance.</returns>
    public static OrganizeRun Organize() => new OrganizeRun("Meadows longhouse");

    /// <summary>A new ManualClock at the default start time.</summary>
    /// <returns>A new clock.</returns>
    public static ManualClock NewClock() => new ManualClock(Start);

    /// <summary>An absolute clock reading the given number of milliseconds after Start. At(1500) is the default deadline.</summary>
    /// <param name="milliseconds">The offset from Start.</param>
    /// <returns>The clock reading.</returns>
    public static TimeSpan At(int milliseconds) => Start + TimeSpan.FromMilliseconds(milliseconds);

    /// <summary>
    /// The 19 rows of contract 3.1, in contracted order. The values are typed in by hand from the contract, so an unreviewed
    /// change to Core's table fails the comparison in the tests.
    /// </summary>
    public static readonly TableRow[] TableRows =
    {
        new(ReservationState.Requesting, ReservationEvent.Grant, ReservationState.Granted, ReservationFailure.None),
        new(ReservationState.Requesting, ReservationEvent.Deny, ReservationState.Failed, ReservationFailure.Denied),
        new(ReservationState.Requesting, ReservationEvent.Cancel, ReservationState.Failed, ReservationFailure.Cancelled),
        new(ReservationState.Requesting, ReservationEvent.RequesterDisconnected, ReservationState.Failed, ReservationFailure.RequesterDisconnected),
        new(ReservationState.Requesting, ReservationEvent.OwnerLost, ReservationState.Failed, ReservationFailure.OwnerLost),
        new(ReservationState.Requesting, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.InUseByOther),
        new(ReservationState.Granted, ReservationEvent.BeginWrite, ReservationState.Writing, ReservationFailure.None),
        new(ReservationState.Granted, ReservationEvent.BeginRelease, ReservationState.Releasing, ReservationFailure.None),
        new(ReservationState.Granted, ReservationEvent.RequesterDisconnected, ReservationState.Failed, ReservationFailure.RequesterDisconnected),
        new(ReservationState.Granted, ReservationEvent.OwnerLost, ReservationState.Failed, ReservationFailure.OwnerLost),
        new(ReservationState.Granted, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.InUseByOther),
        new(ReservationState.Writing, ReservationEvent.BeginRelease, ReservationState.Releasing, ReservationFailure.None),
        new(ReservationState.Writing, ReservationEvent.RequesterDisconnected, ReservationState.Failed, ReservationFailure.RequesterDisconnected),
        new(ReservationState.Writing, ReservationEvent.OwnerLost, ReservationState.Failed, ReservationFailure.OwnerLost),
        new(ReservationState.Writing, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.InUseByOther),
        new(ReservationState.Releasing, ReservationEvent.CompleteRelease, ReservationState.Completed, ReservationFailure.None),
        new(ReservationState.Releasing, ReservationEvent.RequesterDisconnected, ReservationState.Failed, ReservationFailure.RequesterDisconnected),
        new(ReservationState.Releasing, ReservationEvent.OwnerLost, ReservationState.Failed, ReservationFailure.OwnerLost),
        new(ReservationState.Releasing, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.InUseByOther),
    };

    /// <summary>Every (state, event) pair of the 6 x 9 defined values, 54 in all, in matrix order.</summary>
    /// <returns>The pairs as theory data.</returns>
    public static IEnumerable<object[]> AllPairs()
    {
        foreach (ReservationState state in DefinedStates())
        {
            foreach (ReservationEvent trigger in DefinedEvents())
            {
                yield return new object[] { state, trigger };
            }
        }
    }

    /// <summary>The 6 defined states, in contracted order.</summary>
    /// <returns>The states.</returns>
    public static ReservationState[] DefinedStates() => new[]
    {
        ReservationState.Requesting,
        ReservationState.Granted,
        ReservationState.Writing,
        ReservationState.Releasing,
        ReservationState.Completed,
        ReservationState.Failed,
    };

    /// <summary>The 9 defined events, in contracted order.</summary>
    /// <returns>The events.</returns>
    public static ReservationEvent[] DefinedEvents() => new[]
    {
        ReservationEvent.Grant,
        ReservationEvent.Deny,
        ReservationEvent.Cancel,
        ReservationEvent.BeginWrite,
        ReservationEvent.BeginRelease,
        ReservationEvent.CompleteRelease,
        ReservationEvent.RequesterDisconnected,
        ReservationEvent.OwnerLost,
        ReservationEvent.InUseByOther,
    };

    /// <summary>Finds the contract 3.1 row for a pair.</summary>
    /// <param name="from">The state the row leaves.</param>
    /// <param name="trigger">The event that fires the row.</param>
    /// <param name="row">The row when found; default otherwise.</param>
    /// <returns>True when the pair is one of the 19 legal rows.</returns>
    public static bool TryRow(ReservationState from, ReservationEvent trigger, out TableRow row)
    {
        foreach (TableRow candidate in TableRows)
        {
            if (candidate.From == from && candidate.Trigger == trigger)
            {
                row = candidate;
                return true;
            }
        }

        row = default;
        return false;
    }

    /// <summary>The fixed legal events that reach each state from a fresh reservation (ADR-GWIN-023 Drive).</summary>
    /// <param name="target">The state to reach.</param>
    /// <returns>The events, in order.</returns>
    public static ReservationEvent[] PathTo(ReservationState target) => target switch
    {
        ReservationState.Requesting => Array.Empty<ReservationEvent>(),
        ReservationState.Granted => new[] { ReservationEvent.Grant },
        ReservationState.Writing => new[] { ReservationEvent.Grant, ReservationEvent.BeginWrite },
        ReservationState.Releasing => new[] { ReservationEvent.Grant, ReservationEvent.BeginWrite, ReservationEvent.BeginRelease },
        ReservationState.Completed => new[]
        {
            ReservationEvent.Grant,
            ReservationEvent.BeginWrite,
            ReservationEvent.BeginRelease,
            ReservationEvent.CompleteRelease,
        },
        ReservationState.Failed => new[] { ReservationEvent.Deny },
        _ => throw new ArgumentOutOfRangeException(nameof(target)),
    };

    /// <summary>Creates a reservation on ChestA and drives it to a state by PathTo.</summary>
    /// <typeparam name="TOperation">The operation type.</typeparam>
    /// <param name="target">The state to reach.</param>
    /// <param name="operation">The operation the reservation serves.</param>
    /// <param name="clock">The clock the reservation reads.</param>
    /// <returns>The reservation, in target.</returns>
    public static Reservation<TOperation> Drive<TOperation>(ReservationState target, TOperation operation, ManualClock clock)
        where TOperation : notnull
    {
        var reservation = new Reservation<TOperation>(ChestA, operation, clock, TimeoutMs);
        foreach (ReservationEvent trigger in PathTo(target))
        {
            Assert.True(reservation.Apply(trigger), $"Drive: {trigger} was refused on the way to {target}.");
        }

        return reservation;
    }

    /// <summary>Creates an OrganizeRun reservation on ChestA and drives it to a state.</summary>
    /// <param name="target">The state to reach.</param>
    /// <param name="clock">The clock the reservation reads.</param>
    /// <returns>The reservation, in target.</returns>
    public static Reservation<OrganizeRun> Drive(ReservationState target, ManualClock clock) =>
        Drive(target, Organize(), clock);
}

/// <summary>Stand-in operation type 1 (contract section 8): a reference type.</summary>
internal sealed class OrganizeRun
{
    /// <summary>Creates the run.</summary>
    /// <param name="baseName">The base the run organises.</param>
    public OrganizeRun(string baseName)
    {
        BaseName = baseName;
    }

    /// <summary>Gets the base the run organises.</summary>
    public string BaseName { get; }
}

/// <summary>Stand-in operation type 2 (contract section 8): a value type over the existing StationId.</summary>
internal readonly struct StationFeed
{
    /// <summary>Creates the feed.</summary>
    /// <param name="station">The station that receives the feed.</param>
    /// <param name="prefabName">The item fed to the station.</param>
    public StationFeed(StationId station, string prefabName)
    {
        Station = station;
        PrefabName = prefabName;
    }

    /// <summary>Gets the station that receives the feed.</summary>
    public StationId Station { get; }

    /// <summary>Gets the item fed to the station.</summary>
    public string PrefabName { get; }
}

/// <summary>Stand-in operation type 3 (AC-4 "interface"): the search request contract.</summary>
internal interface ISearchRequest
{
    /// <summary>Gets the text searched for.</summary>
    string Text { get; }
}

/// <summary>The sealed implementation of ISearchRequest used by the parity tests.</summary>
internal sealed class SearchRequest : ISearchRequest
{
    /// <summary>Creates the request.</summary>
    /// <param name="text">The text searched for.</param>
    public SearchRequest(string text)
    {
        Text = text;
    }

    /// <summary>Gets the text searched for.</summary>
    public string Text { get; }
}

/// <summary>
/// A test-only operation whose Equals, GetHashCode and ToString all throw. The reservation must never inspect its operation (S2).
/// </summary>
internal sealed class HostileOperation
{
    /// <summary>Throws. The reservation must never call it.</summary>
    /// <param name="obj">Ignored.</param>
    /// <returns>Never returns.</returns>
    public override bool Equals(object? obj) => throw new InvalidOperationException("Equals must not be called.");

    /// <summary>Throws. The reservation must never call it.</summary>
    /// <returns>Never returns.</returns>
    public override int GetHashCode() => throw new InvalidOperationException("GetHashCode must not be called.");

    /// <summary>Throws. The reservation must never call it.</summary>
    /// <returns>Never returns.</returns>
    public override string ToString() => throw new InvalidOperationException("ToString must not be called.");
}

/// <summary>One scripted step: an event, a clock advance, or a CheckTimeout.</summary>
internal enum StepKind
{
    /// <summary>Apply the event.</summary>
    Event,

    /// <summary>Call CheckTimeout.</summary>
    Check,

    /// <summary>Advance the clock. The step's returned value is recorded as true.</summary>
    Advance,
}

/// <summary>One step of a scripted reservation run.</summary>
/// <param name="Kind">What the step does.</param>
/// <param name="Trigger">The event for an Event step.</param>
/// <param name="Milliseconds">The interval for an Advance step.</param>
internal readonly record struct Step(StepKind Kind, ReservationEvent Trigger, int Milliseconds);

/// <summary>Builders for the scripted steps.</summary>
internal static class Steps
{
    /// <summary>An event step.</summary>
    /// <param name="trigger">The event to apply.</param>
    /// <returns>The step.</returns>
    public static Step Event(ReservationEvent trigger) => new(StepKind.Event, trigger, 0);

    /// <summary>A CheckTimeout step.</summary>
    /// <returns>The step.</returns>
    public static Step Check() => new(StepKind.Check, default, 0);

    /// <summary>A clock advance step.</summary>
    /// <param name="milliseconds">The interval to advance by.</param>
    /// <returns>The step.</returns>
    public static Step Advance(int milliseconds) => new(StepKind.Advance, default, milliseconds);
}

/// <summary>One observation after a step: (returned, State, Failure, FailedFrom).</summary>
/// <param name="Returned">The value the step returned.</param>
/// <param name="State">The state after the step.</param>
/// <param name="Failure">The failure after the step.</param>
/// <param name="FailedFrom">The state the reservation failed from, after the step.</param>
internal readonly record struct Snap(bool Returned, ReservationState State, ReservationFailure Failure, ReservationState? FailedFrom);

/// <summary>Replays a script against a fresh Reservation of the given operation and records every step.</summary>
internal static class Replay
{
    /// <summary>Creates a reservation on ChestA with the operation and runs the steps on it.</summary>
    /// <typeparam name="TOperation">The operation type.</typeparam>
    /// <param name="operation">The operation the reservation serves.</param>
    /// <param name="clock">The clock the reservation reads.</param>
    /// <param name="steps">The script.</param>
    /// <returns>The observation after construction and after each step, and the reservation itself.</returns>
    public static (List<Snap> Trace, Reservation<TOperation> Reservation) Run<TOperation>(TOperation operation, ManualClock clock, params Step[] steps)
        where TOperation : notnull
    {
        var reservation = new Reservation<TOperation>(ReservationFixtures.ChestA, operation, clock, ReservationFixtures.TimeoutMs);
        var trace = new List<Snap> { Observe(true, reservation) };
        foreach (Step step in steps)
        {
            bool returned = step.Kind switch
            {
                StepKind.Event => reservation.Apply(step.Trigger),
                StepKind.Check => reservation.CheckTimeout(),
                _ => Advance(clock, step.Milliseconds),
            };
            trace.Add(Observe(returned, reservation));
        }

        return (trace, reservation);
    }

    private static bool Advance(ManualClock clock, int milliseconds)
    {
        clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
        return true;
    }

    private static Snap Observe<TOperation>(bool returned, Reservation<TOperation> reservation)
        where TOperation : notnull =>
        new(returned, reservation.State, reservation.Failure, reservation.FailedFrom);
}
