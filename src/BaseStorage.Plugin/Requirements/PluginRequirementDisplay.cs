namespace BaseStorage.Plugin.Requirements;

using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Crafting;
using TMPro;
using UnityEngine;

/// <summary>
/// The plugin-side entry point of the requirement display (contract requirement-display 5.1). It owns the one session and
/// forwards the patch bodies. Display only: it reads and writes nothing but the requirement element. Main thread only (W2).
/// </summary>
internal static class PluginRequirementDisplay
{
    /// <summary>Gets the session, or null until <see cref="Initialize"/> has run.</summary>
    internal static RequirementDisplaySession? Current { get; private set; }

    /// <summary>Creates the one session at boot. Later calls return the existing instance.</summary>
    /// <returns>The session.</returns>
    internal static RequirementDisplaySession Initialize()
    {
        if (Current == null)
        {
            var craftWorld = new GameCraftWorld();
            var craftGame = new GameCraftGame(craftWorld);
            var world = new GameRequirementDisplayWorld(craftGame, craftWorld);
            Current = new RequirementDisplaySession(world);
        }

        return Current;
    }

    /// <summary>Enters the crafting panel context.</summary>
    /// <param name="qualityLevel">The quality of the prefix.</param>
    /// <param name="multiplier">The multi-craft amount of the prefix.</param>
    internal static void OnCraftingPanelEnter(int qualityLevel, int multiplier)
    {
        if (Current == null)
        {
            return;
        }

        Current.EnterCraftingPanel(qualityLevel, multiplier);
    }

    /// <summary>Enters the build tooltip context for a piece, or leaves it when there is no piece.</summary>
    /// <param name="piece">The piece being described.</param>
    internal static void OnBuildTooltipEnter(Piece piece)
    {
        if (Current == null)
        {
            return;
        }

        if (piece == null)
        {
            Current.Exit(RequirementDisplayContext.BuildTooltip);
            return;
        }

        Current.EnterBuildTooltip();
    }

    /// <summary>Leaves a context.</summary>
    /// <param name="context">The context being left.</param>
    internal static void OnExit(RequirementDisplayContext context)
    {
        if (Current != null)
        {
            Current.Exit(context);
        }
    }

    /// <summary>Rewrites the amount of one requirement element when base chests contribute (contract 5.4).</summary>
    /// <param name="elementRoot">The requirement element.</param>
    /// <param name="req">The requirement.</param>
    /// <param name="craft">The craft argument of the call.</param>
    /// <param name="quality">The quality of the call.</param>
    /// <param name="craftMultiplier">The multiplier of the call.</param>
    /// <param name="result">The vanilla result.</param>
    internal static void OnSetupRequirement(Transform elementRoot, Piece.Requirement req, bool craft, int quality, int craftMultiplier, bool result)
    {
        if (Current == null || Current.Context == RequirementDisplayContext.None)
        {
            return;
        }

        if (!result || elementRoot == null || req == null || req.m_resItem == null)
        {
            return;
        }

        if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(craft ? GlobalKeys.NoCraftCost : GlobalKeys.NoBuildCost))
        {
            return;
        }

        long need = (long)req.GetAmount(quality) * craftMultiplier;
        Transform amountRoot = elementRoot.Find("res_amount");
        if (amountRoot == null)
        {
            return;
        }

        TMP_Text amountText = amountRoot.GetComponent<TMP_Text>();
        if (amountText == null)
        {
            return;
        }

        RequirementDisplay? display = Current.Resolve(req.m_resItem.gameObject.name, need, amountText.text);
        if (display == null || !display.Value.ShowsChests)
        {
            return;
        }

        amountText.text = display.Value.Text;
        amountText.color = display.Value.IsShort ? (Mathf.Sin(Time.time * 10f) > 0f ? Color.red : Color.white) : Color.white;
    }
}
