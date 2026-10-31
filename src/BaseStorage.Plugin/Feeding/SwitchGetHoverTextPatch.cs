namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Switch.GetHoverText: adds the feed panel's hint to a station's ore and fuel switches. A failure leaves the vanilla text.</summary>
[HarmonyPatch(typeof(Switch), PatchTargetNames.Switch.GetHoverText)]
internal static class SwitchGetHoverTextPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.SwitchGetHoverTextPatch.Postfix";

    private static void Postfix(Switch __instance, ref string __result) =>
        __result = PatchSafety.Run(PatchKey, __instance, __result, static (hovered, text) => FeedPanel.HoverText(hovered, text));
}
