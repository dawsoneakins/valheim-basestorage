namespace BaseStorage.Core.HostSync;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using BaseStorage.Core.Settings;

/// <summary>
/// The host's host-synced settings (every <see cref="SettingsCatalog.HostSynced"/> row as canonical text, in catalog order) plus
/// the host's category file text, LF-normalised (contract host-sync-contracts.md 2.1, rules H1 to H3). Immutable, value-equal.
/// </summary>
public sealed class HostSyncPayload : IEquatable<HostSyncPayload>
{
    private const string True = "true";
    private const string False = "false";

    private readonly ReadOnlyCollection<KeyValuePair<string, string>> settings;
    private readonly string categoryFileText;

    /// <summary>Builds a payload from explicit setting texts and category file text. Any input order is accepted; catalog order is stored.</summary>
    /// <param name="settings">One (key, canonical text) pair for every host-synced key, each key once, no other key.</param>
    /// <param name="categoryFileText">The category file text; CRLF and lone CR become LF.</param>
    /// <exception cref="ArgumentNullException">The settings or the text is null.</exception>
    /// <exception cref="ArgumentException">A key or value is null, a key is not host-synced, a key repeats, a synced key is missing, or a value is not canonical for its kind.</exception>
    public HostSyncPayload(IEnumerable<KeyValuePair<string, string>> settings, string categoryFileText)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (categoryFileText is null)
        {
            throw new ArgumentNullException(nameof(categoryFileText));
        }

        IReadOnlyList<SettingDefinition> rows = SettingsCatalog.HostSynced;
        var ordered = new string?[rows.Count];
        foreach (KeyValuePair<string, string> pair in settings)
        {
            if (pair.Key is null || pair.Value is null)
            {
                throw new ArgumentException("A key or value is null.", nameof(settings));
            }

            int index = IndexOf(pair.Key);
            if (index < 0)
            {
                throw new ArgumentException("A key is not host-synced.", nameof(settings));
            }

            if (ordered[index] is not null)
            {
                throw new ArgumentException("A key repeats.", nameof(settings));
            }

            if (!HostSyncValueText.IsCanonical(rows[index].Kind, pair.Value))
            {
                throw new ArgumentException("A value is not canonical for its kind.", nameof(settings));
            }

            ordered[index] = pair.Value;
        }

        var pairs = new KeyValuePair<string, string>[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            pairs[i] = new KeyValuePair<string, string>(rows[i].Key, ordered[i] ?? throw new ArgumentException("A host-synced key is missing.", nameof(settings)));
        }

        this.settings = new ReadOnlyCollection<KeyValuePair<string, string>>(pairs);
        this.categoryFileText = categoryFileText.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    /// <summary>Gets the synced settings as (key, canonical text), in <see cref="SettingsCatalog.HostSynced"/> order.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Settings => settings;

    /// <summary>Gets the host's category file text, LF-normalised. Never null; may be empty.</summary>
    public string CategoryFileText => categoryFileText;

    /// <summary>Reads every host-synced row from a source and formats it canonically; a missing or non-canonical value takes the row's default.</summary>
    /// <param name="source">The local settings source. Never written to.</param>
    /// <param name="categoryFileText">The category file text; CRLF and lone CR become LF.</param>
    /// <returns>The captured payload.</returns>
    /// <exception cref="ArgumentNullException">The source or the text is null.</exception>
    public static HostSyncPayload Capture(ISettingsSource source, string categoryFileText)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (categoryFileText is null)
        {
            throw new ArgumentNullException(nameof(categoryFileText));
        }

        IReadOnlyList<SettingDefinition> rows = SettingsCatalog.HostSynced;
        var pairs = new KeyValuePair<string, string>[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            SettingDefinition row = rows[i];
            string text;
            switch (row.Kind)
            {
                case SettingKind.Bool:
                    text = source.TryGetBool(row.Key, out bool flag) ? (flag ? True : False) : row.DefaultText;
                    break;
                case SettingKind.Int:
                    text = source.TryGetInt(row.Key, out int number) ? number.ToString(CultureInfo.InvariantCulture) : row.DefaultText;
                    break;
                default:
                    text = source.TryGetText(row.Key, out string raw) && HostSyncValueText.TryCanonicalize(row.Kind, raw, out string canonical)
                        ? canonical
                        : row.DefaultText;
                    break;
            }

            pairs[i] = new KeyValuePair<string, string>(row.Key, text);
        }

        return new HostSyncPayload(pairs, categoryFileText);
    }

    /// <summary>Gets the canonical text of one synced key.</summary>
    /// <param name="key">A host-synced key.</param>
    /// <returns>The canonical text.</returns>
    /// <exception cref="ArgumentNullException">The key is null.</exception>
    /// <exception cref="ArgumentException">The key is not host-synced.</exception>
    public string ValueText(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        int index = IndexOf(key);
        if (index < 0)
        {
            throw new ArgumentException("The key is not host-synced.", nameof(key));
        }

        return settings[index].Value;
    }

    /// <summary>Tries to get the canonical text of a key. Never throws for a string key; null gives false.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The canonical text when the method returns true; an empty string otherwise.</param>
    /// <returns>True when the key is host-synced.</returns>
    public bool TryGetValueText(string key, out string value)
    {
        int index = key is null ? -1 : IndexOf(key);
        if (index < 0)
        {
            value = string.Empty;
            return false;
        }

        value = settings[index].Value;
        return true;
    }

    /// <summary>Compares two payloads by value: same settings in order (ordinal) and same category text (ordinal).</summary>
    /// <param name="other">The other payload, possibly null.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(HostSyncPayload? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (settings.Count != other.settings.Count || !string.Equals(categoryFileText, other.categoryFileText, StringComparison.Ordinal))
        {
            return false;
        }

        for (int i = 0; i < settings.Count; i++)
        {
            if (!string.Equals(settings[i].Key, other.settings[i].Key, StringComparison.Ordinal)
                || !string.Equals(settings[i].Value, other.settings[i].Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Compares with any object by value.</summary>
    /// <param name="obj">The other object, possibly null.</param>
    /// <returns>True when the object is an equal payload.</returns>
    public override bool Equals(object? obj) => Equals(obj as HostSyncPayload);

    /// <summary>Computes a hash code consistent with <see cref="Equals(HostSyncPayload)"/>.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = StringComparer.Ordinal.GetHashCode(categoryFileText);
            foreach (KeyValuePair<string, string> pair in settings)
            {
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(pair.Key);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(pair.Value);
            }

            return hash;
        }
    }

    /// <summary>Value equality; null equals only null.</summary>
    /// <param name="left">The left payload.</param>
    /// <param name="right">The right payload.</param>
    /// <returns>True when both are null or equal by value.</returns>
    public static bool operator ==(HostSyncPayload? left, HostSyncPayload? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Value inequality.</summary>
    /// <param name="left">The left payload.</param>
    /// <param name="right">The right payload.</param>
    /// <returns>True when the payloads differ.</returns>
    public static bool operator !=(HostSyncPayload? left, HostSyncPayload? right) => !(left == right);

    /// <summary>The index of a key in <see cref="SettingsCatalog.HostSynced"/>, or -1. Ordinal and case-sensitive.</summary>
    private static int IndexOf(string key)
    {
        IReadOnlyList<SettingDefinition> rows = SettingsCatalog.HostSynced;
        for (int i = 0; i < rows.Count; i++)
        {
            if (string.Equals(rows[i].Key, key, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
