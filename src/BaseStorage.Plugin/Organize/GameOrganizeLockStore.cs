namespace BaseStorage.Plugin.Organize;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.World;

/// <summary>
/// The organize lock store over a crafting station's networked data (contract section 5, rule RN11; ADR-GWIN-109). It is the only
/// Plugin code that writes the organize lock key. Only the owner's writes replicate, so a write or clear first claims the station's
/// ownership. A clear writes the empty string, which every reader treats as no value; no key is ever removed. The station is found by
/// id among the loaded stations on every call. Game-bound, so it is not linked into the tests. Main thread only; Unity null checks use <c>==</c> (W4).
/// </summary>
internal sealed class GameOrganizeLockStore : IOrganizeLockStore
{
    private readonly PluginWorld world;
    private readonly List<CraftingStation> scratch = new List<CraftingStation>();

    /// <summary>Creates the store over the plugin world's station registry.</summary>
    /// <param name="world">The plugin world.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="world"/> is null.</exception>
    internal GameOrganizeLockStore(PluginWorld world)
    {
        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        this.world = world;
    }

    /// <summary>Reads the raw text stored under the organize lock key on a station.</summary>
    /// <param name="station">The station.</param>
    /// <returns>The stored text, or null for no value, an unknown station or an invalid view.</returns>
    public string? Read(StationId station)
    {
        ZDO? zdo = Data(station);
        if (zdo == null)
        {
            return null;
        }

        string value = zdo.GetString(PersistenceKeys.OrganizeLock, string.Empty);
        return value.Length == 0 ? null : value;
    }

    /// <summary>Stores text under the organize lock key on a station, claiming its ownership first.</summary>
    /// <param name="station">The station.</param>
    /// <param name="value">The text to store.</param>
    public void Write(StationId station, string value)
    {
        Put(station, value ?? string.Empty);
    }

    /// <summary>Clears the organize lock key on a station by writing the empty string, claiming its ownership first.</summary>
    /// <param name="station">The station.</param>
    public void Clear(StationId station)
    {
        Put(station, string.Empty);
    }

    private void Put(StationId station, string value)
    {
        CraftingStation? found = FindStation(station);
        if (found == null)
        {
            return;
        }

        ZNetView view = found.m_nview;
        ZDO zdo = view.GetZDO();
        if (zdo == null)
        {
            return;
        }

        if (!view.IsOwner())
        {
            view.ClaimOwnership();
        }

        zdo.Set(PersistenceKeys.OrganizeLock, value);
    }

    private ZDO? Data(StationId station)
    {
        CraftingStation? found = FindStation(station);
        return found == null ? null : found.m_nview.GetZDO();
    }

    private CraftingStation? FindStation(StationId station)
    {
        world.Stations.CopyItemsTo(scratch);
        CraftingStation? found = null;
        for (int i = 0; i < scratch.Count; i++)
        {
            CraftingStation candidate = scratch[i];
            if (candidate == null || candidate.m_nview == null || !candidate.m_nview.IsValid())
            {
                continue;
            }

            ZDO zdo = candidate.m_nview.GetZDO();
            if (zdo == null)
            {
                continue;
            }

            ZDOID uid = zdo.m_uid;
            if (uid.UserID == station.UserId && uid.ID == station.Id)
            {
                found = candidate;
                break;
            }
        }

        scratch.Clear();
        return found;
    }
}
