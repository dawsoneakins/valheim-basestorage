namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize;

/// <summary>
/// The shared ownership authority of the S-E005-004 simulation (ADR-GWIN-114): the vanilla facts of ADR 0002 reduced to a model.
/// Every chest has one owner session at a time and at most one holder (a peer that was granted, or that has the chest open in the
/// vanilla UI). A request is answered on a later tick after a random latency: a holder's in-use gate denies it, a request that
/// reached a stale or disconnected owner is never answered (the requester times out), the owner may deny for access, otherwise the
/// requester becomes the owner and the holder. Ownership is never handed back on release; the server may move it away (lost).
/// The authority asserts, on every write, that the writer is the sole holder. Not a test row.
/// </summary>
internal sealed class ReservationAuthority
{
    /// <summary>The session id of the non-simulated player that owns every chest at the start and receives ownership the server moves away.</summary>
    internal const long GhostOwner = 999_000L;

    private readonly Dictionary<ContainerId, ChestOwnership> chests = new Dictionary<ContainerId, ChestOwnership>();
    private readonly List<ContainerId> chestOrder = new List<ContainerId>();
    private readonly FuzzRandom random;
    private readonly int peerCount;
    private readonly int maxLatencyTicks;
    private readonly int denyPct;
    private readonly int reassignDelayTicks;
    private readonly List<string> log;
    private readonly long[] sessions;
    private int tick;
    private int serial;

    /// <summary>Creates the authority for a number of peers.</summary>
    /// <param name="random">The schedule's random stream; latency and access denials are drawn from it.</param>
    /// <param name="peerCount">The number of simulated peers, 1..3.</param>
    /// <param name="maxLatencyTicks">The largest response latency in ticks; at least 1.</param>
    /// <param name="denyPct">The probability in percent that a reachable owner denies for access.</param>
    /// <param name="reassignDelayTicks">The ticks the server takes to move ownership away from a disconnected holder.</param>
    /// <param name="log">The event log the authority appends to.</param>
    internal ReservationAuthority(FuzzRandom random, int peerCount, int maxLatencyTicks, int denyPct, int reassignDelayTicks, List<string> log)
    {
        this.random = random;
        this.peerCount = peerCount;
        this.maxLatencyTicks = Math.Max(1, maxLatencyTicks);
        this.denyPct = denyPct;
        this.reassignDelayTicks = Math.Max(1, reassignDelayTicks);
        this.log = log;
        sessions = new long[peerCount];
        for (int peer = 0; peer < peerCount; peer++)
        {
            sessions[peer] = SessionOf(peer);
        }
    }

    /// <summary>How a holder holds a chest.</summary>
    internal enum HoldKind
    {
        /// <summary>Nobody holds the chest.</summary>
        None = 0,

        /// <summary>A granted BaseStorage reservation.</summary>
        Reservation = 1,

        /// <summary>The vanilla inventory UI of the holder is open on the chest.</summary>
        UiOpen = 2,
    }

    /// <summary>Gets the number of grants, including the local-owner and self-open fast paths.</summary>
    internal int Grants { get; private set; }

    /// <summary>Gets the number of grants that needed no network request (the requester already owned the chest).</summary>
    internal int LocalOwnerGrants { get; private set; }

    /// <summary>Gets the number of grants on a chest the requester had open in the UI.</summary>
    internal int SelfOpenGrants { get; private set; }

    /// <summary>Gets the number of denials, from the pre-check and from the owner.</summary>
    internal int Denials { get; private set; }

    /// <summary>Gets the number of requests that were never answered.</summary>
    internal int NoResponses { get; private set; }

    /// <summary>Gets the number of granted reservations the server took away.</summary>
    internal int LostHolds { get; private set; }

    /// <summary>Gets the number of vanilla UI opens.</summary>
    internal int UiOpens { get; private set; }

    /// <summary>Gets the number of peer disconnects.</summary>
    internal int Disconnects { get; private set; }

    /// <summary>Gets the number of writes the authority checked.</summary>
    internal int Writes { get; private set; }

    /// <summary>Gets the hold and write ledger the authority fills.</summary>
    internal ExclusivityLedger Ledger { get; } = new ExclusivityLedger();

    /// <summary>Gets the chests in registration order.</summary>
    internal IReadOnlyList<ContainerId> Chests => chestOrder;

    /// <summary>The session id of a peer.</summary>
    /// <param name="peer">The peer index.</param>
    internal static long SessionOf(int peer) => 1001L + peer;

    /// <summary>The session id a peer currently plays under: <see cref="SessionOf"/> until it rejoins, a new id after each rejoin.</summary>
    /// <param name="peer">The peer index.</param>
    internal long CurrentSessionOf(int peer) => sessions[peer];

    /// <summary>A disconnected peer comes back as a new session: it owns nothing and has no reservation.</summary>
    /// <param name="peer">The peer index.</param>
    /// <returns>The new session id.</returns>
    internal long Rejoin(int peer)
    {
        sessions[peer] += 10L;
        Log("rejoin", peer, default, "session " + sessions[peer].ToString(CultureInfo.InvariantCulture));
        return sessions[peer];
    }

    /// <summary>Registers a chest, owned by the ghost player and held by nobody.</summary>
    /// <param name="chest">The chest id.</param>
    internal void Register(ContainerId chest)
    {
        chests.Add(chest, new ChestOwnership(peerCount));
        chestOrder.Add(chest);
    }

    /// <summary>Moves to a tick: delivers the responses due, closes UIs, and finishes ownership reassignments.</summary>
    /// <param name="now">The tick.</param>
    internal void Tick(int now)
    {
        tick = now;
        foreach (ContainerId id in chestOrder)
        {
            ChestOwnership chest = chests[id];
            if (chest.OwnerAwayUntil >= 0 && chest.OwnerAwayUntil <= now)
            {
                chest.OwnerAwayUntil = -1;
                chest.Owner = GhostOwner;
                chest.OwnerSerial++;
                Log("owner-back", -1, id, string.Empty);
            }

            if (chest.UiCloseAt >= 0 && chest.UiCloseAt <= now && chest.Kind == HoldKind.UiOpen)
            {
                if (chest.SelfOpenReserved)
                {
                    chest.UiCloseAt = now + 1;
                }
                else
                {
                    Log("ui-close", chest.Holder, id, string.Empty);
                    CloseHold(id, chest);
                    chest.Holder = -1;
                    chest.Kind = HoldKind.None;
                    chest.UiCloseAt = -1;
                }
            }

            DeliverResponses(id, chest, now);
        }
    }

    /// <summary>Starts a request: a no-op when pending or granted, otherwise pending with the answer on a later tick (or at once on a fast path).</summary>
    /// <param name="peer">The requesting peer.</param>
    /// <param name="chest">The chest.</param>
    internal void Request(int peer, ContainerId chest)
    {
        ChestOwnership state = chests[chest];
        ReservationStatus current = state.Status[peer];
        if (current == ReservationStatus.Pending || current == ReservationStatus.Granted)
        {
            return;
        }

        state.Status[peer] = ReservationStatus.Pending;
        if (state.Holder == peer && state.Kind == HoldKind.UiOpen)
        {
            state.SelfOpenReserved = true;
            state.Status[peer] = ReservationStatus.Granted;
            Grants++;
            SelfOpenGrants++;
            Log("grant-self-open", peer, chest, string.Empty);
            return;
        }

        if (state.Holder >= 0 && state.Holder != peer)
        {
            state.Status[peer] = ReservationStatus.Denied;
            Denials++;
            Log("deny-precheck", peer, chest, "holder " + SimulationItems.PeerNames[state.Holder]);
            return;
        }

        if (state.Owner == sessions[peer])
        {
            Grant(peer, chest, state, "grant-local-owner");
            LocalOwnerGrants++;
            return;
        }

        int latency = random.Next(1, maxLatencyTicks + 1);
        state.Pending.Add(new PendingRequest(peer, tick + latency, serial++, state.OwnerSerial));
        Log("request", peer, chest, "latency " + latency.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Gets the status of a peer's reservation on a chest.</summary>
    /// <param name="peer">The peer.</param>
    /// <param name="chest">The chest.</param>
    internal ReservationStatus Status(int peer, ContainerId chest) => chests[chest].Status[peer];

    /// <summary>Releases a peer's reservation: cancels a pending request, clears the hold when the peer holds it by reservation, keeps ownership.</summary>
    /// <param name="peer">The peer.</param>
    /// <param name="chest">The chest.</param>
    internal void Release(int peer, ContainerId chest)
    {
        ChestOwnership state = chests[chest];
        ReservationStatus previous = state.Status[peer];
        state.Status[peer] = ReservationStatus.None;
        state.Pending.RemoveAll(request => request.Peer == peer);
        if (previous == ReservationStatus.Granted)
        {
            if (state.Holder == peer && state.Kind == HoldKind.Reservation)
            {
                CloseHold(chest, state);
                state.Holder = -1;
                state.Kind = HoldKind.None;
                state.HoldSerial++;
            }

            state.SelfOpenReserved = false;
            Log("release", peer, chest, string.Empty);
        }
    }

    /// <summary>Checks that a peer may write a chest now: it is granted and it is the sole holder. Records nothing; a write that succeeds is recorded with <see cref="RecordWrite"/>.</summary>
    /// <param name="peer">The writing peer.</param>
    /// <param name="chest">The chest.</param>
    /// <returns>True when the peer is granted; false when the write must come back NotReserved.</returns>
    internal bool AllowWrite(int peer, ContainerId chest)
    {
        ChestOwnership state = chests[chest];
        if (state.Status[peer] != ReservationStatus.Granted)
        {
            return false;
        }

        Assert.True(state.Holder == peer, Describe(chest) + ": peer " + peer + " writes while the holder is " + state.Holder);
        for (int other = 0; other < peerCount; other++)
        {
            Assert.True(
                other == peer || state.Status[other] != ReservationStatus.Granted,
                Describe(chest) + ": peers " + peer + " and " + other + " are granted at the same time");
        }

        return true;
    }

    /// <summary>Records a write that changed a chest: counts it, feeds the ledger and asserts that one peer wrote inside the hold.</summary>
    /// <param name="peer">The writing peer; it must have passed <see cref="AllowWrite"/> for this write.</param>
    /// <param name="chest">The chest.</param>
    internal void RecordWrite(int peer, ContainerId chest)
    {
        ChestOwnership state = chests[chest];
        Writes++;
        state.Writers.Add(peer);
        Assert.True(state.Writers.Count == 1, Describe(chest) + ": two peers wrote inside one hold");
        Ledger.Write(peer, chest, tick);
    }

    /// <summary>The peers whose request for a chest is waiting for the owner's answer.</summary>
    /// <param name="chest">The chest.</param>
    internal IReadOnlyList<int> PendingPeersOn(ContainerId chest)
    {
        var peers = new List<int>();
        foreach (PendingRequest request in chests[chest].Pending)
        {
            if (!peers.Contains(request.Peer))
            {
                peers.Add(request.Peer);
            }
        }

        return peers;
    }

    /// <summary>Counts the chests a peer holds, by reservation or by an open vanilla UI.</summary>
    /// <param name="peer">The peer.</param>
    internal int HoldsOf(int peer)
    {
        int count = 0;
        foreach (ContainerId id in chestOrder)
        {
            count += chests[id].Holder == peer ? 1 : 0;
        }

        return count;
    }

    /// <summary>Closes the open vanilla UI holds at the end of a schedule, passes every unreleased reservation to the ledger and asserts it.</summary>
    /// <param name="now">The tick the schedule ended at.</param>
    internal void FinishLedger(int now)
    {
        foreach (ContainerId id in chestOrder)
        {
            ChestOwnership state = chests[id];
            if (state.Holder < 0)
            {
                continue;
            }

            Ledger.Hold(state.Holder, id, state.HoldSince, state.Kind == HoldKind.UiOpen ? now : null);
            state.Holder = -1;
            state.Kind = HoldKind.None;
        }

        Ledger.AssertExclusive();
    }

    /// <summary>Tells whether another peer holds the chest (its in-use flag is set by a listed player who is not the observer).</summary>
    /// <param name="peer">The observing peer.</param>
    /// <param name="chest">The chest.</param>
    internal bool IsInUseByOther(int peer, ContainerId chest)
    {
        ChestOwnership state = chests[chest];
        return state.Holder >= 0 && state.Holder != peer;
    }

    /// <summary>Gets the holder of a chest, or -1.</summary>
    /// <param name="chest">The chest.</param>
    internal int HolderOf(ContainerId chest) => chests[chest].Holder;

    /// <summary>Gets how the chest is held.</summary>
    /// <param name="chest">The chest.</param>
    internal HoldKind KindOf(ContainerId chest) => chests[chest].Kind;

    /// <summary>Gets the owner session of a chest.</summary>
    /// <param name="chest">The chest.</param>
    internal long OwnerOf(ContainerId chest) => chests[chest].Owner;

    /// <summary>The server moves ownership away from the chest (the holder left the area, or the view unloaded): a granted holder loses it.</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>True when a granted reservation was lost.</returns>
    internal bool LoseOwner(ContainerId chest)
    {
        ChestOwnership state = chests[chest];
        if (state.Kind == HoldKind.UiOpen)
        {
            return false;
        }

        state.Owner = GhostOwner;
        state.OwnerSerial++;
        if (state.Holder >= 0)
        {
            int holder = state.Holder;
            CloseHold(chest, state);
            state.Status[holder] = ReservationStatus.Lost;
            state.Holder = -1;
            state.Kind = HoldKind.None;
            state.HoldSerial++;
            LostHolds++;
            Log("owner-lost", holder, chest, string.Empty);
            return true;
        }

        Log("owner-moved", -1, chest, string.Empty);
        return false;
    }

    /// <summary>The ghost owner becomes unreachable for a while: requests routed to it are never answered.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="ticks">How long, in ticks.</param>
    /// <returns>True when the chest was owned by the reachable ghost and is now unreachable.</returns>
    internal bool SetOwnerAway(ContainerId chest, int ticks)
    {
        ChestOwnership state = chests[chest];
        if (state.Owner != GhostOwner || state.OwnerAwayUntil >= 0 || state.Holder >= 0)
        {
            return false;
        }

        state.OwnerAwayUntil = tick + Math.Max(1, ticks);
        Log("owner-away", -1, chest, ticks.ToString(CultureInfo.InvariantCulture) + " ticks");
        return true;
    }

    /// <summary>A peer opens a chest in the vanilla UI: it becomes the owner and the holder until the UI closes.</summary>
    /// <param name="peer">The peer.</param>
    /// <param name="chest">The chest.</param>
    /// <param name="ticks">How long the UI stays open.</param>
    /// <returns>True when the chest was free and is now open.</returns>
    internal bool OpenUi(int peer, ContainerId chest, int ticks)
    {
        ChestOwnership state = chests[chest];
        if (state.Holder >= 0 || state.Status[peer] != ReservationStatus.None || state.OwnerAwayUntil >= 0)
        {
            return false;
        }

        TakeOwnership(state, sessions[peer]);
        state.Holder = peer;
        state.Kind = HoldKind.UiOpen;
        state.HoldSerial++;
        state.HoldSince = tick;
        state.Writers.Clear();
        state.UiCloseAt = tick + Math.Max(1, ticks);
        UiOpens++;
        Log("ui-open", peer, chest, ticks.ToString(CultureInfo.InvariantCulture) + " ticks");
        return true;
    }

    /// <summary>A peer disconnects: its requests and holds vanish, and the server reassigns the chests it held after a delay.</summary>
    /// <param name="peer">The peer.</param>
    internal void DisconnectPeer(int peer)
    {
        Disconnects++;
        foreach (ContainerId id in chestOrder)
        {
            ChestOwnership state = chests[id];
            state.Pending.RemoveAll(request => request.Peer == peer);
            state.Status[peer] = ReservationStatus.None;
            if (state.Holder == peer)
            {
                CloseHold(id, state);
                state.Holder = -1;
                state.Kind = HoldKind.None;
                state.HoldSerial++;
                state.SelfOpenReserved = false;
                state.UiCloseAt = -1;
            }

            if (state.Owner == sessions[peer])
            {
                state.OwnerAwayUntil = tick + reassignDelayTicks;
            }
        }

        Log("disconnect", peer, default, string.Empty);
    }

    /// <summary>Tells whether the peer has any reservation that is not None.</summary>
    /// <param name="peer">The peer.</param>
    internal bool HasAnyReservation(int peer)
    {
        foreach (ContainerId id in chestOrder)
        {
            if (chests[id].Status[peer] != ReservationStatus.None)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Asserts the exclusivity invariant on every chest: at most one granted peer, and that peer is the holder.</summary>
    internal void AssertExclusive()
    {
        foreach (ContainerId id in chestOrder)
        {
            ChestOwnership state = chests[id];
            int granted = -1;
            for (int peer = 0; peer < peerCount; peer++)
            {
                if (state.Status[peer] != ReservationStatus.Granted)
                {
                    continue;
                }

                Assert.True(granted < 0, Describe(id) + ": peers " + granted + " and " + peer + " are granted at the same time");
                Assert.True(state.Holder == peer, Describe(id) + ": peer " + peer + " is granted but the holder is " + state.Holder);
                granted = peer;
            }
        }
    }

    private static string Describe(ContainerId chest) => "chest #" + chest.Id.ToString(CultureInfo.InvariantCulture);

    private void DeliverResponses(ContainerId id, ChestOwnership chest, int now)
    {
        if (chest.Pending.Count == 0)
        {
            return;
        }

        chest.Pending.Sort((a, b) => a.DueTick != b.DueTick ? a.DueTick.CompareTo(b.DueTick) : a.Serial.CompareTo(b.Serial));
        while (chest.Pending.Count > 0 && chest.Pending[0].DueTick <= now)
        {
            PendingRequest request = chest.Pending[0];
            chest.Pending.RemoveAt(0);
            if (chest.Status[request.Peer] != ReservationStatus.Pending)
            {
                continue;
            }

            if (chest.OwnerAwayUntil >= 0)
            {
                NoResponses++;
                Log("no-response", request.Peer, id, "owner away");
                continue;
            }

            // The in-use gate runs on the chest's current state, so a request overtaken by another holder is denied, not lost.
            if (chest.Holder >= 0 && chest.Holder != request.Peer)
            {
                chest.Status[request.Peer] = ReservationStatus.Denied;
                Denials++;
                Log("deny-in-use", request.Peer, id, "holder " + SimulationItems.PeerNames[chest.Holder]);
                continue;
            }

            if (request.OwnerSerial != chest.OwnerSerial)
            {
                NoResponses++;
                Log("no-response", request.Peer, id, "stale owner");
                continue;
            }

            if (random.Chance(denyPct))
            {
                chest.Status[request.Peer] = ReservationStatus.Denied;
                Denials++;
                Log("deny-access", request.Peer, id, string.Empty);
                continue;
            }

            Grant(request.Peer, id, chest, "grant");
        }
    }

    private void Grant(int peer, ContainerId id, ChestOwnership chest, string what)
    {
        Assert.True(chest.Holder < 0, Describe(id) + ": granting peer " + peer + " while peer " + chest.Holder + " holds it");
        TakeOwnership(chest, sessions[peer]);
        chest.Holder = peer;
        chest.Kind = HoldKind.Reservation;
        chest.HoldSerial++;
        chest.HoldSince = tick;
        chest.Writers.Clear();
        chest.Status[peer] = ReservationStatus.Granted;
        Grants++;
        Log(what, peer, id, string.Empty);
    }

    private static void TakeOwnership(ChestOwnership chest, long session)
    {
        if (chest.Owner != session)
        {
            chest.Owner = session;
            chest.OwnerSerial++;
        }
    }

    private void CloseHold(ContainerId id, ChestOwnership chest)
    {
        if (chest.Holder >= 0)
        {
            Ledger.Hold(chest.Holder, id, chest.HoldSince, tick);
        }
    }

    private void Log(string what, int peer, ContainerId chest, string detail)
    {
        string line = "t" + tick.ToString(CultureInfo.InvariantCulture) + " " + what
            + (peer >= 0 ? " " + SimulationItems.PeerNames[peer] : string.Empty)
            + (chest.IsNone ? string.Empty : " #" + chest.Id.ToString(CultureInfo.InvariantCulture))
            + (detail.Length == 0 ? string.Empty : " " + detail);
        log.Add(line);
    }

    private sealed record PendingRequest(int Peer, int DueTick, int Serial, int OwnerSerial);

    private sealed class ChestOwnership
    {
        internal ChestOwnership(int peers)
        {
            Status = new ReservationStatus[peers];
        }

        internal long Owner { get; set; } = GhostOwner;

        internal int OwnerSerial { get; set; }

        internal int Holder { get; set; } = -1;

        internal HoldKind Kind { get; set; }

        internal int HoldSerial { get; set; }

        internal int HoldSince { get; set; }

        internal bool SelfOpenReserved { get; set; }

        internal int OwnerAwayUntil { get; set; } = -1;

        internal int UiCloseAt { get; set; } = -1;

        internal ReservationStatus[] Status { get; }

        internal List<PendingRequest> Pending { get; } = new List<PendingRequest>();

        internal HashSet<int> Writers { get; } = new HashSet<int>();
    }
}
