namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Tests.Reservations;

/// <summary>GW-U-752 (AC-1): the poll table of contract 2.5, the response mapping, and the Apply sweep over every state (2.4).</summary>
public sealed class ChestAccessPollTests
{
    // GW-U-752
    [Theory]
    [InlineData(ReservationState.Requesting, true, false, false, false, null)]
    [InlineData(ReservationState.Requesting, true, false, true, false, null)]
    [InlineData(ReservationState.Requesting, true, true, true, false, ReservationEvent.Grant)]
    [InlineData(ReservationState.Requesting, true, true, false, false, null)]
    [InlineData(ReservationState.Requesting, false, true, true, false, ReservationEvent.OwnerLost)]
    [InlineData(ReservationState.Requesting, true, true, true, true, ReservationEvent.Cancel)]
    [InlineData(ReservationState.Granted, true, true, true, false, null)]
    [InlineData(ReservationState.Writing, true, false, true, false, ReservationEvent.OwnerLost)]
    [InlineData(ReservationState.Writing, true, true, true, true, ReservationEvent.InUseByOther)]
    [InlineData(ReservationState.Releasing, false, false, true, false, ReservationEvent.OwnerLost)]
    [InlineData(ReservationState.Completed, false, false, false, true, null)]
    [InlineData((ReservationState)99, true, true, true, false, null)]
    public void PollMatchesTheContractRow(ReservationState state, bool viewValid, bool isOwner, bool grantReceived, bool openedLocally, ReservationEvent? expected)
    {
        Assert.Equal(expected, ChestAccessRules.Poll(state, viewValid, isOwner, grantReceived, openedLocally));
    }

    // GW-U-752
    [Fact]
    public void EventForResponseMapsADenyAndLeavesAGrantOpen()
    {
        Assert.Equal(ReservationEvent.Deny, ChestAccessRules.EventForResponse(false));
        Assert.Null(ChestAccessRules.EventForResponse(true));
    }

    // GW-U-752: every non-null poll result is a legal transition of the state, so Apply always accepts it.
    [Theory]
    [MemberData(nameof(PollSweepCases))]
    public void EveryNonNullPollResultIsAcceptedByApply(ReservationState state, bool viewValid, bool isOwner, bool grantReceived, bool openedLocally)
    {
        ReservationEvent? poll = ChestAccessRules.Poll(state, viewValid, isOwner, grantReceived, openedLocally);
        if (poll is null)
        {
            return;
        }

        Reservation<string> reservation = DriveTo(state);
        Assert.True(reservation.Apply(poll.Value), $"Apply rejected {poll.Value} in {state}.");
    }

    /// <summary>Every defined state crossed with the 16 combinations of (V, O, G, L).</summary>
    public static IEnumerable<object[]> PollSweepCases()
    {
        ReservationState[] states =
        {
            ReservationState.Requesting, ReservationState.Granted, ReservationState.Writing,
            ReservationState.Releasing, ReservationState.Completed, ReservationState.Failed,
        };
        foreach (ReservationState state in states)
        {
            for (int mask = 0; mask < 16; mask++)
            {
                yield return new object[] { state, (mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0 };
            }
        }
    }

    private static Reservation<string> DriveTo(ReservationState state)
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var reservation = new Reservation<string>(ChestAccessFixtures.Chest42, "probe", clock, 1500);
        if (state == ReservationState.Requesting)
        {
            return reservation;
        }

        if (state == ReservationState.Failed)
        {
            reservation.Apply(ReservationEvent.Cancel);
            return reservation;
        }

        reservation.Apply(ReservationEvent.Grant);
        if (state == ReservationState.Granted)
        {
            return reservation;
        }

        reservation.Apply(ReservationEvent.BeginWrite);
        if (state == ReservationState.Writing)
        {
            return reservation;
        }

        reservation.Apply(ReservationEvent.BeginRelease);
        if (state == ReservationState.Releasing)
        {
            return reservation;
        }

        reservation.Apply(ReservationEvent.CompleteRelease);
        return reservation;
    }
}
