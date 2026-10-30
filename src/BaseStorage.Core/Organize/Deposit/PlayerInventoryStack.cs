namespace BaseStorage.Core.Organize.Deposit;

using System;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>One occupied slot of the player inventory as a Deposit sees it (deposit contract 3.1): where it is, what it holds and its traits. Immutable.</summary>
public sealed class PlayerInventoryStack
{
    /// <summary>Creates the view of one occupied slot.</summary>
    /// <param name="slot">The grid slot: X is the column (0 = left), Y the row (0 = top, the hotbar).</param>
    /// <param name="stack">The stack in the slot; not null.</param>
    /// <param name="traits">The equipped flag and the never-list groups the Plugin knows for the item.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stack"/> is null.</exception>
    public PlayerInventoryStack(InventorySlot slot, ItemStackSnapshot stack, InventoryItemTraits traits)
    {
        Slot = slot;
        Stack = stack ?? throw new ArgumentNullException(nameof(stack));
        Traits = traits;
    }

    /// <summary>Gets the grid slot.</summary>
    public InventorySlot Slot { get; }

    /// <summary>Gets the stack in the slot.</summary>
    public ItemStackSnapshot Stack { get; }

    /// <summary>Gets the traits the Plugin supplied for the item.</summary>
    public InventoryItemTraits Traits { get; }

    /// <summary>Gets a value indicating whether the item is equipped.</summary>
    public bool IsEquipped => (Traits & InventoryItemTraits.Equipped) != 0;
}
