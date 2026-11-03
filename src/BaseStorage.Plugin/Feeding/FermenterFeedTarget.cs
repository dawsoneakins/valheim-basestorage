namespace BaseStorage.Plugin.Feeding;

using System.Collections.Generic;
using BaseStorage.Core.Feeding;
using BaseStorage.Shared;

/// <summary>A fermenter: it holds one mead base at a time, and only an empty one with a roof reports room.</summary>
internal sealed class FermenterFeedTarget : FeedTarget
{
    private readonly Fermenter fermenter;

    /// <summary>Initializes a new instance of the <see cref="FermenterFeedTarget"/> class.</summary>
    /// <param name="station">The fermenter.</param>
    internal FermenterFeedTarget(Fermenter station)
        : base(station, station.m_nview)
    {
        fermenter = station;
    }

    /// <inheritdoc/>
    internal override string Name => fermenter.m_name;

    /// <inheritdoc/>
    internal override FeedStationState State()
    {
        var inputs = new List<string>(fermenter.m_conversion.Count);
        foreach (Fermenter.ItemConversion conversion in fermenter.m_conversion)
        {
            if (conversion.m_from != null)
            {
                inputs.Add(conversion.m_from.gameObject.name);
            }
        }

        return FeedStationState.SingleInput(fermenter.GetStatus() != Fermenter.Status.Empty, inputs);
    }

    /// <inheritdoc/>
    internal override void CopyItems(List<ItemDrop> items)
    {
        foreach (Fermenter.ItemConversion conversion in fermenter.m_conversion)
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
    }

    /// <inheritdoc/>
    internal override void AddInput(string prefabName, bool cheated) =>
        View.InvokeRPC(PatchTargetNames.Fermenter.AddItemRpc, prefabName.GetStableHashCode(), cheated);
}
