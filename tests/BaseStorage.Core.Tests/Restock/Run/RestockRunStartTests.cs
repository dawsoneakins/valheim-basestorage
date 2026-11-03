namespace BaseStorage.Core.Tests.Restock.Run;

using System.Collections.Generic;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize.Running;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;
using static BaseStorage.Core.Tests.Restock.Run.RunFixtures;

/// <summary>GW-U-2575, 2592, 2593 and 2615: the start outcomes of a Restock (S-QR-002 contract 4.3, 10.3, 10.5, 10.13).</summary>
public sealed class RestockRunStartTests
{
    /// <summary>GW-U-2575: a failed capture gives NoPlayer, one Centre notice, no lock, no layer and no summary.</summary>
    [Fact]
    public void RR3_CaptureFailureShowsANoticeAndTakesNoLock()
    {
        RunHarness harness = Harness();
        harness.Port.Carried = null;

        Assert.Equal(OrganizeStartOutcome.NoPlayer, harness.Coordinator.StartRestock(Player));

        (OrganizeNoticePlacement Placement, string Key, object?[] Args) notice = Assert.Single(harness.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, notice.Placement);
        Assert.Equal("message.restock.capture-failed", notice.Key);
        Assert.Empty(notice.Args);
        Assert.Empty(harness.Store.Contents());
        Assert.Equal(0, harness.Store.WriteCount);
        Assert.Equal(OrganizeRunState.Idle, harness.Coordinator.State);
        Assert.Equal(0, harness.Port.ChestCalls);
        Assert.Empty(harness.Port.RestockLayerCalls);
        Assert.Empty(Summaries(harness));
        Assert.Empty(InfoLines(harness));
    }

    /// <summary>GW-U-2592: a start outside every base gives NotInBase, one Centre notice and nothing captured or written.</summary>
    [Fact]
    public void RR1_NoBaseHoldsThePlayerSoNothingMoves()
    {
        RunHarness harness = Harness();

        Assert.Equal(OrganizeStartOutcome.NotInBase, harness.Coordinator.StartRestock(Outside));

        (OrganizeNoticePlacement Placement, string Key, object?[] Args) notice = Assert.Single(harness.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, notice.Placement);
        Assert.Equal("message.base.not-in-base", notice.Key);
        Assert.Empty(notice.Args);
        Assert.Equal(0, harness.Port.CarriedCalls);
        Assert.Equal(0, harness.Port.ChestCalls);
        Assert.Empty(harness.Port.RestockLayerCalls);
        Assert.Empty(harness.Store.Contents());
        Assert.Equal(0, harness.Store.WriteCount);
        Assert.Empty(Summaries(harness));
        Assert.Empty(InfoLines(harness));
    }

    /// <summary>GW-U-2593: the start resolves the base once through the cache.</summary>
    [Fact]
    public void RR1_TheRunUsesTheBaseThatHoldsThePlayer()
    {
        RunHarness harness = Harness();

        harness.Coordinator.StartRestock(Player);

        Assert.Equal(1, harness.Source.StationReads);
    }

    /// <summary>GW-U-2593: without a local player there is no notice and no capture.</summary>
    /// <param name="inWorld">The InWorld value of the port.</param>
    /// <param name="holderId">The HolderId value of the port.</param>
    [Theory]
    [InlineData(false, 1L)]
    [InlineData(true, 0L)]
    public void RR1_NoPlayerGivesNoNoticeAndNoCapture(bool inWorld, long holderId)
    {
        RunHarness harness = Harness();
        harness.Port.InWorld = inWorld;
        harness.Port.HolderId = holderId;

        Assert.Equal(OrganizeStartOutcome.NoPlayer, harness.Coordinator.StartRestock(Player));

        Assert.Empty(harness.Port.Notices);
        Assert.Equal(0, harness.Port.CarriedCalls);
    }

    /// <summary>GW-U-2615: nothing to restock ends with one summary line, a lock taken and released, and no layer.</summary>
    [Fact]
    public void RR12_NothingToRestockEndsWithOneLineAndNoLayer()
    {
        RunHarness harness = Harness();
        harness.Port.Carried = new List<CarriedStack> { H(S("Wood", 50)), U(S("Stone", 10)) };

        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));

        Assert.Equal(OrganizeRunState.Idle, harness.Coordinator.State);
        Assert.Empty(harness.Store.Contents());
        Assert.Empty(harness.Port.RestockLayerCalls);
        (OrganizeNoticePlacement Placement, string Key, object?[] Args) notice = Assert.Single(harness.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.TopLeft, notice.Placement);
        Assert.Equal("message.restock.summary", notice.Key);
        Assert.Equal(new object?[] { "Nothing to restock" }, notice.Args);
        Assert.Equal(new[] { "Nothing to restock" }, InfoLines(harness));
    }

    /// <summary>GW-U-2615: a base whose only chest is in use by another player, with 30 items left, gives the left-behind line.</summary>
    [Fact]
    public void RR12_NothingToRestockWithOnlyAnInUseChestLeavesThirty()
    {
        var inUse = new Dictionary<BaseStorage.Core.Snapshots.ContainerId, string> { { Id(801), "Erik" } };
        RunHarness harness = Harness(chests: new[] { Chest801() }, keep803: false, inUse: inUse);
        harness.Port.Carried = new List<CarriedStack> { H(S("Wood", 10)) };

        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));

        Assert.Equal(new[] { "Nothing restocked; 30 items left behind" }, Summaries(harness));
        Assert.Equal(Summaries(harness), InfoLines(harness));
        Assert.Empty(harness.Port.RestockLayerCalls);
    }
}
