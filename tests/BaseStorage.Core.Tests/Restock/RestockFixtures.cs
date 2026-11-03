namespace BaseStorage.Core.Tests.Restock;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Snapshots.Identities;
using static BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures;

/// <summary>One move as (source chest id, source stack index, target index, count), for readable assertions.</summary>
/// <param name="Chest">The source chest id, the Id part of the contract's ContainerId(3141592653L, id).</param>
/// <param name="Stack">The index of the source stack in the chest's input contents.</param>
/// <param name="Target">The index of the target in the carried list.</param>
/// <param name="Count">The number of items moved.</param>
internal readonly record struct MoveRow(uint Chest, int Stack, int Target, int Count);

/// <summary>
/// The S-QR-001 fixture helpers of contract section 6, verbatim, and the section 7.1 base R (chests) and K (carried).
/// Every builder returns new objects on each call. The helpers do not compute expected values.
/// </summary>
internal static class RestockFixtures
{
    /// <summary>The contract's chest id: the world id is fixed, and only the id varies.</summary>
    /// <param name="id">The id part.</param>
    /// <returns>The container id.</returns>
    internal static ContainerId Id(uint id) => new ContainerId(3141592653L, id);

    /// <summary>A stack with the default quality 1 and max stack 50 (contract 6).</summary>
    /// <param name="prefab">The prefab name.</param>
    /// <param name="count">The count.</param>
    /// <param name="maxStack">The max stack.</param>
    /// <param name="quality">The quality.</param>
    /// <returns>The stack with the empty identity.</returns>
    internal static ItemStackSnapshot S(string prefab, int count, int maxStack = 50, int quality = 1) =>
        new ItemStackSnapshot(prefab, count, quality, maxStack);

    /// <summary>A stack with an identity (contract 6).</summary>
    /// <param name="prefab">The prefab name.</param>
    /// <param name="count">The count.</param>
    /// <param name="identity">The identity.</param>
    /// <param name="maxStack">The max stack.</param>
    /// <param name="quality">The quality.</param>
    /// <returns>The stack.</returns>
    internal static ItemStackSnapshot SI(string prefab, int count, ItemStackIdentity identity, int maxStack = 50, int quality = 1) =>
        new ItemStackSnapshot(prefab, count, quality, maxStack, identity);

    /// <summary>A player-built chest at y 30 with one row of slots (contract 6).</summary>
    /// <param name="id">The id part.</param>
    /// <param name="x">The x position.</param>
    /// <param name="z">The z position.</param>
    /// <param name="slots">The slot count.</param>
    /// <param name="stacks">The contents.</param>
    /// <returns>The chest.</returns>
    internal static ContainerSnapshot C(uint id, float x, float z, int slots, params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(Id(id), new WorldPosition(x, 30f, z), slots, 1, stacks, ContainerFlags.PlayerBuilt);

    /// <summary>A chest with explicit flags at y 30 (contract 6).</summary>
    /// <param name="id">The id part.</param>
    /// <param name="x">The x position.</param>
    /// <param name="z">The z position.</param>
    /// <param name="slots">The slot count.</param>
    /// <param name="flags">The flags.</param>
    /// <param name="stacks">The contents.</param>
    /// <returns>The chest.</returns>
    internal static ContainerSnapshot CF(uint id, float x, float z, int slots, ContainerFlags flags, params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(Id(id), new WorldPosition(x, 30f, z), slots, 1, stacks, flags);

    /// <summary>A hotbar-marked carried stack (contract 6).</summary>
    /// <param name="s">The stack.</param>
    /// <returns>The carried stack.</returns>
    internal static CarriedStack H(ItemStackSnapshot s) => new CarriedStack(s, CarriedMarks.Hotbar);

    /// <summary>A favourite-type carried stack (contract 6).</summary>
    /// <param name="s">The stack.</param>
    /// <returns>The carried stack.</returns>
    internal static CarriedStack F(ItemStackSnapshot s) => new CarriedStack(s, CarriedMarks.FavouriteType);

    /// <summary>An unmarked carried stack (contract 6).</summary>
    /// <param name="s">The stack.</param>
    /// <returns>The carried stack.</returns>
    internal static CarriedStack U(ItemStackSnapshot s) => new CarriedStack(s, CarriedMarks.None);

    /// <summary>A starred-only carried stack (contract 6).</summary>
    /// <param name="s">The stack.</param>
    /// <returns>The carried stack.</returns>
    internal static CarriedStack Star(ItemStackSnapshot s) => new CarriedStack(s, CarriedMarks.StarredSlot);

    /// <summary>The origin of contract 6 and 7.</summary>
    /// <returns>The position.</returns>
    internal static WorldPosition Origin() => new WorldPosition(0f, 30f, 0f);

    /// <summary>KeepChestsCraftable on, ExcludeVehicles on (contract 6).</summary>
    /// <returns>The options.</returns>
    internal static EligibilityOptions Craftable() => new EligibilityOptions(excludeVehicles: true, keepChestsCraftable: true);

    /// <summary>KeepChestsCraftable off, ExcludeVehicles on (contract 6).</summary>
    /// <returns>The options.</returns>
    internal static EligibilityOptions NotCraftable() => new EligibilityOptions(excludeVehicles: true, keepChestsCraftable: false);

    /// <summary>No assignments (contract 6, noAssign).</summary>
    /// <returns>An empty dictionary.</returns>
    internal static Dictionary<ContainerId, CategoryAssignment> NoAssign() => new Dictionary<ContainerId, CategoryAssignment>();

    /// <summary>The 7.1 assignments: chest 803 is Keep.</summary>
    /// <returns>The assignments.</returns>
    internal static Dictionary<ContainerId, CategoryAssignment> AssignR() =>
        new Dictionary<ContainerId, CategoryAssignment> { [Id(803)] = CategoryAssignment.Keep };

    /// <summary>The 7.1 carried list K, with t0 to t9 in contract order.</summary>
    /// <returns>A new list.</returns>
    internal static List<CarriedStack> MainCarried() => new List<CarriedStack>
    {
        H(S("Wood", 10)),
        F(S("Resin", 5)),
        U(S("Stone", 10)),
        Star(S("Flint", 3)),
        H(S("SwordIron", 1, 1)),
        H(S("ArrowWood", 80, 100)),
        H(S("CookedMeat", 20, 20)),
        F(SI("Wood", 45, Wl2())),
        H(S("Coal", 10)),
        new CarriedStack(S("Resin", 48), CarriedMarks.Hotbar | CarriedMarks.FavouriteType),
    };

    /// <summary>The 7.1 chests R in snapshot order, 801 to 806.</summary>
    /// <returns>A new list.</returns>
    internal static List<ContainerSnapshot> MainChests() => new List<ContainerSnapshot>
    {
        C(801, 10, 0, 4, S("Wood", 40), S("ArrowWood", 30, 100), SI("Resin", 20, Wl2())),
        C(802, 3, 4, 4, S("Wood", 8), S("Wood", 12), S("Resin", 12)),
        C(803, 2, 0, 2, S("Wood", 50)),
        CF(804, 1, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Coal", 30), S("Wood", 10)),
        C(805, 0, 5, 4, SI("Wood", 20, Wl2()), S("Flint", 20), S("Stone", 30)),
        CF(806, 1, 1, 2, ContainerFlags.Tombstone, S("Wood", 50)),
    };

    /// <summary>The moves of a plan as rows, in plan order.</summary>
    /// <param name="plan">The plan.</param>
    /// <returns>The rows.</returns>
    internal static MoveRow[] Shape(RestockPlan plan) =>
        plan.Moves.Select(m => new MoveRow(m.Source.Id, m.SourceStackIndex, m.TargetIndex, m.Count)).ToArray();

    /// <summary>Builds one expected move row.</summary>
    /// <param name="chest">The source chest id.</param>
    /// <param name="stack">The source stack index.</param>
    /// <param name="target">The target index.</param>
    /// <param name="count">The count.</param>
    /// <returns>The row.</returns>
    internal static MoveRow Row(uint chest, int stack, int target, int count) => new MoveRow(chest, stack, target, count);
}
