namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-793 (AC-8, RA9): the driver fails closed when vanilla ran after a patch failure (contract 8.6 rows 5-6) and never swallows a port exception.</summary>
public sealed class DriverFailClosedTests
{
    private const string Id = "3141592653:42";

    // GW-U-793: row 5, vanilla opened the chest while Writing.
    [Fact]
    public void WritingWithTheLocalUiOpenFailsWithoutClearingTheMarker()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;
        driver.Begin(chest, "organize");
        driver.BeginWrite(Chest42);

        chest.OpenLocally = true;
        driver.Tick();

        Assert.Equal(ReservationState.Failed, driver.Find(Chest42)!.State);
        Assert.Equal(ReservationFailure.InUseByOther, driver.Find(Chest42)!.Failure);
        Assert.False(driver.CanWrite(Chest42));
        Assert.Equal(new[] { "SetInUse " + Id + " true" }, game.Calls);
    }

    // GW-U-793: row 6, vanilla opened the chest while Requesting.
    [Fact]
    public void RequestingWithTheLocalUiOpenCancels()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        Assert.Equal(ReservationRoute.SendRequest, driver.Begin(chest, "organize"));

        chest.Owner = Me;
        chest.OpenLocally = true;
        driver.Tick();

        Assert.Equal(ReservationFailure.Cancelled, driver.Find(Chest42)!.Failure);
        Assert.False(driver.CanWrite(Chest42));
        Assert.Equal(new[] { "SendOpenRequest " + Id }, game.Calls);
    }

    // GW-U-793: a port that throws propagates unchanged; PatchSafety is the only catch.
    [Theory]
    [InlineData("Observe")]
    [InlineData("SendOpenRequest")]
    [InlineData("IsOwner")]
    [InlineData("SetInUse")]
    [InlineData("InWorld")]
    public void APortExceptionPropagatesUnchanged(string member)
    {
        var (game, chest, clock, sink, driver) = Fresh();
        var fault = new InvalidOperationException("port failed: " + member);
        if (member == "IsOwner" || member == "InWorld")
        {
            driver.Begin(chest, "organize");
        }

        if (member == "SetInUse")
        {
            chest.Owner = Me;
        }

        game.FaultMember = member;
        game.Fault = fault;

        Exception thrown = member switch
        {
            "IsOwner" or "InWorld" => Assert.Throws<InvalidOperationException>(() => driver.Tick()),
            _ => Assert.Throws<InvalidOperationException>(() => driver.Begin(chest, "organize")),
        };

        Assert.Same(fault, thrown);
    }
}
