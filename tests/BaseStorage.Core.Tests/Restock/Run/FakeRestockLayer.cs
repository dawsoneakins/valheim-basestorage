namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Restock.Run;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Snapshots.Identities;
using BaseStorage.Core.Tests.Snapshots.Identities;

/// <summary>How one add to a carried slot behaves (contract section 10.1, and the doubt cases of RR6 and RR8).</summary>
internal enum RestockAddMode
{
    /// <summary>Adds min(count, room) and returns true only when everything was added.</summary>
    Normal,

    /// <summary>The first add merges the scripted part and returns false; later adds behave as Normal.</summary>
    PartialOnce,

    /// <summary>Adds nothing and returns false.</summary>
    Refuse,

    /// <summary>Adds like Normal but replaces the stack's identity, so the re-read differs (RR8).</summary>
    Corrupt,

    /// <summary>The slot is empty after the add, and the add returns true (RR8: a null re-read).</summary>
    Vanish,

    /// <summary>The count drops by one after the add, and the add returns true (RR8: a negative delta).</summary>
    Shrink,

    /// <summary>More than count arrives, and the add returns true (RR8: a delta above count).</summary>
    Overfill,

    /// <summary>Adds nothing and returns true (RR6: the return value is advisory; delta 0).</summary>
    ReturnsTrueAddsNothing,

    /// <summary>Adds everything and returns false (RR6: the return value is advisory).</summary>
    ReturnsFalseAddsAll,
}

/// <summary>
/// The recording Restock layer of the Core run tests (contract section 10.1). Chest slots keep their plan indices: a slot emptied by a
/// remove stays in place as null and is left out of <see cref="IRestockLayer.GetContainer"/>. Every call is recorded in <see cref="Calls"/>.
/// Test code, not a row: the fake is itself proven by GW-U-2600.
/// </summary>
internal sealed class FakeRestockLayer : IRestockLayer
{
    private readonly Dictionary<uint, ContainerSnapshot> chestMeta = new Dictionary<uint, ContainerSnapshot>();
    private readonly Dictionary<uint, List<ItemStackSnapshot?>> chestSlots = new Dictionary<uint, List<ItemStackSnapshot?>>();
    private readonly List<ItemStackSnapshot?> carriedSlots = new List<ItemStackSnapshot?>();
    private readonly Dictionary<int, RestockAddMode> modes = new Dictionary<int, RestockAddMode>();
    private readonly Dictionary<int, int> partialAmounts = new Dictionary<int, int>();
    private readonly HashSet<int> partialFired = new HashSet<int>();
    private readonly Dictionary<uint, Queue<ReservationStatus>> answers = new Dictionary<uint, Queue<ReservationStatus>>();
    private readonly Dictionary<uint, ReservationStatus> reserved = new Dictionary<uint, ReservationStatus>();
    private readonly Dictionary<string, ContainerOperationResult> removeOverrides = new Dictionary<string, ContainerOperationResult>();
    private readonly List<(int CallIndex, int CarriedIndex, int? Count)> reads = new List<(int CallIndex, int CarriedIndex, int? Count)>();

    /// <summary>Creates the layer over the carried stacks and the chests.</summary>
    /// <param name="carried">The carried stacks, in plan index order.</param>
    /// <param name="chests">The chests; their stacks become the slots.</param>
    public FakeRestockLayer(IReadOnlyList<CarriedStack> carried, IEnumerable<ContainerSnapshot> chests)
    {
        foreach (CarriedStack stack in carried)
        {
            carriedSlots.Add(stack.Stack);
        }

        foreach (ContainerSnapshot chest in chests)
        {
            chestMeta[chest.Id.Id] = chest;
            chestSlots[chest.Id.Id] = chest.Contents.Cast<ItemStackSnapshot?>().ToList();
        }
    }

    /// <summary>Gets every call in order, as the contract log writes it: Req, Add, Remove, Release, Get, Read and Unadd.</summary>
    public List<string> Calls { get; } = new List<string>();

    /// <summary>Gets or sets a value that makes every rollback fail (RR9).</summary>
    public bool RollbackFails { get; set; }

    /// <summary>Gets or sets an exception thrown by the status, container and carried reads (a tick that throws).</summary>
    public Exception? ThrowOnCall { get; set; }

    /// <summary>Gets every carried read in order: the index of its call in <see cref="Calls"/>, the carried index and the count seen (null for an empty slot).</summary>
    public IReadOnlyList<(int CallIndex, int CarriedIndex, int? Count)> Reads => reads;

    /// <summary>Gets the calls without the reads (Get and Read), which the contract log filters.</summary>
    /// <returns>The write calls in order.</returns>
    public List<string> Writes() => Calls.Where(line => !line.StartsWith("Get ", StringComparison.Ordinal) && !line.StartsWith("Read ", StringComparison.Ordinal)).ToList();

    /// <summary>Sets how the add to carried slot <paramref name="target"/> behaves.</summary>
    /// <param name="target">The carried index.</param>
    /// <param name="mode">The mode.</param>
    /// <param name="partial">The number of items a PartialOnce add merges before it fails.</param>
    public void SetMode(int target, RestockAddMode mode, int partial = 0)
    {
        modes[target] = mode;
        partialAmounts[target] = partial;
    }

    /// <summary>Scripts the reservation answers of a chest, one per status poll; the last answer repeats. Granted when not scripted.</summary>
    /// <param name="chest">The chest id.</param>
    /// <param name="statuses">The answers in order.</param>
    public void SetReservation(uint chest, params ReservationStatus[] statuses) => answers[chest] = new Queue<ReservationStatus>(statuses);

    /// <summary>Makes the next remove of one slot return the given result, once.</summary>
    /// <param name="chest">The chest id.</param>
    /// <param name="index">The slot index.</param>
    /// <param name="result">The result to return.</param>
    public void FailRemoveOnce(uint chest, int index, ContainerOperationResult result) => removeOverrides[chest + "#" + index] = result;

    /// <summary>Replaces a carried slot before the run touches it (a stale stack).</summary>
    /// <param name="index">The carried index.</param>
    /// <param name="stack">The new live stack.</param>
    public void MutateCarried(int index, ItemStackSnapshot? stack) => carriedSlots[index] = stack;

    /// <summary>Gets the carried slots; null where the slot is empty.</summary>
    public IReadOnlyList<ItemStackSnapshot?> Carried => carriedSlots;

    /// <summary>Gets the slots of one chest; null where the slot is empty.</summary>
    /// <param name="chest">The chest id.</param>
    /// <returns>The slots.</returns>
    public IReadOnlyList<ItemStackSnapshot?> Chest(uint chest) => chestSlots[chest];

    /// <summary>Counts the items of one prefab on the carried side and the chest side.</summary>
    /// <param name="prefab">The prefab name.</param>
    /// <returns>The carried total and the chest total.</returns>
    public (int Carried, int Chests) Totals(string prefab)
    {
        int carried = carriedSlots.Where(s => s is not null && s.PrefabName == prefab).Sum(s => s!.Count);
        int chests = chestSlots.Values.SelectMany(slots => slots).Where(s => s is not null && s.PrefabName == prefab).Sum(s => s!.Count);
        return (carried, chests);
    }

    /// <inheritdoc />
    public void RequestReservation(ContainerId chest)
    {
        Calls.Add("Req " + chest.Id);
        reserved[chest.Id] = ReservationStatus.None;
    }

    /// <inheritdoc />
    public ReservationStatus GetReservationStatus(ContainerId chest)
    {
        Guard();
        ReservationStatus status = NextAnswer(chest.Id);
        reserved[chest.Id] = status;
        return status;
    }

    /// <inheritdoc />
    public void ReleaseReservation(ContainerId chest)
    {
        Calls.Add("Release " + chest.Id);
        reserved[chest.Id] = ReservationStatus.None;
    }

    /// <inheritdoc />
    public ContainerSnapshot? GetContainer(ContainerId chest)
    {
        Guard();
        Calls.Add("Get " + chest.Id);
        if (!chestMeta.TryGetValue(chest.Id, out ContainerSnapshot? meta))
        {
            return null;
        }

        List<ItemStackSnapshot?> slots = chestSlots[chest.Id];
        return new ContainerSnapshot(meta.Id, meta.Position, meta.Width, meta.Height, slots.Where(s => s is not null).Select(s => s!), meta.Flags);
    }

    /// <inheritdoc />
    public ContainerOperationResult RemoveFromStack(ContainerId chest, int stackIndex, ItemStackSnapshot expected, int count)
    {
        Calls.Add("Remove " + chest.Id + "#" + stackIndex + " " + count);
        string key = chest.Id + "#" + stackIndex;
        if (removeOverrides.TryGetValue(key, out ContainerOperationResult forced))
        {
            removeOverrides.Remove(key);
            return forced;
        }

        if (!reserved.TryGetValue(chest.Id, out ReservationStatus status) || status != ReservationStatus.Granted)
        {
            return ContainerOperationResult.NotReserved;
        }

        if (!chestSlots.TryGetValue(chest.Id, out List<ItemStackSnapshot?>? slots) || stackIndex < 0 || stackIndex >= slots.Count)
        {
            return ContainerOperationResult.NotFound;
        }

        ItemStackSnapshot? live = slots[stackIndex];
        if (live is null || live != expected || count < 1 || count > live.Count)
        {
            return ContainerOperationResult.ContainerChanged;
        }

        slots[stackIndex] = Reduced(live, live.Count - count);
        return ContainerOperationResult.Success;
    }

    /// <inheritdoc />
    public ItemStackSnapshot? ReadCarried(int carriedIndex)
    {
        Guard();
        Calls.Add("Read t" + carriedIndex);
        ItemStackSnapshot? seen = carriedIndex >= 0 && carriedIndex < carriedSlots.Count ? carriedSlots[carriedIndex] : null;
        reads.Add((Calls.Count - 1, carriedIndex, seen?.Count));
        return seen;
    }

    /// <summary>
    /// Gets the items that the confirming re-read proves for one remove (RR6): the target's count at the last read after its add,
    /// less its count at the last read before that add. The target is the carried index of the nearest add before the remove.
    /// </summary>
    /// <param name="removeCall">The index of the Remove call in <see cref="Calls"/>.</param>
    /// <returns>The confirmed delta, or null when no add precedes the remove or no re-read follows that add.</returns>
    public int? ConfirmedDelta(int removeCall)
    {
        int add = -1;
        for (int i = removeCall - 1; i >= 0; i--)
        {
            if (Calls[i].StartsWith("Add t", StringComparison.Ordinal))
            {
                add = i;
                break;
            }
        }

        if (add < 0)
        {
            return null;
        }

        int target = int.Parse(Calls[add].Split(' ')[1].Substring(1), System.Globalization.CultureInfo.InvariantCulture);
        int? before = null;
        int? after = null;
        bool afterRead = false;
        foreach ((int call, int carriedIndex, int? count) in reads)
        {
            if (carriedIndex != target)
            {
                continue;
            }

            if (call < add)
            {
                before = count;
            }
            else if (call > add && call < removeCall)
            {
                after = count;
                afterRead = true;
            }
        }

        return afterRead ? (after ?? 0) - (before ?? 0) : (int?)null;
    }

    /// <inheritdoc />
    public bool AddToCarried(int carriedIndex, ItemStackSnapshot expected, int count)
    {
        Calls.Add("Add t" + carriedIndex + " " + count);
        ItemStackSnapshot? live = carriedSlots[carriedIndex];
        if (live is null || live != expected)
        {
            return false;
        }

        RestockAddMode mode = modes.TryGetValue(carriedIndex, out RestockAddMode configured) ? configured : RestockAddMode.Normal;
        int room = Math.Max(0, live.MaxStack - live.Count);
        switch (mode)
        {
            case RestockAddMode.Refuse:
                return false;
            case RestockAddMode.ReturnsTrueAddsNothing:
                return true;
            case RestockAddMode.ReturnsFalseAddsAll:
                carriedSlots[carriedIndex] = Reduced(live, live.Count + count);
                return false;
            case RestockAddMode.Vanish:
                carriedSlots[carriedIndex] = null;
                return true;
            case RestockAddMode.Shrink:
                carriedSlots[carriedIndex] = Reduced(live, live.Count - 1);
                return true;
            case RestockAddMode.Overfill:
                carriedSlots[carriedIndex] = Reduced(live, live.Count + count + 1);
                return true;
            case RestockAddMode.PartialOnce when partialFired.Add(carriedIndex):
                // Edited by S-QR-002 (ADR-GWIN-176): contract 10.11, the scripted merge is 1 <= n < c for the actual add amount c.
                // When c is 1 there is no merge that fits, so the add behaves as Refuse. The merge also stays within the room.
                int partialMerge = count <= 1 ? 0 : Math.Min(Math.Max(1, partialAmounts[carriedIndex]), count - 1);
                carriedSlots[carriedIndex] = Reduced(live, live.Count + Math.Min(partialMerge, room));
                return false;
        }

        int added = Math.Min(count, room);
        ItemStackIdentity identity = mode == RestockAddMode.Corrupt
            ? (live.Identity.Equals(IdentityFixtures.Wl2()) ? ItemStackIdentity.Empty : IdentityFixtures.Wl2())
            : live.Identity;
        carriedSlots[carriedIndex] = Reduced(live, live.Count + added, identity);
        return added == count;
    }

    /// <inheritdoc />
    public bool RemoveFromCarried(int carriedIndex, ItemStackSnapshot expected, int count)
    {
        Calls.Add("Unadd t" + carriedIndex + " " + count);
        if (RollbackFails)
        {
            return false;
        }

        ItemStackSnapshot? live = carriedSlots[carriedIndex];
        if (live is null || live != expected || count < 1 || count > live.Count)
        {
            return false;
        }

        carriedSlots[carriedIndex] = Reduced(live, live.Count - count);
        return true;
    }

    private void Guard()
    {
        if (ThrowOnCall is not null)
        {
            throw ThrowOnCall;
        }
    }

    private ReservationStatus NextAnswer(uint chest)
    {
        if (!answers.TryGetValue(chest, out Queue<ReservationStatus>? queue) || queue.Count == 0)
        {
            return ReservationStatus.Granted;
        }

        return queue.Count > 1 ? queue.Dequeue() : queue.Peek();
    }

    private ItemStackSnapshot? Reduced(ItemStackSnapshot stack, int count, ItemStackIdentity? identity = null) =>
        count <= 0 ? null : new ItemStackSnapshot(stack.PrefabName, count, stack.Quality, stack.MaxStack, identity ?? stack.Identity);
}
