namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Fireplace.Interact: Shift + use opens the fire's feed panel. Anything else runs vanilla, and so does a failure.</summary>
[HarmonyPatch(typeof(Fireplace), PatchTargetNames.Fireplace.Interact)]
internal static class FireplaceInteractPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.FireplaceInteractPatch.Prefix";

    private static bool Prefix(Fireplace __instance, bool hold, bool alt) =>
        PatchSafety.RunPrefix(PatchKey, () => FeedPanel.OnStationInteract(__instance, hold, alt));
}
