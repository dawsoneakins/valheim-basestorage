namespace BaseStorage.Core.Tests.Organize.Running;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Locking;
using BaseStorage.Core.Tests.Organize.Deposit;
using BaseStorage.Core.Ui;

/// <summary>
/// GW-U-1900..1916 (AC-1..AC-4; deposit-button-contracts.md 4.1 and 4.2, rules DB1-DB7): a Deposit is a second run kind of the one
/// Organize coordinator. Every refusal shows its one notice and leaves the coordinator idle, the preview gate is DepositPreview alone,
/// the lock covers both kinds, and every Deposit that reached Running ends with one TopLeft summary and one Info line. Red until the
/// implement phase adds <c>OrganizeRunCoordinator.StartDeposit</c> and the three port members.
/// </summary>
public sealed class DepositRunTests
{
    private static readonly BaseStart AtStation = BaseStart.FromStation(RunHarness.Station.Id);

    // GW-U-1900 (row 1, AC-2): outside a base the Deposit is refused with the not-in-base notice, and nothing is read or locked.
    [Fact]
    public void OutsideABaseGivesNotInABaseWithNoInventoryCapture()
    {
        RunHarness h = RunHarness.Make();

        Assert.Equal(OrganizeStartOutcome.NotInBase, h.Coordinator.StartDeposit(RunHarness.Outside));

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("message.base.not-in-base", key);
        Assert.Empty(args);
        Assert.Equal(0, h.Port.CaptureCalls);
        Assert.Empty(h.Store.Calls);
    }

    // GW-U-1901 (row 2, AC-2): no holder id means no player, so no notice and no capture.
    [Fact]
    public void WithoutAHolderTheDepositGivesNoPlayerAndNoNotice()
    {
        RunHarness h = RunHarness.Make();
        h.Port.HolderId = 0;

        Assert.Equal(OrganizeStartOutcome.NoPlayer, h.Coordinator.StartDeposit(RunHarness.Inside));

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Empty(h.Port.Notices);
        Assert.Equal(0, h.Port.CaptureCalls);
    }

    // GW-U-1902 (row 3, AC-4): an inventory that cannot be mapped gives NoPlayer, no notice, no lock write and no summary.
    [Fact]
    public void AnUnmappableInventoryGivesNoPlayerWithNoLockWrite()
    {
        RunHarness h = RunHarness.Make();
        h.Port.Inventory = null;

        Assert.Equal(OrganizeStartOutcome.NoPlayer, h.Coordinator.StartDeposit(AtStation));

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Empty(h.Port.Notices);
        Assert.Equal(1, h.Port.CaptureCalls);
        LockFixtures.AssertNoWrite(h.Store);
        Assert.Empty(h.Sink.Entries);
    }

    // GW-U-1903 (row 4, AC-1, AC-5.4): a second player in the same base is refused with the holder's name, and nothing is written or moved.
    [Fact]
    public void ASecondPlayerIsRefusedWithTheHolderNameAndNothingMoves()
    {
        RunHarness h = RunHarness.Make();
        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);

        Assert.Equal(OrganizeStartOutcome.LockRefused, h.Coordinator.StartDeposit(AtStation));

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("message.base.in-progress", key);
        Assert.Equal(new object?[] { "Astrid" }, args);
        Assert.Equal("Organize in progress by Astrid", StringTable.English.Format(key, args));
        LockFixtures.AssertNoWrite(h.Store);
        Assert.Null(h.Port.Fake);
        Assert.Empty(h.Port.DepositLayerCalls);
    }

    // GW-U-1904 (row 5, AC-4): a base whose chests are all Keep has no usable chest, so the Deposit refuses with the no-chests notice and no lock write.
    [Fact]
    public void ABaseWithOnlyKeepChestsGivesNoChestsWithNoLockWrite()
    {
        Dictionary<ContainerId, CategoryAssignment> keep = OrganizeFixtures.EChests()
            .ToDictionary(chest => chest.Id, chest => CategoryAssignment.Keep);
        RunHarness h = RunHarness.Make(OrganizeFixtures.Make(OrganizeFixtures.EChests(), keep));

        Assert.Equal(OrganizeStartOutcome.NoChests, h.Coordinator.StartDeposit(AtStation));

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("station.actions.disabled-no-chests", key);
        Assert.Empty(args);
        LockFixtures.AssertNoWrite(h.Store);
    }

    // GW-U-1905 (row 6, AC-1, AC-3): with DepositPreview off the station start runs at once, holds the lock and plans the one move.
    [Fact]
    public void AStationStartWithThePreviewOffRunsAtOnce()
    {
        RunHarness h = RunHarness.Make();

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));

        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.Empty(h.Port.Previews);
        Assert.Equal(new[] { RunHarness.Erik3630 }, h.StoreValues());
        Assert.Single(h.Coordinator.Plan!.Moves);
        Assert.Equal(DepositFixtures.PlayerId, h.Coordinator.Plan.Layouts[0].Container.Id);
    }

    // GW-U-1906 (row 7, AC-4): a completed Put away ends with the one summary and one Info line, moves Wood 10 into chest 201, and conserves every item.
    [Fact]
    public void ACompletedPutAwayEndsWithOneSummaryAndOneInfoLine()
    {
        RunHarness h = RunHarness.Make();
        ContainerSnapshot[] before = h.Input.Containers.Append(h.Port.Inventory!.Container).ToArray();
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));

        h.RunToIdle(2);

        AssertCompletedPutAway(h, before);
    }

    // The Shift + right-click shortcut runs at once, because a popup on every click would defeat it, and leaves the preview on for the next run.
    [Fact]
    public void AQuickDepositSkipsThePreviewEvenWithItOn()
    {
        RunHarness h = RunHarness.Make(depositPreview: true);

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartQuickDeposit(AtStation, new FavouriteSlots(new[] { new InventorySlot(0, 1) })));

        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.Empty(h.Port.Previews);
    }

    // GW-U-1907 (row 8, AC-3): DepositPreview on shows the preview first, with the contracted body, and moves nothing.
    [Fact]
    public void WithThePreviewOnTheDepositShowsThePopupAndMovesNothing()
    {
        RunHarness h = RunHarness.Make(depositPreview: true);

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));

        Assert.Equal(OrganizeRunState.Previewing, h.Coordinator.State);
        (string header, string body) = Assert.Single(h.Port.Previews);
        Assert.Equal("Put away inventory", header);
        Assert.Equal("1 stack to move across 1 chest. 1 chest skipped (in use by Erik). 1 chest skipped (Keep). 1 chest skipped (not eligible).", body);
        Assert.Null(h.Port.Fake);
        Assert.Equal(new[] { RunHarness.Erik3630 }, h.StoreValues());
    }

    // GW-U-1908 (row 9, AC-3): cancelling a Put away releases the lock and shows only the cancelled notice, with no summary and no Info line.
    [Fact]
    public void CancellingThePreviewShowsOnlyThePutAwayCancelledNotice()
    {
        RunHarness h = RunHarness.Make(depositPreview: true);
        h.Coordinator.StartDeposit(AtStation);

        Assert.True(h.Coordinator.Cancel());

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Empty(h.StoreValues());
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.TopLeft, placement);
        Assert.Equal("message.deposit.cancelled", key);
        Assert.Empty(args);
        Assert.Equal("Put away cancelled", StringTable.English.Get(key));
        Assert.Empty(h.SummaryNotices());
        Assert.Empty(h.Sink.Entries);
        Assert.Null(h.Port.Fake);
        Assert.Empty(h.Port.DepositLayerCalls);
        Assert.Equal(new[] { "SwordIron x1 q2 max1", "Wood x10 q1 max50" }, Describe(h.Port.Inventory!.Container.Contents));
    }

    // GW-U-1909 (row 10, AC-4): confirming the preview runs the same Put away to the same end state, notice and Info line as GW-U-1906.
    [Fact]
    public void ConfirmingThePreviewEndsWithTheSameSummaryAsAPlainPutAway()
    {
        RunHarness h = RunHarness.Make(depositPreview: true);
        ContainerSnapshot[] before = h.Input.Containers.Append(h.Port.Inventory!.Container).ToArray();
        h.Coordinator.StartDeposit(AtStation);

        Assert.True(h.Coordinator.Confirm());
        h.RunToIdle(2);

        AssertCompletedPutAway(h, before);
    }

    // GW-U-1910 (row 11, AC-3): only DepositPreview gates a Deposit. OrganizeSkipPreview neither skips nor forces the Deposit preview.
    [Fact]
    public void OrganizeSkipPreviewDoesNotGateADeposit()
    {
        RunHarness h = RunHarness.Make(skipPreview: true, depositPreview: false);

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));

        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.Empty(h.Port.Previews);
    }

    // GW-U-1910 (row 11, second case): DepositPreview alone shows the popup, even with OrganizeSkipPreview set.
    [Fact]
    public void DepositPreviewGatesADepositWhateverOrganizeSkipPreviewSays()
    {
        RunHarness h = RunHarness.Make(skipPreview: true, depositPreview: true);

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));

        Assert.Equal(OrganizeRunState.Previewing, h.Coordinator.State);
        Assert.Single(h.Port.Previews);
    }

    // GW-U-1911 (row 12, AC-4): a stopped Put away ends with the stopped summary, the stop reason in the Info line, no moves and the store empty.
    [Fact]
    public void AStoppedPutAwayEndsWithTheStoppedSummaryAndMovesNothing()
    {
        RunHarness h = RunHarness.Make();
        h.Port.Arrange = fake => fake.InjectFault(OrganizeFixtures.Id(201), FakeFault.DestinationFull);
        ContainerSnapshot[] before = h.Input.Containers.Append(h.Port.Inventory!.Container).ToArray();
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));

        h.RunToIdle(2);

        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.TopLeft, placement);
        Assert.Equal("message.deposit.summary-stopped", key);
        Assert.Equal(new object?[] { 0, 0, "chest full" }, args);
        (LogSeverity severity, string message) = Assert.Single(h.Sink.Entries);
        Assert.Equal(LogSeverity.Info, severity);
        Assert.Equal("Put away: 0 stacks moved, 0 no room; stopped early: chest full", message);
        Assert.Empty(h.StoreValues());
        Assert.Equal(new[] { "SwordIron x1 q2 max1", "Wood x10 q1 max50" }, Describe(Live(h, DepositFixtures.PlayerId).Contents));
        ConservationAssert.Holds(before, h.Port.Fake!.Capture());
    }

    // GW-U-1912 (row 13, AC-4, NFR): a favourite Wood stack is never moved, and the run still ends with one summary and one Info line.
    [Fact]
    public void AFavouriteStackIsNotMovedAndTheRunStillSummarises()
    {
        RunHarness h = RunHarness.Make();
        h.Port.Favourites = new FavouriteSlots(new[] { new InventorySlot(0, 1) });
        ContainerSnapshot[] before = h.Input.Containers.Append(h.Port.Inventory!.Container).ToArray();
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));

        h.RunToIdle(2);

        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.TopLeft, placement);
        Assert.Equal("message.deposit.summary", key);
        Assert.Equal(new object?[] { 0, 0 }, args);
        (LogSeverity severity, string message) = Assert.Single(h.Sink.Entries);
        Assert.Equal(LogSeverity.Info, severity);
        Assert.Equal("Put away: 0 stacks moved, 0 no room", message);
        Assert.Equal(new[] { "SwordIron x1 q2 max1", "Wood x10 q1 max50" }, Describe(Live(h, DepositFixtures.PlayerId).Contents));
        ConservationAssert.Holds(before, h.Port.Fake!.Capture());
    }

    // GW-U-1913 (row 14, AC-1): while one Deposit is previewing, a second Deposit is refused with the holder's name.
    [Fact]
    public void WhileAPreviewIsOpenASecondDepositIsAlreadyActive()
    {
        RunHarness h = RunHarness.Make(depositPreview: true);
        h.Coordinator.StartDeposit(AtStation);

        Assert.Equal(OrganizeStartOutcome.AlreadyActive, h.Coordinator.StartDeposit(AtStation));

        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("message.base.in-progress", key);
        Assert.Equal(new object?[] { "Erik" }, args);
        Assert.Equal(OrganizeRunState.Previewing, h.Coordinator.State);
    }

    // GW-U-1913 (row 14, second case): while one Deposit is previewing, an Organize start is refused the same way.
    [Fact]
    public void WhileAPreviewIsOpenAnOrganizeStartIsAlreadyActive()
    {
        RunHarness h = RunHarness.Make(depositPreview: true);
        h.Coordinator.StartDeposit(AtStation);

        Assert.Equal(OrganizeStartOutcome.AlreadyActive, h.Coordinator.Start(AtStation));

        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("message.base.in-progress", key);
        Assert.Equal(new object?[] { "Erik" }, args);
        Assert.Equal(OrganizeRunState.Previewing, h.Coordinator.State);
    }

    // GW-U-1914 (row 15, AC-1, AC-5.4): an Organize that is running blocks a Deposit, and the Deposit never captures the inventory.
    [Fact]
    public void ARunningOrganizeBlocksADeposit()
    {
        RunHarness h = RunHarness.Make(skipPreview: true);
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(AtStation));

        Assert.Equal(OrganizeStartOutcome.AlreadyActive, h.Coordinator.StartDeposit(AtStation));

        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("message.base.in-progress", key);
        Assert.Equal(new object?[] { "Erik" }, args);
        Assert.Equal(0, h.Port.CaptureCalls);
    }

    // GW-U-1915 (row 16, AC-1, AC-5.4): when another player takes the lock while the preview is open, Confirm is refused and nothing moves.
    [Fact]
    public void ConfirmIsRefusedWhenAnotherPlayerHasTakenTheLock()
    {
        RunHarness h = RunHarness.Make(depositPreview: true);
        h.Coordinator.StartDeposit(AtStation);
        // Edited by S-E004-015 (ADR-GWIN-143): Astrid holds until 3630 s, which is live at 3600 s under the 30 s limit (LK live rule); the former until 3700 s was not live.
        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);

        Assert.True(h.Coordinator.Confirm());

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("message.base.in-progress", key);
        Assert.Equal(new object?[] { "Astrid" }, args);
        Assert.Null(h.Port.Fake);
        Assert.Empty(h.Sink.Entries);
    }

    // GW-U-1916 (row 17, DB7, RN8): a world that is gone during a Deposit ends the run silently, with no notice, no Info line and no store call.
    [Fact]
    public void AWorldThatEndsDuringADepositStopsSilently()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.StartDeposit(AtStation);
        int notices = h.Port.Notices.Count;
        int storeCalls = h.Store.Calls.Count;
        h.Port.InWorld = false;

        h.Coordinator.Tick();

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Equal(notices, h.Port.Notices.Count);
        Assert.Empty(h.Sink.Entries);
        Assert.Equal(storeCalls, h.Store.Calls.Count);
    }

    // The completed-run assertions of GW-U-1906 and GW-U-1909: one summary, one Info line, an empty store, the excluded item kept, and conservation.
    private static void AssertCompletedPutAway(RunHarness h, ContainerSnapshot[] before)
    {
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.TopLeft, placement);
        Assert.Equal("message.deposit.summary", key);
        Assert.Equal(new object?[] { 1, 0 }, args);
        Assert.Empty(h.SummaryNotices());
        (LogSeverity severity, string message) = Assert.Single(h.Sink.Entries);
        Assert.Equal(LogSeverity.Info, severity);
        Assert.Equal("Put away: 1 stacks moved, 0 no room", message);
        Assert.Empty(h.StoreValues());
        Assert.Null(h.Coordinator.Plan);
        Assert.Equal(new[] { "SwordIron x1 q2 max1" }, Describe(Live(h, DepositFixtures.PlayerId).Contents));
        Assert.Equal(new[] { ("Stone", 10), ("Wood", 40) }, Totals(Live(h, OrganizeFixtures.Id(201))));
        ConservationAssert.Holds(before, h.Port.Fake!.Capture());
    }

    // The live chest or inventory of the fake layer after the run.
    private static ContainerSnapshot Live(RunHarness h, ContainerId id) => h.Port.Fake!.Capture().Single(container => container.Id == id);

    // The stacks as prefab, count, quality and maximum stack, in order, so that a stack is compared by value.
    private static string[] Describe(IEnumerable<ItemStackSnapshot> stacks) =>
        stacks.Select(stack => stack.PrefabName + " x" + stack.Count + " q" + stack.Quality + " max" + stack.MaxStack).ToArray();

    // The total count of every prefab in a container, ordered by prefab name.
    private static (string Prefab, int Count)[] Totals(ContainerSnapshot container) =>
        container.Contents
            .GroupBy(stack => stack.PrefabName, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => (group.Key, group.Sum(stack => stack.Count)))
            .ToArray();
}
