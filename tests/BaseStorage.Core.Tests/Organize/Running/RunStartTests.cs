namespace BaseStorage.Core.Tests.Organize.Running;

using System;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Locking;
using BaseStorage.Core.Tests.Organize.Execution;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// GW-U-1350..1358 (AC-2, AC-3, contract 4.1 Start and section 8 rows 1-4, 7, 8, 14): every refusal of Start shows its one notice
/// and leaves the coordinator idle, a start inside the base plans once, takes the lock and shows the preview, and the skip-preview
/// setting runs at once.
/// </summary>
public sealed class RunStartTests
{
    // GW-U-1350 (row 1): outside every base.
    [Fact]
    public void OutsideABaseShowsNotInABaseAndStaysIdle()
    {
        RunHarness h = RunHarness.Make();

        OrganizeStartOutcome outcome = h.Coordinator.Start(RunHarness.Outside);

        Assert.Equal(OrganizeStartOutcome.NotInBase, outcome);
        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Equal(new[] { (OrganizeNoticePlacement.Centre, "message.base.not-in-base") }, h.Port.Notices.Select(n => (n.Placement, n.Key)).ToArray());
        Assert.Empty(h.Port.Previews);
        Assert.Empty(h.Port.GatherCalls);
        Assert.Empty(h.Store.Calls);
        Assert.Null(h.Coordinator.Plan);
    }

    // GW-U-1351 (row 2): no local player, and no world.
    [Theory]
    [InlineData(true, 0L)]
    [InlineData(false, 1923487561234L)]
    public void NoPlayerOrNoWorldShowsNothing(bool inWorld, long holderId)
    {
        RunHarness h = RunHarness.Make();
        h.Port.InWorld = inWorld;
        h.Port.HolderId = holderId;

        Assert.Equal(OrganizeStartOutcome.NoPlayer, h.Coordinator.Start(RunHarness.Inside));
        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Empty(h.Port.Notices);
        Assert.Empty(h.Port.Previews);
        Assert.Equal(0, h.Source.ContainerReads);
        Assert.Empty(h.Store.Calls);
    }

    // GW-U-1352 (row 3, AC-5.4): a foreign live lock refuses with the holder's name and writes nothing.
    [Fact]
    public void AForeignLiveLockRefusesWithTheHolderName()
    {
        RunHarness h = RunHarness.Make();
        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);

        Assert.Equal(OrganizeStartOutcome.LockRefused, h.Coordinator.Start(RunHarness.Inside));
        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("message.base.in-progress", key);
        Assert.Equal(new object?[] { "Astrid" }, args);
        LockFixtures.AssertNoWrite(h.Store);
        Assert.Empty(h.Port.Previews);
        Assert.Equal(new[] { LockFixtures.Astrid3630 }, h.StoreValues());
    }

    // GW-U-1353 (row 4, AC-4.7, AC-4.4): inside the base: one gather, one plan, the lock, the preview.
    [Fact]
    public void InsideTheBasePlansOnceTakesTheLockAndShowsThePreview()
    {
        RunHarness h = RunHarness.Make();

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));

        Assert.Equal(OrganizeRunState.Previewing, h.Coordinator.State);
        Assert.Single(h.Port.GatherCalls);
        Assert.Equal(9, h.Port.GatherCalls[0].Count);
        Assert.NotNull(h.Coordinator.Plan);
        Assert.Equal(6, h.Coordinator.Plan!.Moves.Count);
        Assert.Equal(6, h.Coordinator.Plan.Layouts.Count);
        Assert.Equal(3, h.Coordinator.Plan.Skipped.Count);
        (string header, string body) = Assert.Single(h.Port.Previews);
        Assert.Equal("Organize base", header);
        Assert.StartsWith("6 stacks to move across ", body, StringComparison.Ordinal);
        Assert.EndsWith("2 stacks have no room. Some categories ran out of space. Add a chest or give one that category. 1 chest skipped (in use by Erik). 1 chest skipped (Keep). 1 chest skipped (not eligible).", body, StringComparison.Ordinal);
        Assert.Equal(new[] { RunHarness.Erik3630 }, h.StoreValues());
        LockFixtures.AssertSingleWriteTo(h.Store, RunHarness.Station.Id);
        Assert.Empty(h.Port.Notices);
        Assert.Empty(h.Port.LayerCalls);
        Assert.Empty(h.Sink.Entries);
    }

    // GW-U-1353: the base is resolved through the cache once per start, never per tick.
    [Fact]
    public void TheBaseIsReadOnceAndNeverPerTick()
    {
        RunHarness h = RunHarness.Make(skipPreview: true);
        h.Coordinator.Start(RunHarness.Inside);
        Assert.Equal(1, h.Source.ContainerReads);
        Assert.Equal(1, h.Source.StationReads);

        h.RunToIdle(2);

        Assert.Equal(1, h.Source.ContainerReads);
        Assert.Single(h.Port.GatherCalls);
        Assert.Single(h.Port.LayerCalls);
    }

    // GW-U-1354 (row 7): the skip-preview setting starts the run at once.
    [Fact]
    public void SkipPreviewRunsWithoutAPreview()
    {
        RunHarness h = RunHarness.Make(skipPreview: true);

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));

        Assert.Equal(OrganizeRunState.Running, h.Coordinator.State);
        Assert.Empty(h.Port.Previews);
        Assert.Single(h.Port.LayerCalls);
        Assert.Same(h.Coordinator.Plan, h.Port.LayerCalls[0]);
        Assert.Equal(new[] { RunHarness.Erik3630 }, h.StoreValues());
    }

    // GW-U-1355 (row 8): a second start while active names the own holder.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASecondStartWhileActiveIsRefusedWithTheOwnName(bool skipPreview)
    {
        RunHarness h = RunHarness.Make(skipPreview: skipPreview);
        h.Coordinator.Start(RunHarness.Inside);
        OrganizeRunState active = h.Coordinator.State;

        Assert.Equal(OrganizeStartOutcome.AlreadyActive, h.Coordinator.Start(RunHarness.Inside));

        Assert.Equal(active, h.Coordinator.State);
        (OrganizeNoticePlacement placement, string key, object?[] args) = Assert.Single(h.Port.Notices);
        Assert.Equal(OrganizeNoticePlacement.Centre, placement);
        Assert.Equal("message.base.in-progress", key);
        Assert.Equal(new object?[] { "Erik" }, args);
        Assert.Single(h.Port.GatherCalls);
        Assert.Equal(1, h.Store.WriteCount);
    }

    // GW-U-1356 (row 14): a base with no usable chest shows the no-chests notice and takes no lock.
    [Fact]
    public void ABaseWithoutUsableChestsShowsNoChestsAndTakesNoLock()
    {
        RunHarness h = RunHarness.Make(OrganizeFixtures.Make(
            new[] { OrganizeFixtures.CF(301, 5, 0, 2, ContainerFlags.Tombstone, OrganizeFixtures.S("Wood", 3)) }));

        Assert.Equal(OrganizeStartOutcome.NoChests, h.Coordinator.Start(RunHarness.Inside));

        Assert.Equal(OrganizeRunState.Idle, h.Coordinator.State);
        Assert.Equal(new[] { (OrganizeNoticePlacement.Centre, "station.actions.disabled-no-chests") }, h.Port.Notices.Select(n => (n.Placement, n.Key)).ToArray());
        Assert.Empty(h.Store.Calls);
        Assert.Empty(h.Port.Previews);
        Assert.Null(h.Coordinator.Plan);
    }

    // GW-U-1356: a station with no chest at all is the same refusal.
    [Fact]
    public void AStationWithoutChestsShowsNoChests()
    {
        RunHarness h = RunHarness.Make(OrganizeFixtures.Make(new ContainerSnapshot[0]));

        Assert.Equal(OrganizeStartOutcome.NoChests, h.Coordinator.Start(RunHarness.Inside));
        Assert.Empty(h.Store.Calls);
    }

    // GW-U-1357: a station start resolves the same base as the position start.
    [Fact]
    public void AStationStartPlansTheSameBase()
    {
        RunHarness h = RunHarness.Make();

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(BaseStart.FromStation(RunHarness.Station.Id)));
        Assert.Equal(6, h.Coordinator.Plan!.Moves.Count);
    }

    // GW-U-1358: argument validation and a reading-free constructor.
    [Fact]
    public void ConstructorValidatesAndReadsNothingAndAnUndefinedStartThrows()
    {
        RunHarness h = RunHarness.Make();
        var settings = new BaseStorageSettings(new FakeSettingsSource());
        var clock = new BaseStorage.Core.Tests.Locking.CountingClock();
        var store = new FakeOrganizeLockStore();

        Assert.Equal("port", Assert.Throws<ArgumentNullException>(() => new OrganizeRunCoordinator(null!, h.Cache, store, clock, settings, null)).ParamName);
        Assert.Equal("cache", Assert.Throws<ArgumentNullException>(() => new OrganizeRunCoordinator(h.Port, null!, store, clock, settings, null)).ParamName);
        Assert.Equal("lockStore", Assert.Throws<ArgumentNullException>(() => new OrganizeRunCoordinator(h.Port, h.Cache, null!, clock, settings, null)).ParamName);
        Assert.Equal("clock", Assert.Throws<ArgumentNullException>(() => new OrganizeRunCoordinator(h.Port, h.Cache, store, null!, settings, null)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new OrganizeRunCoordinator(h.Port, h.Cache, store, clock, null!, null)).ParamName);

        var fresh = new OrganizeRunCoordinator(h.Port, h.Cache, store, clock, settings, null);
        Assert.Equal(0, clock.Reads);
        Assert.Empty(store.Calls);
        Assert.Equal(OrganizeRunState.Idle, fresh.State);
        Assert.Equal("start", Assert.Throws<ArgumentException>(() => fresh.Start(default)).ParamName);
    }

    // GW-U-1358: the facts of the port reach the planner (the in-use chest 206 is skipped by Erik's name, 205 by Keep).
    [Fact]
    public void TheGatheredFactsReachThePlanner()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Start(RunHarness.Inside);

        var skipped = h.Coordinator.Plan!.Skipped.Select(s => (s.Id.Id, s.Kind, s.PlayerName)).ToArray();
        Assert.Contains((205u, BaseStorage.Core.Organize.SkipReason.Keep, (string?)null), skipped);
        Assert.Contains((206u, BaseStorage.Core.Organize.SkipReason.InUse, (string?)"Erik"), skipped);
        Assert.Contains((207u, BaseStorage.Core.Organize.SkipReason.Ineligible, (string?)null), skipped);
    }
}
