namespace BaseStorage.Plugin.CharacterMenu;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Inventory.HaveEmptySlot: only items inside the grid use its space. A failure leaves the vanilla answer.</summary>
[HarmonyPatch(typeof(Inventory), PatchTargetNames.Inventory.HaveEmptySlot)]
internal static class InventoryHaveEmptySlotPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.CharacterMenu.InventoryHaveEmptySlotPatch.Postfix";

    private static void Postfix(Inventory __instance, ref bool __result) =>
        __result = PatchSafety.Run(PatchKey, __instance, __result, static (inventory, _) => WornItems.EmptySlots(inventory) > 0);
}
