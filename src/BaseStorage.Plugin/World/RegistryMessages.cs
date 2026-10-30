namespace BaseStorage.Plugin.World;

using System;
using System.Globalization;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Builds the single-line Debug texts of contract section 8 (D1 registry change, D2 base summary). Integers use the
/// invariant culture and ids render as UserId:Id. The lines are written only when the Detail category is enabled, so
/// nothing here runs in a default install. Game-free (W1).
/// </summary>
internal static class RegistryMessages
{
    /// <summary>Formats a D1 registry change line.</summary>
    /// <param name="kind">The registry. An undefined value renders as "object".</param>
    /// <param name="added">True for an add, false for a remove.</param>
    /// <param name="loaded">The registry count after the change. A negative value is rendered as is.</param>
    /// <returns>The line, for example "Registry: container added (37 loaded)".</returns>
    internal static string Changed(RegistryKind kind, bool added, int loaded)
    {
        string name = kind switch
        {
            RegistryKind.Container => "container",
            RegistryKind.Station => "station",
            _ => "object",
        };

        return "Registry: " + name + (added ? " added (" : " removed (")
            + loaded.ToString(CultureInfo.InvariantCulture) + " loaded)";
    }

    /// <summary>Formats the D2 base summary line for a station that has just been registered.</summary>
    /// <param name="station">The station whose base is summarised.</param>
    /// <param name="resolution">The base resolution for the station. Must not be null.</param>
    /// <param name="eligibility">The eligibility of the base containers. Must not be null.</param>
    /// <returns>The line, or "Base at station &lt;id&gt;: not in a base" when the station is not in a base.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolution"/> or <paramref name="eligibility"/> is null.</exception>
    internal static string BaseSummary(StationId station, BaseResolution resolution, EligibilityResult eligibility)
    {
        if (resolution is null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        if (eligibility is null)
        {
            throw new ArgumentNullException(nameof(eligibility));
        }

        string prefix = "Base at station " + station.UserId.ToString(CultureInfo.InvariantCulture) + ":"
            + station.Id.ToString(CultureInfo.InvariantCulture);
        if (!resolution.IsInBase)
        {
            return prefix + ": not in a base";
        }

        int tombstone = 0;
        int vehicle = 0;
        int notPlayerBuilt = 0;
        int accessDenied = 0;
        for (int i = 0; i < eligibility.Excluded.Count; i++)
        {
            switch (eligibility.Excluded[i].Reason)
            {
                case ExclusionReason.Tombstone:
                    tombstone++;
                    break;
                case ExclusionReason.Vehicle:
                    vehicle++;
                    break;
                case ExclusionReason.NotPlayerBuilt:
                    notPlayerBuilt++;
                    break;
                case ExclusionReason.AccessDenied:
                    accessDenied++;
                    break;
                default:
                    break;
            }
        }

        int inUse = 0;
        for (int i = 0; i < resolution.Containers.Count; i++)
        {
            if ((resolution.Containers[i].Flags & ContainerFlags.InUseByOther) != 0)
            {
                inUse++;
            }
        }

        return prefix + ": "
            + resolution.Stations.Count.ToString(CultureInfo.InvariantCulture) + " station(s), "
            + resolution.Containers.Count.ToString(CultureInfo.InvariantCulture) + " container(s), "
            + eligibility.Eligible.Count.ToString(CultureInfo.InvariantCulture) + " eligible; excluded "
            + tombstone.ToString(CultureInfo.InvariantCulture) + " tombstone, "
            + vehicle.ToString(CultureInfo.InvariantCulture) + " vehicle, "
            + notPlayerBuilt.ToString(CultureInfo.InvariantCulture) + " not player-built, "
            + accessDenied.ToString(CultureInfo.InvariantCulture) + " access denied; "
            + inUse.ToString(CultureInfo.InvariantCulture) + " in use by another"
            + (resolution.IsCapped ? " (capped)" : string.Empty);
    }
}
