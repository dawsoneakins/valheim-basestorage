namespace BaseStorage.Core.Settings;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>The typed read path over an <see cref="ISettingsSource"/>. Every read goes to the source, so nothing is cached.</summary>
public sealed class BaseStorageSettings
{
    private readonly ISettingsSource source;

    /// <summary>Creates the read path over a source.</summary>
    /// <param name="source">The source to read from. Must not be null.</param>
    /// <exception cref="ArgumentNullException">The source is null.</exception>
    public BaseStorageSettings(ISettingsSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>Reads a Bool setting by key, falling back to the catalog default.</summary>
    /// <param name="key">A Bool key from the catalog.</param>
    /// <returns>The source value, or the default when the source has none.</returns>
    /// <exception cref="ArgumentNullException">The key is null.</exception>
    /// <exception cref="ArgumentException">The key is unknown or is not a Bool key.</exception>
    public bool GetBool(string key)
    {
        SettingDefinition def = Require(key, SettingKind.Bool);
        return source.TryGetBool(key, out bool value) ? value : bool.Parse(def.DefaultText);
    }

    /// <summary>Reads an Int setting by key, falling back to the catalog default.</summary>
    /// <param name="key">An Int key from the catalog.</param>
    /// <returns>The source value, or the default when the source has none.</returns>
    /// <exception cref="ArgumentNullException">The key is null.</exception>
    /// <exception cref="ArgumentException">The key is unknown or is not an Int key.</exception>
    public int GetInt(string key)
    {
        SettingDefinition def = Require(key, SettingKind.Int);
        return source.TryGetInt(key, out int value) ? value : int.Parse(def.DefaultText, CultureInfo.InvariantCulture);
    }

    /// <summary>Reads an ItemList setting by key, parsed with <see cref="ItemListText"/>.</summary>
    /// <param name="key">An ItemList key from the catalog.</param>
    /// <returns>The parsed source text, or the parsed default when the source has none.</returns>
    /// <exception cref="ArgumentNullException">The key is null.</exception>
    /// <exception cref="ArgumentException">The key is unknown or is not an ItemList key.</exception>
    public IReadOnlyList<string> GetItemList(string key)
    {
        SettingDefinition def = Require(key, SettingKind.ItemList);
        return ItemListText.Parse(source.TryGetText(key, out string value) ? value ?? string.Empty : def.DefaultText);
    }

    /// <summary>Reads a Hotkey setting as opaque text. An empty string means unbound.</summary>
    /// <param name="key">A Hotkey key from the catalog.</param>
    /// <returns>The source text, or the empty default when the source has none.</returns>
    /// <exception cref="ArgumentNullException">The key is null.</exception>
    /// <exception cref="ArgumentException">The key is unknown or is not a Hotkey key.</exception>
    public string GetHotkeyText(string key)
    {
        SettingDefinition def = Require(key, SettingKind.Hotkey);
        return source.TryGetText(key, out string value) ? value ?? string.Empty : def.DefaultText;
    }

    /// <summary>Gets the CraftFromBase setting.</summary>
    public bool CraftFromBase => GetBool(SettingKeys.CraftFromBase);

    /// <summary>Gets the ExcludeVehicles setting.</summary>
    public bool ExcludeVehicles => GetBool(SettingKeys.ExcludeVehicles);

    /// <summary>Gets the KeepChestsCraftable setting.</summary>
    public bool KeepChestsCraftable => GetBool(SettingKeys.KeepChestsCraftable);

    /// <summary>Gets the MaxBaseStations setting.</summary>
    public int MaxBaseStations => GetInt(SettingKeys.MaxBaseStations);

    /// <summary>Gets the BaseCacheSeconds setting.</summary>
    public int BaseCacheSeconds => GetInt(SettingKeys.BaseCacheSeconds);

    /// <summary>Gets the RequireSameVersion setting.</summary>
    public bool RequireSameVersion => GetBool(SettingKeys.RequireSameVersion);

    /// <summary>Gets the AutoAssignThresholdPct setting.</summary>
    public int AutoAssignThresholdPct => GetInt(SettingKeys.AutoAssignThresholdPct);

    /// <summary>Gets the OrganizeSkipPreview setting.</summary>
    public bool OrganizeSkipPreview => GetBool(SettingKeys.OrganizeSkipPreview);

    /// <summary>Gets the DepositPreview setting.</summary>
    public bool DepositPreview => GetBool(SettingKeys.DepositPreview);

    /// <summary>Gets the DepositNeverItems setting.</summary>
    public IReadOnlyList<string> DepositNeverItems => GetItemList(SettingKeys.DepositNeverItems);

    /// <summary>Gets the MovesPerFrame setting.</summary>
    public int MovesPerFrame => GetInt(SettingKeys.MovesPerFrame);

    /// <summary>Gets the OrganizeLockSeconds setting.</summary>
    public int OrganizeLockSeconds => GetInt(SettingKeys.OrganizeLockSeconds);

    /// <summary>Gets the ReserveTimeoutMs setting.</summary>
    public int ReserveTimeoutMs => GetInt(SettingKeys.ReserveTimeoutMs);

    /// <summary>Gets the StationButtons setting.</summary>
    public bool StationButtons => GetBool(SettingKeys.StationButtons);

    /// <summary>Gets the ShowCategoryTooltip setting.</summary>
    public bool ShowCategoryTooltip => GetBool(SettingKeys.ShowCategoryTooltip);

    /// <summary>Gets the CycleCategoryKey hotkey text. Empty means unbound.</summary>
    public string CycleCategoryKey => GetHotkeyText(SettingKeys.CycleCategoryKey);

    /// <summary>Gets the OrganizeKey hotkey text. Empty means unbound.</summary>
    public string OrganizeKey => GetHotkeyText(SettingKeys.OrganizeKey);

    /// <summary>Gets the DepositKey hotkey text. Empty means unbound.</summary>
    public string DepositKey => GetHotkeyText(SettingKeys.DepositKey);

    /// <summary>Gets the DebugLogging setting.</summary>
    public bool DebugLogging => GetBool(SettingKeys.DebugLogging);

    /// <summary>Gets the AutoAssignMixedChests setting.</summary>
    public bool AutoAssignMixedChests => GetBool(SettingKeys.AutoAssignMixedChests);

    private static SettingDefinition Require(string key, SettingKind kind)
    {
        SettingDefinition def = SettingsCatalog.Get(key);
        if (def.Kind != kind)
        {
            throw new ArgumentException("The key is not a " + kind + " setting.", nameof(key));
        }

        return def;
    }
}
