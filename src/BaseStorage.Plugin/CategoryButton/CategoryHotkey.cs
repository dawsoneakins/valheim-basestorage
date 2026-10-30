namespace BaseStorage.Plugin.CategoryButton;

using System;
using BaseStorage.Core.Settings;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Settings;
using UnityEngine;

/// <summary>
/// The optional CycleCategoryKey hotkey (contract S-E004-004 section 4.3, spec AC-4.3 and AC-8.2). Ticked once per frame from the
/// plugin's Update; while the key is unbound the first check fails and nothing else is read or allocated. A press cycles the hovered
/// chest forward through the same path as the button's left click, behind vanilla's own input gate (no chat, console, menu or
/// inventory open). Main thread only; Unity null checks use <c>==</c> (W4).
/// </summary>
internal static class CategoryHotkey
{
    /// <summary>The patch key used by the fail-closed wrapper of the per-frame tick.</summary>
    internal const string TickKey = "BaseStorage.Plugin.CategoryButton.CategoryHotkey.Tick";

    private static IHotkeySource? hotkeys;

    /// <summary>Stores the hotkey source at boot.</summary>
    /// <param name="hotkeys">The settings binding that answers hotkey presses.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hotkeys"/> is null.</exception>
    internal static void Initialize(IHotkeySource hotkeys)
    {
        if (hotkeys == null)
        {
            throw new ArgumentNullException(nameof(hotkeys));
        }

        CategoryHotkey.hotkeys = hotkeys;
    }

    /// <summary>Polls the hotkey for this frame, behind the fail-closed wrapper.</summary>
    internal static void Tick()
    {
        PatchSafety.Run(TickKey, static () => Poll());
    }

    private static void Poll()
    {
        IHotkeySource? source = hotkeys;
        if (source == null || !source.IsHotkeyDown(SettingKeys.CycleCategoryKey))
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (player == null || !player.TakeInput())
        {
            return;
        }

        GameObject hover = player.GetHoverObject();
        if (hover == null)
        {
            return;
        }

        Container chest = hover.GetComponentInParent<Container>();
        if (chest == null)
        {
            return;
        }

        ChestCategoryButton.Cycle(chest, forward: true);
    }
}
