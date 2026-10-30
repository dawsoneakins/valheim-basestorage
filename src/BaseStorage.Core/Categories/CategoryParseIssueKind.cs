namespace BaseStorage.Core.Categories;

/// <summary>The kinds of problem that the category file parser reports.</summary>
public enum CategoryParseIssueKind
{
    /// <summary>An error: a non-blank, non-comment line has no colon, so the line is skipped.</summary>
    MissingSeparator = 1,

    /// <summary>An error: the text before the first colon is empty or blank, so the line is skipped.</summary>
    EmptyCategoryName = 2,

    /// <summary>An error: the category name contains a control character, so the line is skipped.</summary>
    InvalidCategoryName = 3,

    /// <summary>An error: the category name is Auto or Keep (ignoring case), so the line is skipped.</summary>
    ReservedCategoryName = 4,

    /// <summary>A warning: the category was already defined, and this line's prefabs merge into the first definition.</summary>
    DuplicateCategory = 5,

    /// <summary>A warning: the prefab was already mapped to a category, and the first mapping is kept.</summary>
    DuplicatePrefab = 6,
}
