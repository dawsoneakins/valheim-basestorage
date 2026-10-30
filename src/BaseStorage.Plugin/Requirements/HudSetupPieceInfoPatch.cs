namespace BaseStorage.Plugin.Requirements;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Prefix and postfix on Hud.SetupPieceInfo: they bracket the requirement lines of the build tooltip so that the requirement
/// postfix knows its caller (contract requirement-display 5.5). Display only: vanilla always runs.
/// </summary>
[HarmonyPatch(typeof(Hud), PatchTargetNames.Hud.SetupPieceInfo)]
[CraftFromBasePatch]
internal static class HudSetupPieceInfoPatch
{
    /// <summary>The patch key used by the fail-closed wrapper for the prefix.</summary>
    internal const string PrefixKey = "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch.Prefix";

    /// <summary>The patch key used by the fail-closed wrapper for the postfix.</summary>
    internal const string PostfixKey = "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch.Postfix";

    private static void Prefix(Piece piece)
    {
        if (CraftFromBaseGate.IsOpen())
        {
            PrefixCore(piece);
        }
    }

    private static void PrefixCore(Piece piece) =>
        PatchSafety.Run(PrefixKey, () => PluginRequirementDisplay.OnBuildTooltipEnter(piece));

    private static void Postfix()
    {
        if (CraftFromBaseGate.IsOpen())
        {
            PostfixCore();
        }
    }

    private static void PostfixCore() =>
        PatchSafety.Run(PostfixKey, static () => PluginRequirementDisplay.OnExit(RequirementDisplayContext.BuildTooltip));
}
