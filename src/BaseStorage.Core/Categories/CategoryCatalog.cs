namespace BaseStorage.Core.Categories;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// The ordered categories of a parsed category file, with the prefab-to-category mapping and the type-fallback resolver.
/// A built catalog is immutable and safe to share across threads.
/// </summary>
public sealed class CategoryCatalog
{
    private static readonly CategoryCatalog EmptyInstance = new CategoryCatalog(new List<CategoryDefinition>());

    private readonly ReadOnlyCollection<CategoryDefinition> categories;
    private readonly ReadOnlyCollection<string> categoryNames;
    private readonly Dictionary<string, int> indexByName = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> indexByNameIgnoreCase = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> categoryByPrefab = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Creates a catalog. Created only by the catalog builder.</summary>
    /// <param name="definitions">The categories in file order.</param>
    internal CategoryCatalog(List<CategoryDefinition> definitions)
    {
        var names = new string[definitions.Count];
        for (int i = 0; i < definitions.Count; i++)
        {
            CategoryDefinition definition = definitions[i];
            names[i] = definition.Name;
            indexByName[definition.Name] = i;
            indexByNameIgnoreCase[definition.Name] = i;
            foreach (string prefab in definition.Prefabs)
            {
                categoryByPrefab[prefab] = definition.Name;
            }
        }

        categories = new ReadOnlyCollection<CategoryDefinition>(definitions.ToArray());
        categoryNames = new ReadOnlyCollection<string>(names);
    }

    /// <summary>Gets a catalog with no categories, in which every item resolves by fallback.</summary>
    public static CategoryCatalog Empty => EmptyInstance;

    /// <summary>Gets the categories in file order (the order of each name's first valid line). The list is read-only.</summary>
    public IReadOnlyList<CategoryDefinition> Categories => categories;

    /// <summary>Gets the category names in the same order as <see cref="Categories"/>. The list is read-only.</summary>
    public IReadOnlyList<string> CategoryNames => categoryNames;

    /// <summary>Gets the zero-based position of the category with exactly this name (ordinal), or -1 when there is none.</summary>
    /// <param name="categoryName">The category name to find; not null.</param>
    /// <returns>The zero-based position, or -1.</returns>
    public int IndexOf(string categoryName)
    {
        if (categoryName is null)
        {
            throw new ArgumentNullException(nameof(categoryName));
        }

        return indexByName.TryGetValue(categoryName, out int index) ? index : -1;
    }

    /// <summary>Gets the category that the file maps this prefab to (ordinal), or null when the prefab is unmapped.</summary>
    /// <param name="prefabName">The prefab name; not null. Empty or blank names return null.</param>
    /// <returns>The category name, or null.</returns>
    public string? GetMappedCategory(string prefabName)
    {
        if (prefabName is null)
        {
            throw new ArgumentNullException(nameof(prefabName));
        }

        return categoryByPrefab.TryGetValue(prefabName, out string? name) ? name : null;
    }

    /// <summary>
    /// Resolves the category of an item. The file mapping wins. Otherwise the fallback for the item kind is used, spelled as
    /// the catalog spells it when the catalog defines it (ignoring case).
    /// </summary>
    /// <param name="prefabName">The prefab name; not null.</param>
    /// <param name="kind">The item kind. Undefined values resolve like <see cref="ItemKind.Other"/>.</param>
    /// <returns>The category name. Never null and never empty.</returns>
    public string Resolve(string prefabName, ItemKind kind)
    {
        string? mapped = GetMappedCategory(prefabName);
        if (mapped != null)
        {
            return mapped;
        }

        string fallback = FallbackCategories.For(kind);
        return indexByNameIgnoreCase.TryGetValue(fallback, out int index) ? categoryNames[index] : fallback;
    }
}
