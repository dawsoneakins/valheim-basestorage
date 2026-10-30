namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;

/// <summary>AC-9 (contract G7, 4.4, C4): the raw output is LF only with the exact header, and CRLF is normalised before text comparison.</summary>
public sealed class DefaultCategoryFileLayoutTests
{
    public static IEnumerable<object[]> LayoutSources()
    {
        yield return new object[] { 0 };
        yield return new object[] { 1 };
        yield return new object[] { 2 };
    }

    private static string Sample(int which) => which switch
    {
        0 => DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems()),
        1 => DefaultCategoryFile.Generate(Array.Empty<CategoryItem>()),
        _ => DefaultCategoryFile.Generate(DefaultCategoryFixtures.ObjectDbSized()),
    };

    // GW-U-453
    [Theory]
    [MemberData(nameof(LayoutSources))]
    public void RawOutputIsLfOnlyWithTheExactLayout(int which)
    {
        string text = Sample(which);

        Assert.Equal(-1, text.IndexOf('\r'));
        Assert.Equal(-1, text.IndexOf('\t'));
        Assert.False(text.StartsWith('﻿'));
        Assert.EndsWith("\n", text, StringComparison.Ordinal);
        Assert.Equal(21, text.Count(c => c == '\n'));
        Assert.True(text.StartsWith(DefaultCategoryFixtures.Header, StringComparison.Ordinal), "The header does not equal the contract 4.4 literal.");

        string[] lines = text.Split('\n');
        for (int i = 0; i < 6; i++)
        {
            Assert.StartsWith("# ", lines[i], StringComparison.Ordinal);
        }

        Assert.Equal(string.Empty, lines[6]);
        for (int i = 0; i < 14; i++)
        {
            Assert.StartsWith(DefaultCategoryFixtures.Names[i] + ":", lines[7 + i], StringComparison.Ordinal);
        }

        foreach (string line in lines)
        {
            Assert.Equal(line, line.TrimEnd());
        }
    }

    // GW-U-454
    [Fact]
    public void NormaliseMapsCrlfToLfAndLeavesLfAlone()
    {
        Assert.Equal("a\nb\n", DefaultCategoryFixtures.Normalise("a\r\nb\r\n"));
        Assert.Equal("a\nb\n", DefaultCategoryFixtures.Normalise("a\nb\n"));
    }

    // GW-U-454
    [Fact]
    public void ACrlfCopyOfTheExpectedTextComparesEqualOnlyThroughNormalise()
    {
        string generated = DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems());
        string crlfExpected = DefaultCategoryFixtures.ExpectedS.Replace("\n", "\r\n");

        Assert.NotEqual(crlfExpected, generated);
        Assert.Equal(DefaultCategoryFixtures.Normalise(crlfExpected), DefaultCategoryFixtures.Normalise(generated));
    }
}
