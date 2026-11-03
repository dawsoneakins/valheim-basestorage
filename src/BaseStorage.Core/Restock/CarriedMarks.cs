namespace BaseStorage.Core.Restock;

using System;

/// <summary>Marks that capture attaches to one carried stack (S-QR-001, contract 3.1). Valid values are any combination of the defined bits.</summary>
[Flags]
public enum CarriedMarks
{
    /// <summary>Neither on the hotbar nor of a favourite type.</summary>
    None = 0,

    /// <summary>The stack sits in a hotbar slot.</summary>
    Hotbar = 1,

    /// <summary>The stack's item type is in the favourite item types.</summary>
    FavouriteType = 2,

    /// <summary>The slot is starred. Carried for completeness; restock ignores this bit (audit O4).</summary>
    StarredSlot = 4,
}
