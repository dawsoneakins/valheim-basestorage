namespace BaseStorage.Plugin.Reservations;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;

/// <summary>
/// The game-free adapter that drives the reservation state machine with the vanilla open handshake (contract S-E005-002
/// section 3). It reads game facts through <see cref="IReservationGame{TChest}"/>, asks <see cref="ChestAccessRules"/> what
/// they mean, and applies the answer to one <see cref="ChestReservations{TOperation}"/>. Every transition goes through one
/// choke point, and every release clear through one helper, so the Debug lines and the RA8 rule have a single implementation.
/// </summary>
/// <typeparam name="TChest">The chest type of the game.</typeparam>
internal sealed class ReservationDriver<TChest>
    where TChest : class
{
    /// <summary>The operation label of the reservation probe (a Debug label, not user-facing text).</summary>
    internal const string ProbeOperation = "probe";

    private readonly IReservationGame<TChest> game;
    private readonly IClock clock;
    private readonly StringTable strings;
    private readonly ILogSink? sink;
    private readonly int reserveTimeoutMs;
    private readonly ChestReservations<string> reservations;
    private readonly Dictionary<ContainerId, Entry> entries = new Dictionary<ContainerId, Entry>();
    private readonly Dictionary<ContainerId, TimeSpan> late = new Dictionary<ContainerId, TimeSpan>();
    private readonly List<ContainerId> scratch = new List<ContainerId>();

    /// <summary>Creates a driver over a game port, a clock and a log sink.</summary>
    /// <param name="game">The game port.</param>
    /// <param name="clock">The only time source.</param>
    /// <param name="reserveTimeoutMs">The reservation timeout; values below 1 are read as 1.</param>
    /// <param name="strings">The strings table for the holder label.</param>
    /// <param name="sink">The Debug log sink, or null for no Debug lines.</param>
    internal ReservationDriver(IReservationGame<TChest> game, IClock clock, int reserveTimeoutMs, StringTable strings, ILogSink? sink)
    {
        if (game is null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (clock is null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (strings is null)
        {
            throw new ArgumentNullException(nameof(strings));
        }

        this.game = game;
        this.clock = clock;
        this.strings = strings;
        this.sink = sink;
        this.reserveTimeoutMs = Math.Max(1, reserveTimeoutMs);
        reservations = new ChestReservations<string>(clock, this.reserveTimeoutMs);
    }

    /// <summary>Gets the normalised reservation timeout in milliseconds.</summary>
    internal int ReserveTimeoutMs => reserveTimeoutMs;

    /// <summary>Gets the number of non-terminal reservations.</summary>
    internal int ActiveCount
    {
        get
        {
            int count = 0;
            foreach (Entry entry in entries.Values)
            {
                if (!entry.Reservation.IsTerminal)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>Gets the number of entries in the late-response set, including expired entries not yet purged.</summary>
    internal int LateCount => late.Count;

    /// <summary>Begins a reservation of a chest for an operation.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="operation">The operation label.</param>
    /// <returns>The route taken, or null when a non-terminal reservation already exists.</returns>
    internal ReservationRoute? Begin(TChest chest, string operation)
    {
        if (chest is null)
        {
            throw new ArgumentNullException(nameof(chest));
        }

        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException("The operation must not be empty or whitespace-only.", nameof(operation));
        }

        if (!game.TryGetId(chest, out ContainerId id))
        {
            return ReservationRoute.SkipInvalid;
        }

        if (entries.TryGetValue(id, out Entry? existing) && !existing.Reservation.IsTerminal)
        {
            Log(ReservationMessages.AlreadyReserved(id, operation));
            return null;
        }

        ChestObservation observation = game.Observe(chest);
        ReservationRoute route = ChestAccessRules.Route(observation);
        Reservation<string> reservation = reservations.Begin(id, operation)!;
        var entry = new Entry(chest, route, reservation);
        entries[id] = entry;

        Log(ReservationMessages.Route(id, operation, route));
        if (route == ReservationRoute.SkipInUse)
        {
            Log(ReservationMessages.InUse(id, operation, ChestAccessRules.HolderLabel(observation.OwnerName, strings)));
        }

        switch (route)
        {
            case ReservationRoute.SendRequest:
                game.SendOpenRequest(chest);
                entry.RequestSent = true;
                break;
            case ReservationRoute.HostClaim:
                game.ClaimOwnership(chest);
                MarkInUse(entry);
                Step(entry, ReservationEvent.Grant);
                break;
            case ReservationRoute.LocalOwner:
                MarkInUse(entry);
                Step(entry, ReservationEvent.Grant);
                break;
            case ReservationRoute.SelfOpen:
                Step(entry, ReservationEvent.Grant);
                break;
            default:
                Step(entry, ChestAccessRules.EventForRoute(route) ?? ReservationEvent.OwnerLost);
                break;
        }

        return route;
    }

    /// <summary>Finds the stored reservation of a chest.</summary>
    /// <param name="chest">The chest id.</param>
    /// <returns>The stored reservation, terminal or not, or null.</returns>
    internal Reservation<string>? Find(ContainerId chest) => reservations.Find(chest);

    /// <summary>Gets the route of a stored reservation.</summary>
    /// <param name="chest">The chest id.</param>
    /// <returns>The route, or null when nothing is stored.</returns>
    internal ReservationRoute? RouteOf(ContainerId chest) =>
        entries.TryGetValue(chest, out Entry? entry) ? entry.Route : (ReservationRoute?)null;

    /// <summary>Queries whether another player has a chest open. A pure read.</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>The in-use result, or null when the chest is not in use by another player.</returns>
    internal ChestInUse? QueryInUse(TChest chest)
    {
        if (chest is null)
        {
            throw new ArgumentNullException(nameof(chest));
        }

        if (!game.TryGetId(chest, out ContainerId id))
        {
            return null;
        }

        return ChestAccessRules.InUseBy(id, game.Observe(chest), strings);
    }

    /// <summary>Starts writing to a granted reservation.</summary>
    /// <param name="chest">The chest id.</param>
    /// <returns>True when the transition was accepted.</returns>
    internal bool BeginWrite(ContainerId chest) =>
        entries.TryGetValue(chest, out Entry? entry)
        && entry.Reservation.State == ReservationState.Granted
        && Step(entry, ReservationEvent.BeginWrite);

    /// <summary>Tells whether a caller may write to a chest right now (RA2). Changes nothing.</summary>
    /// <param name="chest">The chest id.</param>
    /// <returns>True only in the Writing state with a valid view that the local peer owns and the UI does not hold.</returns>
    internal bool CanWrite(ContainerId chest) =>
        entries.TryGetValue(chest, out Entry? entry)
        && entry.Reservation.State == ReservationState.Writing
        && game.IsViewValid(entry.Chest)
        && game.IsOwner(entry.Chest)
        && (entry.Route == ReservationRoute.SelfOpen || !game.IsOpenLocally(entry.Chest));

    /// <summary>Releases a reservation. Synchronous and idempotent.</summary>
    /// <param name="chest">The chest id.</param>
    /// <returns>True when at least one transition was applied.</returns>
    internal bool Release(ContainerId chest)
    {
        if (!entries.TryGetValue(chest, out Entry? entry) || entry.Reservation.IsTerminal)
        {
            return false;
        }

        if (entry.Reservation.State == ReservationState.Requesting)
        {
            Step(entry, ReservationEvent.Cancel);
            if (entry.RequestSent)
            {
                AddLate(chest, clock.Elapsed);
            }

            return true;
        }

        if (entry.Reservation.State != ReservationState.Releasing)
        {
            Step(entry, ReservationEvent.BeginRelease);
        }

        ClearIfHeld(entry);
        Step(entry, ReservationEvent.CompleteRelease);
        return true;
    }

    /// <summary>Advances every reservation by one frame. Called from the plugin update.</summary>
    internal void Tick()
    {
        if (ActiveCount == 0 && late.Count == 0)
        {
            return;
        }

        if (!game.InWorld)
        {
            foreach (Entry entry in entries.Values)
            {
                if (!entry.Reservation.IsTerminal)
                {
                    Step(entry, ReservationEvent.RequesterDisconnected);
                }
            }

            late.Clear();
            return;
        }

        IReadOnlyList<Reservation<string>> active = reservations.Active;
        for (int i = 0; i < active.Count; i++)
        {
            PollOne(entries[active[i].Chest]);
        }

        IReadOnlyList<Reservation<string>> timedOut = reservations.CheckTimeouts();
        TimeSpan now = clock.Elapsed;
        for (int i = 0; i < timedOut.Count; i++)
        {
            Entry entry = entries[timedOut[i].Chest];
            LogTransition(entry, timedOut[i].FailedFrom ?? ReservationState.Requesting);
            if (timedOut[i].FailedFrom == ReservationState.Requesting)
            {
                if (entry.RequestSent)
                {
                    AddLate(entry.Reservation.Chest, now);
                }
            }
            else
            {
                ClearIfHeld(entry);
            }
        }

        for (int i = 0; i < active.Count; i++)
        {
            ProbeOne(active[i], now);
        }

        PurgeLate(now);
    }

    /// <summary>Consumes an open response that the game is about to process.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="granted">True when the owner granted the request.</param>
    /// <returns>False when the response was consumed and vanilla must not run; true when vanilla runs.</returns>
    internal bool OnOpenResponse(TChest chest, bool granted)
    {
        if (!game.TryGetId(chest, out ContainerId id))
        {
            return true;
        }

        if (entries.TryGetValue(id, out Entry? entry) && !entry.Reservation.IsTerminal && entry.RequestSent)
        {
            Log(ReservationMessages.ResponseConsumed(id, entry.Reservation.Operation, granted));
            if (entry.Reservation.State == ReservationState.Requesting)
            {
                ReservationEvent? response = ChestAccessRules.EventForResponse(granted);
                if (response.HasValue)
                {
                    Step(entry, response.Value);
                }
                else
                {
                    entry.GrantReceived = true;
                    if (game.IsOwner(chest))
                    {
                        MarkInUse(entry);
                        Step(entry, ReservationEvent.Grant);
                    }
                }
            }

            return false;
        }

        if (late.TryGetValue(id, out TimeSpan until))
        {
            late.Remove(id);
            if (clock.Elapsed < until)
            {
                Log(ReservationMessages.LateResponseConsumed(id, granted));
                return false;
            }
        }

        return true;
    }

    /// <summary>Handles a local interaction with a chest.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="hold">True when the interaction is a hold.</param>
    /// <param name="alt">True when the alternative (Shift) interaction is used.</param>
    /// <param name="probeEnabled">True when the reservation probe is enabled.</param>
    /// <returns>False when the probe handled the interaction; true when vanilla runs.</returns>
    internal bool OnLocalInteract(TChest chest, bool hold, bool alt, bool probeEnabled)
    {
        if (hold || !game.TryGetId(chest, out ContainerId id))
        {
            return true;
        }

        if (probeEnabled && alt)
        {
            Begin(chest, ProbeOperation);
            return false;
        }

        late.Remove(id);
        if (entries.TryGetValue(id, out Entry? entry) && !entry.Reservation.IsTerminal)
        {
            if (entry.Reservation.State == ReservationState.Requesting)
            {
                Step(entry, ReservationEvent.Cancel);
            }
            else
            {
                ClearIfHeld(entry);
                Step(entry, ReservationEvent.InUseByOther);
            }
        }

        return true;
    }

    private void PollOne(Entry entry)
    {
        Reservation<string> reservation = entry.Reservation;
        bool openedLocally = entry.Route != ReservationRoute.SelfOpen && game.IsOpenLocally(entry.Chest);
        ReservationEvent? poll = ChestAccessRules.Poll(
            reservation.State,
            game.IsViewValid(entry.Chest),
            game.IsOwner(entry.Chest),
            entry.GrantReceived,
            openedLocally);
        if (!poll.HasValue)
        {
            return;
        }

        if (poll.Value == ReservationEvent.Grant)
        {
            MarkInUse(entry);
            Step(entry, ReservationEvent.Grant);
            return;
        }

        bool wasHolding = reservation.HoldsChest;
        Step(entry, poll.Value);
        if (wasHolding && reservation.State == ReservationState.Failed)
        {
            ClearIfHeld(entry);
        }
    }

    private void ProbeOne(Reservation<string> reservation, TimeSpan now)
    {
        if (!string.Equals(reservation.Operation, ProbeOperation, StringComparison.Ordinal))
        {
            return;
        }

        if (reservation.State == ReservationState.Granted)
        {
            BeginWrite(reservation.Chest);
        }
        else if (reservation.State == ReservationState.Writing
            && now - reservation.StartedAt >= TimeSpan.FromTicks(reservation.Timeout.Ticks / 2))
        {
            Release(reservation.Chest);
        }
    }

    private void AddLate(ContainerId chest, TimeSpan now) =>
        late[chest] = now + ChestAccessRules.LateResponseWindow(reserveTimeoutMs);

    private void PurgeLate(TimeSpan now)
    {
        if (late.Count == 0)
        {
            return;
        }

        TimeSpan horizon = now + ChestAccessRules.LateResponseWindow(reserveTimeoutMs);
        scratch.Clear();
        foreach (KeyValuePair<ContainerId, TimeSpan> pair in late)
        {
            if (pair.Value <= now || pair.Value > horizon)
            {
                scratch.Add(pair.Key);
            }
        }

        for (int i = 0; i < scratch.Count; i++)
        {
            late.Remove(scratch[i]);
        }

        scratch.Clear();
    }

    private void MarkInUse(Entry entry)
    {
        game.SetInUse(entry.Chest, true);
        entry.InUseSet = true;
    }

    private void ClearIfHeld(Entry entry)
    {
        bool clear = ChestAccessRules.ShouldClearInUse(
            entry.InUseSet,
            game.IsViewValid(entry.Chest),
            game.IsOwner(entry.Chest),
            game.IsOpenLocally(entry.Chest));
        if (clear)
        {
            game.SetInUse(entry.Chest, false);
            entry.InUseSet = false;
        }
    }

    private bool Step(Entry entry, ReservationEvent trigger)
    {
        ReservationState from = entry.Reservation.State;
        if (!entry.Reservation.Apply(trigger))
        {
            return false;
        }

        LogTransition(entry, from);
        return true;
    }

    private void LogTransition(Entry entry, ReservationState from)
    {
        if (!LogPolicy.IsEnabled(sink, LogCategory.Detail))
        {
            return;
        }

        Reservation<string> reservation = entry.Reservation;
        LogPolicy.Write(
            sink,
            LogCategory.Detail,
            ReservationMessages.Transition(reservation.Chest, reservation.Operation, from, reservation.State, reservation.Failure));
    }

    private void Log(string message)
    {
        if (LogPolicy.IsEnabled(sink, LogCategory.Detail))
        {
            LogPolicy.Write(sink, LogCategory.Detail, message);
        }
    }

    private sealed class Entry
    {
        internal Entry(TChest chest, ReservationRoute route, Reservation<string> reservation)
        {
            Chest = chest;
            Route = route;
            Reservation = reservation;
        }

        internal TChest Chest { get; }

        internal ReservationRoute Route { get; }

        internal Reservation<string> Reservation { get; }

        internal bool RequestSent { get; set; }

        internal bool GrantReceived { get; set; }

        internal bool InUseSet { get; set; }
    }
}
