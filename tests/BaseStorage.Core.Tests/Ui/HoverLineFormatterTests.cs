namespace BaseStorage.Core.Tests.Ui;

using System;

/// <summary>S-FOUND-001 AC-2: the hover-line formatter output, ranges and single-line rule (contract 5.2 H1-H7, 5.3).</summary>
public sealed class HoverLineFormatterTests
{
    // CR, LF, U+0085, U+2028 and U+2029 as char casts: C# treats the last three as newlines inside source text.
    private static readonly char[] BreakCharacters = ['\r', '\n', (char)0x85, (char)0x2028, (char)0x2029];

    /// <summary>Every stationCount x categoryLabel x cycleKeyLabel combination from contract 5.3 (5 x 8 x 4 = 160 cases).</summary>
    public static TheoryData<int, string?, string?> ChestMatrix()
    {
        var data = new TheoryData<int, string?, string?>();
        foreach (int stationCount in UiFixtures.MatrixStationCounts)
        {
            foreach (string? categoryLabel in UiFixtures.MatrixCategoryLabels)
            {
                foreach (string? cycleKeyLabel in UiFixtures.MatrixCycleKeyLabels)
                {
                    data.Add(stationCount, categoryLabel, cycleKeyLabel);
                }
            }
        }

        return data;
    }

    /// <summary>Station(chestCount) for every chestCount in contract 5.3.</summary>
    public static TheoryData<int> StationMatrix()
    {
        var data = new TheoryData<int>();
        foreach (int chestCount in UiFixtures.MatrixChestCounts)
        {
            data.Add(chestCount);
        }

        return data;
    }

    // GW-U-058
    [Theory]
    [MemberData(nameof(UiFixtures.ChestLines), MemberType = typeof(UiFixtures))]
    public void ChestLineMatchesTheContractExample(int stationCount, string? categoryLabel, string? cycleKeyLabel, string expected)
    {
        Assert.Equal(expected, UiContract.Chest(stationCount, categoryLabel, cycleKeyLabel));
    }

    // GW-U-058
    [Theory]
    [MemberData(nameof(UiFixtures.StationLines), MemberType = typeof(UiFixtures))]
    public void StationLineMatchesTheContractExample(int chestCount, string expected)
    {
        Assert.Equal(expected, UiContract.Station(chestCount));
    }

    // GW-U-058
    [Fact]
    public void NegativeStationCountThrowsArgumentOutOfRangeNamedStationCount()
    {
        UiContract.Find("HoverLineFormatter");

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => UiContract.Chest(-1, null, null));

        Assert.Equal("stationCount", ex.ParamName);
    }

    // GW-U-058
    [Fact]
    public void NegativeChestCountThrowsArgumentOutOfRangeNamedChestCount()
    {
        UiContract.Find("HoverLineFormatter");

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => UiContract.Station(-3));

        Assert.Equal("chestCount", ex.ParamName);
    }

    // GW-U-059
    [Theory]
    [MemberData(nameof(ChestMatrix))]
    public void ChestLineIsOneTrimmedLineForEveryMatrixCell(int stationCount, string? categoryLabel, string? cycleKeyLabel)
    {
        AssertOneTrimmedLine(UiContract.Chest(stationCount, categoryLabel, cycleKeyLabel));
    }

    // GW-U-059
    [Theory]
    [MemberData(nameof(StationMatrix))]
    public void StationLineIsOneTrimmedLineForEveryChestCount(int chestCount)
    {
        AssertOneTrimmedLine(UiContract.Station(chestCount));
    }

    private static void AssertOneTrimmedLine(string line)
    {
        Assert.NotNull(line);
        Assert.NotEmpty(line);
        Assert.Equal(-1, line.IndexOfAny(BreakCharacters));
        Assert.Equal(line.Trim(), line);
    }
}
