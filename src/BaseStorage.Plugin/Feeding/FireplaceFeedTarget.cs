namespace BaseStorage.Plugin.Feeding;

using System.Collections.Generic;
using BaseStorage.Core.Feeding;

/// <summary>A campfire, hearth, bonfire, standing torch or other fire that burns the fuel it names.</summary>
internal sealed class FireplaceFeedTarget : FeedTarget
{
    private readonly Fireplace fire;

    /// <summary>Initializes a new instance of the <see cref="FireplaceFeedTarget"/> class.</summary>
    /// <param name="station">The fire.</param>
    internal FireplaceFeedTarget(Fireplace station)
        : base(station, station.m_nview)
    {
        fire = station;
    }

    /// <inheritdoc/>
    internal override string Name => fire.m_name;

    /// <summary>Tells whether a fire takes fuel at all: dungeon torches burn forever and never need it.</summary>
    /// <param name="station">The fire.</param>
    /// <returns>True when it can be fed.</returns>
    internal static bool Accepts(Fireplace station) => station.m_fuelItem != null && station.m_canRefill && !station.m_infiniteFuel;

    /// <inheritdoc/>
    internal override FeedStationState State()
    {
        ZDO? zdo = View.GetZDO();
        return FeedStationState.FuelOnly(zdo == null ? fire.m_maxFuel : zdo.GetFloat(ZDOVars.s_fuel, 0f), fire.m_maxFuel, fire.m_fuelItem.gameObject.name);
    }

    /// <inheritdoc/>
    internal override void CopyItems(List<ItemDrop> items) => items.Add(fire.m_fuelItem);

    /// <inheritdoc/>
    internal override void AddFuel(int amount) => fire.AddFuel(amount);

    /// <inheritdoc/>
    internal override void AddInput(string prefabName, bool cheated)
    {
    }
}
