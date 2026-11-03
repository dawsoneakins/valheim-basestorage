namespace BaseStorage.Core.Tests.Feeding;

using BaseStorage.Core.Feeding;
using static BaseStorage.Core.Tests.Feeding.FeedFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>Several-input stations (a cooking station): at most one item per empty slot, in the stored order (FM6, FM8, contract 7.3).</summary>
public sealed class FeedSeveralInputTests
{
    // GW-U-2665. An expected amount of 0 means the planner returns null.
    [Theory]
    [InlineData("SV1", 3, 0, 20, 3)]
    [InlineData("SV2", 3, 2, 20, 1)]
    [InlineData("SV3", 3, 3, 20, 0)]
    [InlineData("SV4", 3, 4, 20, 0)]
    [InlineData("SV5", 0, 0, 20, 0)]
    [InlineData("SV6", 3, 0, 2, 2)]
    [InlineData("SV7", -1, 0, 20, 0)]
    public void FM6_SeveralInputTakesAtMostOnePerEmptySlot(string id, int slots, int occupied, int stock, int expectedAmount)
    {
        FeedStationState station = FeedStationState.SeveralInput(slots, occupied, Food);
        Assert.True(station.Shape == FeedStationShape.SeveralInput, $"{id}: shape is {station.Shape}.");
        Assert.True(station.FuelRoom == 0, $"{id}: FuelRoom is {station.FuelRoom}.");

        FeedOrder? order = Next7(station, Items("RawMeat"), new[] { C(1, 1f, 0f, 4, S("RawMeat", stock)) });
        if (expectedAmount == 0)
        {
            Assert.True(order is null, $"{id}: expected no order, got {Describe(order)}.");
            return;
        }

        Assert.True(order is not null, $"{id}: expected an order of {expectedAmount}, got none.");
        Assert.Equal("RawMeat", order!.PrefabName);
        Assert.Equal(expectedAmount, order.Amount);
        Assert.False(order.IsFuel);
    }

    // GW-U-2666
    [Fact]
    public void FM8_SeveralInputFollowsTheChosenOrder()
    {
        FeedStationState station = FeedStationState.SeveralInput(3, 0, Food);

        // SV9: NeckTail is chosen before RawMeat, so it is taken first, up to the three empty slots.
        FeedOrder? order = Next7(station, Items("NeckTail", "RawMeat"), new[] { C(1, 1f, 0f, 4, S("RawMeat", 20), S("NeckTail", 20)) });

        Assert.True(order is not null, "SV9 gave no order.");
        Assert.Equal("NeckTail", order!.PrefabName);
        Assert.Equal(3, order.Amount);
    }
}
