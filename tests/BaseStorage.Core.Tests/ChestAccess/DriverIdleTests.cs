namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-787 (AC-5, RA10): an idle tick makes no game call, reads no clock, writes no line and allocates nothing (contract 8.8).</summary>
public sealed class DriverIdleTests
{
    // GW-U-787: a fresh driver.
    [Fact]
    public void AFreshDriverTicksWithoutTouchingTheGameTheClockOrTheSink()
    {
        var (game, chest, clock, sink, driver) = Fresh();

        driver.Tick();
        long before = GC.GetAllocatedBytesForCurrentThread();
        driver.Tick();
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, game.Accesses);
        Assert.Equal(0, clock.Reads);
        Assert.Empty(sink.Entries);
        Assert.Empty(sink.IsEnabledCalls);
        Assert.Equal(0L, after - before);
        Assert.True(chest.ViewValid);
    }

    // GW-U-787: after a completed reservation and a purged late window the driver is idle again.
    [Fact]
    public void ADriverIsIdleAgainAfterCompletionAndPurge()
    {
        var (game, chest, clock, sink, driver) = Fresh();
        chest.Owner = Me;
        driver.Begin(chest, "organize");
        driver.BeginWrite(Chest42);
        driver.Release(Chest42);
        Assert.Equal(ReservationState.Completed, driver.Find(Chest42)!.State);

        chest.Owner = Erik;
        driver.Begin(chest, "craft");
        clock.Set(TimeSpan.FromSeconds(3601.5));
        driver.Tick();
        Assert.Equal(1, driver.LateCount);
        clock.Set(TimeSpan.FromSeconds(3604.5));
        driver.Tick();
        Assert.Equal(0, driver.LateCount);
        Assert.Equal(0, driver.ActiveCount);

        int accesses = game.Accesses;
        int reads = clock.Reads;
        int lines = sink.Entries.Count;
        driver.Tick();
        long before = GC.GetAllocatedBytesForCurrentThread();
        driver.Tick();
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(accesses, game.Accesses);
        Assert.Equal(reads, clock.Reads);
        Assert.Equal(lines, sink.Entries.Count);
        Assert.Equal(0L, after - before);
    }
}
