namespace BaseStorage.Core.Organize;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Plans an Organize of one base snapshot (S-E004-006, contract organize-plan-contracts.md v1). Given the member chests, the
/// persisted assignments and the in-use names, it returns a move plan. It is pure: it reads its arguments, does no I/O and
/// never applies the plan.
/// </summary>
public static class OrganizePlanner
{
    private const string ReasonTombstone = "Ineligible: tombstone";
    private const string ReasonVehicle = "Ineligible: vehicle";
    private const string ReasonNotPlayerBuilt = "Ineligible: not player-built";
    private const string ReasonAccessDenied = "Ineligible: access denied";
    private const string ReasonKeep = "Keep";
    private const string ReasonInUsePrefix = "In use by ";
    private const string ReasonInUseGeneric = "In use by another player";
    private const string ReasonMoreStacksThanSlots = "Invalid contents: more stacks than slots";
    private const string ReasonStackAboveMaxStack = "Invalid contents: stack above max stack";

    /// <summary>
    /// Plans an Organize of one base snapshot; when <paramref name="autoAssignMixedChests"/> is true, the mixed-chest step runs
    /// after the threshold step (S-A-001), spare chests go to the categories short of the most room, and a stack with no room in its own
    /// category moves to the nearest related chest. Pure (P1).
    /// </summary>
    /// <param name="containers">The base's member chests, in snapshot order, passed unfiltered. Not null, no null elements, no duplicate ids.</param>
    /// <param name="assignments">The persisted assignment per chest id. A missing id means unassigned. Not null, no null values.</param>
    /// <param name="inUseBy">The player name per chest id for chests another player has open. Not null, no null values.</param>
    /// <param name="catalog">The current category catalog. Not null.</param>
    /// <param name="itemKinds">The item kind per prefab name; a missing prefab is Other. Not null.</param>
    /// <param name="options">The eligibility options; only ExcludeVehicles matters for Organize.</param>
    /// <param name="autoAssignThresholdPct">The threshold in percent, passed verbatim to the auto-assign resolver.</param>
    /// <param name="autoAssignMixedChests">True runs the mixed-chest step after the threshold step (the AutoAssignMixedChests setting); false keeps mixed chests as unassigned sources (v0.1).</param>
    /// <returns>The immutable plan, with one layout or one skip entry per input container.</returns>
    /// <exception cref="ArgumentNullException">Thrown for a null <paramref name="containers"/>, <paramref name="assignments"/>, <paramref name="inUseBy"/>, <paramref name="catalog"/> or <paramref name="itemKinds"/>.</exception>
    /// <exception cref="ArgumentException">Thrown for a null element or a duplicate id in <paramref name="containers"/>, or a null value in <paramref name="assignments"/> or <paramref name="inUseBy"/>.</exception>
    public static OrganizePlan Plan(
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        IReadOnlyDictionary<ContainerId, string> inUseBy,
        CategoryCatalog catalog,
        IReadOnlyDictionary<string, ItemKind> itemKinds,
        EligibilityOptions options,
        int autoAssignThresholdPct,
        bool autoAssignMixedChests)
    {
        Validate(containers, assignments, inUseBy, catalog, itemKinds);

        var skipped = new List<SkippedChest>();
        var usable = new List<ContainerSnapshot>(containers.Count);
        Classify(containers, assignments, inUseBy, options, skipped, usable);

        var auto = AutoAssigner.Resolve(usable, assignments, catalog, itemKinds, autoAssignThresholdPct);
        if (autoAssignMixedChests)
        {
            auto = AutoAssigner.AssignMixedChests(auto, catalog, itemKinds);
        }

        var pool = auto.CreateOverflowPool();

        var byId = new Dictionary<ContainerId, WorkingChest>(usable.Count);
        var chests = CreateWorkingChests(auto, byId);

        var moves = new List<PlannedMove>();
        var noRoom = new List<NoRoomEntry>();
        var claimed = new List<WorkingChest>();
        var shares = autoAssignMixedChests ? SpareChestShares.Of(auto, catalog, itemKinds) : null;
        var placement = new Placement(byId, auto, pool, catalog, itemKinds, moves, noRoom, claimed, shares);
        if (autoAssignMixedChests)
        {
            PlaceEverythingPossible(placement, chests, moves, noRoom);
        }
        else
        {
            PlaceUntilNothingMoves(placement, chests, moves, noRoom);
        }

        var layouts = new ChestLayout[chests.Length];
        for (var i = 0; i < chests.Length; i++)
        {
            layouts[i] = BuildLayout(chests[i]);
        }

        var claimedLayouts = new ChestLayout[claimed.Count];
        for (var i = 0; i < claimedLayouts.Length; i++)
        {
            claimedLayouts[i] = layouts[claimed[i].Index];
        }

        return new OrganizePlan(auto, moves.ToArray(), noRoom.ToArray(), skipped.ToArray(), layouts, claimedLayouts, placement.Spilled, placement.SpillRules());
    }

    /// <summary>
    /// Places every stack that can reach a better chest: first into its own category, then, once nothing else moves, into the nearest
    /// related chest. A stack that spills frees a slot, which may let another stack get home, so the two repeat until nothing moves.
    /// </summary>
    private static void PlaceEverythingPossible(Placement placement, WorkingChest[] chests, List<PlannedMove> moves, List<NoRoomEntry> noRoom)
    {
        int before;
        do
        {
            before = moves.Count;
            PlaceUntilNothingMoves(placement, chests, moves, noRoom);
            if (noRoom.Count == 0)
            {
                return;
            }

            placement.AllowAnyClaim();
            PlaceUntilNothingMoves(placement, chests, moves, noRoom);
            if (noRoom.Count == 0)
            {
                return;
            }

            noRoom.Clear();
            placement.Spilling = true;
            for (var c = 0; c < chests.Length; c++)
            {
                placement.PlaceAll(chests[c]);
            }

            placement.Spilling = false;
        }
        while (moves.Count > before && noRoom.Count > 0);
    }

    /// <summary>
    /// Repeats placement passes (S-A-002): a stack that found no room may fit once a later stack has left its chest. Moves stay in plan
    /// order, so each one lands in a slot that is already free when it runs. NoRoom always holds the last pass only.
    /// </summary>
    private static void PlaceUntilNothingMoves(Placement placement, WorkingChest[] chests, List<PlannedMove> moves, List<NoRoomEntry> noRoom)
    {
        int before;
        do
        {
            before = moves.Count;
            noRoom.Clear();
            for (var c = 0; c < chests.Length; c++)
            {
                placement.PlaceAll(chests[c]);
            }
        }
        while (moves.Count > before && noRoom.Count > 0);
    }

    /// <summary>Plans an Organize of one base snapshot. Pure; see contract section 0.2.</summary>
    /// <param name="containers">The base's member chests, in snapshot order, passed unfiltered. Not null, no null elements, no duplicate ids.</param>
    /// <param name="assignments">The persisted assignment per chest id. A missing id means unassigned. Not null, no null values.</param>
    /// <param name="inUseBy">The player name per chest id for chests another player has open. Not null, no null values.</param>
    /// <param name="catalog">The current category catalog. Not null.</param>
    /// <param name="itemKinds">The item kind per prefab name; a missing prefab is Other. Not null.</param>
    /// <param name="options">The eligibility options; only ExcludeVehicles matters for Organize.</param>
    /// <param name="autoAssignThresholdPct">The threshold in percent, passed verbatim to the auto-assign resolver.</param>
    /// <returns>The immutable plan, with one layout or one skip entry per input container.</returns>
    /// <exception cref="ArgumentNullException">Thrown for a null <paramref name="containers"/>, <paramref name="assignments"/>, <paramref name="inUseBy"/>, <paramref name="catalog"/> or <paramref name="itemKinds"/>.</exception>
    /// <exception cref="ArgumentException">Thrown for a null element or a duplicate id in <paramref name="containers"/>, or a null value in <paramref name="assignments"/> or <paramref name="inUseBy"/>.</exception>
    public static OrganizePlan Plan(
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        IReadOnlyDictionary<ContainerId, string> inUseBy,
        CategoryCatalog catalog,
        IReadOnlyDictionary<string, ItemKind> itemKinds,
        EligibilityOptions options,
            int autoAssignThresholdPct)
    {
        return Plan(containers, assignments, inUseBy, catalog, itemKinds, options, autoAssignThresholdPct, false);
    }

    /// <summary>Checks the arguments in the contract 5.2 order. Deposit (S-E004-013) reuses it for its base arguments.</summary>
    internal static void Validate(
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        IReadOnlyDictionary<ContainerId, string> inUseBy,
        CategoryCatalog catalog,
        IReadOnlyDictionary<string, ItemKind> itemKinds)
    {
        if (containers is null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        var seen = new HashSet<ContainerId>();
        for (var c = 0; c < containers.Count; c++)
        {
            var container = containers[c];
            if (container is null)
            {
                throw new ArgumentException("containers contains a null element.", nameof(containers));
            }

            if (!seen.Add(container.Id))
            {
                throw new ArgumentException("containers contains a duplicate id.", nameof(containers));
            }
        }

        if (assignments is null)
        {
            throw new ArgumentNullException(nameof(assignments));
        }

        RequireNoNullAssignments(assignments, containers);

        if (inUseBy is null)
        {
            throw new ArgumentNullException(nameof(inUseBy));
        }

        RequireNoNullNames(inUseBy, containers);

        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (itemKinds is null)
        {
            throw new ArgumentNullException(nameof(itemKinds));
        }
    }

    /// <summary>Rejects a null assignment: every value when the dictionary is a collection, else the values of the input ids.</summary>
    private static void RequireNoNullAssignments(
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        IReadOnlyList<ContainerSnapshot> containers)
    {
        if (assignments is ICollection<KeyValuePair<ContainerId, CategoryAssignment>> all)
        {
            var copy = new KeyValuePair<ContainerId, CategoryAssignment>[all.Count];
            all.CopyTo(copy, 0);
            for (var i = 0; i < copy.Length; i++)
            {
                if (copy[i].Value is null)
                {
                    throw new ArgumentException("assignments contains a null value.", nameof(assignments));
                }
            }

            return;
        }

        for (var c = 0; c < containers.Count; c++)
        {
            if (assignments.TryGetValue(containers[c].Id, out var value) && value is null)
            {
                throw new ArgumentException("assignments contains a null value.", nameof(assignments));
            }
        }
    }

    /// <summary>Rejects a null player name: every value when the dictionary is a collection, else the values of the input ids.</summary>
    private static void RequireNoNullNames(
        IReadOnlyDictionary<ContainerId, string> inUseBy,
        IReadOnlyList<ContainerSnapshot> containers)
    {
        if (inUseBy is ICollection<KeyValuePair<ContainerId, string>> all)
        {
            var copy = new KeyValuePair<ContainerId, string>[all.Count];
            all.CopyTo(copy, 0);
            for (var i = 0; i < copy.Length; i++)
            {
                if (copy[i].Value is null)
                {
                    throw new ArgumentException("inUseBy contains a null value.", nameof(inUseBy));
                }
            }

            return;
        }

        for (var c = 0; c < containers.Count; c++)
        {
            if (inUseBy.TryGetValue(containers[c].Id, out var value) && value is null)
            {
                throw new ArgumentException("inUseBy contains a null value.", nameof(inUseBy));
            }
        }
    }

    /// <summary>Splits the containers into skipped entries and usable chests with the contract 3.1 precedence. Deposit reuses it.</summary>
    internal static void Classify(
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        IReadOnlyDictionary<ContainerId, string> inUseBy,
        EligibilityOptions options,
        List<SkippedChest> skipped,
        List<ContainerSnapshot> usable)
    {
        for (var c = 0; c < containers.Count; c++)
        {
            var container = containers[c];
            var isKeep = assignments.TryGetValue(container.Id, out var assigned) && assigned.IsKeep;
            var exclusion = ContainerEligibility.GetExclusionReason(container, isKeep, ContainerPurpose.Organize, options);
            if (exclusion.HasValue)
            {
                skipped.Add(Excluded(container, exclusion.Value));
                continue;
            }

            var inUse = (container.Flags & ContainerFlags.InUseByOther) != 0;
            if (inUseBy.TryGetValue(container.Id, out var name))
            {
                inUse = true;
            }
            else
            {
                name = null;
            }

            if (inUse)
            {
                var known = !string.IsNullOrWhiteSpace(name);
                skipped.Add(new SkippedChest(
                    container,
                    SkipReason.InUse,
                    known ? ReasonInUsePrefix + name : ReasonInUseGeneric,
                    known ? name : null,
                    null));
                continue;
            }

            if (container.Contents.Count > container.SlotCount)
            {
                skipped.Add(new SkippedChest(container, SkipReason.InvalidContents, ReasonMoreStacksThanSlots, null, null));
                continue;
            }

            if (HasStackAboveMaxStack(container))
            {
                skipped.Add(new SkippedChest(container, SkipReason.InvalidContents, ReasonStackAboveMaxStack, null, null));
                continue;
            }

            usable.Add(container);
        }
    }

    private static SkippedChest Excluded(ContainerSnapshot container, ExclusionReason exclusion)
    {
        switch (exclusion)
        {
            case ExclusionReason.Tombstone:
                return new SkippedChest(container, SkipReason.Ineligible, ReasonTombstone, null, exclusion);
            case ExclusionReason.Vehicle:
                return new SkippedChest(container, SkipReason.Ineligible, ReasonVehicle, null, exclusion);
            case ExclusionReason.NotPlayerBuilt:
                return new SkippedChest(container, SkipReason.Ineligible, ReasonNotPlayerBuilt, null, exclusion);
            case ExclusionReason.AccessDenied:
                return new SkippedChest(container, SkipReason.Ineligible, ReasonAccessDenied, null, exclusion);
            default:
                return new SkippedChest(container, SkipReason.Keep, ReasonKeep, null, exclusion);
        }
    }

    private static bool HasStackAboveMaxStack(ContainerSnapshot container)
    {
        var contents = container.Contents;
        for (var i = 0; i < contents.Count; i++)
        {
            if (contents[i].Count > contents[i].MaxStack)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Creates the working state of every usable chest, in snapshot order, and indexes it by id.</summary>
    /// <param name="auto">The auto-assign result over the usable chests.</param>
    /// <param name="byId">The index to fill: one working chest per usable chest id.</param>
    /// <returns>The working chests in snapshot order; each one's Index is its position in the array.</returns>
    internal static WorkingChest[] CreateWorkingChests(AutoAssignResult auto, Dictionary<ContainerId, WorkingChest> byId)
    {
        var chests = new WorkingChest[auto.Chests.Count];
        for (var i = 0; i < chests.Length; i++)
        {
            var entry = auto.Chests[i];
            chests[i] = new WorkingChest(entry.Container, i, entry.Outcome, entry.CategoryName);
            byId[entry.Id] = chests[i];
        }

        return chests;
    }

    /// <summary>Tops up the destination's same-item stacks below their limit, in slot order (5.1 step 1).</summary>
    private static int MergeInto(WorkingChest destination, ItemKey key, int remaining, List<WorkingChest> targets, List<int> counts)
    {
        var stacks = destination.Stacks;
        for (var k = 0; k < stacks.Count && remaining > 0; k++)
        {
            var target = stacks[k];
            if (target.Count == 0 || target.Count >= key.MaxStack || !target.Key.Equals(key))
            {
                continue;
            }

            var taken = Math.Min(remaining, key.MaxStack - target.Count);
            target.Count += taken;
            remaining -= taken;
            Record(targets, counts, destination, taken);
        }

        return remaining;
    }

    /// <summary>Adds new stacks of at most the limit to the destination's free slots (5.1 step 2).</summary>
    private static int FillEmptySlots(WorkingChest destination, ItemKey key, int remaining, List<WorkingChest> targets, List<int> counts)
    {
        var capacity = destination.Container.SlotCount;
        while (remaining > 0 && destination.UsedSlots < capacity)
        {
            var taken = Math.Min(remaining, key.MaxStack);
            destination.Stacks.Add(new WorkingStack(key, taken));
            destination.UsedSlots++;
            remaining -= taken;
            Record(targets, counts, destination, taken);
        }

        return remaining;
    }

    /// <summary>Adds a partial transfer to the per-destination sums of the current source stack, in first-use order (4.3).</summary>
    private static void Record(List<WorkingChest> targets, List<int> counts, WorkingChest destination, int taken)
    {
        for (var i = 0; i < targets.Count; i++)
        {
            if (ReferenceEquals(targets[i], destination))
            {
                counts[i] += taken;
                return;
            }
        }

        targets.Add(destination);
        counts.Add(taken);
    }

    /// <summary>Resolves a prefab's category once (P6) and caches it for the rest of the call.</summary>
    private static string CategoryOf(
        string prefabName,
        CategoryCatalog catalog,
        IReadOnlyDictionary<string, ItemKind> itemKinds,
        Dictionary<string, string> cache)
    {
        if (cache.TryGetValue(prefabName, out var cached))
        {
            return cached;
        }

        var kind = itemKinds.TryGetValue(prefabName, out var known) ? known : ItemKind.Other;
        var category = catalog.Resolve(prefabName, kind);
        cache[prefabName] = category;
        return category;
    }

    /// <summary>Returns the usable category chests of one category, in snapshot order. The planner reads this once per category (P12).</summary>
    /// <param name="auto">The auto-assign result over the usable chests.</param>
    /// <param name="categoryName">The category name, compared ordinally.</param>
    /// <returns>The usable category chests of the category.</returns>
    private static IReadOnlyList<ContainerSnapshot> CategoryChests(AutoAssignResult auto, string categoryName) =>
        auto.GetCategoryChests(categoryName);

    /// <summary>Builds the final layout of one usable chest (4.6). Deposit reuses it for the base chests and the player inventory.</summary>
    internal static ChestLayout BuildLayout(WorkingChest chest)
    {
        var role = chest.Role;
        switch (role)
        {
            case ChestRole.Category:
            case ChestRole.ClaimedOverflow:
                return new ChestLayout(chest.Container, role, chest.CategoryName, LayoutCompactor.Compact(chest.Stacks));
            case ChestRole.Source:
                return new ChestLayout(chest.Container, role, null, RemainingStacks(chest.Stacks));
            default:
                return new ChestLayout(chest.Container, role, null, Array.Empty<ItemStackSnapshot>());
        }
    }

    /// <summary>The working stacks that still hold items, in input order, as new snapshots.</summary>
    private static ItemStackSnapshot[] RemainingStacks(List<WorkingStack> stacks)
    {
        var result = new List<ItemStackSnapshot>(stacks.Count);
        for (var i = 0; i < stacks.Count; i++)
        {
            var stack = stacks[i];
            if (stack.Count > 0)
            {
                result.Add(new ItemStackSnapshot(stack.Key.PrefabName, stack.Count, stack.Key.Quality, stack.Key.MaxStack));
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// The contract 5.1 fill state of one Plan call: the usable destinations, the overflow pool, the per-call caches (P6, P12) and the
    /// output lists. Organize drives it over every usable chest; Deposit (S-E004-013) drives it with the player inventory as the only
    /// source. Never escapes the call that created it.
    /// </summary>
    internal sealed class Placement
    {
        private readonly Dictionary<ContainerId, WorkingChest> byId;
        private readonly AutoAssignResult auto;
        private readonly OverflowPool pool;
        private readonly CategoryCatalog catalog;
        private readonly IReadOnlyDictionary<string, ItemKind> itemKinds;
        private readonly List<PlannedMove> moves;
        private readonly List<NoRoomEntry> noRoom;
        private readonly List<WorkingChest> claimed;
        private Dictionary<string, int>? claimsLeft;
        private readonly Dictionary<string, string> categoryByPrefab = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<WorkingChest>> destinationsByCategory = new Dictionary<string, List<WorkingChest>>(StringComparer.Ordinal);
        private readonly List<WorkingChest> moveTargets = new List<WorkingChest>();
        private readonly List<int> moveCounts = new List<int>();
        private readonly HashSet<WorkingChest> spillHosts = new HashSet<WorkingChest>();

        /// <summary>Creates the fill state over the usable chests of one call.</summary>
        /// <param name="byId">The working chest per usable chest id; the destinations are looked up here.</param>
        /// <param name="auto">The auto-assign result over the usable chests.</param>
        /// <param name="pool">The overflow pool of this call.</param>
        /// <param name="catalog">The current category catalog.</param>
        /// <param name="itemKinds">The item kind per prefab name.</param>
        /// <param name="moves">The list that receives the planned moves.</param>
        /// <param name="noRoom">The list that receives the no-room entries.</param>
        /// <param name="claimed">The list that receives the claimed overflow chests, in claim order.</param>
        /// <param name="claimsLeft">The empty chests each category may claim, or null when any category may claim any chest.</param>
        internal Placement(
            Dictionary<ContainerId, WorkingChest> byId,
            AutoAssignResult auto,
            OverflowPool pool,
            CategoryCatalog catalog,
            IReadOnlyDictionary<string, ItemKind> itemKinds,
            List<PlannedMove> moves,
            List<NoRoomEntry> noRoom,
            List<WorkingChest> claimed,
            Dictionary<string, int>? claimsLeft = null)
        {
            this.claimsLeft = claimsLeft;
            this.byId = byId;
            this.auto = auto;
            this.pool = pool;
            this.catalog = catalog;
            this.itemKinds = itemKinds;
            this.moves = moves;
            this.noRoom = noRoom;
            this.claimed = claimed;
        }

        /// <summary>Gets or sets a value indicating whether a stack with no room in its own category may move to the nearest related chest.</summary>
        internal bool Spilling { get; set; }

        /// <summary>Gets the number of stacks that moved to a chest of another category.</summary>
        internal int Spilled { get; private set; }

        /// <summary>Lists the chests that took a stack of another category and have no saved category, so a guest cannot flip them later.</summary>
        /// <returns>The rules, in snapshot order.</returns>
        internal ChestAutoAssignment[] SpillRules()
        {
            var mixed = new HashSet<ContainerId>();
            for (var i = 0; i < auto.MixedAssigned.Count; i++)
            {
                mixed.Add(auto.MixedAssigned[i].Id);
            }

            var hosts = new List<WorkingChest>();
            foreach (var host in spillHosts)
            {
                if (host.Outcome != AutoAssignOutcome.AlreadyAssigned && !mixed.Contains(host.Container.Id))
                {
                    hosts.Add(host);
                }
            }

            hosts.Sort(static (a, b) => a.Index.CompareTo(b.Index));
            var rules = new ChestAutoAssignment[hosts.Count];
            for (var i = 0; i < rules.Length; i++)
            {
                rules[i] = new ChestAutoAssignment(hosts[i].Container, AutoAssignOutcome.Assigned, CategoryAssignment.ForCategory(hosts[i].CategoryName!));
            }

            return rules;
        }

        /// <summary>Drops the share-out, once every category has had its share, so a leftover empty chest helps whoever still needs room.</summary>
        internal void AllowAnyClaim() => claimsLeft = null;

        /// <summary>Places every input stack of one source chest, in Contents order (5.1).</summary>
        /// <param name="chest">The source chest.</param>
        internal void PlaceAll(WorkingChest chest)
        {
            var inputCount = chest.Container.Contents.Count;
            for (var i = 0; i < inputCount; i++)
            {
                // A stack that left in an earlier pass already gave its slot back.
                if (chest.Stacks[i].Count > 0)
                {
                    Place(chest, i);
                }
            }
        }

        /// <summary>Resolves a prefab's category once per call (P6).</summary>
        /// <param name="prefabName">The prefab name.</param>
        /// <returns>The category name.</returns>
        internal string CategoryOf(string prefabName) => OrganizePlanner.CategoryOf(prefabName, catalog, itemKinds, categoryByPrefab);

        /// <summary>
        /// Places one input stack of a source (5.1): a stack already in its chest's category stays; otherwise it is merged into the
        /// category chests, then into their empty slots, then into overflow chests claimed one at a time, and any remainder becomes a
        /// no-room entry. One move per destination used is recorded, in first-use order.
        /// </summary>
        /// <param name="chest">The source chest; its CategoryName is null for a source-only chest or the player inventory.</param>
        /// <param name="i">The index of the stack in the source's input Contents.</param>
        internal void Place(WorkingChest chest, int i)
        {
            var stack = chest.Stacks[i];
            var key = stack.Key;
            var category = CategoryOf(key.PrefabName);
            if (string.Equals(category, chest.CategoryName, StringComparison.Ordinal))
            {
                return;
            }

            var destinations = DestinationsOf(category);
            var remaining = stack.Count;
            moveTargets.Clear();
            moveCounts.Clear();

            for (var d = 0; d < destinations.Count && remaining > 0; d++)
            {
                remaining = MergeInto(destinations[d], key, remaining, moveTargets, moveCounts);
            }

            for (var d = 0; d < destinations.Count && remaining > 0; d++)
            {
                remaining = FillEmptySlots(destinations[d], key, remaining, moveTargets, moveCounts);
            }

            while (remaining > 0 && MayClaim(category))
            {
                var overflow = pool.Claim(category);
                if (overflow is null)
                {
                    break;
                }

                var destination = byId[overflow.Id];
                destination.Claim(category);
                destinations.Add(destination);
                claimed.Add(destination);
                remaining = FillEmptySlots(destination, key, remaining, moveTargets, moveCounts);
            }

            if (remaining > 0 && Spilling)
            {
                var left = Spill(chest, key, category, remaining);
                if (left < remaining)
                {
                    Spilled++;
                }

                remaining = left;
            }

            if (remaining > 0)
            {
                noRoom.Add(new NoRoomEntry(chest.Container.Id, i, key, remaining, category));
            }
            else
            {
                chest.UsedSlots--;
            }

            stack.Count = remaining;

            for (var m = 0; m < moveTargets.Count; m++)
            {
                moves.Add(new PlannedMove(chest.Container.Id, i, moveTargets[m].Container.Id, key, moveCounts[m], moveTargets[m].CategoryName!));
            }
        }

        /// <summary>
        /// Tells whether a chest still holds one of its own stacks of an item. The game merges a new stack into it, and that stack may be
        /// due to leave with the amount the plan counted, so a stack of the same item must not land there.
        /// </summary>
        private static bool HoldsOwnStackOf(WorkingChest chest, ItemKey key)
        {
            var own = chest.Container.Contents.Count;
            for (var i = 0; i < own; i++)
            {
                if (chest.Stacks[i].Count > 0 && chest.Stacks[i].Key.Equals(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Uses up one of the category's share of the empty chests.</summary>
        private bool MayClaim(string category)
        {
            if (claimsLeft is null)
            {
                return true;
            }

            if (!claimsLeft.TryGetValue(category, out var left) || left == 0)
            {
                return false;
            }

            claimsLeft[category] = left - 1;
            return true;
        }

        /// <summary>Moves what it can of a stack to chests nearer its category than the one it is in: same-item stacks first, then free slots.</summary>
        private int Spill(WorkingChest chest, ItemKey key, string category, int remaining)
        {
            var here = CategoryNearness.Of(category, chest.CategoryName, catalog);
            var nearer = new List<WorkingChest>();
            foreach (var candidate in byId.Values)
            {
                if (!ReferenceEquals(candidate, chest)
                    && candidate.CategoryName is not null
                    && CategoryNearness.Of(category, candidate.CategoryName, catalog) < here
                    && !HoldsOwnStackOf(candidate, key))
                {
                    nearer.Add(candidate);
                }
            }

            nearer.Sort((a, b) =>
            {
                var byNearness = CategoryNearness.Of(category, a.CategoryName, catalog).CompareTo(CategoryNearness.Of(category, b.CategoryName, catalog));
                return byNearness != 0 ? byNearness : a.Index.CompareTo(b.Index);
            });

            for (var d = 0; d < nearer.Count && remaining > 0; d++)
            {
                remaining = MergeInto(nearer[d], key, remaining, moveTargets, moveCounts);
            }

            for (var d = 0; d < nearer.Count && remaining > 0; d++)
            {
                remaining = FillEmptySlots(nearer[d], key, remaining, moveTargets, moveCounts);
            }

            for (var m = 0; m < moveTargets.Count; m++)
            {
                if (!string.Equals(moveTargets[m].CategoryName, category, StringComparison.Ordinal))
                {
                    spillHosts.Add(moveTargets[m]);
                }
            }

            return remaining;
        }

        /// <summary>Returns the mutable destination list of a category, created on first use from the usable category chests (P12).</summary>
        private List<WorkingChest> DestinationsOf(string categoryName)
        {
            if (destinationsByCategory.TryGetValue(categoryName, out var cached))
            {
                return cached;
            }

            var chests = CategoryChests(auto, categoryName);
            var destinations = new List<WorkingChest>(chests.Count);
            for (var k = 0; k < chests.Count; k++)
            {
                destinations.Add(byId[chests[k].Id]);
            }

            destinationsByCategory[categoryName] = destinations;
            return destinations;
        }
    }
}
