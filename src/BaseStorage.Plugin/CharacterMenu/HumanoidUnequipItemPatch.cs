namespace BaseStorage.Plugin.CharacterMenu;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Humanoid.UnequipItem: gear in the worn row goes back to the grid. A failure leaves it in its slot.</summary>
[HarmonyPatch(typeof(Humanoid), PatchTargetNames.Humanoid.UnequipItem)]
internal static class HumanoidUnequipItemPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.CharacterMenu.HumanoidUnequipItemPatch.Postfix";

    private static void Postfix(Humanoid __instance, ItemDrop.ItemData item) =>
        PatchSafety.Run(PatchKey, __instance, item, static (who, held) => WornItems.OnUnequipped(who, held)!);
}
