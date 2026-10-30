namespace BaseStorage.Core.Categories;

using System;
using BaseStorage.Core.Abstractions;

/// <summary>
/// One problem found while parsing the category file: a line that was skipped, or a duplicate that was merged or dropped.
/// It is a value. Equality compares the line number, kind, trimmed line text and the three names (ordinal). The severity
/// is derived from the kind and is not part of equality.
/// </summary>
public sealed class CategoryParseIssue : IEquatable<CategoryParseIssue>
{
    /// <summary>Creates an issue.</summary>
    /// <param name="lineNumber">The 1-based line number; at least 1.</param>
    /// <param name="kind">The kind of issue; one of the defined values.</param>
    /// <param name="lineText">The trimmed line text; not null.</param>
    /// <param name="categoryName">The category name the issue concerns, or null.</param>
    /// <param name="prefabName">The prefab name the issue concerns, or null.</param>
    /// <param name="keptCategoryName">The category that kept the prefab or the spelling, or null.</param>
    public CategoryParseIssue(
        int lineNumber,
        CategoryParseIssueKind kind,
        string lineText,
        string? categoryName,
        string? prefabName,
        string? keptCategoryName)
    {
        if (lineNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber));
        }

        if (kind < CategoryParseIssueKind.MissingSeparator || kind > CategoryParseIssueKind.DuplicatePrefab)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (lineText is null)
        {
            throw new ArgumentNullException(nameof(lineText));
        }

        LineNumber = lineNumber;
        Kind = kind;
        LineText = lineText;
        CategoryName = categoryName;
        PrefabName = prefabName;
        KeptCategoryName = keptCategoryName;
    }

    /// <summary>Gets the 1-based line number of the problem.</summary>
    public int LineNumber { get; }

    /// <summary>Gets the kind of problem.</summary>
    public CategoryParseIssueKind Kind { get; }

    /// <summary>Gets the severity, derived from the kind: Error for kinds 1 to 4, Warning for kinds 5 and 6.</summary>
    public LogSeverity Severity =>
        Kind <= CategoryParseIssueKind.ReservedCategoryName ? LogSeverity.Error : LogSeverity.Warning;

    /// <summary>Gets the trimmed line text, without the line terminator.</summary>
    public string LineText { get; }

    /// <summary>Gets the category name the issue concerns, or null.</summary>
    public string? CategoryName { get; }

    /// <summary>Gets the prefab name the issue concerns, or null.</summary>
    public string? PrefabName { get; }

    /// <summary>Gets the catalog spelling of the category that kept the prefab or the name, or null.</summary>
    public string? KeptCategoryName { get; }

    /// <summary>Determines whether two issues are equal. Two null values are equal.</summary>
    /// <param name="left">The first issue.</param>
    /// <param name="right">The second issue.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(CategoryParseIssue? left, CategoryParseIssue? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    /// <summary>Determines whether two issues differ.</summary>
    /// <param name="left">The first issue.</param>
    /// <param name="right">The second issue.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(CategoryParseIssue? left, CategoryParseIssue? right) => !(left == right);

    /// <summary>Determines whether another issue has the same line number, kind, line text and names.</summary>
    /// <param name="other">The issue to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(CategoryParseIssue? other)
    {
        return other is not null
            && LineNumber == other.LineNumber
            && Kind == other.Kind
            && string.Equals(LineText, other.LineText, StringComparison.Ordinal)
            && string.Equals(CategoryName, other.CategoryName, StringComparison.Ordinal)
            && string.Equals(PrefabName, other.PrefabName, StringComparison.Ordinal)
            && string.Equals(KeptCategoryName, other.KeptCategoryName, StringComparison.Ordinal);
    }

    /// <summary>Determines whether the object is an equal issue.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as CategoryParseIssue);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = LineNumber;
            hash = (hash * 31) + (int)Kind;
            hash = (hash * 31) + LineText.GetHashCode();
            hash = (hash * 31) + (CategoryName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (PrefabName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (KeptCategoryName?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
