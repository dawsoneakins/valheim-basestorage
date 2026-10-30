namespace BaseStorage.Core.Tests.Reservations;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Reservations.ReservationFixtures;

/// <summary>
/// AC-4: the machine behaves the same for every operation type (a class, a struct and an interface), never inspects the operation,
/// and the registry is generic in the same way. Expected values are the literals of contract 8.7 and 8.8.
/// </summary>
public sealed class ReservationGenericTests
{
    private static readonly StationFeed Feed = new StationFeed(new StationId(3141592653L, 17u), "CopperOre");

    // The five scripts of contract 8.8, with their literal final rows.
    private static readonly (Step[] Steps, ReservationState State, ReservationFailure Failure, ReservationState? FailedFrom)[] EightEightScripts =
    {
        (
            new[] { Steps.Event(ReservationEvent.Grant), Steps.Event(ReservationEvent.BeginWrite), Steps.Event(ReservationEvent.BeginRelease), Steps.Event(ReservationEvent.CompleteRelease) },
            ReservationState.Completed,
            ReservationFailure.None,
            null),
        (
            new[] { Steps.Event(ReservationEvent.Deny) },
            ReservationState.Failed,
            ReservationFailure.Denied,
            ReservationState.Requesting),
        (
            new[] { Steps.Event(ReservationEvent.Grant), Steps.Event(ReservationEvent.BeginWrite), Steps.Event(ReservationEvent.InUseByOther) },
            ReservationState.Failed,
            ReservationFailure.InUseByOther,
            ReservationState.Writing),
        (
            new[] { Steps.Event(ReservationEvent.Grant), Steps.Advance(TimeoutMs), Steps.Check() },
            ReservationState.Failed,
            ReservationFailure.Timeout,
            ReservationState.Granted),
        (
            new[] { Steps.Event(ReservationEvent.BeginWrite) },
            ReservationState.Requesting,
            ReservationFailure.None,
            null),
    };

    // The 8.1 script (grant path with a later timeout check) and the 8.3 script (exact deadline), for the hostile operation.
    private static readonly Step[] GrantPathScript =
    {
        Steps.Event(ReservationEvent.Grant),
        Steps.Event(ReservationEvent.BeginWrite),
        Steps.Event(ReservationEvent.BeginRelease),
        Steps.Event(ReservationEvent.CompleteRelease),
        Steps.Advance(10000),
        Steps.Check(),
        Steps.Event(ReservationEvent.InUseByOther),
    };

    private static readonly Step[] ExactDeadlineScript =
    {
        Steps.Advance(1499),
        Steps.Check(),
        Steps.Advance(1),
        Steps.Check(),
        Steps.Check(),
    };

    // The literal trace of contract 8.7 (two independent chests), one line per step. Chest keys are "userId/id".
    private static readonly string[] RegistryLiterals =
    {
        "1 begin=+,+ active=3141592653/42,3141592653/57",
        "2 begin=- active=3141592653/42,3141592653/57",
        "3 apply=true active=3141592653/42,3141592653/57",
        "4 apply=true active=3141592653/42",
        "5 apply=false active=3141592653/42",
        "6 timeouts=3141592653/42 active=",
        "7 removed=2 find=-,-",
        "8 begin=+ started=36015000000 active=3141592653/57",
        "9 applyToAll=1 active=",
    };

    // GW-U-176 (AC-4, 8.8): the five scripts give one trace and one final row for a class, a struct and an interface operation.
    [Theory]
    [MemberData(nameof(ScriptIndexes))]
    public void ClassStructAndInterfaceOperationsGiveTheSameTraceAndFinalRow(int index)
    {
        var (steps, state, failure, failedFrom) = EightEightScripts[index];
        OrganizeRun organize = Organize();
        ISearchRequest search = new SearchRequest("copper ore");

        var (organizeTrace, organizeReservation) = Replay.Run(organize, NewClock(), steps);
        var (feedTrace, feedReservation) = Replay.Run(Feed, NewClock(), steps);
        var (searchTrace, searchReservation) = Replay.Run<ISearchRequest>(search, NewClock(), steps);

        Assert.Equal(organizeTrace, feedTrace);
        Assert.Equal(organizeTrace, searchTrace);

        Snap last = organizeTrace[^1];
        Assert.Equal((state, failure, failedFrom), (last.State, last.Failure, last.FailedFrom));

        Assert.Same(organize, organizeReservation.Operation);
        Assert.Same(search, searchReservation.Operation);
        Assert.Equal(Feed, feedReservation.Operation);
    }

    // GW-U-177 (AC-4, 8.7 last line): the registry script gives the contract literals for a class and for a struct operation.
    [Fact]
    public void RegistryScriptGivesTheContractLiteralsForClassAndStructOperations()
    {
        Assert.Equal(RegistryLiterals, RegistryTrace(Organize()));
        Assert.Equal(RegistryLiterals, RegistryTrace(Feed));
    }

    // GW-U-178 (AC-4, S2): an operation whose Equals, GetHashCode and ToString all throw is never inspected.
    [Fact]
    public void HostileOperationIsNeverInspected()
    {
        var hostile = new HostileOperation();

        var (hostileTrace, hostileReservation) = Replay.Run(hostile, NewClock(), GrantPathScript);
        var (organizeTrace, _) = Replay.Run(Organize(), NewClock(), GrantPathScript);
        Assert.Equal(organizeTrace, hostileTrace);
        Assert.Same(hostile, hostileReservation.Operation);

        var (hostileExact, _) = Replay.Run(hostile, NewClock(), ExactDeadlineScript);
        var (organizeExact, _) = Replay.Run(Organize(), NewClock(), ExactDeadlineScript);
        Assert.Equal(organizeExact, hostileExact);

        Assert.Equal(RegistryLiterals, RegistryTrace(hostile));
    }

    // GW-U-179 (AC-4): for every one of the 54 pairs, the outcome is identical for a class, a struct and an interface operation.
    [Theory]
    [MemberData(nameof(AllPairs))]
    public void EveryPairGivesTheSameOutcomeForEveryOperationKind(ReservationState from, ReservationEvent trigger)
    {
        var organize = PairOutcome(Organize(), from, trigger);
        var feed = PairOutcome(Feed, from, trigger);
        var search = PairOutcome<ISearchRequest>(new SearchRequest("copper ore"), from, trigger);

        Assert.Equal(organize, feed);
        Assert.Equal(organize, search);
    }

    /// <summary>Indices 0 to 4 of the 8.8 scripts, as theory data.</summary>
    public static IEnumerable<object[]> ScriptIndexes() => Enumerable.Range(0, EightEightScripts.Length).Select(i => new object[] { i });

    /// <summary>The 54 defined (state, event) pairs as theory data.</summary>
    public static IEnumerable<object[]> AllPairs() => ReservationFixtures.AllPairs();

    private static (bool Returned, ReservationState State, ReservationFailure Failure, ReservationState? FailedFrom) PairOutcome<T>(
        T operation,
        ReservationState from,
        ReservationEvent trigger)
        where T : notnull
    {
        Reservation<T> reservation = Drive(from, operation, NewClock());
        bool returned = reservation.Apply(trigger);
        return (returned, reservation.State, reservation.Failure, reservation.FailedFrom);
    }

    /// <summary>Runs the contract 8.7 script on one registry and writes one line per step.</summary>
    private static List<string> RegistryTrace<T>(T operation)
        where T : notnull
    {
        ManualClock clock = NewClock();
        var book = new ChestReservations<T>(clock, TimeoutMs);
        var trace = new List<string>();

        Reservation<T>? a = book.Begin(ChestA, operation);
        Reservation<T>? b = book.Begin(ChestB, operation);
        trace.Add("1 begin=" + Mark(a) + "," + Mark(b) + " active=" + Chests(book.Active));

        trace.Add("2 begin=" + Mark(book.Begin(ChestA, operation)) + " active=" + Chests(book.Active));
        trace.Add("3 apply=" + Flag(book.Apply(ChestA, ReservationEvent.Grant)) + " active=" + Chests(book.Active));
        trace.Add("4 apply=" + Flag(book.Apply(ChestB, ReservationEvent.Deny)) + " active=" + Chests(book.Active));
        trace.Add("5 apply=" + Flag(book.Apply(ChestUnknown, ReservationEvent.Grant)) + " active=" + Chests(book.Active));

        clock.Advance(TimeSpan.FromMilliseconds(TimeoutMs));
        IReadOnlyList<Reservation<T>> expired = book.CheckTimeouts();
        trace.Add("6 timeouts=" + Chests(expired) + " active=" + Chests(book.Active));

        int removed = book.RemoveTerminal();
        trace.Add("7 removed=" + removed.ToString(CultureInfo.InvariantCulture)
            + " find=" + Mark(book.Find(ChestA)) + "," + Mark(book.Find(ChestB)));

        Reservation<T>? again = book.Begin(ChestB, operation);
        string started = again is null ? "-" : again.StartedAt.Ticks.ToString(CultureInfo.InvariantCulture);
        trace.Add("8 begin=" + Mark(again) + " started=" + started + " active=" + Chests(book.Active));

        int notified = book.ApplyToAll(ReservationEvent.RequesterDisconnected);
        trace.Add("9 applyToAll=" + notified.ToString(CultureInfo.InvariantCulture) + " active=" + Chests(book.Active));

        return trace;
    }

    private static string Mark<T>(Reservation<T>? reservation)
        where T : notnull => reservation is null ? "-" : "+";

    private static string Flag(bool value) => value ? "true" : "false";

    private static string Chests<T>(IEnumerable<Reservation<T>> reservations)
        where T : notnull =>
        string.Join(",", reservations.Select(r => r.Chest.UserId.ToString(CultureInfo.InvariantCulture) + "/" + r.Chest.Id.ToString(CultureInfo.InvariantCulture)));
}
