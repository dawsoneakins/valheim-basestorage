namespace BaseStorage.PatchTargets.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.Shared;

/// <summary>
/// GW-U-2622 (AC-17, S-QR-002 contract 9): the eleven new PatchTargetNames rows, read verbatim by reflection, each field with its type
/// companion, and the shape rules still pass. No game install needed. Red until the rows are added to src/Shared/PatchTargetNames.cs.
/// </summary>
public sealed class PatchTargetNamesRestockTests
{
    /// <summary>GW-U-2622: the eleven values are verbatim, every field has its type companion, and the shape rules report nothing.</summary>
    [Fact]
    public void TheElevenRestockValuesAreVerbatim()
    {
        Type itemData = typeof(PatchTargetNames).GetNestedType("ItemData")!;
        Type inventory = typeof(PatchTargetNames).GetNestedType("Inventory")!;
        var expected = new List<(Type Owner, string Name, string Value)>
        {
            (itemData, "FieldCrafterId", "m_crafterID"),
            (itemData, "FieldCrafterIdType", "System.Int64"),
            (itemData, "FieldCrafterName", "m_crafterName"),
            (itemData, "FieldCrafterNameType", "System.String"),
            (itemData, "FieldVariant", "m_variant"),
            (itemData, "FieldVariantType", "System.Int32"),
            (itemData, "FieldWorldLevel", "m_worldLevel"),
            (itemData, "FieldWorldLevelType", "System.Int32"),
            (itemData, "FieldCustomData", "m_customData"),
            (itemData, "FieldCustomDataType", "System.Collections.Generic.Dictionary`2[System.String,System.String]"),
            (inventory, "AddItemParametersItemPosition", "ItemDrop+ItemData,Vector2i"),
        };

        var missing = new List<string>();
        foreach ((Type owner, string name, string value) in expected)
        {
            FieldInfo? field = owner.GetField(name, BindingFlags.Public | BindingFlags.Static);
            if (field is null)
            {
                missing.Add(owner.Name + "." + name + " is missing");
                continue;
            }

            Assert.Equal(value, field.GetValue(null));
        }

        Assert.True(missing.Count == 0, string.Join("; ", missing));
        Assert.Empty(ShapeRules.Check(typeof(PatchTargetNames)));
    }
}
