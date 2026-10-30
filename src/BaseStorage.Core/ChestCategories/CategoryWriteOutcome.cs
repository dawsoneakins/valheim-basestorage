namespace BaseStorage.Core.ChestCategories;

/// <summary>
/// How a chest category write ended (contract S-E004-003 section 2.3). The numeric values are contracted. Every value except
/// <see cref="Applied"/> is a refusal that the player is told about through the strings table.
/// </summary>
public enum CategoryWriteOutcome
{
    /// <summary>The value was written to the chest and the reservation released.</summary>
    Applied = 0,

    /// <summary>Vanilla access denies the local player, or the owner refused the request: nothing was written.</summary>
    Denied = 1,

    /// <summary>Another player has the chest open: nothing was written.</summary>
    InUse = 2,

    /// <summary>The owner did not answer within the reservation timeout: nothing was written.</summary>
    Timeout = 3,

    /// <summary>The chest, its owner or the world went away, or the chest is busy with another operation: nothing was written.</summary>
    Unavailable = 4,
}
