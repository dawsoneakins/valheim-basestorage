namespace BaseStorage.Core.Persistence;

using System;

/// <summary>The value stored on a chest: a category name, or Keep. Auto is the absence of the key.</summary>
public sealed class CategoryAssignment : IEquatable<CategoryAssignment>
{
    private static readonly CategoryAssignment KeepValue = new CategoryAssignment(null);

    private readonly string? name;

    private CategoryAssignment(string? name)
    {
        this.name = name;
    }

    /// <summary>Gets the Keep assignment.</summary>
    public static CategoryAssignment Keep => KeepValue;

    /// <summary>Gets a value indicating whether this is Keep.</summary>
    public bool IsKeep => name is null;

    /// <summary>Gets the category name, or null exactly when this is Keep.</summary>
    public string? CategoryName => name;

    /// <summary>Creates an assignment to a named category.</summary>
    /// <param name="categoryName">The category name; not blank, no control characters.</param>
    /// <returns>The assignment.</returns>
    public static CategoryAssignment ForCategory(string categoryName)
    {
        NameRule.Require(categoryName, nameof(categoryName));
        return new CategoryAssignment(categoryName);
    }

    /// <summary>Determines whether both assignments are Keep or name the same category (ordinal).</summary>
    /// <param name="other">The assignment to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(CategoryAssignment? other) =>
        other is not null && string.Equals(name, other.name, StringComparison.Ordinal);

    /// <summary>Determines whether the object is an equal assignment.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as CategoryAssignment);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => name is null ? 0 : StringComparer.Ordinal.GetHashCode(name);

    /// <summary>Determines whether two assignments are equal.</summary>
    /// <param name="left">The first assignment.</param>
    /// <param name="right">The second assignment.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(CategoryAssignment? left, CategoryAssignment? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two assignments differ.</summary>
    /// <param name="left">The first assignment.</param>
    /// <param name="right">The second assignment.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(CategoryAssignment? left, CategoryAssignment? right) => !(left == right);
}
