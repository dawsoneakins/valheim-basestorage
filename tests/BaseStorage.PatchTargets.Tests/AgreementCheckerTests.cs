namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using BaseStorage.PatchTargets.Tests.Checking;

/// <summary>
/// AC-3: the agreement rule (contract 4.7.3, error-codes.md BST2020 to BST2022) on pure data. Unconditional.
/// </summary>
public sealed class AgreementCheckerTests
{
    private const string Plugin = @"C:\p\BaseStorage.dll";
    private const string RecipeId = "Player.HaveRequirements(Recipe,System.Boolean,System.Int32,System.Int32)";
    private const string PieceId = "Player.HaveRequirements(Piece,Player+RequirementMode)";
    private const string RecipeClass = "BaseStorage.Plugin.Patches.PlayerHaveRequirementsRecipePatch";
    private const string PieceClass = "BaseStorage.Plugin.Patches.PlayerHaveRequirementsPiecePatch";

    private static readonly HarmonyEntry RecipeEntry = new("PlayerHaveRequirementsRecipe", RecipeId);
    private static readonly HarmonyEntry PieceEntry = new("PlayerHaveRequirementsPiece", PieceId);

    // GW-U-100
    [Fact]
    public void EmptyPluginAndEmptyListAgree()
    {
        Assert.Equal(string.Empty, AgreementChecker.Compare(Array.Empty<PatchClass>(), Array.Empty<HarmonyEntry>(), Plugin));
    }

    // GW-U-100
    [Fact]
    public void RecipePatchWithTheRecipeEntryPasses()
    {
        Assert.Equal(string.Empty, AgreementChecker.Compare(new[] { Patch(RecipeClass, RecipeId) }, new[] { RecipeEntry }, Plugin));
    }

    // GW-U-100
    [Fact]
    public void RecipePatchWithAnEmptyListGivesTheBst2020RecipeLine()
    {
        Assert.Equal(
            "BST2020: Plugin patch class 'BaseStorage.Plugin.Patches.PlayerHaveRequirementsRecipePatch' targets 'Player.HaveRequirements(Recipe,System.Boolean,System.Int32,System.Int32)', which is not listed in PatchTargetNames.HarmonyPatched.",
            AgreementChecker.Compare(new[] { Patch(RecipeClass, RecipeId) }, Array.Empty<HarmonyEntry>(), Plugin));
    }

    // GW-U-100
    [Fact]
    public void NoPatchWithTheRecipeEntryGivesTheBst2021RecipeLine()
    {
        Assert.Equal(
            @"BST2021: PatchTargetNames.HarmonyPatched.PlayerHaveRequirementsRecipe = 'Player.HaveRequirements(Recipe,System.Boolean,System.Int32,System.Int32)' has no matching Harmony patch in 'C:\p\BaseStorage.dll'.",
            AgreementChecker.Compare(Array.Empty<PatchClass>(), new[] { RecipeEntry }, Plugin));
    }

    // GW-U-100
    [Fact]
    public void PiecePatchWithOnlyTheRecipeEntryGivesOneLineEach()
    {
        string expected =
            "BST2020: Plugin patch class 'BaseStorage.Plugin.Patches.PlayerHaveRequirementsPiecePatch' targets 'Player.HaveRequirements(Piece,Player+RequirementMode)', which is not listed in PatchTargetNames.HarmonyPatched.\n"
            + @"BST2021: PatchTargetNames.HarmonyPatched.PlayerHaveRequirementsRecipe = 'Player.HaveRequirements(Recipe,System.Boolean,System.Int32,System.Int32)' has no matching Harmony patch in 'C:\p\BaseStorage.dll'.";

        Assert.Equal(expected, AgreementChecker.Compare(new[] { Patch(PieceClass, PieceId) }, new[] { RecipeEntry }, Plugin));
    }

    // GW-U-100
    [Fact]
    public void BothPatchesWithBothEntriesPass()
    {
        Assert.Equal(
            string.Empty,
            AgreementChecker.Compare(
                new[] { Patch(RecipeClass, RecipeId), Patch(PieceClass, PieceId) },
                new[] { RecipeEntry, PieceEntry },
                Plugin));
    }

    // GW-U-100
    [Fact]
    public void TwoClassesWithOneUnlistedIdGiveTwoBst2020Lines()
    {
        string expected =
            "BST2020: Plugin patch class 'BaseStorage.Plugin.Patches.PlayerHaveRequirementsPiecePatch' targets 'Player.HaveRequirements(Piece,Player+RequirementMode)', which is not listed in PatchTargetNames.HarmonyPatched.\n"
            + "BST2020: Plugin patch class 'BaseStorage.Plugin.Patches.PlayerHaveRequirementsRecipePatch' targets 'Player.HaveRequirements(Piece,Player+RequirementMode)', which is not listed in PatchTargetNames.HarmonyPatched.";

        Assert.Equal(
            expected,
            AgreementChecker.Compare(new[] { Patch(RecipeClass, PieceId), Patch(PieceClass, PieceId) }, Array.Empty<HarmonyEntry>(), Plugin));
    }

    // GW-U-100
    [Fact]
    public void DuplicateIdsCollapseForTheComparison()
    {
        Assert.Equal(string.Empty, AgreementChecker.Compare(new[] { Patch(RecipeClass, RecipeId, RecipeId) }, new[] { RecipeEntry }, Plugin));
        Assert.Equal(string.Empty, AgreementChecker.Compare(new[] { Patch(RecipeClass, RecipeId), Patch(PieceClass, RecipeId) }, new[] { RecipeEntry }, Plugin));
    }

    // GW-U-100
    [Fact]
    public void AnUnresolvedTargetFailsEvenWhenTheSetsAgree()
    {
        const string bst2022 = "BST2022: Plugin patch class 'BaseStorage.Plugin.Patches.PlayerHaveRequirementsRecipePatch' has a Harmony target this check cannot resolve: method name is not set.";
        PatchClass unresolved = new(RecipeClass, new[] { RecipeId }, new[] { bst2022 });

        Assert.Equal(bst2022, AgreementChecker.Compare(new[] { unresolved }, new[] { RecipeEntry }, Plugin));
    }

    private static PatchClass Patch(string className, params string[] ids) =>
        new(className, ids, Array.Empty<string>());
}
