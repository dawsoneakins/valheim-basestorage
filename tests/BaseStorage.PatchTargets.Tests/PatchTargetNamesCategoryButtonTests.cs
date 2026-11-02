namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.Shared;

/// <summary>
/// GW-U-1330 (S-E004-004 AC-4, contract section 4.7): the nine Category button constants and the one HarmonyPatched id are the contract
/// literals, the classified list grows to 181 rows with the contracted kind counts, and the shape rules still pass. No game install needed.
/// </summary>
public sealed class PatchTargetNamesCategoryButtonTests
{
    // GW-U-1330
    [Theory]
    [InlineData("InventoryGui", "UpdateContainerParametersPlayer", "Player")]
    [InlineData("InventoryGui", "FieldTakeAllButton", "m_takeAllButton")]
    [InlineData("InventoryGui", "FieldTakeAllButtonType", "UnityEngine.UI.Button")]
    [InlineData("InventoryGui", "FieldStackAllButton", "m_stackAllButton")]
    [InlineData("InventoryGui", "FieldStackAllButtonType", "UnityEngine.UI.Button")]
    [InlineData("InventoryGui", "FieldContainer", "m_container")]
    [InlineData("InventoryGui", "FieldContainerType", "UnityEngine.RectTransform")]
    [InlineData("Player", "TakeInput", "TakeInput")]
    [InlineData("Player", "GetHoverObject", "GetHoverObject")]
    [InlineData("HarmonyPatched", "InventoryGuiUpdateContainer", "InventoryGui.UpdateContainer(Player)")]
    public void CategoryButtonConstantsHoldTheContractedValues(string owner, string member, string expected)
    {
        Type? holder = typeof(PatchTargetNames).GetNestedType(owner, BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, $"PatchTargetNames has no nested class {owner}.");
        FieldInfo? field = holder!.GetField(member, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Assert.True(field is not null, $"PatchTargetNames.{owner} has no constant {member}.");

        Assert.True(field!.IsLiteral && field.FieldType == typeof(string));
        Assert.Equal(expected, (string?)field.GetValue(null));
    }

    // GW-U-1330: the new rows classify as contracted and the overload companion turns UpdateContainer into overload mode.
    [Fact]
    public void TheNewRowsClassifyAsContracted()
    {
        var entries = TargetList.Classify(typeof(PatchTargetNames));

        // Edited by integration (S-E003-004..006 + S-E006-002 + S-E004-004 + S-E004-011): the union of every story's rows, 370.
        // Edited by S-E004-012 (ADR-GWIN-136): plus the 15 station-actions rows, 385.
        // Edited by S-E004-015 (ADR-GWIN-143): plus the seven Deposit rows, 392.
        // Edited by S-E010-002 (organize-console-command 6.2): plus the twelve console rows, 404.
        Assert.Equal(419, entries.Count);
        Assert.Equal(TargetKind.MethodOverloads, entries.Single(e => e.Owner == "InventoryGui" && e.Const == "UpdateContainer").Kind);
        Assert.Equal(TargetKind.OverloadCompanion, entries.Single(e => e.Owner == "InventoryGui" && e.Const == "UpdateContainerParametersPlayer").Kind);
        Assert.Equal(TargetKind.FieldEntry, entries.Single(e => e.Owner == "InventoryGui" && e.Const == "FieldTakeAllButton").Kind);
        Assert.Equal(TargetKind.FieldTypeCompanion, entries.Single(e => e.Owner == "InventoryGui" && e.Const == "FieldTakeAllButtonType").Kind);
        Assert.Equal(TargetKind.FieldEntry, entries.Single(e => e.Owner == "InventoryGui" && e.Const == "FieldStackAllButton").Kind);
        Assert.Equal(TargetKind.FieldEntry, entries.Single(e => e.Owner == "InventoryGui" && e.Const == "FieldContainer").Kind);
        Assert.Equal(TargetKind.MethodNameOnly, entries.Single(e => e.Owner == "Player" && e.Const == "TakeInput").Kind);
        Assert.Equal(TargetKind.MethodNameOnly, entries.Single(e => e.Owner == "Player" && e.Const == "GetHoverObject").Kind);
        Assert.Contains(new HarmonyEntry("InventoryGuiUpdateContainer", "InventoryGui.UpdateContainer(Player)"), TargetList.HarmonyPatchedEntries(typeof(PatchTargetNames)));
        Assert.Empty(ShapeRules.Check(typeof(PatchTargetNames)));
    }
}
