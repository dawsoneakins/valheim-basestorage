namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>
/// Shared fixtures for the consumption executor tests (ADR-GWIN-031): contract 4.1 world W, the planned takes of W, the layer and executor
/// builders, the drive loop and the conservation and consumption assertions. Expected values are literals from the contract. Not a test class.
/// </summary>
internal static class ConsumptionFixtures
{
    /// <summary>The message key of every non-success outcome, copied as a literal (CX11).</summary>
    internal const string MaterialsMoved = "message.craft.materials-moved";

    /// <summary>The English text of <see cref="MaterialsMoved"/>, copied as a literal.</summary>
    internal const string MaterialsMovedText = "Materials moved, try again";

    /// <summary>Contract 4.1: the player inventory, serial 1 at the origin, 8 by 4, slot 0 Wood 3.</summary>
    /// <returns>A new snapshot.</returns>
    internal static ContainerSnapshot InventoryW() => Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, Wood(3));

    /// <summary>Contract 4.1: chest 5 at distance 5, 5 by 2, slot 0 Wood 8 and slot 1 Stone 4.</summary>
    /// <returns>A new snapshot.</returns>
    internal static ContainerSnapshot Chest5W() => Make(5u, 3f, 0f, 4f, 5, 2, ContainerFlags.PlayerBuilt, Wood(8), Stone(4));

    /// <summary>Contract 4.1: chest 7 at distance 10, 5 by 2, slot 0 Wood 10.</summary>
    /// <returns>A new snapshot.</returns>
    internal static ContainerSnapshot Chest7W() => Make(7u, 6f, 0f, 8f, 5, 2, ContainerFlags.PlayerBuilt, Wood(10));

    /// <summary>The plan of world W: Wood 15 and Stone 2, from the snapshot Custom(inventory, [chest7, chest5]) of contract 4.1.</summary>
    /// <returns>A new plan.</returns>
    internal static ConsumptionPlan PlanW() => PlanFor(
        InventoryW(),
        new[] { Chest7W(), Chest5W() },
        new[] { new CraftRequirement("Wood", 15), new CraftRequirement("Stone", 2) });

    /// <summary>Plans the given requirements over the given world, and asserts that the planner found a plan.</summary>
    /// <param name="inventory">The player inventory.</param>
    /// <param name="chests">The chests, in the order given.</param>
    /// <param name="requirements">The requirements to pay.</param>
    /// <returns>The plan.</returns>
    internal static ConsumptionPlan PlanFor(ContainerSnapshot inventory, ContainerSnapshot[] chests, CraftRequirement[] requirements)
    {
        ConsumptionPlan? plan = ConsumptionPlanner.Plan(Custom(inventory, chests, NoAssignments(), true, false), requirements);
        Assert.NotNull(plan);
        return plan!;
    }

    /// <summary>A recording layer seeded from world W, with optional replacement containers (F5 and F6 seed a changed container).</summary>
    /// <param name="inventory">A replacement player inventory, or null for world W.</param>
    /// <param name="chest5">A replacement chest 5, or null for world W.</param>
    /// <param name="chest7">A replacement chest 7, or null for world W.</param>
    /// <returns>A new recording layer over a new fake.</returns>
    internal static RecordingContainerLayer LayerW(
        ContainerSnapshot? inventory = null,
        ContainerSnapshot? chest5 = null,
        ContainerSnapshot? chest7 = null) =>
        LayerOf(inventory ?? InventoryW(), chest5 ?? Chest5W(), chest7 ?? Chest7W());

    /// <summary>A recording layer over a new fake, seeded with the inventory and the given chests in order.</summary>
    /// <param name="inventory">The player inventory.</param>
    /// <param name="chests">The chests to add, in order.</param>
    /// <returns>A new recording layer.</returns>
    internal static RecordingContainerLayer LayerOf(ContainerSnapshot inventory, params ContainerSnapshot[] chests)
    {
        FakeContainerLayer fake = new(inventory);
        foreach (ContainerSnapshot chest in chests)
        {
            fake.AddChest(chest);
        }

        return new RecordingContainerLayer(fake);
    }

    /// <summary>Builds an executor. The clock defaults to a manual clock at 10 s, and the settings default to an empty source (1500 ms).</summary>
    /// <param name="layer">The layer.</param>
    /// <param name="clock">The clock, or null for a new manual clock at 10 s.</param>
    /// <param name="settings">The settings source, or null for an empty one.</param>
    /// <returns>A new executor.</returns>
    internal static ConsumptionExecutor Executor(IContainerLayer layer, IClock? clock = null, FakeSettingsSource? settings = null) =>
        new(layer, clock ?? new ManualClock(TimeSpan.FromSeconds(10)), new BaseStorageSettings(settings ?? new FakeSettingsSource()));

    /// <summary>Polls until a result appears, and fails the test when none does within the poll budget, so no test can loop forever.</summary>
    /// <param name="execution">The execution to advance.</param>
    /// <param name="maxPolls">The poll budget.</param>
    /// <returns>The terminal result.</returns>
    internal static ConsumptionResult Drive(ConsumptionExecution execution, int maxPolls = 5)
    {
        for (int i = 0; i < maxPolls; i++)
        {
            ConsumptionResult? result = execution.Poll();
            if (result is not null)
            {
                return result;
            }
        }

        throw new InvalidOperationException("No terminal result after " + maxPolls.ToString(CultureInfo.InvariantCulture) + " polls.");
    }

    /// <summary>Asserts that a result is present, and returns it without a nullable warning.</summary>
    /// <param name="result">The result to check.</param>
    /// <returns>The same result.</returns>
    internal static ConsumptionResult Done(ConsumptionResult? result)
    {
        Assert.NotNull(result);
        return result!;
    }

    /// <summary>The calls recorded after a given position in the layer's call log.</summary>
    /// <param name="layer">The layer.</param>
    /// <param name="mark">The number of calls recorded before the position.</param>
    /// <returns>The later calls, in order.</returns>
    internal static string[] Since(RecordingContainerLayer layer, int mark) => layer.Calls.Skip(mark).ToArray();

    /// <summary>The calls recorded after the first occurrence of a given call.</summary>
    /// <param name="layer">The layer.</param>
    /// <param name="afterCall">The call to start after.</param>
    /// <returns>The later calls, in order.</returns>
    internal static string[] Tail(RecordingContainerLayer layer, string afterCall)
    {
        List<string> calls = layer.Calls.ToList();
        int index = calls.IndexOf(afterCall);
        Assert.True(index >= 0, "The call was not recorded: " + afterCall);
        return calls.Skip(index + 1).ToArray();
    }

    /// <summary>Asserts the conserved outcome of a failure in world W: conservation, both chests released, not a success, the materials-moved key.</summary>
    /// <param name="result">The result.</param>
    /// <param name="layer">The layer, whose inner fake is read.</param>
    /// <param name="before">The world before Execute.</param>
    internal static void AssertConserved(ConsumptionResult result, RecordingContainerLayer layer, IReadOnlyList<ContainerSnapshot> before)
    {
        ConservationAssert.Holds(before, layer.Inner.Capture());
        AssertAllReleased(layer, Id(5), Id(7));
        Assert.False(result.IsSuccess);
        Assert.Equal(MaterialsMoved, result.MessageKey);
    }

    /// <summary>Asserts the untouched outcome: conserved, every container equal to its state before, and no Remove or Add call.</summary>
    /// <param name="result">The result.</param>
    /// <param name="layer">The layer.</param>
    /// <param name="before">The world before Execute.</param>
    internal static void AssertUntouched(ConsumptionResult result, RecordingContainerLayer layer, IReadOnlyList<ContainerSnapshot> before)
    {
        AssertConserved(result, layer, before);
        Assert.Equal(before, layer.Inner.Capture());
        Assert.DoesNotContain(
            layer.Calls,
            call => call.StartsWith("Remove:", StringComparison.Ordinal) || call.StartsWith("Add:", StringComparison.Ordinal));
    }

    /// <summary>Asserts that each chest's reservation status is None, read from the inner fake so that no call is recorded.</summary>
    /// <param name="layer">The layer.</param>
    /// <param name="ids">The chests.</param>
    internal static void AssertAllReleased(RecordingContainerLayer layer, params ContainerId[] ids)
    {
        foreach (ContainerId id in ids)
        {
            Assert.Equal(ReservationStatus.None, layer.Inner.GetReservationStatus(id));
        }
    }

    /// <summary>Asserts a success that consumed exactly the requirements: per item type, the drop in totals equals the requirement amounts.</summary>
    /// <param name="before">The world before Execute.</param>
    /// <param name="after">The world after the result.</param>
    /// <param name="requirements">The requirements that were paid.</param>
    internal static void AssertConsumedExactly(
        IEnumerable<ContainerSnapshot> before,
        IEnumerable<ContainerSnapshot> after,
        IEnumerable<CraftRequirement> requirements)
    {
        SortedDictionary<string, int> start = Totals(before);
        SortedDictionary<string, int> end = Totals(after);
        SortedDictionary<string, int> expected = new(StringComparer.Ordinal);
        foreach (CraftRequirement requirement in requirements)
        {
            expected[requirement.PrefabName] = Amount(expected, requirement.PrefabName) + requirement.Amount;
        }

        SortedDictionary<string, int> consumed = new(StringComparer.Ordinal);
        foreach (string prefab in start.Keys.Union(end.Keys).OrderBy(name => name, StringComparer.Ordinal))
        {
            int drop = Amount(start, prefab) - Amount(end, prefab);
            if (drop != 0)
            {
                consumed[prefab] = drop;
            }
        }

        Assert.Equal(expected, consumed);
    }

    /// <summary>Per item prefab, the total count over the given containers, in ordinal key order.</summary>
    /// <param name="state">The containers.</param>
    /// <returns>The totals.</returns>
    internal static SortedDictionary<string, int> Totals(IEnumerable<ContainerSnapshot> state)
    {
        SortedDictionary<string, int> totals = new(StringComparer.Ordinal);
        foreach (ContainerSnapshot container in state)
        {
            foreach (ItemStackSnapshot stack in container.Contents)
            {
                totals[stack.PrefabName] = Amount(totals, stack.PrefabName) + stack.Count;
            }
        }

        return totals;
    }

    private static int Amount(SortedDictionary<string, int> totals, string prefab) =>
        totals.TryGetValue(prefab, out int value) ? value : 0;
}
