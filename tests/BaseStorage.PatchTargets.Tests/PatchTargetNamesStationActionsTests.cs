namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.Shared;

/// <summary>
/// GW-U-1790 (AC-5, contract 6 and 6.1): the S-E004-012 PatchTargetNames rows, read verbatim by reflection over the shared constants.
/// UpdateRecipe is an overload-mode method with its companion, the five InventoryGui fields and UIGamePad.m_hint are fields with
/// their field-type companions, UIGamePad is a type row, the classified list totals 385 and the HarmonyPatched set has 23 ids. No game
/// install needed. Red until the rows are added to src/Shared/PatchTargetNames.cs.
/// </summary>
public sealed class PatchTargetNamesStationActionsTests
{
    private static readonly (string Owner, string Const, string Value)[] SectionSix =
    {
        ("InventoryGui", "UpdateRecipe", "UpdateRecipe"),
        ("InventoryGui", "UpdateRecipeParametersPlayerDeltaTime", "Player,System.Single"),
        ("InventoryGui", "FieldCraftButton", "m_craftButton"),
        ("InventoryGui", "FieldCraftButtonType", "UnityEngine.UI.Button"),
        ("InventoryGui", "FieldRecipeListRoot", "m_recipeListRoot"),
        ("InventoryGui", "FieldRecipeListRootType", "UnityEngine.RectTransform"),
        ("InventoryGui", "FieldRecipeRequirementList", "m_recipeRequirementList"),
        ("InventoryGui", "FieldRecipeRequirementListType", "UnityEngine.GameObject[]"),
        ("InventoryGui", "FieldCraftingStationName", "m_craftingStationName"),
        ("InventoryGui", "FieldCraftingStationNameType", "TMPro.TMP_Text"),
        ("InventoryGui", "FieldCrafting", "m_crafting"),
        ("InventoryGui", "FieldCraftingType", "UnityEngine.RectTransform"),
        ("UIGamePad", "TypeName", "UIGamePad"),
        ("UIGamePad", "FieldHint", "m_hint"),
        ("UIGamePad", "FieldHintType", "UnityEngine.GameObject"),
        ("HarmonyPatched", "InventoryGuiUpdateRecipe", "InventoryGui.UpdateRecipe(Player,System.Single)"),
    };

    private static readonly (string Owner, string Const)[] Fields =
    {
        ("InventoryGui", "FieldCraftButton"),
        ("InventoryGui", "FieldRecipeListRoot"),
        ("InventoryGui", "FieldRecipeRequirementList"),
        ("InventoryGui", "FieldCraftingStationName"),
        ("InventoryGui", "FieldCrafting"),
        ("UIGamePad", "FieldHint"),
    };

    // GW-U-1790 (6): every section-6 value is the contracted literal, read by reflection over PatchTargetNames.
    [Fact]
    public void EverySectionSixValueIsTheContractedLiteral()
    {
        foreach ((string owner, string constant, string value) in SectionSix)
        {
            Assert.Equal(value, ConstantOf(owner, constant));
        }
    }

    // GW-U-1790 (6.1): UpdateRecipe classifies as an overload-mode method with its companion, as the contract requires.
    [Fact]
    public void UpdateRecipeIsMethodOverloadsWithItsCompanion()
    {
        TargetEntry[] entries = TargetList.Classify(typeof(PatchTargetNames)).ToArray();

        Assert.Equal(TargetKind.MethodOverloads, Only(entries, "InventoryGui", "UpdateRecipe").Kind);
        Assert.Equal(TargetKind.OverloadCompanion, Only(entries, "InventoryGui", "UpdateRecipeParametersPlayerDeltaTime").Kind);
    }

    // GW-U-1790 (6.1): the six fields are FieldEntry rows, each with a FieldTypeCompanion row.
    [Fact]
    public void TheSixFieldsAreFieldEntriesWithTypeCompanions()
    {
        TargetEntry[] entries = TargetList.Classify(typeof(PatchTargetNames)).ToArray();

        foreach ((string owner, string constant) in Fields)
        {
            Assert.Equal(TargetKind.FieldEntry, Only(entries, owner, constant).Kind);
            Assert.Equal(TargetKind.FieldTypeCompanion, Only(entries, owner, constant + "Type").Kind);
        }
    }

    // GW-U-1790 (6.1): UIGamePad is a top-level type row, and the shape rules hold for the whole list.
    [Fact]
    public void UIGamePadIsATypeRowAndTheShapeRulesHold()
    {
        TargetEntry[] entries = TargetList.Classify(typeof(PatchTargetNames)).ToArray();

        Assert.Equal(TargetKind.TypeEntry, Only(entries, "UIGamePad", "TypeName").Kind);
        Assert.Empty(ShapeRules.Check(typeof(PatchTargetNames)));
    }

    // GW-U-1790 (6.1): the classified list totals 392, by kind 48, 41, 45, 102, 78 and 78.
    // Edited by S-E004-015 (ADR-GWIN-143): renamed from TheClassifiedTotalsAre385ByKind, plus the seven Deposit rows.
    [Fact]
    public void TheClassifiedTotalsAre392ByKind()
    {
        TargetEntry[] entries = TargetList.Classify(typeof(PatchTargetNames)).ToArray();

        // Edited by S-E010-002 (organize-console-command 6.2): 404, by kind 52, 43, 47, 102, 80 and 80.
        Assert.Equal(419, entries.Length);
        Assert.Equal(54, entries.Count(e => e.Kind == TargetKind.TypeEntry));
        Assert.Equal(50, entries.Count(e => e.Kind == TargetKind.MethodOverloads));
        Assert.Equal(54, entries.Count(e => e.Kind == TargetKind.OverloadCompanion));
        Assert.Equal(101, entries.Count(e => e.Kind == TargetKind.MethodNameOnly));
        Assert.Equal(80, entries.Count(e => e.Kind == TargetKind.FieldEntry));
        Assert.Equal(80, entries.Count(e => e.Kind == TargetKind.FieldTypeCompanion));
    }

    // GW-U-1790 (6.1): the HarmonyPatched set has 23 ids, and the station-actions id is one of them.
    [Fact]
    public void HarmonyPatchedHoldsTheStationActionsIdAmongTwentyThree()
    {
        var harmony = TargetList.HarmonyPatchedEntries(typeof(PatchTargetNames));

        // Edited by S-E010-002 (organize-console-command 6.2): plus TerminalInitTerminal, twenty-four.
        Assert.Equal(31, harmony.Count);
        Assert.Contains(new HarmonyEntry("InventoryGuiUpdateRecipe", "InventoryGui.UpdateRecipe(Player,System.Single)"), harmony);
    }

    private static TargetEntry Only(TargetEntry[] entries, string owner, string constant) =>
        Assert.Single(entries, e => e.Owner == owner && e.Const == constant);

    private static string? ConstantOf(string owner, string constant)
    {
        Type? holder = owner == "HarmonyPatched"
            ? typeof(PatchTargetNames).GetNestedType("HarmonyPatched", BindingFlags.Public | BindingFlags.NonPublic)
            : typeof(PatchTargetNames).GetNestedType(owner, BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, "PatchTargetNames has no nested class " + owner + " (S-E004-012, implement phase).");

        FieldInfo? field = holder!.GetField(constant, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(field is not null, owner + "." + constant + " is not declared (S-E004-012, implement phase).");
        return (string?)field!.GetRawConstantValue();
    }
}
