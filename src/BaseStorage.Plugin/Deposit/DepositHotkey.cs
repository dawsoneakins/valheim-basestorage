namespace BaseStorage.Plugin.Deposit;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Settings;
using UnityEngine;

/// <summary>
/// Polls the DepositKey (contract deposit-button-contracts.md, rule DB11, which follows RN12). The shortcut text is re-read at most
/// once per second of the clock and parsed by <see cref="HotkeyChord"/>; the names are mapped to key codes once per text. The poll is
/// skipped while there is no local player, or while the chat, the console, a text input, the menu, the inventory or a popup has the
/// keyboard. Unbound (the default) polls nothing and allocates nothing.
/// </summary>
internal static class DepositHotkey
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);
    private static readonly List<KeyCode> Modifiers = new List<KeyCode>();

    private static string text = string.Empty;
    private static bool bound;
    private static KeyCode main;
    private static TimeSpan lastRead = TimeSpan.Zero;

    /// <summary>Tells whether the bound chord was pressed this frame.</summary>
    /// <param name="settings">The settings that hold the DepositKey text.</param>
    /// <param name="clock">The clock that paces the text refresh.</param>
    /// <returns>True on the frame the main key goes down while every modifier is held and no UI has the keyboard.</returns>
    internal static bool IsPressed(BaseStorageSettings settings, IClock clock)
    {
        TimeSpan now = clock.Elapsed;
        if (now < lastRead || now - lastRead >= RefreshInterval)
        {
            lastRead = now;
            Refresh(settings.DepositKey);
        }

        if (!bound || Player.m_localPlayer == null || KeyboardTaken())
        {
            return false;
        }

        if (!ZInput.GetKeyDown(main, false))
        {
            return false;
        }

        for (int i = 0; i < Modifiers.Count; i++)
        {
            if (!ZInput.GetKey(Modifiers[i], false))
            {
                return false;
            }
        }

        return true;
    }

    private static bool KeyboardTaken()
    {
        Chat chat = Chat.instance;
        return (chat != null && chat.HasFocus())
            || global::Console.IsVisible()
            || TextInput.IsVisible()
            || Menu.IsVisible()
            || InventoryGui.IsVisible()
            || UnifiedPopup.IsVisible();
    }

    private static void Refresh(string current)
    {
        if (string.Equals(current, text, StringComparison.Ordinal))
        {
            return;
        }

        text = current;
        Modifiers.Clear();
        HotkeyChord chord = HotkeyChord.Parse(current);
        bound = chord.IsBound && Enum.TryParse(chord.MainKey, true, out main);
        for (int i = 0; bound && i < chord.Modifiers.Count; i++)
        {
            if (Enum.TryParse(chord.Modifiers[i], true, out KeyCode modifier))
            {
                Modifiers.Add(modifier);
            }
            else
            {
                bound = false;
            }
        }
    }
}
