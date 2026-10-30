namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-753 (AC-1, AC-2, AC-3): network grant, write and release, contract 8.2 steps 1-7.</summary>
public sealed class DriverGrantTests
{
    private const string Id = "3141592653:42";

    // GW-U-753
    [Fact]
    public void NetworkGrantWriteAndReleaseFollowTheContractScenario()
    {
        var (game, chest, clock, sink, driver) = Fresh();

        // Step 1, at 3600 s.
        Assert.Equal(ReservationRoute.SendRequest, driver.Begin(chest, "organize"));
        Assert.Equal(ReservationState.Requesting, driver.Find(Chest42)!.State);
        Assert.Equal(new[] { "SendOpenRequest " + Id }, game.Calls);
        Assert.Equal(1, driver.ActiveCount);

        // Step 2: no write before the grant.
        Assert.False(driver.CanWrite(Chest42));
        Assert.False(driver.BeginWrite(Chest42));
        Assert.Equal(new[] { "SendOpenRequest " + Id }, game.Calls);

        // Step 3, at 3600.2 s: the response is consumed; ownership has not synced yet.
        clock.Set(TimeSpan.FromSeconds(3600.2));
        Assert.False(driver.OnOpenResponse(chest, true));
        Assert.Equal(ReservationState.Requesting, driver.Find(Chest42)!.State);
        Assert.Equal(new[] { "SendOpenRequest " + Id }, game.Calls);

        // Step 4, at 3600.3 s: ownership arrives and the tick grants.
        clock.Set(TimeSpan.FromSeconds(3600.3));
        chest.Owner = ChestAccessFixtures.Me;
        driver.Tick();
        Assert.Equal(ReservationState.Granted, driver.Find(Chest42)!.State);
        Assert.Equal(new[] { "SendOpenRequest " + Id, "SetInUse " + Id + " true" }, game.Calls);

        // Step 5: the write gate opens.
        Assert.True(driver.BeginWrite(Chest42));
        Assert.True(driver.CanWrite(Chest42));
        Assert.Equal(ReservationState.Writing, driver.Find(Chest42)!.State);

        // Step 6, at 3600.4 s: the release clears the marker.
        clock.Set(TimeSpan.FromSeconds(3600.4));
        Assert.True(driver.Release(Chest42));
        Assert.Equal(ReservationState.Completed, driver.Find(Chest42)!.State);
        Assert.Equal(new[] { "SendOpenRequest " + Id, "SetInUse " + Id + " true", "SetInUse " + Id + " false" }, game.Calls);
        Assert.Equal(0, driver.ActiveCount);

        // Step 7: a second release does nothing, and the response now runs vanilla.
        Assert.False(driver.Release(Chest42));
        Assert.True(driver.OnOpenResponse(chest, true));
        Assert.Equal(3, game.Calls.Count);
    }

    // GW-U-753
    [Fact]
    public void TheSixDebugLinesAreWrittenInOrder()
    {
        var (game, chest, clock, sink, driver) = Fresh();

        driver.Begin(chest, "organize");
        clock.Set(TimeSpan.FromSeconds(3600.2));
        driver.OnOpenResponse(chest, true);
        chest.Owner = ChestAccessFixtures.Me;
        clock.Set(TimeSpan.FromSeconds(3600.3));
        driver.Tick();
        driver.BeginWrite(Chest42);
        clock.Set(TimeSpan.FromSeconds(3600.4));
        driver.Release(Chest42);

        Assert.Equal(
            new[]
            {
                "Reserve 3141592653:42 [organize]: route SendRequest",
                "Reserve 3141592653:42 [organize]: open response granted consumed, chest UI not shown",
                "Reserve 3141592653:42 [organize]: Requesting -> Granted",
                "Reserve 3141592653:42 [organize]: Granted -> Writing",
                "Reserve 3141592653:42 [organize]: Writing -> Releasing",
                "Reserve 3141592653:42 [organize]: Releasing -> Completed",
            },
            Lines(sink));
        Assert.Equal(new[] { "SendOpenRequest " + Id, "SetInUse " + Id + " true", "SetInUse " + Id + " false" }, game.Calls);
    }

    // GW-U-753: ownership already synced before the response, so the grant comes without a tick.
    [Fact]
    public void AnOwnedChestGrantsOnTheResponseWithoutATick()
    {
        var (game, chest, clock, sink, driver) = Fresh();

        driver.Begin(chest, "organize");
        chest.Owner = ChestAccessFixtures.Me;
        clock.Set(TimeSpan.FromSeconds(3600.2));

        Assert.False(driver.OnOpenResponse(chest, true));
        Assert.Equal(ReservationState.Granted, driver.Find(Chest42)!.State);
        Assert.Equal(new[] { "SendOpenRequest " + Id, "SetInUse " + Id + " true" }, game.Calls);
    }
}
