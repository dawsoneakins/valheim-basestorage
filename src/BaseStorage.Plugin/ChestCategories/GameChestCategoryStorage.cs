namespace BaseStorage.Plugin.ChestCategories;

using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The storage port over the networked data of a vanilla <see cref="Container"/> (contract S-E004-003 section 4.1). It is the
/// only Plugin code that writes the chest category key. Auto is written as the empty string, which every reader treats as no
/// value (ADR-GWIN-097). Game-bound, so it is not linked into the tests; its facts are proven by source scans. Main thread only;
/// Unity null checks use <c>==</c> (W4).
/// </summary>
internal sealed class GameChestCategoryStorage : IChestCategoryStorage<Container>
{
    /// <summary>Gets the id of a chest from its networked data uid, when the view is valid.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="id">The id, when the call returns true.</param>
    /// <returns>False when the view is invalid or has no networked data.</returns>
    public bool TryGetId(Container chest, out ContainerId id)
    {
        ZDO? zdo = Data(chest);
        if (zdo == null)
        {
            id = default;
            return false;
        }

        ZDOID uid = zdo.m_uid;
        id = new ContainerId(uid.UserID, uid.ID);
        return true;
    }

    /// <summary>Reads the raw text stored under the chest category key.</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>The stored text, the empty string for no key, or null for an invalid view.</returns>
    public string? ReadRaw(Container chest)
    {
        ZDO? zdo = Data(chest);
        return zdo == null ? null : zdo.GetString(PersistenceKeys.ChestCategory, string.Empty);
    }

    /// <summary>Writes the raw text under the chest category key (the one writer of the key).</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="value">The text to store; the empty string clears the value.</param>
    /// <returns>True when the value was written; false for an invalid view.</returns>
    public bool Write(Container chest, string value)
    {
        ZDO? zdo = Data(chest);
        if (zdo == null)
        {
            return false;
        }

        zdo.Set(PersistenceKeys.ChestCategory, value);
        return true;
    }

    private static ZDO? Data(Container chest)
    {
        if (chest == null || chest.m_nview == null || !chest.m_nview.IsValid())
        {
            return null;
        }

        return chest.m_nview.GetZDO();
    }
}
