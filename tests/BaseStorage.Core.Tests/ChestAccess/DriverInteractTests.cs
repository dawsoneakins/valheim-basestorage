namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Plugin.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-774 and GW-U-775 (AC-3): the probe and Interact gating (contract 8.7) and the rule that a real open is never swallowed (8.6 rows 3, 6, 9).</summary>
public sealed class DriverInteractTests
{
    private const string Id = "3141592653:42";

    // GW-U-774: rows 1-4, the probe life cycle on an owned chest.
    [Fact]
    public void TheProbeReservesHoldsHalfTheTimeoutAndReleases()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;

        Assert.False(driver.OnLocalInteract(chest, false, true, true));
        Assert.Equal(ReservationRoute.LocalOwner, driver.RouteOf(Chest42));
        Assert.Equal(ReservationState.Granted, driver.Find(Chest42)!.State);
        Assert.Equal(ReservationDriver<FakeChest>.ProbeOperation, driver.Find(Chest42)!.Operation);
        Assert.Equal(new[] { "SetInUse " + Id + " true" }, game.Calls);

        clock.Set(TimeSpan.FromSeconds(3600.1));
        driver.Tick();
        Assert.Equal(ReservationState.Writing, driver.Find(Chest42)!.State);

        clock.Set(TimeSpan.FromTicks(36007499999L));
        driver.Tick();
        Assert.Equal(ReservationState.Writing, driver.Find(Chest42)!.State);

        clock.Set(TimeSpan.FromSeconds(3600.75));
        driver.Tick();
        Assert.Equal(ReservationState.Completed, driver.Find(Chest42)!.State);
        Assert.Equal(new[] { "SetInUse " + Id + " true", "SetInUse " + Id + " false" }, game.Calls);
        Assert.Equal(
            new[]
            {
                "Reserve 3141592653:42 [probe]: route LocalOwner",
                "Reserve 3141592653:42 [probe]: Requesting -> Granted",
                "Reserve 3141592653:42 [probe]: Granted -> Writing",
                "Reserve 3141592653:42 [probe]: Writing -> Releasing",
                "Reserve 3141592653:42 [probe]: Releasing -> Completed",
            },
            Lines(sink));
    }

    // GW-U-774: rows 5 and 6, the probe is off without Debug and a hold is vanilla.
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    public void ADisabledProbeOrAHoldRunsVanillaAndStoresNothing(bool hold, bool alt, bool probeEnabled)
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;

        Assert.True(driver.OnLocalInteract(chest, hold, alt, probeEnabled));

        Assert.Null(driver.Find(Chest42));
        Assert.Empty(game.Calls);
        Assert.Empty(sink.Entries);
    }

    // GW-U-774: row 7, the probe on a friend's chest sends the request and the response is consumed.
    [Fact]
    public void TheProbeOnAFriendsChestSendsTheRequest()
    {
        var (game, chest, clock, sink, driver) = Fresh();

        Assert.False(driver.OnLocalInteract(chest, false, true, true));

        Assert.Equal(ReservationRoute.SendRequest, driver.RouteOf(Chest42));
        Assert.Equal(new[] { "SendOpenRequest " + Id }, game.Calls);
        Assert.False(driver.OnOpenResponse(chest, true));
    }

    // GW-U-774: a second probe while one is active is refused with M6 and still handled.
    [Fact]
    public void ASecondProbeWhileActiveWritesAlreadyReserved()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;
        driver.OnLocalInteract(chest, false, true, true);

        Assert.False(driver.OnLocalInteract(chest, false, true, true));

        Assert.Contains("Reserve 3141592653:42 [probe]: already reserved", Lines(sink));
        Assert.Single(game.Calls);
    }

    // GW-U-774: a probe on an invalid chest runs vanilla.
    [Fact]
    public void AProbeOnAnInvalidChestRunsVanilla()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.ViewValid = false;

        Assert.True(driver.OnLocalInteract(chest, false, true, true));
        Assert.Null(driver.Find(Chest42));
    }

    // GW-U-775: row 3, a real open during Requesting cancels without a late entry, so the response runs vanilla.
    [Fact]
    public void ARealOpenDuringRequestingCancelsWithoutALateEntry()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        Assert.Equal(ReservationRoute.SendRequest, driver.Begin(chest, "organize"));

        Assert.True(driver.OnLocalInteract(chest, false, false, false));

        Assert.Equal(ReservationState.Failed, driver.Find(Chest42)!.State);
        Assert.Equal(ReservationFailure.Cancelled, driver.Find(Chest42)!.Failure);
        Assert.Equal(0, driver.LateCount);
        Assert.True(driver.OnOpenResponse(chest, true));
        Assert.Equal(new[] { "SendOpenRequest " + Id }, game.Calls);
    }

    // GW-U-775: row 6, vanilla opened the chest during Requesting, so the tick cancels.
    [Fact]
    public void VanillaOpeningTheChestDuringRequestingCancelsOnTick()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        driver.Begin(chest, "organize");
        chest.Owner = Me;
        chest.OpenLocally = true;

        driver.Tick();

        Assert.Equal(ReservationState.Failed, driver.Find(Chest42)!.State);
        Assert.Equal(ReservationFailure.Cancelled, driver.Find(Chest42)!.Failure);
        Assert.Equal(new[] { "SendOpenRequest " + Id }, game.Calls);
    }

    // GW-U-775: row 9, a real open clears a late entry.
    [Fact]
    public void ARealOpenClearsALateEntry()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        driver.Begin(chest, "organize");
        clock.Set(TimeSpan.FromSeconds(3601.5));
        driver.Tick();
        Assert.Equal(1, driver.LateCount);

        Assert.True(driver.OnLocalInteract(chest, false, false, false));

        Assert.Equal(0, driver.LateCount);
        Assert.True(driver.OnOpenResponse(chest, true));
    }
}
