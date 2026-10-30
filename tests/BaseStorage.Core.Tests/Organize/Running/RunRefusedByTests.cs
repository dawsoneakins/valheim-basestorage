namespace BaseStorage.Core.Tests.Organize.Running;

using System;
using System.Linq;
using BaseStorage.Core.Commands;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Tests.Locking;

/// <summary>
/// GW-U-2310 to GW-U-2315 (AC-1, AC-2, AC-3, AC-6; organize-console-command contract 3 and 8.3, rows C1-C10): the read-only
/// <c>OrganizeRunCoordinator.RefusedBy</c> that the console reads, on the existing run harness. Every row keeps the notice,
/// lock and state behaviour of organize-runner 8 unchanged. Red until <c>RefusedBy</c> exists. Not a test row.
/// </summary>
public sealed class RunRefusedByTests
{
    // GW-U-2310 (C6): a start with the preview shows the preview line and refuses nothing.
    [Fact]
    public void AStartWithPreviewIsPreviewingWithNoRefusal()
    {
        RunHarness h = RunHarness.Make();

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));

        Assert.Equal(OrganizeRunState.Previewing, h.Coordinator.State);
        Assert.Null(h.Coordinator.RefusedBy);
        Assert.Equal("Organize preview open, close the console to confirm", ConsoleCommandText.Reply(OrganizeStartOutcome.Started, h.Coordinator.State, h.Coordinator.RefusedBy));
    }

    // GW-U-2311 (C4, C5): a start without the preview runs, and the own run blocks a second start with the own name, which ticks keep.
    [Fact]
    public void TheOwnRunBlocksASecondStartWithItsHolderName()
    {
        RunHarness h = RunHarness.Make(skipPreview: true);

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));
        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.Null(h.Coordinator.RefusedBy);
        Assert.Equal("Organize started", ConsoleCommandText.Reply(OrganizeStartOutcome.Started, h.Coordinator.State, h.Coordinator.RefusedBy));

        Assert.Equal(OrganizeStartOutcome.AlreadyActive, h.Coordinator.Start(RunHarness.Inside));
        Assert.Equal("Erik", h.Coordinator.RefusedBy);
        Assert.Equal("Organize in progress by Erik", ConsoleCommandText.Reply(OrganizeStartOutcome.AlreadyActive, h.Coordinator.State, h.Coordinator.RefusedBy));

        h.RunToIdle(2);
        Assert.Equal("Erik", h.Coordinator.RefusedBy);
    }

    // GW-U-2312 (C1, C3): a new coordinator refuses nothing, and a start outside the base after a refusal clears the holder name and takes no lock.
    [Fact]
    public void AStartOutsideTheBaseClearsTheHolderNameAndTakesNoLock()
    {
        RunHarness h = RunHarness.Make();
        Assert.Null(h.Coordinator.RefusedBy);

        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);
        Assert.Equal(OrganizeStartOutcome.LockRefused, h.Coordinator.Start(RunHarness.Inside));
        Assert.Equal("Astrid", h.Coordinator.RefusedBy);
        int callsBefore = h.Store.Calls.Count();

        Assert.Equal(OrganizeStartOutcome.NotInBase, h.Coordinator.Start(RunHarness.Outside));

        Assert.Null(h.Coordinator.RefusedBy);
        Assert.Equal("Not in a base", ConsoleCommandText.Reply(OrganizeStartOutcome.NotInBase, h.Coordinator.State, h.Coordinator.RefusedBy));
        Assert.Equal(callsBefore, h.Store.Calls.Count());
        Assert.Equal(new[] { LockFixtures.Astrid3630 }, h.StoreValues());
    }

    // GW-U-2313 (C2, C9): a lock held by Astrid refuses Start and StartDeposit with her name, one unchanged centre notice, and no second run.
    [Fact]
    public void ALockHeldByAstridRefusesStartAndStartDepositWithHerName()
    {
        RunHarness h = RunHarness.Make();
        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);

        Assert.Equal(OrganizeStartOutcome.LockRefused, h.Coordinator.Start(RunHarness.Inside));

        Assert.Equal("Astrid", h.Coordinator.RefusedBy);
        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("message.base.in-progress", key);
        Assert.Equal(new object?[] { "Astrid" }, args);
        Assert.Equal("Organize in progress by Astrid", ConsoleCommandText.Reply(OrganizeStartOutcome.LockRefused, h.Coordinator.State, h.Coordinator.RefusedBy));

        RunHarness deposit = RunHarness.Make();
        deposit.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);

        Assert.Equal(OrganizeStartOutcome.LockRefused, deposit.Coordinator.StartDeposit(RunHarness.Inside));
        Assert.Equal("Astrid", deposit.Coordinator.RefusedBy);
        Assert.Equal(OrganizeRunState.Idle, deposit.Coordinator.State);
    }

    // GW-U-2314 (C7): Confirm does not change the holder name, and Start with a default start throws and keeps it.
    [Fact]
    public void ConfirmAndAThrowingStartKeepTheHolderName()
    {
        RunHarness confirm = RunHarness.Make();
        Assert.Equal(OrganizeStartOutcome.Started, confirm.Coordinator.Start(RunHarness.Inside));
        Assert.Equal(OrganizeRunState.Previewing, confirm.Coordinator.State);
        // Edited by S-E010-002 (organize-console-command 8.3 C7): a live foreign lock (3630 s + 30 s lock is live at now = 3600 s); 3700 s is not live. Contract 8.3 C7 erratum: `until 3700` is not live under LK (30 s at 3600 s); corrected to 3630 per ADR-GWIN-143 precedent (DepositRunTests.cs:318).
        confirm.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);

        Assert.True(confirm.Coordinator.Confirm());

        Assert.Equal(OrganizeRunState.Idle, confirm.Coordinator.State);
        Assert.Null(confirm.Coordinator.RefusedBy);

        RunHarness refused = RunHarness.Make();
        refused.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);
        Assert.Equal(OrganizeStartOutcome.LockRefused, refused.Coordinator.Start(RunHarness.Inside));

        Assert.Throws<ArgumentException>(() => refused.Coordinator.Start(default));
        Assert.Equal("Astrid", refused.Coordinator.RefusedBy);
    }

    // GW-U-2315 (C10): a base with no usable chest gives NoChests, which clears the holder name and reads "No chests in this base".
    [Fact]
    public void NoChestsClearsTheHolderNameAndShowsTheNoChestsLine()
    {
        RunHarness h = RunHarness.Make(OrganizeFixtures.Make(
            new[] { OrganizeFixtures.C(501, 0, 0, 2, OrganizeFixtures.S("Wood", 10)) },
            OrganizeFixtures.Assign((501, CategoryAssignment.Keep))));

        Assert.Equal(OrganizeStartOutcome.NoChests, h.Coordinator.Start(RunHarness.Inside));

        Assert.Null(h.Coordinator.RefusedBy);
        Assert.Equal("No chests in this base", ConsoleCommandText.Reply(OrganizeStartOutcome.NoChests, h.Coordinator.State, h.Coordinator.RefusedBy));
    }
}
