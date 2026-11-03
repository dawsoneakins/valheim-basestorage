namespace BaseStorage.Core.Restock;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>Plans the restock of carried stacks from one base's chests (S-QR-001, contract 4.1). Pure: no I/O, no clock, no Random (P1, P2).</summary>
public static class RestockPlanner
{
    private const byte ClassUsable = 0;
    private const byte ClassInUse = 1;
    private const byte ClassIgnored = 2;

    /// <summary>Plans a restock of the carried stacks from one base's chests. Pure; the inputs are not changed.</summary>
    /// <param name="carried">The carried stacks with their marks, in capture order. May be empty.</param>
    /// <param name="origin">The player's position; chest distance is measured from it.</param>
    /// <param name="containers">The base's member chests in snapshot order, unfiltered. May be empty.</param>
    /// <param name="assignments">The persisted assignment per chest id; only Keep matters. A missing id means not Keep.</param>
    /// <param name="options">The eligibility options; KeepChestsCraftable and ExcludeVehicles decide the sources.</param>
    /// <returns>The plan: the moves, the carried stacks after them, and the totals.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="carried"/>, <paramref name="containers"/> or <paramref name="assignments"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="carried"/> or <paramref name="containers"/> has a null element, <paramref name="containers"/> has a duplicate id, or <paramref name="assignments"/> has a null value.</exception>
    public static RestockPlan Plan(
        IReadOnlyList<CarriedStack> carried,
        WorldPosition origin,
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        EligibilityOptions options)
    {
        Validate(carried, containers, assignments);

        byte[] classes = Classify(containers, assignments, options);

        // Order usable source stacks once (Q7).
        var entries = new List<SourceEntry>();
        for (int c = 0; c < containers.Count; c++)
        {
            if (classes[c] != ClassUsable)
            {
                continue;
            }

            double d2 = SquaredDistance(origin, containers[c].Position);
            IReadOnlyList<ItemStackSnapshot> contents = containers[c].Contents;
            for (int s = 0; s < contents.Count; s++)
            {
                entries.Add(new SourceEntry(c, s, d2, contents[s].Count));
            }
        }

        SourceEntry[] sorted = entries.ToArray();
        Array.Sort(sorted, CompareEntries);

        int[] working = new int[sorted.Length];
        var buckets = new Dictionary<RestockStackKey, List<int>>();
        for (int i = 0; i < sorted.Length; i++)
        {
            working[i] = sorted[i].Count;
            var key = new RestockStackKey(containers[sorted[i].Chest].Contents[sorted[i].StackIndex]);
            if (!buckets.TryGetValue(key, out List<int>? bucket))
            {
                bucket = new List<int>();
                buckets.Add(key, bucket);
            }

            bucket.Add(i);
        }

        // Serve targets in carried order (Q8).
        int n = carried.Count;
        var moves = new List<RestockMove>();
        var received = new long[n];
        var isTarget = new bool[n];
        long movedItems = 0;
        for (int t = 0; t < n; t++)
        {
            ItemStackSnapshot target = carried[t].Stack;
            if (((int)carried[t].Marks & ((int)CarriedMarks.Hotbar | (int)CarriedMarks.FavouriteType)) == 0
                || target.MaxStack <= 1
                || target.Count >= target.MaxStack)
            {
                continue;
            }

            isTarget[t] = true;
            if (!buckets.TryGetValue(new RestockStackKey(target), out List<int>? list))
            {
                continue;
            }

            int room = target.MaxStack - target.Count;
            for (int j = 0; j < list.Count && room > 0; j++)
            {
                int idx = list[j];
                if (working[idx] <= 0)
                {
                    continue;
                }

                int take = Math.Min(room, working[idx]);
                working[idx] -= take;
                room -= take;
                received[t] += take;
                movedItems += take;
                SourceEntry e = sorted[idx];
                ContainerSnapshot chest = containers[e.Chest];
                moves.Add(new RestockMove(chest.Id, e.StackIndex, chest.Contents[e.StackIndex], t, target, take));
            }
        }

        // Build CarriedAfter and the totals.
        var after = new ItemStackSnapshot[n];
        long remainingRoom = 0;
        int topped = 0;
        var needByKey = new Dictionary<RestockStackKey, long>();
        var keyOrder = new List<RestockStackKey>();
        var itemKeyOrder = new List<RestockItemKey>();
        var itemKeysSeen = new HashSet<RestockItemKey>();
        for (int t = 0; t < n; t++)
        {
            ItemStackSnapshot s = carried[t].Stack;
            if (received[t] > 0)
            {
                after[t] = new ItemStackSnapshot(s.PrefabName, s.Count + (int)received[t], s.Quality, s.MaxStack, s.Identity);
                topped++;
            }
            else
            {
                after[t] = s;
            }

            if (!isTarget[t])
            {
                continue;
            }

            long need = s.MaxStack - after[t].Count;
            remainingRoom += need;
            var key = new RestockStackKey(s);
            if (needByKey.TryGetValue(key, out long existing))
            {
                needByKey[key] = existing + need;
            }
            else
            {
                needByKey.Add(key, need);
                keyOrder.Add(key);
            }

            var itemKey = new RestockItemKey(s);
            if (itemKeysSeen.Add(itemKey))
            {
                itemKeyOrder.Add(itemKey);
            }
        }

        // busy(K): in-use chests' input counts per stack key with targets.
        var busyByKey = new Dictionary<RestockStackKey, long>();
        for (int c = 0; c < containers.Count; c++)
        {
            if (classes[c] != ClassInUse)
            {
                continue;
            }

            IReadOnlyList<ItemStackSnapshot> contents = containers[c].Contents;
            for (int s = 0; s < contents.Count; s++)
            {
                var key = new RestockStackKey(contents[s]);
                if (!needByKey.ContainsKey(key))
                {
                    continue;
                }

                busyByKey.TryGetValue(key, out long b);
                busyByKey[key] = b + contents[s].Count;
            }
        }

        // spareI: usable working counts per item key with targets.
        var spareByItem = new Dictionary<RestockItemKey, long>();
        for (int i = 0; i < sorted.Length; i++)
        {
            if (working[i] <= 0)
            {
                continue;
            }

            var itemKey = new RestockItemKey(containers[sorted[i].Chest].Contents[sorted[i].StackIndex]);
            if (!itemKeysSeen.Contains(itemKey))
            {
                continue;
            }

            spareByItem.TryGetValue(itemKey, out long sp);
            spareByItem[itemKey] = sp + working[i];
        }

        long leftInUse = 0;
        var needIByItem = new Dictionary<RestockItemKey, long>();
        for (int k = 0; k < keyOrder.Count; k++)
        {
            RestockStackKey key = keyOrder[k];
            long need = needByKey[key];
            busyByKey.TryGetValue(key, out long busy);
            long inUse = Math.Min(need, busy);
            leftInUse += inUse;

            RestockItemKey itemKey = FindItemKey(carried, isTarget, key);
            needIByItem.TryGetValue(itemKey, out long ni);
            needIByItem[itemKey] = ni + (need - inUse);
        }

        long leftDifferent = 0;
        for (int k = 0; k < itemKeyOrder.Count; k++)
        {
            RestockItemKey itemKey = itemKeyOrder[k];
            needIByItem.TryGetValue(itemKey, out long needI);
            spareByItem.TryGetValue(itemKey, out long spareI);
            leftDifferent += Math.Min(needI, spareI);
        }

        return new RestockPlan(
            moves.ToArray(),
            after,
            Clamp(movedItems),
            topped,
            Clamp(remainingRoom),
            Clamp(leftInUse),
            Clamp(leftDifferent));
    }

    private static RestockItemKey FindItemKey(IReadOnlyList<CarriedStack> carried, bool[] isTarget, RestockStackKey key)
    {
        for (int t = 0; t < carried.Count; t++)
        {
            if (isTarget[t] && new RestockStackKey(carried[t].Stack).Equals(key))
            {
                return new RestockItemKey(carried[t].Stack);
            }
        }

        throw new InvalidOperationException("Unreachable: every key with need has a target.");
    }

    private static void Validate(
        IReadOnlyList<CarriedStack> carried,
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments)
    {
        if (carried is null)
        {
            throw new ArgumentNullException(nameof(carried));
        }

        for (int i = 0; i < carried.Count; i++)
        {
            if (carried[i] is null)
            {
                throw new ArgumentException("Must not contain a null element.", nameof(carried));
            }
        }

        if (containers is null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        var ids = new HashSet<ContainerId>();
        for (int i = 0; i < containers.Count; i++)
        {
            if (containers[i] is null)
            {
                throw new ArgumentException("Must not contain a null element.", nameof(containers));
            }
        }

        for (int i = 0; i < containers.Count; i++)
        {
            if (!ids.Add(containers[i].Id))
            {
                throw new ArgumentException("Must not contain a duplicate id.", nameof(containers));
            }
        }

        if (assignments is null)
        {
            throw new ArgumentNullException(nameof(assignments));
        }

        // Copy through the List constructor: a foreach would bind the non-generic IEnumerator, which the dependency pin forbids.
        var pairs = new List<KeyValuePair<ContainerId, CategoryAssignment>>(assignments);
        for (int i = 0; i < pairs.Count; i++)
        {
            if (pairs[i].Value is null)
            {
                throw new ArgumentException("Must not contain a null value.", nameof(assignments));
            }
        }
    }

    private static byte[] Classify(
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        EligibilityOptions options)
    {
        byte[] classes = new byte[containers.Count];
        for (int c = 0; c < containers.Count; c++)
        {
            ContainerSnapshot chest = containers[c];
            bool isKeep = assignments.TryGetValue(chest.Id, out CategoryAssignment? a) && a.IsKeep;
            if (ContainerEligibility.GetExclusionReason(chest, isKeep, ContainerPurpose.CraftAvailability, options) != null
                || !ContentsValid(chest))
            {
                classes[c] = ClassIgnored;
            }
            else if ((chest.Flags & ContainerFlags.InUseByOther) != 0)
            {
                classes[c] = ClassInUse;
            }
            else
            {
                classes[c] = ClassUsable;
            }
        }

        return classes;
    }

    private static bool ContentsValid(ContainerSnapshot chest)
    {
        if (chest.Contents.Count > chest.SlotCount)
        {
            return false;
        }

        for (int i = 0; i < chest.Contents.Count; i++)
        {
            if (chest.Contents[i].Count > chest.Contents[i].MaxStack)
            {
                return false;
            }
        }

        return true;
    }

    private static double SquaredDistance(WorldPosition a, WorldPosition b)
    {
        double dx = (double)a.X - (double)b.X;
        double dy = (double)a.Y - (double)b.Y;
        double dz = (double)a.Z - (double)b.Z;
        return (dx * dx) + (dy * dy) + (dz * dz);
    }

    private static int CompareEntries(SourceEntry a, SourceEntry b)
    {
        int r = a.D2.CompareTo(b.D2);
        if (r != 0)
        {
            return r;
        }

        r = a.Chest.CompareTo(b.Chest);
        if (r != 0)
        {
            return r;
        }

        r = b.Count.CompareTo(a.Count);
        return r != 0 ? r : a.StackIndex.CompareTo(b.StackIndex);
    }

    private static int Clamp(long value) => value > int.MaxValue ? int.MaxValue : (int)value;

    private readonly struct SourceEntry
    {
        internal SourceEntry(int chest, int stackIndex, double d2, int count)
        {
            Chest = chest;
            StackIndex = stackIndex;
            D2 = d2;
            Count = count;
        }

        internal int Chest { get; }

        internal int StackIndex { get; }

        internal double D2 { get; }

        internal int Count { get; }
    }
}
