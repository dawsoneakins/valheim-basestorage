namespace BaseStorage.Core.Feeding;

using System;
using System.Collections.Generic;

/// <summary>What a station holds and accepts at one moment.</summary>
public sealed class FeedStationState
{
    private static readonly string[] NoInputs = Array.Empty<string>();

    private FeedStationState(FeedStationShape shape, int queued, int maxQueued, float fuel, float maxFuel, string? fuelPrefab, IReadOnlyList<string> inputs)
    {
        Shape = shape;
        Queued = queued;
        MaxQueued = maxQueued;
        Fuel = fuel;
        MaxFuel = maxFuel;
        FuelPrefab = fuelPrefab;
        Inputs = inputs;
    }
    /// <summary>Gets the shape of the station.</summary>
    public FeedStationShape Shape { get; }

    /// <summary>Gets the items waiting to be processed.</summary>
    public int Queued { get; }

    /// <summary>Gets the most items the station holds.</summary>
    public int MaxQueued { get; }

    /// <summary>Gets the fuel inside.</summary>
    public float Fuel { get; }

    /// <summary>Gets the most fuel the station holds.</summary>
    public float MaxFuel { get; }

    /// <summary>Gets the fuel item's prefab name, or null for a station that burns nothing.</summary>
    public string? FuelPrefab { get; }

    /// <summary>Gets the prefab names of the items it processes.</summary>
    public IReadOnlyList<string> Inputs { get; }

    /// <summary>Gets how many more items fit.</summary>
    public int QueueRoom => Math.Max(0, MaxQueued - Queued);

    /// <summary>Gets how many whole fuel items fit. A part-burnt item does not leave room for a whole one.</summary>
    public int FuelRoom
    {
        get
        {
            if (FuelPrefab is null)
            {
                return 0;
            }

            var d = MaxFuel - Fuel;
            if (float.IsNaN(d) || float.IsInfinity(d) || d < 1f)
            {
                return 0;
            }

            var w = Math.Floor(d);
            return w >= int.MaxValue ? int.MaxValue : (int)w;
        }
    }

    /// <summary>Creates the state of a smelter, kiln or windmill.</summary>
    /// <param name="queued">The items waiting to be processed, stored as given.</param>
    /// <param name="maxQueued">The most items the station holds, stored as given.</param>
    /// <param name="fuel">The fuel inside, stored as given.</param>
    /// <param name="maxFuel">The most fuel the station holds, stored as given.</param>
    /// <param name="fuelPrefab">The fuel item's prefab name, or null for a station that burns nothing.</param>
    /// <param name="inputs">The prefab names of the items it processes.</param>
    /// <returns>The state.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inputs"/> is null.</exception>
    public static FeedStationState SmelterClass(int queued, int maxQueued, float fuel, float maxFuel, string? fuelPrefab, IReadOnlyList<string> inputs) =>
        new FeedStationState(FeedStationShape.SmelterClass, queued, maxQueued, fuel, maxFuel, fuelPrefab, inputs ?? throw new ArgumentNullException(nameof(inputs)));

    /// <summary>Creates the state of a campfire or a standing torch, which takes fuel only.</summary>
    /// <param name="fuel">The fuel inside, stored as given.</param>
    /// <param name="maxFuel">The most fuel the station holds, stored as given.</param>
    /// <param name="fuelPrefab">The fuel item's prefab name.</param>
    /// <returns>The state.</returns>
    public static FeedStationState FuelOnly(float fuel, float maxFuel, string? fuelPrefab) =>
        new FeedStationState(FeedStationShape.FuelOnly, 0, 0, fuel, maxFuel, fuelPrefab, NoInputs);

    /// <summary>Creates the state of a fermenter, which holds one input at a time.</summary>
    /// <param name="occupied">True when the station already holds an input.</param>
    /// <param name="inputs">The prefab names of the items it accepts.</param>
    /// <returns>The state.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inputs"/> is null.</exception>
    public static FeedStationState SingleInput(bool occupied, IReadOnlyList<string> inputs) =>
        new FeedStationState(FeedStationShape.SingleInput, occupied ? 1 : 0, 1, 0f, 0f, null, inputs ?? throw new ArgumentNullException(nameof(inputs)));

    /// <summary>Creates the state of a cooking station, which has several slots that each hold one input.</summary>
    /// <param name="slotCount">The number of slots, stored as given.</param>
    /// <param name="occupiedSlots">The number of slots that hold an input, stored as given.</param>
    /// <param name="inputs">The prefab names of the items it accepts.</param>
    /// <returns>The state.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inputs"/> is null.</exception>
    public static FeedStationState SeveralInput(int slotCount, int occupiedSlots, IReadOnlyList<string> inputs) =>
        new FeedStationState(FeedStationShape.SeveralInput, occupiedSlots, slotCount, 0f, 0f, null, inputs ?? throw new ArgumentNullException(nameof(inputs)));

    /// <summary>Creates the state of an oven, which has several slots and also burns fuel.</summary>
    /// <param name="slotCount">The number of slots, stored as given.</param>
    /// <param name="occupiedSlots">The number of slots that hold an input, stored as given.</param>
    /// <param name="fuel">The fuel inside, stored as given.</param>
    /// <param name="maxFuel">The most fuel the station holds, stored as given.</param>
    /// <param name="fuelPrefab">The fuel item's prefab name, or null for a station that burns nothing.</param>
    /// <param name="inputs">The prefab names of the items it accepts.</param>
    /// <returns>The state.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inputs"/> is null.</exception>
    public static FeedStationState SeveralInputFuelled(int slotCount, int occupiedSlots, float fuel, float maxFuel, string? fuelPrefab, IReadOnlyList<string> inputs) =>
        new FeedStationState(FeedStationShape.SeveralInput, occupiedSlots, slotCount, fuel, maxFuel, fuelPrefab, inputs ?? throw new ArgumentNullException(nameof(inputs)));
}
