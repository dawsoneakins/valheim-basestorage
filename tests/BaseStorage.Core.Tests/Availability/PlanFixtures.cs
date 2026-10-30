namespace BaseStorage.Core.Tests.Availability;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;

/// <summary>
/// Literal worlds P (ordering), Q (quality and several requirements) and E (exclusions) for the S-E003-002 planner tests (ADR-GWIN-029).
/// Every builder returns fresh instances. Expected takes are written with <see cref="Take"/> from the snapshot's own stacks, and are never
/// computed by calling the planner. Not a test class.
/// </summary>
internal static class PlanFixtures
{
    /// <summary>The caller order of world P when none is given: containers 9, 2, 7 and 5.</summary>
    private static readonly uint[] DefaultCallerOrder = { 9u, 2u, 7u, 5u };

    /// <summary>World P's inventory: serial 1 at the origin, 8 by 4, flags None. Slot 0 Wood 10, slot 1 Stone 3, slot 2 Wood 4.</summary>
    /// <returns>A new inventory.</returns>
    internal static ContainerSnapshot OrderingInventory() =>
        Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, Stack("Wood", 10, 1, 50), Stack("Stone", 3, 1, 50), Stack("Wood", 4, 1, 50));

    /// <summary>World P's container 5, at horizontal distance 5 by default. Stacks: Wood 8, Wood 2, Wood 8.</summary>
    /// <param name="x">The world X position.</param>
    /// <param name="z">The world Z position.</param>
    /// <returns>A new container.</returns>
    internal static ContainerSnapshot OrderingC5(float x = 3f, float z = 4f) =>
        Make(5u, x, 0f, z, 5, 2, ContainerFlags.PlayerBuilt, Stack("Wood", 8, 1, 50), Stack("Wood", 2, 1, 50), Stack("Wood", 8, 1, 50));

    /// <summary>World P's container 7, tied with container 5 at horizontal distance 5 by default. Stacks: Wood 20, Wood 6.</summary>
    /// <param name="x">The world X position.</param>
    /// <param name="z">The world Z position.</param>
    /// <returns>A new container.</returns>
    internal static ContainerSnapshot OrderingC7(float x = 0f, float z = -5f) =>
        Make(7u, x, 0f, z, 5, 2, ContainerFlags.PlayerBuilt, Stack("Wood", 20, 1, 50), Stack("Wood", 6, 1, 50));

    /// <summary>World P's container 2, at (0, 100, 6): horizontal distance 6 but 3D distance about 100. Stacks: Wood 50.</summary>
    /// <returns>A new container.</returns>
    internal static ContainerSnapshot OrderingC2() =>
        Make(2u, 0f, 100f, 6f, 5, 2, ContainerFlags.PlayerBuilt, Stack("Wood", 50, 1, 50));

    /// <summary>World P's container 9, at horizontal distance 20. Stacks: Wood 5.</summary>
    /// <returns>A new container.</returns>
    internal static ContainerSnapshot OrderingC9() =>
        Make(9u, 20f, 0f, 0f, 5, 2, ContainerFlags.PlayerBuilt, Stack("Wood", 5, 1, 50));

    /// <summary>World P: the ordering world, with the chests in the given caller order (default 9, 2, 7, 5) and options on.</summary>
    /// <param name="callerOrder">The serials of the chests in the order they are passed; null for the default order.</param>
    /// <param name="c5X">Container 5's world X.</param>
    /// <param name="c5Z">Container 5's world Z.</param>
    /// <param name="c7X">Container 7's world X.</param>
    /// <param name="c7Z">Container 7's world Z.</param>
    /// <returns>A new snapshot.</returns>
    internal static BaseSnapshot WorldP(uint[]? callerOrder = null, float c5X = 3f, float c5Z = 4f, float c7X = 0f, float c7Z = -5f)
    {
        var containers = new List<ContainerSnapshot>();
        foreach (uint serial in callerOrder ?? DefaultCallerOrder)
        {
            containers.Add(OrderingChest(serial, c5X, c5Z, c7X, c7Z));
        }

        return Custom(OrderingInventory(), containers, NoAssignments(), true, true);
    }

    /// <summary>World Q: the quality world. Inventory slot 0 Wood 5. Container 4 at (2, 0, 0): SwordBronze q3, SwordBronze q1, Wood 7, Resin 9.</summary>
    /// <returns>A new snapshot with options on.</returns>
    internal static BaseSnapshot WorldQ()
    {
        ContainerSnapshot inventory = Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, Stack("Wood", 5, 1, 50));
        ContainerSnapshot c4 = Make(
            4u, 2f, 0f, 0f, 5, 2, ContainerFlags.PlayerBuilt,
            Stack("SwordBronze", 1, 3, 1),
            Stack("SwordBronze", 1, 1, 1),
            Stack("Wood", 7, 1, 50),
            Stack("Resin", 9, 1, 50));

        return Custom(inventory, new[] { c4 }, NoAssignments(), true, true);
    }

    /// <summary>
    /// World E: the exclusion world, with an empty inventory. Containers 10 (Vehicle), 11 (AccessDenied), 12 (not player built),
    /// 13 (Tombstone), 14 (in use) and 15 (Keep) sit at (1, 0, 0) and each holds Wood 10. Container 20 at (50, 0, 0) holds Wood 1.
    /// </summary>
    /// <param name="excludeVehicles">The ExcludeVehicles setting.</param>
    /// <param name="keepChestsCraftable">The KeepChestsCraftable setting.</param>
    /// <returns>A new snapshot.</returns>
    internal static BaseSnapshot WorldE(bool excludeVehicles = true, bool keepChestsCraftable = true)
    {
        var containers = new[]
        {
            Make(10u, 1f, 0f, 0f, 5, 2, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle, Stack("Wood", 10, 1, 50)),
            Make(11u, 1f, 0f, 0f, 5, 2, ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied, Stack("Wood", 10, 1, 50)),
            Make(12u, 1f, 0f, 0f, 5, 2, ContainerFlags.None, Stack("Wood", 10, 1, 50)),
            Make(13u, 1f, 0f, 0f, 5, 2, ContainerFlags.PlayerBuilt | ContainerFlags.Tombstone, Stack("Wood", 10, 1, 50)),
            Make(14u, 1f, 0f, 0f, 5, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, Stack("Wood", 10, 1, 50)),
            Make(15u, 1f, 0f, 0f, 5, 2, ContainerFlags.PlayerBuilt, Stack("Wood", 10, 1, 50)),
            Make(20u, 50f, 0f, 0f, 5, 2, ContainerFlags.PlayerBuilt, Stack("Wood", 1, 1, 50)),
        };

        var assignments = new Dictionary<ContainerId, CategoryAssignment>
        {
            [Id(15u)] = CategoryAssignment.Keep,
        };

        return Custom(EmptyInventory(), containers, assignments, excludeVehicles, keepChestsCraftable);
    }

    /// <summary>Plans the requirements and returns the result as it is, which may be null. Accepts one requirement or several.</summary>
    /// <param name="snapshot">The snapshot to plan against.</param>
    /// <param name="requirements">The requirements, in order.</param>
    /// <returns>The plan, or null when it cannot be covered.</returns>
    internal static ConsumptionPlan? PlanOrNull(BaseSnapshot snapshot, params CraftRequirement[] requirements) =>
        ConsumptionPlanner.Plan(snapshot, requirements);

    /// <summary>Plans the requirements, and asserts that a plan exists.</summary>
    /// <param name="snapshot">The snapshot to plan against.</param>
    /// <param name="requirements">The requirements, in order.</param>
    /// <returns>The non-null plan.</returns>
    internal static ConsumptionPlan Planned(BaseSnapshot snapshot, params CraftRequirement[] requirements)
    {
        ConsumptionPlan? plan = ConsumptionPlanner.Plan(snapshot, requirements);
        Assert.NotNull(plan);
        return plan!;
    }

    /// <summary>
    /// The expected take T(c, s, n, r): a take from container <paramref name="serial"/> (1 is the player inventory), stack
    /// <paramref name="slot"/>, of <paramref name="count"/> items toward requirement <paramref name="requirementIndex"/>. The stack is
    /// taken from the snapshot's own container, so the expected stack has the snapshot's values.
    /// </summary>
    /// <param name="snapshot">The snapshot the take belongs to.</param>
    /// <param name="serial">The per-peer serial of the container.</param>
    /// <param name="slot">The stack index.</param>
    /// <param name="count">The number of items.</param>
    /// <param name="requirementIndex">The requirement index; 0 by default.</param>
    /// <returns>A new take.</returns>
    internal static ConsumptionTake Take(BaseSnapshot snapshot, uint serial, int slot, int count, int requirementIndex = 0)
    {
        ContainerSnapshot container = serial == 1u
            ? snapshot.PlayerInventory
            : snapshot.Containers.Single(candidate => candidate.Id.Id == serial);
        return new ConsumptionTake(Id(serial), slot, container.Contents[slot], count, requirementIndex);
    }

    /// <summary>The container a take draws from: the player inventory, or the chest with the take's id.</summary>
    /// <param name="snapshot">The snapshot to read.</param>
    /// <param name="take">The take.</param>
    /// <returns>The source container snapshot.</returns>
    internal static ContainerSnapshot Source(BaseSnapshot snapshot, ConsumptionTake take) =>
        take.ContainerId == snapshot.PlayerInventory.Id
            ? snapshot.PlayerInventory
            : snapshot.Containers.Single(candidate => candidate.Id == take.ContainerId);

    /// <summary>
    /// The invariants of AC-11, for every requirement index: the takes that pay requirement i sum to its amount, each take has a count
    /// from 1 to its stack's count, and the takes on one stack sum to at most that stack's count.
    /// </summary>
    /// <param name="snapshot">The snapshot the plan was made from.</param>
    /// <param name="requirements">The requirements the plan was made from.</param>
    /// <param name="plan">The plan to check.</param>
    /// <param name="label">A label that names the case in the failure message.</param>
    internal static void AssertInvariants(BaseSnapshot snapshot, IReadOnlyList<CraftRequirement> requirements, ConsumptionPlan plan, string label)
    {
        for (int i = 0; i < requirements.Count; i++)
        {
            int paid = plan.Takes.Where(take => take.RequirementIndex == i).Sum(take => take.Count);
            Assert.True(paid == requirements[i].Amount, $"{label}: requirement {i} is paid {paid} of {requirements[i].Amount}.");
        }

        foreach (ConsumptionTake take in plan.Takes)
        {
            Assert.True(take.Count >= 1 && take.Count <= take.Stack.Count, $"{label}: a take of {take.Count} is outside 1 to the stack count.");
        }

        foreach (var group in plan.Takes.GroupBy(take => (take.ContainerId, take.StackIndex)))
        {
            int stackCount = Source(snapshot, group.First()).Contents[group.Key.StackIndex].Count;
            int taken = group.Sum(take => take.Count);
            Assert.True(taken <= stackCount, $"{label}: stack {group.Key.StackIndex} gives {taken} of {stackCount}.");
        }
    }

    /// <summary>One chest of world P, chosen by serial.</summary>
    private static ContainerSnapshot OrderingChest(uint serial, float c5X, float c5Z, float c7X, float c7Z) => serial switch
    {
        5u => OrderingC5(c5X, c5Z),
        7u => OrderingC7(c7X, c7Z),
        2u => OrderingC2(),
        9u => OrderingC9(),
        _ => throw new ArgumentOutOfRangeException(nameof(serial)),
    };
}
