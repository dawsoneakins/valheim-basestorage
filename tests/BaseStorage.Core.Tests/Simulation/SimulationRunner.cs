namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Locking;
using BaseStorage.Core.Tests.Organize;
using BaseStorage.Core.Tests.Reservations;
using Xunit.Sdk;
using static BaseStorage.Core.Tests.Simulation.ScheduleGenerator;

/// <summary>
/// Runs one schedule of the S-E005-004 simulation (AC-5.5): two or three peers interleave crafts, Organizes and Deposits frame by
/// frame against one <see cref="SimulatedWorld"/>, one <see cref="ReservationAuthority"/>, one lock store and one clock, with random
/// grant latency, denials, unanswered requests, lost ownership, vanilla UI opens and peer disconnects. After every peer step the
/// authority's exclusivity invariant and the one-lock-holder rule are asserted; after every operation the writes are reconciled with
/// its result (all-or-nothing crafts, add-then-remove executors, every reservation released); after the schedule, conservation of
/// every item across every inventory, every chest and the consumed ledger, and untouched Keep and ineligible chests. Not a test row.
/// </summary>
internal static class SimulationRunner
{
    /// <summary>The frames between two lock renewals of a running Organize or Deposit (5 s at 20 frames per second).</summary>
    internal const int RenewEveryTicks = 100;

    /// <summary>The id of the virtual container that holds everything the crafts consumed, for the conservation check.</summary>
    internal static readonly ContainerId ConsumedLedgerId = new ContainerId(0L, 7u);

    /// <summary>What one schedule exercised, summed by the shard for its log line and non-vacuity checks.</summary>
    internal sealed class Stats
    {
        /// <summary>Gets or sets the schedules run.</summary>
        internal int Schedules { get; set; }

        /// <summary>Gets or sets the frames run.</summary>
        internal long Ticks { get; set; }

        /// <summary>Gets or sets the operations started.</summary>
        internal int Operations { get; set; }

        /// <summary>Gets or sets the crafts that consumed their materials.</summary>
        internal int CraftsCompleted { get; set; }

        /// <summary>Gets or sets the completed crafts that took from at least one chest.</summary>
        internal int CraftsFromChests { get; set; }

        /// <summary>Gets or sets the crafts whose materials were not available.</summary>
        internal int CraftsNotAvailable { get; set; }

        /// <summary>Gets or sets the crafts cancelled by a denied reservation.</summary>
        internal int CraftsDenied { get; set; }

        /// <summary>Gets or sets the crafts cancelled by a reservation timeout.</summary>
        internal int CraftsTimedOut { get; set; }

        /// <summary>Gets or sets the crafts cancelled by a lost reservation.</summary>
        internal int CraftsLost { get; set; }

        /// <summary>Gets or sets the crafts cancelled because a chest changed since the plan.</summary>
        internal int CraftsChanged { get; set; }

        /// <summary>Gets or sets the Organizes that completed every move.</summary>
        internal int OrganizesCompleted { get; set; }

        /// <summary>Gets or sets the Organizes the executor stopped early.</summary>
        internal int OrganizesStopped { get; set; }

        /// <summary>Gets or sets the Deposits that completed every move.</summary>
        internal int DepositsCompleted { get; set; }

        /// <summary>Gets or sets the Deposits the executor stopped early.</summary>
        internal int DepositsStopped { get; set; }

        /// <summary>Gets or sets the executor stops with a denied reservation.</summary>
        internal int StoppedDenied { get; set; }

        /// <summary>Gets or sets the executor stops with a reservation timeout.</summary>
        internal int StoppedTimedOut { get; set; }

        /// <summary>Gets or sets the executor stops with a lost reservation.</summary>
        internal int StoppedLost { get; set; }

        /// <summary>Gets or sets the executor stops with a changed chest.</summary>
        internal int StoppedChanged { get; set; }

        /// <summary>Gets or sets the executor stops with another reason.</summary>
        internal int StoppedOther { get; set; }

        /// <summary>Gets or sets the Organizes and Deposits refused the base lock.</summary>
        internal int LockRefusals { get; set; }

        /// <summary>Gets or sets the Organizes and Deposits aborted because the lock could not be renewed.</summary>
        internal int LockLost { get; set; }

        /// <summary>Gets or sets the granted lock renewals.</summary>
        internal int Renewals { get; set; }

        /// <summary>Gets or sets the Organizes and Deposits with nothing to move.</summary>
        internal int Nothing { get; set; }

        /// <summary>Gets or sets the operations dropped by a disconnect.</summary>
        internal int Crashes { get; set; }

        /// <summary>Gets or sets the authority's grants.</summary>
        internal int Grants { get; set; }

        /// <summary>Gets or sets the authority's self-open grants.</summary>
        internal int SelfOpenGrants { get; set; }

        /// <summary>Gets or sets the authority's denials.</summary>
        internal int Denials { get; set; }

        /// <summary>Gets or sets the requests never answered.</summary>
        internal int NoResponses { get; set; }

        /// <summary>Gets or sets the granted holds the server took away.</summary>
        internal int LostHolds { get; set; }

        /// <summary>Gets or sets the vanilla UI opens.</summary>
        internal int UiOpens { get; set; }

        /// <summary>Gets or sets the chest writes the authority checked.</summary>
        internal int Writes { get; set; }

        /// <summary>Gets or sets the items the executors moved.</summary>
        internal long ItemsMoved { get; set; }

        /// <summary>Gets or sets the items the crafts consumed.</summary>
        internal long ItemsConsumed { get; set; }

        /// <summary>Adds another schedule's stats into this one.</summary>
        /// <param name="other">The stats to add.</param>
        internal void Add(Stats other)
        {
            Schedules += other.Schedules;
            Ticks += other.Ticks;
            Operations += other.Operations;
            CraftsCompleted += other.CraftsCompleted;
            CraftsFromChests += other.CraftsFromChests;
            CraftsNotAvailable += other.CraftsNotAvailable;
            CraftsDenied += other.CraftsDenied;
            CraftsTimedOut += other.CraftsTimedOut;
            CraftsLost += other.CraftsLost;
            CraftsChanged += other.CraftsChanged;
            OrganizesCompleted += other.OrganizesCompleted;
            OrganizesStopped += other.OrganizesStopped;
            DepositsCompleted += other.DepositsCompleted;
            DepositsStopped += other.DepositsStopped;
            StoppedDenied += other.StoppedDenied;
            StoppedTimedOut += other.StoppedTimedOut;
            StoppedLost += other.StoppedLost;
            StoppedChanged += other.StoppedChanged;
            StoppedOther += other.StoppedOther;
            LockRefusals += other.LockRefusals;
            LockLost += other.LockLost;
            Renewals += other.Renewals;
            Nothing += other.Nothing;
            Crashes += other.Crashes;
            Grants += other.Grants;
            SelfOpenGrants += other.SelfOpenGrants;
            Denials += other.Denials;
            NoResponses += other.NoResponses;
            LostHolds += other.LostHolds;
            UiOpens += other.UiOpens;
            Writes += other.Writes;
            ItemsMoved += other.ItemsMoved;
            ItemsConsumed += other.ItemsConsumed;
        }

        /// <summary>One line with every counter, for the shard's output.</summary>
        internal string Describe() => string.Format(
            CultureInfo.InvariantCulture,
            "schedules={0} ticks={1} ops={2} crafts(ok={3} fromChests={4} n/a={5} denied={6} timeout={7} lost={8} changed={9}) "
            + "organize(ok={10} stopped={11}) deposit(ok={12} stopped={13}) stops(denied={14} timeout={15} lost={16} changed={17} other={18}) "
            + "lock(refused={19} lost={20} renewals={21}) nothing={22} crashes={23} "
            + "authority(grants={24} selfOpen={25} denials={26} noResponse={27} lostHolds={28} uiOpens={29} writes={30}) moved={31} consumed={32}",
            Schedules,
            Ticks,
            Operations,
            CraftsCompleted,
            CraftsFromChests,
            CraftsNotAvailable,
            CraftsDenied,
            CraftsTimedOut,
            CraftsLost,
            CraftsChanged,
            OrganizesCompleted,
            OrganizesStopped,
            DepositsCompleted,
            DepositsStopped,
            StoppedDenied,
            StoppedTimedOut,
            StoppedLost,
            StoppedChanged,
            StoppedOther,
            LockRefusals,
            LockLost,
            Renewals,
            Nothing,
            Crashes,
            Grants,
            SelfOpenGrants,
            Denials,
            NoResponses,
            LostHolds,
            UiOpens,
            Writes,
            ItemsMoved,
            ItemsConsumed);
    }

    /// <summary>The outcome of one schedule.</summary>
    /// <param name="Stats">What it exercised.</param>
    /// <param name="Log">The event log, one line per event.</param>
    /// <param name="Before">The world before the first frame.</param>
    /// <param name="After">The world after the last frame.</param>
    /// <param name="Consumed">The stacks the crafts consumed.</param>
    internal sealed record RunResult(Stats Stats, List<string> Log, List<ContainerSnapshot> Before, List<ContainerSnapshot> After, List<ItemStackSnapshot> Consumed);

    /// <summary>Runs a schedule and asserts every invariant. A failure carries the tail of the event log.</summary>
    /// <param name="config">The schedule.</param>
    /// <returns>The outcome.</returns>
    /// <exception cref="XunitException">An invariant failed.</exception>
    internal static RunResult Run(SimulationConfig config) => Run(config, MaxTicksPerSchedule, null);

    /// <summary>Runs a schedule with a frame cap and an optional hook that edits the world after the last frame, before the conservation check.</summary>
    /// <param name="config">The schedule.</param>
    /// <param name="maxTicks">The frame cap; a schedule still running at the cap fails as a livelock.</param>
    /// <param name="afterSchedule">The hook, or null.</param>
    /// <returns>The outcome.</returns>
    /// <exception cref="XunitException">An invariant failed.</exception>
    internal static RunResult Run(SimulationConfig config, int maxTicks, Action<SimulatedWorld>? afterSchedule)
    {
        var log = new List<string>();
        try
        {
            return new Session(config, log, maxTicks, afterSchedule).Run();
        }
        catch (XunitException failure)
        {
            throw new XunitException(failure.Message + "\nEvent log (last 120 of " + log.Count.ToString(CultureInfo.InvariantCulture) + " lines):\n"
                + string.Join("\n", log.Skip(Math.Max(0, log.Count - 120))));
        }
    }

    /// <summary>Crashes one peer mid-operation, lets it rejoin as a new peer, runs its next craft and reads back what the disconnect left.</summary>
    /// <param name="config">The schedule.</param>
    /// <param name="peer">The peer to crash.</param>
    /// <param name="during">The operation the peer is in when it dies.</param>
    /// <returns>The report.</returns>
    internal static SimulationProbe.DisconnectReport CrashAndRejoin(SimulationConfig config, int peer, OperationKind during) =>
        new Session(config, new List<string>(), MaxTicksPerSchedule, null).CrashAndRejoin(peer, during);

    /// <summary>Asserts that every item is conserved between two world states once the consumed stacks are counted as a virtual container.</summary>
    /// <param name="before">The world before.</param>
    /// <param name="after">The world after.</param>
    /// <param name="consumed">The stacks the crafts consumed.</param>
    internal static void AssertConserved(IReadOnlyList<ContainerSnapshot> before, IReadOnlyList<ContainerSnapshot> after, IReadOnlyList<ItemStackSnapshot> consumed)
    {
        var ledger = new List<ItemStackSnapshot>();
        foreach (KeyValuePair<string, long> pair in SimulationItems.Totals(consumed))
        {
            ItemStackSnapshot sample = consumed.First(stack => SimulationItems.KeyOf(stack) == pair.Key);
            ledger.Add(new ItemStackSnapshot(sample.PrefabName, (int)pair.Value, sample.Quality, int.MaxValue));
        }

        var afterWithLedger = new List<ContainerSnapshot>(after)
        {
            new ContainerSnapshot(ConsumedLedgerId, new WorldPosition(0f, 0f, 0f), 256, 256, ledger, ContainerFlags.None),
        };
        ConservationAssert.Holds(before, afterWithLedger);
    }

    private static bool PerMille(FuzzRandom random, int perMille) => perMille > 0 && random.Next(0, 1000) < perMille;

    private sealed class Peer
    {
        internal Peer(int index, long sessionId, SimulatedWorld world, FakeOrganizeLockStore store, ManualClock clock, BaseStorageSettings settings)
        {
            Index = index;
            SessionId = sessionId;
            Layer = new PeerContainerLayer(world, index);
            Lock = new OrganizeLock(store, clock, settings);
            Consumption = new ConsumptionExecutor(Layer, clock, settings);
        }

        internal int Index { get; }

        internal string Name => SimulationItems.PeerNames[Index];

        internal long SessionId { get; }

        internal PeerContainerLayer Layer { get; }

        internal OrganizeLock Lock { get; }

        internal ConsumptionExecutor Consumption { get; }

        internal PeerOperation? Current { get; set; }

        internal int OpsDone { get; set; }
    }

    private sealed class Session
    {
        private readonly SimulationConfig config;
        private readonly List<string> log;
        private readonly FuzzRandom random;
        private readonly ReservationAuthority authority;
        private readonly SimulatedWorld world;
        private readonly ManualClock clock;
        private readonly BaseStorageSettings settings;
        private readonly EligibilityOptions options;
        private readonly CategoryCatalog catalog;
        private readonly Dictionary<string, ItemKind> kinds;
        private readonly Peer[] peers;
        private readonly FakeOrganizeLockStore store;
        private readonly int maxTicks;
        private readonly Action<SimulatedWorld>? afterSchedule;
        private readonly HashSet<ContainerId> untouchable = new HashSet<ContainerId>();
        private readonly Stats stats = new Stats();
        private readonly List<ItemStackSnapshot> consumed = new List<ItemStackSnapshot>();
        private int tick;

        internal Session(SimulationConfig config, List<string> log, int maxTicks, Action<SimulatedWorld>? afterSchedule)
        {
            this.config = config;
            this.log = log;
            this.maxTicks = maxTicks;
            this.afterSchedule = afterSchedule;
            random = new FuzzRandom(config.RunSeed);
            authority = new ReservationAuthority(random, config.Peers, config.MaxLatencyTicks, config.DenyPct, config.ReassignDelayTicks, log);
            world = new SimulatedWorld(authority, config.Chests, config.Inventories);
            clock = new ManualClock(TimeSpan.FromSeconds(3600));
            settings = SimulationItems.Settings(config.MovesPerFrame);
            options = EligibilityOptions.FromSettings(settings);
            catalog = SimulationItems.Catalog();
            kinds = SimulationItems.Kinds();
            store = new FakeOrganizeLockStore();
            peers = new Peer[config.Peers];
            for (int i = 0; i < peers.Length; i++)
            {
                peers[i] = new Peer(i, authority.CurrentSessionOf(i), world, store, clock, settings);
            }
        }

        internal RunResult Run()
        {
            List<ContainerSnapshot> before = world.Capture();
            var untouchedChests = new Dictionary<ContainerId, ContainerSnapshot>();
            foreach (ContainerSnapshot chest in config.Chests)
            {
                bool keep = config.Assignments.TryGetValue(chest.Id, out CategoryAssignment? assignment) && assignment!.IsKeep;
                bool ineligible = (chest.Flags & ContainerFlags.Tombstone) != 0 || (chest.Flags & ContainerFlags.Vehicle) != 0;
                if (keep || ineligible)
                {
                    untouchedChests[chest.Id] = chest;
                    untouchable.Add(chest.Id);
                }
            }

            bool finished = false;
            var order = new int[peers.Length];
            for (tick = 0; tick < maxTicks && !finished; tick++)
            {
                clock.Advance(TimeSpan.FromMilliseconds(SimulationItems.TickMs));
                authority.Tick(tick);
                FireEvents();

                for (int i = 0; i < order.Length; i++)
                {
                    order[i] = i;
                }

                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = random.Next(0, i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }

                foreach (int index in order)
                {
                    Peer peer = peers[index];
                    if (peer.Current is null)
                    {
                        if (peer.OpsDone < config.OpsPerPeer && PerMille(random, config.StartPerMille))
                        {
                            StartOperation(peer);
                        }
                    }
                    else
                    {
                        int steps = random.Chance(25) ? 2 : 1;
                        for (int s = 0; s < steps && peer.Current is not null; s++)
                        {
                            peer.Current.Step();
                            if (peer.Current.IsDone)
                            {
                                FinishOperation(peer);
                            }
                        }
                    }

                    authority.AssertExclusive();
                    Assert.True(peers.Count(p => p.Current is not null && p.Current.HoldsLock) <= 1, "two peers hold the base lock at once");
                }

                finished = peers.All(p => p.Current is null && p.OpsDone >= config.OpsPerPeer);
            }

            Assert.True(
                finished,
                "livelock: seed " + Seed.ToString(CultureInfo.InvariantCulture) + ", schedule " + config.Index.ToString(CultureInfo.InvariantCulture)
                + " did not finish within " + maxTicks.ToString(CultureInfo.InvariantCulture) + " frames");
            stats.Schedules = 1;
            stats.Ticks = tick;
            stats.Grants = authority.Grants;
            stats.SelfOpenGrants = authority.SelfOpenGrants;
            stats.Denials = authority.Denials;
            stats.NoResponses = authority.NoResponses;
            stats.LostHolds = authority.LostHolds;
            stats.UiOpens = authority.UiOpens;
            stats.Writes = authority.Writes;

            foreach (Peer peer in peers)
            {
                Assert.False(authority.HasAnyReservation(peer.Index), peer.Name + " still has a reservation after the schedule");
            }

            authority.FinishLedger(tick);
            afterSchedule?.Invoke(world);
            List<ContainerSnapshot> after = world.Capture();
            AssertConserved(before, after, consumed);
            foreach (KeyValuePair<ContainerId, ContainerSnapshot> pair in untouchedChests)
            {
                Assert.True(pair.Value == world.Chest(pair.Key), "untouchable chest #" + pair.Key.Id.ToString(CultureInfo.InvariantCulture) + " changed");
            }

            return new RunResult(stats, log, before, after, consumed);
        }

        private void FireEvents()
        {
            if (PerMille(random, config.UiOpenPerMille))
            {
                Peer peer = random.Pick(peers);
                ContainerId chest = random.Pick(world.ChestIds);
                if (peer.Current is null)
                {
                    authority.OpenUi(peer.Index, chest, random.Next(20, 200));
                }
            }

            if (PerMille(random, config.OwnerLostPerMille))
            {
                authority.LoseOwner(random.Pick(world.ChestIds));
            }

            if (PerMille(random, config.OwnerAwayPerMille))
            {
                authority.SetOwnerAway(random.Pick(world.ChestIds), random.Next(10, 80));
            }

            if (PerMille(random, config.WriteDuringLatencyPerMille))
            {
                WriteDuringLatency();
            }

            if (PerMille(random, config.DisconnectPerMille))
            {
                Peer[] busy = peers.Where(p => p.Current is not null).ToArray();
                if (busy.Length > 0)
                {
                    Crash(random.Pick(busy));
                }
            }
        }

        /// <summary>A disconnect is process death: no Cancel, no release. The authority reclaims the holds and the peer comes back as a new one.</summary>
        private void Crash(Peer peer)
        {
            authority.DisconnectPeer(peer.Index);
            peer.Current!.Crash();
            peer.Layer.Retire();
            Log("crash", peer, peer.Current!.Describe());
            FinishOperation(peer);
            Rejoin(peer);
        }

        private Peer Rejoin(Peer dead)
        {
            long session = authority.Rejoin(dead.Index);
            var fresh = new Peer(dead.Index, session, world, store, clock, settings) { OpsDone = dead.OpsDone };
            peers[dead.Index] = fresh;
            return fresh;
        }

        /// <summary>
        /// While some peer's reservation request for a chest is still travelling to the chest's owner, the owner (idle) reserves the chest,
        /// moves one item from it into its own inventory and releases it, so the waiting peer's verification sees a changed chest.
        /// </summary>
        private void WriteDuringLatency()
        {
            foreach (ContainerId id in world.ChestIds)
            {
                IReadOnlyList<int> waiting = authority.PendingPeersOn(id);
                if (waiting.Count == 0 || untouchable.Contains(id))
                {
                    continue;
                }

                long owner = authority.OwnerOf(id);
                Peer? writer = peers.FirstOrDefault(p => p.Current is null && authority.CurrentSessionOf(p.Index) == owner && !waiting.Contains(p.Index));
                if (writer is null)
                {
                    continue;
                }

                writer.Layer.RequestReservation(id);
                if (writer.Layer.GetReservationStatus(id) == ReservationStatus.Granted)
                {
                    ContainerSnapshot chest = writer.Layer.GetContainer(id)!;
                    if (chest.Contents.Count > 0)
                    {
                        ItemStackSnapshot stack = chest.Contents[0];
                        if (writer.Layer.RemoveFromStack(id, 0, stack, 1) == ContainerOperationResult.Success)
                        {
                            var one = new ItemStackSnapshot(stack.PrefabName, 1, stack.Quality, stack.MaxStack);
                            if (writer.Layer.AddItems(writer.Layer.PlayerInventoryId, one) != ContainerOperationResult.Success)
                            {
                                Assert.Equal(ContainerOperationResult.Success, writer.Layer.AddItems(id, one));
                            }

                            Log("write-during-latency", writer, "took 1 " + stack.PrefabName + " from #" + id.Id.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }

                writer.Layer.ReleaseReservation(id);
                return;
            }
        }

        internal SimulationProbe.DisconnectReport CrashAndRejoin(int index, OperationKind during)
        {
            Peer old = peers[index];
            Advance();
            old.Current = during == OperationKind.Craft ? StageCraft(old) : StageExecutor(old, during);
            old.Current.Step();
            if (old.Current.IsDone)
            {
                throw new InvalidOperationException("schedule " + config.Index.ToString(CultureInfo.InvariantCulture) + " offers " + during + " nothing to be interrupted in");
            }

            bool heldLock = old.Current.HoldsLock;
            long oldSession = old.SessionId;
            authority.DisconnectPeer(index);
            old.Current.Crash();
            old.Layer.Retire();
            int holdsAfterCrash = authority.HoldsOf(index) + (authority.HasAnyReservation(index) ? 1 : 0);

            var third = new OrganizeLock(store, clock, settings);
            const long ThirdParty = 777_000L;
            bool blocked = true;
            if (heldLock)
            {
                OrganizeLockResult early = third.Acquire(config.Stations, ThirdParty, "Third");
                blocked = !early.IsGranted;
                if (early.IsGranted)
                {
                    third.Release(config.Stations, ThirdParty);
                }
            }

            clock.Advance(TimeSpan.FromSeconds(SimulationItems.OrganizeLockSeconds + 1));
            OrganizeLockResult late = third.Acquire(config.Stations, ThirdParty, "Third");
            bool expired = blocked && late.IsGranted;
            if (late.IsGranted)
            {
                third.Release(config.Stations, ThirdParty);
            }

            Peer fresh = Rejoin(old);
            Exception? thrown = null;
            try
            {
                CraftOperation next = StageCraft(fresh);
                for (int i = 0; i < 100 && !next.IsDone; i++)
                {
                    Advance();
                    next.Step();
                }
            }
            catch (InvalidOperationException failure)
            {
                thrown = failure;
            }

            return new SimulationProbe.DisconnectReport(
                old.SessionId,
                fresh.SessionId,
                old.Consumption,
                fresh.Consumption,
                old.Lock,
                fresh.Lock,
                old.Layer,
                fresh.Layer,
                thrown,
                old.Layer.CallsAfterRetire,
                holdsAfterCrash,
                expired);
        }

        private void Advance()
        {
            clock.Advance(TimeSpan.FromMilliseconds(SimulationItems.TickMs));
            authority.Tick(tick);
            tick++;
        }

        private CraftOperation StageCraft(Peer peer)
        {
            ContainerSnapshot inventory = peer.Layer.GetContainer(peer.Layer.PlayerInventoryId)!;
            IReadOnlyList<ContainerSnapshot> chests = peer.Layer.GetContainers();
            var snapshot = new BaseSnapshot(inventory, chests, config.Assignments, options);
            string? prefab = null;
            foreach (ContainerSnapshot chest in chests)
            {
                bool keep = config.Assignments.TryGetValue(chest.Id, out CategoryAssignment? assignment) && assignment!.IsKeep;
                if (keep || (chest.Flags & ContainerFlags.InUseByOther) != 0 || ContainerEligibility.GetExclusionReason(chest, false, ContainerPurpose.CraftAvailability, options).HasValue)
                {
                    continue;
                }

                prefab = chest.Contents.Select(stack => stack.PrefabName).FirstOrDefault(name => inventory.Contents.All(own => own.PrefabName != name));
                if (prefab is not null)
                {
                    break;
                }
            }

            prefab ??= inventory.Contents.Select(stack => stack.PrefabName).FirstOrDefault() ?? SimulationItems.CraftablePrefabs[0];
            return new CraftOperation(peer.Consumption, snapshot, new List<CraftRequirement> { new CraftRequirement(prefab, 1) });
        }

        private ExecutorOperation StageExecutor(Peer peer, OperationKind kind)
        {
            Func<OrganizePlan> planner = kind == OperationKind.Organize ? () => PlanOrganize(peer) : () => PlanDeposit(peer);
            return new ExecutorOperation(kind, peer.Lock, config.Stations, peer.SessionId, peer.Name, planner, peer.Layer, clock, settings, config.RenewEveryTicks);
        }

        private void StartOperation(Peer peer)
        {
            world.Writers[peer.Index].Reset();
            int roll = random.Next(0, 100);
            PeerOperation operation;
            if (roll < 40)
            {
                operation = StartCraft(peer);
            }
            else
            {
                operation = StageExecutor(peer, roll < 70 ? OperationKind.Organize : OperationKind.Deposit);
            }

            stats.Operations++;
            peer.Current = operation;
            Log("start", peer, operation.Describe());
            if (operation.IsDone)
            {
                FinishOperation(peer);
            }
        }

        private CraftOperation StartCraft(Peer peer)
        {
            ContainerSnapshot inventory = peer.Layer.GetContainer(peer.Layer.PlayerInventoryId)!;
            IReadOnlyList<ContainerSnapshot> chests = peer.Layer.GetContainers();
            var snapshot = new BaseSnapshot(inventory, chests, config.Assignments, options);
            return new CraftOperation(peer.Consumption, snapshot, Recipe(snapshot));
        }

        private List<CraftRequirement> Recipe(BaseSnapshot snapshot)
        {
            int count = random.Next(1, 3);
            var prefabs = new List<string>();
            while (prefabs.Count < count)
            {
                string prefab = random.Pick(SimulationItems.CraftablePrefabs);
                if (!prefabs.Contains(prefab))
                {
                    prefabs.Add(prefab);
                }
            }

            var requirements = new List<CraftRequirement>();
            foreach (string prefab in prefabs)
            {
                long visible = snapshot.PlayerInventory.Contents.Where(s => s.PrefabName == prefab).Sum(s => (long)s.Count);
                foreach (ContainerSnapshot chest in snapshot.Containers)
                {
                    bool keep = snapshot.Assignments.TryGetValue(chest.Id, out CategoryAssignment? assignment) && assignment!.IsKeep;
                    if (keep || (chest.Flags & ContainerFlags.InUseByOther) != 0 || ContainerEligibility.GetExclusionReason(chest, false, ContainerPurpose.CraftAvailability, options).HasValue)
                    {
                        continue;
                    }

                    visible += chest.Contents.Where(s => s.PrefabName == prefab).Sum(s => (long)s.Count);
                }

                int amount;
                int roll = random.Next(0, 100);
                if (visible > 0 && roll < 15)
                {
                    amount = (int)Math.Min(visible, 500);
                }
                else if (visible > 0 && roll < 80)
                {
                    amount = random.Next(1, (int)Math.Min(visible, 500) + 1);
                }
                else
                {
                    amount = random.Next(1, 61);
                }

                requirements.Add(new CraftRequirement(prefab, amount));
            }

            return requirements;
        }

        private OrganizePlan PlanOrganize(Peer peer) =>
            OrganizePlanner.Plan(peer.Layer.GetContainers(), config.Assignments, world.InUseByFor(peer.Index), catalog, kinds, options, settings.AutoAssignThresholdPct);

        private OrganizePlan PlanDeposit(Peer peer)
        {
            ContainerSnapshot inventory = peer.Layer.GetContainer(peer.Layer.PlayerInventoryId)!;
            var stacks = new List<PlayerInventoryStack>();
            var favourites = new List<InventorySlot>();
            for (int i = 0; i < inventory.Contents.Count; i++)
            {
                ItemStackSnapshot stack = inventory.Contents[i];
                var slot = new InventorySlot(i % inventory.Width, i / inventory.Width);
                InventoryItemTraits traits = stack.MaxStack == 1 && random.Chance(30) ? InventoryItemTraits.Equipped : InventoryItemTraits.None;
                stacks.Add(new PlayerInventoryStack(slot, stack, traits));
                if (random.Chance(10))
                {
                    favourites.Add(slot);
                }
            }

            var player = new PlayerInventorySnapshot(inventory.Id, inventory.Width, inventory.Height, stacks);
            var exclusions = new DepositExclusions(new FavouriteSlots(favourites), SettingsCatalog.DepositNeverItemsDefault);
            return DepositPlanner.Plan(player, exclusions, peer.Layer.GetContainers(), config.Assignments, world.InUseByFor(peer.Index), catalog, kinds, options, settings.AutoAssignThresholdPct).Plan;
        }

        private void FinishOperation(Peer peer)
        {
            PeerOperation operation = peer.Current!;
            peer.Current = null;
            peer.OpsDone++;
            SimulatedWorld.WriteCounter writes = world.Writers[peer.Index];
            Log("end", peer, operation.Describe() + " -> " + operation.Outcome + " (added " + writes.AddedItems.ToString(CultureInfo.InvariantCulture) + ", removed " + writes.RemovedItems.ToString(CultureInfo.InvariantCulture) + ")");

            if (operation.Outcome == OperationOutcome.Crashed)
            {
                stats.Crashes++;
                Assert.False(authority.HasAnyReservation(peer.Index), peer.Name + " still has a reservation after disconnecting");
                return;
            }

            if (operation is CraftOperation craft)
            {
                FinishCraft(peer, craft, writes);
            }
            else
            {
                FinishExecutor(peer, (ExecutorOperation)operation, writes);
            }

            Assert.False(operation.HoldsLock, peer.Name + " still holds the lock after " + operation.Describe());
            Assert.False(authority.HasAnyReservation(peer.Index), peer.Name + " still has a reservation after " + operation.Describe());
        }

        private void FinishCraft(Peer peer, CraftOperation craft, SimulatedWorld.WriteCounter writes)
        {
            Assert.True(craft.Outcome != OperationOutcome.WriteFailed, peer.Name + ": " + craft.FailureDetail);
            Assert.Equal(0L, writes.AddedItems);
            switch (craft.Outcome)
            {
                case OperationOutcome.Completed:
                    stats.CraftsCompleted++;
                    if (craft.ChestTakes > 0)
                    {
                        stats.CraftsFromChests++;
                    }

                    long expected = craft.Requirements.Sum(requirement => (long)requirement.Amount);
                    long taken = craft.Consumed.Sum(stack => (long)stack.Count);
                    Assert.True(expected == taken, peer.Name + ": the craft consumed " + taken + " items for " + expected + " required");
                    Assert.True(expected == writes.RemovedItems, peer.Name + ": the craft removed " + writes.RemovedItems + " items for " + expected + " required");
                    foreach (CraftRequirement requirement in craft.Requirements)
                    {
                        long ofPrefab = craft.Consumed.Where(stack => stack.PrefabName == requirement.PrefabName).Sum(stack => (long)stack.Count);
                        Assert.True(ofPrefab == requirement.Amount, peer.Name + ": consumed " + ofPrefab + " " + requirement.PrefabName + " for " + requirement.Amount + " required");
                    }

                    consumed.AddRange(craft.Consumed);
                    stats.ItemsConsumed += taken;
                    break;
                case OperationOutcome.NotAvailable:
                    stats.CraftsNotAvailable++;
                    break;
                case OperationOutcome.Denied:
                    stats.CraftsDenied++;
                    break;
                case OperationOutcome.TimedOut:
                    stats.CraftsTimedOut++;
                    break;
                case OperationOutcome.Lost:
                    stats.CraftsLost++;
                    break;
                case OperationOutcome.Changed:
                    stats.CraftsChanged++;
                    break;
                default:
                    Assert.Fail(peer.Name + ": unexpected craft outcome " + craft.Outcome);
                    break;
            }

            if (craft.Outcome != OperationOutcome.Completed)
            {
                Assert.True(writes.RemovedItems == 0, peer.Name + ": a cancelled craft removed " + writes.RemovedItems + " items");
            }
        }

        private void FinishExecutor(Peer peer, ExecutorOperation operation, SimulatedWorld.WriteCounter writes)
        {
            stats.Renewals += operation.Renewals;
            Assert.True(writes.AddedItems == writes.RemovedItems, peer.Name + ": added " + writes.AddedItems + " but removed " + writes.RemovedItems);
            if (operation.Executor is null)
            {
                Assert.Equal(0L, writes.AddedItems);
                if (operation.Outcome == OperationOutcome.LockRefused)
                {
                    stats.LockRefusals++;
                }
                else
                {
                    Assert.Equal(OperationOutcome.Nothing, operation.Outcome);
                    stats.Nothing++;
                }

                return;
            }

            OrganizeExecutionResult result = operation.Executor.Result;
            OrganizePlan plan = operation.Plan!;
            Assert.True(result.UnrecoveredItems == 0, peer.Name + ": a rollback left " + result.UnrecoveredItems + " items in the destination");
            Assert.True(result.MovesCompleted <= plan.Moves.Count, peer.Name + ": more moves completed than planned");
            long expectedMoved = plan.Moves.Take(result.MovesCompleted).Sum(move => (long)move.Count);
            Assert.True(expectedMoved == result.ItemsMoved, peer.Name + ": ItemsMoved " + result.ItemsMoved + " differs from the first " + result.MovesCompleted + " moves (" + expectedMoved + ")");
            Assert.True(writes.AddedItems >= result.ItemsMoved, peer.Name + ": the world saw " + writes.AddedItems + " items added for " + result.ItemsMoved + " moved");
            Assert.True(writes.AddedItems - result.ItemsMoved <= (result.FailedMove?.Count ?? 0), peer.Name + ": more items added than moved plus the failed move");
            stats.ItemsMoved += result.ItemsMoved;

            bool organize = operation.Kind == OperationKind.Organize;
            switch (operation.Outcome)
            {
                case OperationOutcome.Completed:
                    Assert.Equal(plan.Moves.Count, result.MovesCompleted);
                    Assert.Equal(ExecutionStatus.Completed, result.Status);
                    if (organize)
                    {
                        stats.OrganizesCompleted++;
                    }
                    else
                    {
                        stats.DepositsCompleted++;
                    }

                    break;
                case OperationOutcome.LockLost:
                    stats.LockLost++;
                    break;
                default:
                    Assert.Equal(ExecutionStatus.Stopped, result.Status);
                    if (organize)
                    {
                        stats.OrganizesStopped++;
                    }
                    else
                    {
                        stats.DepositsStopped++;
                    }

                    CountStop(operation.Outcome);
                    break;
            }
        }

        private void CountStop(OperationOutcome outcome)
        {
            switch (outcome)
            {
                case OperationOutcome.Denied:
                    stats.StoppedDenied++;
                    break;
                case OperationOutcome.TimedOut:
                    stats.StoppedTimedOut++;
                    break;
                case OperationOutcome.Lost:
                    stats.StoppedLost++;
                    break;
                case OperationOutcome.Changed:
                    stats.StoppedChanged++;
                    break;
                default:
                    stats.StoppedOther++;
                    break;
            }
        }

        private void Log(string what, Peer peer, string detail) =>
            log.Add("t" + tick.ToString(CultureInfo.InvariantCulture) + " " + what + " " + peer.Name + " " + detail);
    }
}
