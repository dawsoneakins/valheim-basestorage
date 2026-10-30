namespace BaseStorage.Core.ChestCategories;

/// <summary>
/// What a call that asks for a chest category write returns (contract S-E004-003 section 2.3). The numeric values are
/// contracted. <see cref="Pending"/> means the reservation is in flight and the result arrives on a later frame.
/// </summary>
public enum CategoryWriteStatus
{
    /// <summary>The value was written during the call (the local peer owned the chest, or another fast path applied).</summary>
    Applied = 0,

    /// <summary>A reservation request was sent; the write completes or is refused on a later frame.</summary>
    Pending = 1,

    /// <summary>The chest already holds the requested value: nothing was reserved or written.</summary>
    Unchanged = 2,

    /// <summary>The write was refused during the call and the player was told why.</summary>
    Refused = 3,
}
