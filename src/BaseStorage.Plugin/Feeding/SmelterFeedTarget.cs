namespace BaseStorage.Plugin.Feeding;

using System.Collections.Generic;
using BaseStorage.Core.Feeding;
using UnityEngine;

/// <summary>A smelter, kiln, blast furnace or windmill.</summary>
internal sealed class SmelterFeedTarget : FeedTarget
{
    private readonly Smelter smelter;

    /// <summary>Initializes a new instance of the <see cref="SmelterFeedTarget"/> class.</summary>
    /// <param name="station">The station.</param>
    internal SmelterFeedTarget(Smelter station)
        : base(station, station.m_nview)
    {
        smelter = station;
    }

    /// <inheritdoc/>
    internal override string Name => smelter.m_name;

    /// <inheritdoc/>
    internal override FeedStationState State()
    {
        var inputs = new List<string>(smelter.m_conversion.Count);
        foreach (Smelter.ItemConversion conversion in smelter.m_conversion)
        {
            if (conversion.m_from != null)
            {
                inputs.Add(conversion.m_from.gameObject.name);
            }
        }

        string? fuel = smelter.m_fuelItem == null ? null : smelter.m_fuelItem.gameObject.name;
        return FeedStationState.SmelterClass(smelter.GetQueueSize(), smelter.m_maxOre, smelter.GetFuel(), smelter.m_maxFuel, fuel, inputs);
    }

    /// <inheritdoc/>
    internal override void CopyItems(List<ItemDrop> items)
    {
        if (smelter.m_fuelItem != null)
        {
            items.Add(smelter.m_fuelItem);
        }

        foreach (Smelter.ItemConversion conversion in smelter.m_conversion)
        {
            if (conversion.m_from != null)
            {
                items.Add(conversion.m_from);
            }
        }
    }

    /// <inheritdoc/>
    internal override void AddFuel(int amount)
    {
        Transform spot = smelter.transform;
        smelter.SetFuel(smelter.GetFuel() + amount);
        smelter.m_fuelAddedEffects.Create(spot.position, spot.rotation, spot);
    }

    /// <inheritdoc/>
    internal override void AddInput(string prefabName, bool cheated) => smelter.QueueOre(prefabName, cheated);

    /// <inheritdoc/>
    internal override void InputsAdded()
    {
        Transform spot = smelter.transform;
        smelter.m_oreAddedEffects.Create(spot.position, spot.rotation);
    }
}
