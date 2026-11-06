namespace BaseStorage.Plugin.BaseMove;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.ChestNames;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Reservations;
using BaseStorage.Plugin.Ui;
using BaseStorage.Plugin.World;
using UnityEngine;

/// <summary>Wires a move's two sides to the game: one string RPC, a per-frame tick, and the menu's entry points. Main thread only.</summary>
internal static class PluginBaseMove
{
    private const string Rpc = MoveMessage.RpcName;
    private const string TickKey = "BaseStorage.Plugin.BaseMove.PluginBaseMove.Tick";
    private const string RpcKey = "BaseStorage.Plugin.BaseMove.PluginBaseMove.Rpc";
    private const double RequesterTimeoutSeconds = 20d;
    private const double PreviewSeconds = 120d;
    private const double HoldSeconds = 20d;

    private static readonly List<Action<FarWorld?>> ScanWaiters = new List<Action<FarWorld?>>();
    private static readonly List<CraftingStation> StationScratch = new List<CraftingStation>();

    private static ZRoutedRpc? registeredOn;
    private static IClock? clock;
    private static BaseStorageSettings? settings;
    private static PluginWorld? world;
    private static ILogSink? log;
    private static MoveHolds? holds;
    private static GameMoveRequester? requesterPort;
    private static MoveRequesterSession? requester;
    private static GameMoveHost? hostPort;
    private static MoveHostCoordinator? host;
    private static FarWorld? scan;
    private static Action<IReadOnlyList<BaseEntry>>? onBases;
    private static int nextRequestId;
    private static bool wasConnected;

    /// <summary>Boots the feature.</summary>
    /// <param name="pluginWorld">The loaded world registries.</param>
    /// <param name="reservations">The reservation driver.</param>
    /// <param name="gameClock">The clock.</param>
    /// <param name="baseSettings">The settings.</param>
    /// <param name="sink">The log.</param>
    internal static void Initialize(PluginWorld pluginWorld, ReservationDriver<Container> reservations, IClock gameClock, BaseStorageSettings baseSettings, ILogSink sink)
    {
        world = pluginWorld;
        clock = gameClock;
        settings = baseSettings;
        log = sink;
        holds = new MoveHolds(pluginWorld, reservations);
        requesterPort = new GameMoveRequester(SendToHost, Show, holds, sink) { Now = Now };
        requester = new MoveRequesterSession(requesterPort, RequesterTimeoutSeconds);
        requesterPort.Session = requester;
        hostPort = new GameMoveHost(Send, holds, baseSettings, sink) { Now = Now, Scan = RunScan };
        host = new MoveHostCoordinator(hostPort, PreviewSeconds, HoldSeconds);
        hostPort.Coordinator = host;
    }

    /// <summary>Advances the feature once per frame.</summary>
    internal static void Tick() => PatchSafety.Run(TickKey, static () => Step());

    /// <summary>Asks the host for every base and hands the list to a callback.</summary>
    /// <param name="callback">Receives the bases.</param>
    internal static void AskBases(Action<IReadOnlyList<BaseEntry>> callback)
    {
        onBases = callback;
        SendToHost(new MoveMessage { Kind = MoveMessageKind.ListAsk });
    }

    /// <summary>Starts a move between two bases, wherever the player stands.</summary>
    /// <param name="source">The source base.</param>
    /// <param name="destination">The destination base.</param>
    internal static void StartMove(BaseEntry source, BaseEntry destination)
    {
        Player player = Player.m_localPlayer;
        if (player == null || requester == null || requesterPort == null)
        {
            return;
        }

        requesterPort.Prepare(source.Station, source.DisplayName, destination.Station, player.GetPlayerID());
        if (!requester.Start(++nextRequestId, destination.DisplayName, Now()))
        {
            Show(StringTable.English.Get(StringKeys.MoveRefusedBusy));
        }
    }

    /// <summary>Asks the player for a base's new name through the game's text input.</summary>
    /// <param name="station">A station in the base.</param>
    /// <param name="current">The base's saved name.</param>
    internal static void AskName(ContainerId station, string current)
    {
        TextInput? input = TextInput.instance;
        if (input != null)
        {
            input.RequestText(new NameReceiver(station, current), StringTable.English.Get(StringKeys.MoveNameTopic), ChestNameRules.MaxLength);
        }
    }

    /// <summary>Gets a station of the base the player stands in.</summary>
    /// <returns>The station, or none outside a base.</returns>
    internal static ContainerId StationHereId() => StationHere();

    private sealed class NameReceiver : TextReceiver
    {
        private readonly ContainerId station;
        private readonly string current;

        internal NameReceiver(ContainerId station, string current)
        {
            this.station = station;
            this.current = current;
        }

        public string GetText() => current;

        public void SetText(string text)
        {
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                SendToHost(new MoveMessage { Kind = MoveMessageKind.Rename, SourceStation = station, DestinationName = ChestNameRules.Clean(text), RequesterPlayerId = player.GetPlayerID() });
            }
        }
    }

    private static double Now() => clock == null ? 0d : clock.Elapsed.TotalSeconds;

    private static void Show(string text)
    {
        Notices.Show(NoticePlacement.TopLeft, StringKeys.MoveLine, text);
    }

    /// <summary>Keeps the bases window current after a move finishes.</summary>
    internal static void RefreshBasesWindow()
    {
        if (BasesWindow.IsOpen)
        {
            AskBases(BasesWindow.ShowList);
        }
    }

    private static void Send(long peer, MoveMessage message)
    {
        ZRoutedRpc rpc = ZRoutedRpc.instance;
        if (rpc != null)
        {
            rpc.InvokeRoutedRPC(peer, Rpc, message.Encode());
        }
    }

    private static void SendToHost(MoveMessage message)
    {
        ZRoutedRpc rpc = ZRoutedRpc.instance;
        if (rpc != null)
        {
            rpc.InvokeRoutedRPC(rpc.GetServerPeerID(), Rpc, message.Encode());
        }
    }

    private static void Step()
    {
        if (requester == null || host == null || hostPort == null || requesterPort == null || holds == null)
        {
            return;
        }

        ZRoutedRpc rpc = ZRoutedRpc.instance;
        ZNet net = ZNet.instance;
        bool connected = rpc != null && net != null && ZDOMan.instance != null;
        if (!connected)
        {
            if (wasConnected)
            {
                wasConnected = false;
                requester.OnDisconnected();
                host.OnPeerLeft(hostPort.RequesterPeer);
                ScanWaiters.Clear();
                scan = null;
            }

            return;
        }

        wasConnected = true;
        if (!ReferenceEquals(rpc, registeredOn))
        {
            registeredOn = rpc;
            rpc!.Register<string>(Rpc, OnMessage);
        }

        double now = Now();
        if (requesterPort.WaitingForHolds)
        {
            PollRequesterHolds(now);
        }

        requester.Tick(now);
        if (!net!.IsServer())
        {
            return;
        }

        if (scan != null && scan.Step())
        {
            FinishScan(scan);
        }

        long peer = hostPort.RequesterPeer;
        if (peer != 0L && peer != ZNet.GetUID() && net.GetPeer(peer) == null)
        {
            host.OnPeerLeft(peer);
        }

        if (hostPort.WaitingForHolds)
        {
            PollHostHolds(now);
        }

        host.Tick(now);
    }

    private static void PollRequesterHolds(double now)
    {
        bool? done = holds!.Poll();
        if (done.HasValue)
        {
            requesterPort!.WaitingForHolds = false;
            requester!.OnHoldsFinished(done.Value ? holds.Revisions() : null, now);
        }
    }

    private static void PollHostHolds(double now)
    {
        bool? done = holds!.Poll();
        if (done.HasValue)
        {
            hostPort!.WaitingForHolds = false;
            host!.OnHostHoldsFinished(done.Value, now);
        }
    }

    private static void RunScan(Action<FarWorld?> callback)
    {
        ScanWaiters.Add(callback);
        if (scan == null)
        {
            scan = FarWorld.Begin();
            if (scan == null)
            {
                FinishScan(null);
            }
        }
    }

    private static void FinishScan(FarWorld? finished)
    {
        scan = null;
        var waiters = new List<Action<FarWorld?>>(ScanWaiters);
        ScanWaiters.Clear();
        foreach (Action<FarWorld?> waiter in waiters)
        {
            waiter(finished);
        }
    }

    private static void OnMessage(long sender, string payload) => PatchSafety.Run(RpcKey, () => Handle(sender, payload));

    private static void Handle(long sender, string payload)
    {
        MoveMessage? message = MoveMessage.Decode(payload);
        ZRoutedRpc rpc = ZRoutedRpc.instance;
        if (message == null || rpc == null || ZNet.instance == null)
        {
            log?.Log(LogSeverity.Warning, "Base move: ignored a malformed message from peer " + sender);
            return;
        }

        double now = Now();
        if (ZNet.instance.IsServer())
        {
            HandleAsHost(sender, message, now);
        }

        if (sender == rpc.GetServerPeerID())
        {
            HandleAsRequester(message, now);
        }
    }

    private static void HandleAsHost(long sender, MoveMessage message, double now)
    {
        switch (message.Kind)
        {
            case MoveMessageKind.ListAsk:
                RunScan(found =>
                {
                    List<BaseEntry> bases = found == null ? new List<BaseEntry>() : GameMoveHost.Directory(found, settings!.MaxBaseStations);
                    Send(sender, new MoveMessage { Kind = MoveMessageKind.ListReply, Bases = bases });
                });
                break;
            case MoveMessageKind.Rename:
                string name = ChestNameRules.Clean(message.DestinationName);
                RunScan(found =>
                {
                    if (found == null || !GameMoveHost.Rename(found, message.SourceStation, name, message.RequesterPlayerId, settings!.MaxBaseStations))
                    {
                        Send(sender, new MoveMessage { Kind = MoveMessageKind.RenameRefused });
                        return;
                    }

                    Send(sender, new MoveMessage { Kind = MoveMessageKind.Renamed, DestinationName = name, Bases = GameMoveHost.Directory(found, settings!.MaxBaseStations) });
                });
                break;
            case MoveMessageKind.Request:
                host!.OnRequest(sender, new MoveRequest(message.RequestId, message.SourceStation, message.RequesterPlayerId, message.DestinationName ?? string.Empty, message.DestinationStation), now);
                break;
            case MoveMessageKind.Confirm:
                host!.OnConfirm(sender, message.RequestId, message.Revisions, now);
                break;
            case MoveMessageKind.Cancel:
                host!.OnCancel(sender, message.RequestId);
                break;
        }
    }

    private static void HandleAsRequester(MoveMessage message, double now)
    {
        switch (message.Kind)
        {
            case MoveMessageKind.ListReply:
                Action<IReadOnlyList<BaseEntry>>? callback = onBases;
                onBases = null;
                callback?.Invoke(message.Bases);
                break;
            case MoveMessageKind.Renamed:
                Show(string.IsNullOrEmpty(message.DestinationName) ? StringTable.English.Get(StringKeys.MoveNameCleared) : StringTable.English.Format(StringKeys.MoveNamed, message.DestinationName));
                BasesWindow.ShowList(message.Bases);
                break;
            case MoveMessageKind.RenameRefused:
                Show(StringTable.English.Get(StringKeys.BasesRenameDenied));
                break;
            case MoveMessageKind.Preview:
                requester!.OnPreview(message.RequestId, message.Refusal, message.PlayerName, message.Summary);
                break;
            case MoveMessageKind.Cancelled:
                requester!.OnCancelled(message.RequestId, message.Refusal, message.PlayerName);
                break;
            case MoveMessageKind.Done:
                if (message.Summary != null)
                {
                    requester!.OnDone(message.RequestId, message.Revisions, message.Summary, now);
                    RefreshBasesWindow();
                }

                break;
        }
    }

    private static ContainerId StationHere()
    {
        List<CraftingStation> stations = StationsOfBaseHere();
        if (stations.Count == 0)
        {
            return default;
        }

        ZDOID uid = stations[0].m_nview.GetZDO().m_uid;
        return new ContainerId(uid.UserID, uid.ID);
    }

    /// <summary>The loaded stations of the base around the player, nearest station's base first; empty outside a base.</summary>
    private static List<CraftingStation> StationsOfBaseHere()
    {
        var result = new List<CraftingStation>();
        Player player = Player.m_localPlayer;
        if (player == null || world == null || settings == null)
        {
            return result;
        }

        Vector3 here = player.transform.position;
        world.Stations.CopyItemsTo(StationScratch);
        var snapshots = new List<StationSnapshot>(StationScratch.Count);
        var byId = new Dictionary<StationId, CraftingStation>(StationScratch.Count);
        StationId nearest = default;
        float nearestDistance = float.MaxValue;
        foreach (CraftingStation station in StationScratch)
        {
            if (station == null || station.m_nview == null || !station.m_nview.IsValid() || station.m_nview.GetZDO() == null)
            {
                continue;
            }

            ZDOID uid = station.m_nview.GetZDO().m_uid;
            var id = new StationId(uid.UserID, uid.ID);
            Vector3 point = station.transform.position;
            float range = station.GetStationBuildRange();
            snapshots.Add(new StationSnapshot(id, new WorldPosition(point.x, point.y, point.z), range));
            byId[id] = station;
            float distance = Vector3.Distance(here, point);
            if (distance <= range && distance < nearestDistance)
            {
                nearest = id;
                nearestDistance = distance;
            }
        }

        StationScratch.Clear();
        if (nearest.IsNone)
        {
            return result;
        }

        foreach (StationSnapshot member in StationGraph.FindBase(snapshots, nearest, settings.MaxBaseStations).Stations)
        {
            result.Add(byId[member.Id]);
        }

        return result;
    }
}
