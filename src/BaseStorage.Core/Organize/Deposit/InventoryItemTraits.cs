namespace BaseStorage.Core.Organize.Deposit;

using System;

/// <summary>
/// What the Plugin knows about one player-inventory stack beyond its <see cref="Categories.ItemKind"/> (deposit contract 2.1): whether
/// the item is equipped, and which never-list groups it belongs to. Core never derives these from the prefab name.
/// </summary>
[Flags]
public enum InventoryItemTraits
{
    /// <summary>No trait.</summary>
    None = 0,

    /// <summary>The item is worn or wielded by the player (the game's equipped flag).</summary>
    Equipped = 1,

    /// <summary>A mead or potion: a consumable that is not food, the <c>@mead</c> group.</summary>
    Mead = 4,

    /// <summary>Arrows and bolts, the <c>@ammo</c> group. A kind of Ammo in the kinds table counts as well.</summary>
    Ammo = 8,

    /// <summary>Fishing bait of any kind, the <c>@bait</c> group.</summary>
    Bait = 16,
}
