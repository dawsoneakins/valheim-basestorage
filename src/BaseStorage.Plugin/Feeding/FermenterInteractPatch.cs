namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Fermenter.Interact: Shift + use opens the fermenter's feed panel. Anything else runs vanilla, and so does a failure.</summary>
[HarmonyPatch(typeof(Fermenter), PatchTargetNames.Fermenter.Interact)]
internal static class FermenterInteractPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.FermenterInteractPatch.Prefix";

    private static bool Prefix(Fermenter __instance, bool hold, bool alt) =>
        PatchSafety.RunPrefix(PatchKey, () => FeedPanel.OnStationInteract(__instance, hold, alt));
}
