namespace BaseStorage.Plugin.Ui;

using System;
using BaseStorage.Core.Logging;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The one look of every panel the mod builds: the game's own wood behind it, with darker recesses for lists and fields. The wood lives in the game's
/// UI assets, which only the running game can show, so it is found once by its sprite's name. A panel is never left see-through. Main thread only.
/// </summary>
internal static class PanelStyle
{
    /// <summary>How far a list moves per notch of the mouse wheel, shared so every list scrolls alike.</summary>
    internal const float ScrollSpeed = 400f;

    /// <summary>A recess in the wood, for lists and rows.</summary>
    internal static readonly Color Inset = new Color(0.07f, 0.04f, 0.02f, 0.45f);

    /// <summary>A deeper recess, for a text field.</summary>
    internal static readonly Color Field = new Color(0.05f, 0.03f, 0.02f, 0.85f);

    /// <summary>A dark recess behind long text, which is hard to read directly on the bright wood.</summary>
    internal static readonly Color Reading = new Color(0.07f, 0.04f, 0.02f, 0.78f);

    /// <summary>A thin line between groups on the wood.</summary>
    internal static readonly Color Divider = new Color(0.07f, 0.04f, 0.02f, 0.6f);

    private const string WoodSpriteName = "woodpanel";

    private static readonly Color FallbackWood = new Color(0.47f, 0.31f, 0.18f, 1f);

    private static Sprite? wood;
    private static bool searched;

    /// <summary>Gives an image the game's wood, or a solid wood colour if the game's can't be found.</summary>
    /// <param name="target">The panel's background.</param>
    /// <param name="gui">The inventory screen to look for the wood under first.</param>
    internal static void Wood(Image target, InventoryGui gui)
    {
        if (wood == null && !searched)
        {
            searched = true;
            wood = Find(gui);
            Log(wood == null ? "no woodpanel sprite found; panels use a solid colour" : "panels use sprite '" + wood.name + "'");
        }

        if (wood == null)
        {
            target.color = FallbackWood;
            return;
        }

        target.sprite = wood;
        target.type = wood.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        target.color = Color.white;
    }

    private static Sprite? Find(InventoryGui gui)
    {
        Sprite? best = null;
        foreach (Image image in gui.GetComponentsInChildren<Image>(true))
        {
            best = Better(best, image.sprite);
        }

        if (best != null)
        {
            return best;
        }

        foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            best = Better(best, sprite);
        }

        return best;
    }

    private static Sprite? Better(Sprite? best, Sprite? candidate)
    {
        if (candidate == null || candidate.name.IndexOf(WoodSpriteName, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return best;
        }

        return best == null || Area(candidate) > Area(best) ? candidate : best;
    }

    private static float Area(Sprite sprite) => sprite.rect.width * sprite.rect.height;

    private static void Log(string message) => LogPolicy.Write(BaseStoragePlugin.LogSink, LogCategory.Load, "Panel style: " + message);
}
