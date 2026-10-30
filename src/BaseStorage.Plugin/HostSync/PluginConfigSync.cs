namespace BaseStorage.Plugin.HostSync;

using System;
using System.Collections.Generic;
using System.IO;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Settings;
using BaseStorage.Plugin.Categories;
using BaseStorage.Plugin.Patching;
using static BaseStorage.Core.HostSync.Session.ConfigSyncMode;

/// <summary>
/// The plugin-side adapter of config sync (contract config-sync-contracts.md 6). It owns the one <see cref="ConfigSyncSession"/>,
/// sends the host payload to an accepted modded client, hands received payloads to the session and ticks it once per frame.
/// Game-bound, so its facts are proven by source scans. Every decision is the session's; it never writes the local config.
/// </summary>
internal static class PluginConfigSync
{
    /// <summary>The patch key of the per-frame tick.</summary>
    internal const string TickKey = "BaseStorage.Plugin.HostSync.PluginConfigSync.Tick";

    /// <summary>The patch key of the payload receive handler.</summary>
    internal const string ReceiveKey = "BaseStorage.Plugin.HostSync.PluginConfigSync.Receive";

    /// <summary>The patch key of the host send.</summary>
    internal const string SendKey = "BaseStorage.Plugin.HostSync.PluginConfigSync.Send";

    private static ILogSink? logSink;
    private static string? categoryFolder;

    /// <summary>Gets the session, or null until <see cref="Initialize"/> has run.</summary>
    internal static ConfigSyncSession? Current { get; private set; }

    /// <summary>Gets the effective category catalog: the host's while a host payload is applied, otherwise the local one.</summary>
    internal static CategoryCatalog Categories => Current?.Categories ?? CategoryCatalog.Empty;

    /// <summary>Creates the one session at boot. Later calls return the existing session's settings view.</summary>
    /// <param name="local">The local settings source.</param>
    /// <param name="sink">The log sink.</param>
    /// <returns>The layered settings view.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static LayeredSettingsSource Initialize(ISettingsSource local, ILogSink sink)
    {
        if (local == null)
        {
            throw new ArgumentNullException(nameof(local));
        }

        if (sink == null)
        {
            throw new ArgumentNullException(nameof(sink));
        }

        if (Current == null)
        {
            Current = new ConfigSyncSession(local);
            logSink = sink;
        }

        return Current.Settings;
    }

    /// <summary>Records the BepInEx config folder that holds the local category file.</summary>
    /// <param name="folder">The config folder.</param>
    internal static void UseCategoryFolder(string folder)
    {
        categoryFolder = folder;
    }

    /// <summary>Receives the host payload on a client, behind the fail-closed wrapper.</summary>
    /// <param name="id">The handshake peer id.</param>
    /// <param name="package">The received package, which may be null.</param>
    internal static void OnPayload(long id, ZPackage package)
    {
        if (Current == null)
        {
            return;
        }

        PatchSafety.Run(ReceiveKey, () =>
        {
            ILogSink? sink = logSink;
            if (Current == null || sink == null)
            {
                return;
            }

            byte[]? bytes;
            try
            {
                bytes = package == null ? null : package.ReadByteArray();
            }
            catch (Exception)
            {
                bytes = null;
            }

            ConfigSyncOutcome o = Current.Receive(id, bytes, Mode());
            string? line = ConfigSyncMessages.Format(o);
            if (line == null)
            {
                return;
            }

            if (ConfigSyncMessages.IsWarning(o))
            {
                try
                {
                    sink.Log(LogSeverity.Warning, line);
                }
                catch (Exception)
                {
                    return;
                }
            }
            else if (LogPolicy.IsEnabled(sink, ConfigSyncMessages.CategoryOf(o)))
            {
                LogPolicy.Write(sink, ConfigSyncMessages.CategoryOf(o), line);
            }

            if (o.Action == ConfigSyncAction.Applied && o.Payload != null && LogPolicy.IsEnabled(sink, LogCategory.Detail))
            {
                foreach (KeyValuePair<string, string> pair in o.Payload.Settings)
                {
                    LogPolicy.Write(sink, LogCategory.Detail, ConfigSyncMessages.SettingDetail(pair.Key, pair.Value));
                }
            }
        });
    }

    /// <summary>Reacts to a handshake outcome: the host sends its payload to an accepted modded client; a client records its host.</summary>
    /// <param name="outcome">The handshake outcome.</param>
    /// <param name="peer">The peer of the outcome, which may be null.</param>
    internal static void OnVersionOutcome(PeerVersionOutcome outcome, ZNetPeer? peer)
    {
        if (Current == null)
        {
            return;
        }

        PatchSafety.Run(SendKey, () =>
        {
            ILogSink? sink = logSink;
            if (Current == null || sink == null)
            {
                return;
            }

            ConfigSyncOutcome o = Current.OnVersionOutcome(outcome, Mode());
            if (o.Action != ConfigSyncAction.Send || peer == null || peer.m_rpc == null)
            {
                return;
            }

            byte[]? bytes = Current.BuildHostPayload(ReadCategoryText());
            if (bytes == null)
            {
                try
                {
                    sink.Log(LogSeverity.Warning, ConfigSyncMessages.TooLarge(o.PeerId));
                }
                catch (Exception)
                {
                    return;
                }

                return;
            }

            var package = new ZPackage();
            package.Write(bytes);
            peer.m_rpc.Invoke(ConfigSyncRpc.Name, package);
            if (LogPolicy.IsEnabled(sink, LogCategory.Detail))
            {
                LogPolicy.Write(sink, LogCategory.Detail, ConfigSyncMessages.Sent(o.PeerId, bytes.Length));
            }
        });
    }

    /// <summary>Advances the session by one frame, behind the fail-closed wrapper.</summary>
    internal static void Tick()
    {
        if (Current == null)
        {
            return;
        }

        PatchSafety.Run(TickKey, () =>
        {
            if (Current == null)
            {
                return;
            }

            ConfigSyncTick t = Current.Tick(Mode());
            if (t == ConfigSyncTick.None)
            {
                return;
            }

            ILogSink? sink = logSink;
            if ((t & ConfigSyncTick.HostCleared) != 0 && sink != null && LogPolicy.IsEnabled(sink, LogCategory.Detail))
            {
                LogPolicy.Write(sink, LogCategory.Detail, ConfigSyncMessages.Cleared());
            }

            if ((t & ConfigSyncTick.ReloadLocalCategories) != 0)
            {
                Current.LoadLocalCategories(ReadCategoryText());
            }
        });
    }

    private static ConfigSyncMode Mode()
    {
        return ZNet.instance == null ? Menu : ZNet.instance.IsServer() ? LocalWorld : Client;
    }

    private static string ReadCategoryText()
    {
        string path = categoryFolder == null ? string.Empty : DefaultCategoryFileWriter.PathFor(categoryFolder);
        if (categoryFolder != null)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception)
            {
                // Missing or unreadable: fall through to the empty text.
            }
        }

        ILogSink? sink = logSink;
        if (sink != null && LogPolicy.IsEnabled(sink, LogCategory.Detail))
        {
            LogPolicy.Write(sink, LogCategory.Detail, ConfigSyncMessages.CategoryFileUnavailable(path));
        }

        return "";
    }
}
