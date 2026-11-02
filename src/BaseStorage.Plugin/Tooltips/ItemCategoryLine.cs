namespace BaseStorage.Plugin.Tooltips;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Categories;
using BaseStorage.Plugin.CategoryButton;
using BaseStorage.Plugin.Favourites;

/// <summary>
/// The tooltip's category line, from the same catalog Organize uses. A tooltip can be asked for every frame, so each prefab's line
/// is built once and the cache is dropped when the catalog changes. Main thread only.
/// </summary>
internal static class ItemCategoryLine
{
    private static readonly Dictionary<string, string> Lines = new Dictionary<string, string>(StringComparer.Ordinal);
    private static CategoryCatalog? builtFor;
    private static BaseStorageSettings? settings;

    /// <summary>Stores the settings the line reads.</summary>
    /// <param name="current">The settings.</param>
    internal static void Initialize(BaseStorageSettings current) => settings = current;

    /// <summary>Appends the category line to a tooltip, or returns it unchanged when the setting is off or the item has no prefab name.</summary>
    /// <param name="item">The item.</param>
    /// <param name="text">The vanilla tooltip.</param>
    /// <returns>The tooltip text.</returns>
    internal static string Append(ItemDrop.ItemData item, string text)
    {
        if (item != null && FavouriteSession.IsFavourite(item))
        {
            text += "\n" + StringTable.English.Get(StringKeys.TooltipFavourite);
        }

        BaseStorageSettings? current = settings;
        if (current == null || !current.ShowCategoryTooltip || item == null)
        {
            return text;
        }

        CategoryCatalog catalog = PluginCategoryCatalog.Current;
        if (!ReferenceEquals(catalog, builtFor))
        {
            Lines.Clear();
            builtFor = catalog;
        }

        string prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared.m_name;
        if (!Lines.TryGetValue(prefab, out string? line))
        {
            line = "\n" + StringTable.English.Format(StringKeys.TooltipCategory, catalog.Resolve(prefab, KindOf(item)));
            Lines[prefab] = line;
        }

        return text + line;
    }

    private static ItemKind KindOf(ItemDrop.ItemData item) => ItemKindAdapter.From(item.m_shared);
}
