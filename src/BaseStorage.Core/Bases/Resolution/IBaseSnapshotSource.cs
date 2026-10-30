namespace BaseStorage.Core.Bases.Resolution;

using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The world that <see cref="BaseCache"/> reads on each rebuild (ADR-GWIN-024). The Plugin registry implements it in S-E002-004.
/// The cache copies what it reads, so later changes to the returned lists do not reach a built snapshot.
/// </summary>
public interface IBaseSnapshotSource
{
    /// <summary>Returns the crafting stations of the world.</summary>
    /// <returns>The stations; must not be null and must not contain null elements.</returns>
    IReadOnlyList<StationSnapshot> GetStations();

    /// <summary>Returns the containers of the world.</summary>
    /// <returns>The containers; must not be null and must not contain null elements.</returns>
    IReadOnlyList<ContainerSnapshot> GetContainers();
}
