namespace BaseStorage.Core.Feeding;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Decides the next transfer into a station. Pure. Fuel comes first, because a station with ore and no fuel stands idle, then the inputs
/// in the order the player chose them. A transfer never takes more than the station has room for, never takes a selected item's keep
/// amount, and comes from the nearest chest that can give it.
/// </summary>
public static class FeedPlanner
{
    /// <summary>Counts an item across the chests the player may use.</summary>
    /// <param name="chests">The base's chests.</param>
    /// <param name="prefabName">The item's prefab name.</param>
    /// <returns>The total; chests that deny access do not count.</returns>
    public static int CountInBase(IReadOnlyList<ContainerSnapshot> chests, string prefabName)
    {
        if (chests is null)
        {
            throw new ArgumentNullException(nameof(chests));
        }

        long total = 0;
        for (var i = 0; i < chests.Count; i++)
        {
            if ((chests[i].Flags & ContainerFlags.AccessDenied) == 0)
            {
                total += CountIn(chests[i], prefabName);
            }
        }

        return total > int.MaxValue ? int.MaxValue : (int)total;
    }

    /// <summary>Plans the next transfer.</summary>
    /// <param name="station">The station.</param>
    /// <param name="selection">The items it is kept fed with.</param>
    /// <param name="chests">The base's chests, with current contents.</param>
    /// <param name="stationPosition">Where the station is.</param>
    /// <param name="skip">Chests to leave alone for now.</param>
    /// <returns>The order, or null when there is nothing to do.</returns>
    public static FeedOrder? Next(
        FeedStationState station,
        FeedSelection selection,
        IReadOnlyList<ContainerSnapshot> chests,
        WorldPosition stationPosition,
        ICollection<ContainerId> skip)
    {
        if (station is null)
        {
            throw new ArgumentNullException(nameof(station));
        }

        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        if (chests is null)
        {
            throw new ArgumentNullException(nameof(chests));
        }

        if (skip is null)
        {
            throw new ArgumentNullException(nameof(skip));
        }

        if (station.FuelRoom > 0)
        {
            var fuel = selection.Find(station.FuelPrefab!);
            var order = fuel is null ? null : Plan(fuel, station.FuelRoom, true, chests, stationPosition, skip);
            if (order is not null)
            {
                return order;
            }
        }

        if (station.QueueRoom == 0)
        {
            return null;
        }

        for (var i = 0; i < selection.Entries.Count; i++)
        {
            var entry = selection.Entries[i];
            if (Accepts(station, entry.PrefabName))
            {
                var order = Plan(entry, station.QueueRoom, false, chests, stationPosition, skip);
                if (order is not null)
                {
                    return order;
                }
            }
        }

        return null;
    }

    private static bool Accepts(FeedStationState station, string prefabName)
    {
        for (var i = 0; i < station.Inputs.Count; i++)
        {
            if (string.Equals(station.Inputs[i], prefabName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static FeedOrder? Plan(
        FeedEntry entry,
        int room,
        bool isFuel,
        IReadOnlyList<ContainerSnapshot> chests,
        WorldPosition stationPosition,
        ICollection<ContainerId> skip)
    {
        var spare = CountInBase(chests, entry.PrefabName) - entry.Keep;
        if (spare <= 0)
        {
            return null;
        }

        ContainerSnapshot? nearest = null;
        var nearestDistance = float.MaxValue;
        for (var i = 0; i < chests.Count; i++)
        {
            var chest = chests[i];
            if ((chest.Flags & (ContainerFlags.AccessDenied | ContainerFlags.InUseByOther)) != 0
                || skip.Contains(chest.Id)
                || CountIn(chest, entry.PrefabName) == 0)
            {
                continue;
            }

            var distance = DistanceSquared(chest.Position, stationPosition);
            if (distance < nearestDistance)
            {
                nearest = chest;
                nearestDistance = distance;
            }
        }

        if (nearest is null)
        {
            return null;
        }

        var amount = Math.Min(Math.Min(room, spare), CountIn(nearest, entry.PrefabName));
        return new FeedOrder(nearest.Id, entry.PrefabName, amount, isFuel);
    }

    private static int CountIn(ContainerSnapshot chest, string prefabName)
    {
        var count = 0;
        for (var i = 0; i < chest.Contents.Count; i++)
        {
            if (string.Equals(chest.Contents[i].PrefabName, prefabName, StringComparison.Ordinal))
            {
                count += chest.Contents[i].Count;
            }
        }

        return count;
    }

    private static float DistanceSquared(WorldPosition a, WorldPosition b)
    {
        var x = a.X - b.X;
        var y = a.Y - b.Y;
        var z = a.Z - b.Z;
        return (x * x) + (y * y) + (z * z);
    }
}
