namespace BaseStorage.Plugin.Hover;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Postfix on Container.GetHoverText: appends the base line (and the category or Keep) to the vanilla hover text (ADR-GWIN-032, spec S-E002-005).
/// The body is one fail-closed call that forwards to <see cref="HoverLines"/>; a failure keeps the vanilla text.
/// </summary>
[HarmonyPatch(typeof(Container), PatchTargetNames.Container.GetHoverText)]
internal static class ContainerHoverTextPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Hover.ContainerHoverTextPatch.Postfix";

    private static void Postfix(Container __instance, ref string __result)
    {
        __result = PatchSafety.Run(PatchKey, __instance, __result, static (c, text) => HoverLines.AppendChest(c, text));
    }
}
