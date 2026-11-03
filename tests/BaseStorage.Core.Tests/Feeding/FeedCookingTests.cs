namespace BaseStorage.Core.Tests.Feeding;

using System.Collections.Generic;
using BaseStorage.Core.Feeding;

/// <summary>Fires, fermenters, cooking stations and ovens through the planner's room rules (S-QF-002, S-QF-003).</summary>
public sealed class FeedCookingTests
{
    private static readonly string[] Meat = { "RawMeat", "NeckTail" };

    [Fact]
    public void AnOvenReportsItsFuelAndFreeSlots()
    {
        FeedStationState oven = FeedStationState.SeveralInputFuelled(2, 1, 3.5f, 10f, "Coal", Meat);

        Assert.Equal(FeedStationShape.SeveralInput, oven.Shape);
        Assert.Equal(1, oven.QueueRoom);
        Assert.Equal(6, oven.FuelRoom);
        Assert.Equal("Coal", oven.FuelPrefab);
    }

    [Fact]
    public void ACookingStationWithNoFuelItemNeverAsksForFuel()
    {
        FeedStationState station = FeedStationState.SeveralInputFuelled(3, 0, 0f, 0f, null, Meat);

        Assert.Equal(0, station.FuelRoom);
        Assert.Equal(3, station.QueueRoom);
    }

    [Fact]
    public void AFullStationHasNoRoomForFood()
    {
        Assert.Equal(0, FeedStationState.SeveralInputFuelled(2, 2, 0f, 0f, null, Meat).QueueRoom);
        Assert.Equal(0, FeedStationState.SingleInput(true, Meat).QueueRoom);
    }

    [Fact]
    public void AnEmptyFermenterTakesExactlyOneInput()
    {
        Assert.Equal(1, FeedStationState.SingleInput(false, new List<string> { "MeadBaseHealthMinor" }).QueueRoom);
    }

    [Fact]
    public void AFireOnlyEverTakesFuel()
    {
        FeedStationState fire = FeedStationState.FuelOnly(1.5f, 10f, "Wood");

        Assert.Equal(0, fire.QueueRoom);
        Assert.Equal(8, fire.FuelRoom);
    }
}
