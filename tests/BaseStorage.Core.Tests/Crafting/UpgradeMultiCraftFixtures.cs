namespace BaseStorage.Core.Tests.Crafting;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Crafting;

/// <summary>
/// The contract 4.1 fixtures of S-E003-007 (upgrade and multi-craft): the stacks, the Bronze sword and Wood arrow recipes, the
/// upgrade world U(b5, b7) and the multi-craft world A, and the independent residual calculator <see cref="Apply"/> that does not use
/// the planner or the executor. Built over the <see cref="CraftFixtures"/> ids and positions. Not a test class (ADR-GWIN-133).
/// </summary>
internal static class UpgradeMultiCraftFixtures
{
    /// <summary>The stacks of one world, in slot order per container.</summary>
    /// <param name="Inventory">The player inventory stacks.</param>
    /// <param name="C5">The chest c5 stacks.</param>
    /// <param name="C7">The chest c7 stacks.</param>
    internal sealed record StackWorld(ItemStackSnapshot[] Inventory, ItemStackSnapshot[] C5, ItemStackSnapshot[] C7);

    /// <summary>Gets the item being upgraded: SwordBronze at quality 3 with max stack 1; it sits in inventory slot 3.</summary>
    internal static ItemStackSnapshot SwordBronze => new ItemStackSnapshot("SwordBronze", 1, 3, 1);

    /// <summary>A quality 1 Bronze stack with max stack 30.</summary>
    /// <param name="count">The stack size.</param>
    /// <returns>The stack.</returns>
    internal static ItemStackSnapshot Bronze(int count) => new ItemStackSnapshot("Bronze", count, 1, 30);

    /// <summary>A quality 1 LeatherScraps stack with max stack 50.</summary>
    /// <param name="count">The stack size.</param>
    /// <returns>The stack.</returns>
    internal static ItemStackSnapshot LeatherScraps(int count) => new ItemStackSnapshot("LeatherScraps", count, 1, 50);

    /// <summary>A quality 1 Feathers stack with max stack 50.</summary>
    /// <param name="count">The stack size.</param>
    /// <returns>The stack.</returns>
    internal static ItemStackSnapshot Feathers(int count) => new ItemStackSnapshot("Feathers", count, 1, 50);

    /// <summary>The Bronze sword recipe lines at target quality <paramref name="q"/>: Bronze (8, 4 per level), Wood (2, 1), LeatherScraps (2, 1).</summary>
    /// <param name="q">The target quality level.</param>
    /// <returns>The recipe lines, in recipe order.</returns>
    internal static RecipeLine[] SwordLines(int q) => new[]
    {
        new RecipeLine("Bronze", VanillaAmountOracle.For(8, 4, q, false), false),
        new RecipeLine("Wood", VanillaAmountOracle.For(2, 1, q, false), false),
        new RecipeLine("LeatherScraps", VanillaAmountOracle.For(2, 1, q, false), false),
    };

    /// <summary>The Wood arrow recipe lines: Wood 8 and Feathers 2 per craft.</summary>
    /// <returns>The recipe lines, in recipe order.</returns>
    internal static RecipeLine[] ArrowLines() => new[]
    {
        new RecipeLine("Wood", 8, false),
        new RecipeLine("Feathers", 2, false),
    };

    /// <summary>The requirement list of a recipe at a multiplier; fails when the list overflows.</summary>
    /// <param name="lines">The recipe lines.</param>
    /// <param name="multiplier">The multiplier.</param>
    /// <returns>The multiplied requirement list.</returns>
    internal static IReadOnlyList<CraftRequirement> Requirements(IReadOnlyList<RecipeLine> lines, int multiplier) =>
        CraftRequirementLines.Build(lines, multiplier) ?? throw new InvalidOperationException("The requirement list overflowed.");

    /// <summary>The upgrade world U(b5, b7): inventory Bronze 5, Wood 10, LeatherScraps 10 and SwordBronze; c5 Bronze b5; c7 Bronze b7.</summary>
    /// <param name="b5">The Bronze in chest c5; no stack when 0.</param>
    /// <param name="b7">The Bronze in chest c7; no stack when 0.</param>
    /// <returns>The stacks.</returns>
    internal static StackWorld WorldU(int b5, int b7) => new StackWorld(
        new[] { Bronze(5), CraftFixtures.Wood(10), LeatherScraps(10), SwordBronze },
        b5 == 0 ? Array.Empty<ItemStackSnapshot>() : new[] { Bronze(b5) },
        b7 == 0 ? Array.Empty<ItemStackSnapshot>() : new[] { Bronze(b7) });

    /// <summary>The multi-craft world A: inventory Wood 10 and Feathers 4; c5 Wood 20 and Feathers 3; c7 Wood 18 and Feathers 5 (Wood 48, Feathers 12).</summary>
    /// <returns>The stacks.</returns>
    internal static StackWorld WorldA() => new StackWorld(
        new[] { CraftFixtures.Wood(10), Feathers(4) },
        new[] { CraftFixtures.Wood(20), Feathers(3) },
        new[] { CraftFixtures.Wood(18), Feathers(5) });

    /// <summary>Builds the base snapshot of a world over the rig ids: inventory at the origin, c5 at 5 m, c7 at 10 m, both chests PlayerBuilt.</summary>
    /// <param name="world">The stacks.</param>
    /// <returns>The snapshot, with no category assignments and the default eligibility.</returns>
    internal static BaseSnapshot Snapshot(StackWorld world)
    {
        ContainerSnapshot inventory = new ContainerSnapshot(CraftFixtures.Inventory, new WorldPosition(0f, 0f, 0f), 8, 4, world.Inventory, ContainerFlags.None);
        ContainerSnapshot c5 = new ContainerSnapshot(CraftFixtures.C5, new WorldPosition(5f, 0f, 0f), 5, 2, world.C5, ContainerFlags.PlayerBuilt);
        ContainerSnapshot c7 = new ContainerSnapshot(CraftFixtures.C7, new WorldPosition(10f, 0f, 0f), 5, 2, world.C7, ContainerFlags.PlayerBuilt);
        return CraftFixtures.Snapshot(inventory, new[] { c5, c7 });
    }

    /// <summary>Replays takes against a snapshot and returns the residual count per prefab over the inventory and both chests.</summary>
    /// <remarks>Independent of ConsumptionPlanner and the executor: it only subtracts each take from its stack. Fails when a take would
    /// leave a negative count, or when a take names a stack that does not exist.</remarks>
    /// <param name="snapshot">The snapshot the takes were planned on.</param>
    /// <param name="takes">The takes, in plan order.</param>
    /// <returns>The residual count per prefab.</returns>
    internal static IReadOnlyDictionary<string, int> Apply(BaseSnapshot snapshot, IEnumerable<ConsumptionTake> takes)
    {
        var counts = new Dictionary<ContainerId, int[]>();
        foreach (ContainerSnapshot container in new[] { snapshot.PlayerInventory }.Concat(snapshot.Containers))
        {
            counts[container.Id] = container.Contents.Select(stack => stack.Count).ToArray();
        }

        foreach (ConsumptionTake take in takes)
        {
            int[] stacks = counts[take.ContainerId];
            if (take.StackIndex < 0 || take.StackIndex >= stacks.Length)
            {
                throw new InvalidOperationException("A take names a stack that does not exist.");
            }

            stacks[take.StackIndex] -= take.Count;
            if (stacks[take.StackIndex] < 0)
            {
                throw new InvalidOperationException("A take leaves a negative count.");
            }
        }

        var residual = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (ContainerSnapshot container in new[] { snapshot.PlayerInventory }.Concat(snapshot.Containers))
        {
            for (int i = 0; i < container.Contents.Count; i++)
            {
                string prefab = container.Contents[i].PrefabName;
                residual[prefab] = (residual.TryGetValue(prefab, out int sum) ? sum : 0) + counts[container.Id][i];
            }
        }

        return residual;
    }

    /// <summary>Sums the stack counts of a snapshot per prefab over the inventory and both chests, before any take.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <returns>The total per prefab.</returns>
    internal static IReadOnlyDictionary<string, int> Totals(BaseSnapshot snapshot)
    {
        var totals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (ContainerSnapshot container in new[] { snapshot.PlayerInventory }.Concat(snapshot.Containers))
        {
            foreach (ItemStackSnapshot stack in container.Contents)
            {
                totals[stack.PrefabName] = (totals.TryGetValue(stack.PrefabName, out int sum) ? sum : 0) + stack.Count;
            }
        }

        return totals;
    }
}
