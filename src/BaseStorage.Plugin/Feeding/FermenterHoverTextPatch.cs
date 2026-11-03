namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Fermenter.GetHoverText: adds the feed panel's hint to a fermenter. A failure leaves the vanilla text.</summary>
[HarmonyPatch(typeof(Fermenter), PatchTargetNames.Fermenter.GetHoverText)]
internal static class FermenterHoverTextPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.FermenterHoverTextPatch.Postfix";

    private static void Postfix(Fermenter __instance, ref string __result) =>
        __result = PatchSafety.Run(PatchKey, __instance, __result, static (hovered, text) => FeedPanel.HoverText(hovered, text));
}
