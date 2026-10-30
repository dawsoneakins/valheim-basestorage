namespace BaseStorage.Plugin.Building;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Prefix and postfix on Player.TryPlacePiece(Piece): the prefix skips vanilla and starts a consume-before-place build when the
/// build-from-base gate is open and chest items are needed; the postfix turns a guarded, suppressed placement into a failed one
/// (contract build-from-base 5.4, BF5 and BF8).
/// </summary>
[HarmonyPatch(typeof(Player), PatchTargetNames.Player.TryPlacePiece)]
[CraftFromBasePatch]
internal static class PlayerTryPlacePiecePatch
{
    /// <summary>The patch key of the prefix used by the fail-closed wrapper.</summary>
    internal const string PrefixKey = "BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch.Prefix";

    /// <summary>The patch key of the postfix used by the fail-closed wrapper.</summary>
    internal const string PostfixKey = "BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch.Postfix";

    private static bool Prefix(Player __instance, Piece piece, ref bool __result) =>
        !CraftFromBaseGate.IsOpen() || PrefixCore(__instance, piece, ref __result);

    private static bool PrefixCore(Player player, Piece piece, ref bool result) =>
        PatchSafety.RunPrefix(PrefixKey, ref result, (ref bool answer) => PluginBuilding.OnTryPlacePiece(player, piece, ref answer));

    private static void Postfix(ref bool __result)
    {
        if (CraftFromBaseGate.IsOpen())
        {
            PostfixCore(ref __result);
        }
    }

    private static void PostfixCore(ref bool result)
    {
        result = PatchSafety.Run(PostfixKey, result, static placed => PluginBuilding.AfterTryPlacePiece(placed));
    }
}
