namespace BaseStorage.Core.Categories;

using System.Collections.Generic;

/// <summary>The outcome of parsing the category file: the catalog and the ordered problems found on the way.</summary>
public sealed class CategoryParseResult
{
    /// <summary>Creates a result. Created only by <see cref="CategoryFile.Parse"/>.</summary>
    /// <param name="catalog">The built catalog.</param>
    /// <param name="issues">The ordered issues, already read-only.</param>
    internal CategoryParseResult(CategoryCatalog catalog, IReadOnlyList<CategoryParseIssue> issues)
    {
        Catalog = catalog;
        Issues = issues;
    }

    /// <summary>Gets the catalog built from the valid lines. Never null.</summary>
    public CategoryCatalog Catalog { get; }

    /// <summary>Gets the problems in line order. Never null, and read-only.</summary>
    public IReadOnlyList<CategoryParseIssue> Issues { get; }
}
