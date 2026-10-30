namespace BaseStorage.Core.Availability;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>Turns a snapshot and a requirement list into the ordered takes that pay for them: inventory first, then chests nearest first, then smallest stacks first (ADR-GWIN-028).</summary>
/// <remarks>
/// The methods are pure. They never mutate their arguments, read a clock, log or do I/O, and are safe to call from any thread on immutable inputs.
/// </remarks>
public static class ConsumptionPlanner
{
    /// <summary>Plans the takes that pay for every requirement, or reports that they cannot all be paid.</summary>
    /// <param name="snapshot">The inventory, containers, assignments and options; must not be null.</param>
    /// <param name="requirements">The requirements to pay, in the order given; must not be null and must not contain null elements. May be empty.</param>
    /// <returns>The plan, or null when any requirement cannot be fully covered. Never a partial plan.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> or <paramref name="requirements"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="requirements"/> contains a null element.</exception>
    public static ConsumptionPlan? Plan(BaseSnapshot snapshot, IReadOnlyList<CraftRequirement> requirements)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (requirements is null)
        {
            throw new ArgumentNullException(nameof(requirements));
        }

        foreach (CraftRequirement requirement in requirements)
        {
            if (requirement is null)
            {
                throw new ArgumentException("Must not contain null elements.", nameof(requirements));
            }
        }

        AvailabilityResult availability = BaseAvailability.Compute(snapshot, requirements);
        if (!availability.AllMet)
        {
            return null;
        }

        List<ContainerSnapshot> sources = BuildSources(snapshot, availability);
        int[][] order = SortedStackOrder(sources);
        return Allocate(snapshot, requirements, sources, order);
    }

    /// <summary>Inventory first, then counted chests nearest first (ties by id).</summary>
    private static List<ContainerSnapshot> BuildSources(BaseSnapshot snapshot, AvailabilityResult availability)
    {
        WorldPosition origin = snapshot.PlayerInventory.Position;
        var chests = new List<ContainerSnapshot>(availability.CountedContainers);
        chests.Sort((a, b) =>
        {
            int byDistance = SquaredDistance(origin, a.Position).CompareTo(SquaredDistance(origin, b.Position));
            return byDistance != 0 ? byDistance : a.Id.CompareTo(b.Id);
        });

        var sources = new List<ContainerSnapshot>(chests.Count + 1) { snapshot.PlayerInventory };
        sources.AddRange(chests);
        return sources;
    }

    /// <summary>Per source, slot indices ordered smallest stack first (ties by slot).</summary>
    private static int[][] SortedStackOrder(IReadOnlyList<ContainerSnapshot> sources)
    {
        var order = new int[sources.Count][];
        for (int s = 0; s < sources.Count; s++)
        {
            IReadOnlyList<ItemStackSnapshot> contents = sources[s].Contents;
            var indices = new int[contents.Count];
            for (int k = 0; k < indices.Length; k++)
            {
                indices[k] = k;
            }

            Array.Sort(indices, (x, y) =>
            {
                int byCount = contents[x].Count.CompareTo(contents[y].Count);
                return byCount != 0 ? byCount : x.CompareTo(y);
            });
            order[s] = indices;
        }

        return order;
    }

    private static ConsumptionPlan? Allocate(
        BaseSnapshot snapshot,
        IReadOnlyList<CraftRequirement> requirements,
        List<ContainerSnapshot> sources,
        int[][] order)
    {
        var leftovers = new int[sources.Count][];
        for (int s = 0; s < sources.Count; s++)
        {
            IReadOnlyList<ItemStackSnapshot> contents = sources[s].Contents;
            leftovers[s] = new int[contents.Count];
            for (int k = 0; k < contents.Count; k++)
            {
                leftovers[s][k] = contents[k].Count;
            }
        }

        var takes = new List<ConsumptionTake>();
        var touched = new Dictionary<ContainerId, ContainerSnapshot>();
        for (int i = 0; i < requirements.Count; i++)
        {
            CraftRequirement requirement = requirements[i];
            int remaining = requirement.Amount;
            for (int s = 0; s < sources.Count && remaining > 0; s++)
            {
                ContainerSnapshot source = sources[s];
                foreach (int slot in order[s])
                {
                    if (remaining == 0)
                    {
                        break;
                    }

                    ItemStackSnapshot stack = source.Contents[slot];
                    if (leftovers[s][slot] <= 0 || !BaseAvailability.Matches(stack, requirement))
                    {
                        continue;
                    }

                    int taken = Math.Min(leftovers[s][slot], remaining);
                    leftovers[s][slot] -= taken;
                    remaining -= taken;
                    takes.Add(new ConsumptionTake(source.Id, slot, stack, taken, i));
                    if (s > 0)
                    {
                        touched[source.Id] = source;
                    }
                }
            }

            if (remaining > 0)
            {
                return null;
            }
        }

        var expected = new List<ContainerSnapshot>(touched.Values);
        expected.Sort((a, b) => a.Id.CompareTo(b.Id));
        return new ConsumptionPlan(
            snapshot.PlayerInventory.Id,
            new List<CraftRequirement>(requirements).AsReadOnly(),
            takes.AsReadOnly(),
            expected.AsReadOnly());
    }

    private static double SquaredDistance(WorldPosition a, WorldPosition b)
    {
        double dx = (double)a.X - b.X;
        double dz = (double)a.Z - b.Z;
        return (dx * dx) + (dz * dz);
    }
}
