namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Snapshots;

/// <summary>
/// An independent replay of a plan's moves in a slot model (contract P7 and P8, ADR-GWIN-039). It starts from the input contents
/// of the usable chests, applies <c>Moves</c> in list order, and asserts at every step that the source stack holds the item and
/// the count, and that no chest exceeds its slot count and no stack exceeds its max stack. It shares no code with the planner.
/// </summary>
internal static class PlanReplay
{
    /// <summary>The replay outcome: the state after each move, the final stacks and the peak slot use per usable chest.</summary>
    /// <param name="Steps">One map per move, after that move. Each value lists the chest's stacks in slot order, as describe-strings.</param>
    /// <param name="Final">The final stacks of each usable chest, in slot order (source stacks keep their input order).</param>
    /// <param name="Peak">The largest number of occupied slots of each usable chest over all steps.</param>
    internal sealed record ReplayResult(
        IReadOnlyList<IReadOnlyDictionary<ContainerId, string[]>> Steps,
        IReadOnlyDictionary<ContainerId, ItemStackSnapshot[]> Final,
        IReadOnlyDictionary<ContainerId, int> Peak);

    /// <summary>Replays the moves of a plan over the usable chests of an input, and asserts feasibility at every step.</summary>
    /// <param name="containers">The input containers of the plan, unchanged.</param>
    /// <param name="plan">The plan whose moves are replayed.</param>
    /// <returns>The replay outcome.</returns>
    internal static ReplayResult Replay(IReadOnlyList<ContainerSnapshot> containers, OrganizePlan plan)
    {
        var chests = new Dictionary<ContainerId, List<Slot?>>();
        var slotCounts = new Dictionary<ContainerId, int>();
        foreach (ContainerSnapshot container in containers)
        {
            if (plan.FindLayout(container.Id) is null)
            {
                continue;
            }

            var slots = new List<Slot?>();
            foreach (ItemStackSnapshot stack in container.Contents)
            {
                slots.Add(new Slot(stack.PrefabName, stack.Quality, stack.MaxStack, stack.Count));
            }

            chests[container.Id] = slots;
            slotCounts[container.Id] = container.SlotCount;
        }

        Assert.Equal(plan.Layouts.Count, chests.Count);

        var peak = new Dictionary<ContainerId, int>();
        foreach (ContainerId id in chests.Keys)
        {
            peak[id] = Occupied(chests[id]);
        }

        var steps = new List<IReadOnlyDictionary<ContainerId, string[]>>();
        int step = 0;
        foreach (PlannedMove move in plan.Moves)
        {
            step++;
            string where = "step " + step + " (" + move.Source.Id + "," + move.SourceStackIndex + " -> " + move.Destination.Id + ")";
            Assert.NotEqual(move.Source, move.Destination);
            Assert.True(chests.ContainsKey(move.Source), where + ": source is not a usable chest");
            Assert.True(chests.ContainsKey(move.Destination), where + ": destination is not a usable chest");
            Assert.True(move.Count >= 1, where + ": count must be at least 1");

            List<Slot?> source = chests[move.Source];
            Assert.True(move.SourceStackIndex >= 0 && move.SourceStackIndex < source.Count, where + ": stack index out of range");
            Slot? held = source[move.SourceStackIndex];
            Assert.True(held is not null, where + ": the source stack was already moved out");
            Slot stack = held!;
            Assert.True(
                stack.Prefab == move.PrefabName && stack.Quality == move.Quality && stack.MaxStack == move.MaxStack,
                where + ": the source stack is not the moved item");
            Assert.True(stack.Count >= move.Count, where + ": the source stack holds fewer items than the move");

            Place(chests[move.Destination], slotCounts[move.Destination], move, where);

            stack.Count -= move.Count;
            if (stack.Count == 0)
            {
                source[move.SourceStackIndex] = null;
            }

            foreach (ContainerId id in chests.Keys)
            {
                AssertWithinCapacity(chests[id], slotCounts[id], where + " chest " + id.Id);
                peak[id] = Math.Max(peak[id], Occupied(chests[id]));
            }

            steps.Add(Describe(chests));
        }

        var final = new Dictionary<ContainerId, ItemStackSnapshot[]>();
        foreach (var pair in chests)
        {
            final[pair.Key] = pair.Value
                .Where(slot => slot is not null)
                .Select(slot => new ItemStackSnapshot(slot!.Prefab, slot!.Count, slot!.Quality, slot!.MaxStack))
                .ToArray();
        }

        return new ReplayResult(steps, final, peak);
    }

    /// <summary>Places a move's count into a destination: top up same-item stacks below max in slot order, then fill free slots (P8).</summary>
    private static void Place(List<Slot?> chest, int slotCount, PlannedMove move, string where)
    {
        int remaining = move.Count;
        foreach (Slot? slot in chest)
        {
            if (remaining == 0)
            {
                break;
            }

            if (slot is null || !SameItem(slot, move) || slot.Count >= slot.MaxStack)
            {
                continue;
            }

            int take = Math.Min(remaining, slot.MaxStack - slot.Count);
            slot.Count += take;
            remaining -= take;
        }

        while (remaining > 0)
        {
            Assert.True(Occupied(chest) < slotCount, where + ": the destination has no free slot for " + remaining + " more items");
            int take = Math.Min(remaining, move.MaxStack);
            var created = new Slot(move.PrefabName, move.Quality, move.MaxStack, take);
            int empty = chest.IndexOf(null);
            if (empty >= 0)
            {
                chest[empty] = created;
            }
            else
            {
                chest.Add(created);
            }

            remaining -= take;
        }
    }

    private static bool SameItem(Slot slot, PlannedMove move) =>
        slot.Prefab == move.PrefabName && slot.Quality == move.Quality && slot.MaxStack == move.MaxStack;

    private static void AssertWithinCapacity(List<Slot?> chest, int slotCount, string where)
    {
        Assert.True(Occupied(chest) <= slotCount, where + ": more occupied slots than the chest has");
        foreach (Slot? slot in chest)
        {
            if (slot is null)
            {
                continue;
            }

            Assert.True(slot.Count >= 1 && slot.Count <= slot.MaxStack, where + ": a stack is outside 1..MaxStack");
        }
    }

    private static int Occupied(List<Slot?> chest) => chest.Count(slot => slot is not null);

    private static IReadOnlyDictionary<ContainerId, string[]> Describe(Dictionary<ContainerId, List<Slot?>> chests)
    {
        var map = new Dictionary<ContainerId, string[]>();
        foreach (var pair in chests)
        {
            map[pair.Key] = pair.Value
                .Where(slot => slot is not null)
                .Select(slot => PlanAssert.Describe(slot!.Prefab, slot!.Count, slot!.Quality, slot!.MaxStack))
                .ToArray();
        }

        return map;
    }

    /// <summary>One occupied or freed slot of the model. A null entry in a chest list is a free slot.</summary>
    private sealed class Slot
    {
        internal Slot(string prefab, int quality, int maxStack, int count)
        {
            Prefab = prefab;
            Quality = quality;
            MaxStack = maxStack;
            Count = count;
        }

        internal string Prefab { get; }

        internal int Quality { get; }

        internal int MaxStack { get; }

        internal int Count { get; set; }
    }
}
