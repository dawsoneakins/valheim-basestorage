namespace BaseStorage.Core.Tests.Categories;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-2 (contract 3.3, 5.4): CategoryParseIssue is a value with the contracted equality, severity and validation.</summary>
public sealed class CategoryParseIssueTests
{
    // GW-U-161
    [Fact]
    public void EqualIssuesAreEqualWithEqualHashCodes()
    {
        ValueSemantics.AssertEqual(
            new CategoryParseIssue(6, CategoryParseIssueKind.MissingSeparator, "CopperOre Copper", null, null, null),
            new CategoryParseIssue(6, CategoryParseIssueKind.MissingSeparator, "CopperOre Copper", null, null, null));
    }

    // GW-U-161
    [Fact]
    public void IssuesThatDifferInOneFieldAreNotEqual()
    {
        CategoryParseIssue baseline = new CategoryParseIssue(6, CategoryParseIssueKind.MissingSeparator, "CopperOre Copper", null, null, null);

        ValueSemantics.AssertNotEqual(
            baseline,
            new CategoryParseIssue(7, CategoryParseIssueKind.MissingSeparator, "CopperOre Copper", null, null, null));
        ValueSemantics.AssertNotEqual(
            baseline,
            new CategoryParseIssue(6, CategoryParseIssueKind.MissingSeparator, "copperore copper", null, null, null));
        ValueSemantics.AssertNotEqual(
            baseline,
            new CategoryParseIssue(6, CategoryParseIssueKind.EmptyCategoryName, "CopperOre Copper", null, null, null));
        ValueSemantics.AssertNotEqual(
            new CategoryParseIssue(9, CategoryParseIssueKind.DuplicatePrefab, "x: a", "x", "a", "y"),
            new CategoryParseIssue(9, CategoryParseIssueKind.DuplicatePrefab, "x: a", "x", "a", null));
    }

    // GW-U-161
    [Fact]
    public void NullHandlingFollowsTheValueOperators()
    {
        CategoryParseIssue issue = new CategoryParseIssue(6, CategoryParseIssueKind.MissingSeparator, "CopperOre Copper", null, null, null);
        CategoryParseIssue? none = null;

        ValueSemantics.AssertNullHandling(typeof(CategoryParseIssue), issue);
        Assert.True(none == null);
        Assert.True((CategoryParseIssue?)null == (CategoryParseIssue?)null);
        Assert.False(issue.Equals(null));
    }

    // GW-U-161
    [Theory]
    [InlineData(CategoryParseIssueKind.MissingSeparator, LogSeverity.Error)]
    [InlineData(CategoryParseIssueKind.EmptyCategoryName, LogSeverity.Error)]
    [InlineData(CategoryParseIssueKind.InvalidCategoryName, LogSeverity.Error)]
    [InlineData(CategoryParseIssueKind.ReservedCategoryName, LogSeverity.Error)]
    [InlineData(CategoryParseIssueKind.DuplicateCategory, LogSeverity.Warning)]
    [InlineData(CategoryParseIssueKind.DuplicatePrefab, LogSeverity.Warning)]
    public void SeverityIsDerivedFromTheKind(CategoryParseIssueKind kind, LogSeverity expected)
    {
        CategoryParseIssue issue = new CategoryParseIssue(1, kind, "x", null, null, null);

        Assert.Equal(expected, issue.Severity);
    }

    // GW-U-161
    [Fact]
    public void LineNumberBelowOneIsAnArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new CategoryParseIssue(0, CategoryParseIssueKind.MissingSeparator, "x", null, null, null);
        });
        Assert.Equal("lineNumber", ex.ParamName);
    }

    // GW-U-161
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void KindOutsideTheSixDefinedValuesIsAnArgumentOutOfRangeException(int kind)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new CategoryParseIssue(1, (CategoryParseIssueKind)kind, "x", null, null, null);
        });
        Assert.Equal("kind", ex.ParamName);
    }

    // GW-U-161
    [Fact]
    public void NullLineTextIsAnArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new CategoryParseIssue(1, CategoryParseIssueKind.MissingSeparator, null!, null, null, null);
        });
        Assert.Equal("lineText", ex.ParamName);
    }

    // GW-U-161
    [Fact]
    public void NullOptionalNamesAreAccepted()
    {
        CategoryParseIssue issue = new CategoryParseIssue(1, CategoryParseIssueKind.DuplicatePrefab, "x", null, null, null);

        Assert.Null(issue.CategoryName);
        Assert.Null(issue.PrefabName);
        Assert.Null(issue.KeptCategoryName);
    }
}
