namespace BaseStorage.Core.Tests.Reservations;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Reservations;

/// <summary>AC-1: Apply accepts every legal row and rejects every illegal pair without changing the reservation (S4, S5).</summary>
public sealed class ReservationApplyTests
{
    // GW-U-153 (AC-1): a legal row is accepted and lands in the contracted state, failure and flags.
    [Theory]
    [MemberData(nameof(RowIndices))]
    public void LegalRowIsAcceptedAndLandsInTheContractedState(int index)
    {
        TableRow row = ReservationFixtures.TableRows[index];
        var clock = ReservationFixtures.NewClock();
        Reservation<OrganizeRun> reservation = ReservationFixtures.Drive(row.From, clock);

        Assert.True(reservation.Apply(row.Trigger), $"row {index + 1} was refused: {row.From} {row.Trigger}");

        Assert.Equal(row.To, reservation.State);
        Assert.Equal(row.Failure, reservation.Failure);
        Assert.Equal(row.To == ReservationState.Failed ? (ReservationState?)row.From : null, reservation.FailedFrom);
        Assert.Equal(row.To is ReservationState.Completed or ReservationState.Failed, reservation.IsTerminal);
        Assert.Equal(row.To is ReservationState.Granted or ReservationState.Writing or ReservationState.Releasing, reservation.HoldsChest);
    }

    // GW-U-154 (AC-1, S4): an illegal pair is refused, twice, and the state, failure and failed-from stay the same.
    [Theory]
    [MemberData(nameof(IllegalPairs))]
    public void IllegalEventIsRefusedAndChangesNothing(ReservationState from, ReservationEvent trigger)
    {
        var clock = ReservationFixtures.NewClock();
        Reservation<OrganizeRun> reservation = ReservationFixtures.Drive(from, clock);
        var before = (reservation.State, reservation.Failure, reservation.FailedFrom);

        Assert.False(reservation.Apply(trigger));
        Assert.Equal(before, (reservation.State, reservation.Failure, reservation.FailedFrom));

        Assert.False(reservation.Apply(trigger));
        Assert.Equal(before, (reservation.State, reservation.Failure, reservation.FailedFrom));
    }

    // GW-U-154 (AC-1, S4): undefined event values are refused from every state, without a throw and without a change.
    [Theory]
    [MemberData(nameof(StateIndexes))]
    public void UndefinedEventsAreRefusedFromEveryStateWithoutThrowing(int stateIndex)
    {
        ReservationState from = ReservationFixtures.DefinedStates()[stateIndex];
        foreach (ReservationEvent bogus in new[] { (ReservationEvent)42, (ReservationEvent)(-1) })
        {
            var clock = ReservationFixtures.NewClock();
            Reservation<OrganizeRun> reservation = ReservationFixtures.Drive(from, clock);
            var before = (reservation.State, reservation.Failure, reservation.FailedFrom);

            Assert.False(reservation.Apply(bogus));
            Assert.Equal(before, (reservation.State, reservation.Failure, reservation.FailedFrom));
        }
    }

    /// <summary>Row indices 0 to 18 as theory data.</summary>
    public static IEnumerable<object[]> RowIndices() => Enumerable.Range(0, 19).Select(i => new object[] { i });

    /// <summary>The 35 defined pairs that are not rows of the table, as theory data.</summary>
    public static IEnumerable<object[]> IllegalPairs() =>
        ReservationFixtures.AllPairs()
            .Where(pair => !ReservationFixtures.TryRow((ReservationState)pair[0], (ReservationEvent)pair[1], out _));

    /// <summary>Indices 0 to 5 of the defined states as theory data.</summary>
    public static IEnumerable<object[]> StateIndexes() => Enumerable.Range(0, 6).Select(i => new object[] { i });
}
