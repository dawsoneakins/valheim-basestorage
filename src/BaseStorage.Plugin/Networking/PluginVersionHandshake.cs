namespace BaseStorage.Plugin.Networking;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Networking;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.HostSync;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;

/// <summary>
/// The plugin-side adapter of the version handshake (contract version-rpc-contracts.md 5). It owns the one
/// <see cref="PeerVersionTable"/>, registers and sends the version RPC on every new connection, hands received payloads to the
/// table, ticks it once per frame and performs the returned actions through the vanilla connection-error path. Game-bound, so it
/// is not linked into the tests; its facts are proven by source scans. Every decision is the table's.
/// </summary>
internal static class PluginVersionHandshake
{
    /// <summary>The patch key used by the fail-closed wrapper of the per-frame tick.</summary>
    internal const string TickKey = "BaseStorage.Plugin.Networking.PluginVersionHandshake.Tick";

    private static readonly Dictionary<long, ZNetPeer> Peers = new Dictionary<long, ZNetPeer>();
    private static readonly List<long> Scratch = new List<long>();
    private static BaseStorageSettings? settings;
    private static ILogSink? sink;

    /// <summary>Gets the table, or null until <see cref="Initialize"/> has run.</summary>
    internal static PeerVersionTable? Current { get; private set; }

    /// <summary>Creates the one table at boot. Later calls return the existing instance.</summary>
    /// <param name="clock">The clock.</param>
    /// <param name="settings">The settings; RequireSameVersion is read on every decision.</param>
    /// <param name="sink">The log sink.</param>
    /// <returns>The table.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static PeerVersionTable Initialize(IClock clock, BaseStorageSettings settings, ILogSink sink)
    {
        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (sink == null)
        {
            throw new ArgumentNullException(nameof(sink));
        }

        if (Current == null)
        {
            Current = new PeerVersionTable(clock, ModVersion.Current);
            PluginVersionHandshake.settings = settings;
            PluginVersionHandshake.sink = sink;
        }

        return Current;
    }

    /// <summary>Registers the version RPC on a new connection and sends the local payload once (contract 5.1).</summary>
    /// <param name="peer">The new peer, which may be null or half-built.</param>
    internal static void OnNewConnection(ZNetPeer peer)
    {
        PeerVersionTable? table = Current;
        if (table == null || peer == null || peer.m_rpc == null)
        {
            return;
        }

        long id = table.Track(peer.m_server);
        Peers[id] = peer;
        peer.m_rpc.Register<ZPackage>(ConfigSyncRpc.Name, (rpc, package) => PluginConfigSync.OnPayload(id, package));
        peer.m_rpc.Register<ZPackage>(VersionRpc.Name, (rpc, package) => OnPayload(id, package));
        var payload = new ZPackage();
        payload.Write(VersionHandshake.EncodePayloadBytes(ModVersion.Current));
        peer.m_rpc.Invoke(VersionRpc.Name, payload);
    }

    /// <summary>Advances the handshake by one frame, behind the fail-closed wrapper.</summary>
    internal static void Tick()
    {
        PatchSafety.Run(TickKey, static () => TickCore());
    }

    /// <summary>Appends the pending host mismatch to the vanilla connection-error panel when it shows the version error (contract 5.4).</summary>
    /// <param name="startup">The main menu, which may be null.</param>
    internal static void OnConnectErrorShown(FejdStartup startup)
    {
        PeerVersionTable? table = Current;
        if (table == null || startup == null || startup.m_connectionFailedPanel == null || !startup.m_connectionFailedPanel.activeSelf)
        {
            return;
        }

        VersionCheckResult? mismatch = table.TakeHostMismatch();
        if (mismatch == null || startup.m_connectionFailedError == null || ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.ErrorVersion)
        {
            return;
        }

        string? line = mismatch.FormatMessage(StringTable.English);
        if (line != null)
        {
            startup.m_connectionFailedError.text = startup.m_connectionFailedError.text + "\n" + line;
        }
    }

    private static void OnPayload(long id, ZPackage package)
    {
        PatchSafety.Run(TickKey, () =>
        {
            PeerVersionTable? table = Current;
            if (table == null)
            {
                return;
            }

            byte[]? bytes = package == null ? null : package.ReadByteArray();
            Apply(table.Receive(id, bytes, RequireSameVersion()));
        });
    }

    private static void TickCore()
    {
        PluginConfigSync.Tick();
        PeerVersionTable? table = Current;
        if (table == null)
        {
            return;
        }

        ZNet net = ZNet.instance;
        if (net == null)
        {
            if (table.Count > 0)
            {
                table.Clear();
                Peers.Clear();
            }

            return;
        }

        if (table.HasHostMismatch && !net.IsServer() && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
        {
            table.TakeHostMismatch();
        }

        if (table.Count == 0)
        {
            return;
        }

        List<ZNetPeer> connected = net.GetPeers();
        foreach (KeyValuePair<long, ZNetPeer> pair in Peers)
        {
            if (!connected.Contains(pair.Value))
            {
                Scratch.Add(pair.Key);
            }
        }

        for (int i = 0; i < Scratch.Count; i++)
        {
            table.Forget(Scratch[i]);
            Peers.Remove(Scratch[i]);
        }

        Scratch.Clear();

        IReadOnlyList<PeerVersionOutcome> outcomes = table.Tick(RequireSameVersion());
        for (int i = 0; i < outcomes.Count; i++)
        {
            Apply(outcomes[i]);
        }
    }

    private static void Apply(PeerVersionOutcome outcome)
    {
        if (outcome.Action == PeerVersionAction.Ignore)
        {
            return;
        }

        string? line = PeerVersionMessages.Format(outcome);
        if (line != null && LogPolicy.IsEnabled(sink, LogCategory.ConfigSync))
        {
            LogPolicy.Write(sink, LogCategory.ConfigSync, line);
        }

        string? key = outcome.MessageKey;
        if (key != null && outcome.Result != null && outcome.Action != PeerVersionAction.LeaveMismatch)
        {
            Notices.Show(NoticePlacement.Center, key, outcome.Result.HostVersion, outcome.Result.ClientVersion);
        }

        Peers.TryGetValue(outcome.PeerId, out ZNetPeer? peer);
        PluginConfigSync.OnVersionOutcome(outcome, peer);
        switch (outcome.Action)
        {
            case PeerVersionAction.RefuseMismatch:
            case PeerVersionAction.RefuseMissing:
                if (peer != null && peer.m_rpc != null)
                {
                    peer.m_rpc.Invoke(VersionRpc.VanillaErrorName, (int)ZNet.ConnectionStatus.ErrorVersion);
                }

                break;
            case PeerVersionAction.Disconnect:
                Peers.Remove(outcome.PeerId);
                if (peer != null && ZNet.instance != null)
                {
                    ZNet.instance.Disconnect(peer);
                }

                break;
            case PeerVersionAction.LeaveMismatch:
                ZNet.SetExternalError(ZNet.ConnectionStatus.ErrorVersion);
                break;
            default:
                break;
        }
    }

    private static bool RequireSameVersion() => settings == null || settings.RequireSameVersion;
}
