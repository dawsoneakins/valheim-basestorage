namespace BaseStorage.Core.Settings;

/// <summary>A source of setting values. The Plugin implements it over its own config; tests use a fake.</summary>
public interface ISettingsSource
{
    /// <summary>Reads a Bool value.</summary>
    /// <param name="key">The setting key. Implementations never throw for a string key.</param>
    /// <param name="value">The current value when the method returns true; false otherwise.</param>
    /// <returns>True when this source holds a Bool value for the key.</returns>
    bool TryGetBool(string key, out bool value);

    /// <summary>Reads an Int value.</summary>
    /// <param name="key">The setting key. Implementations never throw for a string key.</param>
    /// <param name="value">The current value when the method returns true; 0 otherwise.</param>
    /// <returns>True when this source holds an Int value for the key.</returns>
    bool TryGetInt(string key, out int value);

    /// <summary>Reads text for an ItemList or Hotkey value.</summary>
    /// <param name="key">The setting key. Implementations never throw for a string key.</param>
    /// <param name="value">The current text when the method returns true; an empty string otherwise.</param>
    /// <returns>True when this source holds an ItemList or Hotkey value for the key.</returns>
    bool TryGetText(string key, out string value);
}
