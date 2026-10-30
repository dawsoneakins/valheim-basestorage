namespace BaseStorage.Core.Organize.Run;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;

/// <summary>
/// Runs one Organize from start to summary (contract section 4.1): resolves the base once, gathers the facts once, plans once,
/// takes the base lock, shows the preview unless the setting skips it, steps the executor once per tick while renewing the lock,
/// and ends every run with one summary notice and one Info line. Every branch of a run is taken here; the Plugin forwards the
/// hotkey, the popup buttons and the frame tick through <see cref="IOrganizeRunPort"/>. Not thread-safe; main thread only.
/// </summary>
public sealed class OrganizeRunCoordinator
{
    private const int RenewDivisor = 3;
    private static readonly object?[] NoArgs = Array.Empty<object?>();

    private readonly IOrganizeRunPort port;
    private readonly BaseCache cache;
    private readonly OrganizeLock locks;
    private readonly IClock clock;
    private readonly BaseStorageSettings settings;
    private readonly ILogSink? sink;

    private OrganizeRunState state;
    private OrganizePlan? plan;
    private DepositPlan? deposit;
    private IReadOnlyList<StationSnapshot>? stations;
    private long holderId;
    private string holderName = string.Empty;
    private OrganizeExecutor? executor;
    private TimeSpan renewInterval;
    private TimeSpan lastRenew;

    /// <summary>Creates a coordinator. The constructor reads nothing from the port, the cache, the store, the clock or the settings.</summary>
    /// <param name="port">The game port.</param>
    /// <param name="cache">The single base cache; the base of a start is resolved through it once per run.</param>
    /// <param name="lockStore">The per-station lock store the base lock writes through.</param>
    /// <param name="clock">The only time source.</param>
    /// <param name="settings">The settings read per run.</param>
    /// <param name="sink">The log sink of the one Info summary line; null writes no line.</param>
    /// <exception cref="ArgumentNullException">An argument other than <paramref name="sink"/> is null; ParamName names it.</exception>
    public OrganizeRunCoordinator(
        IOrganizeRunPort port,
        BaseCache cache,
        IOrganizeLockStore lockStore,
        IClock clock,
        BaseStorageSettings settings,
        ILogSink? sink)
    {
        this.port = port ?? throw new ArgumentNullException(nameof(port));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        if (lockStore is null)
        {
            throw new ArgumentNullException(nameof(lockStore));
        }

        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.sink = sink;
        locks = new OrganizeLock(lockStore, clock, settings);
    }

    /// <summary>Gets the current state.</summary>
    public OrganizeRunState State => state;

    /// <summary>Gets the plan of the active run; null while idle.</summary>
    public OrganizePlan? Plan => plan;

    /// <summary>
    /// Gets the holder name carried by the in-progress notice of the last Start or StartDeposit that returned AlreadyActive or LockRefused;
    /// null after any Start or StartDeposit that returned another outcome.
    /// </summary>
    public string? RefusedBy { get; private set; }

    /// <summary>Starts a run for the base that contains a start (contract 4.1 Start). Every refusal shows its notice and leaves the state idle.</summary>
    /// <param name="start">A station or position start.</param>
    /// <returns>What happened.</returns>
    /// <exception cref="ArgumentException"><paramref name="start"/> has no defined kind.</exception>
    public OrganizeStartOutcome Start(BaseStart start) => StartRun(start, asDeposit: false);

    /// <summary>Starts a Deposit (put away inventory) for the base that contains a start (DB2). Every refusal shows its notice and leaves the state idle.</summary>
    /// <param name="start">A station or position start.</param>
    /// <returns>What happened.</returns>
    /// <exception cref="ArgumentException"><paramref name="start"/> has no defined kind.</exception>
    public OrganizeStartOutcome StartDeposit(BaseStart start) => StartRun(start, asDeposit: true);

    private OrganizeStartOutcome StartRun(BaseStart start, bool asDeposit)
    {
        if (start.Kind != BaseStartKind.Station && start.Kind != BaseStartKind.Position)
        {
            throw new ArgumentException("The start has no defined kind.", nameof(start));
        }

        RefusedBy = null;
        if (state != OrganizeRunState.Idle)
        {
            RefusedBy = holderName;
            Notice(OrganizeNoticePlacement.Centre, StringKeys.MessageBaseInProgress, holderName);
            return OrganizeStartOutcome.AlreadyActive;
        }

        long id = port.HolderId;
        if (!port.InWorld || id == 0)
        {
            return OrganizeStartOutcome.NoPlayer;
        }

        string name = port.HolderName;
        BaseResolution resolution = cache.Resolve(start, new BaseCacheOptions(settings.BaseCacheSeconds, settings.MaxBaseStations));
        if (!resolution.IsInBase)
        {
            Notice(OrganizeNoticePlacement.Centre, StringKeys.MessageBaseNotInBase);
            return OrganizeStartOutcome.NotInBase;
        }

        OrganizeBaseFacts facts = port.GatherFacts(resolution.Containers);
        DepositPlan? madeDeposit = null;
        OrganizePlan made;
        int minLayouts;
        if (asDeposit)
        {
            PlayerInventorySnapshot? inventory = port.CaptureInventory();
            if (inventory is null)
            {
                return OrganizeStartOutcome.NoPlayer;
            }

            madeDeposit = DepositPlanner.Plan(
                inventory,
                DepositExclusions.FromSettings(settings, port.Favourites),
                resolution.Containers,
                facts.Assignments,
                facts.InUseBy,
                port.Catalog,
                port.ItemKinds,
                EligibilityOptions.FromSettings(settings),
                settings.AutoAssignThresholdPct);
            made = madeDeposit.Plan;
            minLayouts = 2;
        }
        else
        {
            made = OrganizePlanner.Plan(
                resolution.Containers,
                facts.Assignments,
                facts.InUseBy,
                port.Catalog,
                port.ItemKinds,
                EligibilityOptions.FromSettings(settings),
                settings.AutoAssignThresholdPct,
                settings.AutoAssignMixedChests);
            minLayouts = 1;
        }

        if (made.Layouts.Count < minLayouts)
        {
            Notice(OrganizeNoticePlacement.Centre, StringKeys.StationActionsDisabledNoChests);
            return OrganizeStartOutcome.NoChests;
        }

        OrganizeLockResult acquired = locks.Acquire(resolution.Stations, id, name);
        if (!acquired.IsGranted)
        {
            RefusedBy = acquired.HolderName;
            Notice(OrganizeNoticePlacement.Centre, StringKeys.MessageBaseInProgress, acquired.HolderName);
            return OrganizeStartOutcome.LockRefused;
        }

        plan = made;
        deposit = madeDeposit;
        stations = resolution.Stations;
        holderId = id;
        holderName = name;
        try
        {
            if (asDeposit ? !settings.DepositPreview : settings.OrganizeSkipPreview)
            {
                BeginRun();
            }
            else
            {
                state = OrganizeRunState.Previewing;
                StringTable table = StringTable.English;
                string header = table.Get(asDeposit ? StringKeys.DepositPreviewHeader : StringKeys.OrganizePreviewHeader);
                port.ShowPreview(header, OrganizeSummaryFormatter.PreviewBody(made, port.Catalog));
            }
        }
        catch (Exception)
        {
            AbandonBeforeRunning();
            throw;
        }

        return OrganizeStartOutcome.Started;
    }

    /// <summary>Confirms the preview: re-acquires the lock and begins the run, or refuses when another holder took the base meanwhile.</summary>
    /// <returns>True when the input was consumed (the state was Previewing); false otherwise.</returns>
    public bool Confirm()
    {
        if (state != OrganizeRunState.Previewing)
        {
            return false;
        }

        OrganizeLockResult acquired;
        try
        {
            acquired = locks.Acquire(stations!, holderId, holderName);
        }
        catch (Exception)
        {
            AbandonBeforeRunning();
            throw;
        }

        if (!acquired.IsGranted)
        {
            Notice(OrganizeNoticePlacement.Centre, StringKeys.MessageBaseInProgress, acquired.HolderName);
            Reset();
            return true;
        }

        try
        {
            BeginRun();
        }
        catch (Exception)
        {
            AbandonBeforeRunning();
            throw;
        }

        return true;
    }

    /// <summary>Cancels the preview: releases the lock and shows the cancelled notice.</summary>
    /// <returns>True when the input was consumed (the state was Previewing); false otherwise.</returns>
    public bool Cancel()
    {
        if (state != OrganizeRunState.Previewing)
        {
            return false;
        }

        try
        {
            locks.Release(stations!, holderId);
            Notice(OrganizeNoticePlacement.TopLeft, deposit is null ? StringKeys.MessageOrganizeCancelled : StringKeys.MessageDepositCancelled);
        }
        finally
        {
            Reset();
        }

        return true;
    }

    /// <summary>Does one frame of work (contract 4.1 Tick): nothing while idle; one executor step and the due lock renewal while running.</summary>
    public void Tick()
    {
        if (state == OrganizeRunState.Idle)
        {
            return;
        }

        if (!port.InWorld)
        {
            executor?.Abort();
            Reset();
            return;
        }

        if (state != OrganizeRunState.Running)
        {
            return;
        }

        try
        {
            StepRun();
        }
        catch (Exception ex)
        {
            sink?.Log(LogSeverity.Error, "Organize run failed: " + ex.ToString().Replace('\n', ' ').Replace('\r', ' '));
            FailRun();
        }
    }

    private void StepRun()
    {
        TimeSpan now = clock.Elapsed;
        if (now < lastRenew || now - lastRenew >= renewInterval)
        {
            OrganizeLockResult renewed = locks.Renew(stations!, holderId, holderName);
            if (!renewed.IsGranted)
            {
                sink?.Log(LogSeverity.Warning, "Organize lock renewal refused; run stopped");
                executor!.Abort();
                Finish(OrganizeRunText.LockLostKey, releaseLock: false);
                return;
            }

            lastRenew = now;
        }

        executor!.Step();
        if (executor.IsComplete)
        {
            Finish(null, releaseLock: true);
        }
    }

    /// <summary>Ends a run whose tick threw: aborts the executor, releases the lock once and shows the one summary (never throws).</summary>
    private void FailRun()
    {
        if (state != OrganizeRunState.Running || executor is null)
        {
            Reset();
            return;
        }

        try
        {
            executor.Abort();
        }
        catch (Exception)
        {
            // The run is already failed; the summary below still has to be written.
        }

        try
        {
            locks.Release(stations!, holderId);
        }
        catch (Exception)
        {
            // The lock expires on its own if the store is failing.
        }

        try
        {
            Finish(null, releaseLock: false);
        }
        catch (Exception)
        {
            Reset();
        }
    }

    /// <summary>Ends a run that failed after the lock was taken and before it reached Running: releases the lock (best effort) and returns to Idle. Writes no summary (RN5).</summary>
    private void AbandonBeforeRunning()
    {
        try
        {
            if (stations is not null)
            {
                locks.Release(stations, holderId);
            }
        }
        catch (Exception)
        {
            // The lock expires on its own if the store is failing.
        }

        Reset();
    }

    private void BeginRun()
    {
        IContainerLayer layer = deposit is null ? port.CreateLayer(plan!) : port.CreateDepositLayer(deposit);
        executor = new OrganizeExecutor(plan!, layer, clock, settings);
        int seconds = Math.Max(1, settings.OrganizeLockSeconds);
        renewInterval = TimeSpan.FromTicks(seconds * TimeSpan.TicksPerSecond / RenewDivisor);
        lastRenew = clock.Elapsed;
        state = OrganizeRunState.Running;
    }

    private void SaveRules()
    {
        try
        {
            port.SaveCategoryRules(plan!.AutoAssign.MixedAssigned);
        }
        catch (Exception ex)
        {
            // The items have already moved, so a failed save must not fail the run.
            sink?.Log(LogSeverity.Error, "Saving chest categories failed: " + ex.ToString().Replace('\n', ' ').Replace('\r', ' '));
        }
    }

    private void Finish(string? reasonKey, bool releaseLock)
    {
        if (releaseLock)
        {
            locks.Release(stations!, holderId);
        }

        OrganizeExecutionResult result = executor!.Result;
        bool stopped = result.Status == ExecutionStatus.Stopped;
        string? reason = stopped ? StringTable.English.Get(reasonKey ?? OrganizeRunText.StopReasonKey(result.StopReason)) : null;
        string key;
        object?[] args;
        string text;
        if (deposit is null)
        {
            string summary = OrganizeSummaryFormatter.Summary(OrganizeRunReport.FromPlan(plan!, result.StacksMoved, stopped, reason));
            key = StringKeys.MessageOrganizeRunSummary;
            args = new object?[] { summary };
            text = summary;
        }
        else
        {
            int noRoom = deposit.Plan.NoRoom.Count;
            key = stopped ? StringKeys.MessageDepositSummaryStopped : StringKeys.MessageDepositSummary;
            args = stopped
                ? new object?[] { result.StacksMoved, noRoom, reason }
                : new object?[] { result.StacksMoved, noRoom };
            text = SingleLine.Of(StringTable.English.Format(key, args));
        }

        port.ShowNotice(OrganizeNoticePlacement.TopLeft, key, args);
        if (deposit is null && !stopped && plan!.AutoAssign.MixedAssigned.Count > 0)
        {
            SaveRules();
        }
        LogPolicy.Write(sink, LogCategory.OrganizeSummary, text);
        Reset();
    }

    private void Reset()
    {
        state = OrganizeRunState.Idle;
        plan = null;
        deposit = null;
        stations = null;
        executor = null;
    }

    private void Notice(OrganizeNoticePlacement placement, string key) => port.ShowNotice(placement, key, NoArgs);

    private void Notice(OrganizeNoticePlacement placement, string key, string? argument) =>
        port.ShowNotice(placement, key, new object?[] { argument });
}
