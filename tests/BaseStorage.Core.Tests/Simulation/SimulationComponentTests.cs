namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Locking;
using BaseStorage.Core.Tests.Organize;
using BaseStorage.Core.Tests.Reservations;
using static BaseStorage.Core.Tests.Simulation.SimulationItems;

/// <summary>
/// GW-U-1458 and GW-U-1460 (S-E005-004, AC-3.4, AC-5.2, AC-5.4): the simulation's building blocks on a literal two-peer base. The
/// per-peer layer obeys the container-layer check order and shows another peer's hold as InUseByOther; a craft through Core's
/// consumption executor consumes nothing when the plan is not available, when it is denied or when a container changed, and exactly
/// the recipe when granted; an Organize is refused the base lock while another peer holds it, moves the misplaced stack when granted,
/// and releases the lock at once when there is nothing to move.
/// </summary>
public sealed class SimulationComponentTests
{
    private static readonly ContainerId ChestA = new ContainerId(3141592653L, 201u);
    private static readonly ContainerId ChestB = new ContainerId(3141592653L, 202u);

    // GW-U-1460
    [Fact]
    public void ThePeerLayerGatesWritesInTheContractedOrderAndShowsAnotherPeersHoldAsInUse()
    {
        Harness h = Harness.Make();
        PeerContainerLayer erik = h.Layers[0];
        PeerContainerLayer astrid = h.Layers[1];
        var unknown = new ContainerId(1L, 1u);

        Assert.Null(erik.GetContainer(unknown));
        Assert.Equal(ContainerOperationResult.NotFound, erik.AddItems(unknown, Stack("Wood", 1)));
        Assert.Equal(ReservationStatus.None, erik.GetReservationStatus(unknown));
        Assert.Equal(ContainerOperationResult.NotReserved, erik.AddItems(ChestA, Stack("Wood", 1)));
        Assert.Equal(ContainerOperationResult.NotReserved, erik.RemoveFromStack(ChestA, 0, Stack("Wood", 10), 1));

        Assert.Equal(ReservationStatus.Granted, erik.GetReservationStatus(erik.PlayerInventoryId));
        Assert.Equal(ContainerOperationResult.Success, erik.AddItems(erik.PlayerInventoryId, Stack("Stone", 5)));
        Assert.Equal(new[] { Stack("Wood", 20), Stack("Stone", 5) }, erik.GetContainer(erik.PlayerInventoryId)!.Contents);

        erik.RequestReservation(ChestA);
        Assert.Equal(ReservationStatus.Pending, erik.GetReservationStatus(ChestA));
        h.Tick();
        Assert.Equal(ReservationStatus.Granted, erik.GetReservationStatus(ChestA));
        Assert.Equal(ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, astrid.GetContainers().Single(c => c.Id == ChestA).Flags);
        Assert.Equal(ContainerFlags.PlayerBuilt, erik.GetContainers().Single(c => c.Id == ChestA).Flags);
        Assert.Equal(new[] { ChestA }, h.World.InUseByFor(1).Keys);
        Assert.Equal("Erik", h.World.InUseByFor(1)[ChestA]);
        Assert.Empty(h.World.InUseByFor(0));
        Assert.Equal(ContainerOperationResult.NotReserved, astrid.AddItems(ChestA, Stack("Wood", 1)));

        Assert.Equal(ContainerOperationResult.ContainerChanged, erik.RemoveFromStack(ChestA, 0, Stack("Wood", 9), 1));
        Assert.Equal(ContainerOperationResult.ContainerChanged, erik.RemoveFromStack(ChestA, 5, Stack("Wood", 10), 1));
        Assert.Equal(ContainerOperationResult.Success, erik.RemoveFromStack(ChestA, 0, Stack("Wood", 10), 4));
        Assert.Equal(ContainerOperationResult.Success, erik.AddItems(ChestA, Stack("Wood", 44)));
        Assert.Equal(new[] { Stack("Wood", 50), Stack("Stone", 20) }, erik.GetContainer(ChestA)!.Contents);
        Assert.Equal(ContainerOperationResult.DestinationFull, erik.AddItems(ChestA, Stack("Stone", 131)));
        Assert.Equal(new[] { Stack("Wood", 50), Stack("Stone", 20) }, erik.GetContainer(ChestA)!.Contents);
        Assert.Equal(ContainerOperationResult.Success, erik.AddItems(ChestA, Stack("Stone", 130)));
        Assert.Equal(new[] { Stack("Wood", 50), Stack("Stone", 50), Stack("Stone", 50), Stack("Stone", 50) }, erik.GetContainer(ChestA)!.Contents);
        Assert.Equal(ContainerOperationResult.DestinationFull, erik.AddItems(ChestA, Stack("Stone", 1)));

        erik.ReleaseReservation(ChestA);
        Assert.Equal(ReservationStatus.None, erik.GetReservationStatus(ChestA));
        Assert.Equal(ContainerFlags.PlayerBuilt, astrid.GetContainers().Single(c => c.Id == ChestA).Flags);
        Assert.Equal(179L, h.World.Writers[0].AddedItems);
        Assert.Equal(4L, h.World.Writers[0].RemovedItems);
        Assert.Equal(0L, h.World.Writers[1].AddedItems);
        Assert.Equal(3, h.Authority.Writes);
    }

    // GW-U-1458
    [Fact]
    public void ACraftConsumesExactlyTheRecipeWhenGrantedAndNothingWhenUnavailableDeniedOrChanged()
    {
        Harness h = Harness.Make();
        PeerContainerLayer erik = h.Layers[0];
        PeerContainerLayer astrid = h.Layers[1];
        List<ContainerSnapshot> before = h.World.Capture();

        // Not available: Astrid has chest A open in the vanilla UI, so Erik's view skips A and the inventory alone cannot pay.
        Assert.True(h.Authority.OpenUi(1, ChestA, 50));
        CraftOperation unavailable = h.Craft(0, new CraftRequirement("Wood", 25));
        Assert.Equal(OperationOutcome.NotAvailable, unavailable.Outcome);
        Assert.Null(unavailable.Plan);
        h.Authority.DisconnectPeer(1);
        h.Tick();
        h.Tick();

        // Denied: the plan is made while A is free, then Astrid opens A before Erik's request is answered.
        CraftOperation denied = h.Craft(0, new CraftRequirement("Wood", 25));
        Assert.Equal(new[] { ChestA }, denied.Plan!.ExpectedContainers.Select(c => c.Id));
        Assert.Equal(ReservationStatus.Pending, erik.GetReservationStatus(ChestA));
        Assert.True(h.Authority.OpenUi(1, ChestA, 50));
        h.Tick();
        denied.Step();
        Assert.Equal(OperationOutcome.Denied, denied.Outcome);
        Assert.Equal(ConsumptionOutcome.ReservationFailed, denied.Result!.Outcome);
        Assert.Empty(denied.Consumed);
        Assert.False(h.Authority.HasAnyReservation(0));
        h.Authority.DisconnectPeer(1);
        h.Tick();
        h.Tick();
        Assert.Equal(before, h.World.Capture());

        // Changed: Astrid, A's owner since an earlier grant, takes one Wood out of A between Erik's plan and his grant.
        astrid.RequestReservation(ChestA);
        h.Tick();
        astrid.ReleaseReservation(ChestA);
        CraftOperation changed = h.Craft(0, new CraftRequirement("Wood", 25));
        astrid.RequestReservation(ChestA);
        Assert.Equal(ReservationStatus.Granted, astrid.GetReservationStatus(ChestA));
        Assert.Equal(ContainerOperationResult.Success, astrid.RemoveFromStack(ChestA, 0, Stack("Wood", 10), 1));
        astrid.ReleaseReservation(ChestA);
        h.Tick();
        Assert.Equal(ReservationStatus.Granted, erik.GetReservationStatus(ChestA));
        changed.Step();
        Assert.Equal(OperationOutcome.Changed, changed.Outcome);
        Assert.Equal(ConsumptionOutcome.ContentsChanged, changed.Result!.Outcome);
        Assert.Equal(ChestA, changed.Result.FailedContainer);
        Assert.Empty(changed.Consumed);
        Assert.False(h.Authority.HasAnyReservation(0));
        Assert.Equal(new[] { Stack("Wood", 9), Stack("Stone", 20) }, h.World.Chest(ChestA).Contents);
        Assert.Equal(new[] { Stack("Wood", 20) }, erik.GetContainer(erik.PlayerInventoryId)!.Contents);

        // Granted: inventory first, then the nearest chest, exactly the recipe, every chest released.
        // Erik owns A since his grant above and ownership is never handed back; the server's release pass (ADR 0002 "Release") moves it away,
        // so this craft needs a round trip and is still running after its first step.
        Assert.False(h.Authority.LoseOwner(ChestA));
        h.World.Writers[0].Reset();
        List<ContainerSnapshot> beforeCraft = h.World.Capture();
        CraftOperation granted = h.Craft(0, new CraftRequirement("Wood", 25), new CraftRequirement("Stone", 3));
        granted.Step();
        Assert.Equal(OperationOutcome.Running, granted.Outcome);
        h.Tick();
        granted.Step();
        Assert.Equal(OperationOutcome.Completed, granted.Outcome);
        Assert.Equal(28L, granted.Consumed.Sum(s => (long)s.Count));
        Assert.Equal(2, granted.ChestTakes);
        Assert.Equal(28L, h.World.Writers[0].RemovedItems);
        Assert.Equal(0L, h.World.Writers[0].AddedItems);
        Assert.Empty(erik.GetContainer(erik.PlayerInventoryId)!.Contents);
        Assert.Equal(new[] { Stack("Wood", 4), Stack("Stone", 17) }, h.World.Chest(ChestA).Contents);
        Assert.Equal(new[] { Stack("Stone", 10) }, h.World.Chest(ChestB).Contents);
        Assert.False(h.Authority.HasAnyReservation(0));
        SimulationRunner.AssertConserved(beforeCraft, h.World.Capture(), granted.Consumed);

        // Changed in the inventory: the planned inventory stack shrinks before the grant.
        Assert.Equal(ContainerOperationResult.Success, erik.AddItems(erik.PlayerInventoryId, Stack("Wood", 10)));
        Assert.False(h.Authority.LoseOwner(ChestA));
        CraftOperation changedLate = h.Craft(0, new CraftRequirement("Wood", 12));
        changedLate.Step();
        Assert.Equal(ContainerOperationResult.Success, erik.RemoveFromStack(erik.PlayerInventoryId, 0, Stack("Wood", 10), 1));
        h.Tick();
        changedLate.Step();
        Assert.Equal(OperationOutcome.Changed, changedLate.Outcome);
        Assert.Equal(erik.PlayerInventoryId, changedLate.Result!.FailedContainer);
        Assert.Empty(changedLate.Consumed);
        Assert.Equal(new[] { Stack("Wood", 9) }, erik.GetContainer(erik.PlayerInventoryId)!.Contents);
        Assert.Equal(new[] { Stack("Wood", 4), Stack("Stone", 17) }, h.World.Chest(ChestA).Contents);
        Assert.False(h.Authority.HasAnyReservation(0));
    }

    // GW-U-1458
    [Fact]
    public void AnOrganizeIsRefusedTheLockWhileAnotherPeerHoldsItMovesTheMisplacedStackWhenGrantedAndReleasesTheLockWhenIdle()
    {
        Harness h = Harness.Make();
        long astrid = ReservationAuthority.SessionOf(1);
        Assert.True(h.Locks[1].Acquire(h.Stations, astrid, "Astrid").IsGranted);

        ExecutorOperation refused = h.Organize(0);
        refused.Step();
        Assert.Equal(OperationOutcome.LockRefused, refused.Outcome);
        Assert.Null(refused.Plan);
        Assert.False(refused.HoldsLock);
        Assert.Equal("Astrid", refused.LockResult!.HolderName);

        Assert.True(h.Locks[1].Release(h.Stations, astrid));
        ExecutorOperation organize = h.Organize(0);
        organize.Step();
        Assert.True(organize.LockResult!.IsGranted);
        Assert.True(organize.HoldsLock);
        Assert.Single(organize.Plan!.Moves);
        Assert.Equal(OperationOutcome.Running, organize.Outcome);
        Assert.Equal(OrganizeLockStatus.Refused, h.Locks[1].Acquire(h.Stations, astrid, "Astrid").Status);
        int steps = 0;
        while (!organize.IsDone)
        {
            Assert.True(steps++ < 100, "the Organize did not finish");
            h.Tick();
            organize.Step();
        }

        Assert.Equal(OperationOutcome.Completed, organize.Outcome);
        Assert.False(organize.HoldsLock);
        Assert.Equal(new[] { Stack("Wood", 10) }, h.World.Chest(ChestA).Contents);
        Assert.Equal(new[] { Stack("Stone", 30) }, h.World.Chest(ChestB).Contents);
        Assert.Equal(20L, h.World.Writers[0].AddedItems);
        Assert.Equal(20L, h.World.Writers[0].RemovedItems);
        Assert.False(h.Authority.HasAnyReservation(0));
        Assert.True(h.Locks[1].Acquire(h.Stations, astrid, "Astrid").IsGranted);
        Assert.True(h.Locks[1].Release(h.Stations, astrid));

        ExecutorOperation nothing = h.Organize(0);
        nothing.Step();
        Assert.Equal(OperationOutcome.Nothing, nothing.Outcome);
        Assert.Empty(nothing.Plan!.Moves);
        Assert.False(nothing.HoldsLock);
        Assert.True(h.Locks[1].Acquire(h.Stations, astrid, "Astrid").IsGranted);
    }

    /// <summary>
    /// Two peers over a one-tick authority: Erik holds Wood 20; chest A (Wood, at x=1) holds Wood 10 and Stone 20; chest B (Stone, at x=5)
    /// holds Stone 10; Astrid holds nothing. One station carries the base lock.
    /// </summary>
    private sealed class Harness
    {
        private int tick;

        private Harness(ReservationAuthority authority, SimulatedWorld world, ManualClock clock, BaseStorageSettings settings, StationSnapshot[] stations)
        {
            Authority = authority;
            World = world;
            Clock = clock;
            Settings = settings;
            Stations = stations;
            var store = new FakeOrganizeLockStore();
            Layers = new[] { new PeerContainerLayer(world, 0), new PeerContainerLayer(world, 1) };
            Locks = new[] { new OrganizeLock(store, clock, settings), new OrganizeLock(store, clock, settings) };
            Executors = new[] { new ConsumptionExecutor(Layers[0], clock, settings), new ConsumptionExecutor(Layers[1], clock, settings) };
            Assignments = new Dictionary<ContainerId, CategoryAssignment>
            {
                [ChestA] = CategoryAssignment.ForCategory("Wood"),
                [ChestB] = CategoryAssignment.ForCategory("Stone"),
            };
        }

        internal ReservationAuthority Authority { get; }

        internal SimulatedWorld World { get; }

        internal ManualClock Clock { get; }

        internal BaseStorageSettings Settings { get; }

        internal StationSnapshot[] Stations { get; }

        internal PeerContainerLayer[] Layers { get; }

        internal OrganizeLock[] Locks { get; }

        internal ConsumptionExecutor[] Executors { get; }

        internal Dictionary<ContainerId, CategoryAssignment> Assignments { get; }

        internal static Harness Make()
        {
            var log = new List<string>();
            var authority = new ReservationAuthority(new FuzzRandom(7UL), 2, 1, 0, 2, log);
            var chests = new[]
            {
                new ContainerSnapshot(ChestA, new WorldPosition(1f, 30f, 0f), 4, 1, new[] { Stack("Wood", 10), Stack("Stone", 20) }, ContainerFlags.PlayerBuilt),
                new ContainerSnapshot(ChestB, new WorldPosition(5f, 30f, 0f), 4, 1, new[] { Stack("Stone", 10) }, ContainerFlags.PlayerBuilt),
            };
            var inventories = new[]
            {
                new ContainerSnapshot(new ContainerId(ReservationAuthority.SessionOf(0), 1u), new WorldPosition(0f, 30f, 0f), 8, 4, new[] { Stack("Wood", 20) }, ContainerFlags.None),
                new ContainerSnapshot(new ContainerId(ReservationAuthority.SessionOf(1), 1u), new WorldPosition(0f, 30f, 0f), 8, 4, Array.Empty<ItemStackSnapshot>(), ContainerFlags.None),
            };
            var world = new SimulatedWorld(authority, chests, inventories);
            var clock = new ManualClock(TimeSpan.FromSeconds(3600));
            var stations = new[] { new StationSnapshot(new StationId(2718281828L, 900u), new WorldPosition(0f, 30f, 0f), 20f) };
            var harness = new Harness(authority, world, clock, SimulationItems.Settings(20), stations);
            authority.Tick(0);
            return harness;
        }

        /// <summary>Advances the clock one frame and delivers the authority's responses.</summary>
        internal void Tick()
        {
            tick++;
            Clock.Advance(TimeSpan.FromMilliseconds(TickMs));
            Authority.Tick(tick);
        }

        /// <summary>Plans a craft from the peer's current view and starts it.</summary>
        internal CraftOperation Craft(int peer, params CraftRequirement[] requirements)
        {
            PeerContainerLayer layer = Layers[peer];
            var snapshot = new BaseSnapshot(layer.GetContainer(layer.PlayerInventoryId)!, layer.GetContainers(), Assignments, EligibilityOptions.FromSettings(Settings));
            return new CraftOperation(Executors[peer], snapshot, requirements);
        }

        /// <summary>Creates an Organize operation of a peer over the harness base.</summary>
        internal ExecutorOperation Organize(int peer)
        {
            PeerContainerLayer layer = Layers[peer];
            return new ExecutorOperation(
                OperationKind.Organize,
                Locks[peer],
                Stations,
                ReservationAuthority.SessionOf(peer),
                PeerNames[peer],
                () => OrganizePlanner.Plan(layer.GetContainers(), Assignments, World.InUseByFor(peer), Catalog(), Kinds(), EligibilityOptions.FromSettings(Settings), Settings.AutoAssignThresholdPct),
                layer,
                Clock,
                Settings,
                SimulationRunner.RenewEveryTicks);
        }
    }
}
