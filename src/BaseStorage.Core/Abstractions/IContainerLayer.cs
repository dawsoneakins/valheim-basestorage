namespace BaseStorage.Core.Abstractions;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Polled access to the game's containers. Nothing is async and there are no callbacks. Timeouts are not the
/// layer's job: Core times a pending reservation with <see cref="IClock"/> and releases it.
/// </summary>
public interface IContainerLayer
{
    /// <summary>Gets the local player's inventory, addressed like a container; none when there is no local player.</summary>
    ContainerId PlayerInventoryId { get; }

    /// <summary>Gets every loaded, registered container, excluding the player inventory, ordered by id ascending.</summary>
    /// <returns>Fresh snapshots ordered by <see cref="ContainerId.CompareTo(ContainerId)"/>.</returns>
    IReadOnlyList<ContainerSnapshot> GetContainers();

    /// <summary>Gets a fresh snapshot of one container, including the player inventory.</summary>
    /// <param name="id">The container id.</param>
    /// <returns>The snapshot, or null when the container is unknown or unloaded.</returns>
    ContainerSnapshot? GetContainer(ContainerId id);

    /// <summary>
    /// Starts acquiring write access. No-op when pending or granted; restarts (to pending) when none, denied or lost.
    /// The player inventory never needs a reservation, so this is a no-op for it.
    /// </summary>
    /// <param name="id">The container id.</param>
    void RequestReservation(ContainerId id);

    /// <summary>
    /// Gets the current reservation state. The player inventory is always granted. A container that is unknown or
    /// unloaded and was never requested is none; if it unloads while pending or granted it becomes lost.
    /// </summary>
    /// <param name="id">The container id.</param>
    /// <returns>The reservation status.</returns>
    ReservationStatus GetReservationStatus(ContainerId id);

    /// <summary>Gives write access back. Always ends in none; idempotent; unknown ids and the player inventory are a no-op.</summary>
    /// <param name="id">The container id.</param>
    void ReleaseReservation(ContainerId id);

    /// <summary>
    /// Adds <c>items.Count</c> items of the prefab and quality, all or nothing. Check order: unknown or unloaded gives
    /// NotFound, then a status other than granted gives NotReserved, then a container that cannot accept all items gives
    /// DestinationFull and nothing is added. Existing partial stacks of the same prefab and quality are topped up first,
    /// then empty slots are used, with <c>items.MaxStack</c> as the size of new stacks. Success may renumber contents, so
    /// callers must fetch the container again before using a stack index. On any non-success result the container is unchanged.
    /// </summary>
    /// <param name="id">The container id.</param>
    /// <param name="items">The items to add; the prefab, quality, count and max stack are used.</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
    ContainerOperationResult AddItems(ContainerId id, ItemStackSnapshot items);

    /// <summary>
    /// Names the exact live stack the next <see cref="AddItems"/> copies from (S-A-004). Two stacks with the same item, quality and
    /// count can differ in durability or crafter, so a move must never look its source up by those values.
    /// </summary>
    /// <param name="source">The source container.</param>
    /// <param name="stackIndex">The index of the stack in the source's live contents.</param>
    void SelectSourceStack(ContainerId source, int stackIndex);

    /// <summary>
    /// Removes items from the stack at an index, provided that stack still equals the expected one. Check order: unknown
    /// or unloaded gives NotFound, then a status other than granted gives NotReserved, then an index at or beyond the
    /// contents, or a stack that differs by value from the expected one, gives ContainerChanged. On success the
    /// stack shrinks by the count; when it equals the stack's count the stack disappears and later indices
    /// shift down. Success may renumber contents. On any non-success result the container is unchanged.
    /// </summary>
    /// <param name="id">The container id.</param>
    /// <param name="stackIndex">The index of the stack in the container's contents; not negative.</param>
    /// <param name="expected">The stack the caller expects at that index.</param>
    /// <param name="count">How many items to remove; from 1 to the expected stack's count.</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expected"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stackIndex"/> or <paramref name="count"/> is out of range.</exception>
    ContainerOperationResult RemoveFromStack(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count);
}
