namespace BaseStorage.Plugin.Building;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Prefix on Player.PlacePiece: marks the committed placement and, while the placement guard is set, suppresses a piece the
/// inventory alone does not cover (contract build-from-base 5.4, BF8).
/// </summary>
[HarmonyPatch(typeof(Player), PatchTargetNames.Player.PlacePiece)]
[CraftFromBasePatch]
internal static class PlayerPlacePiecePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Building.PlayerPlacePiecePatch.Prefix";

    private static bool Prefix(Player __instance, Piece piece) =>
        !CraftFromBaseGate.IsOpen() || PrefixCore(__instance, piece);

    private static bool PrefixCore(Player player, Piece piece) =>
        PatchSafety.RunPrefix(PatchKey, () => PluginBuilding.OnPlacePiece(player, piece));
}
