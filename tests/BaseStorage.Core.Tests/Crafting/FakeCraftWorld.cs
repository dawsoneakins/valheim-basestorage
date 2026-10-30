namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.ChestAccess;
using BaseStorage.Plugin.Crafting;

/// <summary>A stack kept by a take, under the id it came from.</summary>
/// <param name="Id">The container the stack was taken from.</param>
/// <param name="Stack">The kept clone.</param>
internal readonly record struct KeptStack(ContainerId Id, ItemStackSnapshot Stack);

/// <summary>
/// The game world double of contracts 4.1 and 7.2: contents by id, kept clones of every taken stack, an injectable take
/// failure and the chest views. Not a test row (ADR-GWIN-124).
/// </summary>
internal sealed class FakeCraftWorld : ICraftWorld<FakeChest>
{
    private readonly Dictionary<ContainerId, List<ItemStackSnapshot>> contents = new();
    private readonly Dictionary<ContainerId, FakeChest> chests = new();
    private readonly Dictionary<ContainerId, (WorldPosition Position, int Width, int Height)> shapes = new();
    private readonly List<KeptStack> kept = new();

    /// <summary>Initialises an empty world with the player inventory.</summary>
    internal FakeCraftWorld()
    {
        InventoryId = CraftFixtures.Inventory;
        contents[InventoryId] = new List<ItemStackSnapshot>();
        shapes[InventoryId] = (new WorldPosition(0f, 0f, 0f), 8, 4);
    }

    /// <summary>Gets or sets the player inventory id; the layer reads it on every call.</summary>
    public ContainerId InventoryId { get; set; }

    /// <summary>Gets the ordered call record of the takes, put backs, returns and forgets.</summary>
    internal List<string> Calls { get; } = new();

    /// <summary>Gets or sets the 1-based number of the Take call that fails with <see cref="FailWith"/>; null means none.</summary>
    internal int? FailTakeNumber { get; set; }

    /// <summary>Gets or sets the result of the failing take.</summary>
    internal ContainerOperationResult FailWith { get; set; } = ContainerOperationResult.ContainerChanged;

    /// <summary>Gets or sets an exception that every Take throws when set.</summary>
    internal Exception? ThrowOnTake { get; set; }

    /// <summary>Gets the number of Take calls so far.</summary>
    internal int TakeCount { get; private set; }

    /// <summary>Gets the stacks kept by takes and not yet put back, returned or forgotten.</summary>
    internal IReadOnlyList<KeptStack> Kept => kept;

    /// <summary>Replaces the player inventory contents.</summary>
    /// <param name="stacks">The stacks, in order.</param>
    internal void SetInventory(params ItemStackSnapshot[] stacks) => contents[InventoryId] = stacks.ToList();

    /// <summary>Registers a loaded chest with its contents; c5 sits at 5 m and every other chest at 10 m.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="stacks">The stacks, in order.</param>
    internal void AddChest(FakeChest chest, params ItemStackSnapshot[] stacks)
    {
        chests[chest.Id] = chest;
        contents[chest.Id] = stacks.ToList();
        shapes[chest.Id] = (new WorldPosition(chest.Id.Equals(CraftFixtures.C5) ? 5f : 10f, 0f, 0f), 5, 2);
    }

    /// <summary>Unloads a chest: its id resolves to nothing afterwards.</summary>
    /// <param name="id">The chest id.</param>
    internal void Unload(ContainerId id) => chests.Remove(id);

    /// <summary>Moves stacks out of a container without the craft, as another player would.</summary>
    /// <param name="id">The container.</param>
    /// <param name="stackIndex">The stack index.</param>
    /// <param name="count">The number moved out.</param>
    internal void MoveOut(ContainerId id, int stackIndex, int count)
    {
        List<ItemStackSnapshot> list = contents[id];
        ItemStackSnapshot stack = list[stackIndex];
        if (stack.Count == count)
        {
            list.RemoveAt(stackIndex);
        }
        else
        {
            list[stackIndex] = new ItemStackSnapshot(stack.PrefabName, stack.Count - count, stack.Quality, stack.MaxStack);
        }
    }

    /// <summary>Gets the current stacks of a container.</summary>
    /// <param name="id">The container.</param>
    /// <returns>The stacks, in order.</returns>
    internal IReadOnlyList<ItemStackSnapshot> ContentsOf(ContainerId id) => contents[id];

    /// <summary>Counts one prefab over the inventory, the chests and the kept clones.</summary>
    /// <param name="prefab">The prefab name.</param>
    /// <returns>The total count.</returns>
    internal int Total(string prefab) =>
        contents.Values.SelectMany(list => list).Concat(kept.Select(item => item.Stack))
            .Where(stack => stack.PrefabName == prefab).Sum(stack => stack.Count);

    /// <summary>Gets the views of the inventory and every registered chest, as the layer reads them.</summary>
    /// <returns>The views.</returns>
    internal IEnumerable<ContainerSnapshot> Views() =>
        new[] { InventoryId }.Concat(chests.Keys).Select(id => Read(id)!).ToArray();

    /// <summary>Finds a loaded chest with a valid view.</summary>
    /// <param name="id">The chest id.</param>
    /// <returns>The chest, or null.</returns>
    public FakeChest? FindChest(ContainerId id) =>
        chests.TryGetValue(id, out FakeChest? chest) && chest.ViewValid ? chest : null;

    /// <summary>Reads a fresh snapshot of the inventory or a chest.</summary>
    /// <param name="id">The container id.</param>
    /// <returns>The snapshot, or null when unknown or unloaded.</returns>
    public ContainerSnapshot? Read(ContainerId id)
    {
        if (!contents.TryGetValue(id, out List<ItemStackSnapshot>? list) || !shapes.TryGetValue(id, out var shape))
        {
            return null;
        }

        if (!id.Equals(InventoryId) && (!chests.TryGetValue(id, out FakeChest? chest) || !chest.ViewValid))
        {
            return null;
        }

        return new ContainerSnapshot(id, shape.Position, shape.Width, shape.Height, list.ToArray(),
            id.Equals(InventoryId) ? ContainerFlags.None : ContainerFlags.PlayerBuilt);
    }

    /// <summary>Takes a count of one stack and keeps a clone of it under the id.</summary>
    /// <param name="id">The container id.</param>
    /// <param name="stackIndex">The stack index.</param>
    /// <param name="expected">The stack the caller expects at that index.</param>
    /// <param name="count">The number to take.</param>
    /// <returns>The result of the take.</returns>
    public ContainerOperationResult Take(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count)
    {
        TakeCount++;
        Calls.Add("Take " + id.Id + " " + stackIndex + " " + count);
        if (ThrowOnTake is not null)
        {
            throw ThrowOnTake;
        }

        if (FailTakeNumber == TakeCount)
        {
            return FailWith;
        }

        if (!contents.TryGetValue(id, out List<ItemStackSnapshot>? list) || (!id.Equals(InventoryId) && !chests.ContainsKey(id)))
        {
            return ContainerOperationResult.NotFound;
        }

        if (stackIndex < 0 || stackIndex >= list.Count || !SameKind(list[stackIndex], expected) || list[stackIndex].Count < count)
        {
            return ContainerOperationResult.ContainerChanged;
        }

        ItemStackSnapshot item = list[stackIndex];
        kept.Add(new KeptStack(id, new ItemStackSnapshot(item.PrefabName, count, item.Quality, item.MaxStack)));
        if (item.Count == count)
        {
            list.RemoveAt(stackIndex);
        }
        else
        {
            list[stackIndex] = new ItemStackSnapshot(item.PrefabName, item.Count - count, item.Quality, item.MaxStack);
        }

        return ContainerOperationResult.Success;
    }

    /// <summary>Puts a kept clone back: the clone under the id first, then any kept clone of the same stack.</summary>
    /// <param name="id">The container id.</param>
    /// <param name="items">The stack to put back.</param>
    /// <returns>Success, or DestinationFull when no kept clone matches or the target is full.</returns>
    public ContainerOperationResult PutBack(ContainerId id, ItemStackSnapshot items)
    {
        Calls.Add("PutBack " + id.Id);
        int index = kept.FindIndex(item => item.Id.Equals(id) && SameStack(item.Stack, items));
        if (index < 0)
        {
            index = kept.FindIndex(item => SameStack(item.Stack, items));
        }

        if (index < 0 || !contents.TryGetValue(id, out List<ItemStackSnapshot>? list) || list.Count >= Capacity(id))
        {
            return ContainerOperationResult.DestinationFull;
        }

        list.Add(kept[index].Stack);
        kept.RemoveAt(index);
        return ContainerOperationResult.Success;
    }

    /// <summary>Returns every kept clone to the player inventory, in take order.</summary>
    /// <returns>The number of clones returned.</returns>
    public int ReturnAllTaken()
    {
        Calls.Add("ReturnAllTaken");
        int returned = kept.Count;
        foreach (KeptStack item in kept)
        {
            contents[InventoryId].Add(item.Stack);
        }

        kept.Clear();
        return returned;
    }

    /// <summary>Clears the kept clones without touching the contents.</summary>
    public void ForgetTaken()
    {
        Calls.Add("ForgetTaken");
        kept.Clear();
    }

    private static bool SameKind(ItemStackSnapshot stack, ItemStackSnapshot expected) =>
        stack.PrefabName == expected.PrefabName && stack.Quality == expected.Quality;

    private static bool SameStack(ItemStackSnapshot stack, ItemStackSnapshot items) =>
        SameKind(stack, items) && stack.Count == items.Count;

    private int Capacity(ContainerId id) => id.Equals(InventoryId) ? 32 : 10;
}
