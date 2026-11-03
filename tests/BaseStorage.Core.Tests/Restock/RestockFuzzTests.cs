namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;

/// <summary>
/// S-QR-001 AC-5 (contract 7.10, ADR-GWIN-172): a seeded 1,000-case fuzz of the planner. Every case uses System.Random(Seed + n).
/// Each failure message names Seed, n, the invariant and the input dump, so a case can be replayed.
/// </summary>
public sealed class RestockFuzzTests
{
    private const int Seed = 20261102;
    private const int Cases = 1000;
    private const int MinimumCasesWithMove = 170;

    // GW-U-2520
    [Fact]
    public void RandomBasesConserveItemsAndNeverOverfill()
    {
        var failures = new List<string>();
        for (int n = 0; n < Cases; n++)
        {
            FuzzCase input = RestockFuzzGenerator.Generate(Seed + n);
            RestockPlan plan = Plan(input);
            RestockPlan again = Plan(input);
            FuzzCase fresh = RestockFuzzGenerator.Generate(Seed + n);
            OracleOutcome expected = Expected(input);

            foreach (string failure in RestockFuzzInvariants.Check(input, plan, again, fresh, expected))
            {
                failures.Add("Seed=" + Seed + " n=" + n + " " + failure + "\n" + input.Dump());
            }
        }

        Assert.True(failures.Count == 0, failures.Count + " invariant failures. First:\n" + failures.FirstOrDefault());
    }

    // GW-U-2521
    [Fact]
    public void RandomBasesMatchTheRuleOracle()
    {
        var failures = new List<string>();
        for (int n = 0; n < Cases; n++)
        {
            FuzzCase input = RestockFuzzGenerator.Generate(Seed + n);
            RestockPlan plan = Plan(input);
            OracleOutcome expected = Expected(input);

            var problems = new List<string>();
            if (plan.CarriedAfter.Count != input.Carried.Count)
            {
                problems.Add("CarriedAfter has a different length");
            }
            else
            {
                for (int t = 0; t < input.Carried.Count; t++)
                {
                    if (expected.TargetRoom[t] < 0)
                    {
                        if (!ReferenceEquals(plan.CarriedAfter[t], input.Carried[t].Stack))
                        {
                            problems.Add("non-target carried[" + t + "] was changed");
                        }
                    }
                    else if (input.Carried[t].Stack.MaxStack - plan.CarriedAfter[t].Count != expected.TargetRoom[t])
                    {
                        problems.Add("target carried[" + t + "] has room " + (input.Carried[t].Stack.MaxStack - plan.CarriedAfter[t].Count)
                            + ", the oracle says " + expected.TargetRoom[t]);
                    }
                }
            }

            if (plan.MovedItems != expected.MovedItems || plan.ToppedUpStacks != expected.ToppedUpStacks)
            {
                problems.Add("MovedItems or ToppedUpStacks differs from the oracle");
            }

            if (plan.RemainingRoom != expected.RemainingRoom || plan.LeftInUse != expected.LeftInUse
                || plan.LeftDifferentIdentity != expected.LeftDifferentIdentity)
            {
                problems.Add("RemainingRoom, LeftInUse or LeftDifferentIdentity differs from the oracle");
            }

            if (!RestockOracle.IsMaximal(input.Carried, input.Containers, input.Assignments, input.Options, plan.Moves))
            {
                problems.Add("the plan is not maximal (Q13)");
            }

            foreach (string problem in problems)
            {
                failures.Add("Seed=" + Seed + " n=" + n + " " + problem + "\n" + input.Dump());
            }
        }

        Assert.True(failures.Count == 0, failures.Count + " oracle mismatches. First:\n" + failures.FirstOrDefault());
    }

    // GW-U-2522
    [Fact]
    public void TheGeneratorExercisesEveryBranch()
    {
        int withMove = 0;
        int withInUse = 0;
        int withDifferentIdentity = 0;
        int withKeepSkipped = 0;
        int withOverFull = 0;

        for (int n = 0; n < Cases; n++)
        {
            FuzzCase input = RestockFuzzGenerator.Generate(Seed + n);
            RestockPlan plan = Plan(input);

            if (plan.Moves.Count > 0)
            {
                withMove++;
            }

            if (plan.LeftInUse > 0)
            {
                withInUse++;
            }

            if (plan.LeftDifferentIdentity > 0)
            {
                withDifferentIdentity++;
            }

            if (HasSkippedKeepChest(input))
            {
                withKeepSkipped++;
            }

            if (HasOverFullStack(input))
            {
                withOverFull++;
            }
        }

        // A floor, not just "above 0": the widened generator measured 343 cases with a move in 1,000 (probe, S-QR-001).
        // Half of that is the regression bar, so a generator that quietly reaches the fill in only a few cases fails here.
        Assert.True(withMove >= MinimumCasesWithMove, $"Only {withMove} cases have a move; the floor is {MinimumCasesWithMove}.");
        Assert.True(withInUse > 0, "No case has LeftInUse above 0.");
        Assert.True(withDifferentIdentity > 0, "No case has LeftDifferentIdentity above 0.");
        Assert.True(withKeepSkipped > 0, "No case skips a Keep chest.");
        Assert.True(withOverFull > 0, "No case has an over-full stack.");
    }

    private static RestockPlan Plan(FuzzCase input) =>
        RestockPlanner.Plan(input.Carried, input.Origin, input.Containers, input.Assignments, input.Options);

    private static OracleOutcome Expected(FuzzCase input) =>
        RestockOracle.Compute(input.Carried, input.Origin, input.Containers, input.Assignments, input.Options);

    private static bool HasSkippedKeepChest(FuzzCase input)
    {
        foreach (ContainerSnapshot chest in input.Containers)
        {
            bool keep = RestockOracle.IsKeep(chest, input.Assignments);
            bool otherExclusion = (chest.Flags & ContainerFlags.Tombstone) != 0
                || (input.Options.ExcludeVehicles && (chest.Flags & ContainerFlags.Vehicle) != 0)
                || (chest.Flags & ContainerFlags.PlayerBuilt) == 0
                || (chest.Flags & ContainerFlags.AccessDenied) != 0;
            if (keep && !input.Options.KeepChestsCraftable && !otherExclusion)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasOverFullStack(FuzzCase input)
    {
        foreach (CarriedStack entry in input.Carried)
        {
            if (entry.Stack.Count > entry.Stack.MaxStack)
            {
                return true;
            }
        }

        foreach (ContainerSnapshot chest in input.Containers)
        {
            if (chest.Contents.Count > chest.SlotCount)
            {
                return true;
            }

            foreach (ItemStackSnapshot stack in chest.Contents)
            {
                if (stack.Count > stack.MaxStack)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
