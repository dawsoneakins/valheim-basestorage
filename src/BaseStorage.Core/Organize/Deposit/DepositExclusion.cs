namespace BaseStorage.Core.Organize.Deposit;

using System;

/// <summary>Why a Deposit keeps a player-inventory stack where it is (deposit contract 2.2). Several reasons may apply at once.</summary>
[Flags]
public enum DepositExclusion
{
    /// <summary>Not excluded: the stack may be deposited.</summary>
    None = 0,

    /// <summary>The stack sits in the hotbar row, row <see cref="DepositPlanner.HotbarRow"/> (spec AC-4.9 "row 1").</summary>
    Hotbar = 1,

    /// <summary>The item is equipped (<see cref="InventoryItemTraits.Equipped"/>).</summary>
    Equipped = 2,

    /// <summary>The slot is a mod favourite (<see cref="Persistence.FavouriteSlots"/>).</summary>
    Favourite = 4,

    /// <summary>The item is on the DepositNeverItems list, by prefab name or by <c>@</c> group.</summary>
    NeverList = 8,

    /// <summary>
    /// The stack is the same item (prefab, quality, max stack) as an excluded stack. It is kept back because the executor locates a
    /// source stack by value, so it could not tell the two apart.
    /// </summary>
    SameItemAsExcluded = 16,

    /// <summary>The stack's count is above its max stack: unclear contents, kept back for safety.</summary>
    InvalidStack = 32,

    /// <summary>Put away selected is running and this stack is not one of the selected ones.</summary>
    NotSelected = 64,
}
