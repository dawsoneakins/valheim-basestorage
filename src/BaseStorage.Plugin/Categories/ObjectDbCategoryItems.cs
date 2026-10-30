namespace BaseStorage.Plugin.Categories;

using System.Collections.Generic;
using BaseStorage.Core.Categories.Defaults;
using UnityEngine;

/// <summary>Reads the valid category items out of the game item database (contract F4).</summary>
internal static class ObjectDbCategoryItems
{
    /// <summary>Collects one item per valid ObjectDB entry. Entries that are null, have no ItemDrop or have an invalid name are skipped.</summary>
    /// <param name="db">The item database.</param>
    /// <returns>The valid items; empty when there are none.</returns>
    internal static List<CategoryItem> Collect(ObjectDB db)
    {
        var result = new List<CategoryItem>();
        List<GameObject>? entries = db.m_items;
        if (entries is null)
        {
            return result;
        }

        foreach (GameObject go in entries)
        {
            if (go == null)
            {
                continue;
            }

            ItemDrop? itemDrop = go.GetComponent<ItemDrop>();
            if (itemDrop == null || !CategoryItem.IsValidPrefabName(go.name))
            {
                continue;
            }

            result.Add(new CategoryItem(go.name, ItemKindAdapter.From(itemDrop.m_itemData?.m_shared)));
        }

        return result;
    }
}
