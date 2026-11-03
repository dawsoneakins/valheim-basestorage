namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using System.Linq;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Tests.Organize.Running;
using static BaseStorage.Core.Tests.Restock.Run.RunFixtures;

/// <summary>GW-U-2585..2588: the base lock of a Restock (S-QR-002 contract 4.2 to 4.4, 10.4, RR2).</summary>
public sealed class RestockRunLockTests
{
    /// <summary>GW-U-2585: another live holder refuses the restock with the in-progress notice, and nothing is touched.</summary>
    [Fact]
    public void RR2_AnotherHoldersLockRefusesTheRestock()
    {
        // Edited by S-QR-002 (ADR-GWIN-176): the harness player is the owner 1923487561234 (FakeOrganizeRunPort.HolderId),
        // which is the owner of RunHarness.Erik3630, so that record is the player's own lock. A foreign owner id is seeded here.
        const string ForeignErikLock = "L1;2000000000002;36300000000;Erik";
        RunHarness harness = Harness();
        harness.Store.Seed(RunHarness.Station.Id, ForeignErikLock);

        Assert.Equal(OrganizeStartOutcome.LockRefused, harness.Coordinator.StartRestock(Player));

        Assert.Equal("Erik", harness.Coordinator.RefusedBy);
        (OrganizeNoticePlacement Placement, string Key, object?[] Args) notice = Assert.Single(harness.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, notice.Placement);
        Assert.Equal("message.base.in-progress", notice.Key);
        Assert.Equal(new object?[] { "Erik" }, notice.Args);
        Assert.Empty(harness.Port.RestockLayerCalls);
        Assert.Null(harness.Port.RestockFake);
        Assert.Equal(ForeignErikLock, harness.Store.Read(RunHarness.Station.Id));
        Assert.Equal(0, harness.Store.WriteCount);
    }

    /// <summary>GW-U-2586: an own Organize preview or an own Restock run refuses a second restock with the own name, and captures nothing.</summary>
    /// <param name="active">Which own run is active: "organize" or "restock".</param>
    [Theory]
    [InlineData("organize")]
    [InlineData("restock")]
    public void RR2_AnOwnActiveRunRefusesTheRestock(string active)
    {
        RunHarness harness = Harness();
        if (active == "organize")
        {
            Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.Start(RunHarness.Inside));
        }
        else
        {
            Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));
        }

        int captures = harness.Port.CarriedCalls;
        int notices = harness.Port.Notices.Count;

        Assert.Equal(OrganizeStartOutcome.AlreadyActive, harness.Coordinator.StartRestock(Player));

        Assert.Equal("Erik", harness.Coordinator.RefusedBy);
        Assert.Equal(notices + 1, harness.Port.Notices.Count);
        Assert.Equal("message.base.in-progress", harness.Port.Notices[notices].Key);
        Assert.Equal(new object?[] { "Erik" }, harness.Port.Notices[notices].Args);
        Assert.Equal(captures, harness.Port.CarriedCalls);
        if (active == "organize")
        {
            Assert.Empty(harness.Port.RestockLayerCalls);
        }
    }

    /// <summary>GW-U-2587: while a Restock runs, Organize and Put away are refused, Confirm and Cancel return false, and there is no plan.</summary>
    [Fact]
    public void RR2_ARunningRestockBlocksOrganizeAndPutAway()
    {
        RunHarness harness = Harness();
        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));
        Assert.Equal(OrganizeRunState.Running, harness.Coordinator.State);

        Assert.Equal(OrganizeStartOutcome.AlreadyActive, harness.Coordinator.Start(RunHarness.Inside));
        Assert.Equal(OrganizeStartOutcome.AlreadyActive, harness.Coordinator.StartDeposit(RunHarness.Inside));
        Assert.False(harness.Coordinator.Confirm());
        Assert.False(harness.Coordinator.Cancel());
        Assert.Null(harness.Coordinator.Plan);
    }

    /// <summary>GW-U-2588: the own lock is written at start, renewed at a third of OrganizeLockSeconds, and released at the end.</summary>
    [Fact]
    public void RR2_TheRestockHoldsRenewsAndReleasesTheBaseLock()
    {
        RunHarness harness = Harness(lockSeconds: 30);
        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));
        Assert.Equal(RunHarness.Erik3630, harness.Store.Read(RunHarness.Station.Id));

        harness.Clock.Advance(TimeSpan.FromSeconds(10));
        harness.Coordinator.Tick();
        Assert.Equal("L1;1923487561234;36400000000;Erik", harness.Store.Read(RunHarness.Station.Id));

        RunToEnd(harness);
        Assert.Empty(harness.Store.Contents());
    }

    /// <summary>GW-U-2588: a refused renewal stops the run with LockLost, leaves the lock in place, and ends the line with "; stopped early".</summary>
    [Fact]
    public void RR2_ARefusedRenewalStopsTheRunWithoutRelease()
    {
        RunHarness harness = Harness(lockSeconds: 30);
        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));

        const string foreign = "L1;42;36400000000;Mara";
        harness.Store.Seed(RunHarness.Station.Id, foreign);
        harness.Clock.Advance(TimeSpan.FromSeconds(10));
        harness.Coordinator.Tick();

        Assert.Equal(foreign, harness.Store.Read(RunHarness.Station.Id));
        RunToEnd(harness);
        Assert.Equal(foreign, harness.Store.Read(RunHarness.Station.Id));
        string summary = Assert.Single(Summaries(harness));
        Assert.EndsWith("; stopped early", summary, StringComparison.Ordinal);
    }
}
