namespace BaseStorage.Core.Tests.Reservations;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Reservations.ReservationFixtures;

/// <summary>AC-2: a requester that vanishes frees its chest in every phase, observed or not (contract 0.3, 8.5).</summary>
public sealed class ReservationDisconnectTests
{
    // GW-U-166 (AC-2, 8.5): a disconnect that is observed fails the reservation at once, in every phase.
    [Theory]
    [MemberData(nameof(Phases))]
    public void RequesterDisconnectFreesTheChestInEveryPhase(ReservationState phase)
    {
        var clock = NewClock();
        Reservation<OrganizeRun> r = Drive(phase, clock);

        Assert.True(r.Apply(ReservationEvent.RequesterDisconnected));

        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.RequesterDisconnected, r.Failure);
        Assert.Equal(phase, r.FailedFrom);
        Assert.False(r.HoldsChest);

        foreach (ReservationEvent later in DefinedEvents())
        {
            Assert.False(r.Apply(later));
        }

        Assert.Equal(ReservationState.Failed, r.State);
    }

    // GW-U-166 (AC-2, 8.5): the registry frees three chests at once, in three different phases.
    [Fact]
    public void RegistryDisconnectFailsEveryHoldingChestAndEmptiesActive()
    {
        var book = new ChestReservations<OrganizeRun>(NewClock(), TimeoutMs);
        OrganizeRun organize = Organize();

        book.Begin(ChestA, organize);

        book.Begin(ChestB, organize);
        Assert.True(book.Apply(ChestB, ReservationEvent.Grant));

        book.Begin(ChestUnknown, organize);
        Assert.True(book.Apply(ChestUnknown, ReservationEvent.Grant));
        Assert.True(book.Apply(ChestUnknown, ReservationEvent.BeginWrite));

        Assert.Equal(3, book.ApplyToAll(ReservationEvent.RequesterDisconnected));

        Assert.Empty(book.Active);
        foreach (ContainerId chest in new[] { ChestA, ChestB, ChestUnknown })
        {
            Reservation<OrganizeRun>? found = book.Find(chest);
            Assert.NotNull(found);
            Assert.False(found.HoldsChest);
            Assert.Equal(ReservationFailure.RequesterDisconnected, found.Failure);
        }

        Assert.Equal(0, book.ApplyToAll(ReservationEvent.RequesterDisconnected));
    }

    // GW-U-167 (AC-2, 0.3): a disconnect that is never reported still expires at the fixed deadline, in every phase.
    [Theory]
    [MemberData(nameof(Phases))]
    public void UnobservedDisconnectStillExpiresAtTheDeadline(ReservationState phase)
    {
        var clock = NewClock();
        Reservation<OrganizeRun> r = Drive(phase, clock);

        clock.Set(At(TimeoutMs));

        Assert.True(r.CheckTimeout());
        Assert.Equal(ReservationState.Failed, r.State);
        Assert.Equal(ReservationFailure.Timeout, r.Failure);
        Assert.Equal(phase, r.FailedFrom);
        Assert.False(r.HoldsChest);
    }

    // GW-U-167 (AC-2, 8.5): through the registry, the silent reservation is returned by CheckTimeouts and Active is empty.
    [Theory]
    [MemberData(nameof(Phases))]
    public void RegistryTimeoutReturnsTheSilentReservationAndEmptiesActive(ReservationState phase)
    {
        var clock = NewClock();
        var book = new ChestReservations<OrganizeRun>(clock, TimeoutMs);
        Reservation<OrganizeRun>? r = book.Begin(ChestA, Organize());
        Assert.NotNull(r);
        foreach (ReservationEvent trigger in PathTo(phase))
        {
            Assert.True(r.Apply(trigger));
        }

        clock.Set(At(TimeoutMs));

        Reservation<OrganizeRun> expired = Assert.Single(book.CheckTimeouts());
        Assert.Same(r, expired);
        Assert.Empty(book.Active);
        Assert.Equal(ReservationFailure.Timeout, r.Failure);
        Assert.Equal(phase, r.FailedFrom);
    }

    /// <summary>The four phases in which a requester can hold a reservation, as theory data.</summary>
    public static IEnumerable<object[]> Phases() =>
        new[]
        {
            ReservationState.Requesting,
            ReservationState.Granted,
            ReservationState.Writing,
            ReservationState.Releasing,
        }.Select(state => new object[] { state });
}
