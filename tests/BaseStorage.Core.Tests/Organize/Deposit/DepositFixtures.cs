namespace BaseStorage.Core.Tests.Organize.Deposit;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// The S-E004-013 Deposit fixtures (deposit contract section 6): the player inventory id, stack and inventory factories, the kinds
/// table with the never-list items, base D and inventory I. Every expected value in the tests is a literal copied from the contract,
/// never computed by the planner. A fixture holder, not a test class.
/// </summary>
internal static class DepositFixtures
{
    /// <summary>The peer id of the player inventory; different from the chest peer id so no chest id can collide.</summary>
    internal const long PlayerUserId = 99L;

    /// <summary>The threshold of base D: 100, so chest 303 stays source-only.</summary>
    internal const int DThreshold = 100;

    /// <summary>The player inventory id (contract 6.1).</summary>
    internal static ContainerId PlayerId => new ContainerId(PlayerUserId, 1u);

    /// <summary>The favourite slot of inventory I: (5, 1), which holds Resin.</summary>
    internal static InventorySlot FavouriteSlot => new InventorySlot(5, 1);

    /// <summary>One occupied slot.</summary>
    internal static PlayerInventoryStack At(int x, int y, ItemStackSnapshot stack, InventoryItemTraits traits = InventoryItemTraits.None) =>
        new PlayerInventoryStack(new InventorySlot(x, y), stack, traits);

    /// <summary>An 8 x 4 inventory with the given stacks, in the given order.</summary>
    internal static PlayerInventorySnapshot Inventory(params PlayerInventoryStack[] stacks) =>
        new PlayerInventorySnapshot(PlayerId, 8, 4, stacks);

    /// <summary>The contract 6.1 kinds: the Organize 6.1 kinds plus the never-list items.</summary>
    internal static Dictionary<string, ItemKind> DepositKinds()
    {
        Dictionary<string, ItemKind> kinds = Kinds();
        kinds["HelmetBronze"] = ItemKind.Armour;
        kinds["Hammer"] = ItemKind.Tool;
        kinds["Hoe"] = ItemKind.Tool;
        kinds["Cultivator"] = ItemKind.Tool;
        kinds["FishingRod"] = ItemKind.Tool;
        kinds["MeadHealthMinor"] = ItemKind.Other;
        kinds["FishingBait"] = ItemKind.Other;
        kinds["BoltIron"] = ItemKind.Ammo;
        return kinds;
    }

    /// <summary>Exclusions with the default never list and the given favourite slots.</summary>
    internal static DepositExclusions DefaultExclusions(params InventorySlot[] favourites) =>
        new DepositExclusions(new FavouriteSlots(favourites), SettingsCatalog.DepositNeverItemsDefault);

    /// <summary>Exclusions with no favourites and an empty never list.</summary>
    internal static DepositExclusions NoExclusions() => new DepositExclusions(FavouriteSlots.Empty, Array.Empty<string>());

    /// <summary>Exclusions with no favourites and the given never list.</summary>
    internal static DepositExclusions NeverList(params string[] entries) => new DepositExclusions(FavouriteSlots.Empty, entries);

    /// <summary>The 6.2 chests of base D, in snapshot order. A fresh array on every call.</summary>
    internal static ContainerSnapshot[] DChests() => new[]
    {
        C(301, 0, 0, 2, S("Wood", 30)),
        C(302, 4, 0, 1, S("Stone", 50)),
        C(303, 8, 0, 2, S("Flint", 5), S("Wood", 5)),
        C(304, 20, 0, 1),
        C(305, 12, 0, 1, S("Wood", 10)),
        C(306, 2, 0, 2),
    };

    /// <summary>Base D of contract 6.2: 301 Wood, 302 Stone, 305 Keep, threshold 100, the 6.1 catalog and kinds.</summary>
    internal static PlanInput D() => new PlanInput(
        DChests(),
        Assign(
            (301, CategoryAssignment.ForCategory("Wood")),
            (302, CategoryAssignment.ForCategory("Stone")),
            (305, CategoryAssignment.Keep)),
        new Dictionary<ContainerId, string>(),
        Catalog(),
        DepositKinds(),
        Options(),
        DThreshold);

    /// <summary>A base input over any chests with the Deposit kinds and the 6.1 catalog.</summary>
    internal static PlanInput Base(ContainerSnapshot[] containers, Dictionary<ContainerId, CategoryAssignment>? assignments = null, int threshold = DThreshold) =>
        new PlanInput(
            containers,
            assignments ?? new Dictionary<ContainerId, CategoryAssignment>(),
            new Dictionary<ContainerId, string>(),
            Catalog(),
            DepositKinds(),
            Options(),
            threshold);

    /// <summary>Inventory I of contract 6.3: 12 stacks, indices 0 to 11 in this order.</summary>
    internal static PlayerInventorySnapshot I() => Inventory(
        At(0, 0, S("SwordIron", 1, 1, 2), InventoryItemTraits.Equipped),
        At(1, 0, S("CookedMeat", 5)),
        At(0, 1, S("Wood", 50)),
        At(1, 1, S("Wood", 40)),
        At(2, 1, S("Stone", 30)),
        At(3, 1, S("Flint", 20)),
        At(4, 1, S("HelmetBronze", 1, 1), InventoryItemTraits.Equipped),
        At(5, 1, S("Resin", 12)),
        At(6, 1, S("ArrowWood", 20)),
        At(7, 1, S("Hammer", 1, 1)),
        At(0, 2, S("TrophyDeer", 1, 20)),
        At(1, 2, S("Wood", 10)));

    /// <summary>Calls <c>DepositPlanner.Plan</c> with an inventory, exclusions and a base input.</summary>
    internal static DepositPlan Run(PlayerInventorySnapshot player, DepositExclusions exclusions, PlanInput input) =>
        DepositPlanner.Plan(player, exclusions, input.Containers, input.Assignments, input.InUseBy, input.Catalog, input.Kinds, input.Options, input.Threshold);

    /// <summary>The contract 6.3 Deposit: inventory I into base D with the default never list and (5, 1) favourite.</summary>
    internal static DepositPlan RunI() => Run(I(), DefaultExclusions(FavouriteSlot), D());

    /// <summary>The excluded entries of a plan as (index, reasons) rows.</summary>
    internal static (int Index, DepositExclusion Reasons)[] ExcludedOf(DepositPlan plan)
    {
        var rows = new (int, DepositExclusion)[plan.Excluded.Count];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = (plan.Excluded[i].StackIndex, plan.Excluded[i].Reasons);
        }

        return rows;
    }
}
