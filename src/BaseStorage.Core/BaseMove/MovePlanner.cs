namespace BaseStorage.Core.BaseMove;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Plans a move of one base's items into another base's chests (base-move spec section 3). Every source chest is a source-only chest
/// and the destination is filled by the Organize planner's own placement, as in Put away inventory. A stack the portal rule blocks stays
/// where it is. A chest holding an item with no known traits gives up and receives nothing, because a chest that cannot be fully
/// understood must not be rewritten. Pure: it reads its arguments, does no I/O and never applies the plan.
/// </summary>
public static class MovePlanner
{
    /// <summary>The skip reason of a chest that holds an item the game does not know.</summary>
    public const string ReasonUnknownItem = "holds an unknown item";

    /// <summary>Tells whether a chest appears on both sides, which a move must refuse.</summary>
    /// <param name="sources">The source base's chests. Not null.</param>
    /// <param name="destinations">The destination base's chests. Not null.</param>
    /// <returns>True when any chest id appears in both lists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a list is null.</exception>
    public static bool Overlaps(IReadOnlyList<ContainerSnapshot> sources, IReadOnlyList<ContainerSnapshot> destinations)
    {
        if (sources is null)
        {
            throw new ArgumentNullException(nameof(sources));
        }

        if (destinations is null)
        {
            throw new ArgumentNullException(nameof(destinations));
        }

        var ids = new HashSet<ContainerId>();
        for (var i = 0; i < sources.Count; i++)
        {
            ids.Add(sources[i].Id);
        }

        for (var i = 0; i < destinations.Count; i++)
        {
            if (ids.Contains(destinations[i].Id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Plans one move. Pure.</summary>
    /// <param name="sources">The source base's chests, in snapshot order. Not null.</param>
    /// <param name="destinations">The destination base's chests, in snapshot order. Not null, and sharing no chest with <paramref name="sources"/>.</param>
    /// <param name="assignments">The persisted assignment per chest id, for both bases. A missing id means unassigned. Not null.</param>
    /// <param name="inUseBy">The player name per chest id for chests another player has open. Not null.</param>
    /// <param name="catalog">The current category catalog. Not null.</param>
    /// <param name="itemKinds">The item kind per prefab name. Not null.</param>
    /// <param name="traits">The portal fields per prefab name; an item with no entry is unknown. Not null.</param>
    /// <param name="teleportAll">True when the world has the <c>TeleportAll</c> key.</param>
    /// <param name="options">The eligibility options.</param>
    /// <param name="autoAssignThresholdPct">The threshold in percent, passed to the auto-assign resolver of the destination.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="traits"/> or a list the Organize planner validates is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a chest is on both sides, or for input the Organize planner refuses.</exception>
    public static MovePlan Plan(
        IReadOnlyList<ContainerSnapshot> sources,
        IReadOnlyList<ContainerSnapshot> destinations,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        IReadOnlyDictionary<ContainerId, string> inUseBy,
        CategoryCatalog catalog,
        IReadOnlyDictionary<string, ItemKind> itemKinds,
        IReadOnlyDictionary<string, ItemPortalTraits> traits,
        bool teleportAll,
        EligibilityOptions options,
        int autoAssignThresholdPct)
    {
        if (traits is null)
        {
            throw new ArgumentNullException(nameof(traits));
        }

        OrganizePlanner.Validate(sources, assignments, inUseBy, catalog, itemKinds);
        OrganizePlanner.Validate(destinations, assignments, inUseBy, catalog, itemKinds);
        if (Overlaps(sources, destinations))
        {
            throw new ArgumentException("A chest is in both the source and the destination.", nameof(destinations));
        }

        var skippedSources = new List<SkippedChest>();
        var usableSources = new List<ContainerSnapshot>(sources.Count);
        Classify(sources, assignments, inUseBy, options, traits, skippedSources, usableSources);

        var skippedDestinations = new List<SkippedChest>();
        var usableDestinations = new List<ContainerSnapshot>(destinations.Count);
        Classify(destinations, assignments, inUseBy, options, traits, skippedDestinations, usableDestinations);

        var auto = AutoAssigner.Resolve(usableDestinations, assignments, catalog, itemKinds, autoAssignThresholdPct);
        var pool = auto.CreateOverflowPool();
        var byId = new Dictionary<ContainerId, WorkingChest>(usableDestinations.Count);
        OrganizePlanner.CreateWorkingChests(auto, byId);

        var moves = new List<PlannedMove>();
        var noRoom = new List<NoRoomEntry>();
        var stayed = new List<StayedStack>();
        var placement = new OrganizePlanner.Placement(byId, auto, pool, catalog, itemKinds, moves, noRoom, new List<WorkingChest>());
        var workingSources = new WorkingChest[usableSources.Count];
        for (var c = 0; c < workingSources.Length; c++)
        {
            workingSources[c] = new WorkingChest(usableSources[c], -1, AutoAssignOutcome.SourceOnly, null);
            var contents = usableSources[c].Contents;
            for (var i = 0; i < contents.Count; i++)
            {
                if (MovePortalRule.CanMove(traits[contents[i].PrefabName], teleportAll))
                {
                    placement.Place(workingSources[c], i);
                }
                else
                {
                    stayed.Add(new StayedStack(usableSources[c].Id, i, contents[i]));
                }
            }
        }

        if (noRoom.Count > 0)
        {
            noRoom.Clear();
            placement.Spilling = true;
            for (var c = 0; c < workingSources.Length; c++)
            {
                var contents = usableSources[c].Contents;
                for (var i = 0; i < contents.Count; i++)
                {
                    if (workingSources[c].Stacks[i].Count > 0 && MovePortalRule.CanMove(traits[contents[i].PrefabName], teleportAll))
                    {
                        placement.Place(workingSources[c], i);
                    }
                }
            }
        }

        var rules = new List<ChestAutoAssignment>(auto.MixedAssigned);
        rules.AddRange(placement.SpillRules());
        return new MovePlan(moves.ToArray(), noRoom.ToArray(), stayed.ToArray(), skippedSources.ToArray(), skippedDestinations.ToArray(), rules.AsReadOnly());
    }

    private static void Classify(
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        IReadOnlyDictionary<ContainerId, string> inUseBy,
        EligibilityOptions options,
        IReadOnlyDictionary<string, ItemPortalTraits> traits,
        List<SkippedChest> skipped,
        List<ContainerSnapshot> usable)
    {
        var classified = new List<ContainerSnapshot>(containers.Count);
        OrganizePlanner.Classify(containers, assignments, inUseBy, options, skipped, classified);
        for (var c = 0; c < classified.Count; c++)
        {
            if (HoldsUnknownItem(classified[c], traits))
            {
                skipped.Add(new SkippedChest(classified[c], SkipReason.InvalidContents, ReasonUnknownItem, null, null));
            }
            else
            {
                usable.Add(classified[c]);
            }
        }
    }

    private static bool HoldsUnknownItem(ContainerSnapshot container, IReadOnlyDictionary<string, ItemPortalTraits> traits)
    {
        for (var i = 0; i < container.Contents.Count; i++)
        {
            if (!traits.ContainsKey(container.Contents[i].PrefabName))
            {
                return true;
            }
        }

        return false;
    }
}
