namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Linq;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.StationGraphFixtures;

/// <summary>AC-4: the station cap, the clamp of values below 1, and the IsCapped flag (contract 7.3, fixtures G10 and the 64/65 chain).</summary>
public sealed class StationGraphCapTests
{
    /// <summary>Rows that are capped, with the uncapped order they are a prefix of.</summary>
    public static TheoryData<string> CappedRowIds() => new() { "G10c", "G10d", "G10e", "G10g" };

    /// <summary>Rows whose IsCapped is false (G1-G9, G11-G13, plus G10a, G10b, G10h and G10i), without the throwing G14 rows.</summary>
    public static TheoryData<string> UncappedRowIds()
    {
        var data = new TheoryData<string>();
        string[] ids =
        {
            "G1", "G2a", "G2b", "G3a", "G3b", "G4a", "G4b", "G5", "G6a", "G6b", "G6c", "G7a", "G7b", "G7c",
            "G8a", "G8b", "G8c", "G8d", "G9a", "G9b", "G9c", "G9d", "G9e", "G9f", "G10a", "G10b", "G10h", "G10i",
            "G11", "G12a", "G12b", "G12c", "G12d", "G13a", "G13b", "G13c",
        };
        foreach (string id in ids)
        {
            data.Add(id);
        }

        return data;
    }

    // GW-U-132 (AC-4): exactly MaxBaseStations is not truncated and not capped.
    [Fact]
    public void ExactlyMaxStationsIsNotCapped()
    {
        AssertRow("G10b");
    }

    // GW-U-132 (AC-4): more connected stations than the cap are capped at the cap.
    [Theory]
    [InlineData("G10c")]
    [InlineData("G10d")]
    [InlineData("G10e")]
    [InlineData("G10g")]
    public void MoreThanMaxStationsIsCappedAtTheCap(string id)
    {
        AssertRow(id);
    }

    // GW-U-132 (AC-4): a capped result is a prefix of the matching uncapped order, and has exactly cap stations.
    [Theory]
    [MemberData(nameof(CappedRowIds))]
    public void CappedResultIsAPrefixOfTheUncappedOrder(string id)
    {
        Row row = Rows[id];
        string uncapped = id == "G10g" ? "G10h" : "G10a";
        (long UserId, uint Id)[] full = Rows[uncapped].ExpectedFor(VerifiedRule);

        var result = Run(id);
        (long UserId, uint Id)[] actual = result.Stations.Select(s => (s.Id.UserId, s.Id.Id)).ToArray();

        Assert.Equal(row.MaxBaseStations, actual.Length);
        Assert.Equal(full.Take(row.MaxBaseStations).ToArray(), actual);
        Assert.True(result.IsCapped);
    }

    // GW-U-133 (AC-4): maxBaseStations of 0, -1 and int.MinValue clamp to 1 and never throw.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void MaxBelowOneClampsToOneAndIsCapped(int max)
    {
        AssertRow("G10f", maxOverride: max);
        AssertRow("G10f", reversed: true, maxOverride: max);
    }

    // GW-U-134 (AC-4): IsCapped is false for every uncapped contract row.
    [Theory]
    [MemberData(nameof(UncappedRowIds))]
    public void IsCappedIsFalseForEveryUncappedRow(string id)
    {
        StationGraphResult result = Run(id);

        Assert.False(result.IsCapped);
    }

    // GW-U-134 (AC-4): a 64-station chain fits the default cap exactly.
    [Fact]
    public void SixtyFourStationChainFitsTheDefaultCapWithoutCapping()
    {
        StationSnapshot[] chain = Enumerable.Range(1, 64).Select(i => S((uint)i, 15f * i)).ToArray();

        StationGraphResult result = StationGraph.FindBase(chain, Id(1), DefaultMax);

        Assert.Equal(Enumerable.Range(1, 64).Select(i => (uint)i), result.Stations.Select(s => s.Id.Id));
        Assert.False(result.IsCapped);
    }

    // GW-U-134 (AC-4): a 65-station chain returns ids 1 to 64 and is capped.
    [Fact]
    public void SixtyFiveStationChainReturnsTheFirst64AndIsCapped()
    {
        StationSnapshot[] chain = Enumerable.Range(1, 65).Select(i => S((uint)i, 15f * i)).ToArray();

        StationGraphResult result = StationGraph.FindBase(chain, Id(1), DefaultMax);

        Assert.Equal(Enumerable.Range(1, 64).Select(i => (uint)i), result.Stations.Select(s => s.Id.Id));
        Assert.True(result.IsCapped);
    }
}
