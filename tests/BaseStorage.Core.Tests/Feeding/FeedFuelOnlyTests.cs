namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Feeding.FeedFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>Fuel-only stations: the fuel room and the order at each boundary of contract 7.1 (FM3, FM7, FM10).</summary>
public sealed class FeedFuelOnlyTests
{
    // GW-U-2660. An order always moves at least one item, so an expected amount of 0 means the planner returns null.
    [Theory]
    [InlineData("FB1", 0f, 10f, 10, 10)]
    [InlineData("FB2", 9f, 10f, 1, 1)]
    [InlineData("FB3", 10f, 10f, 0, 0)]
    [InlineData("FB4", 11f, 10f, 0, 0)]
    [InlineData("FB5", 0f, 0f, 0, 0)]
    [InlineData("FB6", 0f, 10.5f, 10, 10)]
    [InlineData("FB7", 0.5f, 10.5f, 10, 10)]
    [InlineData("FB8", 0.75f, 10.5f, 9, 9)]
    [InlineData("FB9", 9.5f, 10.5f, 1, 1)]
    [InlineData("FB10", 9.75f, 10.5f, 0, 0)]
    [InlineData("FB11", 10.5f, 10.5f, 0, 0)]
    [InlineData("FB12", 0f, 0.5f, 0, 0)]
    [InlineData("FB13", 0f, 1f, 1, 1)]
    [InlineData("FB14", 9.25f, 10f, 0, 0)]
    public void FM3_FuelOnlyRoomRoundsDownAtEveryBoundary(string id, float fuel, float max, int expectedRoom, int expectedAmount)
    {
        FeedStationState station = FeedStationState.FuelOnly(fuel, max, "Wood");
        Assert.True(station.FuelRoom == expectedRoom, $"{id}: FuelRoom is {station.FuelRoom}, expected {expectedRoom}.");

        FeedOrder? order = Next7(station, Items("Wood"), new[] { C(1, 1f, 0f, 4, S("Wood", 50)) });
        if (expectedAmount == 0)
        {
            Assert.True(order is null, $"{id}: expected no order, got {Describe(order)}.");
            return;
        }

        Assert.True(order is not null, $"{id}: expected an order of {expectedAmount}, got none.");
        Assert.Equal(Id(1), order!.Chest);
        Assert.Equal("Wood", order.PrefabName);
        Assert.Equal(expectedAmount, order.Amount);
        Assert.True(order.IsFuel);
    }

    // GW-U-2661
    [Fact]
    public void FM3_NonFiniteOrHugeReadingsGiveNoRoomOrSaturate()
    {
        Assert.Equal(0, FeedStationState.FuelOnly(float.NaN, 10f, "Wood").FuelRoom);
        Assert.Equal(0, FeedStationState.FuelOnly(0f, float.PositiveInfinity, "Wood").FuelRoom);
        Assert.Equal(0, FeedStationState.FuelOnly(float.NegativeInfinity, 10f, "Wood").FuelRoom);
        Assert.Equal(int.MaxValue, FeedStationState.FuelOnly(-1f, 3e9f, "Wood").FuelRoom);

        // A null fuel prefab burns nothing (FB16): no room and no order.
        FeedStationState burnsNothing = FeedStationState.FuelOnly(0f, 10f, null);
        Assert.Equal(0, burnsNothing.FuelRoom);
        Assert.Null(Next7(burnsNothing, Items("Wood"), new[] { C(1, 1f, 0f, 4, S("Wood", 50)) }));

        // The strange readings with a room give no overfill: an order of the chest's stock, and no exception.
        FeedOrder? saturated = Next7(FeedStationState.FuelOnly(-1f, 3e9f, "Wood"), Items("Wood"), new[] { C(1, 1f, 0f, 4, S("Wood", 50)) });
        Assert.True(saturated is not null && saturated.Amount == 50, "A saturated room should take the chest's 50, got " + Describe(saturated));

        foreach ((float fuel, float max) in new[] { (float.NaN, 10f), (0f, float.PositiveInfinity), (float.NegativeInfinity, 10f) })
        {
            FeedOrder? none = Next7(FeedStationState.FuelOnly(fuel, max, "Wood"), Items("Wood"), new[] { C(1, 1f, 0f, 4, S("Wood", 50)) });
            Assert.True(none is null, $"FuelOnly({fuel}, {max}) should give no order, got {Describe(none)}.");
        }
    }

    // GW-U-2662
    [Fact]
    public void FM7_FuelOnlyNeverTakesAnInput()
    {
        FeedStationState empty = FeedStationState.FuelOnly(0f, 10f, "Wood");
        Assert.Equal(FeedStationShape.FuelOnly, empty.Shape);
        Assert.Empty(empty.Inputs);
        Assert.Equal(0, empty.Queued);
        Assert.Equal(0, empty.MaxQueued);
        Assert.Equal(0, empty.QueueRoom);

        FeedStationState full = FeedStationState.FuelOnly(10f, 10f, "Wood");
        Assert.Null(Next7(full, Items("Resin"), new[] { C(1, 1f, 0f, 4, S("Resin", 50)) }));
    }

    // GW-U-2668
    // Contract 7.1 and FM10: "quarters 0..12" and "0..14" are value ranges in steps of a quarter, so maxFuel runs 0..12 and fuel 0..14.
    [Fact]
    public void FM10_NoStateEverOffersRoomBeyondCapacity()
    {
        var failures = new List<string>();
        for (int m = 0; m <= 48; m++)
        {
            float max = m / 4f;
            for (int f = 0; f <= 56; f++)
            {
                float fuel = f / 4f;
                CheckFuelRoom(failures, "FuelOnly", FeedStationState.FuelOnly(fuel, max, "Wood"), max, fuel, "Wood");
                CheckFuelRoom(failures, "SmelterClass", FeedStationState.SmelterClass(0, 10, fuel, max, "Coal", Ores), max, fuel, "Coal");
            }
        }

        for (int slots = -1; slots <= 6; slots++)
        {
            for (int occupied = 0; occupied <= 7; occupied++)
            {
                FeedStationState several = FeedStationState.SeveralInput(slots, occupied, Food);
                int expectedRoom = Math.Max(0, slots - occupied);
                if (several.QueueRoom != expectedRoom)
                {
                    failures.Add($"SeveralInput({slots},{occupied}) QueueRoom {several.QueueRoom}, expected {expectedRoom}");
                }

                FeedOrder? order = Next7(several, Items("RawMeat"), Unlimited("RawMeat"));
                if (expectedRoom == 0 ? order is not null : order?.Amount != expectedRoom)
                {
                    failures.Add($"SeveralInput({slots},{occupied}) order {Describe(order)}, expected an amount of {expectedRoom}");
                }
            }
        }

        Assert.True(failures.Count == 0, failures.Count + " overfill rows. First: " + failures.FirstOrDefault());
    }

    private static void CheckFuelRoom(List<string> failures, string kind, FeedStationState station, float max, float fuel, string fuelPrefab)
    {
        int room = station.FuelRoom;
        if (fuel > max)
        {
            // Over-full readings have no room (the sum below does not apply to them).
            if (room != 0)
            {
                failures.Add($"{kind} fuel {fuel} above max {max} has FuelRoom {room}");
            }
        }
        else
        {
            if ((double)fuel + room > max)
            {
                failures.Add($"{kind} fuel {fuel} + room {room} exceeds max {max}");
            }

            if (room > 0 && !((double)fuel + room + 1 > max))
            {
                failures.Add($"{kind} fuel {fuel} room {room} leaves a whole item that fits (max {max})");
            }
        }

        FeedSelection selection = kind == "FuelOnly" ? Items(fuelPrefab) : Items("Coal", "CopperOre");
        FeedOrder? order = Next7(station, selection, Unlimited(fuelPrefab, "CopperOre"));
        if (room > 0)
        {
            if (order is null || order.Amount != room || !order.IsFuel)
            {
                failures.Add($"{kind} fuel {fuel} max {max}: order {Describe(order)}, expected fuel {room}");
            }
        }
        else if (kind == "SmelterClass" && station.QueueRoom > 0)
        {
            if (order is null || order.Amount != station.QueueRoom || order.IsFuel)
            {
                failures.Add($"{kind} fuel {fuel} max {max}: order {Describe(order)}, expected input {station.QueueRoom}");
            }
        }
        else if (order is not null)
        {
            failures.Add($"{kind} fuel {fuel} max {max}: order {Describe(order)}, expected none");
        }
    }

    private static ContainerSnapshot[] Unlimited(params string[] prefabs) =>
        new[] { C(1, 1f, 0f, 4, prefabs.Select(prefab => S(prefab, 1000000, int.MaxValue)).ToArray()) };
}
