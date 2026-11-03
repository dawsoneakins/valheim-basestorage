namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on CookingStation.Interact: Shift + use opens the cooking station's feed panel. Anything else runs vanilla, and so does a failure.</summary>
[HarmonyPatch(typeof(CookingStation), PatchTargetNames.CookingStation.Interact)]
internal static class CookingStationInteractPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.CookingStationInteractPatch.Prefix";

    private static bool Prefix(CookingStation __instance, bool hold, bool alt) =>
        PatchSafety.RunPrefix(PatchKey, () => FeedPanel.OnStationInteract(__instance, hold, alt));
}
