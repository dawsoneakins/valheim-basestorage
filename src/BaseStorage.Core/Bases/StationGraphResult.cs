namespace BaseStorage.Core.Bases;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Snapshots;

/// <summary>The stations of one base, as found by <see cref="StationGraph.FindBase"/>, and whether the fill was capped.</summary>
public sealed class StationGraphResult
{
    internal StationGraphResult(StationSnapshot[] stations, bool isCapped)
    {
        Stations = new ReadOnlyCollection<StationSnapshot>(stations);
        IsCapped = isCapped;
    }

    /// <summary>Gets the stations of the base in fill order; never null, and read-only.</summary>
    public IReadOnlyList<StationSnapshot> Stations { get; }

    /// <summary>Gets a value indicating whether more stations were connected than the cap allowed.</summary>
    public bool IsCapped { get; }
}
