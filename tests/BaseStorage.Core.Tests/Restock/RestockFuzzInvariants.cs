namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;

/// <summary>The F1 to F8 invariants of S-QR-001 contract 7.10, checked on one case. Each returned message starts with its invariant id.</summary>
internal static class RestockFuzzInvariants
{
    /// <summary>Checks F1 to F8 for one case.</summary>
    /// <param name="input">The case, as generated.</param>
    /// <param name="plan">The plan from the first call.</param>
    /// <param name="again">The plan from a second call on the same inputs.</param>
    /// <param name="fresh">The same case regenerated from its seed, as a copy of the inputs before the call.</param>
    /// <param name="expected">The oracle outcome for the same inputs.</param>
    /// <returns>The failures, empty when every invariant holds.</returns>
    internal static List<string> Check(FuzzCase input, RestockPlan plan, RestockPlan again, FuzzCase fresh, OracleOutcome expected)
    {
        var failures = new List<string>();
        IReadOnlyList<RestockMove> moves = plan.Moves;
        int n = input.Carried.Count;

        CheckF1(input, moves, failures);
        CheckF2(input, moves, failures);
        CheckF3(input, moves, failures);
        CheckF4(input, plan, failures);
        CheckF5(input, moves, failures);

        if (!RestockOracle.IsMaximal(input.Carried, input.Containers, input.Assignments, input.Options, moves))
        {
            failures.Add("F6: the plan is not maximal (a target with room has a usable same-key source left)");
        }

        CheckF7(input, plan, expected, failures);
        CheckF8(input, plan, again, fresh, failures);

        if (n != plan.CarriedAfter.Count)
        {
            failures.Add("F4: CarriedAfter.Count " + plan.CarriedAfter.Count + " differs from carried.Count " + n);
        }

        return failures;
    }

    private static int ChestIndex(FuzzCase input, RestockMove move)
    {
        for (int c = 0; c < input.Containers.Count; c++)
        {
            if (input.Containers[c].Id == move.Source)
            {
                return c;
            }
        }

        return -1;
    }

    private static void CheckF1(FuzzCase input, IReadOnlyList<RestockMove> moves, List<string> failures)
    {
        foreach (RestockMove move in moves)
        {
            if (move.Count < 1)
            {
                failures.Add("F1: a move has Count " + move.Count);
            }

            int c = ChestIndex(input, move);
            if (c < 0)
            {
                failures.Add("F1: a move's source chest is not an input chest");
                continue;
            }

            if (move.SourceStackIndex < 0 || move.SourceStackIndex >= input.Containers[c].Contents.Count
                || !ReferenceEquals(move.SourceStack, input.Containers[c].Contents[move.SourceStackIndex]))
            {
                failures.Add("F1: SourceStack is not the input stack at its index");
            }

            if (move.TargetIndex < 0 || move.TargetIndex >= input.Carried.Count
                || !ReferenceEquals(move.TargetStack, input.Carried[move.TargetIndex].Stack))
            {
                failures.Add("F1: TargetStack is not the carried stack at TargetIndex");
            }
            else if (!RestockOracle.SameKey(move.SourceStack, move.TargetStack))
            {
                failures.Add("F1: a move joins two different stack keys");
            }
        }
    }

    private static void CheckF2(FuzzCase input, IReadOnlyList<RestockMove> moves, List<string> failures)
    {
        foreach (RestockMove move in moves)
        {
            if (move.TargetIndex >= 0 && move.TargetIndex < input.Carried.Count && !RestockOracle.IsTarget(input.Carried[move.TargetIndex]))
            {
                failures.Add("F2: a move targets carried[" + move.TargetIndex + "], which is not a target (Q3)");
            }

            int c = ChestIndex(input, move);
            if (c >= 0)
            {
                bool keep = RestockOracle.IsKeep(input.Containers[c], input.Assignments);
                if (RestockOracle.Classify(input.Containers[c], keep, input.Options) != OracleClass.Usable)
                {
                    failures.Add("F2: a move draws from a chest that is not a usable source (Q5)");
                }
            }
        }
    }

    private static void CheckF3(FuzzCase input, IReadOnlyList<RestockMove> moves, List<string> failures)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            int c = ChestIndex(input, moves[i]);
            if (c < 0 || moves[i].SourceStackIndex < 0 || moves[i].SourceStackIndex >= input.Containers[c].Contents.Count)
            {
                continue;
            }

            long drawn = 0;
            foreach (RestockMove other in moves)
            {
                if (other.Source == moves[i].Source && other.SourceStackIndex == moves[i].SourceStackIndex)
                {
                    drawn += other.Count;
                }
            }

            if (drawn > input.Containers[c].Contents[moves[i].SourceStackIndex].Count)
            {
                failures.Add("F3: the moves from one source stack exceed its input Count");
                return;
            }
        }
    }

    private static void CheckF4(FuzzCase input, RestockPlan plan, List<string> failures)
    {
        IReadOnlyList<RestockMove> moves = plan.Moves;
        IReadOnlyList<ItemStackSnapshot> after = plan.CarriedAfter;
        if (after.Count != input.Carried.Count)
        {
            failures.Add("F4: CarriedAfter.Count differs from carried.Count");
            return;
        }

        for (int t = 0; t < input.Carried.Count; t++)
        {
            ItemStackSnapshot original = input.Carried[t].Stack;
            long received = 0;
            foreach (RestockMove move in moves)
            {
                if (move.TargetIndex == t)
                {
                    received += move.Count;
                }
            }

            if (received == 0)
            {
                if (!ReferenceEquals(after[t], original))
                {
                    failures.Add("F4: untouched carried[" + t + "] is not the same instance");
                }

                continue;
            }

            if (after[t].Count != original.Count + received)
            {
                failures.Add("F4: CarriedAfter[" + t + "].Count is not the input count plus its moves");
            }

            if (!RestockOracle.SameKey(after[t], original))
            {
                failures.Add("F4: CarriedAfter[" + t + "] changed its key (Q4)");
            }

            if (RestockOracle.IsTarget(input.Carried[t]) && after[t].Count > original.MaxStack)
            {
                failures.Add("F4: CarriedAfter[" + t + "].Count is above MaxStack");
            }
        }
    }

    private static void CheckF5(FuzzCase input, IReadOnlyList<RestockMove> moves, List<string> failures)
    {
        // A model of carried plus every chest stack, with counts that the moves change.
        var stacks = new List<ItemStackSnapshot>();
        var counts = new List<int>();
        var carriedAt = new int[input.Carried.Count];
        for (int t = 0; t < input.Carried.Count; t++)
        {
            stacks.Add(input.Carried[t].Stack);
            counts.Add(input.Carried[t].Stack.Count);
            carriedAt[t] = stacks.Count - 1;
        }

        var chestAt = new int[input.Containers.Count][];
        for (int c = 0; c < input.Containers.Count; c++)
        {
            chestAt[c] = new int[input.Containers[c].Contents.Count];
            for (int s = 0; s < input.Containers[c].Contents.Count; s++)
            {
                stacks.Add(input.Containers[c].Contents[s]);
                counts.Add(input.Containers[c].Contents[s].Count);
                chestAt[c][s] = stacks.Count - 1;
            }
        }

        var before = Totals(stacks, counts);

        foreach (RestockMove move in moves)
        {
            int c = ChestIndex(input, move);
            if (c < 0 || move.SourceStackIndex < 0 || move.SourceStackIndex >= chestAt[c].Length
                || move.TargetIndex < 0 || move.TargetIndex >= input.Carried.Count)
            {
                continue;
            }

            int source = chestAt[c][move.SourceStackIndex];
            int target = carriedAt[move.TargetIndex];
            counts[source] -= move.Count;
            if (counts[source] < 0)
            {
                failures.Add("F5: a source working count fell below 0");
                return;
            }

            counts[target] += move.Count;
            ItemStackSnapshot targetStack = input.Carried[move.TargetIndex].Stack;
            if (targetStack.Count <= targetStack.MaxStack && counts[target] > targetStack.MaxStack)
            {
                failures.Add("F5: a carried stack went above its MaxStack during the moves");
                return;
            }
        }

        var after = Totals(stacks, counts);
        foreach (KeyValuePair<ItemStackSnapshot, long> pair in before)
        {
            long total = 0;
            foreach (KeyValuePair<ItemStackSnapshot, long> other in after)
            {
                if (RestockOracle.SameKey(pair.Key, other.Key))
                {
                    total += other.Value;
                }
            }

            if (total != pair.Value)
            {
                failures.Add("F5: the total for " + pair.Key.PrefabName + " changed from " + pair.Value + " to " + total);
                return;
            }
        }
    }

    private static List<KeyValuePair<ItemStackSnapshot, long>> Totals(List<ItemStackSnapshot> stacks, List<int> counts)
    {
        var groups = new List<KeyValuePair<ItemStackSnapshot, long>>();
        for (int i = 0; i < stacks.Count; i++)
        {
            int g = -1;
            for (int k = 0; k < groups.Count; k++)
            {
                if (RestockOracle.SameKey(groups[k].Key, stacks[i]))
                {
                    g = k;
                    break;
                }
            }

            if (g < 0)
            {
                groups.Add(new KeyValuePair<ItemStackSnapshot, long>(stacks[i], counts[i]));
            }
            else
            {
                groups[g] = new KeyValuePair<ItemStackSnapshot, long>(groups[g].Key, groups[g].Value + counts[i]);
            }
        }

        return groups;
    }

    private static void CheckF7(FuzzCase input, RestockPlan plan, OracleOutcome expected, List<string> failures)
    {
        long moved = 0;
        var topped = new HashSet<int>();
        foreach (RestockMove move in plan.Moves)
        {
            moved += move.Count;
            topped.Add(move.TargetIndex);
        }

        if (plan.MovedItems != (int)Math.Min(moved, int.MaxValue))
        {
            failures.Add("F7: MovedItems " + plan.MovedItems + " is not the sum of the move counts");
        }

        if (plan.ToppedUpStacks != topped.Count)
        {
            failures.Add("F7: ToppedUpStacks " + plan.ToppedUpStacks + " is not the number of distinct targets");
        }

        long room = 0;
        for (int t = 0; t < input.Carried.Count && t < plan.CarriedAfter.Count; t++)
        {
            if (RestockOracle.IsTarget(input.Carried[t]))
            {
                room += input.Carried[t].Stack.MaxStack - (long)plan.CarriedAfter[t].Count;
            }
        }

        if (plan.RemainingRoom != (int)Math.Min(room, int.MaxValue))
        {
            failures.Add("F7: RemainingRoom " + plan.RemainingRoom + " is not the sum of the target rooms");
        }

        if (plan.LeftInUse < 0 || plan.LeftDifferentIdentity < 0)
        {
            failures.Add("F7: a left-behind count is negative");
        }

        if (plan.LeftBehind != plan.LeftInUse + plan.LeftDifferentIdentity)
        {
            failures.Add("F7: LeftBehind is not LeftInUse plus LeftDifferentIdentity");
        }

        if (plan.LeftBehind > plan.RemainingRoom)
        {
            failures.Add("F7: LeftBehind " + plan.LeftBehind + " is above RemainingRoom " + plan.RemainingRoom);
        }

        if (plan.LeftInUse != expected.LeftInUse)
        {
            failures.Add("F7: LeftInUse " + plan.LeftInUse + " differs from the oracle " + expected.LeftInUse);
        }

        if (plan.LeftDifferentIdentity != expected.LeftDifferentIdentity)
        {
            failures.Add("F7: LeftDifferentIdentity " + plan.LeftDifferentIdentity + " differs from the oracle " + expected.LeftDifferentIdentity);
        }

        if (plan.RemainingRoom != expected.RemainingRoom)
        {
            failures.Add("F7: RemainingRoom " + plan.RemainingRoom + " differs from the oracle " + expected.RemainingRoom);
        }

        if (plan.MovedItems != expected.MovedItems)
        {
            failures.Add("F7: MovedItems " + plan.MovedItems + " differs from the oracle " + expected.MovedItems);
        }
    }

    private static void CheckF8(FuzzCase input, RestockPlan plan, RestockPlan again, FuzzCase fresh, List<string> failures)
    {
        if (plan.Moves.Count != again.Moves.Count || plan.MovedItems != again.MovedItems || plan.LeftBehind != again.LeftBehind)
        {
            failures.Add("F8: a second call gives different totals or a different move count");
        }
        else
        {
            for (int i = 0; i < plan.Moves.Count; i++)
            {
                RestockMove a = plan.Moves[i];
                RestockMove b = again.Moves[i];
                if (a.Source != b.Source || a.SourceStackIndex != b.SourceStackIndex || a.TargetIndex != b.TargetIndex
                    || a.Count != b.Count || a.SourceStack != b.SourceStack || a.TargetStack != b.TargetStack)
                {
                    failures.Add("F8: a second call gives a different move at index " + i);
                    break;
                }
            }
        }

        for (int t = 0; t < input.Carried.Count && t < again.CarriedAfter.Count; t++)
        {
            if (plan.CarriedAfter[t] != again.CarriedAfter[t])
            {
                failures.Add("F8: a second call gives a different CarriedAfter[" + t + "]");
                break;
            }
        }

        if (input.Containers.Count != fresh.Containers.Count || input.Carried.Count != fresh.Carried.Count)
        {
            failures.Add("F8: an input changed size during the call");
            return;
        }

        for (int t = 0; t < input.Carried.Count; t++)
        {
            if (input.Carried[t].Stack != fresh.Carried[t].Stack || input.Carried[t].Marks != fresh.Carried[t].Marks)
            {
                failures.Add("F8: carried[" + t + "] changed during the call (P2)");
                return;
            }
        }

        for (int c = 0; c < input.Containers.Count; c++)
        {
            if (input.Containers[c] != fresh.Containers[c])
            {
                failures.Add("F8: chest[" + c + "] changed during the call (P2)");
                return;
            }
        }
    }
}
