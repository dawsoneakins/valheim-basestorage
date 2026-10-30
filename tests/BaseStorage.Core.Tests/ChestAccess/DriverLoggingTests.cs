namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Tests.Logging;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-759 (AC-1): the Debug lines are gated by the sink (contract 3.4) and are Debug only.</summary>
public sealed class DriverLoggingTests
{
    // GW-U-759: a sink that disables Debug receives no entry and the scenario still completes.
    [Fact]
    public void ADisabledSinkReceivesNoEntry()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        sink.Enabled = _ => false;

        RunScenario(game, chest, clock, driver);

        Assert.Empty(sink.Entries);
        Assert.Equal(0, sink.LogCalls);
        Assert.Equal(ReservationState.Completed, driver.Find(Chest42)!.State);
    }

    // GW-U-759: a null sink runs the scenario and writes nothing.
    [Fact]
    public void ANullSinkRunsTheScenario()
    {
        var (game, chest, clock, _, _) = Fresh();
        var driver = new ReservationDriver<FakeChest>(game, clock, 1500, StringTable.English, sink: null);

        RunScenario(game, chest, clock, driver);

        Assert.Equal(ReservationState.Completed, driver.Find(Chest42)!.State);
        Assert.Equal(3, game.Calls.Count);
    }

    // GW-U-759: every entry has the Debug severity of LogCategory.Detail, and IsEnabled is asked before each line.
    [Fact]
    public void EveryEntryIsDebugAndGatedByIsEnabled()
    {
        var (game, chest, clock, sink, driver) = Fresh();

        RunScenario(game, chest, clock, driver);

        Assert.Equal(6, sink.Entries.Count);
        Assert.All(sink.Entries, entry => Assert.Equal(LogSeverity.Debug, entry.Severity));
        Assert.All(sink.IsEnabledCalls, severity => Assert.Equal(LogSeverity.Debug, severity));
        Assert.True(sink.IsEnabledCalls.Count >= sink.Entries.Count);
    }

    private static void RunScenario(FakeReservationGame game, FakeChest chest, ManualClock clock, ReservationDriver<FakeChest> driver)
    {
        driver.Begin(chest, "organize");
        clock.Set(TimeSpan.FromSeconds(3600.2));
        driver.OnOpenResponse(chest, true);
        chest.Owner = Me;
        clock.Set(TimeSpan.FromSeconds(3600.3));
        driver.Tick();
        driver.BeginWrite(Chest42);
        clock.Set(TimeSpan.FromSeconds(3600.4));
        driver.Release(Chest42);
        Assert.Equal(Me, chest.Owner);
        Assert.Equal(3, game.Calls.Count);
    }
}
