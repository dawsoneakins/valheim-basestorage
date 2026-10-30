namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-785 (AC-5): the fast paths send no request (contract 8.5).</summary>
public sealed class DriverFastPathTests
{
    private const string Id = "3141592653:42";

    // GW-U-785: the six setups of contract 8.5, with the exact command record after Begin and no Tick.
    [Theory]
    [InlineData("ownedClient", ReservationRoute.LocalOwner, ReservationState.Granted, "SetInUse")]
    [InlineData("ownedHost", ReservationRoute.LocalOwner, ReservationState.Granted, "SetInUse")]
    [InlineData("hostNoOwner", ReservationRoute.HostClaim, ReservationState.Granted, "ClaimOwnership,SetInUse")]
    [InlineData("hostOwnerLeft", ReservationRoute.HostClaim, ReservationState.Granted, "ClaimOwnership,SetInUse")]
    [InlineData("selfOpen", ReservationRoute.SelfOpen, ReservationState.Granted, "")]
    [InlineData("hostFriendConnected", ReservationRoute.SendRequest, ReservationState.Requesting, "SendOpenRequest")]
    public void FastPathsGrantWithoutARequest(string setup, ReservationRoute route, ReservationState state, string commands)
    {
        bool isServer = setup.StartsWith("host", StringComparison.Ordinal) || setup == "ownedHost";
        var (game, chest, clock, sink, driver) = Fresh(isServer: isServer);
        switch (setup)
        {
            case "ownedClient":
            case "ownedHost":
                chest.Owner = Me;
                break;
            case "hostNoOwner":
                chest.Owner = 0L;
                break;
            case "hostOwnerLeft":
                chest.OwnerPresent = false;
                break;
            case "selfOpen":
                chest.Owner = Me;
                chest.OpenLocally = true;
                chest.InUseFlag = true;
                break;
            default:
                break;
        }

        Assert.Equal(route, driver.Begin(chest, "organize"));

        Assert.Equal(state, driver.Find(Chest42)!.State);
        string[] expected = commands.Length == 0
            ? Array.Empty<string>()
            : commands.Split(',').Select(c => c == "SetInUse" ? "SetInUse " + Id + " true" : c + " " + Id).ToArray();
        Assert.Equal(expected, game.Calls);
        if (route != ReservationRoute.SendRequest)
        {
            Assert.DoesNotContain(game.Calls, call => call.StartsWith("SendOpenRequest", StringComparison.Ordinal));
            Assert.Equal(Me, chest.Owner);
        }
    }

    // GW-U-785: the self-open release writes and completes with no command at all.
    [Fact]
    public void SelfOpenWritesAndReleasesWithoutACommand()
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

    // GW-U-785: the host claim publishes the marker before Granted, so the fake sees the flag set by the new owner.
    [Fact]
    public void HostClaimPublishesTheMarkerBeforeGranted()
    {
        var (game, chest, clock, sink, driver) = Fresh(isServer: true);
        chest.Owner = 0L;

        driver.Begin(chest, "organize");

        Assert.True(chest.InUseFlag);
        Assert.Equal(Me, chest.Owner);
        Assert.Equal(
            new[]
            {
                "Reserve 3141592653:42 [organize]: route HostClaim",
                "Reserve 3141592653:42 [organize]: Requesting -> Granted",
            },
            Lines(sink));
    }
}
