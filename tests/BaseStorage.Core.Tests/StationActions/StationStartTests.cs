namespace BaseStorage.Core.Tests.StationActions;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.StationPanel;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Locking;
using BaseStorage.Core.Tests.Organize;
using BaseStorage.Core.Tests.Organize.Running;
using BaseStorage.Core.Tests.StationPanel;

/// <summary>
/// GW-U-1772..1777 (AC-2, AC-3, contract 9.4): a station start drives the existing organize coordinator (RunHarness, base E, Erik as
/// holder). GW-U-1772..1776 drive only existing Core and runner code and may be green on arrival. GW-U-1777 reads the holder through
/// StationActionsFacts and is red until it exists.
/// </summary>
public sealed class StationStartTests
{
    private static readonly StationId S7 = RunHarness.Station.Id;

    // GW-U-1772 (AC-2, 9.4): a station start opens the preview, writes Erik's lock until 3630 s, and moves nothing.
    [Fact]
    public void AStationStartOpensThePreviewAndMovesNothing()
    {
        RunHarness h = RunHarness.Make();
        ContainerSnapshot[] before = h.Input.Containers.ToArray();

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(BaseStart.FromStation(S7)));

        Assert.Equal(OrganizeRunState.Previewing, h.Coordinator.State);
        (string header, string body) = Assert.Single(h.Port.Previews);
        Assert.Equal("Organize base", header);
        Assert.StartsWith("6 stacks to move across ", body, StringComparison.Ordinal);
        Assert.Equal(new[] { RunHarness.Erik3630 }, h.StoreValues());
        LockFixtures.AssertSingleWriteTo(h.Store, S7);
        Assert.Empty(h.Port.Notices);
        Assert.Equal(0, h.AddCalls());
        Assert.Empty(h.Port.LayerCalls);
        Assert.Equal(before, h.Input.Containers.ToArray());
    }

    // GW-U-1773 (AC-2, 9.4): Cancel moves nothing: the run is idle, the lock is cleared, the cancelled notice shows, and every chest is unchanged.
    [Fact]
    public void CancelAfterAStationStartMovesNothing()
    {
        RunHarness h = RunHarness.Make();
        ContainerSnapshot[] before = h.Input.Containers.ToArray();
        h.Coordinator.Start(BaseStart.FromStation(S7));

        Assert.True(h.Coordinator.Cancel());

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Empty(h.StoreValues());
        Assert.Equal(new[] { (OrganizeNoticePlacement.TopLeft, "message.organize.cancelled") }, h.Port.Notices.Select(n => (n.Placement, n.Key)).ToArray());
        Assert.Empty(h.SummaryNotices());
        ConservationAssert.Holds(before, h.Input.Containers);
        Assert.Equal(before, h.Input.Containers.ToArray());
        Assert.Equal(0, h.AddCalls());
        Assert.Empty(h.Port.LayerCalls);
    }

    // GW-U-1774 (AC-2, 9.4): Confirm runs the plan to idle with the organize-runner 8 row 6 end state and one summary.
    [Fact]
    public void ConfirmAfterAStationStartRunsToTheCompleteSummary()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 2);
        h.Coordinator.Start(BaseStart.FromStation(S7));
        Assert.True(h.Coordinator.Confirm());

        h.RunToIdle(2);

        Assert.Equal(6, h.AddCalls());
        Assert.Empty(h.StoreValues());
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.SummaryNotices());
        Assert.Equal(OrganizeNoticePlacement.TopLeft, placement);
        Assert.Equal("message.organize.run-summary", key);
        Assert.Equal(new object?[] { RunHarness.CompleteSummary }, args);
    }

    // GW-U-1775 (AC-3, 9.4): with OrganizeSkipPreview the station start runs at once and shows no preview.
    [Fact]
    public void SkipPreviewStartsTheRunWithoutAPanel()
    {
        RunHarness h = RunHarness.Make(skipPreview: true);

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(BaseStart.FromStation(S7)));

        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.Empty(h.Port.Previews);
    }

    // GW-U-1776 (AC-2, 9.4): a station that is not in the snapshot source is not in a base: one centre notice and no lock write.
    [Fact]
    public void AStationOutsideTheSourceIsNotInABase()
    {
        RunHarness h = RunHarness.Make();

        Assert.Equal(OrganizeStartOutcome.NotInBase, h.Coordinator.Start(BaseStart.FromStation(new StationId(OrganizeFixtures.UserId, 99u))));

        Assert.Equal(new[] { (OrganizeNoticePlacement.Centre, "message.base.not-in-base") }, h.Port.Notices.Select(n => (n.Placement, n.Key)).ToArray());
        Assert.Equal(0, h.Store.WriteCount);
        Assert.Empty(h.StoreValues());
    }

    // GW-U-1777 (AC-4, SA14, 9.4): after the own start, the holder is Erik and the panel disables the button with the holder tooltip.
    [Fact]
    public void AnOwnRunDisablesTheButtonWithTheHolderTooltip()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Start(BaseStart.FromStation(S7));
        BaseResolution resolution = h.Cache.Resolve(BaseStart.FromStation(S7), new BaseCacheOptions(0, 64));

        string? holder = (string?)StationActionsTypes.CallStatic(
            "StationActionsFacts", "HolderName", resolution.Stations, h.Store, h.Clock.Elapsed, 30);
        Assert.Equal("Erik", holder);

        StationPanelState state = StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, holder: holder));
        Assert.False(state.ButtonsEnabled);
        Assert.Equal("Organize in progress by Erik", state.TooltipText);
    }
}
