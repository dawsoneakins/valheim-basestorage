namespace BaseStorage.Core.Tests.Categories;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;

/// <summary>
/// AC-1, AC-2 and AC-4 (contract C4, C5, C6, spec section 10): LF, CRLF, CR and a leading byte order mark give the same result.
/// </summary>
public sealed class CategoryFileLineEndingTests
{
    private const string MultiLineCrlf = "Wood: Wood\r\n\r\nStone: Stone\r\n";

    // GW-U-155 (AC-1)
    [Theory]
    [MemberData(nameof(Sources))]
    public void LineEndingAndByteOrderMarkVariantsGiveEqualIssuesAndCatalog(string source)
    {
        CategoryParseResult baseline = CategoryFile.Parse(Lf(source));

        foreach (string variant in Variants(source))
        {
            CategoryParseResult actual = CategoryFile.Parse(variant);
            Assert.Equal(baseline.Issues, actual.Issues);
            Assert.Equal(CategoryFixtures.Describe(baseline.Catalog), CategoryFixtures.Describe(actual.Catalog));
        }
    }

    // GW-U-155 (AC-4)
    [Fact]
    public void CategoryOrderIsIndependentOfLineEndings()
    {
        string[] expected = CategoryFile.Parse(CategoryFixtures.F).Catalog.CategoryNames.ToArray();

        foreach (string variant in Variants(CategoryFixtures.F))
        {
            Assert.Equal(expected, CategoryFile.Parse(variant).Catalog.CategoryNames);
        }
    }

    // GW-U-155 (AC-2)
    [Fact]
    public void IssueLineNumbersAreIndependentOfLineEndings()
    {
        int[] expected = CategoryFile.Parse(CategoryFixtures.F).Issues.Select(issue => issue.LineNumber).ToArray();

        foreach (string variant in Variants(CategoryFixtures.F))
        {
            Assert.Equal(expected, CategoryFile.Parse(variant).Issues.Select(issue => issue.LineNumber));
        }
    }

    // GW-U-155 (AC-1)
    [Fact]
    public void TrailingTerminatorAddsNoLineAndAFinalUnterminatedLineIsParsed()
    {
        CategoryParseIssue leading = Assert.Single(CategoryFile.Parse("Bad line\n").Issues);
        Assert.Equal(1, leading.LineNumber);

        CategoryParseIssue last = Assert.Single(CategoryFile.Parse("Wood: Wood\nBad").Issues);
        Assert.Equal(new CategoryParseIssue(2, CategoryParseIssueKind.MissingSeparator, "Bad", null, null, null), last);

        Assert.Empty(CategoryFile.Parse("Wood: Wood\n").Issues);
    }

    // GW-U-155 (AC-1)
    [Fact]
    public void ByteOrderMarkNotAtIndexZeroIsOrdinaryText()
    {
        CategoryCatalog catalog = CategoryFile.Parse("Wood: \uFEFFWood").Catalog;

        Assert.Equal("Wood", catalog.GetMappedCategory("\uFEFFWood"));
        Assert.Null(catalog.GetMappedCategory("Wood"));
    }

    /// <summary>The source texts of the line-ending theory as xunit member data.</summary>
    public static IEnumerable<object[]> Sources()
    {
        yield return new object[] { CategoryFixtures.F };
        yield return new object[] { MultiLineCrlf };
    }

    /// <summary>The LF form of a text, the baseline every variant must match.</summary>
    private static string Lf(string text) => text.Replace("\r\n", "\n");

    /// <summary>The LF, CRLF, CR and byte-order-mark variants of a text.</summary>
    private static IEnumerable<string> Variants(string text)
    {
        string lf = Lf(text);
        yield return lf;
        yield return lf.Replace("\n", "\r\n");
        yield return lf.Replace("\n", "\r");
        yield return "\uFEFF" + lf;
    }
}
