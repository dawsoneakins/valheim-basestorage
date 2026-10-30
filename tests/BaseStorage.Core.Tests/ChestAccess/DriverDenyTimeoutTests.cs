namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Tests.Logging;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Plugin.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-765, GW-U-766 and GW-U-771 (AC-2, AC-3): deny, timeout, the late-response window and the clock-decrease purge (contract 8.3, 3.5).</summary>
public sealed class DriverDenyTimeoutTests
{
    private const string Id = "3141592653:42";

    // GW-U-765
    [Fact]
    public void ADenyFailsTheRequestAndAddsNoLateEntry()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        driver.Begin(chest, "organize");
        clock.Set(TimeSpan.FromSeconds(3600.1));

        Assert.False(driver.OnOpenResponse(chest, false));

        ChestReservationState(driver, out ReservationState state, out ReservationFailure failure, out ReservationState? failedFrom);
        Assert.Equal(ReservationState.Failed, state);
        Assert.Equal(ReservationFailure.Denied, failure);
        Assert.Equal(ReservationState.Requesting, failedFrom);
        Assert.Equal(new[] { "SendOpenRequest " + Id }, game.Calls);
        Assert.Equal(0, driver.LateCount);
        Assert.True(driver.OnOpenResponse(chest, true));
    }

    // GW-U-766: the four-step timeout table, with the clock one tick before the deadline first.
    [Fact]
    public void TheTimeoutTableFollowsTheContractSteps()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        driver.Begin(chest, "organize");

        clock.Set(TimeSpan.FromTicks(36014999999L));
        driver.Tick();
        Assert.Equal(ReservationState.Requesting, driver.Find(Chest42)!.State);
        Assert.Equal(0, driver.LateCount);

        clock.Set(TimeSpan.FromSeconds(3601.5));
        driver.Tick();
        Assert.Equal(ReservationState.Failed, driver.Find(Chest42)!.State);
        Assert.Equal(ReservationFailure.Timeout, driver.Find(Chest42)!.Failure);
        Assert.Equal(1, driver.LateCount);

        clock.Set(TimeSpan.FromSeconds(3602.0));
        Assert.False(driver.OnOpenResponse(chest, true));
        Assert.Equal(0, driver.LateCount);
        Assert.Equal(new[] { "SendOpenRequest " + Id }, game.Calls);
        Assert.Contains("Reserve 3141592653:42 [late]: open response granted consumed, chest UI not shown", Lines(sink));

        clock.Set(TimeSpan.FromSeconds(3602.1));
        Assert.True(driver.OnOpenResponse(chest, true));
    }

    // GW-U-766: the late window ends just before 3604.5 s, so the response is still consumed one tick earlier.
    [Fact]
    public void TheLateWindowStillConsumesJustBeforeItsEdge()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        driver.Begin(chest, "organize");
        clock.Set(TimeSpan.FromSeconds(3601.5));
        driver.Tick();

        clock.Set(TimeSpan.FromTicks(36044999999L));

        Assert.False(driver.OnOpenResponse(chest, true));
    }

    // GW-U-766: at exactly 3604.5 s the tick purges the entry, so vanilla runs.
    [Fact]
    public void TheLateWindowIsPurgedAtExactlyItsEdge()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        driver.Begin(chest, "organize");
        clock.Set(TimeSpan.FromSeconds(3601.5));
        driver.Tick();

        clock.Set(TimeSpan.FromSeconds(3604.5));
        driver.Tick();

        Assert.Equal(0, driver.LateCount);
        Assert.True(driver.OnOpenResponse(chest, true));
    }

    // GW-U-766: a timeout from Writing releases the marker and adds no late entry.
    [Fact]
    public void ATimeoutFromWritingClearsTheMarkerAndAddsNoLateEntry()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = ChestAccessFixtures.Me;
        Assert.Equal(ReservationRoute.LocalOwner, driver.Begin(chest, "organize"));
        driver.BeginWrite(Chest42);

        clock.Set(TimeSpan.FromSeconds(3601.5));
        driver.Tick();

        Assert.Equal(ReservationState.Failed, driver.Find(Chest42)!.State);
        Assert.Equal(ReservationState.Writing, driver.Find(Chest42)!.FailedFrom);
        Assert.Equal(new[] { "SetInUse " + Id + " true", "SetInUse " + Id + " false" }, game.Calls);
        Assert.Equal(0, driver.LateCount);
    }

    // GW-U-766: a cancel from Requesting fails with Cancelled and keeps a late entry, because the request was sent.
    [Fact]
    public void ACancelFromRequestingKeepsALateEntry()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        driver.Begin(chest, "organize");

        Assert.True(driver.Release(Chest42));

        Assert.Equal(ReservationState.Failed, driver.Find(Chest42)!.State);
        Assert.Equal(ReservationFailure.Cancelled, driver.Find(Chest42)!.Failure);
        Assert.Equal(1, driver.LateCount);
    }

    // GW-U-771: a clock that goes back expires a late entry, and the response then runs vanilla.
    [Fact]
    public void ALateEntryFromTheFutureExpiresWhenTheClockGoesBack()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        driver.Begin(chest, "organize");
        clock.Set(TimeSpan.FromSeconds(3601.5));
        driver.Tick();
        Assert.Equal(1, driver.LateCount);

        clock.Set(TimeSpan.FromSeconds(3500));
        driver.Tick();

        Assert.Equal(0, driver.LateCount);
        Assert.True(driver.OnOpenResponse(chest, true));
    }

    private static void ChestReservationState(ReservationDriver<FakeChest> driver, out ReservationState state, out ReservationFailure failure, out ReservationState? failedFrom)
    {
        var reservation = driver.Find(Chest42)!;
        state = reservation.State;
        failure = reservation.Failure;
        failedFrom = reservation.FailedFrom;
    }
}
