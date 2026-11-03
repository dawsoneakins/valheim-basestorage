namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Fireplace.GetHoverText: adds the feed panel's hint to a fire. A failure leaves the vanilla text.</summary>
[HarmonyPatch(typeof(Fireplace), PatchTargetNames.Fireplace.GetHoverText)]
internal static class FireplaceHoverTextPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.FireplaceHoverTextPatch.Postfix";

    private static void Postfix(Fireplace __instance, ref string __result) =>
        __result = PatchSafety.Run(PatchKey, __instance, __result, static (hovered, text) => FeedPanel.HoverText(hovered, text));
}
