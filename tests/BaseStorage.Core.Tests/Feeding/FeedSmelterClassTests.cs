namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Feeding.FeedFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>Smelter-class stations: the v0.4.0 rooms and orders are unchanged (FM2), and the queue and fuel boundaries hold (FM3, FM4, contract 7.4).</summary>
public sealed class FeedSmelterClassTests
{
    // GW-U-2643. The v0.4.0 formulas are copied here, not taken from the implementation.
    [Fact]
    public void FM2_SmelterClassMatchesTheV040RoomsAndOrders()
    {
        var failures = new List<string>();
        int[] maxQueuedValues = { 0, 1, 10 };
        int[] maxFuelValues = { 0, 1, 20 };
        string?[] fuelPrefabs = { "Coal", null };

        for (int queued = -1; queued <= 11; queued++)
        {
            foreach (int maxQueued in maxQueuedValues)
            {
                for (int quarters = 0; quarters <= 22; quarters++)
                {
                    float fuel = quarters / 4f;
                    foreach (int intMax in maxFuelValues)
                    {
                        foreach (string? fuelPrefab in fuelPrefabs)
                        {
                            CheckGridPoint(failures, queued, maxQueued, fuel, intMax, fuelPrefab);
                        }
                    }
                }
            }
        }

        Assert.True(failures.Count == 0, failures.Count + " grid points differ from v0.4.0. First: " + failures.FirstOrDefault());
    }

    // GW-U-2667 (SM1-SM3)
    [Theory]
    [InlineData("SM1", 9, 1)]
    [InlineData("SM2", 10, 0)]
    [InlineData("SM3", 11, 0)]
    public void FM4_SmelterClassQueueAndFuelBoundaries(string id, int queued, int expectedAmount)
    {
        FeedStationState station = FeedStationState.SmelterClass(queued, 10, 20f, 20f, "Coal", Ores);
        Assert.True(station.QueueRoom == Math.Max(0, 10 - queued), $"{id}: QueueRoom is {station.QueueRoom}.");

        FeedOrder? order = Next7(station, Items("CopperOre"), new[] { C(1, 1f, 0f, 4, S("CopperOre", 30)) });
        if (expectedAmount == 0)
        {
            Assert.True(order is null, $"{id}: expected no order, got {Describe(order)}.");
            return;
        }

        Assert.True(order is not null, $"{id}: expected an order of {expectedAmount}, got none.");
        Assert.Equal("CopperOre", order!.PrefabName);
        Assert.Equal(expectedAmount, order.Amount);
    }

    // GW-U-2667 (SM4-SM8)
    [Theory]
    [InlineData("SM4", 19f, 1)]
    [InlineData("SM5", 19.5f, 0)]
    [InlineData("SM6", 20f, 0)]
    [InlineData("SM7", 21f, 0)]
    [InlineData("SM8", 0f, 20)]
    public void FM4_SmelterClassFuelRoomBoundaries(string id, float fuel, int expectedRoom)
    {
        FeedStationState station = FeedStationState.SmelterClass(0, 10, fuel, 20f, "Coal", Ores);
        Assert.True(station.FuelRoom == expectedRoom, $"{id}: FuelRoom is {station.FuelRoom}, expected {expectedRoom}.");
    }

    // GW-U-2667 (SM9)
    [Fact]
    public void FM4_SmelterClassShapeAndMaxFuel()
    {
        FeedStationState station = FeedStationState.SmelterClass(0, 10, 20f, 20f, "Coal", Ores);
        Assert.Equal(FeedStationShape.SmelterClass, station.Shape);
        Assert.Equal(20f, station.MaxFuel);
    }

    private static void CheckGridPoint(List<string> failures, int queued, int maxQueued, float fuel, int intMax, string? fuelPrefab)
    {
        // v0.4.0: QueueRoom = Math.Max(0, mq - q); FuelRoom = Math.Max(0, (int)Math.Floor(intMax - fuel)) with a null prefab giving 0.
        int oldQueueRoom = Math.Max(0, maxQueued - queued);
        int oldFuelRoom = fuelPrefab is null ? 0 : Math.Max(0, (int)Math.Floor(intMax - fuel));

        FeedStationState station = FeedStationState.SmelterClass(queued, maxQueued, fuel, intMax, fuelPrefab, Ores);
        string point = $"queued={queued} maxQueued={maxQueued} fuel={fuel} maxFuel={intMax} prefab={fuelPrefab ?? "null"}";

        if (station.Queued != queued || station.MaxQueued != maxQueued || station.Fuel != fuel || station.FuelPrefab != fuelPrefab)
        {
            failures.Add(point + ": stored values differ");
        }

        if (!ReferenceEquals(station.Inputs, Ores))
        {
            failures.Add(point + ": Inputs is not the same instance");
        }

        if (station.QueueRoom != oldQueueRoom || station.FuelRoom != oldFuelRoom || station.MaxFuel != (float)intMax)
        {
            failures.Add($"{point}: rooms {station.QueueRoom}/{station.FuelRoom}, v0.4.0 {oldQueueRoom}/{oldFuelRoom}");
        }

        // The 5-argument Next over one chest that holds both items: fuel first, then the first accepted input.
        ContainerSnapshot[] chests = { C(1, 1f, 0f, 4, S("Coal", 50), S("CopperOre", 50)) };
        ExpectedOrder? expected = null;
        if (oldFuelRoom > 0)
        {
            expected = new ExpectedOrder(1, "Coal", Math.Min(oldFuelRoom, 50), true);
        }
        else if (oldQueueRoom > 0)
        {
            expected = new ExpectedOrder(1, "CopperOre", Math.Min(oldQueueRoom, 50), false);
        }

        FeedOrder? actual = Next5(station, Sel(("Coal", 0), ("CopperOre", 0)), chests);
        if (!Matches(expected, actual))
        {
            failures.Add($"{point}: order {Describe(actual)}, v0.4.0 predicts {expected}");
        }
    }
}
