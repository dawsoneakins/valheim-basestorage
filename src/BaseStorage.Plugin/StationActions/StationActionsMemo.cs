namespace BaseStorage.Plugin.StationActions;

using System;
using BaseStorage.Core.Snapshots;

/// <summary>Remembers when the base facts of a station were last read, so the panel reads them at most once a second (contract S-E004-012 section 3.3).</summary>
internal sealed class StationActionsMemo
{
    /// <summary>The longest time the facts are kept before they are read again.</summary>
    internal static readonly TimeSpan FactsInterval = TimeSpan.FromSeconds(1);

    private bool hasRead;
    private StationId lastStation;
    private TimeSpan lastRead;

    /// <summary>Tells whether the facts must be read again for a station.</summary>
    /// <param name="station">The station shown.</param>
    /// <param name="now">The clock reading.</param>
    /// <returns>True for a new station, an elapsed interval, a clock that went back, or after an invalidation.</returns>
    internal bool FactsDue(StationId station, TimeSpan now) =>
        !hasRead || !station.Equals(lastStation) || now < lastRead || now - lastRead >= FactsInterval;

    /// <summary>Records a facts read.</summary>
    /// <param name="station">The station read.</param>
    /// <param name="now">The clock reading.</param>
    internal void MarkRead(StationId station, TimeSpan now)
    {
        hasRead = true;
        lastStation = station;
        lastRead = now;
    }

    /// <summary>Forces the next <see cref="FactsDue"/> to return true.</summary>
    internal void Invalidate()
    {
        hasRead = false;
    }
}
