namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;

/// <summary>The class of a chest for restock (S-QR-001 Q5, Q6).</summary>
internal enum OracleClass
{
    /// <summary>A usable source (Q5).</summary>
    Usable,

    /// <summary>In use by another player (Q6): counted, never a source.</summary>
    InUse,

    /// <summary>Ignored (Q5 (a) or (b)): never a source and counted nowhere.</summary>
    Ignored,
}

/// <summary>The totals and per-target rooms that the oracle recomputes for one call.</summary>
internal sealed class OracleOutcome
{
    /// <summary>Creates an outcome.</summary>
    /// <param name="targetRoom">Per carried entry: the room left after the fill, or -1 when the entry is not a target.</param>
    /// <param name="movedItems">The items that the fill moves.</param>
    /// <param name="toppedUpStacks">The number of targets that receive at least one item.</param>
    /// <param name="remainingRoom">The sum of the rooms left over the targets.</param>
    /// <param name="leftInUse">The Q10 count.</param>
    /// <param name="leftDifferentIdentity">The Q11 count.</param>
    internal OracleOutcome(int[] targetRoom, int movedItems, int toppedUpStacks, int remainingRoom, int leftInUse, int leftDifferentIdentity)
    {
        TargetRoom = targetRoom;
        MovedItems = movedItems;
        ToppedUpStacks = toppedUpStacks;
        RemainingRoom = remainingRoom;
        LeftInUse = leftInUse;
        LeftDifferentIdentity = leftDifferentIdentity;
    }

    /// <summary>Gets the room left per carried entry, or -1 for a non-target.</summary>
    internal int[] TargetRoom { get; }

    /// <summary>Gets the items moved.</summary>
    internal int MovedItems { get; }

    /// <summary>Gets the number of topped-up targets.</summary>
    internal int ToppedUpStacks { get; }

    /// <summary>Gets the sum of the target rooms left.</summary>
    internal int RemainingRoom { get; }

    /// <summary>Gets the items left in in-use chests.</summary>
    internal int LeftInUse { get; }

    /// <summary>Gets the items left because only a different identity was available.</summary>
    internal int LeftDifferentIdentity { get; }
}

/// <summary>
/// An independent oracle for the restock rules, written from the rule text of S-QR-001 contract 0.2 and 5.1 (ADR-GWIN-172).
/// It never calls <see cref="RestockPlanner"/>. It uses no buckets and no sort: the Q7 order is found by selecting the least
/// remaining candidate each time, and eligibility comes from the flags directly, as ContainerEligibility's table reads.
/// </summary>
internal static class RestockOracle
{
    /// <summary>Classifies a chest by Q5 and Q6, reading the flags directly.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="isKeep">True when the chest is assigned Keep.</param>
    /// <param name="options">The eligibility options.</param>
    /// <returns>Usable, InUse or Ignored.</returns>
    internal static OracleClass Classify(ContainerSnapshot chest, bool isKeep, EligibilityOptions options)
    {
        ContainerFlags flags = chest.Flags;
        if ((flags & ContainerFlags.Tombstone) != 0)
        {
            return OracleClass.Ignored;
        }

        if (options.ExcludeVehicles && (flags & ContainerFlags.Vehicle) != 0)
        {
            return OracleClass.Ignored;
        }

        if ((flags & ContainerFlags.PlayerBuilt) == 0)
        {
            return OracleClass.Ignored;
        }

        if ((flags & ContainerFlags.AccessDenied) != 0)
        {
            return OracleClass.Ignored;
        }

        if (isKeep && !options.KeepChestsCraftable)
        {
            return OracleClass.Ignored;
        }

        if (chest.Contents.Count > chest.SlotCount)
        {
            return OracleClass.Ignored;
        }

        foreach (ItemStackSnapshot stack in chest.Contents)
        {
            if (stack.Count > stack.MaxStack)
            {
                return OracleClass.Ignored;
            }
        }

        return (flags & ContainerFlags.InUseByOther) != 0 ? OracleClass.InUse : OracleClass.Usable;
    }

    /// <summary>Reads whether a chest is assigned Keep. A missing id means not Keep.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="assignments">The assignments.</param>
    /// <returns>True when the chest is Keep.</returns>
    internal static bool IsKeep(ContainerSnapshot chest, IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments) =>
        assignments.TryGetValue(chest.Id, out CategoryAssignment? assignment) && assignment.IsKeep;

    /// <summary>Q3: a carried entry is a target when it is hotbar or favourite-type, its max stack is above 1 and it is below max. The star bit is ignored.</summary>
    /// <param name="entry">The carried entry.</param>
    /// <returns>True when the entry is a target.</returns>
    internal static bool IsTarget(CarriedStack entry)
    {
        int relevant = (int)CarriedMarks.Hotbar | (int)CarriedMarks.FavouriteType;
        return (((int)entry.Marks) & relevant) != 0
            && entry.Stack.MaxStack > 1
            && entry.Stack.Count < entry.Stack.MaxStack;
    }

    /// <summary>Q1: the same key is the same prefab, quality, max stack and identity, whatever the count.</summary>
    /// <param name="a">The first stack.</param>
    /// <param name="b">The second stack.</param>
    /// <returns>True when the keys are equal.</returns>
    internal static bool SameKey(ItemStackSnapshot a, ItemStackSnapshot b) =>
        SameItem(a, b) && a.Identity.Equals(b.Identity);

    /// <summary>Q2: the same item is the same prefab, quality and max stack, whatever the identity.</summary>
    /// <param name="a">The first stack.</param>
    /// <param name="b">The second stack.</param>
    /// <returns>True when the items are equal.</returns>
    internal static bool SameItem(ItemStackSnapshot a, ItemStackSnapshot b) =>
        string.Equals(a.PrefabName, b.PrefabName, StringComparison.Ordinal) && a.Quality == b.Quality && a.MaxStack == b.MaxStack;

    /// <summary>The A11 distance: squared Euclidean over X, Y and Z, in double.</summary>
    /// <param name="from">The origin.</param>
    /// <param name="to">The chest position.</param>
    /// <returns>The squared distance.</returns>
    internal static double Distance2(WorldPosition from, WorldPosition to)
    {
        double dx = (double)from.X - (double)to.X;
        double dy = (double)from.Y - (double)to.Y;
        double dz = (double)from.Z - (double)to.Z;
        return (dx * dx) + (dy * dy) + (dz * dz);
    }

    /// <summary>Recomputes the outcome of one call from the rule text.</summary>
    /// <param name="carried">The carried entries.</param>
    /// <param name="origin">The origin.</param>
    /// <param name="containers">The chests in snapshot order.</param>
    /// <param name="assignments">The assignments.</param>
    /// <param name="options">The options.</param>
    /// <returns>The outcome.</returns>
    internal static OracleOutcome Compute(
        IReadOnlyList<CarriedStack> carried,
        WorldPosition origin,
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        EligibilityOptions options)
    {
        int n = carried.Count;
        var isTarget = new bool[n];
        var room = new long[n];
        for (int t = 0; t < n; t++)
        {
            if (IsTarget(carried[t]))
            {
                isTarget[t] = true;
                room[t] = (long)carried[t].Stack.MaxStack - carried[t].Stack.Count;
            }
        }

        var classes = new OracleClass[containers.Count];
        for (int c = 0; c < containers.Count; c++)
        {
            classes[c] = Classify(containers[c], IsKeep(containers[c], assignments), options);
        }

        // Q7 candidates: stacks of usable chests, in snapshot order. Their order is found by selection below.
        var candidates = new List<Candidate>();
        for (int c = 0; c < containers.Count; c++)
        {
            if (classes[c] != OracleClass.Usable)
            {
                continue;
            }

            for (int s = 0; s < containers[c].Contents.Count; s++)
            {
                ItemStackSnapshot item = containers[c].Contents[s];
                candidates.Add(new Candidate(c, s, item, Distance2(origin, containers[c].Position)));
            }
        }

        // Q8: targets in carried order, each walks its same-key candidates in Q7 order, least remaining first.
        var taken = new long[n];
        for (int t = 0; t < n; t++)
        {
            if (!isTarget[t])
            {
                continue;
            }

            var visited = new bool[candidates.Count];
            while (room[t] > 0)
            {
                int best = -1;
                for (int k = 0; k < candidates.Count; k++)
                {
                    if (!visited[k] && SameKey(candidates[k].Item, carried[t].Stack)
                        && (best < 0 || Precedes(candidates[k], candidates[best])))
                    {
                        best = k;
                    }
                }

                if (best < 0)
                {
                    break;
                }

                visited[best] = true;
                long take = Math.Min(room[t], candidates[best].Work);
                if (take > 0)
                {
                    candidates[best].Work -= (int)take;
                    room[t] -= take;
                    taken[t] += take;
                }
            }
        }

        // Q10 and Q11, per key and per item, with the first target of each group as its representative.
        var keyReps = new List<int>();
        for (int t = 0; t < n; t++)
        {
            if (isTarget[t] && !AnyRepSameKey(keyReps, carried, t))
            {
                keyReps.Add(t);
            }
        }

        var keyNeed = new long[keyReps.Count];
        var keyLeftOver = new long[keyReps.Count];
        long leftInUse = 0;
        for (int g = 0; g < keyReps.Count; g++)
        {
            ItemStackSnapshot rep = carried[keyReps[g]].Stack;
            long need = 0;
            for (int t = 0; t < n; t++)
            {
                if (isTarget[t] && SameKey(carried[t].Stack, rep))
                {
                    need += room[t];
                }
            }

            long busy = 0;
            for (int c = 0; c < containers.Count; c++)
            {
                if (classes[c] != OracleClass.InUse)
                {
                    continue;
                }

                foreach (ItemStackSnapshot item in containers[c].Contents)
                {
                    if (SameKey(item, rep))
                    {
                        busy += item.Count;
                    }
                }
            }

            keyNeed[g] = need;
            leftInUse += Math.Min(need, busy);
            keyLeftOver[g] = need - Math.Min(need, busy);
        }

        long leftDifferentIdentity = 0;
        var itemReps = new List<int>();
        for (int g = 0; g < keyReps.Count; g++)
        {
            bool seen = false;
            foreach (int earlier in itemReps)
            {
                if (SameItem(carried[earlier].Stack, carried[keyReps[g]].Stack))
                {
                    seen = true;
                    break;
                }
            }

            if (!seen)
            {
                itemReps.Add(keyReps[g]);
            }
        }

        foreach (int itemRep in itemReps)
        {
            ItemStackSnapshot rep = carried[itemRep].Stack;
            long needI = 0;
            for (int g = 0; g < keyReps.Count; g++)
            {
                if (SameItem(carried[keyReps[g]].Stack, rep))
                {
                    needI += keyLeftOver[g];
                }
            }

            long spareI = 0;
            foreach (Candidate candidate in candidates)
            {
                if (SameItem(candidate.Item, rep))
                {
                    spareI += candidate.Work;
                }
            }

            leftDifferentIdentity += Math.Min(needI, spareI);
        }

        long remainingRoom = 0;
        long movedItems = 0;
        int toppedUp = 0;
        var targetRoom = new int[n];
        for (int t = 0; t < n; t++)
        {
            if (!isTarget[t])
            {
                targetRoom[t] = -1;
                continue;
            }

            targetRoom[t] = (int)room[t];
            remainingRoom += room[t];
            movedItems += taken[t];
            if (taken[t] > 0)
            {
                toppedUp++;
            }
        }

        return new OracleOutcome(
            targetRoom,
            Clamp(movedItems),
            toppedUp,
            Clamp(remainingRoom),
            Clamp(leftInUse),
            Clamp(leftDifferentIdentity));
    }

    /// <summary>Q13: after the moves, no usable same-key source stack of a target with room holds a working count above 0.</summary>
    /// <param name="carried">The carried entries.</param>
    /// <param name="containers">The chests in snapshot order.</param>
    /// <param name="assignments">The assignments.</param>
    /// <param name="options">The options.</param>
    /// <param name="moves">The planned moves.</param>
    /// <returns>True when the plan is maximal.</returns>
    internal static bool IsMaximal(
        IReadOnlyList<CarriedStack> carried,
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        EligibilityOptions options,
        IReadOnlyList<RestockMove> moves)
    {
        for (int t = 0; t < carried.Count; t++)
        {
            if (!IsTarget(carried[t]))
            {
                continue;
            }

            long received = 0;
            foreach (RestockMove move in moves)
            {
                if (move.TargetIndex == t)
                {
                    received += move.Count;
                }
            }

            if (carried[t].Stack.Count + received >= carried[t].Stack.MaxStack)
            {
                continue;
            }

            for (int c = 0; c < containers.Count; c++)
            {
                if (Classify(containers[c], IsKeep(containers[c], assignments), options) != OracleClass.Usable)
                {
                    continue;
                }

                for (int s = 0; s < containers[c].Contents.Count; s++)
                {
                    ItemStackSnapshot item = containers[c].Contents[s];
                    if (!SameKey(item, carried[t].Stack))
                    {
                        continue;
                    }

                    long drawn = 0;
                    foreach (RestockMove move in moves)
                    {
                        if (move.Source == containers[c].Id && move.SourceStackIndex == s)
                        {
                            drawn += move.Count;
                        }
                    }

                    if (item.Count - drawn > 0)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>The Q7 total order: distance, then chest snapshot index, then input count descending, then stack index.</summary>
    /// <param name="a">The first candidate.</param>
    /// <param name="b">The second candidate.</param>
    /// <returns>True when a comes before b.</returns>
    private static bool Precedes(Candidate a, Candidate b)
    {
        if (a.Distance != b.Distance)
        {
            return a.Distance < b.Distance;
        }

        if (a.Chest != b.Chest)
        {
            return a.Chest < b.Chest;
        }

        if (a.Item.Count != b.Item.Count)
        {
            return a.Item.Count > b.Item.Count;
        }

        return a.Stack < b.Stack;
    }

    private static bool AnyRepSameKey(List<int> reps, IReadOnlyList<CarriedStack> carried, int t)
    {
        foreach (int rep in reps)
        {
            if (SameKey(carried[rep].Stack, carried[t].Stack))
            {
                return true;
            }
        }

        return false;
    }

    private static int Clamp(long value) => value > int.MaxValue ? int.MaxValue : (int)value;

    /// <summary>A stack of a usable chest, with its working count (the oracle's own state).</summary>
    private sealed class Candidate
    {
        internal Candidate(int chest, int stack, ItemStackSnapshot item, double distance)
        {
            Chest = chest;
            Stack = stack;
            Item = item;
            Distance = distance;
            Work = item.Count;
        }

        internal int Chest { get; }

        internal int Stack { get; }

        internal ItemStackSnapshot Item { get; }

        internal double Distance { get; }

        internal int Work { get; set; }
    }
}
