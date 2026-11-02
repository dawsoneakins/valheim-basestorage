namespace BaseStorage.Core.Organize.Deposit;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Plans a Deposit (spec AC-4.9, deposit contract section 5): the player inventory is the only source and the base's category
/// chests are the destinations, filled by the Organize planner's own placement (merge first, then empty slots, then claimed overflow
/// chests). Hotbar, equipped, favourite and never-list stacks are not sources: they appear in no move and no no-room entry. Base
/// chests never give up anything in a Deposit. Pure: it reads its arguments, does no I/O and never applies the plan.
/// </summary>
public static class DepositPlanner
{
    /// <summary>The grid row of the hotbar: row 0, the top row of the game's inventory grid (spec AC-4.9 "row 1").</summary>
    public const int HotbarRow = 0;

    /// <summary>Plans a Deposit of one inventory into one base snapshot. Pure.</summary>
    /// <param name="player">The player inventory with every occupied slot. Not null.</param>
    /// <param name="exclusions">The favourite slots and the never list. Not null.</param>
    /// <param name="containers">The base's member chests, in snapshot order, passed unfiltered. Not null, no null elements, no duplicate ids, not the inventory id.</param>
    /// <param name="assignments">The persisted assignment per chest id. A missing id means unassigned. Not null, no null values.</param>
    /// <param name="inUseBy">The player name per chest id for chests another player has open. Not null, no null values.</param>
    /// <param name="catalog">The current category catalog. Not null.</param>
    /// <param name="itemKinds">The item kind per prefab name; a missing prefab is Other. Not null.</param>
    /// <param name="options">The eligibility options; only ExcludeVehicles matters.</param>
    /// <param name="autoAssignThresholdPct">The threshold in percent, passed verbatim to the auto-assign resolver.</param>
    /// <param name="spill">True lets a stack with no room in its own category go to the nearest related chest.</param>
    /// <returns>The plan: the inventory's moves and no-room entries, the skipped chests, the inventory layout first and one layout per usable chest, and the excluded stacks.</returns>
    /// <exception cref="ArgumentNullException">Thrown for a null <paramref name="player"/>, <paramref name="exclusions"/>, <paramref name="containers"/>, <paramref name="assignments"/>, <paramref name="inUseBy"/>, <paramref name="catalog"/> or <paramref name="itemKinds"/>.</exception>
    /// <exception cref="ArgumentException">Thrown for a null element, a duplicate id or the inventory id in <paramref name="containers"/>, or a null value in <paramref name="assignments"/> or <paramref name="inUseBy"/>.</exception>
    public static DepositPlan Plan(
        PlayerInventorySnapshot player,
        DepositExclusions exclusions,
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        IReadOnlyDictionary<ContainerId, string> inUseBy,
        CategoryCatalog catalog,
        IReadOnlyDictionary<string, ItemKind> itemKinds,
        EligibilityOptions options,
        int autoAssignThresholdPct,
        bool spill = false)
    {
        if (player is null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (exclusions is null)
        {
            throw new ArgumentNullException(nameof(exclusions));
        }

        OrganizePlanner.Validate(containers, assignments, inUseBy, catalog, itemKinds);
        for (var c = 0; c < containers.Count; c++)
        {
            if (containers[c].Id == player.Id)
            {
                throw new ArgumentException("containers contains the player inventory id.", nameof(containers));
            }
        }

        var reasons = Exclude(player, exclusions, itemKinds);

        var skipped = new List<SkippedChest>();
        var usable = new List<ContainerSnapshot>(containers.Count);
        OrganizePlanner.Classify(containers, assignments, inUseBy, options, skipped, usable);

        var auto = AutoAssigner.Resolve(usable, assignments, catalog, itemKinds, autoAssignThresholdPct);
        var pool = auto.CreateOverflowPool();
        var byId = new Dictionary<ContainerId, WorkingChest>(usable.Count);
        var chests = OrganizePlanner.CreateWorkingChests(auto, byId);
        var source = new WorkingChest(player.Container, -1, AutoAssignOutcome.SourceOnly, null);

        var moves = new List<PlannedMove>();
        var noRoom = new List<NoRoomEntry>();
        var claimed = new List<WorkingChest>();
        var placement = new OrganizePlanner.Placement(byId, auto, pool, catalog, itemKinds, moves, noRoom, claimed);
        for (var i = 0; i < reasons.Length; i++)
        {
            if (reasons[i] == DepositExclusion.None)
            {
                placement.Place(source, i);
            }
        }

        if (spill && noRoom.Count > 0)
        {
            noRoom.Clear();
            placement.Spilling = true;
            for (var i = 0; i < reasons.Length; i++)
            {
                if (reasons[i] == DepositExclusion.None && source.Stacks[i].Count > 0)
                {
                    placement.Place(source, i);
                }
            }
        }

        var layouts = new ChestLayout[chests.Length + 1];
        layouts[0] = OrganizePlanner.BuildLayout(source);
        for (var i = 0; i < chests.Length; i++)
        {
            layouts[i + 1] = OrganizePlanner.BuildLayout(chests[i]);
        }

        var claimedLayouts = new ChestLayout[claimed.Count];
        for (var i = 0; i < claimedLayouts.Length; i++)
        {
            claimedLayouts[i] = layouts[claimed[i].Index + 1];
        }

        var excluded = new List<ExcludedStack>();
        for (var i = 0; i < reasons.Length; i++)
        {
            if (reasons[i] != DepositExclusion.None)
            {
                excluded.Add(new ExcludedStack(i, player.Stacks[i], reasons[i]));
            }
        }

        var plan = new OrganizePlan(auto, moves.ToArray(), noRoom.ToArray(), skipped.ToArray(), layouts, claimedLayouts, placement.Spilled, placement.SpillRules());
        return new DepositPlan(plan, player, excluded.ToArray());
    }

    /// <summary>
    /// Decides the per-stack exclusions of one stack (deposit contract 5.1 rules D1 to D4 and D6): hotbar row, equipped, favourite slot,
    /// never list, and a count above the max stack. The same-item rule (D5) needs the whole inventory and is applied by <see cref="Plan"/> only.
    /// </summary>
    /// <param name="stack">The stack; not null.</param>
    /// <param name="exclusions">The favourites and never list; not null.</param>
    /// <param name="kind">The item kind of the stack's prefab, as the kinds table gives it (Other when absent).</param>
    /// <returns>Every reason that applies, or None when the stack may be deposited as far as these rules go.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stack"/> or <paramref name="exclusions"/> is null.</exception>
    public static DepositExclusion ExclusionOf(PlayerInventoryStack stack, DepositExclusions exclusions, ItemKind kind)
    {
        if (stack is null)
        {
            throw new ArgumentNullException(nameof(stack));
        }

        if (exclusions is null)
        {
            throw new ArgumentNullException(nameof(exclusions));
        }

        var result = DepositExclusion.None;
        if (stack.Slot.Y == HotbarRow)
        {
            result |= DepositExclusion.Hotbar;
        }

        if (stack.IsEquipped)
        {
            result |= DepositExclusion.Equipped;
        }

        if (exclusions.Favourites.Contains(stack.Slot))
        {
            result |= DepositExclusion.Favourite;
        }

        if (exclusions.IsNeverItem(stack, kind))
        {
            result |= DepositExclusion.NeverList;
        }

        if (stack.Stack.Count > stack.Stack.MaxStack)
        {
            result |= DepositExclusion.InvalidStack;
        }

        return result;
    }

    /// <summary>The reasons per stack: the per-stack rules, then the same-item rule over the stacks excluded by them.</summary>
    private static DepositExclusion[] Exclude(PlayerInventorySnapshot player, DepositExclusions exclusions, IReadOnlyDictionary<string, ItemKind> itemKinds)
    {
        var stacks = player.Stacks;
        var reasons = new DepositExclusion[stacks.Count];
        var excludedKeys = new HashSet<ItemKey>();
        for (var i = 0; i < reasons.Length; i++)
        {
            var stack = stacks[i].Stack;
            var kind = itemKinds.TryGetValue(stack.PrefabName, out var known) ? known : ItemKind.Other;
            reasons[i] = ExclusionOf(stacks[i], exclusions, kind);
            if (reasons[i] != DepositExclusion.None)
            {
                excludedKeys.Add(new ItemKey(stack.PrefabName, stack.Quality, stack.MaxStack));
            }
        }

        for (var i = 0; i < reasons.Length; i++)
        {
            var stack = stacks[i].Stack;
            if (reasons[i] == DepositExclusion.None && excludedKeys.Contains(new ItemKey(stack.PrefabName, stack.Quality, stack.MaxStack)))
            {
                reasons[i] = DepositExclusion.SameItemAsExcluded;
            }
        }

        return reasons;
    }
}
