namespace BaseStorage.Core.Categories.Defaults;

using System;
using BaseStorage.Core.Categories;

/// <summary>
/// One item of the running game's item database as the category file generator sees it: its prefab name and its kind
/// (contract section 2). Value equality uses the stored kind, so an undefined kind is not equal to <see cref="ItemKind.Other"/>.
/// </summary>
public sealed class CategoryItem : IEquatable<CategoryItem>
{
    private readonly string prefabName;
    private readonly ItemKind kind;

    /// <summary>Creates an item. The prefab name must satisfy rule P; the kind is stored as given.</summary>
    /// <param name="prefabName">The prefab name, exactly as given. Must satisfy rule P.</param>
    /// <param name="kind">The item kind, stored as given. An undefined value is accepted.</param>
    /// <exception cref="ArgumentNullException">prefabName is null.</exception>
    /// <exception cref="ArgumentException">prefabName does not satisfy rule P.</exception>
    public CategoryItem(string prefabName, ItemKind kind)
    {
        if (prefabName is null)
        {
            throw new ArgumentNullException(nameof(prefabName));
        }

        if (!IsValidPrefabName(prefabName))
        {
            throw new ArgumentException("The prefab name must be non-empty, free of control characters and commas, and have no leading or trailing white space.", nameof(prefabName));
        }

        this.prefabName = prefabName;
        this.kind = kind;
    }

    /// <summary>The prefab name, exactly as given. Satisfies rule P.</summary>
    public string PrefabName => prefabName;

    /// <summary>The item kind, exactly as given. An undefined value is stored as is.</summary>
    public ItemKind Kind => kind;

    /// <summary>Tells whether a prefab name satisfies rule P. Never throws.</summary>
    /// <param name="prefabName">The candidate prefab name, or null.</param>
    /// <returns>True when prefabName is non-null, non-empty, free of control characters and commas, and has no leading or trailing white space.</returns>
    public static bool IsValidPrefabName(string? prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            return false;
        }

        string value = prefabName!;
        if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1]))
        {
            return false;
        }

        foreach (char c in value)
        {
            if (c == ',' || char.IsControl(c))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Value equality: the prefab names are ordinal-equal and the kinds are numerically equal.</summary>
    /// <param name="other">The item to compare with, or null.</param>
    /// <returns>True when other is equal to this item.</returns>
    public bool Equals(CategoryItem? other)
    {
        return !(other is null) && string.Equals(prefabName, other.prefabName, StringComparison.Ordinal) && (int)kind == (int)other.kind;
    }

    /// <summary>Value equality for object comparisons.</summary>
    /// <param name="obj">The object to compare with, or null.</param>
    /// <returns>True when obj is a <see cref="CategoryItem"/> equal to this item.</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as CategoryItem);
    }

    /// <summary>The hash code, consistent with <see cref="Equals(CategoryItem?)"/>.</summary>
    /// <returns>A hash code for this item, consistent with the equality members.</returns>
    public override int GetHashCode()
    {
        return unchecked((StringComparer.Ordinal.GetHashCode(prefabName) * 31) + (int)kind);
    }

    /// <summary>Value equality operator. Two null references are equal.</summary>
    /// <param name="left">The left operand, or null.</param>
    /// <param name="right">The right operand, or null.</param>
    /// <returns>True when the items are equal.</returns>
    public static bool operator ==(CategoryItem? left, CategoryItem? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    /// <summary>Value inequality operator.</summary>
    /// <param name="left">The left operand, or null.</param>
    /// <param name="right">The right operand, or null.</param>
    /// <returns>True when the items are not equal.</returns>
    public static bool operator !=(CategoryItem? left, CategoryItem? right)
    {
        return !(left == right);
    }
}
