namespace BaseStorage.Core.ChestCategories;

using System;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;

/// <summary>
/// Reads the chest category value stored under the chest category key (contract S-E004-003 section 2.2). No key, the empty
/// string and an undecodable value all read as Auto (<c>null</c>). A decodable name the catalog does not know is returned as
/// stored, so callers can show it and so the stored text survives until the player writes a new value (principles 4 and 5).
/// Every member is pure and never throws for stored data.
/// </summary>
public static class ChestCategoryReader
{
    /// <summary>Decodes the raw stored text. Null, empty and malformed text give Auto.</summary>
    /// <param name="raw">The stored text, or null when the chest has no key.</param>
    /// <returns>The stored assignment, or null for Auto.</returns>
    public static CategoryAssignment? Read(string? raw) => string.IsNullOrEmpty(raw) ? null : CategoryCodec.Decode(raw);

    /// <summary>Tells whether a value is one the cycle knows: Auto, Keep or a category of the catalog.</summary>
    /// <param name="value">The value; null for Auto.</param>
    /// <param name="catalog">The catalog; must not be null.</param>
    /// <returns>True for Auto, Keep and every catalog category; false for a name the catalog does not define.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is null.</exception>
    public static bool IsKnown(CategoryAssignment? value, CategoryCatalog catalog)
    {
        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        return value is null || value.IsKeep || catalog.IndexOf(value.CategoryName!) >= 0;
    }

    /// <summary>Gets the value the mod acts on: the stored value when the catalog knows it, otherwise Auto.</summary>
    /// <param name="value">The stored value; null for Auto.</param>
    /// <param name="catalog">The catalog; must not be null.</param>
    /// <returns>The value itself, or null when it names a category the catalog does not define.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is null.</exception>
    public static CategoryAssignment? Effective(CategoryAssignment? value, CategoryCatalog catalog) =>
        IsKnown(value, catalog) ? value : null;

    /// <summary>Gets the text written to the chest for a value: the codec text, or the empty string for Auto.</summary>
    /// <param name="value">The value; null for Auto.</param>
    /// <returns>The text to store; the empty string clears the value (reads as Auto).</returns>
    public static string StoredValue(CategoryAssignment? value) => value is null ? string.Empty : CategoryCodec.Encode(value);

    /// <summary>Tells whether writing a value would change what the chest stores.</summary>
    /// <param name="desired">The value to write; null for Auto.</param>
    /// <param name="raw">The stored text, or null when the chest has no key.</param>
    /// <returns>False when the stored text already equals the text that would be written (Auto also matches no key).</returns>
    public static bool WouldChange(CategoryAssignment? desired, string? raw)
    {
        if (desired is null)
        {
            return !string.IsNullOrEmpty(raw);
        }

        return !string.Equals(StoredValue(desired), raw, StringComparison.Ordinal);
    }

    /// <summary>Gets the display label of a value from the strings table: Auto, Keep or the category name.</summary>
    /// <param name="value">The value; null for Auto.</param>
    /// <param name="strings">The strings table; must not be null.</param>
    /// <returns>The label.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="strings"/> is null.</exception>
    public static string Label(CategoryAssignment? value, StringTable strings)
    {
        if (strings is null)
        {
            throw new ArgumentNullException(nameof(strings));
        }

        if (value is null)
        {
            return strings.Get(StringKeys.ChestCategoryAuto);
        }

        return value.IsKeep ? strings.Get(StringKeys.ChestCategoryKeep) : value.CategoryName!;
    }
}
