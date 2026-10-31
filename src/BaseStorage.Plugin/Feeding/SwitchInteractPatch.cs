namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Switch.Interact: Shift + use on a station's ore or fuel switch opens its feed panel. Anything else runs vanilla, and so does a failure.</summary>
[HarmonyPatch(typeof(Switch), PatchTargetNames.Switch.Interact)]
internal static class SwitchInteractPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.SwitchInteractPatch.Prefix";

    private static bool Prefix(Switch __instance, bool alt) =>
        PatchSafety.RunPrefix(PatchKey, () => FeedPanel.OnSwitchInteract(__instance, alt));
}
