namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.Shared;

/// <summary>
/// GW-U-1930 (AC-5, deposit-button contract 7): the seven S-E004-015 PatchTargetNames rows, read verbatim by reflection over the shared
/// constants. Three fields carry their type companions, Humanoid.IsItemEquiped is a name-only method, and the shape rules still pass. No
/// game install needed. Red until the rows are added to src/Shared/PatchTargetNames.cs.
/// </summary>
public sealed class PatchTargetNamesDepositTests
{
    private static readonly (string Owner, string Const, string Value)[] DepositRows =
    {
        ("ItemData", "FieldEquipped", "m_equipped"),
        ("ItemData", "FieldEquippedType", "System.Boolean"),
        ("ItemData", "FieldGridPos", "m_gridPos"),
        ("ItemData", "FieldGridPosType", "Vector2i"),
        ("SharedData", "FieldItemType", "m_itemType"),
        ("SharedData", "FieldItemTypeType", "ItemDrop+ItemData+ItemType"),
        ("Humanoid", "IsItemEquiped", "IsItemEquiped"),
    };

    // GW-U-1930 (7.1): the seven values, verbatim, on the owner classes the contract names.
    [Fact]
    public void TheSevenDepositValuesAreVerbatim()
    {
        foreach ((string owner, string constant, string value) in DepositRows)
        {
            Type? ownerType = typeof(PatchTargetNames).GetNestedType(owner, BindingFlags.Public);
            Assert.NotNull(ownerType);
            FieldInfo? field = ownerType!.GetField(constant, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            Assert.Equal(value, (string?)field!.GetValue(null));
        }
    }

    // GW-U-1930 (7.1): the three fields are field entries with their type companions, and the Humanoid method is name-only.
    [Fact]
    public void TheDepositRowsClassifyAsTheContract()
    {
        TargetEntry[] entries = TargetList.Classify(typeof(PatchTargetNames)).ToArray();

        Assert.Equal(TargetKind.FieldEntry, Only(entries, "ItemData", "FieldEquipped").Kind);
        Assert.Equal(TargetKind.FieldTypeCompanion, Only(entries, "ItemData", "FieldEquippedType").Kind);
        Assert.Equal(TargetKind.FieldEntry, Only(entries, "ItemData", "FieldGridPos").Kind);
        Assert.Equal(TargetKind.FieldTypeCompanion, Only(entries, "ItemData", "FieldGridPosType").Kind);
        Assert.Equal(TargetKind.FieldEntry, Only(entries, "SharedData", "FieldItemType").Kind);
        Assert.Equal(TargetKind.FieldTypeCompanion, Only(entries, "SharedData", "FieldItemTypeType").Kind);
        Assert.Equal(TargetKind.MethodNameOnly, Only(entries, "Humanoid", "IsItemEquiped").Kind);
    }

    // GW-U-1930 (7.1): the Deposit rows add no shape violation.
    [Fact]
    public void TheShapeRulesStillPassWithTheDepositRows()
    {
        Assert.Empty(ShapeRules.Check(typeof(PatchTargetNames)));
    }

    private static TargetEntry Only(TargetEntry[] entries, string owner, string constant) =>
        Assert.Single(entries, entry => entry.Owner == owner && entry.Const == constant);
}
