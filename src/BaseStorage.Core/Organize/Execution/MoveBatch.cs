namespace BaseStorage.Core.Organize.Execution;

using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// One batch of an execution (contract X4): a maximal run of consecutive planned moves with the same source chest, and the chests it
/// reserves: the source first, then each destination in first-use order, each id once.
/// </summary>
internal sealed class MoveBatch
{
    /// <summary>Creates a batch.</summary>
    /// <param name="source">The source chest of every move in the batch.</param>
    /// <param name="first">The index of the batch's first move in the plan's move list.</param>
    /// <param name="count">The number of moves in the batch, at least 1.</param>
    /// <param name="chests">The chests to reserve: the source first, then the destinations in first-use order.</param>
    internal MoveBatch(ContainerId source, int first, int count, List<ContainerId> chests)
    {
        Source = source;
        First = first;
        Count = count;
        Chests = chests;
    }

    /// <summary>Gets the source chest of every move in the batch.</summary>
    internal ContainerId Source { get; }

    /// <summary>Gets the index of the first move in the plan's move list.</summary>
    internal int First { get; }

    /// <summary>Gets the number of moves in the batch.</summary>
    internal int Count { get; }

    /// <summary>Gets the index one past the last move of the batch.</summary>
    internal int End => First + Count;

    /// <summary>Gets the chests the batch reserves, the source first, each id once.</summary>
    internal List<ContainerId> Chests { get; }

    /// <summary>Splits a plan's moves into batches, in plan order.</summary>
    /// <param name="plan">The plan.</param>
    /// <returns>The batches; empty when the plan has no moves.</returns>
    internal static List<MoveBatch> Split(OrganizePlan plan)
    {
        var batches = new List<MoveBatch>();
        var moves = plan.Moves;
        var i = 0;
        while (i < moves.Count)
        {
            var source = moves[i].Source;
            var chests = new List<ContainerId> { source };
            var first = i;
            while (i < moves.Count && moves[i].Source == source)
            {
                var destination = moves[i].Destination;
                if (!chests.Contains(destination))
                {
                    chests.Add(destination);
                }

                i++;
            }

            batches.Add(new MoveBatch(source, first, i - first, chests));
        }

        return batches;
    }
}
