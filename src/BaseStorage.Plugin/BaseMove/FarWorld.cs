namespace BaseStorage.Plugin.BaseMove;

using System;
using System.Collections.Generic;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.ChestNames;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using UnityEngine;

/// <summary>
/// What the host's world records say about every station, chest and ward, loaded or not, read across frames from the same prefab data the
/// game uses. Only a host holds the far records. A station's range here leaves out extensions, because those need the loaded object, so a far
/// base can only come out smaller than it looks when you stand in it. Main thread only.
/// </summary>
internal sealed class FarWorld
{
    private const int ScanCallsPerFrame = 24;

    private static readonly Dictionary<string, PrefabInfo> Prefabs = new Dictionary<string, PrefabInfo>(StringComparer.Ordinal);
    private static ZNetScene? builtFor;

    private readonly List<string> names = new List<string>();
    private readonly List<ZDO> scratch = new List<ZDO>();
    private int nameIndex;
    private int sectorIndex;

    private FarWorld()
    {
    }

    /// <summary>Gets the stations, sorted by id.</summary>
    internal List<FarStation> Stations { get; } = new List<FarStation>();

    /// <summary>Gets the chests, sorted by id.</summary>
    internal List<FarChestRecord> Chests { get; } = new List<FarChestRecord>();

    /// <summary>Gets the wards.</summary>
    internal List<MoveWard> Wards { get; } = new List<MoveWard>();

    /// <summary>Starts a scan.</summary>
    /// <returns>The scan, or null when the world is not ready.</returns>
    internal static FarWorld? Begin()
    {
        if (ZNetScene.instance == null || ZDOMan.instance == null)
        {
            return null;
        }

        BuildPrefabs();
        var world = new FarWorld();
        world.names.AddRange(Prefabs.Keys);
        return world;
    }

    /// <summary>Scans some of the world.</summary>
    /// <returns>True when the scan is complete.</returns>
    internal bool Step()
    {
        for (int calls = 0; calls < ScanCallsPerFrame && nameIndex < names.Count; calls++)
        {
            if (ZDOMan.instance.GetAllZDOsWithPrefabIterative(names[nameIndex], scratch, ref sectorIndex))
            {
                Collect(Prefabs[names[nameIndex]]);
                scratch.Clear();
                sectorIndex = 0;
                nameIndex++;
            }
        }

        if (nameIndex < names.Count)
        {
            return false;
        }

        Stations.Sort(static (a, b) => a.Snapshot.Id.CompareTo(b.Snapshot.Id));
        Chests.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        return true;
    }

    private static void BuildPrefabs()
    {
        ZNetScene scene = ZNetScene.instance;
        if (ReferenceEquals(builtFor, scene))
        {
            return;
        }

        builtFor = scene;
        Prefabs.Clear();
        foreach (GameObject prefab in scene.m_prefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            CraftingStation station = prefab.GetComponent<CraftingStation>();
            PrivateArea ward = prefab.GetComponent<PrivateArea>();
            Container chest = prefab.GetComponent<Container>();

            // Vehicles keep their Container on a child, so a root Container is never a cart or a ship.
            if (chest != null && (prefab.GetComponent<TombStone>() != null || prefab.GetComponent<Vagon>() != null || prefab.GetComponent<Ship>() != null))
            {
                chest = null!;
            }

            if (station != null || ward != null || chest != null)
            {
                Prefabs[prefab.name] = new PrefabInfo(station, ward, chest);
            }
        }
    }

    private static List<long> PermittedPlayers(ZDO zdo)
    {
        var permitted = new List<long>();
        int count = zdo.GetInt(ZDOVars.s_permitted, 0);
        for (int i = 0; i < count; i++)
        {
            permitted.Add(zdo.GetLong("pu_id" + i, 0L));
        }

        return permitted;
    }

    private static WorldPosition PositionOf(ZDO zdo)
    {
        Vector3 point = zdo.GetPosition();
        return new WorldPosition(point.x, point.y, point.z);
    }

    private void Collect(PrefabInfo info)
    {
        for (int i = 0; i < scratch.Count; i++)
        {
            ZDO zdo = scratch[i];
            ZDOID uid = zdo.m_uid;
            if (info.Station != null)
            {
                var id = new StationId(uid.UserID, uid.ID);
                if (!id.IsNone)
                {
                    Stations.Add(new FarStation(new StationSnapshot(id, PositionOf(zdo), info.Station.m_rangeBuild), zdo));
                }
            }

            if (info.Ward != null)
            {
                Wards.Add(new MoveWard(PositionOf(zdo), info.Ward.m_radius, zdo.GetBool(ZDOVars.s_enabled, false), zdo.GetLong(ZDOVars.s_creator, 0L), PermittedPlayers(zdo)));
            }

            if (info.Chest != null)
            {
                var id = new ContainerId(uid.UserID, uid.ID);
                if (!id.IsNone)
                {
                    Chests.Add(new FarChestRecord(id, zdo, info.Chest));
                }
            }
        }
    }

    private sealed class PrefabInfo
    {
        internal PrefabInfo(CraftingStation? station, PrivateArea? ward, Container? chest)
        {
            Station = station;
            Ward = ward;
            Chest = chest;
        }

        internal CraftingStation? Station { get; }

        internal PrivateArea? Ward { get; }

        internal Container? Chest { get; }
    }
}

/// <summary>A station from world records.</summary>
internal sealed class FarStation
{
    internal FarStation(StationSnapshot snapshot, ZDO zdo)
    {
        Snapshot = snapshot;
        Zdo = zdo;
    }

    internal StationSnapshot Snapshot { get; }

    internal ZDO Zdo { get; }

    /// <summary>Gets the base name on this station, cleaned, or empty.</summary>
    internal string BaseName => ChestNameRules.Clean(Zdo.GetString(PersistenceKeys.BaseName, string.Empty));
}

/// <summary>A chest from world records, with the prefab that says how big it is and who may use it.</summary>
internal sealed class FarChestRecord
{
    internal FarChestRecord(ContainerId id, ZDO zdo, Container prefab)
    {
        Id = id;
        Zdo = zdo;
        Prefab = prefab;
    }

    internal ContainerId Id { get; }

    internal ZDO Zdo { get; }

    internal Container Prefab { get; }
}
