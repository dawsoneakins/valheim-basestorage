namespace BaseStorage.Plugin.Crafting;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;

/// <summary>The world port under the craft container layer (contract craft-from-base 4.1). It never checks reservations.</summary>
/// <typeparam name="TChest">The chest type of the game.</typeparam>
internal interface ICraftWorld<TChest>
    where TChest : class
{
    /// <summary>Gets the player inventory id; none when there is no local player.</summary>
    ContainerId InventoryId { get; }

    /// <summary>Finds a registered, loaded chest by id.</summary>
    /// <param name="id">The chest id.</param>
    /// <returns>The chest, or null.</returns>
    TChest? FindChest(ContainerId id);

    /// <summary>Reads a fresh snapshot of the inventory or a chest.</summary>
    /// <param name="id">The container id.</param>
    /// <returns>The snapshot, or null when unknown, unloaded or unreadable.</returns>
    ContainerSnapshot? Read(ContainerId id);

    /// <summary>Takes items from a stack, keeping a clone.</summary>
    /// <param name="id">The container id.</param>
    /// <param name="stackIndex">The stack index.</param>
    /// <param name="expected">The expected stack.</param>
    /// <param name="count">The count to take.</param>
    /// <returns>The result.</returns>
    ContainerOperationResult Take(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count);

    /// <summary>Puts a kept stack back into a container.</summary>
    /// <param name="id">The container id.</param>
    /// <param name="items">The stack to put back.</param>
    /// <returns>The result.</returns>
    ContainerOperationResult PutBack(ContainerId id, ItemStackSnapshot items);

    /// <summary>Returns every kept clone to the inventory, dropping the rest at the player.</summary>
    /// <returns>The number of clones.</returns>
    int ReturnAllTaken();

    /// <summary>Clears the kept clones without touching the game.</summary>
    void ForgetTaken();
}
