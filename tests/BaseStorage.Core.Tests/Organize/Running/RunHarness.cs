namespace BaseStorage.Core.Tests.Organize.Running;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Bases;
using BaseStorage.Core.Tests.Locking;
using BaseStorage.Core.Tests.Logging;
using BaseStorage.Core.Tests.Organize.Execution;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// The run coordinator harness of contract organize-runner-contracts.md section 8: base E of the S-E004-006 fixtures under one
/// station, a <see cref="BaseCache"/> over a counting snapshot source, a manual clock at 3600 s, the lock store fake, a recording
/// sink and a <see cref="FakeOrganizeRunPort"/>. Every expected text and encoded lock value is a literal. Not a test row.
/// </summary>
internal sealed class RunHarness
{
    /// <summary>The station of the fixture base; its build range of 20 m covers every chest of base E (x from -4 to 14).</summary>
    public static readonly StationSnapshot Station = new StationSnapshot(new StationId(OrganizeFixtures.UserId, 7u), new WorldPosition(5f, 30f, 0f), 20f);

    /// <summary>A start inside the station's build range.</summary>
    public static readonly BaseStart Inside = BaseStart.FromPosition(new WorldPosition(5f, 30f, 1f));

    /// <summary>A start far away from the station.</summary>
    public static readonly BaseStart Outside = BaseStart.FromPosition(new WorldPosition(500f, 30f, 0f));

    /// <summary>Erik's lock written at 3600 s with a 30 s duration.</summary>
    public const string Erik3630 = "L1;1923487561234;36300000000;Erik";

    /// <summary>The summary of a complete run over base E (execution contract 5.1: 5 stacks moved).</summary>
    public const string CompleteSummary = "Organized 5 stacks; 1 chest skipped (in use by Erik); 1 chest skipped (Keep); 1 chest skipped (not eligible); 2 stacks had no room";

    /// <summary>The summary when the first move fails on a full chest 202.</summary>
    public const string FullSummary = "Nothing moved; stopped early: chest full; 1 chest skipped (in use by Erik); 1 chest skipped (Keep); 1 chest skipped (not eligible); 2 stacks had no room";

    private RunHarness(
        OrganizeFixtures.PlanInput input,
        FakeOrganizeRunPort port,
        CountingSnapshotSource source,
        BaseCache cache,
        FakeOrganizeLockStore store,
        BaseStorage.Core.Tests.Reservations.ManualClock clock,
        FakeSettingsSource settingsSource,
        RecordingSink sink,
        OrganizeRunCoordinator coordinator)
    {
        Input = input;
        Port = port;
        Source = source;
        Cache = cache;
        Store = store;
        Clock = clock;
        SettingsSource = settingsSource;
        Sink = sink;
        Coordinator = coordinator;
    }

    /// <summary>Gets the plan input the facts and the layer come from.</summary>
    public OrganizeFixtures.PlanInput Input { get; }

    /// <summary>Gets the recording port.</summary>
    public FakeOrganizeRunPort Port { get; }

    /// <summary>Gets the snapshot source behind the cache.</summary>
    public CountingSnapshotSource Source { get; }

    /// <summary>Gets the base cache.</summary>
    public BaseCache Cache { get; }

    /// <summary>Gets the lock store fake.</summary>
    public FakeOrganizeLockStore Store { get; }

    /// <summary>Gets the manual clock, starting at 3600 s.</summary>
    public BaseStorage.Core.Tests.Reservations.ManualClock Clock { get; }

    /// <summary>Gets the settings source.</summary>
    public FakeSettingsSource SettingsSource { get; }

    /// <summary>Gets the recording log sink.</summary>
    public RecordingSink Sink { get; }

    /// <summary>Gets the coordinator under test.</summary>
    public OrganizeRunCoordinator Coordinator { get; }

    /// <summary>Builds a harness over base E (or another input) with the section 8 settings.</summary>
    /// <param name="input">The base; E by default.</param>
    /// <param name="movesPerFrame">The MovesPerFrame value.</param>
    /// <param name="skipPreview">The OrganizeSkipPreview value.</param>
    /// <param name="lockSeconds">The OrganizeLockSeconds value.</param>
    /// <param name="withSink">False builds the coordinator with a null sink.</param>
    /// <param name="depositPreview">The DepositPreview value (S-E004-015, default false).</param>
    /// <param name="autoAssignMixedChests">The AutoAssignMixedChests value (S-A-001, default false: the v0.1 Organize plan).</param>
    /// <returns>A new harness.</returns>
    // Edited by S-E004-015 (ADR-GWIN-143): the depositPreview option, read only by a Deposit (DB3).
    // Edited by S-A-001 (mixed-chest-category-contracts.md 7.1): the autoAssignMixedChests option, default false.
    public static RunHarness Make(OrganizeFixtures.PlanInput? input = null, int movesPerFrame = 2, bool skipPreview = false, int lockSeconds = 30, bool withSink = true, bool depositPreview = false, bool autoAssignMixedChests = false)
    {
        input ??= OrganizeFixtures.E();
        var source = new CountingSnapshotSource();
        source.Stations.Add(Station);
        source.Containers.AddRange(input.Containers);
        var clock = new BaseStorage.Core.Tests.Reservations.ManualClock(TimeSpan.FromSeconds(3600));
        var cache = new BaseCache(source, clock);
        var store = new FakeOrganizeLockStore();
        FakeSettingsSource settingsSource = new FakeSettingsSource()
            .WithInt(SettingKeys.MovesPerFrame, movesPerFrame)
            .WithInt(SettingKeys.ReserveTimeoutMs, 1500)
            .WithInt(SettingKeys.OrganizeLockSeconds, lockSeconds)
            .WithInt(SettingKeys.AutoAssignThresholdPct, OrganizeFixtures.DefaultThreshold)
            .WithBool(SettingKeys.OrganizeSkipPreview, skipPreview)
            .WithBool(SettingKeys.DepositPreview, depositPreview)
            .WithBool(SettingKeys.AutoAssignMixedChests, autoAssignMixedChests);
        var port = new FakeOrganizeRunPort(input);
        var sink = new RecordingSink();
        var coordinator = new OrganizeRunCoordinator(port, cache, store, clock, new BaseStorageSettings(settingsSource), withSink ? sink : null);
        return new RunHarness(input, port, source, cache, store, clock, settingsSource, sink, coordinator);
    }

    /// <summary>Ticks until the coordinator is idle, asserting that no tick adds more than the bound to a chest.</summary>
    /// <param name="bound">The MovesPerFrame value in force.</param>
    /// <param name="maxTicks">The tick count after which the run is a failure.</param>
    /// <returns>The number of ticks taken.</returns>
    public int RunToIdle(int bound, int maxTicks = 200)
    {
        int ticks = 0;
        while (Coordinator.State != OrganizeRunState.Idle)
        {
            Assert.True(ticks < maxTicks, "The run did not end within " + maxTicks + " ticks.");
            int before = AddCalls();
            Coordinator.Tick();
            Assert.True(AddCalls() - before <= Math.Max(1, bound), "One tick added " + (AddCalls() - before) + " stacks, above the bound " + bound + ".");
            ticks++;
        }

        return ticks;
    }

    /// <summary>The number of AddItems calls the recorder saw so far.</summary>
    /// <returns>The count; 0 before a layer exists.</returns>
    public int AddCalls() => Port.Recorder is null ? 0 : Port.Recorder.Calls.Count(line => line.StartsWith("Add ", StringComparison.Ordinal));

    /// <summary>The summary notices shown so far.</summary>
    /// <returns>The notices with the run-summary key.</returns>
    public List<(OrganizeNoticePlacement Placement, string Key, object?[] Args)> SummaryNotices() =>
        Port.Notices.Where(n => n.Key == "message.organize.run-summary").ToList();

    /// <summary>The values the store holds, as one literal per station in id order.</summary>
    /// <returns>The encoded values.</returns>
    public string[] StoreValues() => Store.Contents().Select(entry => entry.Value).ToArray();
}
