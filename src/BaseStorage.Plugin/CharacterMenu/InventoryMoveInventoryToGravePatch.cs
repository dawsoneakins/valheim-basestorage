namespace BaseStorage.Plugin.CharacterMenu;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Inventory.MoveInventoryToGrave: the copy takes the player's 8x4 size, so the grave gets its extra row back. A failure leaves it 8x4.</summary>
[HarmonyPatch(typeof(Inventory), PatchTargetNames.Inventory.MoveInventoryToGrave)]
internal static class InventoryMoveInventoryToGravePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.CharacterMenu.InventoryMoveInventoryToGravePatch.Postfix";

    private static void Postfix(Inventory __instance) =>
        PatchSafety.Run(PatchKey, __instance, static grave => WornItems.WidenGrave(grave));
}
