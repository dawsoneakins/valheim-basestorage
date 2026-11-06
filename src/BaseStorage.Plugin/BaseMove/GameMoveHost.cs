namespace BaseStorage.Plugin.BaseMove;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Compendium;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Categories;
using BaseStorage.Plugin.CategoryButton;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Compendium;
using UnityEngine;

/// <summary>The host's game side of a move: finds and reads both bases, plans, holds, and writes in one frame. Main thread only.</summary>
internal sealed class GameMoveHost : IMoveHostPort
{
    private readonly Action<long, MoveMessage> send;
    private readonly MoveHolds holds;
    private readonly BaseStorageSettings settings;
    private readonly ILogSink log;
    private readonly List<ContainerId> empty = new List<ContainerId>();
    private List<FarChestRecord> sources = new List<FarChestRecord>();
    private List<FarChestRecord> destinations = new List<FarChestRecord>();
    private List<MoveWard> wards = new List<MoveWard>();
    private Dictionary<ContainerId, FarChest> chests = new Dictionary<ContainerId, FarChest>();
    private MovePlan? plan;
    private MoveSummary? summary;
    private int unreadable;
    private long requesterPeer;
    private long requesterPlayer;

    internal GameMoveHost(Action<long, MoveMessage> send, MoveHolds holds, BaseStorageSettings settings, ILogSink log)
    {
        this.send = send;
        this.holds = holds;
        this.settings = settings;
        this.log = log;
    }

    /// <summary>Gets the peer of the running move.</summary>
    internal long RequesterPeer => requesterPeer;

    /// <summary>Gets or sets the move's coordinator, which the plan's result goes back to.</summary>
    internal MoveHostCoordinator? Coordinator { get; set; }

    /// <summary>Gets or sets the time source, in seconds.</summary>
    internal Func<double>? Now { get; set; }

    /// <summary>Gets or sets the action that runs a world scan and calls back when it is complete.</summary>
    internal Action<Action<FarWorld?>>? Scan { get; set; }

    /// <inheritdoc/>
    public void BeginPlan(long peer, MoveRequest request)
    {
        requesterPeer = peer;
        requesterPlayer = request.RequesterPlayerId;
        Scan!(world => FinishPlan(world, request));
    }

    /// <inheritdoc/>
    public void SendPreview(long peer, int requestId, MoveRefusal refusal, string? playerName, MoveSummary? plan) =>
        send(peer, new MoveMessage { Kind = MoveMessageKind.Preview, RequestId = requestId, Refusal = refusal, PlayerName = playerName, Summary = plan });

    /// <inheritdoc/>
    public void BeginHostHolds(IReadOnlyList<MoveChestRevision> heldByRequester)
    {
        var skip = new HashSet<ContainerId>();
        for (int i = 0; i < heldByRequester.Count; i++)
        {
            skip.Add(heldByRequester[i].Id);
        }

        IReadOnlyList<ContainerId> chestsToHold = plan == null ? empty : plan.ChangedChests();
        if (!holds.Begin(chestsToHold, skip))
        {
            Coordinator!.OnHostHoldsFinished(false, Now!());
            return;
        }

        WaitingForHolds = true;
    }

    /// <summary>Gets or sets a value indicating whether the plugin should poll this host's holds.</summary>
    internal bool WaitingForHolds { get; set; }

    /// <inheritdoc/>
    public void EndMove()
    {
        WaitingForHolds = false;
        holds.Release();
        chests = new Dictionary<ContainerId, FarChest>();
        sources = new List<FarChestRecord>();
        destinations = new List<FarChestRecord>();
        plan = null;
        summary = null;
        requesterPeer = 0L;
    }

    /// <inheritdoc/>
    public uint? LocalRevision(ContainerId id) => MoveHolds.RevisionOf(id);

    /// <inheritdoc/>
    public ulong? Replan()
    {
        try
        {
            return BuildPlan() ? plan!.Fingerprint : (ulong?)null;
        }
        catch (Exception exception)
        {
            log.Log(LogSeverity.Error, "Base move: planning failed, nothing was written: " + exception);
            return null;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<MoveChestRevision>? Apply()
    {
        try
        {
            return ApplyPlan();
        }
        catch (Exception exception)
        {
            // Everything that can throw runs on temporary inventories before the first world write.
            log.Log(LogSeverity.Error, "Base move: nothing was written: " + exception);
            return null;
        }
    }

    /// <inheritdoc/>
    public void SendDone(long peer, int requestId, IReadOnlyList<MoveChestRevision> revisions, MoveSummary done) =>
        send(peer, new MoveMessage { Kind = MoveMessageKind.Done, RequestId = requestId, Revisions = revisions, Summary = done });

    /// <inheritdoc/>
    public void SendCancelled(long peer, int requestId, MoveRefusal refusal, string? playerName) =>
        send(peer, new MoveMessage { Kind = MoveMessageKind.Cancelled, RequestId = requestId, Refusal = refusal, PlayerName = playerName });

    /// <summary>Lists every base in the world.</summary>
    /// <param name="world">The scanned world.</param>
    /// <param name="maxStations">The largest base.</param>
    /// <returns>The bases.</returns>
    internal static List<BaseEntry> Directory(FarWorld world, int maxStations)
    {
        var stations = new List<KeyValuePair<StationSnapshot, string>>(world.Stations.Count);
        foreach (FarStation station in world.Stations)
        {
            stations.Add(new KeyValuePair<StationSnapshot, string>(station.Snapshot, station.BaseName));
        }

        var chests = new List<DirectoryChest>(world.Chests.Count);
        foreach (FarChestRecord record in world.Chests)
        {
            // Only player-built chests: a grave or a dungeon chest near a workbench isn't part of a base.
            if (record.Zdo.GetLong(ZDOVars.s_creator, 0L) != 0L)
            {
                Vector3 point = record.Zdo.GetPosition();
                var placement = new ContainerSnapshot(record.Id, new WorldPosition(point.x, point.y, point.z), 1, 1, Array.Empty<ItemStackSnapshot>(), ContainerFlags.None);
                chests.Add(new DirectoryChest(placement, FarChest.UsedSlots(record), record.Prefab.m_width * record.Prefab.m_height));
            }
        }

        List<BaseEntry> entries = BaseDirectory.Find(stations, chests, maxStations);
        foreach (BaseEntry entry in entries)
        {
            if (WorldGenerator.instance != null
                && GameCompendiumSource.TryMapBiome(WorldGenerator.instance.GetBiome(new Vector3(entry.X, 0f, entry.Z)), out CompendiumBiome biome))
            {
                entry.Biome = biome;
            }
        }

        return entries;
    }

    /// <summary>Renames the base around a station, on all of its stations, unless a ward over any of them denies the player.</summary>
    /// <param name="world">The scanned world.</param>
    /// <param name="station">A station in the base.</param>
    /// <param name="name">The cleaned name; empty clears it.</param>
    /// <param name="player">The requesting player's id.</param>
    /// <param name="maxStations">The largest base.</param>
    /// <returns>False when a ward denied it or the base is gone.</returns>
    internal static bool Rename(FarWorld world, ContainerId station, string name, long player, int maxStations)
    {
        var start = new StationId(station.UserId, station.Id);
        if (!world.Stations.Exists(s => s.Snapshot.Id == start))
        {
            return false;
        }

        var members = new HashSet<StationId>();
        foreach (StationSnapshot member in StationGraph.FindBase(Snapshots(world), start, maxStations).Stations)
        {
            if (MoveWard.Denies(world.Wards, member.Position, player))
            {
                return false;
            }

            members.Add(member.Id);
        }

        foreach (FarStation candidate in world.Stations)
        {
            if (members.Contains(candidate.Snapshot.Id))
            {
                candidate.Zdo.Set(PersistenceKeys.BaseName, name);
            }
        }

        return true;
    }

    private static List<StationSnapshot> Snapshots(FarWorld world)
    {
        var snapshots = new List<StationSnapshot>(world.Stations.Count);
        foreach (FarStation station in world.Stations)
        {
            snapshots.Add(station.Snapshot);
        }

        return snapshots;
    }

    private void FinishPlan(FarWorld? world, MoveRequest request)
    {
        double now = Now!();
        try
        {
            if (world == null)
            {
                Coordinator!.OnPlanReady(MoveRefusal.Failed, null, 0UL, null, now);
                return;
            }

            MoveRefusal refusal = FindBases(world, request, out string? playerName);
            if (refusal != MoveRefusal.None || !BuildPlan())
            {
                Coordinator!.OnPlanReady(refusal == MoveRefusal.None ? MoveRefusal.Failed : refusal, playerName, 0UL, null, now);
                return;
            }

            Coordinator!.OnPlanReady(MoveRefusal.None, null, plan!.Fingerprint, summary, now);
        }
        catch (Exception exception)
        {
            log.Log(LogSeverity.Error, "Base move: planning failed, nothing was written: " + exception);
            Coordinator!.OnPlanReady(MoveRefusal.Failed, null, 0UL, null, now);
        }
    }

    private MoveRefusal FindBases(FarWorld world, MoveRequest request, out string? playerName)
    {
        playerName = null;
        List<StationSnapshot> snapshots = Snapshots(world);
        var start = new StationId(request.SourceStation.UserId, request.SourceStation.Id);
        if (!world.Stations.Exists(s => s.Snapshot.Id == start))
        {
            return MoveRefusal.NoSuchBase;
        }

        IReadOnlyList<StationSnapshot> sourceStations = StationGraph.FindBase(snapshots, start, settings.MaxBaseStations).Stations;
        var destinationStart = new StationId(request.DestinationStation.UserId, request.DestinationStation.Id);
        FarStation? named = world.Stations.Find(s => s.Snapshot.Id == destinationStart);
        if (named == null)
        {
            return MoveRefusal.NoSuchBase;
        }

        foreach (StationSnapshot station in sourceStations)
        {
            if (station.Id == named.Snapshot.Id)
            {
                return MoveRefusal.BasesTooNear;
            }
        }

        IReadOnlyList<StationSnapshot> destinationStations = StationGraph.FindBase(snapshots, named.Snapshot.Id, settings.MaxBaseStations).Stations;
        var placeholders = new List<ContainerSnapshot>(world.Chests.Count);
        var records = new Dictionary<ContainerId, FarChestRecord>(world.Chests.Count);
        foreach (FarChestRecord record in world.Chests)
        {
            Vector3 point = record.Zdo.GetPosition();
            placeholders.Add(new ContainerSnapshot(record.Id, new WorldPosition(point.x, point.y, point.z), 1, 1, Array.Empty<ItemStackSnapshot>(), ContainerFlags.None));
            records[record.Id] = record;
        }

        sources = Members(sourceStations, placeholders, records);
        destinations = Members(destinationStations, placeholders, records);
        wards = world.Wards;
        var ids = new HashSet<ContainerId>();
        foreach (FarChestRecord record in sources)
        {
            ids.Add(record.Id);
        }

        foreach (FarChestRecord record in destinations)
        {
            if (ids.Contains(record.Id))
            {
                return MoveRefusal.BasesTooNear;
            }
        }

        playerName = ThirdPlayerAtBases();
        return playerName == null ? MoveRefusal.None : MoveRefusal.PlayerAtBase;
    }

    private static List<FarChestRecord> Members(
        IReadOnlyList<StationSnapshot> stations,
        List<ContainerSnapshot> placeholders,
        Dictionary<ContainerId, FarChestRecord> records)
    {
        var members = new List<FarChestRecord>();
        foreach (ContainerSnapshot member in BaseMembership.FindMembers(stations, placeholders))
        {
            members.Add(records[member.Id]);
        }

        return members;
    }

    /// <summary>A third player's game owns the chests near them, and the host cannot hold those still.</summary>
    private string? ThirdPlayerAtBases()
    {
        long self = ZNet.GetUID();
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
        {
            if (peer.m_uid == requesterPeer || peer.m_uid == self)
            {
                continue;
            }

            Vector3 at = peer.GetRefPos();
            if (Near(sources, at) || Near(destinations, at))
            {
                return peer.m_playerName;
            }
        }

        return null;
    }

    private static bool Near(List<FarChestRecord> records, Vector3 at)
    {
        foreach (FarChestRecord record in records)
        {
            if (ZNetScene.InActiveArea(record.Zdo.GetPosition(), at))
            {
                return true;
            }
        }

        return false;
    }

    private bool BuildPlan()
    {
        Dictionary<string, ItemPortalTraits> traits = new Dictionary<string, ItemPortalTraits>(StringComparer.Ordinal);
        Dictionary<string, ItemKind> kinds = new Dictionary<string, ItemKind>(StringComparer.Ordinal);
        ObjectDB db = ObjectDB.instance;
        if (db == null || db.m_items == null || ZoneSystem.instance == null)
        {
            return false;
        }

        foreach (GameObject prefab in db.m_items)
        {
            ItemDrop drop = prefab == null ? null! : prefab.GetComponent<ItemDrop>();
            if (drop != null && drop.m_itemData != null && drop.m_itemData.m_shared != null)
            {
                traits[prefab!.name] = new ItemPortalTraits(drop.m_itemData.m_shared.m_toolTier, drop.m_itemData.m_shared.m_teleportable);
            }
        }

        List<CategoryItem> items = ObjectDbCategoryItems.Collect(db);
        for (int i = 0; i < items.Count; i++)
        {
            kinds[items[i].PrefabName] = items[i].Kind;
        }

        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        var read = new Dictionary<ContainerId, FarChest>();
        unreadable = 0;
        List<ContainerSnapshot> sourceSnapshots = ReadAll(sources, read, assignments);
        List<ContainerSnapshot> destinationSnapshots = ReadAll(destinations, read, assignments);
        chests = read;
        plan = MovePlanner.Plan(
            sourceSnapshots,
            destinationSnapshots,
            assignments,
            new Dictionary<ContainerId, string>(),
            PluginCategoryCatalog.Current,
            kinds,
            traits,
            ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll),
            new EligibilityOptions(settings.ExcludeVehicles, settings.KeepChestsCraftable),
            settings.AutoAssignThresholdPct);
        summary = MoveSummary.Of(plan, unreadable);
        return true;
    }

    private List<ContainerSnapshot> ReadAll(List<FarChestRecord> records, Dictionary<ContainerId, FarChest> read, Dictionary<ContainerId, CategoryAssignment> assignments)
    {
        var snapshots = new List<ContainerSnapshot>(records.Count);
        foreach (FarChestRecord record in records)
        {
            FarChest? chest = FarChest.Read(record, FlagsOf(record));
            if (chest == null)
            {
                unreadable++;
                log.Log(LogSeverity.Warning, "Base move: skipped a chest that could not be read without losing items: " + record.Id.UserId + ":" + record.Id.Id);
                continue;
            }

            read[record.Id] = chest;
            snapshots.Add(chest.Snapshot);
            CategoryAssignment? assignment = ChestCategoryReader.Read(GameChestCategoryStorage.ReadRecord(record.Zdo));
            if (assignment != null)
            {
                assignments[record.Id] = assignment;
            }
        }

        return snapshots;
    }

    private ContainerFlags FlagsOf(FarChestRecord record)
    {
        ZDO zdo = record.Zdo;
        ContainerFlags flags = ContainerFlags.None;
        long creator = zdo.GetLong(ZDOVars.s_creator, 0L);
        if (creator != 0L)
        {
            flags |= ContainerFlags.PlayerBuilt;
        }

        Vector3 point = zdo.GetPosition();
        Container prefab = record.Prefab;
        bool allowed = prefab.m_privacy == Container.PrivacySetting.Public
            || (prefab.m_privacy == Container.PrivacySetting.Private && creator == requesterPlayer);
        if (!allowed || (prefab.m_checkGuardStone && MoveWard.Denies(wards, new WorldPosition(point.x, point.y, point.z), requesterPlayer)))
        {
            flags |= ContainerFlags.AccessDenied;
        }

        // The requester and the host hold chests on purpose for this move; holding must not change the plan.
        if (zdo.GetInt(ZDOVars.s_inUse, 0) == 1)
        {
            long owner = zdo.GetOwner();
            if (owner != requesterPeer && owner != ZNet.GetUID() && owner != 0L && ZNet.instance.GetPeer(owner) != null)
            {
                flags |= ContainerFlags.InUseByOther;
            }
        }

        return flags;
    }

    private IReadOnlyList<MoveChestRevision>? ApplyPlan()
    {
        if (plan == null)
        {
            return null;
        }

        var before = new List<ContainerSnapshot>();
        foreach (FarChest chest in chests.Values)
        {
            before.Add(chest.Snapshot);
        }

        foreach (PlannedMove move in plan.Moves)
        {
            FarChest source = chests[move.Source];
            FarChest destination = chests[move.Destination];
            ItemDrop.ItemData item = source.Items[move.SourceStackIndex];
            if (item.m_stack < move.Count)
            {
                return null;
            }

            ItemDrop.ItemData copy = item.Clone();
            copy.m_stack = move.Count;
            if (!destination.Inventory.AddItem(copy) || !source.Inventory.RemoveItem(item, move.Count))
            {
                return null;
            }
        }

        var after = new List<ContainerSnapshot>();
        foreach (FarChest chest in chests.Values)
        {
            ContainerSnapshot? snapshot = chest.SnapshotNow();
            if (snapshot == null)
            {
                return null;
            }

            after.Add(snapshot);
        }

        if (!MoveConservation.Holds(before, after))
        {
            log.Log(LogSeverity.Error, "Base move: item totals did not match, nothing was written.");
            return null;
        }

        IReadOnlyList<ContainerId> changed = plan.ChangedChests();
        var encoded = new List<byte[]>(changed.Count);
        for (int i = 0; i < changed.Count; i++)
        {
            encoded.Add(chests[changed[i]].Encode());
        }

        // Past this line nothing can fail part way: these are plain sets on records in this one frame.
        var revisions = new List<MoveChestRevision>(changed.Count);
        for (int i = 0; i < changed.Count; i++)
        {
            ZDO zdo = chests[changed[i]].Record.Zdo;
            zdo.Set(ZDOVars.s_items, encoded[i]);
        }

        foreach (ChestAutoAssignment rule in plan.RulesToSave)
        {
            if (rule.Assignment != null && chests.TryGetValue(rule.Id, out FarChest? chest))
            {
                GameChestCategoryStorage.WriteRecord(chest!.Record.Zdo, ChestCategoryReader.StoredValue(rule.Assignment));
            }
        }

        for (int i = 0; i < changed.Count; i++)
        {
            revisions.Add(new MoveChestRevision(changed[i], chests[changed[i]].Record.Zdo.DataRevision));
        }

        log.Log(LogSeverity.Debug, "Base move: wrote " + changed.Count + " chests for peer " + requesterPeer);
        return revisions;
    }
}
