namespace BaseStorage.Core.Categories;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Collects the categories and the prefab mappings of one parse, then builds the immutable catalog. One builder is used
/// by one <see cref="CategoryFile.Parse"/> call and is never shared.
/// </summary>
internal sealed class CategoryCatalogBuilder
{
    private readonly List<string> names = new List<string>();
    private readonly List<List<string>> prefabLists = new List<List<string>>();
    private readonly Dictionary<string, int> indexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> ownerByPrefab = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Creates an empty builder.</summary>
    internal CategoryCatalogBuilder()
    {
    }

    /// <summary>Finds a category by name, ignoring case.</summary>
    /// <param name="name">The category name.</param>
    /// <returns>The category index, or -1.</returns>
    internal int FindCategory(string name)
    {
        return indexByName.TryGetValue(name, out int index) ? index : -1;
    }

    /// <summary>Adds a new category at the end.</summary>
    /// <param name="name">The category name, spelled as first written.</param>
    /// <returns>The index of the new category.</returns>
    internal int AddCategory(string name)
    {
        int index = names.Count;
        names.Add(name);
        prefabLists.Add(new List<string>());
        indexByName.Add(name, index);
        return index;
    }

    /// <summary>Gets the kept spelling of a category.</summary>
    /// <param name="index">The category index.</param>
    /// <returns>The category name.</returns>
    internal string NameAt(int index) => names[index];

    /// <summary>Finds the category that owns a prefab.</summary>
    /// <param name="prefab">The prefab name.</param>
    /// <returns>The category index, or -1.</returns>
    internal int FindOwner(string prefab)
    {
        return ownerByPrefab.TryGetValue(prefab, out int index) ? index : -1;
    }

    /// <summary>Maps a prefab, not yet mapped, to a category.</summary>
    /// <param name="index">The category index.</param>
    /// <param name="prefab">The prefab name.</param>
    internal void AddPrefab(int index, string prefab)
    {
        ownerByPrefab.Add(prefab, index);
        prefabLists[index].Add(prefab);
    }

    /// <summary>Builds the immutable catalog.</summary>
    /// <returns>The catalog.</returns>
    internal CategoryCatalog Build()
    {
        var definitions = new List<CategoryDefinition>(names.Count);
        for (int i = 0; i < names.Count; i++)
        {
            definitions.Add(new CategoryDefinition(names[i], new ReadOnlyCollection<string>(prefabLists[i].ToArray())));
        }

        return new CategoryCatalog(definitions);
    }
}
