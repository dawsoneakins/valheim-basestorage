namespace BaseStorage.Core.ChestCategories;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Persistence;

/// <summary>
/// The category cycle of spec AC-4.3 (contract S-E004-003 section 2.1): Auto, then every catalog category in catalog order, then
/// Keep, wrapping around in both directions. Auto is <c>null</c>. A stored name that the catalog does not know cycles as Auto;
/// the stored text itself is left to the writer. Every member is pure.
/// </summary>
public static class CategoryCycle
{
    /// <summary>Lists the cycle in order: Auto (null), each catalog category, then Keep.</summary>
    /// <param name="catalog">The catalog; must not be null.</param>
    /// <returns>A read-only list with <c>catalog.CategoryNames.Count + 2</c> entries.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is null.</exception>
    public static IReadOnlyList<CategoryAssignment?> Order(CategoryCatalog catalog)
    {
        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        IReadOnlyList<string> names = catalog.CategoryNames;
        var order = new CategoryAssignment?[names.Count + 2];
        order[0] = null;
        for (int i = 0; i < names.Count; i++)
        {
            order[i + 1] = CategoryAssignment.ForCategory(names[i]);
        }

        order[names.Count + 1] = CategoryAssignment.Keep;
        return new ReadOnlyCollection<CategoryAssignment?>(order);
    }

    /// <summary>Gets the zero-based position of a value in the cycle. Auto and any name the catalog does not know are position 0.</summary>
    /// <param name="current">The current value; null for Auto.</param>
    /// <param name="catalog">The catalog; must not be null.</param>
    /// <returns>0 for Auto or an unknown name, 1 to N for a catalog category, N + 1 for Keep.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is null.</exception>
    public static int IndexOf(CategoryAssignment? current, CategoryCatalog catalog)
    {
        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (current is null)
        {
            return 0;
        }

        if (current.IsKeep)
        {
            return catalog.CategoryNames.Count + 1;
        }

        int index = catalog.IndexOf(current.CategoryName!);
        return index < 0 ? 0 : index + 1;
    }

    /// <summary>Gets the value after the current one, wrapping from Keep to Auto.</summary>
    /// <param name="current">The current value; null for Auto.</param>
    /// <param name="catalog">The catalog; must not be null.</param>
    /// <returns>The next value; null means Auto.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is null.</exception>
    public static CategoryAssignment? Next(CategoryAssignment? current, CategoryCatalog catalog) => Step(current, catalog, 1);

    /// <summary>Gets the value before the current one, wrapping from Auto to Keep.</summary>
    /// <param name="current">The current value; null for Auto.</param>
    /// <param name="catalog">The catalog; must not be null.</param>
    /// <returns>The previous value; null means Auto.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is null.</exception>
    public static CategoryAssignment? Previous(CategoryAssignment? current, CategoryCatalog catalog) => Step(current, catalog, -1);

    private static CategoryAssignment? Step(CategoryAssignment? current, CategoryCatalog catalog, int delta)
    {
        int index = IndexOf(current, catalog);
        int count = catalog.CategoryNames.Count + 2;
        int target = (((index + delta) % count) + count) % count;
        if (target == 0)
        {
            return null;
        }

        return target == count - 1 ? CategoryAssignment.Keep : CategoryAssignment.ForCategory(catalog.CategoryNames[target - 1]);
    }
}
