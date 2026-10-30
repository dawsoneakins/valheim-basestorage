namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;

/// <summary>AC-4 (contract G1-G6, 4.3-4.7): the generator's exact output, its validation and its input-order independence.</summary>
public sealed class DefaultCategoryFileGenerateTests
{
    public static IEnumerable<object[]> EdgeRowIndices() => Enumerable.Range(0, 9).Select(i => new object[] { i });

    // GW-U-442
    [Fact]
    public void GenerateOfSampleSMatchesTheContractText()
    {
        string actual = DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems());

        Assert.Equal(DefaultCategoryFixtures.Normalise(DefaultCategoryFixtures.ExpectedS), DefaultCategoryFixtures.Normalise(actual));
    }

    // GW-U-443
    [Fact]
    public void EveryItemOfSIsWrittenExactlyOnceOnTheLineOfItsWrittenCategory()
    {
        string text = DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems());
        List<(string Category, string Token)> tokens = DefaultCategoryFixtures.ParseTokens(text);

        string[] categoryLines = DefaultCategoryFixtures.Normalise(text).Split('\n')
            .Where(line => line.Length > 0 && line[0] != '#')
            .ToArray();
        Assert.Equal(14, categoryLines.Length);
        Assert.Equal(DefaultCategoryFixtures.Names, categoryLines.Select(line => line.Substring(0, line.IndexOf(':')).Trim()));

        Assert.Equal(25, tokens.Count);
        foreach (var row in DefaultCategoryFixtures.S.GroupBy(r => r.Prefab, StringComparer.Ordinal).Select(group => group.First()))
        {
            var hit = Assert.Single(tokens, token => token.Token == row.Prefab);
            Assert.Equal(row.WrittenUnder, hit.Category);
        }

        Assert.DoesNotContain(tokens, token => token.Token is "TinOre" or "Iron" or "Coins_X");
    }

    // GW-U-444
    [Theory]
    [MemberData(nameof(EdgeRowIndices))]
    public void EdgeRowProducesExactlyTheExpectedNonEmptyLine(int row)
    {
        (CategoryItem[] items, string expectedLine) = DefaultCategoryFixtures.EdgeRows()[row];

        string text = DefaultCategoryFile.Generate(items);

        Assert.Equal(expectedLine, NonEmptyCategoryLines(text));
    }

    // GW-U-445
    [Fact]
    public void GenerateIsIdenticalForEveryOrderAndRepetitionOfS()
    {
        CategoryItem[] s = DefaultCategoryFixtures.SItems();
        string expected = DefaultCategoryFile.Generate(s);

        Assert.Equal(expected, DefaultCategoryFile.Generate(Enumerable.Reverse(s)));
        Assert.Equal(expected, DefaultCategoryFile.Generate(Enumerable.OrderBy(s, item => item.PrefabName, StringComparer.Ordinal)));
        Assert.Equal(expected, DefaultCategoryFile.Generate(Enumerable.Concat(s, s)));

        var random = new Random(20261026);
        for (int n = 0; n < 20; n++)
        {
            List<CategoryItem> shuffled = s.ToList();
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            Assert.Equal(expected, DefaultCategoryFile.Generate(shuffled));
        }
    }

    // GW-U-445
    [Fact]
    public void GenerateEnumeratesTheItemsExactlyOnce()
    {
        var counting = new CountingItems(DefaultCategoryFixtures.SItems());

        _ = DefaultCategoryFile.Generate(counting);

        Assert.Equal(1, counting.GetEnumeratorCalls);
    }

    // GW-U-446
    [Fact]
    public void GenerateRejectsNullItemsWithTheItemsParameter()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => DefaultCategoryFile.Generate(null!));
        Assert.Equal("items", error.ParamName);
    }

    // GW-U-446
    [Fact]
    public void GenerateRejectsANullElementWithTheItemsParameter()
    {
        var items = new List<CategoryItem> { new CategoryItem("Wood", ItemKind.Other), null! };

        ArgumentException error = Assert.Throws<ArgumentException>(() => DefaultCategoryFile.Generate(items));
        Assert.Equal("items", error.ParamName);
    }

    // GW-U-446
    [Fact]
    public void EmptyInputGivesTheHeaderAndFourteenEmptyLines()
    {
        string expected = DefaultCategoryFixtures.Normalise(DefaultCategoryFixtures.ExpectedEmpty);

        Assert.Equal(expected, DefaultCategoryFixtures.Normalise(DefaultCategoryFile.Generate(Array.Empty<CategoryItem>())));
        Assert.Equal(expected, DefaultCategoryFixtures.Normalise(DefaultCategoryFile.Generate(new List<CategoryItem>())));
    }

    private static string NonEmptyCategoryLines(string text) => string.Join(
        "\n",
        DefaultCategoryFixtures.Normalise(text).Split('\n')
            .Where(line => line.Length > 0 && line[0] != '#' && line.Substring(line.IndexOf(':') + 1).Trim().Length > 0));

    /// <summary>Counts how many times the enumerable is enumerated (GW-U-445).</summary>
    private sealed class CountingItems : IEnumerable<CategoryItem>
    {
        private readonly CategoryItem[] items;

        public CountingItems(CategoryItem[] items)
        {
            this.items = items;
        }

        public int GetEnumeratorCalls { get; private set; }

        public IEnumerator<CategoryItem> GetEnumerator()
        {
            GetEnumeratorCalls++;
            return ((IEnumerable<CategoryItem>)items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
