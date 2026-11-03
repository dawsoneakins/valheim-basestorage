namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Restock.Run;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;

/// <summary>GW-U-2602 and 2603: the restock fuzz run through the executor over 1,000 seeded cases (S-QR-002 contract 10.11, RR10).</summary>
public sealed class RestockRunFuzzTests
{
    private const int Seed = 20261103;
    private const int Cases = 1000;
    private static readonly string[] Prefabs = { "Wood", "Resin", "Coal", "ArrowWood" };

    /// <summary>GW-U-2602: over 1,000 seeded cases every run completes, conserves each item key, keeps the moved and left counts consistent with the plan, never overfills a carried stack, and removes only after a confirming add and re-read.</summary>
    [Fact]
    public void RR10_RandomRunsConserveItems()
    {
        var failures = new List<string>();
        for (int n = 0; n < Cases; n++)
        {
            RestockFuzzGenerator.RunFuzzCase input = RestockFuzzGenerator.GenerateRun(Seed + n);
            CaseResult result = RunCase(input);
            if (result.Problem is not null)
            {
                failures.Add("seed " + (Seed + n) + " (n " + n + "): " + result.Problem);
            }
        }

        Assert.True(failures.Count == 0, "Fuzz failures: " + failures.Count + ". First: " + string.Join(" | ", failures.Take(3)));
    }

    /// <summary>GW-U-2603: across the 1,000 cases each branch fires at least once: a partial merge, a refusal, an unavailable chest, a stale target and a completed move.</summary>
    [Fact]
    public void RR10_TheRunFuzzExercisesEveryBranch()
    {
        int partial = 0;
        int refuse = 0;
        int unavailable = 0;
        int stale = 0;
        int completed = 0;
        for (int n = 0; n < Cases; n++)
        {
            CaseResult result = RunCase(RestockFuzzGenerator.GenerateRun(Seed + n));
            partial += result.PartialFired ? 1 : 0;
            refuse += result.RefuseUsed ? 1 : 0;
            unavailable += result.ChestUnavailable ? 1 : 0;
            stale += result.StaleStacks > 0 ? 1 : 0;
            completed += result.MovesCompleted > 0 ? 1 : 0;
        }

        Assert.True(
            partial > 0 && refuse > 0 && unavailable > 0 && stale > 0 && completed > 0,
            "Branch counts: partial " + partial + ", refuse " + refuse + ", unavailable " + unavailable + ", stale " + stale + ", completed " + completed + ".");
    }

    private sealed record CaseResult(string? Problem, bool PartialFired, bool RefuseUsed, bool ChestUnavailable, int StaleStacks, int MovesCompleted);

    private static CaseResult RunCase(RestockFuzzGenerator.RunFuzzCase input)
    {
        FuzzCase basic = input.Basic;
        List<CarriedStack> carried = basic.Carried;
        List<ContainerSnapshot> chests = basic.Containers;
        RestockPlan plan = RestockPlanner.Plan(carried, basic.Origin, chests, basic.Assignments, basic.Options);

        var layer = new FakeRestockLayer(carried, chests);
        foreach (KeyValuePair<int, RestockAddMode> entry in input.Modes)
        {
            layer.SetMode(entry.Key, entry.Value, input.Partials[entry.Key]);
        }

        foreach (KeyValuePair<uint, ReservationStatus[]> entry in input.Reservations)
        {
            layer.SetReservation(entry.Key, entry.Value);
        }

        foreach ((int index, ItemStackSnapshot stack) in input.Stale)
        {
            layer.MutateCarried(index, stack);
        }

        var before = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string prefab in Prefabs)
        {
            (int carriedTotal, int chestTotal) = layer.Totals(prefab);
            before[prefab] = carriedTotal + chestTotal;
        }

        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var executor = new RestockExecutor(plan, layer, clock, new BaseStorageSettings(new FakeSettingsSource().WithInt(BaseStorage.Core.Settings.SettingKeys.MovesPerFrame, 2)));
        int steps = 0;
        while (!executor.IsComplete && steps < 10000)
        {
            executor.Step();
            clock.Advance(TimeSpan.FromMilliseconds(100));
            steps++;
        }

        string? problem = null;
        RestockRunResult result = executor.Result;
        var problems = new List<string>();
        if (executor.Status != ExecutionStatus.Completed)
        {
            problems.Add("status " + executor.Status + " after " + steps + " steps");
        }

        foreach (string prefab in Prefabs)
        {
            (int carriedTotal, int chestTotal) = layer.Totals(prefab);
            if (carriedTotal + chestTotal != before[prefab])
            {
                problems.Add("conservation of " + prefab + " is " + (carriedTotal + chestTotal) + ", not " + before[prefab]);
            }
        }

        if (result.MovedItems + result.LeftDuringRun != plan.MovedItems)
        {
            problems.Add("moved " + result.MovedItems + " + left " + result.LeftDuringRun + " != plan " + plan.MovedItems);
        }

        for (int i = 0; i < layer.Carried.Count; i++)
        {
            ItemStackSnapshot? slot = layer.Carried[i];
            if (slot is not null && slot.Count > slot.MaxStack)
            {
                problems.Add("carried " + i + " holds " + slot.Count + ", above its MaxStack " + slot.MaxStack);
            }
        }

        foreach (ContainerSnapshot chest in chests)
        {
            if (layer.Chest(chest.Id.Id).Any(slot => slot is not null && slot.Count < 0))
            {
                problems.Add("chest " + chest.Id.Id + " has a stack below 0");
            }
        }

        List<string> calls = layer.Calls;
        for (int r = 0; r < calls.Count; r++)
        {
            if (!calls[r].StartsWith("Remove ", StringComparison.Ordinal))
            {
                continue;
            }

            int count = int.Parse(calls[r].Split(' ')[2], System.Globalization.CultureInfo.InvariantCulture);
            int? delta = layer.ConfirmedDelta(r);
            if (delta is null || delta < count)
            {
                problems.Add("remove " + calls[r] + " not confirmed by an add and re-read of at least " + count + " (delta " + (delta?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none") + ")");
            }
        }

        if (problems.Count > 0)
        {
            problem = string.Join("; ", problems);
        }

        bool partialFired = input.Modes.Any(entry => entry.Value == RestockAddMode.PartialOnce && plan.Moves.Any(move => move.TargetIndex == entry.Key));
        bool refuseUsed = input.Modes.Any(entry => entry.Value == RestockAddMode.Refuse && plan.Moves.Any(move => move.TargetIndex == entry.Key));
        bool unavailable = input.Reservations.Any(entry => entry.Value.Any(status => status != ReservationStatus.Granted) && plan.Moves.Any(move => move.Source.Id == entry.Key));
        return new CaseResult(problem, partialFired, refuseUsed, unavailable, result.StaleStacks, result.MovesCompleted);
    }
}
