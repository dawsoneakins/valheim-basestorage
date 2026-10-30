namespace BaseStorage.Plugin.Settings;

/// <summary>
/// Answers whether a hotkey setting was pressed this frame (contract S-E004-004 section 3). Implemented by the one settings binding,
/// so the BepInEx shortcut type stays in that file and callers never see a key code. Game-free.
/// </summary>
internal interface IHotkeySource
{
    /// <summary>Tells whether the bound shortcut of a hotkey setting went down this frame.</summary>
    /// <param name="key">The settings catalog key of the hotkey.</param>
    /// <returns>True on the frame the shortcut is pressed with its modifiers; false for an unbound, unknown or null key. Never throws.</returns>
    bool IsHotkeyDown(string key);
}
