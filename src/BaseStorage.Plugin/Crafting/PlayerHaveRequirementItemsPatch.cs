namespace BaseStorage.Plugin.Crafting;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Prefix on the private materials half of the recipe check, Player.HaveRequirementItems(Recipe, bool, int, int): the answer comes
/// from Core availability over the inventory and the base chests when the craft-from-base gate is open (contract craft-from-base 5.3).
/// The piece overload of HaveRequirements is not patched.
/// </summary>
[HarmonyPatch(typeof(Player), PatchTargetNames.Player.HaveRequirementItems)]
[CraftFromBasePatch]
internal static class PlayerHaveRequirementItemsPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Crafting.PlayerHaveRequirementItemsPatch.Prefix";

    private static bool Prefix(Player __instance, Recipe piece, bool discover, int qualityLevel, int amount, ref bool __result) =>
        !CraftFromBaseGate.IsOpen() || PrefixCore(__instance, piece, discover, qualityLevel, amount, ref __result);

    private static bool PrefixCore(Player player, Recipe recipe, bool discover, int qualityLevel, int amount, ref bool result) =>
        PatchSafety.RunPrefix(PatchKey, ref result, (ref bool answer) => PluginCrafting.OnHaveRequirementItems(player, recipe, discover, qualityLevel, amount, ref answer));
}
