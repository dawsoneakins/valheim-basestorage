namespace BaseStorage.Plugin.Ui;

using System;
using BaseStorage.Core.Ui;

/// <summary>The single place that shows MessageHud notices. Text always comes from the strings table.</summary>
internal static class Notices
{
    /// <summary>Shows one MessageHud line whose text comes from the strings table; a no-op when there is no HUD.</summary>
    /// <param name="placement">Where the notice appears; an undefined value is treated as Center.</param>
    /// <param name="key">The strings table key (built with Plural.Form for plural notices).</param>
    /// <param name="args">The placeholder values for the key's text.</param>
    public static void Show(NoticePlacement placement, string key, params object?[]? args)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        MessageHud? hud = MessageHud.instance;
        if (hud == null)
        {
            return;
        }

        MessageHud.MessageType type = placement == NoticePlacement.TopLeft
            ? MessageHud.MessageType.TopLeft
            : MessageHud.MessageType.Center;

        hud.ShowMessage(type, SingleLine.Of(StringTable.English.Format(key, args)), 0, null);
    }
}
