namespace BaseStorage.Core.HostSync.Session;

using System;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Settings;

/// <summary>
/// Decides when the host sends its host-synced settings and category text, and when a client applies, clears or rejects them
/// (contract config-sync-contracts.md 4.1 and 4.2, ADR-GWIN-104). Stateful; main thread only.
/// </summary>
public sealed class ConfigSyncSession
{
    private readonly ISettingsSource local;
    private CategoryCatalog localCatalog = CategoryCatalog.Empty;
    private CategoryCatalog? hostCatalog;

    /// <summary>Wraps the local source in a new LayeredSettingsSource. Starts in Menu, with no host and the empty catalog.</summary>
    /// <param name="local">The local settings source. Never written to.</param>
    /// <exception cref="ArgumentNullException">The local source is null.</exception>
    public ConfigSyncSession(ISettingsSource local)
    {
        this.local = local ?? throw new ArgumentNullException(nameof(local));
        Settings = new LayeredSettingsSource(local);
    }

    /// <summary>Gets the layered view that BaseStorageSettings is built over.</summary>
    public LayeredSettingsSource Settings { get; }

    /// <summary>Gets the mode seen by the last Tick (Menu before the first).</summary>
    public ConfigSyncMode Mode { get; private set; }

    /// <summary>Gets the peer id of the host whose handshake was accepted in this Client session, or null.</summary>
    public long? HostPeerId { get; private set; }

    /// <summary>Gets the effective catalog: the host's parsed text while a host payload is applied, otherwise the local catalog.</summary>
    public CategoryCatalog Categories => Settings.HasHost && hostCatalog is not null ? hostCatalog : localCatalog;

    /// <summary>Replaces the local catalog with the parse of the local category text. Never throws.</summary>
    /// <param name="localFileText">The local category file text, or null when the file is missing.</param>
    public void LoadLocalCategories(string? localFileText)
    {
        try
        {
            localCatalog = CategoryFile.Parse(localFileText ?? string.Empty).Catalog;
        }
        catch (Exception)
        {
            localCatalog = CategoryCatalog.Empty;
        }
    }

    /// <summary>Advances the session to the current mode and returns what the Plugin must do. Never throws.</summary>
    /// <param name="mode">The mode computed by the Plugin for this frame.</param>
    /// <returns>The flags of what changed.</returns>
    public ConfigSyncTick Tick(ConfigSyncMode mode)
    {
        ConfigSyncMode m = Normalize(mode);
        ConfigSyncTick result = ConfigSyncTick.None;
        if (m != ConfigSyncMode.Client)
        {
            if (Settings.HasHost)
            {
                Settings.ClearHost();
                hostCatalog = null;
                result |= ConfigSyncTick.HostCleared;
            }

            HostPeerId = null;
        }

        if (m != Mode && m != ConfigSyncMode.Menu)
        {
            result |= ConfigSyncTick.ReloadLocalCategories;
        }

        Mode = m;
        return result;
    }

    /// <summary>Reacts to one handshake outcome: Send for an accepted modded client on the host, and the host peer is recorded on a client.</summary>
    /// <param name="outcome">The handshake outcome from PeerVersionTable.</param>
    /// <param name="mode">The mode computed by the Plugin.</param>
    /// <returns>The config-sync outcome.</returns>
    /// <exception cref="ArgumentNullException">The outcome is null.</exception>
    public ConfigSyncOutcome OnVersionOutcome(PeerVersionOutcome outcome, ConfigSyncMode mode)
    {
        if (outcome is null)
        {
            throw new ArgumentNullException(nameof(outcome));
        }

        ConfigSyncMode m = Normalize(mode);
        if (outcome.Action != PeerVersionAction.Accept)
        {
            return ConfigSyncOutcome.None;
        }

        if (!outcome.RemoteIsServer && m == ConfigSyncMode.LocalWorld && outcome.Result is not null && outcome.Result.ClientVersion is not null)
        {
            return new ConfigSyncOutcome(outcome.PeerId, ConfigSyncAction.Send, ConfigSyncRejectReason.None, null, 0);
        }

        if (outcome.RemoteIsServer && m == ConfigSyncMode.Client)
        {
            HostPeerId = outcome.PeerId;
        }

        return ConfigSyncOutcome.None;
    }

    /// <summary>Builds the host payload bytes from the local values and the category text. Never throws.</summary>
    /// <param name="categoryFileText">The category file text, or null for the empty text.</param>
    /// <returns>The payload bytes, or null when they exceed the payload cap.</returns>
    public byte[]? BuildHostPayload(string? categoryFileText)
    {
        try
        {
            byte[] bytes = HostSyncCodec.EncodeBytes(HostSyncPayload.Capture(local, categoryFileText ?? string.Empty));
            return bytes.Length > HostSyncCodec.MaxPayloadLength ? null : bytes;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Decides one received payload. Applied outcomes change Settings and Categories; Rejected ones change nothing. Never throws.</summary>
    /// <param name="peerId">The peer the payload came from.</param>
    /// <param name="payload">The received bytes, possibly null.</param>
    /// <param name="mode">The mode computed by the Plugin.</param>
    /// <returns>The config-sync outcome.</returns>
    public ConfigSyncOutcome Receive(long peerId, byte[]? payload, ConfigSyncMode mode)
    {
        if (Normalize(mode) != ConfigSyncMode.Client)
        {
            return Reject(peerId, ConfigSyncRejectReason.NotClient);
        }

        if (HostPeerId != peerId)
        {
            return Reject(peerId, ConfigSyncRejectReason.NoAcceptedHost);
        }

        if (!Settings.TryApplyHost(payload))
        {
            return Reject(peerId, ConfigSyncRejectReason.Malformed);
        }

        HostSyncPayload applied = Settings.Host!;
        hostCatalog = CategoryFile.Parse(applied.CategoryFileText).Catalog;
        return new ConfigSyncOutcome(peerId, ConfigSyncAction.Applied, ConfigSyncRejectReason.None, applied, hostCatalog.Categories.Count);
    }

    private static ConfigSyncOutcome Reject(long peerId, ConfigSyncRejectReason reason)
    {
        return new ConfigSyncOutcome(peerId, ConfigSyncAction.Rejected, reason, null, 0);
    }

    private static ConfigSyncMode Normalize(ConfigSyncMode mode)
    {
        return mode == ConfigSyncMode.LocalWorld || mode == ConfigSyncMode.Client ? mode : ConfigSyncMode.Menu;
    }
}
