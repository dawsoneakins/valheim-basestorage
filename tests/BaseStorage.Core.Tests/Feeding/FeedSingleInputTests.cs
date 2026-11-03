namespace BaseStorage.Core.Tests.Feeding;

using BaseStorage.Core.Feeding;
using static BaseStorage.Core.Tests.Feeding.FeedFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>Single-input stations (a fermenter): one item at a time, and only when empty (FM5, contract 7.2).</summary>
public sealed class FeedSingleInputTests
{
    // GW-U-2663. An expected prefab of null means the planner returns null.
    [Theory]
    [InlineData("SI1", false, "MeadBaseTasty", "MeadBaseTasty", "MeadBaseTasty", 1)]
    [InlineData("SI2", true, "MeadBaseTasty", "MeadBaseTasty", null, 0)]
    [InlineData("SI3", false, "MeadBaseHealthMinor,MeadBaseTasty", "MeadBaseTasty", "MeadBaseTasty", 1)]
    [InlineData("SI6", false, "RawMeat", "RawMeat", null, 0)]
    public void FM5_SingleInputTakesOneOnlyWhenEmpty(
        string id,
        bool occupied,
        string selectionNames,
        string chestPrefab,
        string? expectedPrefab,
        int expectedAmount)
    {
        FeedStationState station = FeedStationState.SingleInput(occupied, Mead);
        FeedOrder? order = Next7(station, Items(selectionNames.Split(',')), new[] { C(1, 1f, 0f, 4, S(chestPrefab, 10, 10)) });

        if (expectedPrefab is null)
        {
            Assert.True(order is null, $"{id}: expected no order, got {Describe(order)}.");
            return;
        }

        Assert.True(order is not null, $"{id}: expected {expectedPrefab}, got none.");
        Assert.Equal(Id(1), order!.Chest);
        Assert.Equal(expectedPrefab, order.PrefabName);
        Assert.Equal(expectedAmount, order.Amount);
        Assert.False(order.IsFuel);
    }

    // GW-U-2664
    [Fact]
    public void FM5_SingleInputRoomIsOneOrZero()
    {
        FeedStationState empty = FeedStationState.SingleInput(false, Mead);
        Assert.Equal(FeedStationShape.SingleInput, empty.Shape);
        Assert.Equal(0, empty.Queued);
        Assert.Equal(1, empty.MaxQueued);
        Assert.Equal(1, empty.QueueRoom);
        Assert.Equal(0, empty.FuelRoom);
        Assert.Null(empty.FuelPrefab);
        Assert.Equal(0f, empty.Fuel);
        Assert.Equal(0f, empty.MaxFuel);

        FeedStationState occupied = FeedStationState.SingleInput(true, Mead);
        Assert.Equal(1, occupied.Queued);
        Assert.Equal(1, occupied.MaxQueued);
        Assert.Equal(0, occupied.QueueRoom);
        Assert.Equal(0, occupied.FuelRoom);
        Assert.Equal(0f, occupied.Fuel);
        Assert.Equal(0f, occupied.MaxFuel);
    }
}
