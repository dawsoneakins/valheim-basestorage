namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-767 (AC-2): owner lost, view lost, local open and requester gone (contract 8.6 rows 1, 2, 4, 5, 7).</summary>
public sealed class DriverLostTests
{
    private const string Id = "3141592653:42";

    // GW-U-767
    [Theory]
    [InlineData(1, "ownerChanged", ReservationFailure.OwnerLost, ReservationState.Writing, false)]
    [InlineData(2, "viewLost", ReservationFailure.OwnerLost, ReservationState.Writing, false)]
    [InlineData(4, "localInteract", ReservationFailure.InUseByOther, ReservationState.Writing, true)]
    [InlineData(5, "openLocally", ReservationFailure.InUseByOther, ReservationState.Writing, false)]
    [InlineData(7, "leftWorld", ReservationFailure.RequesterDisconnected, ReservationState.Writing, false)]
    public void AHeldReservationFailsAndClearsOnlyWhenItStillHoldsTheChest(int row, string action, ReservationFailure failure, ReservationState failedFrom, bool clears)
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;
        Assert.Equal(ReservationRoute.LocalOwner, driver.Begin(chest, "organize"));
        Assert.True(driver.BeginWrite(Chest42));
        Assert.True(driver.CanWrite(Chest42));

        switch (action)
        {
            case "ownerChanged":
                chest.Owner = Erik;
                driver.Tick();
                break;
            case "viewLost":
                chest.ViewValid = false;
                driver.Tick();
                break;
            case "localInteract":
                Assert.True(driver.OnLocalInteract(chest, false, false, false));
                break;
            case "openLocally":
                chest.OpenLocally = true;
                driver.Tick();
                break;
            default:
                game.InWorld = false;
                driver.Tick();
                break;
        }

        Reservation<string> reservation = driver.Find(Chest42)!;
        Assert.Equal(ReservationState.Failed, reservation.State);
        Assert.Equal(failure, reservation.Failure);
        Assert.Equal(failedFrom, reservation.FailedFrom);
        Assert.False(driver.CanWrite(Chest42));
        Assert.Equal(0, driver.LateCount);
        string[] expected = clears
            ? new[] { "SetInUse " + Id + " true", "SetInUse " + Id + " false" }
            : new[] { "SetInUse " + Id + " true" };
        Assert.Equal(expected, game.Calls);
        Assert.True(row > 0);
        Assert.Equal(0, driver.ActiveCount);
    }

    // GW-U-767: leaving the world fails every non-terminal reservation and clears the late set with no game call.
    [Fact]
    public void LeavingTheWorldFailsEveryReservationAndClearsTheLateSet()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        FakeChest second = game.AddChest(new ContainerId(Me, 43u));
        second.Owner = Me;
        driver.Begin(chest, "organize");
        driver.Begin(second, "craft");
        FakeChest third = game.AddChest(new ContainerId(Me, 44u));
        driver.Begin(third, "deposit");
        driver.Release(third.Id);
        Assert.Equal(1, driver.LateCount);
        int calls = game.Calls.Count;

        game.InWorld = false;
        driver.Tick();

        Assert.Equal(ReservationFailure.RequesterDisconnected, driver.Find(Chest42)!.Failure);
        Assert.Equal(ReservationFailure.RequesterDisconnected, driver.Find(second.Id)!.Failure);
        Assert.Equal(ReservationFailure.Cancelled, driver.Find(third.Id)!.Failure);
        Assert.Equal(0, driver.LateCount);
        Assert.Equal(0, driver.ActiveCount);
        Assert.Equal(calls, game.Calls.Count);
        Assert.Contains("Reserve 3141592653:43 [craft]: Granted -> Failed (RequesterDisconnected)", Lines(sink));
    }
}
