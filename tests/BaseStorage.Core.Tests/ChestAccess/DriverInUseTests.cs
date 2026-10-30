namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-779 and GW-U-781 (AC-4): the in-use and skip table (contract 8.4) and the pure-read rule of QueryInUse.</summary>
public sealed class DriverInUseTests
{
    // GW-U-779: the six setups of contract 8.4.
    [Theory]
    [InlineData("erikOpen", "Erik", ReservationRoute.SkipInUse, ReservationFailure.InUseByOther)]
    [InlineData("erikOpenNoName", "another player", ReservationRoute.SkipInUse, ReservationFailure.InUseByOther)]
    [InlineData("erikLeft", null, ReservationRoute.SkipNoOwner, ReservationFailure.OwnerLost)]
    [InlineData("accessDenied", null, ReservationRoute.SkipAccessDenied, ReservationFailure.Denied)]
    [InlineData("viewInvalid", null, ReservationRoute.SkipInvalid, ReservationFailure.None)]
    [InlineData("noOwner", null, ReservationRoute.SkipNoOwner, ReservationFailure.OwnerLost)]
    public void InUseAndSkipRowsIssueNoCommand(string setup, string? holder, ReservationRoute route, ReservationFailure failure)
    {
        var (game, chest, clock, sink, driver) = Fresh();
        Apply(chest, setup);

        ChestInUse? inUse = driver.QueryInUse(chest);
        ReservationRoute? begun = driver.Begin(chest, "organize");

        if (holder is null)
        {
            Assert.Null(inUse);
        }
        else
        {
            Assert.Equal(new ChestInUse(Chest42, holder), inUse);
        }

        Assert.Equal(route, begun);
        Assert.Empty(game.Calls);
        if (setup == "viewInvalid")
        {
            Assert.Null(driver.Find(Chest42));
        }
        else
        {
            Assert.Equal(ReservationState.Failed, driver.Find(Chest42)!.State);
            Assert.Equal(failure, driver.Find(Chest42)!.Failure);
            Assert.Equal(ReservationState.Requesting, driver.Find(Chest42)!.FailedFrom);
        }

        Assert.Equal(0, driver.ActiveCount);
        Assert.Equal(0, driver.LateCount);
    }

    // GW-U-779: the M3 line names the holder.
    [Fact]
    public void TheInUseLineNamesTheHolder()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.InUseFlag = true;

        driver.Begin(chest, "organize");

        Assert.Equal(
            new[]
            {
                "Reserve 3141592653:42 [organize]: route SkipInUse",
                "Reserve 3141592653:42 [organize]: in use by Erik",
                "Reserve 3141592653:42 [organize]: Requesting -> Failed (InUseByOther)",
            },
            Lines(sink));
    }

    // GW-U-779: after the friend closes the chest, Begin replaces the terminal reservation and sends the request.
    [Fact]
    public void BeginAfterTheFriendClosesReplacesTheTerminalReservation()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.InUseFlag = true;
        Assert.Equal(ReservationRoute.SkipInUse, driver.Begin(chest, "organize"));

        chest.InUseFlag = false;
        Assert.Equal(ReservationRoute.SendRequest, driver.Begin(chest, "organize"));

        Assert.Equal(ReservationState.Requesting, driver.Find(Chest42)!.State);
        Assert.Equal(new[] { "SendOpenRequest 3141592653:42" }, game.Calls);
        Assert.Equal(1, driver.ActiveCount);
    }

    // GW-U-781: QueryInUse is a pure read across the 8.4 setups.
    [Theory]
    [InlineData("erikOpen")]
    [InlineData("erikOpenNoName")]
    [InlineData("erikLeft")]
    [InlineData("accessDenied")]
    [InlineData("viewInvalid")]
    [InlineData("noOwner")]
    [InlineData("default")]
    public void QueryInUseChangesNothing(string setup)
    {
        var (game, chest, clock, sink, driver) = Fresh();
        Apply(chest, setup);

        driver.QueryInUse(chest);
        driver.QueryInUse(chest);

        Assert.Empty(game.Calls);
        Assert.Null(driver.Find(Chest42));
        Assert.Equal(0, driver.ActiveCount);
        Assert.Equal(0, driver.LateCount);
        Assert.Equal(0, clock.Reads);
        Assert.Empty(sink.Entries);
    }

    // GW-U-781: an invalid chest gives null and reads nothing beyond the id.
    [Fact]
    public void QueryInUseOnAnInvalidChestGivesNull()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.ViewValid = false;
        chest.InUseFlag = true;

        Assert.Null(driver.QueryInUse(chest));
        Assert.Equal(1, game.Accesses);
    }

    private static void Apply(FakeChest chest, string setup)
    {
        switch (setup)
        {
            case "erikOpen":
                chest.InUseFlag = true;
                break;
            case "erikOpenNoName":
                chest.InUseFlag = true;
                chest.OwnerName = string.Empty;
                break;
            case "erikLeft":
                chest.InUseFlag = true;
                chest.OwnerPresent = false;
                break;
            case "accessDenied":
                chest.AccessAllowed = false;
                break;
            case "viewInvalid":
                chest.ViewValid = false;
                break;
            case "noOwner":
                chest.Owner = 0L;
                break;
            default:
                break;
        }
    }
}
