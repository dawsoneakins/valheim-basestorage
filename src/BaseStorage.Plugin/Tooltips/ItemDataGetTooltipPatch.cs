namespace BaseStorage.Plugin.Tooltips;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Postfix on the static ItemData.GetTooltip the inventory grid and the recipe preview share: appends the category line (S-B-001).
/// The body is one fail-closed call; a failure keeps the vanilla text.
/// </summary>
[HarmonyPatch(
    typeof(ItemDrop.ItemData),
    PatchTargetNames.ItemData.GetTooltip,
    new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
internal static class ItemDataGetTooltipPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Tooltips.ItemDataGetTooltipPatch.Postfix";

    private static void Postfix(ItemDrop.ItemData item, ref string __result)
    {
        __result = PatchSafety.Run(PatchKey, item, __result, static (i, text) => ItemCategoryLine.Append(i, text));
    }
}
