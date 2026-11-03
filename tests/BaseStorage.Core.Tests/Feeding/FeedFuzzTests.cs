namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Feeding.FeedFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// S-QF-001 AC-5 (contract section 8, ADR-GWIN-180): the seeded randomized test of the 7-argument <c>FeedPlanner.Next</c>. There are 1,000 cases per
/// shape, and case n is <c>shape + 4k</c>, so <c>n % 4</c> is the shape. Every failure names Seed, n, the step, the G id and a dump of the case.
/// </summary>
public sealed class FeedFuzzTests
{
    private const int CasesPerShape = 1000;
    private const int StepLimit = 100;
    private const int MinimumOrdersPerShape = 150;
    private const int MinimumKeepChestOrders = 50;

    // GW-U-2680
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomStationsConserveItemsAndNeverOverfill(int shape)
    {
        var failures = new List<string>();
        for (int k = 0; k < CasesPerShape; k++)
        {
            CheckCase(shape + (4 * k), failures);
        }

        Assert.True(failures.Count == 0, failures.Count + " failures in shape " + shape + ". First:\n" + failures.FirstOrDefault());
    }

    // GW-U-2681
    [Fact]
    public void TheFeedGeneratorExercisesEveryShapeAndKeepChests()
    {
        var orders = new int[4];
        int keepChestOrders = 0;
        bool excludedKeepHoldsSelected = false;

        for (int n = 0; n < CasesPerShape * 4; n++)
        {
            FeedFuzzCase model = FeedFuzzCase.Generate(n);
            excludedKeepHoldsSelected |= HoldsExcludedKeep(model);

            for (int step = 0; step < StepLimit; step++)
            {
                FeedStationState station = model.Station();
                ContainerSnapshot[] chests = model.Snapshots();
                FeedOrder? order = FeedPlanner.Next(station, model.Selection, chests, model.Assignments, model.Options, model.Position, model.Skip);
                if (order is null)
                {
                    break;
                }

                orders[(int)model.Shape]++;
                if (model.Options.KeepChestsCraftable && IsKeep(model, order.Chest))
                {
                    keepChestOrders++;
                }

                if (!model.Apply(order))
                {
                    break;
                }
            }
        }

        for (int shape = 0; shape < 4; shape++)
        {
            Assert.True(orders[shape] >= MinimumOrdersPerShape, $"Shape {shape} has {orders[shape]} non-null orders; the floor is {MinimumOrdersPerShape}.");
        }

        Assert.True(keepChestOrders >= MinimumKeepChestOrders, $"{keepChestOrders} orders from Keep chests; the floor is {MinimumKeepChestOrders}.");
        Assert.True(excludedKeepHoldsSelected, "No case has a Keep chest holding a selected item that KeepChestsCraftable = false excludes.");
    }

    // GW-U-2682
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomStationsStopOnlyWhenNothingQualifies(int shape)
    {
        var failures = new List<string>();
        for (int k = 0; k < CasesPerShape; k++)
        {
            int n = shape + (4 * k);
            FeedFuzzCase model = FeedFuzzCase.Generate(n);
            for (int step = 0; step < StepLimit; step++)
            {
                FeedStationState station = model.Station();
                ContainerSnapshot[] chests = model.Snapshots();
                FeedOrder? order = FeedPlanner.Next(station, model.Selection, chests, model.Assignments, model.Options, model.Position, model.Skip);
                if (order is null)
                {
                    string? qualifying = FeedFuzzOracle.FirstQualifying(model, chests, station);
                    if (qualifying is not null)
                    {
                        failures.Add($"Seed={FeedFuzzCase.Seed} n={n} step={step} FM8/FM9: Next returned null while {qualifying}\n{model.Dump()}");
                    }

                    break;
                }

                if (!model.Apply(order))
                {
                    break;
                }
            }
        }

        Assert.True(failures.Count == 0, failures.Count + " null results with a qualifying item. First:\n" + failures.FirstOrDefault());
    }

    /// <summary>Plays one case with every check of G1 to G8 (contract section 8).</summary>
    private static void CheckCase(int n, List<string> failures)
    {
        FeedFuzzCase model = FeedFuzzCase.Generate(n);
        FeedStationState first = model.Station();
        int bound = first.FuelRoom + first.QueueRoom;

        var before = new Dictionary<string, int>(StringComparer.Ordinal);
        var placed = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string item in FeedFuzzCase.Pool)
        {
            before[item] = model.ChestTotal(item);
            placed[item] = 0;
        }

        ContainerSnapshot[] startChests = model.Snapshots();
        ContainerSnapshot[] startSources = SourcesOf(startChests, model.Assignments, model.Options);
        var frozen = new Dictionary<uint, string>();
        foreach (FeedFuzzChest chest in model.Chests)
        {
            ContainerId id = Id(chest.Number);
            bool isSource = startSources.Any(source => source.Id.Equals(id));
            if (!isSource || model.Skip.Contains(id))
            {
                frozen[chest.Number] = StackText(chest);
            }
        }

        int orders = 0;
        for (int step = 0; ; step++)
        {
            FeedStationState station = model.Station();
            ContainerSnapshot[] chests = model.Snapshots();
            FeedOrder? order = FeedPlanner.Next(station, model.Selection, chests, model.Assignments, model.Options, model.Position, model.Skip);
            FeedOrder? again = FeedPlanner.Next(station, model.Selection, chests, model.Assignments, model.Options, model.Position, model.Skip);
            if (!SameOrder(order, again))
            {
                Report(failures, n, step, "G6", "a second call on the same inputs gave a different order", model);
            }

            ContainerSnapshot[] sources = SourcesOf(chests, model.Assignments, model.Options);
            FeedOrder? fiveArgument = FeedPlanner.Next(station, model.Selection, sources, model.Position, model.Skip);
            if (!SameOrder(order, fiveArgument))
            {
                Report(failures, n, step, "G7", "the 7-argument order differs from the 5-argument order over the FM12 sources", model);
            }

            if (order is null)
            {
                string? qualifying = FeedFuzzOracle.FirstQualifying(model, chests, station);
                if (qualifying is not null)
                {
                    Report(failures, n, step, "FM8/FM9", "Next returned null while " + qualifying, model);
                }

                break;
            }

            CheckOrder(failures, n, step, order, model, station, chests, sources);

            orders++;
            if (orders > bound)
            {
                Report(failures, n, step, "G5", $"{orders} orders exceed the bound FuelRoom + QueueRoom = {bound}", model);
                break;
            }

            placed[order.PrefabName] = placed.GetValueOrDefault(order.PrefabName) + order.Amount;
            if (!model.Apply(order))
            {
                Report(failures, n, step, "G1", "the order names a chest the model does not hold", model);
                break;
            }
        }

        // G8: conservation per pool item, and the chests that may not give stay as they were, stack for stack.
        foreach (string item in FeedFuzzCase.Pool)
        {
            int after = model.ChestTotal(item);
            if (before[item] != after + placed[item])
            {
                Report(failures, n, -1, "G8", $"{item}: {before[item]} before, {after} after, {placed[item]} placed in the station", model);
            }
        }

        foreach (FeedFuzzChest chest in model.Chests)
        {
            if (frozen.TryGetValue(chest.Number, out string? text) && text != StackText(chest))
            {
                Report(failures, n, -1, "G8", $"chest {chest.Number} changed although it is not a source or is skipped", model);
            }
        }
    }

    /// <summary>Checks G1 to G4 for one order, against the state and chests the order was planned from.</summary>
    private static void CheckOrder(
        List<string> failures,
        int n,
        int step,
        FeedOrder order,
        FeedFuzzCase model,
        FeedStationState station,
        ContainerSnapshot[] chests,
        ContainerSnapshot[] sources)
    {
        // G1: a source, not denied or in use, not skipped, holding at least the amount.
        ContainerSnapshot? chest = chests.FirstOrDefault(candidate => candidate.Id.Equals(order.Chest));
        bool isSource = sources.Any(source => source.Id.Equals(order.Chest));
        bool mayGive = chest is not null
            && (chest.Flags & (ContainerFlags.AccessDenied | ContainerFlags.InUseByOther)) == 0
            && !model.Skip.Contains(order.Chest);
        int held = chest is null ? 0 : CountOf(chest, order.PrefabName);
        if (order.Amount < 1 || !isSource || !mayGive || held < order.Amount)
        {
            Report(failures, n, step, "G1", $"amount {order.Amount}, source {isSource}, may give {mayGive}, holds {held}", model);
        }

        // G2: fuel goes to the fuel prefab within the fuel room; an input is selected, accepted and within the queue room.
        FeedEntry? entry = model.Selection.Find(order.PrefabName);
        if (order.IsFuel)
        {
            if (entry is null || order.PrefabName != station.FuelPrefab || order.Amount > station.FuelRoom)
            {
                Report(failures, n, step, "G2", $"fuel {order.PrefabName} {order.Amount} against FuelRoom {station.FuelRoom}", model);
            }
        }
        else if (entry is null || !Accepts(station, order.PrefabName) || order.Amount > station.QueueRoom)
        {
            Report(failures, n, step, "G2", $"input {order.PrefabName} {order.Amount} against QueueRoom {station.QueueRoom}", model);
        }

        // G3: no overfill.
        if (order.IsFuel && (double)station.Fuel + order.Amount > station.MaxFuel)
        {
            Report(failures, n, step, "G3", $"fuel {station.Fuel} + {order.Amount} exceeds MaxFuel {station.MaxFuel}", model);
        }
        else if (!order.IsFuel && station.Queued + order.Amount > station.MaxQueued)
        {
            Report(failures, n, step, "G3", $"queued {station.Queued} + {order.Amount} exceeds MaxQueued {station.MaxQueued}", model);
        }

        if (model.Shape == FeedStationShape.SingleInput && (model.Occupied || order.Amount != 1))
        {
            Report(failures, n, step, "G3", $"single input order of {order.Amount} while occupied {model.Occupied}", model);
        }

        if (model.Shape == FeedStationShape.SeveralInput && order.Amount > model.SlotCount - model.OccupiedSlots)
        {
            Report(failures, n, step, "G3", $"several input order of {order.Amount} with {model.SlotCount - model.OccupiedSlots} empty slots", model);
        }

        // G4: the keep amount stays in the sources.
        if (entry is not null && FeedPlanner.CountInBase(sources, order.PrefabName) - order.Amount < entry.Keep)
        {
            Report(failures, n, step, "G4", $"{order.PrefabName}: keep {entry.Keep} not left after taking {order.Amount}", model);
        }
    }

    private static void Report(List<string> failures, int n, int step, string id, string detail, FeedFuzzCase model) =>
        failures.Add($"Seed={FeedFuzzCase.Seed} n={n} step={step} {id}: {detail}\n{model.Dump()}");

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

    private static string StackText(FeedFuzzChest chest) =>
        string.Join(",", chest.Stacks.Select(stack => stack.Prefab + "x" + stack.Count));

    private static bool IsKeep(FeedFuzzCase model, ContainerId id) =>
        model.Assignments.TryGetValue(id, out CategoryAssignment? assignment) && assignment is not null && assignment.IsKeep;

    private static bool HoldsExcludedKeep(FeedFuzzCase model)
    {
        ContainerSnapshot[] chests = model.Snapshots();
        var withoutKeep = new EligibilityOptions(model.Options.ExcludeVehicles, false);
        foreach (ContainerSnapshot chest in chests)
        {
            if (!IsKeep(model, chest.Id) || ContainerEligibility.GetExclusionReason(chest, true, ContainerPurpose.CraftAvailability, withoutKeep) != ExclusionReason.Keep)
            {
                continue;
            }

            if (model.Selection.Entries.Any(entry => CountOf(chest, entry.PrefabName) > 0))
            {
                return true;
            }
        }

        return false;
    }
}
