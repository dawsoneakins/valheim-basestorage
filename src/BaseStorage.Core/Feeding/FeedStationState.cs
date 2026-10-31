namespace BaseStorage.Core.Feeding;

using System;
using System.Collections.Generic;

/// <summary>What a station holds and accepts at one moment.</summary>
public sealed class FeedStationState
{
    /// <summary>Creates a state.</summary>
    /// <param name="queued">The items waiting to be processed.</param>
    /// <param name="maxQueued">The most items the station holds.</param>
    /// <param name="fuel">The fuel inside, which the game counts in fractions.</param>
    /// <param name="maxFuel">The most fuel the station holds.</param>
    /// <param name="fuelPrefab">The fuel item's prefab name, or null for a station that burns nothing.</param>
    /// <param name="inputs">The prefab names of the items it processes.</param>
    public FeedStationState(int queued, int maxQueued, float fuel, int maxFuel, string? fuelPrefab, IReadOnlyList<string> inputs)
    {
        Queued = queued;
        MaxQueued = maxQueued;
        Fuel = fuel;
        MaxFuel = maxFuel;
        FuelPrefab = fuelPrefab;
        Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
    }

    /// <summary>Gets the items waiting to be processed.</summary>
    public int Queued { get; }

    /// <summary>Gets the most items the station holds.</summary>
    public int MaxQueued { get; }

    /// <summary>Gets the fuel inside.</summary>
    public float Fuel { get; }

    /// <summary>Gets the most fuel the station holds.</summary>
    public int MaxFuel { get; }

    /// <summary>Gets the fuel item's prefab name, or null for a station that burns nothing.</summary>
    public string? FuelPrefab { get; }

    /// <summary>Gets the prefab names of the items it processes.</summary>
    public IReadOnlyList<string> Inputs { get; }

    /// <summary>Gets how many more items fit.</summary>
    public int QueueRoom => Math.Max(0, MaxQueued - Queued);

    /// <summary>Gets how many whole fuel items fit. A part-burnt item does not leave room for a whole one.</summary>
    public int FuelRoom => FuelPrefab is null ? 0 : Math.Max(0, (int)Math.Floor(MaxFuel - Fuel));
}
