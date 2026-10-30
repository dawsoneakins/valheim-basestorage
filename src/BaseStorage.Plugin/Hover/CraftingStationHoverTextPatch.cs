namespace BaseStorage.Plugin.Hover;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Postfix on CraftingStation.GetHoverText: appends the chest count of the station's base to the vanilla hover text (ADR-GWIN-032, spec S-E002-005).
/// The body is one fail-closed call that forwards to <see cref="HoverLines"/>; a failure keeps the vanilla text.
/// </summary>
[HarmonyPatch(typeof(CraftingStation), PatchTargetNames.CraftingStation.GetHoverText)]
internal static class CraftingStationHoverTextPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Hover.CraftingStationHoverTextPatch.Postfix";

    private static void Postfix(CraftingStation __instance, ref string __result)
    {
        __result = PatchSafety.Run(PatchKey, __instance, __result, static (s, text) => HoverLines.AppendStation(s, text));
    }
}
