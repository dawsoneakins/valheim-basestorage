namespace BaseStorage.Core.Organize;

using System;
using BaseStorage.Core.Categories;

/// <summary>
/// How good a chest is for a stack that has no room in its own category. A Misc chest is the catch-all, so it comes first; after that,
/// the closer two categories sit in the catalog the better, which lets a player steer it by the order of the categories file.
/// </summary>
internal static class CategoryNearness
{
    private const int NoCategory = int.MaxValue;
    private const int NotInCatalog = int.MaxValue - 1;

    /// <summary>Scores a chest for a stack; lower is better.</summary>
    /// <param name="itemCategory">The stack's category.</param>
    /// <param name="chestCategory">The chest's category, or null for a chest without one.</param>
    /// <param name="catalog">The catalog.</param>
    /// <returns>The score.</returns>
    internal static int Of(string itemCategory, string? chestCategory, CategoryCatalog catalog)
    {
        if (chestCategory is null)
        {
            return NoCategory;
        }

        if (string.Equals(chestCategory, FallbackCategories.Misc, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var item = catalog.IndexOf(itemCategory);
        var chest = catalog.IndexOf(chestCategory);
        return item < 0 || chest < 0 ? NotInCatalog : 1 + Math.Abs(item - chest);
    }
}
