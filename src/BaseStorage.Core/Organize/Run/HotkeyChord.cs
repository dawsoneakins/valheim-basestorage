namespace BaseStorage.Core.Organize.Run;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// A hotkey as the settings store it (contract 4.4): the BepInEx shortcut text <c>Main</c> or <c>Main+Mod1+Mod2</c>, split into
/// key names. Core never names a key code; the Plugin maps the names. An empty text, or the main key <c>None</c>, is unbound.
/// </summary>
public sealed class HotkeyChord
{
    private const char Separator = '+';
    private const string NoneKey = "None";

    private HotkeyChord(string mainKey, IReadOnlyList<string> modifiers)
    {
        MainKey = mainKey;
        Modifiers = modifiers;
    }

    /// <summary>Gets the unbound chord: no main key and no modifier.</summary>
    public static HotkeyChord Unbound { get; } = new HotkeyChord(string.Empty, new ReadOnlyCollection<string>(Array.Empty<string>()));

    /// <summary>Gets a value indicating whether a main key other than None is set.</summary>
    public bool IsBound => MainKey.Length > 0;

    /// <summary>Gets the main key name; empty when unbound.</summary>
    public string MainKey { get; }

    /// <summary>Gets the modifier key names in text order, blanks dropped; empty when unbound.</summary>
    public IReadOnlyList<string> Modifiers { get; }

    /// <summary>Parses the shortcut text. Never throws.</summary>
    /// <param name="text">The text, for example <c>G</c> or <c>G+LeftShift</c>; null, blank or <c>None</c> is unbound.</param>
    /// <returns>The chord.</returns>
    public static HotkeyChord Parse(string? text)
    {
        if (text is null)
        {
            return Unbound;
        }

        string[] parts = text.Split(Separator);
        string main = parts[0].Trim();
        if (main.Length == 0 || string.Equals(main, NoneKey, StringComparison.Ordinal))
        {
            return Unbound;
        }

        var modifiers = new List<string>(parts.Length - 1);
        for (int i = 1; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            if (part.Length > 0 && !string.Equals(part, NoneKey, StringComparison.Ordinal))
            {
                modifiers.Add(part);
            }
        }

        return new HotkeyChord(main, new ReadOnlyCollection<string>(modifiers.ToArray()));
    }
}
