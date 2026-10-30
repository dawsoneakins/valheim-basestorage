namespace BaseStorage.Plugin.Crafting;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Prefix on Player.ConsumeResources: skips vanilla only while the committed recipe's resources are consumed, because the
/// materials were already taken by the atomic executor (contract craft-from-base 5.3). Building consumption stays vanilla.
/// </summary>
[HarmonyPatch(typeof(Player), PatchTargetNames.Player.ConsumeResources)]
[CraftFromBasePatch]
internal static class PlayerConsumeResourcesPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Crafting.PlayerConsumeResourcesPatch.Prefix";

    private static bool Prefix(Player __instance, Piece.Requirement[] requirements) =>
        !CraftFromBaseGate.IsOpen() || PrefixCore(__instance, requirements);

    private static bool PrefixCore(Player player, Piece.Requirement[] requirements) =>
        PatchSafety.RunPrefix(PatchKey, () => PluginCrafting.OnConsumeResources(player, requirements));
}
