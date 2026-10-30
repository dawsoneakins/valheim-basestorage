namespace BaseStorage.Core.Tests.Bases;

using System.Collections.Generic;

/// <summary>AC-1 and the NFR "Deterministic": breadth-first fill order, neighbours in ascending StationId, and order independence.</summary>
public sealed class StationGraphOrderTests
{
    /// <summary>Contract rows G2a to G11 (every row whose order and connection are fixed by the contract, except the G10f clamp).</summary>
    private static readonly string[] OrderRows =
    {
        "G2a", "G2b", "G3a", "G3b", "G4a", "G4b", "G5", "G6a", "G6b", "G6c", "G7a", "G7b", "G7c",
        "G8a", "G8b", "G8c", "G8d", "G9a", "G9b", "G9c", "G9d", "G9e", "G9f",
        "G10a", "G10b", "G10c", "G10d", "G10e", "G10g", "G10h", "G10i", "G11",
    };

    // GW-U-119 (AC-1)
    [Theory]
    [InlineData("G10a")]
    [InlineData("G10h")]
    [InlineData("G11")]
    public void FillOrderIsBreadthFirstWithNeighboursInAscendingStationId(string id)
    {
        StationGraphFixtures.AssertRow(id);
    }

    // GW-U-120 (AC-1, NFR Deterministic)
    [Theory]
    [MemberData(nameof(OrderRowIds))]
    public void ReversedInputGivesTheSameSequenceAndFlag(string id)
    {
        StationGraphFixtures.AssertRow(id, reversed: true);
    }

    // GW-U-120 (AC-1, NFR Deterministic): the forward list as well, so both orders are covered for each row.
    [Theory]
    [MemberData(nameof(OrderRowIds))]
    public void ForwardInputGivesTheContractSequenceAndFlag(string id)
    {
        StationGraphFixtures.AssertRow(id);
    }

    /// <summary>Every row ID from G2a to G11, one case per row, for the order-independence theories.</summary>
    public static TheoryData<string> OrderRowIds()
    {
        var data = new TheoryData<string>();
        foreach (string id in OrderRows)
        {
            data.Add(id);
        }

        return data;
    }
}
