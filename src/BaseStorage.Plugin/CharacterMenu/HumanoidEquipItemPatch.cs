namespace BaseStorage.Plugin.CharacterMenu;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Humanoid.EquipItem: worn gear moves into its slot. A failure leaves the item equipped where it was.</summary>
[HarmonyPatch(typeof(Humanoid), PatchTargetNames.Humanoid.EquipItem)]
internal static class HumanoidEquipItemPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.CharacterMenu.HumanoidEquipItemPatch.Postfix";

    private static void Postfix(Humanoid __instance, ItemDrop.ItemData item) =>
        PatchSafety.Run(PatchKey, __instance, item, static (who, held) => WornItems.OnEquipped(who, held));
}
