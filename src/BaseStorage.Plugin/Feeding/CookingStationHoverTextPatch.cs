namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on CookingStation.GetHoverText: adds the feed panel's hint to a cooking station. A failure leaves the vanilla text.</summary>
[HarmonyPatch(typeof(CookingStation), PatchTargetNames.CookingStation.GetHoverText)]
internal static class CookingStationHoverTextPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.CookingStationHoverTextPatch.Postfix";

    private static void Postfix(CookingStation __instance, ref string __result) =>
        __result = PatchSafety.Run(PatchKey, __instance, __result, static (hovered, text) => FeedPanel.HoverText(hovered, text));
}
