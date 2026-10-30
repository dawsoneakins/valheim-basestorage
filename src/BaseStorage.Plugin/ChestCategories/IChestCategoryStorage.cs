namespace BaseStorage.Plugin.ChestCategories;

using BaseStorage.Core.Snapshots;

/// <summary>
/// The game port of the chest category write driver (contract S-E004-003 section 3.1): the id of a chest, the raw text stored
/// under the chest category key, and the one write. The driver is game-free and is tested with a recording fake. Port members
/// never throw for a destroyed or invalid chest: they return false or null, and the write does nothing and returns false.
/// </summary>
/// <typeparam name="TChest">The chest type of the game.</typeparam>
internal interface IChestCategoryStorage<TChest>
    where TChest : class
{
    /// <summary>Gets the id of a chest.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="id">The id, when the call returns true.</param>
    /// <returns>False when the view is invalid or has no networked data.</returns>
    bool TryGetId(TChest chest, out ContainerId id);

    /// <summary>Reads the raw text stored under the chest category key.</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>The stored text, the empty string when the chest has no key, or null when the view is invalid.</returns>
    string? ReadRaw(TChest chest);

    /// <summary>Writes the raw text under the chest category key. Called only while the write gate of the reservation is open.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="value">The text to store; the empty string clears the value.</param>
    /// <returns>True when the value was written; false when the view is invalid.</returns>
    bool Write(TChest chest, string value);
}
