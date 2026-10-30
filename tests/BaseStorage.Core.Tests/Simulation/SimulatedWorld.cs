namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The shared fake world of the S-E005-004 simulation: the base chests and one inventory per peer, with the same mutation rules as
/// <c>FakeContainerLayer</c> (all-or-nothing adds that top up partial stacks of the same prefab and quality, removes checked against
/// the expected stack) and the write gate routed through the <see cref="ReservationAuthority"/>. Peers see the world through
/// <see cref="PeerContainerLayer"/>, which adds the per-peer in-use flag. Not a test row.
/// </summary>
internal sealed class SimulatedWorld
{
    private readonly Dictionary<ContainerId, Entry> chests = new Dictionary<ContainerId, Entry>();
    private readonly List<ContainerId> chestOrder = new List<ContainerId>();
    private readonly Entry[] inventories;

    /// <summary>Creates the world.</summary>
    /// <param name="authority">The ownership authority the writes are gated by.</param>
    /// <param name="chests">The base chests, any order.</param>
    /// <param name="inventories">One inventory per peer, by peer index.</param>
    internal SimulatedWorld(ReservationAuthority authority, IReadOnlyList<ContainerSnapshot> chests, IReadOnlyList<ContainerSnapshot> inventories)
    {
        Authority = authority;
        var sorted = new List<ContainerSnapshot>(chests);
        sorted.Sort((a, b) => a.Id.CompareTo(b.Id));
        foreach (ContainerSnapshot chest in sorted)
        {
            this.chests.Add(chest.Id, new Entry(chest));
            chestOrder.Add(chest.Id);
            authority.Register(chest.Id);
        }

        this.inventories = new Entry[inventories.Count];
        Writers = new WriteCounter[inventories.Count];
        for (int peer = 0; peer < inventories.Count; peer++)
        {
            this.inventories[peer] = new Entry(inventories[peer]);
            Writers[peer] = new WriteCounter();
        }
    }

    /// <summary>Gets the authority.</summary>
    internal ReservationAuthority Authority { get; }

    /// <summary>Gets the per-peer write counters, reset by each operation.</summary>
    internal WriteCounter[] Writers { get; }

    /// <summary>Gets the chest ids in ascending order.</summary>
    internal IReadOnlyList<ContainerId> ChestIds => chestOrder;

    /// <summary>Gets the inventory id of a peer.</summary>
    /// <param name="peer">The peer index.</param>
    internal ContainerId InventoryIdOf(int peer) => inventories[peer].Id;

    /// <summary>Tells whether an id is one of the base chests.</summary>
    /// <param name="id">The container id.</param>
    internal bool HasChest(ContainerId id) => chests.ContainsKey(id);

    /// <summary>Gets the chests as one peer sees them: the seed flags plus InUseByOther when another peer holds the chest.</summary>
    /// <param name="peer">The observing peer.</param>
    internal IReadOnlyList<ContainerSnapshot> ChestsFor(int peer)
    {
        var list = new List<ContainerSnapshot>(chestOrder.Count);
        foreach (ContainerId id in chestOrder)
        {
            list.Add(chests[id].ToSnapshot(FlagsFor(peer, id)));
        }

        return list;
    }

    /// <summary>Gets one container as one peer sees it, including the peer's own inventory.</summary>
    /// <param name="peer">The observing peer.</param>
    /// <param name="id">The container id.</param>
    internal ContainerSnapshot? GetFor(int peer, ContainerId id)
    {
        if (id == inventories[peer].Id)
        {
            return inventories[peer].ToSnapshot(ContainerFlags.None);
        }

        return chests.TryGetValue(id, out Entry? entry) ? entry.ToSnapshot(FlagsFor(peer, id)) : null;
    }

    /// <summary>Gets the names of the holders other peers see, keyed by chest, for the planners' in-use dictionary.</summary>
    /// <param name="peer">The observing peer.</param>
    internal Dictionary<ContainerId, string> InUseByFor(int peer)
    {
        var map = new Dictionary<ContainerId, string>();
        foreach (ContainerId id in chestOrder)
        {
            if (Authority.IsInUseByOther(peer, id))
            {
                map[id] = SimulationItems.PeerNames[Authority.HolderOf(id)];
            }
        }

        return map;
    }

    /// <summary>Adds items to a container for a peer, all or nothing, under the layer's check order.</summary>
    /// <param name="peer">The writing peer.</param>
    /// <param name="id">The container id.</param>
    /// <param name="items">The items to add.</param>
    internal ContainerOperationResult Add(int peer, ContainerId id, ItemStackSnapshot items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        Entry? entry = Find(peer, id, out ContainerOperationResult gate);
        if (entry is null)
        {
            return gate;
        }

        long room = Math.Max(0, entry.SlotCount - entry.Contents.Count) * (long)items.MaxStack;
        foreach (ItemStackSnapshot stack in entry.Contents)
        {
            if (Matches(stack, items) && stack.Count < items.MaxStack)
            {
                room += items.MaxStack - stack.Count;
            }
        }

        if (room < items.Count)
        {
            return ContainerOperationResult.DestinationFull;
        }

        int remaining = items.Count;
        for (int i = 0; i < entry.Contents.Count && remaining > 0; i++)
        {
            ItemStackSnapshot stack = entry.Contents[i];
            if (Matches(stack, items) && stack.Count < items.MaxStack)
            {
                int add = Math.Min(remaining, items.MaxStack - stack.Count);
                entry.Contents[i] = new ItemStackSnapshot(stack.PrefabName, stack.Count + add, stack.Quality, items.MaxStack);
                remaining -= add;
            }
        }

        while (remaining > 0)
        {
            int add = Math.Min(remaining, items.MaxStack);
            entry.Contents.Add(new ItemStackSnapshot(items.PrefabName, add, items.Quality, items.MaxStack));
            remaining -= add;
        }

        Writers[peer].AddedItems += items.Count;
        RecordChestWrite(peer, id);
        return ContainerOperationResult.Success;
    }

    /// <summary>Removes items from the stack at an index for a peer, when the stack equals the expected one.</summary>
    /// <param name="peer">The writing peer.</param>
    /// <param name="id">The container id.</param>
    /// <param name="stackIndex">The stack index.</param>
    /// <param name="expected">The stack expected there.</param>
    /// <param name="count">How many to remove.</param>
    internal ContainerOperationResult Remove(int peer, ContainerId id, int stackIndex, ItemStackSnapshot expected, int count)
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

        Entry? entry = Find(peer, id, out ContainerOperationResult gate);
        if (entry is null)
        {
            return gate;
        }

        if (stackIndex >= entry.Contents.Count || entry.Contents[stackIndex] != expected)
        {
            return ContainerOperationResult.ContainerChanged;
        }

        if (count == expected.Count)
        {
            entry.Contents.RemoveAt(stackIndex);
        }
        else
        {
            entry.Contents[stackIndex] = new ItemStackSnapshot(expected.PrefabName, expected.Count - count, expected.Quality, expected.MaxStack);
        }

        Writers[peer].RemovedItems += count;
        RecordChestWrite(peer, id);
        return ContainerOperationResult.Success;
    }

    /// <summary>Captures the whole world with the seed flags: every inventory by peer, then every chest by id.</summary>
    internal List<ContainerSnapshot> Capture()
    {
        var all = new List<ContainerSnapshot>();
        foreach (Entry inventory in inventories)
        {
            all.Add(inventory.ToSnapshot(ContainerFlags.None));
        }

        foreach (ContainerId id in chestOrder)
        {
            all.Add(chests[id].ToSnapshot(chests[id].SeedFlags));
        }

        return all;
    }

    /// <summary>Gets the current contents of a chest, by value.</summary>
    /// <param name="id">The chest id.</param>
    internal ContainerSnapshot Chest(ContainerId id) => chests[id].ToSnapshot(chests[id].SeedFlags);

    /// <summary>Test-only corruption: removes the first stack found in any inventory or chest, so the conservation check must fail.</summary>
    internal void DropOneStack()
    {
        foreach (Entry entry in AllEntries())
        {
            if (entry.Contents.Count > 0)
            {
                entry.Contents.RemoveAt(0);
                return;
            }
        }
    }

    /// <summary>Test-only corruption: adds a second copy of the first stack found in any inventory or chest, so the conservation check must fail.</summary>
    internal void DuplicateOneStack()
    {
        foreach (Entry entry in AllEntries())
        {
            if (entry.Contents.Count > 0)
            {
                entry.Contents.Add(entry.Contents[0]);
                return;
            }
        }
    }

    private IEnumerable<Entry> AllEntries()
    {
        foreach (Entry inventory in inventories)
        {
            yield return inventory;
        }

        foreach (ContainerId id in chestOrder)
        {
            yield return chests[id];
        }
    }

    private void RecordChestWrite(int peer, ContainerId id)
    {
        if (chests.ContainsKey(id))
        {
            Authority.RecordWrite(peer, id);
        }
    }

    private static bool Matches(ItemStackSnapshot stack, ItemStackSnapshot items) =>
        string.Equals(stack.PrefabName, items.PrefabName, StringComparison.Ordinal) && stack.Quality == items.Quality;

    private ContainerFlags FlagsFor(int peer, ContainerId id)
    {
        ContainerFlags flags = chests[id].SeedFlags;
        return Authority.IsInUseByOther(peer, id) ? flags | ContainerFlags.InUseByOther : flags;
    }

    /// <summary>Finds the container a write addresses and applies the gate: NotFound, then NotReserved unless the inventory or granted.</summary>
    private Entry? Find(int peer, ContainerId id, out ContainerOperationResult gate)
    {
        gate = ContainerOperationResult.Success;
        if (id == inventories[peer].Id)
        {
            return inventories[peer];
        }

        if (!chests.TryGetValue(id, out Entry? entry))
        {
            gate = ContainerOperationResult.NotFound;
            return null;
        }

        if (!Authority.AllowWrite(peer, id))
        {
            gate = ContainerOperationResult.NotReserved;
            return null;
        }

        return entry;
    }

    /// <summary>The items a peer added and removed through the world since the last reset.</summary>
    internal sealed class WriteCounter
    {
        /// <summary>Gets or sets the items added.</summary>
        internal long AddedItems { get; set; }

        /// <summary>Gets or sets the items removed.</summary>
        internal long RemovedItems { get; set; }

        /// <summary>Zeroes both counters.</summary>
        internal void Reset()
        {
            AddedItems = 0;
            RemovedItems = 0;
        }
    }

    private sealed class Entry
    {
        private readonly ContainerSnapshot seed;

        internal Entry(ContainerSnapshot seed)
        {
            this.seed = seed;
            Contents = new List<ItemStackSnapshot>(seed.Contents);
        }

        internal ContainerId Id => seed.Id;

        internal int SlotCount => seed.SlotCount;

        internal ContainerFlags SeedFlags => seed.Flags;

        internal List<ItemStackSnapshot> Contents { get; }

        internal ContainerSnapshot ToSnapshot(ContainerFlags flags) =>
            new ContainerSnapshot(seed.Id, seed.Position, seed.Width, seed.Height, Contents, flags);
    }
}
