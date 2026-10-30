namespace BaseStorage.Core.Categories;

using System.Collections.Generic;

/// <summary>One category of the parsed category file: its name and the prefabs it holds, in file order.</summary>
public sealed class CategoryDefinition
{
    /// <summary>Creates a definition. Created only by the catalog builder.</summary>
    /// <param name="name">The category name, spelled as the file first spelled it.</param>
    /// <param name="prefabs">The prefab names, in file order, without duplicates.</param>
    internal CategoryDefinition(string name, IReadOnlyList<string> prefabs)
    {
        Name = name;
        Prefabs = prefabs;
    }

    /// <summary>Gets the category name, spelled as the file first spelled it. It satisfies name rule N.</summary>
    public string Name { get; }

    /// <summary>Gets the prefab names of this category, in file order and without duplicates. The list is read-only.</summary>
    public IReadOnlyList<string> Prefabs { get; }
}
