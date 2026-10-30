namespace BaseStorage.Core.Organize.Run;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The pure decisions of the game container layer (contract 4.2): what the reservation driver's state means as a layer status,
/// whether a stack fits a chest, and which live stack a planned move comes from. Every member is pure.
/// </summary>
public static class OrganizeLayerRules
{
    /// <summary>The reservation operation label of an Organize (a Debug label, not user-facing text).</summary>
    public const string Operation = "organize";

    /// <summary>Maps the driver's reservation of a chest to the layer status (contract 4.2 table).</summary>
    /// <param name="state">The driver's reservation state, or null when nothing is stored.</param>
    /// <param name="failure">The failure of a Failed reservation; ignored otherwise.</param>
    /// <param name="busy">True when Begin returned null because another operation holds the chest.</param>
    /// <returns>The status the executor acts on.</returns>
    public static ReservationStatus StatusOf(ReservationState? state, ReservationFailure failure, bool busy)
    {
        if (busy)
        {
            return ReservationStatus.Denied;
        }

        if (state is null)
        {
            return ReservationStatus.None;
        }

        switch (state.Value)
        {
            case ReservationState.Requesting:
                return ReservationStatus.Pending;
            case ReservationState.Granted:
            case ReservationState.Writing:
                return ReservationStatus.Granted;
            case ReservationState.Completed:
                return ReservationStatus.None;
            case ReservationState.Failed:
                return FailedStatus(failure);
            default:
                return ReservationStatus.Lost;
        }
    }

    /// <summary>Tells whether the live contents can take the whole stack: free slots times the max stack plus the room in same-item partial stacks.</summary>
    /// <param name="contents">The live stacks of the chest.</param>
    /// <param name="slotCount">The chest's slot count.</param>
    /// <param name="items">The stack to add; its prefab, quality, count and max stack are used.</param>
    /// <returns>True when the whole count fits.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contents"/> or <paramref name="items"/> is null.</exception>
    public static bool CanFit(IReadOnlyList<ItemStackSnapshot> contents, int slotCount, ItemStackSnapshot items)
    {
        if (contents is null)
        {
            throw new ArgumentNullException(nameof(contents));
        }

        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (slotCount < contents.Count)
        {
            return false;
        }

        long room = (slotCount - contents.Count) * (long)items.MaxStack;
        for (int i = 0; i < contents.Count; i++)
        {
            ItemStackSnapshot stack = contents[i];
            if (SameItem(stack, items.PrefabName, items.Quality) && stack.Count < items.MaxStack)
            {
                room += items.MaxStack - stack.Count;
            }
        }

        return room >= items.Count;
    }

    /// <summary>Finds the first live stack of an item with at least the given count.</summary>
    /// <param name="contents">The live stacks of the source chest.</param>
    /// <param name="prefabName">The item's prefab name, compared ordinally.</param>
    /// <param name="quality">The item's quality.</param>
    /// <param name="maxStack">The item's max stack.</param>
    /// <param name="count">The number of items the move takes.</param>
    /// <returns>The index of the first matching stack, or -1 when none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contents"/> or <paramref name="prefabName"/> is null.</exception>
    public static int IndexOfSource(IReadOnlyList<ItemStackSnapshot> contents, string prefabName, int quality, int maxStack, int count)
    {
        if (contents is null)
        {
            throw new ArgumentNullException(nameof(contents));
        }

        if (prefabName is null)
        {
            throw new ArgumentNullException(nameof(prefabName));
        }

        for (int i = 0; i < contents.Count; i++)
        {
            ItemStackSnapshot stack = contents[i];
            if (SameItem(stack, prefabName, quality) && stack.MaxStack == maxStack && stack.Count >= count)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool SameItem(ItemStackSnapshot stack, string prefabName, int quality) =>
        string.Equals(stack.PrefabName, prefabName, StringComparison.Ordinal) && stack.Quality == quality;

    private static ReservationStatus FailedStatus(ReservationFailure failure)
    {
        switch (failure)
        {
            case ReservationFailure.Denied:
            case ReservationFailure.InUseByOther:
                return ReservationStatus.Denied;
            case ReservationFailure.Timeout:
                return ReservationStatus.Pending;
            default:
                return ReservationStatus.Lost;
        }
    }
}
