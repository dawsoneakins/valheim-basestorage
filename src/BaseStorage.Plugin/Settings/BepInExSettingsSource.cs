namespace BaseStorage.Plugin.Settings;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Settings;
using BepInEx.Configuration;
using UnityEngine;

/// <summary>
/// The one place that binds the settings catalog to BepInEx config entries. Plugin code reads values through the Core settings model, never
/// from here; the one exception is the hotkey press query of <see cref="IHotkeySource"/>, which needs the BepInEx shortcut itself (S-E004-004).
/// </summary>
internal sealed class BepInExSettingsSource : ISettingsSource, IHotkeySource
{
    private readonly Dictionary<string, ConfigEntry<bool>> bools = new Dictionary<string, ConfigEntry<bool>>();
    private readonly Dictionary<string, ConfigEntry<int>> ints = new Dictionary<string, ConfigEntry<int>>();
    private readonly Dictionary<string, ConfigEntry<string>> texts = new Dictionary<string, ConfigEntry<string>>();
    private readonly Dictionary<string, ConfigEntry<KeyboardShortcut>> hotkeys = new Dictionary<string, ConfigEntry<KeyboardShortcut>>();

    /// <summary>Binds every catalog entry on the plugin's own config file.</summary>
    /// <param name="config">The plugin's config file.</param>
    /// <exception cref="ArgumentNullException">The config file is null.</exception>
    public BepInExSettingsSource(ConfigFile config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        bool previousSaveOnConfigSet = config.SaveOnConfigSet;
        config.SaveOnConfigSet = false;
        try
        {
            foreach (SettingDefinition definition in SettingsCatalog.All)
            {
                switch (definition.Kind)
                {
                    case SettingKind.Bool:
                        bools[definition.Key] = config.Bind(definition.Section, definition.Key, bool.Parse(definition.DefaultText), definition.Description);
                        break;
                    case SettingKind.Int:
                        ints[definition.Key] = config.Bind(definition.Section, definition.Key, int.Parse(definition.DefaultText, CultureInfo.InvariantCulture), definition.Description);
                        break;
                    case SettingKind.ItemList:
                        texts[definition.Key] = config.Bind(definition.Section, definition.Key, definition.DefaultText, definition.Description);
                        break;
                    case SettingKind.Hotkey:
                        hotkeys[definition.Key] = config.Bind(definition.Section, definition.Key, KeyboardShortcut.Empty, definition.Description);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            config.Save();
        }
        finally
        {
            config.SaveOnConfigSet = previousSaveOnConfigSet;
        }
    }

    /// <inheritdoc />
    public bool TryGetBool(string key, out bool value)
    {
        value = false;
        if (key == null)
        {
            return false;
        }

        if (!bools.TryGetValue(key, out ConfigEntry<bool> entry))
        {
            return false;
        }

        value = entry.Value;
        return true;
    }

    /// <inheritdoc />
    public bool TryGetInt(string key, out int value)
    {
        value = 0;
        if (key == null)
        {
            return false;
        }

        if (!ints.TryGetValue(key, out ConfigEntry<int> entry))
        {
            return false;
        }

        value = entry.Value;
        return true;
    }

    /// <inheritdoc />
    public bool TryGetText(string key, out string value)
    {
        value = string.Empty;
        if (key == null)
        {
            return false;
        }

        if (texts.TryGetValue(key, out ConfigEntry<string> text))
        {
            value = text.Value ?? string.Empty;
            return true;
        }

        if (hotkeys.TryGetValue(key, out ConfigEntry<KeyboardShortcut> hotkey))
        {
            value = hotkey.Value.MainKey == KeyCode.None ? string.Empty : hotkey.Value.Serialize();
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsHotkeyDown(string key)
    {
        if (key == null)
        {
            return false;
        }

        return hotkeys.TryGetValue(key, out ConfigEntry<KeyboardShortcut> hotkey) && hotkey.Value.IsDown();
    }
}
