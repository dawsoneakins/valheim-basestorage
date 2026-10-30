namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.Shared;

/// <summary>
/// GW-U-1650 and GW-U-1651 (AC-9; build-from-base contract 8.1): every building constant of contract 8.1 holds its verbatim value,
/// and the three HarmonyPatched ids of the building patches compose. No ValheimDir is needed. Constants are read by reflection so
/// that a missing constant fails its own row (ADR-GWIN-132).
/// </summary>
public sealed class PatchTargetNamesBuildingTests
{
    // GW-U-1650 (8.1): every owner and constant of contract 8.1, verbatim. New owners sit before HarmonyPatched.
    [Theory]
    [InlineData("Player", "TryPlacePiece", "TryPlacePiece")]
    [InlineData("Player", "TryPlacePieceParametersPiece", "Piece")]
    [InlineData("Player", "PlacePiece", "PlacePiece")]
    [InlineData("Player", "PlacePieceParametersPiecePositionRotation", "Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean")]
    [InlineData("Player", "UpdatePlacementGhost", "UpdatePlacementGhost")]
    [InlineData("Player", "InPlaceMode", "InPlaceMode")]
    [InlineData("Player", "GetSelectedPiece", "GetSelectedPiece")]
    [InlineData("Player", "GetBuildStamina", "GetBuildStamina")]
    [InlineData("Player", "GetPlaceDurability", "GetPlaceDurability")]
    [InlineData("Player", "UseStamina", "UseStamina")]
    [InlineData("Player", "RaiseSkill", "RaiseSkill")]
    [InlineData("Player", "IsDead", "IsDead")]
    [InlineData("Player", "FieldPlacementStatus", "m_placementStatus")]
    [InlineData("Player", "FieldPlacementStatusType", "Player+PlacementStatus")]
    [InlineData("Player", "FieldPlacementGhost", "m_placementGhost")]
    [InlineData("Player", "FieldPlacementGhostType", "UnityEngine.GameObject")]
    [InlineData("Player", "FieldLastToolUseTime", "m_lastToolUseTime")]
    [InlineData("Player", "FieldLastToolUseTimeType", "System.Single")]
    [InlineData("Player", "FieldBuildPieces", "m_buildPieces")]
    [InlineData("Player", "FieldBuildPiecesType", "PieceTable")]
    [InlineData("Player", "FieldBuildRemoveDebt", "m_buildRemoveDebt")]
    [InlineData("Player", "FieldBuildRemoveDebtType", "System.Int32")]
    [InlineData("Humanoid", "GetRightItem", "GetRightItem")]
    [InlineData("Character", "TypeName", "Character")]
    [InlineData("Character", "GetZdoid", "GetZDOID")]
    [InlineData("Hud", "TypeName", "Hud")]
    [InlineData("Hud", "GetInstance", "get_instance")]
    [InlineData("Hud", "IsPieceSelectionVisible", "IsPieceSelectionVisible")]
    [InlineData("Hud", "FieldBuildUi", "m_buildUi")]
    [InlineData("Hud", "FieldBuildUiType", "BuildUi")]
    [InlineData("BuildUi", "TypeName", "BuildUi")]
    [InlineData("BuildUi", "AddRecentPiece", "AddRecentPiece")]
    [InlineData("CraftingStation", "HaveBuildStationInRange", "HaveBuildStationInRange")]
    [InlineData("Piece", "FieldCraftingStation", "m_craftingStation")]
    [InlineData("Piece", "FieldCraftingStationType", "CraftingStation")]
    [InlineData("Piece", "FieldDlc", "m_dlc")]
    [InlineData("Piece", "FieldDlcType", "System.String")]
    [InlineData("Piece", "FieldResources", "m_resources")]
    [InlineData("Piece", "FieldResourcesType", "Piece+Requirement[]")]
    [InlineData("Piece", "FreeBuildKey", "FreeBuildKey")]
    [InlineData("Requirement", "FieldAmount", "m_amount")]
    [InlineData("Requirement", "FieldAmountType", "System.Int32")]
    [InlineData("PieceTable", "TypeName", "PieceTable")]
    [InlineData("PieceTable", "FieldSkill", "m_skill")]
    [InlineData("PieceTable", "FieldSkillType", "Skills+SkillType")]
    [InlineData("ItemData", "FieldDurability", "m_durability")]
    [InlineData("ItemData", "FieldDurabilityType", "System.Single")]
    [InlineData("SharedData", "FieldUseDurability", "m_useDurability")]
    [InlineData("SharedData", "FieldUseDurabilityType", "System.Boolean")]
    [InlineData("SharedData", "FieldBuildEffect", "m_buildEffect")]
    [InlineData("SharedData", "FieldBuildEffectType", "EffectList")]
    [InlineData("Game", "GetInstance", "get_instance")]
    [InlineData("Game", "IncrementPlayerStat", "IncrementPlayerStat")]
    [InlineData("Game", "FieldDurabilityRate", "m_durabilityRate")]
    [InlineData("Game", "FieldDurabilityRateType", "System.Single")]
    [InlineData("EffectList", "TypeName", "EffectList")]
    [InlineData("EffectList", "Create", "Create")]
    [InlineData("Inventory", "CountItems", "CountItems")]
    public void TheBuildingNameHoldsTheContractValue(string owner, string name, string expected)
    {
        Assert.Equal(expected, Constant(owner, name));
    }

    // GW-U-1651 (8.1): the piece overload of HaveRequirements composes with the Piece and RequirementMode argument text.
    [Fact]
    public void PlayerHaveRequirementsPieceComposesFromItsDeclaredParts()
    {
        Assert.Equal("Player.HaveRequirements(Piece,Player+RequirementMode)", HarmonyPatched("PlayerHaveRequirementsPiece"));
    }

    // GW-U-1651 (8.1): the TryPlacePiece patch id composes from the declared method and its piece argument.
    [Fact]
    public void PlayerTryPlacePieceComposesFromItsDeclaredParts()
    {
        Assert.Equal("Player.TryPlacePiece(Piece)", HarmonyPatched("PlayerTryPlacePiece"));
    }

    // GW-U-1651 (8.1): the PlacePiece patch id composes from the five declared parameters of the vanilla overload.
    [Fact]
    public void PlayerPlacePieceComposesFromItsDeclaredParts()
    {
        Assert.Equal("Player.PlacePiece(Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean)", HarmonyPatched("PlayerPlacePiece"));
    }

    // GW-U-1651 (8.1): the existing piece parameter text is unchanged by this story.
    [Fact]
    public void HaveRequirementsParametersPieceIsUnchanged()
    {
        Assert.Equal("Piece,Player+RequirementMode", Constant("Player", "HaveRequirementsParametersPiece"));
    }

    private static string? HarmonyPatched(string name)
    {
        Type? holder = typeof(PatchTargetNames).GetNestedType("HarmonyPatched", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, "PatchTargetNames has no nested class HarmonyPatched.");

        FieldInfo? field = holder!.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(field is not null, $"PatchTargetNames.HarmonyPatched.{name} is missing.");

        return (string?)field!.GetValue(null);
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
