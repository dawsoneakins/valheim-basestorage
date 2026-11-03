namespace BaseStorage.Plugin.Feeding;

using System.Collections.Generic;
using BaseStorage.Core.Feeding;
using BaseStorage.Shared;

/// <summary>A cooking station, iron cooking station or oven: each slot holds one item, and an oven also burns fuel.</summary>
internal sealed class CookingFeedTarget : FeedTarget
{
    private readonly CookingStation cooker;

    /// <summary>Initializes a new instance of the <see cref="CookingFeedTarget"/> class.</summary>
    /// <param name="station">The station.</param>
    internal CookingFeedTarget(CookingStation station)
        : base(station, station.m_nview)
    {
        cooker = station;
    }

    /// <inheritdoc/>
    internal override string Name => cooker.m_name;

    /// <summary>Gets a value indicating whether food put on now would cook: a station over a fire waits for the fire.</summary>
    internal override bool IsReady => !cooker.m_requireFire || cooker.IsFireLit();

    /// <inheritdoc/>
    internal override FeedStationState State()
    {
        var inputs = new List<string>(cooker.m_conversion.Count);
        foreach (CookingStation.ItemConversion conversion in cooker.m_conversion)
        {
            if (conversion.m_from != null)
            {
                inputs.Add(conversion.m_from.gameObject.name);
            }
        }

        int occupied = 0;
        for (int i = 0; i < cooker.m_slots.Length; i++)
        {
            cooker.GetSlot(i, out string item, out _, out _, out _);
            if (!string.IsNullOrEmpty(item))
            {
                occupied++;
            }
        }

        string? fuel = cooker.m_useFuel && cooker.m_fuelItem != null ? cooker.m_fuelItem.gameObject.name : null;
        return FeedStationState.SeveralInputFuelled(cooker.m_slots.Length, occupied, fuel == null ? 0f : cooker.GetFuel(), cooker.m_maxFuel, fuel, inputs);
    }

    /// <inheritdoc/>
    internal override void CopyItems(List<ItemDrop> items)
    {
        if (cooker.m_useFuel && cooker.m_fuelItem != null)
        {
            items.Add(cooker.m_fuelItem);
        }

        foreach (CookingStation.ItemConversion conversion in cooker.m_conversion)
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
        for (int i = 0; i < amount; i++)
        {
            View.InvokeRPC(PatchTargetNames.CookingStation.AddFuelRpc);
        }
    }

    /// <inheritdoc/>
    internal override void AddInput(string prefabName, bool cheated) =>
        View.InvokeRPC(PatchTargetNames.CookingStation.AddItemRpc, prefabName, cheated);
}
