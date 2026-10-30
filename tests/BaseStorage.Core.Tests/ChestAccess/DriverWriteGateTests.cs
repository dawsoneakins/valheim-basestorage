namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-754 (AC-1): the granted-before-write gate CanWrite and BeginWrite (contract 3.3, RA2).</summary>
public sealed class DriverWriteGateTests
{
    // GW-U-754
    [Theory]
    [InlineData("Requesting")]
    [InlineData("Granted")]
    [InlineData("Completed")]
    [InlineData("Failed")]
    [InlineData("Unknown")]
    public void CanWriteIsFalseOutsideWritingAndForAnUnknownId(string state)
    {
        var (game, chest, clock, sink, driver) = Fresh();
        ContainerId id = Chest42;
        if (state == "Granted" || state == "Completed")
        {
            chest.Owner = Me;
            driver.Begin(chest, "organize");
        }

        if (state == "Requesting" || state == "Failed")
        {
            driver.Begin(chest, "organize");
        }

        if (state == "Completed")
        {
            driver.Release(id);
        }

        if (state == "Failed")
        {
            driver.Release(id);
        }

        if (state == "Unknown")
        {
            id = new ContainerId(1L, 1u);
        }

        Assert.False(driver.CanWrite(id));
        Assert.DoesNotContain(game.Calls, call => call.StartsWith("SendOpenRequest", StringComparison.Ordinal) && state == "Unknown");
    }

    // GW-U-754: in Writing, CanWrite needs a valid view that the local peer owns and that the local UI does not hold.
    [Theory]
    [InlineData("owned", true)]
    [InlineData("notOwned", false)]
    [InlineData("viewLost", false)]
    [InlineData("openLocally", false)]
    public void CanWriteInWritingNeedsOwnershipAValidViewAndNoLocalUi(string setup, bool expected)
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;
        driver.Begin(chest, "organize");
        driver.BeginWrite(Chest42);
        if (setup == "notOwned")
        {
            chest.Owner = ChestAccessFixtures.Erik;
        }

        if (setup == "viewLost")
        {
            chest.ViewValid = false;
        }

        if (setup == "openLocally")
        {
            chest.OpenLocally = true;
        }

        Assert.Equal(expected, driver.CanWrite(Chest42));
    }

    // GW-U-754: for SelfOpen the local UI holds the chest by design, so it does not block the write.
    [Fact]
    public void CanWriteIsTrueForASelfOpenWithTheLocalUiOpen()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;
        chest.OpenLocally = true;
        chest.InUseFlag = true;
        Assert.Equal(ReservationRoute.SelfOpen, driver.Begin(chest, "organize"));
        Assert.True(driver.BeginWrite(Chest42));

        Assert.True(driver.CanWrite(Chest42));
    }

    // GW-U-754: BeginWrite is accepted only from Granted.
    [Theory]
    [InlineData("Requesting")]
    [InlineData("Writing")]
    [InlineData("Completed")]
    public void BeginWriteIsRefusedOutsideGranted(string state)
    {
        var (game, chest, clock, sink, driver) = Fresh();
        if (state == "Requesting")
        {
            driver.Begin(chest, "organize");
        }
        else
        {
            chest.Owner = Me;
            driver.Begin(chest, "organize");
            driver.BeginWrite(Chest42);
            if (state == "Completed")
            {
                driver.Release(Chest42);
            }
        }

        Assert.False(driver.BeginWrite(Chest42));
    }

    // GW-U-754: the gate changes nothing.
    [Fact]
    public void CanWriteChangesNothing()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;
        driver.Begin(chest, "organize");
        driver.BeginWrite(Chest42);
        int calls = game.Calls.Count;

        driver.CanWrite(Chest42);
        driver.CanWrite(Chest42);

        Assert.Equal(calls, game.Calls.Count);
        Assert.Equal(ReservationState.Writing, driver.Find(Chest42)!.State);
    }
}
