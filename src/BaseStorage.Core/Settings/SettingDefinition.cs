namespace BaseStorage.Core.Settings;

/// <summary>One entry of the settings catalog: where a setting lives, its kind, its default and its description.</summary>
public sealed class SettingDefinition
{
    /// <summary>Creates one catalog entry. Only <see cref="SettingsCatalog"/> calls this.</summary>
    /// <param name="section">One of the <see cref="SettingSections"/> values.</param>
    /// <param name="key">One of the <see cref="SettingKeys"/> values.</param>
    /// <param name="kind">The value kind.</param>
    /// <param name="defaultText">The canonical invariant default text.</param>
    /// <param name="hostSynced">True when the host's value wins in multiplayer.</param>
    /// <param name="description">The single-line description shown in the settings file.</param>
    internal SettingDefinition(string section, string key, SettingKind kind, string defaultText, bool hostSynced, string description)
    {
        Section = section;
        Key = key;
        Kind = kind;
        DefaultText = defaultText;
        HostSynced = hostSynced;
        Description = description;
    }

    /// <summary>The section name, one of the <see cref="SettingSections"/> values.</summary>
    public string Section { get; }

    /// <summary>The key, one of the <see cref="SettingKeys"/> values. Unique in the catalog.</summary>
    public string Key { get; }

    /// <summary>The value kind.</summary>
    public SettingKind Kind { get; }

    /// <summary>The canonical invariant default text. An empty string means unbound for a hotkey.</summary>
    public string DefaultText { get; }

    /// <summary>True when the host's value wins in multiplayer.</summary>
    public bool HostSynced { get; }

    /// <summary>The single-line description, shown verbatim in the settings file.</summary>
    public string Description { get; }
}
