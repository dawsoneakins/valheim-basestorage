namespace BaseStorage.Plugin.Requirements;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Prefix and postfix on InventoryGui.SetupRequirementList: they bracket the requirement lines of the crafting panel so that
/// the requirement postfix knows its caller (contract requirement-display 5.5). Display only: vanilla always runs.
/// </summary>
[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.SetupRequirementList)]
[CraftFromBasePatch]
internal static class InventoryGuiSetupRequirementListPatch
{
    /// <summary>The patch key used by the fail-closed wrapper for the prefix.</summary>
    internal const string PrefixKey = "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch.Prefix";

    /// <summary>The patch key used by the fail-closed wrapper for the postfix.</summary>
    internal const string PostfixKey = "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch.Postfix";

    private static void Prefix(int quality, int amount)
    {
        if (CraftFromBaseGate.IsOpen())
        {
            PrefixCore(quality, amount);
        }
    }

    private static void PrefixCore(int quality, int amount) =>
        PatchSafety.Run(PrefixKey, () => PluginRequirementDisplay.OnCraftingPanelEnter(quality, amount));

    private static void Postfix()
    {
        if (CraftFromBaseGate.IsOpen())
        {
            PostfixCore();
        }
    }

    private static void PostfixCore() =>
        PatchSafety.Run(PostfixKey, static () => PluginRequirementDisplay.OnExit(RequirementDisplayContext.CraftingPanel));
}
