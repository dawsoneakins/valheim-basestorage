namespace BaseStorage.Core.BaseMove;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>What the requester is told about a plan: the counts for the preview and for the final line.</summary>
public sealed class MoveSummary
{
    /// <summary>Creates the summary.</summary>
    /// <param name="movedStacks">The source stacks that give up at least one item.</param>
    /// <param name="stayedStacks">The source stacks the portal rule keeps in place.</param>
    /// <param name="noRoomStacks">The source stacks with something the destination has no room for.</param>
    /// <param name="skippedChests">The chests on either side that take no part.</param>
    /// <param name="stayedPrefabs">The prefab names the portal rule keeps in place. Not null.</param>
    /// <param name="changedChests">The chests the move writes, which each machine holds if it has them loaded; null means none.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stayedPrefabs"/> is null.</exception>
    public MoveSummary(int movedStacks, int stayedStacks, int noRoomStacks, int skippedChests, IReadOnlyList<string> stayedPrefabs, IReadOnlyList<ContainerId>? changedChests = null)
    {
        ChangedChests = changedChests ?? Array.Empty<ContainerId>();
        MovedStacks = movedStacks;
        StayedStacks = stayedStacks;
        NoRoomStacks = noRoomStacks;
        SkippedChests = skippedChests;
        StayedPrefabs = stayedPrefabs ?? throw new ArgumentNullException(nameof(stayedPrefabs));
    }

    /// <summary>Gets the source stacks that give up at least one item.</summary>
    public int MovedStacks { get; }

    /// <summary>Gets the source stacks the portal rule keeps in place.</summary>
    public int StayedStacks { get; }

    /// <summary>Gets the source stacks with something the destination has no room for.</summary>
    public int NoRoomStacks { get; }

    /// <summary>Gets the chests on either side that take no part.</summary>
    public int SkippedChests { get; }

    /// <summary>Gets the prefab names the portal rule keeps in place.</summary>
    public IReadOnlyList<string> StayedPrefabs { get; }

    /// <summary>Gets the chests the move writes.</summary>
    public IReadOnlyList<ContainerId> ChangedChests { get; }

    /// <summary>Summarises a plan.</summary>
    /// <param name="plan">The plan. Not null.</param>
    /// <param name="unreadableChests">Chests the reader could not decode, which never reached the planner.</param>
    /// <returns>The summary.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plan"/> is null.</exception>
    public static MoveSummary Of(MovePlan plan, int unreadableChests)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        return new MoveSummary(
            plan.MovedStacks,
            plan.Stayed.Count,
            plan.NoRoom.Count,
            plan.SkippedSources.Count + plan.SkippedDestinations.Count + unreadableChests,
            plan.StayedPrefabs(),
            plan.ChangedChests());
    }
}
