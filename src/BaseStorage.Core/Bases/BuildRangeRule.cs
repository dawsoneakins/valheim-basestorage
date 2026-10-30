namespace BaseStorage.Core.Bases;

using System;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The single build-range rule of Core (contract section 7.4). It mirrors vanilla Valheim's build-range check,
/// <c>CraftingStation.HaveBuildStationInRange</c>: horizontal distance in the X/Z plane, compared with the range using a strict less-than.
/// The rule is recorded in ADR-GWIN-016.
/// </summary>
internal static class BuildRangeRule
{
    /// <summary>
    /// Returns true when <paramref name="point"/> is strictly inside the build range of a station at <paramref name="stationPosition"/>.
    /// </summary>
    /// <param name="stationPosition">The position of the station that owns the build range.</param>
    /// <param name="buildRange">The build range of the station in metres.</param>
    /// <param name="point">The point being tested. Its Y component is ignored.</param>
    /// <returns>True when the horizontal distance is less than <paramref name="buildRange"/>; false at exactly the range.</returns>
    internal static bool IsInRange(WorldPosition stationPosition, float buildRange, WorldPosition point)
    {
        // ADR-GWIN-016: vanilla rule, horizontal and strict.
        var dx = stationPosition.X - point.X;
        var dz = stationPosition.Z - point.Z;
        return (float)Math.Sqrt((dx * dx) + (dz * dz)) < buildRange;
    }
}
