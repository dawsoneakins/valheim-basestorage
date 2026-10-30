namespace BaseStorage.Core.Tests.Categories;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Persistence;

/// <summary>
/// AC-2 and the spec NFR "never throws" (contract C3): fixed edge inputs and a seeded corpus (ADR-GWIN-019). Every input gives
/// a result, every catalog name passes name rule N, every issue line is in range, and a second parse gives the same result.
/// </summary>
public sealed class CategoryFileRobustnessTests
{
    private const int CorpusSeed = 20261025;
    private const int CorpusSize = 2000;

    private static readonly char[] Alphabet = { ':', ',', '#', '\r', '\n', '\t', ' ', 'a', 'A', '\uFEFF', '\uD800' };

    // GW-U-157
    [Theory]
    [MemberData(nameof(EdgeInputs))]
    public void EdgeInputIsParsedWithoutThrowing(string text)
    {
        AssertTotalParse(text);
    }

    // GW-U-157
    [Fact]
    public void SeededCorpusIsParsedWithoutThrowing()
    {
        var random = new Random(CorpusSeed);

        for (int i = 0; i < CorpusSize; i++)
        {
            AssertTotalParse(RandomText(random));
        }
    }

    /// <summary>The fixed edge inputs as xunit member data.</summary>
    public static IEnumerable<object[]> EdgeInputs()
    {
        yield return new object[] { "\0" };
        yield return new object[] { "Wood: Wood\0" };
        yield return new object[] { "\uD800" };
        yield return new object[] { "\uDC00" };
        yield return new object[] { "\u0085" };
        yield return new object[] { "\u2028" };
        yield return new object[] { "Wood: Wood\uFEFF\nStone: Stone" };
        yield return new object[] { new string(':', 10000) };
        yield return new object[] { new string(',', 10000) };
        yield return new object[] { new string('a', 100000) };
    }

    private static string RandomText(Random random)
    {
        int length = random.Next(0, 41);
        char[] chars = new char[length];

        for (int i = 0; i < length; i++)
        {
            chars[i] = Alphabet[random.Next(Alphabet.Length)];
        }

        return new string(chars);
    }

    private static void AssertTotalParse(string text)
    {
        CategoryParseResult first = CategoryFile.Parse(text);
        Assert.NotNull(first);
        Assert.NotNull(first.Catalog);
        Assert.NotNull(first.Issues);

        int lineCount = CategoryFixtures.CountLines(text);
        foreach (CategoryDefinition category in first.Catalog.Categories)
        {
            _ = CategoryAssignment.ForCategory(category.Name);
        }

        foreach (CategoryParseIssue issue in first.Issues)
        {
            Assert.InRange(issue.LineNumber, 1, lineCount);
        }

        CategoryParseResult second = CategoryFile.Parse(text);
        Assert.Equal(first.Issues, second.Issues);
        Assert.Equal(CategoryFixtures.Describe(first.Catalog), CategoryFixtures.Describe(second.Catalog));
    }
}
