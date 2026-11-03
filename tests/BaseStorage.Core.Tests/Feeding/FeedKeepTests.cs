namespace BaseStorage.Core.Tests.Feeding;

using BaseStorage.Core.Feeding;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Feeding.FeedFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>The keep amount of a selected item, for every shape (FM9, FM11, contract 7.1-7.3 keep rows).</summary>
public sealed class FeedKeepTests
{
    // GW-U-2670. An expected amount of 0 means the planner returns null.
    [Theory]
    [InlineData("FB17", 45, 50, 5)]
    [InlineData("SI4", 10, 10, 0)]
    [InlineData("SI5", 9, 10, 1)]
    [InlineData("SV8", 19, 20, 1)]
    public void FM11_EachShapeLeavesTheKeepAmountInTheBase(string id, int keep, int stock, int expectedAmount)
    {
        (FeedStationState station, string prefab) = id switch
        {
            "FB17" => (FeedStationState.FuelOnly(0f, 10f, "Wood"), "Wood"),
            "SI4" or "SI5" => (FeedStationState.SingleInput(false, Mead), "MeadBaseTasty"),
            "SV8" => (FeedStationState.SeveralInput(3, 0, Food), "RawMeat"),
            _ => throw new System.ArgumentException("Unknown keep row: " + id, nameof(id)),
        };

        ContainerSnapshot[] chests = { C(1, 1f, 0f, 4, S(prefab, stock, 50)) };
        FeedOrder? order = Next7(station, FeedSelectionOf(prefab, keep), chests);

        if (expectedAmount == 0)
        {
            Assert.True(order is null, $"{id}: expected no order, got {Describe(order)}.");
            return;
        }

        Assert.True(order is not null, $"{id}: expected an order of {expectedAmount}, got none.");
        Assert.Equal(expectedAmount, order!.Amount);
        Assert.True(FeedPlanner.CountInBase(chests, prefab) - order.Amount >= keep, $"{id}: the keep amount {keep} was taken.");
    }

    // FM9 (no contract row of its own; contract 0.2 FM9 is pinned here by name)
    [Fact]
    public void FM9_AmountIsTheSmallestOfRoomSpareAndStock()
    {
        // Room 10, spare 4 and stock 4: the stock limits it.
        Assert.Equal(4, Next7(FeedStationState.FuelOnly(0f, 10f, "Wood"), Items("Wood"), new[] { C(1, 1f, 0f, 4, S("Wood", 4)) })!.Amount);

        // Room 3 with a full stock of 50: the room limits it.
        Assert.Equal(3, Next7(FeedStationState.FuelOnly(7f, 10f, "Wood"), Items("Wood"), new[] { C(1, 1f, 0f, 4, S("Wood", 50)) })!.Amount);

        // Room 10 with a stock of 50: the room limits it again, not the spare.
        Assert.Equal(10, Next7(FeedStationState.FuelOnly(0f, 10f, "Wood"), Items("Wood"), new[] { C(1, 1f, 0f, 4, S("Wood", 50)) })!.Amount);
    }

    private static FeedSelection FeedSelectionOf(string prefab, int keep) => Sel((prefab, keep));
}
