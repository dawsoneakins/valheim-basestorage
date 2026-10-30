namespace BaseStorage.Core.Tests.Organize.Running;

using System;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Locking;
using BaseStorage.Core.Tests.Organize.Execution;

/// <summary>
/// GW-U-1360..1369 (AC-1, AC-2, AC-4, contract 4.1 Confirm, Cancel, Tick, Finish and section 8 rows 5, 6, 9-13, 15): the preview
/// buttons, one executor step per tick within MovesPerFrame, the renew cadence, the lock lost mid-run, the executor stop, the world
/// gone, and exactly one summary notice and one Info line per run.
/// </summary>
public sealed class RunLifecycleTests
{
    // GW-U-1360 (row 5): cancel releases the lock and shows the cancelled notice; no summary, no log line.
    [Fact]
    public void CancelReleasesTheLockAndShowsCancelled()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Start(RunHarness.Inside);

        Assert.True(h.Coordinator.Cancel());

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Empty(h.StoreValues());
        Assert.Equal(1, h.Store.ClearCount);
        Assert.Equal(new[] { (OrganizeNoticePlacement.TopLeft, "message.organize.cancelled") }, h.Port.Notices.Select(n => (n.Placement, n.Key)).ToArray());
        Assert.Empty(h.Sink.Entries);
        Assert.Empty(h.Port.LayerCalls);
        Assert.Null(h.Coordinator.Plan);
        Assert.False(h.Coordinator.Cancel());
        Assert.False(h.Coordinator.Confirm());
    }

    // GW-U-1361 (row 6, AC-1, AC-4): confirm runs the plan at most MovesPerFrame per tick, moves everything, releases the lock and
    // ends with exactly one summary notice and one Info line.
    [Fact]
    public void ConfirmRunsThePlanWithinTheBoundAndEndsWithOneSummary()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 2);
        h.Coordinator.Start(RunHarness.Inside);
        Assert.True(h.Coordinator.Confirm());
        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.Equal(0, h.AddCalls());

        int ticks = h.RunToIdle(2);

        Assert.True(ticks >= 3, "6 moves at 2 per tick need at least 3 ticks; took " + ticks + ".");
        Assert.Equal(6, h.AddCalls());
        Assert.Empty(h.StoreValues());
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.SummaryNotices());
        Assert.Equal(OrganizeNoticePlacement.TopLeft, placement);
        Assert.Equal("message.organize.run-summary", key);
        Assert.Equal(new object?[] { RunHarness.CompleteSummary }, args);
        Assert.Single(h.Port.Notices);
        (LogSeverity severity, string message) = Assert.Single(h.Sink.Entries);
        Assert.Equal(LogSeverity.Info, severity);
        Assert.Equal(RunHarness.CompleteSummary, message);
        Assert.Null(h.Coordinator.Plan);

        FakeContainerLayer fake = h.Port.Fake!;
        Assert.Equal(ExecutionHarness.Expect(("Wood", 100, 50)), ExecutionHarness.Totals(FakeFixtures.Get(fake, OrganizeFixtures.Id(201)).Contents));
        Assert.Equal(ExecutionHarness.Expect(("Stone", 55, 50), ("Flint", 5, 50)), ExecutionHarness.Totals(FakeFixtures.Get(fake, OrganizeFixtures.Id(202)).Contents));
        Assert.Equal(ExecutionHarness.Expect(("CookedMeat", 5, 50), ("TrophyDeer", 1, 20)), ExecutionHarness.Totals(FakeFixtures.Get(fake, OrganizeFixtures.Id(209)).Contents));
        foreach (ContainerSnapshot chest in fake.GetContainers())
        {
            Assert.Equal(ReservationStatus.None, fake.GetReservationStatus(chest.Id));
        }

        ConservationAssert.Holds(h.Input.Containers, fake.Capture());
    }

    // GW-U-1361: a further tick while idle reads nothing and shows nothing.
    [Fact]
    public void AnIdleTickDoesNothing()
    {
        RunHarness h = RunHarness.Make(skipPreview: true);
        h.Coordinator.Start(RunHarness.Inside);
        h.RunToIdle(2);
        int reads = h.Clock.Reads;
        int calls = h.Store.Calls.Count;
        int notices = h.Port.Notices.Count;

        h.Coordinator.Tick();
        h.Coordinator.Tick();

        Assert.Equal(reads, h.Clock.Reads);
        Assert.Equal(calls, h.Store.Calls.Count);
        Assert.Equal(notices, h.Port.Notices.Count);
        Assert.Single(h.Sink.Entries);
    }

    // GW-U-1362 (row 9): an executor stop ends the run with the reason text, the lock released and one summary.
    [Fact]
    public void AnExecutorStopSummarisesWithTheReasonAndReleasesTheLock()
    {
        RunHarness h = RunHarness.Make(skipPreview: true);
        h.Port.Arrange = fake => fake.InjectFault(OrganizeFixtures.Id(202), FakeFault.DestinationFull);
        h.Coordinator.Start(RunHarness.Inside);

        h.RunToIdle(2);

        Assert.Equal(new object?[] { RunHarness.FullSummary }, Assert.Single(h.SummaryNotices()).Args);
        Assert.Equal(RunHarness.FullSummary, Assert.Single(h.Sink.Entries).Message);
        Assert.Empty(h.StoreValues());
        Assert.Equal(1, h.Store.ClearCount);
        ConservationAssert.Holds(h.Input.Containers, h.Port.Fake!.Capture());
    }

    // GW-U-1362: every executor stop reason renders through its own key.
    [Theory]
    [InlineData(FakeFault.ReservationRefused, 209u, "a chest refused the reservation")]
    [InlineData(FakeFault.OwnerDisconnect, 202u, "a chest changed owner")]
    [InlineData(FakeFault.ChestChanged, 209u, "a chest changed during the run")]
    public void FaultsRenderTheirReasonText(FakeFault fault, uint chest, string reason)
    {
        RunHarness h = RunHarness.Make(skipPreview: true);
        h.Port.Arrange = fake => fake.InjectFault(OrganizeFixtures.Id(chest), fault);
        h.Coordinator.Start(RunHarness.Inside);

        h.RunToIdle(2);

        string summary = (string)Assert.Single(h.SummaryNotices()).Args[0]!;
        Assert.Contains("; stopped early: " + reason + ";", summary, StringComparison.Ordinal);
        Assert.Empty(h.StoreValues());
    }

    // GW-U-1363 (row 10): a lock taken over mid-run aborts the executor and leaves the foreign record alone.
    [Fact]
    public void ALockLostMidRunAbortsAndKeepsTheForeignRecord()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 1, skipPreview: true);
        h.Coordinator.Start(RunHarness.Inside);
        h.Coordinator.Tick();
        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3635);
        h.Clock.Advance(TimeSpan.FromSeconds(10));
        int adds = h.AddCalls();

        h.Coordinator.Tick();

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Equal(adds, h.AddCalls());
        Assert.Equal(new[] { LockFixtures.Astrid3635 }, h.StoreValues());
        Assert.Equal(0, h.Store.ClearCount);
        string summary = (string)Assert.Single(h.SummaryNotices()).Args[0]!;
        Assert.Contains("stopped early: the base lock was lost", summary, StringComparison.Ordinal);
        Assert.Single(h.Sink.Entries, e => e.Item1 == LogSeverity.Info);
        foreach (ContainerSnapshot chest in h.Port.Fake!.GetContainers())
        {
            Assert.Equal(ReservationStatus.None, h.Port.Fake.GetReservationStatus(chest.Id));
        }

        ConservationAssert.Holds(h.Input.Containers, h.Port.Fake.Capture());
    }

    // GW-U-1364 (row 15): the lock is renewed every OrganizeLockSeconds / 3 and not before.
    [Fact]
    public void TheLockIsRenewedAtAThirdOfItsDuration()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 1, skipPreview: true);
        h.Coordinator.Start(RunHarness.Inside);
        h.Store.ResetLog();

        h.Coordinator.Tick();
        h.Clock.Advance(TimeSpan.FromSeconds(5));
        h.Coordinator.Tick();
        Assert.Equal(0, h.Store.WriteCount);
        Assert.Equal(new[] { RunHarness.Erik3630 }, h.StoreValues());

        h.Clock.Advance(TimeSpan.FromSeconds(5));
        h.Coordinator.Tick();

        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.Equal(1, h.Store.WriteCount);
        Assert.Equal(new[] { "L1;1923487561234;36400000000;Erik" }, h.StoreValues());

        // The renew resets the cadence: the next write is a third of the duration after the renew (3620 s), not after the start.
        h.Clock.Advance(TimeSpan.FromSeconds(5));
        h.Coordinator.Tick();
        Assert.Equal(1, h.Store.WriteCount);
        h.Clock.Advance(TimeSpan.FromSeconds(5));
        h.Coordinator.Tick();
        Assert.Equal(2, h.Store.WriteCount);
        Assert.Equal(new[] { "L1;1923487561234;36500000000;Erik" }, h.StoreValues());
    }

    // GW-U-1363 (RN3): the lock vanishing from the station (renew NotHeld) aborts the run like a takeover and releases nothing.
    [Fact]
    public void ALockThatVanishesMidRunAbortsAsLost()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 1, skipPreview: true);
        h.Coordinator.Start(RunHarness.Inside);
        h.Coordinator.Tick();
        int adds = h.AddCalls();
        h.Store.Remove(RunHarness.Station.Id);
        h.Clock.Advance(TimeSpan.FromSeconds(10));

        h.Coordinator.Tick();

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Equal(adds, h.AddCalls());
        Assert.Empty(h.StoreValues());
        Assert.Equal(0, h.Store.ClearCount);
        string summary = (string)Assert.Single(h.SummaryNotices()).Args[0]!;
        Assert.Contains("stopped early: the base lock was lost", summary, StringComparison.Ordinal);
        Assert.Single(h.Sink.Entries, e => e.Item1 == LogSeverity.Info);
        foreach (ContainerSnapshot chest in h.Port.Fake!.GetContainers())
        {
            Assert.Equal(ReservationStatus.None, h.Port.Fake.GetReservationStatus(chest.Id));
        }

        ConservationAssert.Holds(h.Input.Containers, h.Port.Fake.Capture());
    }

    // GW-U-1364 (RN3): the renew interval is max(1, OrganizeLockSeconds) / 3, so a zero duration renews every third of a second.
    [Fact]
    public void TheRenewIntervalFloorsTheDurationAtOneSecond()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 1, skipPreview: true, lockSeconds: 0);
        h.Coordinator.Start(RunHarness.Inside);
        h.Store.ResetLog();

        h.Clock.Advance(TimeSpan.FromMilliseconds(300));
        h.Coordinator.Tick();
        Assert.Equal(0, h.Store.WriteCount);

        h.Clock.Advance(TimeSpan.FromMilliseconds(100));
        h.Coordinator.Tick();
        Assert.Equal(1, h.Store.WriteCount);
    }

    // GW-U-1364 (contract 4.1 clock decrease, fails closed): a clock that went back renews at once; the renew is refused because
    // Erik's record is no longer live (31 s remaining > 30 s), so the run ends lock-lost with no store write and no clear.
    [Fact]
    public void AClockDecreaseEndsTheRunLockLostWithoutAWrite()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 1, skipPreview: true);
        h.Coordinator.Start(RunHarness.Inside);
        h.Store.ResetLog();
        int adds = h.AddCalls();
        h.Clock.Set(TimeSpan.FromSeconds(3599));

        h.Coordinator.Tick();

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Equal(0, h.Store.WriteCount);
        Assert.Equal(new[] { RunHarness.Erik3630 }, h.StoreValues());
        Assert.Equal(0, h.Store.ClearCount);
        Assert.Equal(adds, h.AddCalls());
        string summary = (string)Assert.Single(h.SummaryNotices()).Args[0]!;
        Assert.Contains("stopped early: the base lock was lost", summary, StringComparison.Ordinal);
        Assert.Single(h.Sink.Entries, e => e.Item1 == LogSeverity.Info);
        foreach (ContainerSnapshot chest in h.Port.Fake!.GetContainers())
        {
            Assert.Equal(ReservationStatus.None, h.Port.Fake.GetReservationStatus(chest.Id));
        }

        ConservationAssert.Holds(h.Input.Containers, h.Port.Fake.Capture());
    }

    // GW-U-1386 (AC-6, AC-11, AC-12; contract 4.1 Tick, RN5): a throw inside a run tick ends the run failed, releases the lock
    // once and shows exactly one summary notice and one Info line.
    [Fact]
    public void AThrowDuringATickEndsTheRunFailedAndReleasesTheLock()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 1, skipPreview: true);
        h.Coordinator.Start(RunHarness.Inside);
        h.Clock.Advance(TimeSpan.FromSeconds(10));
        h.Store.ThrowOnWrite = new InvalidOperationException("store failed");

        h.Coordinator.Tick();

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Equal(1, h.Store.ClearCount);
        Assert.Empty(h.StoreValues());
        string summary = (string)Assert.Single(h.SummaryNotices()).Args[0]!;
        Assert.Contains("stopped early", summary, StringComparison.Ordinal);
        Assert.Equal(OrganizeNoticePlacement.TopLeft, Assert.Single(h.SummaryNotices()).Placement);
        Assert.Single(h.Sink.Entries, e => e.Item1 == LogSeverity.Info);
        foreach (ContainerSnapshot chest in h.Port.Fake!.GetContainers())
        {
            Assert.Equal(ReservationStatus.None, h.Port.Fake.GetReservationStatus(chest.Id));
        }

        h.Coordinator.Tick();
        Assert.Single(h.SummaryNotices());
        Assert.Single(h.Sink.Entries, e => e.Item1 == LogSeverity.Info);
    }

    // GW-U-1387 (AC-6, AC-11, AC-12): a store that fails on every read cannot swallow the summary, and the next run can start.
    [Fact]
    public void AThrowWithAFailingStoreStillShowsOneSummary()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 1, skipPreview: true);
        h.Coordinator.Start(RunHarness.Inside);
        h.Clock.Advance(TimeSpan.FromSeconds(10));
        h.Store.ThrowOnRead = new InvalidOperationException("store failed");

        h.Coordinator.Tick();

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Single(h.SummaryNotices());
        Assert.Single(h.Sink.Entries, e => e.Item1 == LogSeverity.Info);
    }

    // GW-U-1388 (AC-6, RN5): a throw while showing the preview releases the lock, returns to Idle and shows no summary.
    [Fact]
    public void AThrowShowingThePreviewReleasesTheLockAndEndsIdle()
    {
        RunHarness h = RunHarness.Make();
        h.Port.ThrowOnShowPreview = new InvalidOperationException("preview failed");

        Exception? thrown = Record.Exception(() => h.Coordinator.Start(RunHarness.Inside));

        Assert.Same(h.Port.ThrowOnShowPreview, thrown);
        AssertAbandoned(h);
        h.Port.ThrowOnShowPreview = null;
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));
        Assert.Equal(OrganizeRunState.Previewing, h.Coordinator.State);
    }

    // GW-U-1389 (AC-6, RN5): a throw creating the layer when the run starts at once releases the lock and ends Idle.
    [Fact]
    public void AThrowCreatingTheLayerOnStartReleasesTheLockAndEndsIdle()
    {
        RunHarness h = RunHarness.Make(skipPreview: true);
        h.Port.ThrowOnCreateLayer = new InvalidOperationException("layer failed");

        Exception? thrown = Record.Exception(() => h.Coordinator.Start(RunHarness.Inside));

        Assert.Same(h.Port.ThrowOnCreateLayer, thrown);
        AssertAbandoned(h);
    }

    // GW-U-1390 (AC-6, RN5): a throw creating the layer on Confirm releases the lock and ends Idle, not stuck in Previewing.
    [Fact]
    public void AThrowCreatingTheLayerOnConfirmReleasesTheLockAndEndsIdle()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Start(RunHarness.Inside);
        h.Port.ThrowOnCreateLayer = new InvalidOperationException("layer failed");

        Exception? thrown = Record.Exception(() => h.Coordinator.Confirm());

        Assert.Same(h.Port.ThrowOnCreateLayer, thrown);
        AssertAbandoned(h);
        Assert.False(h.Coordinator.Confirm());
    }

    // GW-U-1391 (AC-6, RN5): a throw in the Confirm re-acquire ends Idle even when the release also fails.
    [Fact]
    public void AThrowInTheConfirmReacquireEndsIdleWithNoSummary()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Start(RunHarness.Inside);
        h.Store.ThrowOnRead = new InvalidOperationException("store failed");

        Exception? thrown = Record.Exception(() => h.Coordinator.Confirm());

        Assert.Same(h.Store.ThrowOnRead, thrown);
        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Null(h.Coordinator.Plan);
        Assert.Empty(h.SummaryNotices());
        Assert.Empty(h.Sink.Entries);
        Assert.Empty(h.Port.LayerCalls);
    }

    // GW-U-1392 (AC-6): a store failure while cancelling still returns to Idle.
    [Fact]
    public void AThrowWhileCancellingStillEndsIdle()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Start(RunHarness.Inside);
        h.Store.ThrowOnRead = new InvalidOperationException("store failed");

        Assert.NotNull(Record.Exception(() => h.Coordinator.Cancel()));

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Null(h.Coordinator.Plan);
    }

    private static void AssertAbandoned(RunHarness h)
    {
        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Null(h.Coordinator.Plan);
        Assert.Empty(h.StoreValues());
        Assert.Equal(1, h.Store.ClearCount);
        Assert.Empty(h.SummaryNotices());
        Assert.Empty(h.Port.Notices);
        Assert.Empty(h.Sink.Entries);
    }

    // GW-U-1393 (AC-3, AC-9.2): a tick while idle allocates nothing (no per-tick planning, snapshot or clone).
    [Fact]
    public void AnIdleTickAllocatesNothing()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Tick();
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 1000; i++)
        {
            h.Coordinator.Tick();
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    // GW-U-1365 (row 11): the world gone during a run aborts silently.
    [Fact]
    public void TheWorldGoneAbortsWithoutNoticeOrLogOrStoreCall()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 1, skipPreview: true);
        h.Coordinator.Start(RunHarness.Inside);
        h.Coordinator.Tick();
        h.Store.ResetLog();
        h.Port.InWorld = false;

        h.Coordinator.Tick();

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Empty(h.Port.Notices);
        Assert.Empty(h.Sink.Entries);
        Assert.Empty(h.Store.Calls);
        Assert.Equal(new[] { RunHarness.Erik3630 }, h.StoreValues());
        Assert.Null(h.Coordinator.Plan);
    }

    // GW-U-1365: the world gone during the preview drops the preview silently.
    [Fact]
    public void TheWorldGoneDuringThePreviewReturnsToIdle()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Start(RunHarness.Inside);
        h.Port.InWorld = false;

        h.Coordinator.Tick();

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Empty(h.Port.Notices);
        Assert.False(h.Coordinator.Confirm());
    }

    // GW-U-1366 (row 12): confirming after the own lock expired re-acquires it.
    [Fact]
    public void ConfirmAfterTheOwnLockExpiredReacquiresIt()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Start(RunHarness.Inside);
        h.Clock.Advance(TimeSpan.FromSeconds(40));

        Assert.True(h.Coordinator.Confirm());

        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.Equal(new[] { "L1;1923487561234;36700000000;Erik" }, h.StoreValues());
        Assert.Empty(h.Port.Notices);
    }

    // GW-U-1367 (row 13): confirming after another holder took the base refuses and cancels the run.
    [Fact]
    public void ConfirmAfterAnotherHolderTookTheBaseRefuses()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Start(RunHarness.Inside);
        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3620);

        Assert.True(h.Coordinator.Confirm());

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("message.base.in-progress", key);
        Assert.Equal(new object?[] { "Astrid" }, args);
        Assert.Empty(h.Port.LayerCalls);
        Assert.Equal(new[] { LockFixtures.Astrid3620 }, h.StoreValues());
        Assert.Empty(h.Sink.Entries);
    }

    // GW-U-1368: confirm and cancel outside the preview change nothing.
    [Fact]
    public void ConfirmAndCancelOutsideThePreviewAreIgnored()
    {
        RunHarness h = RunHarness.Make(skipPreview: true);
        Assert.False(h.Coordinator.Confirm());
        Assert.False(h.Coordinator.Cancel());

        h.Coordinator.Start(RunHarness.Inside);
        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.False(h.Coordinator.Confirm());
        Assert.False(h.Coordinator.Cancel());
        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.Empty(h.Port.Notices);
    }

    // GW-U-1369: a null sink writes no Info line and still shows the summary; a second run after the first works the same.
    [Fact]
    public void ANullSinkWritesNoLineAndASecondRunRepeatsTheSummary()
    {
        RunHarness h = RunHarness.Make(skipPreview: true, withSink: false);
        h.Coordinator.Start(RunHarness.Inside);
        h.RunToIdle(2);
        Assert.Single(h.SummaryNotices());
        Assert.Empty(h.Sink.Entries);

        h.Source.Containers.Clear();
        h.Source.Containers.AddRange(h.Port.Fake!.GetContainers());
        h.Cache.OnContainerPlaced();
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));
        h.RunToIdle(2);

        Assert.Equal(2, h.SummaryNotices().Count);
        Assert.StartsWith("Nothing moved;", (string)h.SummaryNotices()[1].Args[0]!, StringComparison.Ordinal);
        Assert.Empty(h.StoreValues());
    }
}
