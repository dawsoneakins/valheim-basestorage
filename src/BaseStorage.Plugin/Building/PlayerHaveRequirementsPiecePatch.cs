namespace BaseStorage.Plugin.Building;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Prefix on Player.HaveRequirements(Piece, Player.RequirementMode): the answer for a build piece comes from Core availability over
/// the inventory and the chests of the base containing the player when the build-from-base gate is open (contract build-from-base 5.4).
/// </summary>
[HarmonyPatch(typeof(Player), PatchTargetNames.Player.HaveRequirements, typeof(Piece), typeof(Player.RequirementMode))]
[CraftFromBasePatch]
internal static class PlayerHaveRequirementsPiecePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Building.PlayerHaveRequirementsPiecePatch.Prefix";

    private static bool Prefix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result) =>
        !CraftFromBaseGate.IsOpen() || PrefixCore(__instance, piece, mode, ref __result);

    private static bool PrefixCore(Player player, Piece piece, Player.RequirementMode mode, ref bool result) =>
        PatchSafety.RunPrefix(PatchKey, ref result, (ref bool answer) => PluginBuilding.OnHaveRequirements(player, piece, mode, ref answer));
}
