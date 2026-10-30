namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-768 (AC-2): Release is synchronous and idempotent (contract 3.3 Release).</summary>
public sealed class DriverReleaseTests
{
    private const string Id = "3141592653:42";

    // GW-U-768: Release from Granted, without BeginWrite, completes and clears the marker.
    [Fact]
    public void ReleaseFromGrantedCompletesAndClearsTheMarker()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;
        driver.Begin(chest, "organize");

        Assert.True(driver.Release(Chest42));

        Assert.Equal(ReservationState.Completed, driver.Find(Chest42)!.State);
        Assert.Equal(new[] { "SetInUse " + Id + " true", "SetInUse " + Id + " false" }, game.Calls);
        Assert.False(chest.InUseFlag);
        Assert.Equal(
            new[]
            {
                "Reserve 3141592653:42 [organize]: route LocalOwner",
                "Reserve 3141592653:42 [organize]: Requesting -> Granted",
                "Reserve 3141592653:42 [organize]: Granted -> Releasing",
                "Reserve 3141592653:42 [organize]: Releasing -> Completed",
            },
            Lines(sink));
    }

    // GW-U-768: after any Release the state is never Releasing.
    [Theory]
    [InlineData("Requesting")]
    [InlineData("Granted")]
    [InlineData("Writing")]
    public void TheStateIsNeverReleasingAfterRelease(string from)
    {
        var (game, chest, clock, sink, driver) = Fresh();
        if (from != "Requesting")
        {
            chest.Owner = Me;
        }

        driver.Begin(chest, "organize");
        if (from == "Writing")
        {
            driver.BeginWrite(Chest42);
        }

        Assert.True(driver.Release(Chest42));

        Assert.NotEqual(ReservationState.Releasing, driver.Find(Chest42)!.State);
        Assert.True(driver.Find(Chest42)!.IsTerminal);
        Assert.Equal(0, driver.ActiveCount);
    }

    // GW-U-768: Release on an unknown id or a terminal reservation returns false and changes nothing.
    [Fact]
    public void ReleaseOnUnknownOrTerminalReturnsFalse()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;
        driver.Begin(chest, "organize");
        Assert.True(driver.Release(Chest42));
        int calls = game.Calls.Count;
        int lines = sink.Entries.Count;

        Assert.False(driver.Release(Chest42));
        Assert.False(driver.Release(new ContainerId(1L, 1u)));

        Assert.Equal(calls, game.Calls.Count);
        Assert.Equal(lines, sink.Entries.Count);
        Assert.Equal(ReservationState.Completed, driver.Find(Chest42)!.State);
    }

    // GW-U-768: a SelfOpen reservation never touches the marker, so its release issues no command.
    [Fact]
    public void ReleaseOfASelfOpenReservationIssuesNoCommand()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;
        chest.OpenLocally = true;
        chest.InUseFlag = true;
        Assert.Equal(ReservationRoute.SelfOpen, driver.Begin(chest, "organize"));
        Assert.True(driver.BeginWrite(Chest42));
        Assert.True(driver.CanWrite(Chest42));

        Assert.True(driver.Release(Chest42));

        Assert.Equal(ReservationState.Completed, driver.Find(Chest42)!.State);
        Assert.Empty(game.Calls);
        Assert.True(chest.InUseFlag);
    }
}
