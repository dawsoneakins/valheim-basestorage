namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The null oracle of the S-QF-001 randomized test (contract 0.2 FM8 and FM9, ADR-GWIN-180). It recomputes from the rule text, without calling
/// the planner, whether a selected fuel or accepted input still has room, has spare above zero over the sources, and has a chest that can give it.
/// When the planner returns null and this oracle finds one, the planner stopped early.
/// </summary>
internal static class FeedFuzzOracle
{
    /// <summary>Finds the first selected item that still qualifies.</summary>
    /// <param name="model">The case.</param>
    /// <param name="chests">The chest snapshots of the case as they are now.</param>
    /// <param name="station">The station state of the case as it is now.</param>
    /// <returns>A description of the qualifying item, or null when nothing qualifies.</returns>
    internal static string? FirstQualifying(FeedFuzzCase model, ContainerSnapshot[] chests, FeedStationState station)
    {
        ContainerSnapshot[] sources = FeedFixtures.SourcesOf(chests, model.Assignments, model.Options);

        if (station.FuelPrefab is not null && station.FuelRoom > 0)
        {
            FeedEntry? fuel = model.Selection.Find(station.FuelPrefab);
            if (fuel is not null)
            {
                string? found = Qualifies(model, chests, sources, fuel, "fuel");
                if (found is not null)
                {
                    return found;
                }
            }
        }

        if (station.QueueRoom > 0)
        {
            foreach (FeedEntry entry in model.Selection.Entries)
            {
                if (!Accepts(station, entry.PrefabName))
                {
                    continue;
                }

                string? found = Qualifies(model, chests, sources, entry, "input");
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static string? Qualifies(FeedFuzzCase model, ContainerSnapshot[] chests, ContainerSnapshot[] sources, FeedEntry entry, string kind)
    {
        int stock = 0;
        foreach (ContainerSnapshot chest in sources)
        {
            if ((chest.Flags & ContainerFlags.AccessDenied) == 0)
            {
                stock += CountOf(chest, entry.PrefabName);
            }
        }

        int spare = stock - entry.Keep;
        if (spare <= 0)
        {
            return null;
        }

        foreach (ContainerSnapshot chest in chests)
        {
            bool isSource = Array.Exists(sources, source => ReferenceEquals(source, chest));
            bool denied = (chest.Flags & (ContainerFlags.AccessDenied | ContainerFlags.InUseByOther)) != 0;
            if (isSource && !denied && !model.Skip.Contains(chest.Id) && CountOf(chest, entry.PrefabName) > 0)
            {
                return $"{kind} {entry.PrefabName} has spare {spare} and chest {chest.Id} holds it";
            }
        }

        return null;
    }

    private static bool Accepts(FeedStationState station, string prefab)
    {
        foreach (string input in station.Inputs)
        {
            if (string.Equals(input, prefab, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static int CountOf(ContainerSnapshot chest, string prefab)
    {
        int count = 0;
        foreach (ItemStackSnapshot stack in chest.Contents)
        {
            if (string.Equals(stack.PrefabName, prefab, StringComparison.Ordinal))
            {
                count += stack.Count;
            }
        }

        return count;
    }
}
