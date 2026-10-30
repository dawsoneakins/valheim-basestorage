namespace BaseStorage.Core.Tests.Fakes;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;

/// <summary>
/// In-memory fake of <see cref="IContainerLayer"/> for tests: chests and a player inventory, polled reservations and
/// injectable faults (<see cref="FakeFault"/>). Test support only; it is not part of Core.
/// </summary>
public sealed class FakeContainerLayer : IContainerLayer
{
    private readonly Entry? _inventory;
    private readonly Dictionary<ContainerId, Entry> _chests = new();

    /// <summary>Creates a layer with an optional player inventory.</summary>
    /// <param name="playerInventory">The player inventory snapshot; null means there is no local player.</param>
    public FakeContainerLayer(ContainerSnapshot? playerInventory = null)
    {
        _inventory = playerInventory is null ? null : new Entry(playerInventory);
    }

    /// <summary>Gets the player inventory id, or none when the layer has no inventory.</summary>
    public ContainerId PlayerInventoryId => _inventory is null ? default : _inventory.Id;

    /// <summary>
    /// Gets or sets whether a <c>GetReservationStatus</c> call that finds Pending resolves it at once (the call returns
    /// Pending and the next poll sees the outcome). Default true.
    /// </summary>
    public bool AutoResolveReservations { get; set; } = true;

    /// <summary>Adds a chest to the layer as seed data. It starts unreserved.</summary>
    /// <param name="chest">The chest to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chest"/> is null.</exception>
    /// <exception cref="ArgumentException">The id is already a chest, or it reuses the player inventory id.</exception>
    public void AddChest(ContainerSnapshot chest)
    {
        if (chest is null)
        {
            throw new ArgumentNullException(nameof(chest));
        }

        if (_inventory is not null && chest.Id == _inventory.Id)
        {
            throw new ArgumentException("Reuses the player inventory id.", nameof(chest));
        }

        if (_chests.ContainsKey(chest.Id))
        {
            throw new ArgumentException("Duplicate container id.", nameof(chest));
        }

        _chests.Add(chest.Id, new Entry(chest));
    }

    /// <summary>Gets every chest, excluding the player inventory, by id.</summary>
    /// <returns>Fresh snapshots ordered by id.</returns>
    public IReadOnlyList<ContainerSnapshot> GetContainers() =>
        _chests.Values.OrderBy(e => e.Id).Select(e => e.ToSnapshot()).ToArray();

    /// <summary>Gets a fresh snapshot of one container, including the player inventory.</summary>
    /// <param name="id">The container id.</param>
    /// <returns>The snapshot, or null when unknown.</returns>
    public ContainerSnapshot? GetContainer(ContainerId id) => Find(id)?.ToSnapshot();

    /// <summary>Starts a reservation. A no-op when Pending or Granted, for the inventory and for unknown ids.</summary>
    /// <param name="id">The container id.</param>
    public void RequestReservation(ContainerId id)
    {
        Entry? e = FindChest(id);
        if (e is not null
            && (e.Status == ReservationStatus.None || e.Status == ReservationStatus.Denied || e.Status == ReservationStatus.Lost))
        {
            e.Status = ReservationStatus.Pending;
        }
    }

    /// <summary>Gets the reservation status, resolving a pending reservation when auto-resolve is on.</summary>
    /// <param name="id">The container id.</param>
    /// <returns>The reservation status.</returns>
    public ReservationStatus GetReservationStatus(ContainerId id)
    {
        if (_inventory is not null && id == _inventory.Id)
        {
            return ReservationStatus.Granted;
        }

        Entry? e = FindChest(id);
        if (e is null)
        {
            return ReservationStatus.None;
        }

        ReservationStatus current = e.Status;
        if (current == ReservationStatus.Pending && AutoResolveReservations)
        {
            Resolve(e);
        }

        return current;
    }

    /// <summary>Releases a reservation. Always ends in None; idempotent.</summary>
    /// <param name="id">The container id.</param>
    public void ReleaseReservation(ContainerId id)
    {
        Entry? e = FindChest(id);
        if (e is not null)
        {
            e.Status = ReservationStatus.None;
        }
    }

    /// <summary>Adds items, all or nothing.</summary>
    /// <param name="id">The container id.</param>
    /// <param name="items">The items to add.</param>
    /// <returns>The operation result.</returns>
    public ContainerOperationResult AddItems(ContainerId id, ItemStackSnapshot items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        Entry? e = Find(id);
        ContainerOperationResult? gate = Gate(e);
        if (gate is not null)
        {
            return gate.Value;
        }

        if (e!.Faults.Contains(FakeFault.DestinationFull))
        {
            return ContainerOperationResult.DestinationFull;
        }

        long room = Math.Max(0, e.SlotCount - e.Contents.Count) * (long)items.MaxStack;
        foreach (ItemStackSnapshot s in e.Contents)
        {
            if (Matches(s, items) && s.Count < items.MaxStack)
            {
                room += items.MaxStack - s.Count;
            }
        }

        if (room < items.Count)
        {
            return ContainerOperationResult.DestinationFull;
        }

        int remaining = items.Count;
        for (int i = 0; i < e.Contents.Count && remaining > 0; i++)
        {
            ItemStackSnapshot s = e.Contents[i];
            if (Matches(s, items) && s.Count < items.MaxStack)
            {
                int add = Math.Min(remaining, items.MaxStack - s.Count);
                e.Contents[i] = new ItemStackSnapshot(s.PrefabName, s.Count + add, s.Quality, items.MaxStack);
                remaining -= add;
            }
        }

        while (remaining > 0)
        {
            int add = Math.Min(remaining, items.MaxStack);
            e.Contents.Add(new ItemStackSnapshot(items.PrefabName, add, items.Quality, items.MaxStack));
            remaining -= add;
        }

        return ContainerOperationResult.Success;
    }

    /// <inheritdoc/>
    public void SelectSourceStack(ContainerId source, int stackIndex)
    {
    }

    /// <summary>Removes items from the stack at an index when it equals the expected stack.</summary>
    /// <param name="id">The container id.</param>
    /// <param name="stackIndex">The stack index.</param>
    /// <param name="expected">The stack expected at that index.</param>
    /// <param name="count">How many items to remove.</param>
    /// <returns>The operation result.</returns>
    public ContainerOperationResult RemoveFromStack(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count)
    {
        if (expected is null)
        {
            throw new ArgumentNullException(nameof(expected));
        }

        if (stackIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stackIndex));
        }

        if (count < 1 || count > expected.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Entry? e = Find(id);
        ContainerOperationResult? gate = Gate(e);
        if (gate is not null)
        {
            return gate.Value;
        }

        if (e!.Faults.Contains(FakeFault.ChestChanged)
            || stackIndex >= e.Contents.Count
            || e.Contents[stackIndex] != expected)
        {
            return ContainerOperationResult.ContainerChanged;
        }

        if (count == expected.Count)
        {
            e.Contents.RemoveAt(stackIndex);
        }
        else
        {
            e.Contents[stackIndex] = new ItemStackSnapshot(
                expected.PrefabName, expected.Count - count, expected.Quality, expected.MaxStack);
        }

        return ContainerOperationResult.Success;
    }

    /// <summary>Resolves every pending reservation.</summary>
    public void ResolvePendingReservations()
    {
        foreach (Entry e in _chests.Values)
        {
            if (e.Status == ReservationStatus.Pending)
            {
                Resolve(e);
            }
        }
    }

    /// <summary>Injects a fault on a container and keeps it armed until cleared.</summary>
    /// <param name="id">The container id.</param>
    /// <param name="fault">The fault to inject.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fault"/> is not defined.</exception>
    /// <exception cref="ArgumentException">The id is unknown, or the fault cannot apply to the player inventory.</exception>
    public void InjectFault(ContainerId id, FakeFault fault)
    {
        CheckFault(fault);
        Entry e = FindOrThrow(id);
        bool inventory = ReferenceEquals(e, _inventory);
        if (inventory && (fault == FakeFault.ReservationRefused || fault == FakeFault.OwnerDisconnect))
        {
            throw new ArgumentException("The player inventory is always granted.", nameof(fault));
        }

        e.Faults.Add(fault);
        if (fault == FakeFault.OwnerDisconnect
            && (e.Status == ReservationStatus.Pending || e.Status == ReservationStatus.Granted))
        {
            e.Status = ReservationStatus.Lost;
        }
    }

    /// <summary>Clears an injected fault on a container.</summary>
    /// <param name="id">The container id.</param>
    /// <param name="fault">The fault to clear.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fault"/> is not defined.</exception>
    /// <exception cref="ArgumentException">The id is unknown.</exception>
    public void ClearFault(ContainerId id, FakeFault fault)
    {
        CheckFault(fault);
        FindOrThrow(id).Faults.Remove(fault);
    }

    /// <summary>Captures the world state: the player inventory first, then every chest by id.</summary>
    /// <returns>Fresh snapshots of every container in the layer.</returns>
    public IReadOnlyList<ContainerSnapshot> Capture()
    {
        List<ContainerSnapshot> all = new();
        if (_inventory is not null)
        {
            all.Add(_inventory.ToSnapshot());
        }

        all.AddRange(GetContainers());
        return all;
    }

    private static void CheckFault(FakeFault fault)
    {
        if (!Enum.IsDefined(typeof(FakeFault), fault))
        {
            throw new ArgumentOutOfRangeException(nameof(fault));
        }
    }

    private static bool Matches(ItemStackSnapshot s, ItemStackSnapshot items) =>
        string.Equals(s.PrefabName, items.PrefabName, StringComparison.Ordinal) && s.Quality == items.Quality;

    private static void Resolve(Entry e)
    {
        if (e.Faults.Contains(FakeFault.OwnerDisconnect))
        {
            e.Status = ReservationStatus.Lost;
        }
        else if (e.Faults.Contains(FakeFault.ReservationRefused))
        {
            e.Status = ReservationStatus.Denied;
        }
        else
        {
            e.Status = ReservationStatus.Granted;
        }
    }

    private Entry? Find(ContainerId id)
    {
        if (_inventory is not null && id == _inventory.Id)
        {
            return _inventory;
        }

        return _chests.TryGetValue(id, out Entry? e) ? e : null;
    }

    private Entry? FindChest(ContainerId id) => _chests.TryGetValue(id, out Entry? e) ? e : null;

    private Entry FindOrThrow(ContainerId id) =>
        Find(id) ?? throw new ArgumentException("Unknown container id.", nameof(id));

    private ContainerOperationResult? Gate(Entry? entry)
    {
        if (entry is null)
        {
            return ContainerOperationResult.NotFound;
        }

        if (!ReferenceEquals(entry, _inventory) && entry.Status != ReservationStatus.Granted)
        {
            return ContainerOperationResult.NotReserved;
        }

        return null;
    }

    private sealed class Entry
    {
        private readonly ContainerSnapshot _seed;

        public Entry(ContainerSnapshot seed)
        {
            _seed = seed;
            Contents = new List<ItemStackSnapshot>(seed.Contents);
        }

        public ContainerId Id => _seed.Id;

        public int SlotCount => _seed.SlotCount;

        public List<ItemStackSnapshot> Contents { get; }

        public HashSet<FakeFault> Faults { get; } = new();

        public ReservationStatus Status { get; set; } = ReservationStatus.None;

        public ContainerSnapshot ToSnapshot() =>
            new(_seed.Id, _seed.Position, _seed.Width, _seed.Height, Contents, _seed.Flags);
    }
}
