namespace BaseStorage.Core.Tests.Reservations;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Reservations;
using static BaseStorage.Core.Tests.Reservations.ReservationFixtures;

/// <summary>
/// AC-2: the whole-reservation deadline (contract 0.3, 8.3) on a manual clock. No test waits: time moves only through ManualClock,
/// so every case runs in microseconds (spec NFR).
/// </summary>
public sealed class ReservationTimeoutTests
{
    // GW-U-162 (AC-2, S6): each chest has its own deadline.
    [Fact]
    public void PerChestDeadlinesAreIndependent()
    {
        var clock = NewClock();
        var a = new Reservation<OrganizeRun>(ChestA, Organize(), clock, TimeoutMs);
        clock.Set(At(1000));
        var b = new Reservation<OrganizeRun>(ChestB, Organize(), clock, TimeoutMs);

        clock.Set(At(1500));
        Assert.True(a.CheckTimeout());
        Assert.False(b.CheckTimeout());
        Assert.Equal(ReservationState.Requesting, b.State);

        clock.Set(At(2500));
        Assert.True(b.CheckTimeout());
        Assert.Equal(ReservationFailure.Timeout, b.Failure);
    }

    // GW-U-162 (AC-2, S6): a Deny on one chest never touches the other chest's state, failure or failed-from.
    [Fact]
    public void DenyOnOneChestNeverChangesTheOther()
    {
        var clock = NewClock();
        var a = new Reservation<OrganizeRun>(ChestA, Organize(), clock, TimeoutMs);
        var b = new Reservation<OrganizeRun>(ChestB, Organize(), clock, TimeoutMs);
        var before = (b.State, b.Failure, b.FailedFrom);

        Assert.True(a.Apply(ReservationEvent.Deny));

        Assert.Equal(before, (b.State, b.Failure, b.FailedFrom));
    }

    // GW-U-163 (AC-2, 8.3): the deadline is exact. At 1499 ms the result is false; at 1500 ms it is true, and it is then final.
    [Fact]
    public void TimeoutFiresExactlyAtTheDeadlineOnAFakeClock()
    {
        var clock = NewClock();
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), clock, TimeoutMs);

        clock.Advance(TimeSpan.FromMilliseconds(1499));
        Assert.False(r.CheckTimeout());
        Assert.Equal(ReservationState.Requesting, r.State);

        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(r.CheckTimeout());
        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.Timeout, r.Failure);
        Assert.Equal(ReservationState.Requesting, r.FailedFrom);
        Assert.False(r.HoldsChest);

        Assert.False(r.CheckTimeout());
        Assert.Equal(Start, r.StartedAt);
        Assert.Equal(TimeSpan.FromMilliseconds(1500), r.Timeout);
    }

    // GW-U-163 (AC-2): with reserveTimeoutMs = 1, one tick short of 1 ms is not expired and 1 ms exactly is.
    [Fact]
    public void OneMillisecondTimeoutFiresAtOneMillisecondNotBefore()
    {
        var clock = NewClock();
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), clock, 1);

        clock.Advance(TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond - 1));
        Assert.False(r.CheckTimeout());

        clock.Advance(TimeSpan.FromTicks(1));
        Assert.True(r.CheckTimeout());
    }

    // GW-U-164 (AC-2, 0.3): the deadline never moves. Accepted transitions at intermediate times do not restart it.
    [Theory]
    [MemberData(nameof(HoldingStates))]
    public void AcceptedTransitionsNeverRestartTheDeadline(ReservationState target)
    {
        var clock = NewClock();
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), clock, TimeoutMs);
        foreach ((int ms, ReservationEvent trigger) in Timeline(target))
        {
            clock.Set(At(ms));
            Assert.True(r.Apply(trigger));
        }

        Assert.Equal(target, r.State);

        clock.Set(At(1499));
        Assert.False(r.CheckTimeout());
        Assert.Equal(target, r.State);

        clock.Set(At(1500));
        Assert.True(r.CheckTimeout());
        Assert.Equal(ReservationFailure.Timeout, r.Failure);
        Assert.Equal(target, r.FailedFrom);
    }

    // GW-U-164 (AC-2, 0.3, fail-closed): a clock reading before the start counts as expired.
    [Theory]
    [MemberData(nameof(HoldingStates))]
    public void ClockGoingBackwardsFailsClosedFromEveryHoldingState(ReservationState target)
    {
        var clock = NewClock();
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), clock, TimeoutMs);
        foreach ((int ms, ReservationEvent trigger) in Timeline(target))
        {
            clock.Set(At(ms));
            Assert.True(r.Apply(trigger));
        }

        clock.Set(TimeSpan.FromSeconds(3599));

        Assert.True(r.CheckTimeout());
        Assert.Equal(ReservationFailure.Timeout, r.Failure);
        Assert.Equal(target, r.FailedFrom);
    }

    // GW-U-164 (AC-2, NFR): extreme clock readings never throw, because the deadline uses tick arithmetic that cannot overflow.
    [Fact]
    public void ExtremeClockReadingsNeverThrow()
    {
        var high = new ManualClock(TimeSpan.MaxValue);
        var highReservation = new Reservation<OrganizeRun>(ChestA, Organize(), high, TimeoutMs);
        high.Set(TimeSpan.MinValue);
        Assert.True(highReservation.CheckTimeout());

        var low = new ManualClock(TimeSpan.MinValue);
        var lowReservation = new Reservation<OrganizeRun>(ChestA, Organize(), low, TimeoutMs);
        Assert.False(lowReservation.CheckTimeout());
        low.Set(TimeSpan.MaxValue);
        Assert.True(lowReservation.CheckTimeout());
    }

    // GW-U-165 (AC-2, S3): the constructor reads the clock once and Apply never reads it, legal or illegal.
    [Fact]
    public void ApplyNeverReadsTheClock()
    {
        var clock = NewClock();
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), clock, TimeoutMs);
        Assert.Equal(1, clock.Reads);

        ReservationEvent[] events = DefinedEvents();
        for (int i = 0; i < 20; i++)
        {
            _ = r.Apply(events[i % events.Length]);
        }

        Assert.Equal(1, clock.Reads);
    }

    // GW-U-165 (AC-2, S3): CheckTimeout on a non-terminal reservation reads the clock exactly once.
    [Fact]
    public void CheckTimeoutOnANonTerminalReservationReadsOnce()
    {
        var clock = NewClock();
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), clock, TimeoutMs);
        int before = clock.Reads;

        Assert.False(r.CheckTimeout());

        Assert.Equal(before + 1, clock.Reads);
    }

    // GW-U-165 (AC-2, S3): CheckTimeout on a terminal reservation reads nothing, even after the deadline.
    [Fact]
    public void CheckTimeoutOnATerminalReservationReadsNothing()
    {
        var clock = NewClock();
        var r = Drive(ReservationState.Failed, clock);
        int before = clock.Reads;
        clock.Set(At(5000));

        Assert.False(r.CheckTimeout());

        Assert.Equal(before, clock.Reads);
    }

    // GW-U-165 (AC-2, S3): the registry constructor reads nothing, and CheckTimeouts reads once per active reservation.
    [Fact]
    public void RegistryReadsTheClockOnlyWhereTheContractSays()
    {
        var clock = NewClock();
        var book = new ChestReservations<OrganizeRun>(clock, TimeoutMs);
        Assert.Equal(0, clock.Reads);

        book.Begin(ChestA, Organize());
        book.Begin(ChestB, Organize());
        int before = clock.Reads;

        Assert.Empty(book.CheckTimeouts());

        Assert.Equal(before + 2, clock.Reads);
    }

    // GW-U-165 (AC-2, S5): an Apply after the deadline is evaluated by the table; the next CheckTimeout then fails it.
    [Fact]
    public void ApplyAfterTheDeadlineIsEvaluatedByTheTableUntilCheckTimeoutRuns()
    {
        var clock = NewClock();
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), clock, TimeoutMs);

        clock.Set(At(2000));
        Assert.True(r.Apply(ReservationEvent.Grant));
        Assert.Equal(ReservationState.Granted, r.State);

        Assert.True(r.CheckTimeout());
        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.Timeout, r.Failure);
        Assert.Equal(ReservationState.Granted, r.FailedFrom);
    }

    /// <summary>The three states that hold the chest, as theory data.</summary>
    public static IEnumerable<object[]> HoldingStates() => new[]
    {
        new object[] { ReservationState.Granted },
        new object[] { ReservationState.Writing },
        new object[] { ReservationState.Releasing },
    };

    /// <summary>The events that reach each holding state, with the millisecond offset of each (contract 8.3 and 0.3 examples).</summary>
    private static (int Ms, ReservationEvent Trigger)[] Timeline(ReservationState target) => target switch
    {
        ReservationState.Granted => new[] { (1000, ReservationEvent.Grant) },
        ReservationState.Writing => new[] { (1000, ReservationEvent.Grant), (1200, ReservationEvent.BeginWrite) },
        ReservationState.Releasing => new[]
        {
            (1000, ReservationEvent.Grant),
            (1200, ReservationEvent.BeginWrite),
            (1300, ReservationEvent.BeginRelease),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(target)),
    };
}
