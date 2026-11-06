namespace BaseStorage.Plugin.CharacterMenu;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Inventory.GetEmptySlots: only items inside the grid use its space. A failure leaves the vanilla count.</summary>
[HarmonyPatch(typeof(Inventory), PatchTargetNames.Inventory.GetEmptySlots)]
internal static class InventoryGetEmptySlotsPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.CharacterMenu.InventoryGetEmptySlotsPatch.Postfix";

    private static void Postfix(Inventory __instance, ref int __result) =>
        __result = PatchSafety.Run(PatchKey, __instance, __result, static (inventory, _) => WornItems.EmptySlots(inventory));
}
