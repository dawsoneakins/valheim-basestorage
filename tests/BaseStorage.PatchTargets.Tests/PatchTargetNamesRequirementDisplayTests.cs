namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.Shared;

/// <summary>
/// GW-U-1540 and GW-U-1541 (AC-6; contract 8.1 and CD8): every requirement-display constant of contract 8.1 holds its verbatim value,
/// the two new owners are declared before HarmonyPatched, and no HarmonyPatched value targets the piece check or placement.
/// No ValheimDir is needed. Constants are read by reflection so that a missing constant fails its own row (ADR-GWIN-127).
/// </summary>
public sealed class PatchTargetNamesRequirementDisplayTests
{
    // GW-U-1540 (8.1): every value of contract 8.1, verbatim.
    [Theory]
    [InlineData("InventoryGui", "SetupRequirement", "SetupRequirement")]
    [InlineData("InventoryGui", "SetupRequirementParametersElement", "UnityEngine.Transform,Piece+Requirement,Player,System.Boolean,System.Int32,System.Int32")]
    [InlineData("InventoryGui", "SetupRequirementList", "SetupRequirementList")]
    [InlineData("InventoryGui", "SetupRequirementListParametersQuality", "System.Int32,Player,System.Boolean,System.Int32")]
    [InlineData("InventoryGui", "FieldSelectedRecipe", "m_selectedRecipe")]
    [InlineData("InventoryGui", "FieldSelectedRecipeType", "InventoryGui+RecipeDataPair")]
    [InlineData("RecipeDataPair", "TypeName", "InventoryGui+RecipeDataPair")]
    [InlineData("RecipeDataPair", "GetRecipe", "get_Recipe")]
    [InlineData("Hud", "TypeName", "Hud")]
    [InlineData("Hud", "SetupPieceInfo", "SetupPieceInfo")]
    [InlineData("Hud", "SetupPieceInfoParametersPiece", "Piece")]
    [InlineData("HarmonyPatched", "InventoryGuiSetupRequirement", "InventoryGui.SetupRequirement(UnityEngine.Transform,Piece+Requirement,Player,System.Boolean,System.Int32,System.Int32)")]
    [InlineData("HarmonyPatched", "InventoryGuiSetupRequirementList", "InventoryGui.SetupRequirementList(System.Int32,Player,System.Boolean,System.Int32)")]
    [InlineData("HarmonyPatched", "HudSetupPieceInfo", "Hud.SetupPieceInfo(Piece)")]
    public void TheRequirementDisplayNameHoldsTheContractValue(string owner, string name, string expected)
    {
        Assert.Equal(expected, Constant(owner, name));
    }

    // GW-U-1540 (8.1): the new owners RecipeDataPair and Hud are declared before HarmonyPatched.
    [Fact]
    public void TheNewOwnersAreDeclaredBeforeHarmonyPatched()
    {
        string[] order = typeof(PatchTargetNames)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Select(type => type.Name)
            .ToArray();

        int recipe = Array.IndexOf(order, "RecipeDataPair");
        int hud = Array.IndexOf(order, "Hud");
        int harmony = Array.IndexOf(order, "HarmonyPatched");

        Assert.True(recipe >= 0 && hud >= 0 && harmony >= 0, "RecipeDataPair, Hud and HarmonyPatched must all be declared.");
        Assert.True(recipe < harmony && hud < harmony, "RecipeDataPair and Hud must be declared before HarmonyPatched.");
    }

    // GW-U-1541 (CD8, 8.2): no HarmonyPatched value starts with Player.HaveRequirements(, Player.UpdatePlacement( or Piece.
    [Fact]
    public void NoHarmonyPatchedValueTargetsThePieceCheckOrPlacement()
    {
        Assert.NotNull(Constant("HarmonyPatched", "HudSetupPieceInfo"));

        Type? holder = typeof(PatchTargetNames).GetNestedType("HarmonyPatched", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, "PatchTargetNames has no nested class HarmonyPatched.");

        // Edited by integration (S-E003-005 + S-E003-006): contract CD8 names S-E003-006 as the owner of the build checks and placement,
        // so its three build-from-base ids (contract build-from-base 8.1) are outside this check.
        string[] buildFromBase = { "PlayerHaveRequirementsPiece", "PlayerTryPlacePiece", "PlayerPlacePiece" };
        string[] values = holder!
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(field => !buildFromBase.Contains(field.Name))
            .Select(field => (string?)field.GetValue(null) ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(
            values,
            value => value.StartsWith("Player.HaveRequirements(", StringComparison.Ordinal)
                || value.StartsWith("Player.UpdatePlacement(", StringComparison.Ordinal)
                || value.StartsWith("Piece.", StringComparison.Ordinal));
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
