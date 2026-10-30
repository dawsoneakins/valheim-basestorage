namespace BaseStorage.Core.Categories;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using BaseStorage.Core.Persistence;

/// <summary>
/// Parses the text of the category file (BaseStorage.categories.txt). Parsing is a pure function of its input: it does no I/O,
/// logs nothing and reads no clock, and it never throws for non-null text. Bad lines are reported as issues.
/// </summary>
public static class CategoryFile
{
    private const string ReservedAuto = "Auto";
    private const string ReservedKeep = "Keep";

    /// <summary>Parses the full text of the category file.</summary>
    /// <param name="text">The file text, with any line endings; not null.</param>
    /// <returns>The catalog and the ordered issues. Never null.</returns>
    public static CategoryParseResult Parse(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text.Substring(1);
        }

        var builder = new CategoryCatalogBuilder();
        var issues = new List<CategoryParseIssue>();
        using (var reader = new StringReader(text))
        {
            int lineNumber = 0;
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                ParseLine(builder, issues, lineNumber, line.Trim());
            }
        }

        return new CategoryParseResult(builder.Build(), new ReadOnlyCollection<CategoryParseIssue>(issues));
    }

    private static void ParseLine(CategoryCatalogBuilder builder, List<CategoryParseIssue> issues, int lineNumber, string t)
    {
        if (t.Length == 0 || t[0] == '#')
        {
            return;
        }

        int colon = t.IndexOf(':');
        if (colon < 0)
        {
            issues.Add(new CategoryParseIssue(lineNumber, CategoryParseIssueKind.MissingSeparator, t, null, null, null));
            return;
        }

        string name = t.Substring(0, colon).Trim();
        if (name.Length == 0)
        {
            issues.Add(new CategoryParseIssue(lineNumber, CategoryParseIssueKind.EmptyCategoryName, t, null, null, null));
            return;
        }

        if (!NameRule.IsValid(name))
        {
            issues.Add(new CategoryParseIssue(lineNumber, CategoryParseIssueKind.InvalidCategoryName, t, name, null, null));
            return;
        }

        if (string.Equals(name, ReservedAuto, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, ReservedKeep, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new CategoryParseIssue(lineNumber, CategoryParseIssueKind.ReservedCategoryName, t, name, null, null));
            return;
        }

        int index = builder.FindCategory(name);
        if (index < 0)
        {
            index = builder.AddCategory(name);
        }
        else
        {
            issues.Add(new CategoryParseIssue(
                lineNumber, CategoryParseIssueKind.DuplicateCategory, t, name, null, builder.NameAt(index)));
        }

        string catalogName = builder.NameAt(index);
        foreach (string rawToken in t.Substring(colon + 1).Split(','))
        {
            string token = rawToken.Trim();
            if (token.Length == 0)
            {
                continue;
            }

            int owner = builder.FindOwner(token);
            if (owner >= 0)
            {
                issues.Add(new CategoryParseIssue(
                    lineNumber, CategoryParseIssueKind.DuplicatePrefab, t, catalogName, token, builder.NameAt(owner)));
            }
            else
            {
                builder.AddPrefab(index, token);
            }
        }
    }
}
