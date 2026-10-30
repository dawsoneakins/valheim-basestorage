namespace BaseStorage.Core.Tests.Availability;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The contract 9.7 world, and the contract 2.6 fixture <c>a</c> that it reuses as <c>c42</c>, as literals (ADR-GWIN-027). Expected values
/// in the tests are copied from the contract tables and are never computed by calling Core. <see cref="Create"/> returns fresh collections
/// on every call, because row V7 mutates the caller's list and dictionary. Not a test class.
/// </summary>
internal static class AvailabilityFixtures
{
    private const long UserId = 3141592653L;

    /// <summary>Rows A1-A5 of contract 9.7 (keep on), as (InInventory, InContainers, Total, IsMet, Missing).</summary>
    internal static readonly (int InInventory, int InContainers, int Total, bool IsMet, int Missing)[] KeepOnRows =
    {
        (12, 57, 69, true, 0),
        (5, 0, 5, true, 0),
        (3, 10, 13, false, 2),
        (0, 30, 30, false, 1),
        (0, 0, 0, false, 2),
    };

    /// <summary>Row A7 of contract 9.7 (keep off), as (InInventory, InContainers, Total, IsMet, Missing).</summary>
    internal static readonly (int InInventory, int InContainers, int Total, bool IsMet, int Missing)[] KeepOffRows =
    {
        (12, 17, 29, false, 31),
        (5, 0, 5, true, 0),
        (3, 0, 3, false, 12),
        (0, 30, 30, false, 1),
        (0, 0, 0, false, 2),
    };

    /// <summary>Builds a container id in the shared user id of the 9.7 world.</summary>
    /// <param name="serial">The per-peer serial number.</param>
    /// <returns>The id.</returns>
    internal static ContainerId Id(uint serial) => new ContainerId(UserId, serial);

    /// <summary>Builds one stack of items.</summary>
    /// <param name="prefab">The prefab name.</param>
    /// <param name="count">The item count.</param>
    /// <param name="quality">The quality level.</param>
    /// <param name="maxStack">The max stack size.</param>
    /// <returns>A new stack snapshot.</returns>
    internal static ItemStackSnapshot Stack(string prefab, int count, int quality, int maxStack) =>
        new ItemStackSnapshot(prefab, count, quality, maxStack);

    /// <summary>Builds a container at an explicit position and grid size.</summary>
    /// <param name="serial">The per-peer serial number of the container id.</param>
    /// <param name="x">The world X position.</param>
    /// <param name="y">The world Y position.</param>
    /// <param name="z">The world Z position.</param>
    /// <param name="width">The grid columns.</param>
    /// <param name="height">The grid rows.</param>
    /// <param name="flags">The observed flags.</param>
    /// <param name="contents">The stacks, in order.</param>
    /// <returns>A new container snapshot.</returns>
    internal static ContainerSnapshot Make(
        uint serial,
        float x,
        float y,
        float z,
        int width,
        int height,
        ContainerFlags flags,
        params ItemStackSnapshot[] contents) =>
        new ContainerSnapshot(Id(serial), new WorldPosition(x, y, z), width, height, contents, flags);

    /// <summary>Builds a 5 by 2 container at the origin. Position does not affect availability, so local fixtures use this.</summary>
    /// <param name="serial">The per-peer serial number of the container id.</param>
    /// <param name="flags">The observed flags.</param>
    /// <param name="contents">The stacks, in order.</param>
    /// <returns>A new container snapshot.</returns>
    internal static ContainerSnapshot Box(uint serial, ContainerFlags flags, params ItemStackSnapshot[] contents) =>
        Make(serial, 0f, 0f, 0f, 5, 2, flags, contents);

    /// <summary>An empty player inventory (id 1, 8 by 4, no flags), for local fixtures.</summary>
    /// <returns>A new container snapshot.</returns>
    internal static ContainerSnapshot EmptyInventory() => Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None);

    /// <summary>A fresh, empty assignment dictionary.</summary>
    /// <returns>A new dictionary.</returns>
    internal static Dictionary<ContainerId, CategoryAssignment> NoAssignments() => new Dictionary<ContainerId, CategoryAssignment>();

    /// <summary>Builds a snapshot from explicit parts, for local fixtures.</summary>
    /// <param name="inventory">The player inventory.</param>
    /// <param name="containers">The containers, in the order given.</param>
    /// <param name="assignments">The category assignments.</param>
    /// <param name="excludeVehicles">The ExcludeVehicles setting.</param>
    /// <param name="keepChestsCraftable">The KeepChestsCraftable setting.</param>
    /// <returns>A new snapshot.</returns>
    internal static BaseSnapshot Custom(
        ContainerSnapshot inventory,
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        bool excludeVehicles,
        bool keepChestsCraftable) =>
        new BaseSnapshot(inventory, containers, assignments, new EligibilityOptions(excludeVehicles, keepChestsCraftable));

    /// <summary>Builds the snapshot of the 9.7 world, from the world's own collections.</summary>
    /// <param name="world">The world to read.</param>
    /// <param name="keepChestsCraftable">The KeepChestsCraftable setting; ExcludeVehicles is on.</param>
    /// <returns>A new snapshot. Use <c>true</c> for keepOn and <c>false</c> for keepOff.</returns>
    internal static BaseSnapshot Snapshot(World world, bool keepChestsCraftable) =>
        Custom(world.Inventory, world.Containers, world.Assignments, true, keepChestsCraftable);

    /// <summary>The requirements of contract 9.7 (<c>reqs</c>): Wood 60, Resin 5, LeatherScraps 15, CopperOre 31 and Bronze 2.</summary>
    /// <returns>A new array on every call.</returns>
    internal static CraftRequirement[] Requirements() =>
        new[]
        {
            new CraftRequirement("Wood", 60),
            new CraftRequirement("Resin", 5),
            new CraftRequirement("LeatherScraps", 15),
            new CraftRequirement("CopperOre", 31),
            new CraftRequirement("Bronze", 2),
        };

    /// <summary>The ids of a container list, in list order, as the <c>Id.Id</c> serials.</summary>
    /// <param name="containers">The list to read.</param>
    /// <returns>One serial per element.</returns>
    internal static uint[] Ids(IReadOnlyList<ContainerSnapshot> containers) => containers.Select(c => c.Id.Id).ToArray();

    /// <summary>The counts of one requirement row as a tuple, for comparison with contract table literals.</summary>
    /// <param name="row">The row to read.</param>
    /// <returns>InInventory, InContainers, Total, IsMet and Missing.</returns>
    internal static (int InInventory, int InContainers, int Total, bool IsMet, int Missing) Counts(RequirementAvailability row) =>
        (row.InInventory, row.InContainers, row.Total, row.IsMet, row.Missing);

    /// <summary>Creates a fresh copy of the contract 9.7 world: the inventory, the five containers and the assignments.</summary>
    /// <returns>A new world on every call.</returns>
    internal static World Create()
    {
        // Contract 9.7 inventory.
        ContainerSnapshot inv = Make(
            1u, 12.5f, 31.25f, -840.75f, 8, 4, ContainerFlags.None,
            Stack("Wood", 12, 1, 50),
            Stack("Resin", 5, 1, 50),
            Stack("LeatherScraps", 3, 1, 50));

        // Contract 2.6 fixture a, reused as c42: a wooden chest holding a full stack of copper ore and 17 wood.
        ContainerSnapshot c42 = Make(
            42u, 14f, 31.25f, -836.5f, 5, 2, ContainerFlags.PlayerBuilt,
            Stack("CopperOre", 30, 1, 30),
            Stack("Wood", 17, 1, 50));

        // Erik has it open, so it is in use.
        ContainerSnapshot c57 = Make(
            57u, 18f, 31.25f, -836.5f, 5, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther,
            Stack("Wood", 23, 1, 50),
            Stack("Bronze", 4, 1, 30));

        // Assigned Keep.
        ContainerSnapshot c63 = Make(
            63u, 22f, 31.25f, -836.5f, 6, 4, ContainerFlags.PlayerBuilt,
            Stack("Wood", 40, 1, 50),
            Stack("LeatherScraps", 10, 1, 50));

        // Tombstone.
        ContainerSnapshot c70 = Make(
            70u, 16f, 30.5f, -830f, 4, 2, ContainerFlags.Tombstone,
            Stack("Wood", 50, 1, 50));

        // Two SwordBronze stacks, at quality 1 and quality 3.
        ContainerSnapshot c88 = Make(
            88u, 26f, 31.25f, -836.5f, 5, 2, ContainerFlags.PlayerBuilt,
            Stack("SwordBronze", 1, 1, 1),
            Stack("SwordBronze", 1, 3, 1));

        // Deliberately unsorted, as in contract 9.7.
        ContainerSnapshot[] containers = { c88, c57, c42, c70, c63 };

        var assignments = new Dictionary<ContainerId, CategoryAssignment>
        {
            [c63.Id] = CategoryAssignment.Keep,
            [c42.Id] = CategoryAssignment.ForCategory("Ores"),
        };

        return new World(inv, c42, c57, c63, c70, c88, containers, assignments);
    }

    /// <summary>The contract 9.7 world. Collections are fresh per <see cref="Create"/> call and are exposed as they were built.</summary>
    internal sealed class World
    {
        /// <summary>Creates a world from its parts. Use <see cref="Create"/> instead of calling this directly.</summary>
        /// <param name="inventory">The player inventory.</param>
        /// <param name="c42">Container 42.</param>
        /// <param name="c57">Container 57, in use.</param>
        /// <param name="c63">Container 63, assigned Keep.</param>
        /// <param name="c70">Container 70, a tombstone.</param>
        /// <param name="c88">Container 88.</param>
        /// <param name="containers">The unsorted container array.</param>
        /// <param name="assignments">The assignment dictionary.</param>
        internal World(
            ContainerSnapshot inventory,
            ContainerSnapshot c42,
            ContainerSnapshot c57,
            ContainerSnapshot c63,
            ContainerSnapshot c70,
            ContainerSnapshot c88,
            ContainerSnapshot[] containers,
            Dictionary<ContainerId, CategoryAssignment> assignments)
        {
            Inventory = inventory;
            C42 = c42;
            C57 = c57;
            C63 = c63;
            C70 = c70;
            C88 = c88;
            Containers = containers;
            Assignments = assignments;
        }

        /// <summary>Gets the player inventory (contract 9.7 <c>inv</c>).</summary>
        internal ContainerSnapshot Inventory { get; }

        /// <summary>Gets container 42 (contract 2.6 <c>a</c>).</summary>
        internal ContainerSnapshot C42 { get; }

        /// <summary>Gets container 57, in use.</summary>
        internal ContainerSnapshot C57 { get; }

        /// <summary>Gets container 63, assigned Keep.</summary>
        internal ContainerSnapshot C63 { get; }

        /// <summary>Gets container 70, a tombstone.</summary>
        internal ContainerSnapshot C70 { get; }

        /// <summary>Gets container 88.</summary>
        internal ContainerSnapshot C88 { get; }

        /// <summary>Gets the containers in the unsorted contract order: 88, 57, 42, 70, 63.</summary>
        internal ContainerSnapshot[] Containers { get; }

        /// <summary>Gets the assignments: 63 Keep and 42 "Ores".</summary>
        internal Dictionary<ContainerId, CategoryAssignment> Assignments { get; }
    }
}
