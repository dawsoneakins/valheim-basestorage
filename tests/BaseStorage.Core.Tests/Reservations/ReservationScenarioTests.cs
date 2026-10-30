namespace BaseStorage.Core.Tests.Reservations;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Reservations.ReservationFixtures;

/// <summary>AC-3: the four outcomes of spec AC-3 as contract walkthroughs (8.1, 8.2, 8.3, 8.4, 8.6), on the manual clock.</summary>
public sealed class ReservationScenarioTests
{
    // GW-U-170 (AC-3, 8.1): the grant path reaches Completed, and a later CheckTimeout or InUseByOther changes nothing.
    [Fact]
    public void GrantPathReachesCompletionAndLaterEventsChangeNothing()
    {
        var clock = NewClock();
        OrganizeRun organize = Organize();
        var r = new Reservation<OrganizeRun>(ChestA, organize, clock, TimeoutMs);
        Assert.Equal(ReservationState.Requesting, r.State);
        Assert.Equal(Start, r.StartedAt);
        Assert.Equal(TimeSpan.FromMilliseconds(TimeoutMs), r.Timeout);
        Assert.False(r.HoldsChest);

        Assert.True(r.Apply(ReservationEvent.Grant));
        Assert.Equal(ReservationState.Granted, r.State);
        Assert.True(r.HoldsChest);

        Assert.False(r.Apply(ReservationEvent.Grant));
        Assert.Equal(ReservationState.Granted, r.State);

        Assert.True(r.Apply(ReservationEvent.BeginWrite));
        Assert.Equal(ReservationState.Writing, r.State);

        Assert.True(r.Apply(ReservationEvent.BeginRelease));
        Assert.Equal(ReservationState.Releasing, r.State);
        Assert.True(r.HoldsChest);

        Assert.True(r.Apply(ReservationEvent.CompleteRelease));
        Assert.Equal(ReservationState.Completed, r.State);
        Assert.True(r.IsTerminal);
        Assert.Equal(ReservationFailure.None, r.Failure);
        Assert.Null(r.FailedFrom);
        Assert.False(r.HoldsChest);

        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.False(r.CheckTimeout());
        Assert.Equal(ReservationState.Completed, r.State);

        Assert.False(r.Apply(ReservationEvent.InUseByOther));
        Assert.Equal(ReservationState.Completed, r.State);
        Assert.Same(organize, r.Operation);
    }

    // GW-U-171 (AC-3, 8.2): a deny ends in Failed with Denied, and a late grant is rejected.
    [Fact]
    public void DenyEndsInFailedAndALateGrantIsRejected()
    {
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), NewClock(), TimeoutMs);

        Assert.False(r.Apply(ReservationEvent.BeginWrite));
        Assert.Equal(ReservationState.Requesting, r.State);

        Assert.True(r.Apply(ReservationEvent.Deny));
        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.Denied, r.Failure);
        Assert.Equal(ReservationState.Requesting, r.FailedFrom);
        Assert.False(r.HoldsChest);

        Assert.False(r.Apply(ReservationEvent.Grant));
        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.Denied, r.Failure);
        Assert.Equal(ReservationState.Requesting, r.FailedFrom);
    }

    // GW-U-171 (AC-3, 8.2): through the registry, a deny leaves no active reservation.
    [Fact]
    public void RegistryDenyLeavesNoActiveReservation()
    {
        var book = new ChestReservations<OrganizeRun>(NewClock(), TimeoutMs);
        book.Begin(ChestA, Organize());

        Assert.True(book.Apply(ChestA, ReservationEvent.Deny));

        Assert.Empty(book.Active);
    }

    // GW-U-172 (AC-3, 8.3): a timeout in the middle of writing fails from Writing, and the release that follows is rejected.
    [Fact]
    public void TimeoutInTheMiddleOfWritingFailsFromWriting()
    {
        var clock = NewClock();
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), clock, TimeoutMs);

        clock.Set(At(1000));
        Assert.True(r.Apply(ReservationEvent.Grant));

        clock.Set(At(1200));
        Assert.True(r.Apply(ReservationEvent.BeginWrite));

        clock.Set(At(1500));
        Assert.True(r.CheckTimeout());
        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.Timeout, r.Failure);
        Assert.Equal(ReservationState.Writing, r.FailedFrom);
        Assert.False(r.HoldsChest);

        Assert.False(r.Apply(ReservationEvent.BeginRelease));
        Assert.Equal(ReservationState.Failed, r.State);
    }

    // GW-U-173 (AC-3, 8.4): the owner is lost while writing, and the release that follows is rejected.
    [Fact]
    public void OwnerLostWhileWritingFailsFromWriting()
    {
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), NewClock(), TimeoutMs);
        Assert.True(r.Apply(ReservationEvent.Grant));
        Assert.True(r.Apply(ReservationEvent.BeginWrite));

        Assert.True(r.Apply(ReservationEvent.OwnerLost));

        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.OwnerLost, r.Failure);
        Assert.Equal(ReservationState.Writing, r.FailedFrom);
        Assert.False(r.HoldsChest);
        Assert.False(r.Apply(ReservationEvent.BeginRelease));
    }

    // GW-U-173 (AC-3, 8.4): the owner is lost while granted or releasing, and FailedFrom matches that phase.
    [Theory]
    [MemberData(nameof(GrantedOrReleasing))]
    public void OwnerLostWhileGrantedOrReleasingKeepsItsFailedFrom(ReservationState phase)
    {
        var r = Drive(phase, NewClock());

        Assert.True(r.Apply(ReservationEvent.OwnerLost));

        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.OwnerLost, r.Failure);
        Assert.Equal(phase, r.FailedFrom);
        Assert.False(r.HoldsChest);
        Assert.False(r.Apply(ReservationEvent.BeginRelease));
    }

    // GW-U-173 (AC-3): in one registry, the owner lost on one chest leaves the other chest, also writing, untouched.
    [Fact]
    public void RegistryOwnerLostLeavesTheOtherWritingChestUnaffected()
    {
        var book = new ChestReservations<OrganizeRun>(NewClock(), TimeoutMs);
        Reservation<OrganizeRun>? a = StartWriting(book, ChestA);
        Reservation<OrganizeRun>? b = StartWriting(book, ChestB);

        Assert.True(book.Apply(ChestA, ReservationEvent.OwnerLost));

        Assert.NotNull(a);
        Assert.Equal(ReservationState.Failed, a.State);
        Assert.Equal(ReservationFailure.OwnerLost, a.Failure);
        Assert.NotNull(b);
        Assert.Equal(ReservationState.Writing, b.State);
        Assert.Same(b, Assert.Single(book.Active));
    }

    // GW-U-174 (AC-3, 8.6, S7): another player has the chest open mid-operation. The reservation fails and no hand-back is pretended.
    [Fact]
    public void InUseByOtherWhileWritingFailsWithoutHandingBack()
    {
        var r = new Reservation<OrganizeRun>(ChestA, Organize(), NewClock(), TimeoutMs);
        Assert.True(r.Apply(ReservationEvent.Grant));
        Assert.True(r.Apply(ReservationEvent.BeginWrite));

        Assert.True(r.Apply(ReservationEvent.InUseByOther));

        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.InUseByOther, r.Failure);
        Assert.Equal(ReservationState.Writing, r.FailedFrom);
        Assert.False(r.HoldsChest);
        Assert.False(r.Apply(ReservationEvent.BeginRelease));
        Assert.False(r.Apply(ReservationEvent.CompleteRelease));
        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.InUseByOther, r.Failure);
    }

    // GW-U-174 (AC-3, 8.6): the same result from requesting, granted and releasing, each with its own FailedFrom.
    [Theory]
    [MemberData(nameof(RequestingGrantedOrReleasing))]
    public void InUseByOtherFromEveryOtherNonTerminalPhaseFailsWithThatPhase(ReservationState phase)
    {
        var r = Drive(phase, NewClock());

        Assert.True(r.Apply(ReservationEvent.InUseByOther));

        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.InUseByOther, r.Failure);
        Assert.Equal(phase, r.FailedFrom);
        Assert.False(r.HoldsChest);
    }

    // GW-U-174 (AC-3, 8.6, S7): in one registry, the other player takes one of two writing chests. The other chest keeps writing.
    [Fact]
    public void RegistryInUseOnOneChestLeavesTheOtherWriting()
    {
        var book = new ChestReservations<OrganizeRun>(NewClock(), TimeoutMs);
        Reservation<OrganizeRun>? a = StartWriting(book, ChestA);
        Reservation<OrganizeRun>? b = StartWriting(book, ChestB);

        Assert.True(book.Apply(ChestA, ReservationEvent.InUseByOther));

        Assert.NotNull(a);
        Assert.Equal(ReservationState.Failed, a.State);
        Assert.Equal(ReservationFailure.InUseByOther, a.Failure);
        Assert.NotNull(b);
        Assert.Equal(ReservationState.Writing, b.State);
        Assert.Same(b, Assert.Single(book.Active));
    }

    /// <summary>The two phases that hold the chest without writing or still releasing it, as theory data.</summary>
    public static IEnumerable<object[]> GrantedOrReleasing() => new[] { ReservationState.Granted, ReservationState.Releasing }
        .Select(state => new object[] { state });

    /// <summary>The three phases where another player can be observed with the chest open, as theory data.</summary>
    public static IEnumerable<object[]> RequestingGrantedOrReleasing() => new[]
        {
            ReservationState.Requesting,
            ReservationState.Granted,
            ReservationState.Releasing,
        }
        .Select(state => new object[] { state });

    /// <summary>Requests a chest, grants it and begins writing, for the registry scenarios.</summary>
    private static Reservation<OrganizeRun>? StartWriting(ChestReservations<OrganizeRun> book, ContainerId chest)
    {
        Reservation<OrganizeRun>? reservation = book.Begin(chest, Organize());
        Assert.NotNull(reservation);
        Assert.True(book.Apply(chest, ReservationEvent.Grant));
        Assert.True(book.Apply(chest, ReservationEvent.BeginWrite));
        return reservation;
    }
}
