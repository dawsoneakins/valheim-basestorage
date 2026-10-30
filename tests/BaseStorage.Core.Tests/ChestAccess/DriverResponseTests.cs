namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-773 (AC-3): the open-response consumption matrix (RA6, contract 3.3 OnOpenResponse).</summary>
public sealed class DriverResponseTests
{
    // GW-U-773: vanilla runs for an invalid chest, an unknown chest, a terminal reservation, a fast-path reservation and a Denied one.
    [Theory]
    [InlineData("invalid")]
    [InlineData("unknown")]
    [InlineData("completed")]
    [InlineData("localOwner")]
    [InlineData("hostClaim")]
    [InlineData("selfOpen")]
    [InlineData("denied")]
    [InlineData("expiredLate")]
    public void VanillaRunsWhenNoRequestIsOutstanding(string setup)
    {
        var (game, chest, clock, sink, driver) = Fresh(isServer: setup == "hostClaim");
        switch (setup)
        {
            case "invalid":
                driver.Begin(chest, "organize");
                chest.ViewValid = false;
                break;
            case "unknown":
                break;
            case "completed":
                chest.Owner = Me;
                driver.Begin(chest, "organize");
                driver.Release(Chest42);
                break;
            case "localOwner":
                chest.Owner = Me;
                Assert.Equal(ReservationRoute.LocalOwner, driver.Begin(chest, "organize"));
                break;
            case "hostClaim":
                chest.Owner = 0L;
                Assert.Equal(ReservationRoute.HostClaim, driver.Begin(chest, "organize"));
                break;
            case "selfOpen":
                chest.Owner = Me;
                chest.OpenLocally = true;
                Assert.Equal(ReservationRoute.SelfOpen, driver.Begin(chest, "organize"));
                break;
            case "denied":
                driver.Begin(chest, "organize");
                Assert.False(driver.OnOpenResponse(chest, false));
                break;
            default:
                driver.Begin(chest, "organize");
                clock.Set(TimeSpan.FromSeconds(3601.5));
                driver.Tick();
                clock.Set(TimeSpan.FromSeconds(3604.5));
                break;
        }

        int calls = game.Calls.Count;
        int lines = sink.Entries.Count;

        Assert.True(driver.OnOpenResponse(chest, true));
        Assert.True(driver.OnOpenResponse(chest, false));

        Assert.Equal(calls, game.Calls.Count);
        Assert.Equal(lines, sink.Entries.Count);
        Assert.Equal(0, driver.LateCount);
    }

    // GW-U-773: a consumed grant outside Requesting changes nothing but is still consumed while the request is outstanding.
    [Fact]
    public void AConsumedGrantOutsideRequestingChangesNothing()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        driver.Begin(chest, "organize");
        chest.Owner = Me;
        Assert.False(driver.OnOpenResponse(chest, true));
        Assert.Equal(ReservationState.Granted, driver.Find(Chest42)!.State);
        int calls = game.Calls.Count;

        Assert.False(driver.OnOpenResponse(chest, true));
        Assert.False(driver.OnOpenResponse(chest, false));

        Assert.Equal(ReservationState.Granted, driver.Find(Chest42)!.State);
        Assert.Equal(calls, game.Calls.Count);
    }

    // GW-U-773: one late entry consumes exactly one response.
    [Fact]
    public void ALateEntryConsumesExactlyOneResponse()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        driver.Begin(chest, "organize");
        clock.Set(TimeSpan.FromSeconds(3601.5));
        driver.Tick();
        Assert.Equal(1, driver.LateCount);

        clock.Set(TimeSpan.FromSeconds(3602));
        Assert.False(driver.OnOpenResponse(chest, false));
        Assert.Equal(0, driver.LateCount);
        Assert.True(driver.OnOpenResponse(chest, false));
        Assert.Contains("Reserve 3141592653:42 [late]: open response refused consumed, chest UI not shown", Lines(sink));
    }

    // GW-U-773: a response for another chest is never consumed by this chest's reservation.
    [Fact]
    public void AResponseForAnotherChestIsNotConsumed()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        FakeChest other = game.AddChest(new ContainerId(Erik, 9u));
        driver.Begin(chest, "organize");

        Assert.True(driver.OnOpenResponse(other, true));
        Assert.Equal(ReservationState.Requesting, driver.Find(Chest42)!.State);
    }
}
