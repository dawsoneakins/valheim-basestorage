namespace BaseStorage.Plugin.Favourites;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on Humanoid.IsItemEquiped: while the Place stacks guard is on, a favourite answers as equipped, which is the check vanilla Place
/// stacks already makes. Outside the guard the vanilla answer stands. A failure leaves the vanilla answer.
/// </summary>
[HarmonyPatch(typeof(Humanoid), PatchTargetNames.Humanoid.IsItemEquiped)]
internal static class HumanoidIsItemEquipedPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Favourites.HumanoidIsItemEquipedPatch.Postfix";

    private static void Postfix(ItemDrop.ItemData item, ref bool __result) =>
        __result = PatchSafety.Run(PatchKey, item, __result, static (held, equipped) => equipped || (FavouritePlaceGuard.Active && FavouritePlaceGuard.Protects(held)));
}
