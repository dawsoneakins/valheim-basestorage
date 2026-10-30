namespace BaseStorage.Core.Tests.Reservations;

using System;
using BaseStorage.Core.Reservations;
using static BaseStorage.Core.Tests.Reservations.ReservationFixtures;

/// <summary>AC-2 (S6): the per-chest registry, the contract walkthrough of two independent chests, and the registry rules.</summary>
public sealed class ChestReservationsTests
{
    // GW-U-160 (AC-2, 8.7): the two-chest walkthrough, steps 1 to 9.
    [Fact]
    public void TwoChestsAreIndependentAsInTheContractWalkthrough()
    {
        var clock = NewClock();
        OrganizeRun organize = Organize();
        var book = new ChestReservations<OrganizeRun>(clock, TimeoutMs);

        Reservation<OrganizeRun>? a = book.Begin(ChestA, organize);
        Reservation<OrganizeRun>? b = book.Begin(ChestB, organize);
        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(2, book.Active.Count);
        Assert.Same(a, book.Active[0]);
        Assert.Same(b, book.Active[1]);
        Assert.Equal(ReservationState.Requesting, a.State);
        Assert.Equal(ReservationState.Requesting, b.State);

        Assert.Null(book.Begin(ChestA, organize));
        Assert.Equal(ReservationState.Requesting, a.State);

        Assert.True(book.Apply(ChestA, ReservationEvent.Grant));
        Assert.Equal(ReservationState.Granted, a.State);
        Assert.Equal(ReservationState.Requesting, b.State);

        Assert.True(book.Apply(ChestB, ReservationEvent.Deny));
        Assert.Equal(ReservationState.Failed, b.State);
        Assert.Equal(ReservationFailure.Denied, b.Failure);
        Assert.Equal(ReservationState.Requesting, b.FailedFrom);
        Assert.Equal(ReservationState.Granted, a.State);
        Assert.Single(book.Active);
        Assert.Same(a, book.Active[0]);

        Assert.False(book.Apply(ChestUnknown, ReservationEvent.Grant));
        Assert.Equal(ReservationState.Granted, a.State);
        Assert.Equal(ReservationState.Failed, b.State);

        clock.Advance(TimeSpan.FromMilliseconds(TimeoutMs));
        var expired = book.CheckTimeouts();
        Assert.Same(a, Assert.Single(expired));
        Assert.Equal(ReservationState.Failed, a.State);
        Assert.Equal(ReservationFailure.Timeout, a.Failure);
        Assert.Equal(ReservationState.Granted, a.FailedFrom);

        Assert.Equal(2, book.RemoveTerminal());
        Assert.Null(book.Find(ChestA));
        Assert.Null(book.Find(ChestB));

        Reservation<OrganizeRun>? newB = book.Begin(ChestB, organize);
        Assert.NotNull(newB);
        Assert.NotSame(b, newB);
        Assert.Equal(ReservationState.Requesting, newB.State);
        Assert.Equal(At(TimeoutMs), newB.StartedAt);

        Assert.Equal(1, book.ApplyToAll(ReservationEvent.RequesterDisconnected));
        Assert.Equal(ReservationState.Failed, newB.State);
        Assert.Equal(ReservationFailure.RequesterDisconnected, newB.Failure);
    }

    // GW-U-161 (AC-2): Active lists non-terminal reservations ordered by chest, whatever the order of Begin.
    [Fact]
    public void ActiveIsOrderedByChestWhateverTheBeginOrder()
    {
        var book = new ChestReservations<OrganizeRun>(NewClock(), TimeoutMs);
        Reservation<OrganizeRun>? b = book.Begin(ChestB, Organize());
        Reservation<OrganizeRun>? a = book.Begin(ChestA, Organize());

        Assert.Equal(2, book.Active.Count);
        Assert.Same(a, book.Active[0]);
        Assert.Same(b, book.Active[1]);
    }

    // GW-U-161 (AC-2): an Active snapshot does not change when the registry changes later.
    [Fact]
    public void ActiveSnapshotDoesNotChangeLater()
    {
        var book = new ChestReservations<OrganizeRun>(NewClock(), TimeoutMs);
        book.Begin(ChestA, Organize());
        book.Begin(ChestB, Organize());

        var snapshot = book.Active;
        Assert.True(book.Apply(ChestB, ReservationEvent.Deny));

        Assert.Equal(2, snapshot.Count);
        Assert.Single(book.Active);
    }

    // GW-U-161 (AC-2): Find returns terminal reservations until RemoveTerminal forgets them.
    [Fact]
    public void FindKeepsTerminalReservationsUntilRemoved()
    {
        var book = new ChestReservations<OrganizeRun>(NewClock(), TimeoutMs);
        book.Begin(ChestB, Organize());
        Assert.True(book.Apply(ChestB, ReservationEvent.Deny));

        Reservation<OrganizeRun>? found = book.Find(ChestB);
        Assert.NotNull(found);
        Assert.Equal(ReservationState.Failed, found.State);

        Assert.Equal(1, book.RemoveTerminal());
        Assert.Null(book.Find(ChestB));
    }

    // GW-U-161 (AC-2): Begin over a terminal reservation replaces it.
    [Fact]
    public void BeginReplacesATerminalReservation()
    {
        var book = new ChestReservations<OrganizeRun>(NewClock(), TimeoutMs);
        Reservation<OrganizeRun>? old = book.Begin(ChestB, Organize());
        Assert.True(book.Apply(ChestB, ReservationEvent.Deny));

        Reservation<OrganizeRun>? replacement = book.Begin(ChestB, Organize());

        Assert.NotNull(replacement);
        Assert.NotSame(old, replacement);
        Assert.Same(replacement, book.Find(ChestB));
        Assert.Equal(ReservationState.Requesting, replacement.State);
    }

    // GW-U-161 (AC-2): RemoveTerminal never removes a non-terminal reservation.
    [Fact]
    public void RemoveTerminalKeepsEveryNonTerminalReservation()
    {
        var book = new ChestReservations<OrganizeRun>(NewClock(), TimeoutMs);
        Reservation<OrganizeRun>? a = book.Begin(ChestA, Organize());
        book.Begin(ChestB, Organize());
        Assert.True(book.Apply(ChestB, ReservationEvent.Deny));

        Assert.Equal(1, book.RemoveTerminal());

        Assert.Same(a, book.Find(ChestA));
        Assert.Same(a, Assert.Single(book.Active));
        Assert.Equal(0, book.RemoveTerminal());
    }

    // GW-U-161 (AC-2): a second CheckTimeouts returns nothing, because the first one already failed the reservation.
    [Fact]
    public void SecondCheckTimeoutsReturnsEmpty()
    {
        var clock = NewClock();
        var book = new ChestReservations<OrganizeRun>(clock, TimeoutMs);
        book.Begin(ChestA, Organize());
        Assert.Empty(book.CheckTimeouts());

        clock.Set(At(TimeoutMs));
        Assert.Single(book.CheckTimeouts());
        Assert.Empty(book.CheckTimeouts());
    }

    // GW-U-161 (AC-2): Apply on the object that Begin returned is visible through Find, because it is the stored object.
    [Fact]
    public void ApplyDirectlyOnTheReturnedObjectIsVisibleThroughFind()
    {
        var book = new ChestReservations<OrganizeRun>(NewClock(), TimeoutMs);
        Reservation<OrganizeRun>? a = book.Begin(ChestA, Organize());
        Assert.NotNull(a);

        Assert.True(a.Apply(ReservationEvent.Grant));

        Assert.Same(a, book.Find(ChestA));
        Assert.Equal(ReservationState.Granted, book.Find(ChestA)!.State);
    }
}
