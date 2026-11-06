namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.Shared;

/// <summary>
/// GW-U-1151 and GW-U-1152 (AC-3 and AC-5; contract 8.1 and 8.2): every crafting constant of contract 8.1 holds its verbatim value,
/// and no crafting id is a HaveRequirements or GetFirstRequiredItem patch. No ValheimDir is needed. Constants are read by reflection
/// so that a missing constant fails its own row (ADR-GWIN-124).
/// </summary>
public sealed class PatchTargetNamesCraftingTests
{
    // GW-U-1152 (8.1): every value of contract 8.1, verbatim.
    [Theory]
    [InlineData("Player", "HaveRequirementItems", "HaveRequirementItems")]
    [InlineData("Player", "HaveRequirementItemsParametersRecipe", "Recipe,System.Boolean,System.Int32,System.Int32")]
    [InlineData("Player", "ConsumeResources", "ConsumeResources")]
    [InlineData("Player", "ConsumeResourcesParametersRequirements", "Piece+Requirement[],System.Int32,System.Int32,System.Int32")]
    [InlineData("Player", "GetCurrentCraftingStation", "GetCurrentCraftingStation")]
    [InlineData("Player", "NoCostCheat", "NoCostCheat")]
    [InlineData("Player", "RequiredCraftingStation", "RequiredCraftingStation")]
    [InlineData("Player", "GetFirstRequiredItem", "GetFirstRequiredItem")]
    [InlineData("Humanoid", "TypeName", "Humanoid")]
    [InlineData("Humanoid", "GetInventory", "GetInventory")]
    [InlineData("CraftingStation", "FieldUpgrader", "m_upgrader")]
    [InlineData("CraftingStation", "FieldUpgraderType", "System.Boolean")]
    [InlineData("CraftingStation", "FieldCraftingSkill", "m_craftingSkill")]
    [InlineData("CraftingStation", "FieldCraftingSkillType", "Skills+SkillType")]
    [InlineData("InventoryGui", "DoCrafting", "DoCrafting")]
    [InlineData("InventoryGui", "DoCraftingParametersPlayer", "Player")]
    [InlineData("InventoryGui", "FieldCraftRecipe", "m_craftRecipe")]
    [InlineData("InventoryGui", "FieldCraftRecipeType", "Recipe")]
    [InlineData("InventoryGui", "FieldCraftUpgradeItem", "m_craftUpgradeItem")]
    [InlineData("InventoryGui", "FieldCraftUpgradeItemType", "ItemDrop+ItemData")]
    [InlineData("InventoryGui", "FieldMultiCrafting", "m_multiCrafting")]
    [InlineData("InventoryGui", "FieldMultiCraftingType", "System.Boolean")]
    [InlineData("InventoryGui", "FieldMultiCraftAmount", "m_multiCraftAmount")]
    [InlineData("InventoryGui", "FieldMultiCraftAmountType", "System.Int32")]
    [InlineData("InventoryGui", "FieldCraftBonusAmount", "m_craftBonusAmount")]
    [InlineData("InventoryGui", "FieldCraftBonusAmountType", "System.Int32")]
    [InlineData("InventoryGui", "IsVisible", "IsVisible")]
    [InlineData("InventoryGui", "InCraftTab", "InCraftTab")]
    [InlineData("InventoryGui", "InUpradeTab", "InUpradeTab")]
    [InlineData("Recipe", "TypeName", "Recipe")]
    [InlineData("Recipe", "FieldItem", "m_item")]
    [InlineData("Recipe", "FieldItemType", "ItemDrop")]
    [InlineData("Recipe", "FieldAmount", "m_amount")]
    [InlineData("Recipe", "FieldAmountType", "System.Int32")]
    [InlineData("Recipe", "FieldResources", "m_resources")]
    [InlineData("Recipe", "FieldResourcesType", "Piece+Requirement[]")]
    [InlineData("Recipe", "FieldRequireOnlyOneIngredient", "m_requireOnlyOneIngredient")]
    [InlineData("Recipe", "FieldRequireOnlyOneIngredientType", "System.Boolean")]
    [InlineData("Requirement", "TypeName", "Piece+Requirement")]
    [InlineData("Requirement", "GetAmount", "GetAmount")]
    [InlineData("Requirement", "FieldResItem", "m_resItem")]
    [InlineData("Requirement", "FieldResItemType", "ItemDrop")]
    [InlineData("Requirement", "FieldUpgraderResource", "m_upgraderResource")]
    [InlineData("Requirement", "FieldUpgraderResourceType", "System.Boolean")]
    [InlineData("ItemDrop", "TypeName", "ItemDrop")]
    [InlineData("ItemDrop", "DropItem", "DropItem")]
    [InlineData("ItemDrop", "FieldItemData", "m_itemData")]
    [InlineData("ItemDrop", "FieldItemDataType", "ItemDrop+ItemData")]
    [InlineData("ItemData", "Clone", "Clone")]
    [InlineData("SharedData", "FieldMaxQuality", "m_maxQuality")]
    [InlineData("SharedData", "FieldMaxQualityType", "System.Int32")]
    [InlineData("SharedData", "FieldDlc", "m_dlc")]
    [InlineData("SharedData", "FieldDlcType", "System.String")]
    [InlineData("Inventory", "ContainsItem", "ContainsItem")]
    [InlineData("Inventory", "CanAddItem", "CanAddItem")]
    [InlineData("Inventory", "CanAddItemParametersItemStack", "ItemDrop+ItemData,System.Int32")]
    [InlineData("Inventory", "CanAddItemParametersPrefabStack", "UnityEngine.GameObject,System.Int32")]
    [InlineData("Inventory", "AddItem", "AddItem")]
    [InlineData("Inventory", "AddItemParametersItem", "ItemDrop+ItemData")]
    [InlineData("Inventory", "RemoveItem", "RemoveItem")]
    [InlineData("Inventory", "RemoveItemParametersItemAmount", "ItemDrop+ItemData,System.Int32")]
    [InlineData("DLCMan", "TypeName", "DLCMan")]
    [InlineData("DLCMan", "GetInstance", "get_instance")]
    [InlineData("DLCMan", "IsDlcInstalled", "IsDLCInstalled")]
    [InlineData("DLCMan", "IsDlcInstalledParametersName", "System.String")]
    [InlineData("ZoneSystem", "TypeName", "ZoneSystem")]
    [InlineData("ZoneSystem", "GetInstance", "get_instance")]
    [InlineData("ZoneSystem", "GetGlobalKey", "GetGlobalKey")]
    [InlineData("ZoneSystem", "GetGlobalKeyParametersKey", "GlobalKeys")]
    [InlineData("HarmonyPatched", "PlayerHaveRequirementItems", "Player.HaveRequirementItems(Recipe,System.Boolean,System.Int32,System.Int32)")]
    [InlineData("HarmonyPatched", "PlayerConsumeResources", "Player.ConsumeResources(Piece+Requirement[],System.Int32,System.Int32,System.Int32)")]
    [InlineData("HarmonyPatched", "InventoryGuiDoCrafting", "InventoryGui.DoCrafting(Player)")]
    // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1654): the three building patch ids of contract 8.1 join the crafting ids.
    [InlineData("HarmonyPatched", "PlayerHaveRequirementsPiece", "Player.HaveRequirements(Piece,Player+RequirementMode)")]
    [InlineData("HarmonyPatched", "PlayerTryPlacePiece", "Player.TryPlacePiece(Piece)")]
    [InlineData("HarmonyPatched", "PlayerPlacePiece", "Player.PlacePiece(Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean)")]
    public void TheCraftingNameHoldsTheContractValue(string owner, string name, string expected)
    {
        Assert.Equal(expected, Constant(owner, name));
    }

    // GW-U-1151 (AC-3): no HarmonyPatched value starts with HaveRequirements( or GetFirstRequiredItem(.
    [Fact]
    public void NoHarmonyPatchedValueTargetsTheForbiddenPlayerMethods()
    {
        // The negative is meaningful only once the crafting constants exist, so the row also needs them.
        Assert.NotNull(Constant("Player", "HaveRequirementItems"));
        Type? holder = typeof(PatchTargetNames).GetNestedType("HarmonyPatched", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, "PatchTargetNames has no nested class HarmonyPatched.");

        string[] values = holder!
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(field => (string?)field.GetValue(null) ?? string.Empty)
            .ToArray();

        // Edited by S-E003-006, ADR-GWIN-132 (craft v3 text, GW-U-1151): exactly one value starts with Player.HaveRequirements( and it is the piece overload.
        Assert.Equal(
            new[] { "Player.HaveRequirements(Piece,Player+RequirementMode)" },
            values.Where(value => value.StartsWith("Player.HaveRequirements(", StringComparison.Ordinal)).ToArray());
    }

    // GW-U-1151 (craft v3 text, AC-3): no HarmonyPatched value targets the recipe overload or the first-required-item lookup.
    [Fact]
    public void NoHarmonyPatchedValueTargetsTheRecipeOverloadOrTheFirstRequiredLookup()
    {
        Type? holder = typeof(PatchTargetNames).GetNestedType("HarmonyPatched", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, "PatchTargetNames has no nested class HarmonyPatched.");

        string[] values = holder!
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(field => (string?)field.GetValue(null) ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(values, value => value.StartsWith("Player.HaveRequirements(Recipe", StringComparison.Ordinal) || value.StartsWith("Player.GetFirstRequiredItem(", StringComparison.Ordinal));
    }

    // GW-U-1654 (ADR-GWIN-132): the HarmonyPatched set holds eighteen ids, the fifteen of earlier stories plus the three of contract 8.1.
    // Edited by integration (S-E003-005 + S-E003-006): plus the three requirement-display ids of S-E003-005, twenty-one.
    // Edited by integration (S-E004-004): plus InventoryGuiUpdateContainer, twenty-two.
    // Edited by S-E004-012 (ADR-GWIN-136): plus InventoryGuiUpdateRecipe, twenty-three.
    [Fact]
    public void HarmonyPatchedHoldsTwentyThreeIds()
    {
        Type? holder = typeof(PatchTargetNames).GetNestedType("HarmonyPatched", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, "PatchTargetNames has no nested class HarmonyPatched.");

        // Edited by S-E010-002 (organize-console-command 6.2): plus TerminalInitTerminal, twenty-four.
        Assert.Equal(49, holder!.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly).Length);
    }

    // GW-U-1151 (AC-3): the piece overload's parameter text is unchanged.
    [Fact]
    public void TheHaveRequirementsPieceParameterTextIsUnchanged()
    {
        Assert.Equal("Piece,Player+RequirementMode", Constant("Player", "HaveRequirementsParametersPiece"));
    }

    private static string? Constant(string owner, string name)
    {
        Type? holder = typeof(PatchTargetNames).GetNestedType(owner, BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, $"PatchTargetNames has no owner class {owner}.");

        FieldInfo? field = holder!.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(field is not null, $"PatchTargetNames.{owner}.{name} is missing.");

        return (string?)field!.GetValue(null);
    }
}
