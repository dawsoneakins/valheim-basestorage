namespace BaseStorage.Core.Tests.Crafting;

using System.Linq;
using Xunit;

/// <summary>GW-U-1700 (AC-1, AC-6; contract upgrade-multicraft 3.1 O1-O12): the test-only vanilla amount oracle over literal rows.</summary>
public sealed class VanillaAmountOracleTests
{
    /// <summary>The O1-O12 rows of contract 3.1: amount, amount per level, quality level, upgrader resource, and the Result column.</summary>
    /// <returns>The rows.</returns>
    public static TheoryData<int, int, int, bool, int> OracleRows() => new()
    {
        { 8, 4, 1, false, 8 },
        { 8, 4, 2, false, 4 },
        { 8, 4, 3, false, 8 },
        { 8, 4, 4, false, 16 },
        { 8, 4, 5, false, 18 },
        { 2, 1, 4, false, 4 },
        { 2, 1, 5, false, 4 },
        { 2, 3, 5, false, 13 },
        { 10, 5, 2, true, 15 },
        { 10, 5, 4, true, 30 },
        { 2, 1, 0, false, 2 },
        { 8, 0, 3, false, 0 },
    };

    // GW-U-1700 (AC-1, AC-6, O1-O12): the oracle gives the Result column; level 4 is 4x the per-level amount, and floor applies at level 5.
    [Theory]
    [MemberData(nameof(OracleRows))]
    public void TheOracleGivesTheVanillaAmountForEachRow(int amount, int amountPerLevel, int qualityLevel, bool upgraderResource, int expected)
    {
        Assert.Equal(expected, VanillaAmountOracle.For(amount, amountPerLevel, qualityLevel, upgraderResource));
    }

    // GW-U-1700 (case-count guard): the oracle table has the twelve rows of contract 3.1, so a dropped row cannot pass silently.
    [Fact]
    public void TheOracleTableHasTwelveRows()
    {
        Assert.Equal(12, OracleRows().Count());
    }
}
