namespace BaseStorage.Core.HostSync;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Settings;

/// <summary>
/// An <see cref="ISettingsSource"/> that answers host-synced keys from an applied <see cref="HostSyncPayload"/> and everything else
/// from the local source (contract host-sync-contracts.md 5, rules H5 and H6). Not thread-safe: apply, clear and read on the game
/// main thread (ADR-GWIN-083). Never writes to the local source.
/// </summary>
public sealed class LayeredSettingsSource : ISettingsSource
{
    private readonly ISettingsSource local;
    private HostLayer? host;

    /// <summary>Wraps the local source. Starts with no host payload applied.</summary>
    /// <param name="local">The local settings source (the Plugin's config adapter).</param>
    /// <exception cref="ArgumentNullException">The local source is null.</exception>
    public LayeredSettingsSource(ISettingsSource local)
    {
        this.local = local ?? throw new ArgumentNullException(nameof(local));
    }

    /// <summary>Gets a value indicating whether a host payload is applied (connected as a client).</summary>
    public bool HasHost => host is not null;

    /// <summary>Gets the applied host payload, or null. Its category file text is what the Plugin parses for the session.</summary>
    public HostSyncPayload? Host => host?.Payload;

    /// <summary>Overlays a host payload: every host-synced key now answers from it.</summary>
    /// <param name="payload">The decoded host payload.</param>
    /// <exception cref="ArgumentNullException">The payload is null.</exception>
    public void ApplyHost(HostSyncPayload payload)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        host = new HostLayer(payload);
    }

    /// <summary>Decodes payload text and applies it; a malformed payload changes nothing. Never throws.</summary>
    /// <param name="payload">The received text, possibly null.</param>
    /// <returns>True when the payload decoded and was applied; false when nothing changed.</returns>
    public bool TryApplyHost(string? payload)
    {
        return TryApply(HostSyncCodec.Decode(payload));
    }

    /// <summary>Decodes payload bytes and applies them; a malformed payload changes nothing. Never throws.</summary>
    /// <param name="payload">The received bytes, possibly null.</param>
    /// <returns>True when the payload decoded and was applied; false when nothing changed.</returns>
    public bool TryApplyHost(byte[]? payload)
    {
        return TryApply(HostSyncCodec.Decode(payload));
    }

    /// <summary>Removes the host payload: every key answers from the local source again. Idempotent.</summary>
    public void ClearHost()
    {
        host = null;
    }

    /// <summary>Reads a Bool value: the host's while a payload is applied and the key is host-synced, the local source's otherwise.</summary>
    /// <param name="key">The setting key. Never throws for a string key; null gives false.</param>
    /// <param name="value">The value when the method returns true; false otherwise.</param>
    /// <returns>True when the answering layer holds a Bool value for the key.</returns>
    public bool TryGetBool(string key, out bool value)
    {
        value = false;
        if (key is null)
        {
            return false;
        }

        HostLayer? layer = host;
        if (layer is not null && layer.Payload.TryGetValueText(key, out _))
        {
            return layer.Bools.TryGetValue(key, out value);
        }

        return local.TryGetBool(key, out value);
    }

    /// <summary>Reads an Int value: the host's while a payload is applied and the key is host-synced, the local source's otherwise.</summary>
    /// <param name="key">The setting key. Never throws for a string key; null gives false.</param>
    /// <param name="value">The value when the method returns true; 0 otherwise.</param>
    /// <returns>True when the answering layer holds an Int value for the key.</returns>
    public bool TryGetInt(string key, out int value)
    {
        value = 0;
        if (key is null)
        {
            return false;
        }

        HostLayer? layer = host;
        if (layer is not null && layer.Payload.TryGetValueText(key, out _))
        {
            return layer.Ints.TryGetValue(key, out value);
        }

        return local.TryGetInt(key, out value);
    }

    /// <summary>Reads ItemList or Hotkey text: the host's while a payload is applied and the key is host-synced, the local source's otherwise.</summary>
    /// <param name="key">The setting key. Never throws for a string key; null gives false.</param>
    /// <param name="value">The text when the method returns true; an empty string otherwise.</param>
    /// <returns>True when the answering layer holds text for the key.</returns>
    public bool TryGetText(string key, out string value)
    {
        value = string.Empty;
        if (key is null)
        {
            return false;
        }

        HostLayer? layer = host;
        if (layer is not null && layer.Payload.TryGetValueText(key, out _))
        {
            if (layer.Texts.TryGetValue(key, out string? text))
            {
                value = text;
                return true;
            }

            return false;
        }

        return local.TryGetText(key, out value);
    }

    private bool TryApply(HostSyncPayload? payload)
    {
        if (payload is null)
        {
            return false;
        }

        host = new HostLayer(payload);
        return true;
    }

    /// <summary>The applied payload with its values parsed once, by kind, so reads do no parsing.</summary>
    private sealed class HostLayer
    {
        public HostLayer(HostSyncPayload payload)
        {
            Payload = payload;
            Bools = new Dictionary<string, bool>(StringComparer.Ordinal);
            Ints = new Dictionary<string, int>(StringComparer.Ordinal);
            Texts = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (SettingDefinition row in SettingsCatalog.HostSynced)
            {
                string text = payload.ValueText(row.Key);
                switch (row.Kind)
                {
                    case SettingKind.Bool:
                        Bools[row.Key] = bool.Parse(text);
                        break;
                    case SettingKind.Int:
                        Ints[row.Key] = int.Parse(text, CultureInfo.InvariantCulture);
                        break;
                    default:
                        Texts[row.Key] = text;
                        break;
                }
            }
        }

        public HostSyncPayload Payload { get; }

        public Dictionary<string, bool> Bools { get; }

        public Dictionary<string, int> Ints { get; }

        public Dictionary<string, string> Texts { get; }
    }
}
