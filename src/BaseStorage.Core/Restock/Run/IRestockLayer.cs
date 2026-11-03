namespace BaseStorage.Core.Restock.Run;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The game port of one Restock run (S-QR-002 contract section 3.1). The Plugin implements it over the gathered chests and the captured
/// player slots; the Core tests implement it with a recording fake that can merge part of an add and then fail. Core never sees grid
/// coordinates.
/// </summary>
public interface IRestockLayer
{
    /// <summary>Requests the reservation of a chest.</summary>
    /// <param name="chest">The chest.</param>
    void RequestReservation(ContainerId chest);

    /// <summary>Gets the reservation status of a chest.</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>The status.</returns>
    ReservationStatus GetReservationStatus(ContainerId chest);

    /// <summary>Releases the reservation of a chest.</summary>
    /// <param name="chest">The chest.</param>
    void ReleaseReservation(ContainerId chest);

    /// <summary>Gets the live chest with identity-filled stacks (P4).</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>The chest; null when it is gone or unreadable.</returns>
    ContainerSnapshot? GetContainer(ContainerId chest);

    /// <summary>Removes count items from Contents[stackIndex] when it equals expected (identity included) and the chest is reserved.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="stackIndex">The stack index in the chest.</param>
    /// <param name="expected">The stack the removal expects.</param>
    /// <param name="count">The number of items to remove.</param>
    /// <returns>The outcome of the removal.</returns>
    ContainerOperationResult RemoveFromStack(ContainerId chest, int stackIndex, ItemStackSnapshot expected, int count);

    /// <summary>Gets the live stack at the captured slot of carried[carriedIndex], identity filled.</summary>
    /// <param name="carriedIndex">The index into the carried list the plan was made from.</param>
    /// <returns>The stack; null when the slot is empty or unreadable.</returns>
    ItemStackSnapshot? ReadCarried(int carriedIndex);

    /// <summary>Adds count items of expected's key to that exact slot only, when the live stack equals expected. The return value is advisory (RR6).</summary>
    /// <param name="carriedIndex">The index into the carried list.</param>
    /// <param name="expected">The stack the add expects.</param>
    /// <param name="count">The number of items to add.</param>
    /// <returns>True when the game reports everything added; advisory only.</returns>
    bool AddToCarried(int carriedIndex, ItemStackSnapshot expected, int count);

    /// <summary>Removes count items from that exact slot when the live stack equals expected (rollback, RR9).</summary>
    /// <param name="carriedIndex">The index into the carried list.</param>
    /// <param name="expected">The stack the removal expects.</param>
    /// <param name="count">The number of items to remove.</param>
    /// <returns>True when removed.</returns>
    bool RemoveFromCarried(int carriedIndex, ItemStackSnapshot expected, int count);
}
