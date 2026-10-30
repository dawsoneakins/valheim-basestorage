namespace BaseStorage.Core.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// A small mutable pool of empty chests that a full category claims from, nearest first. It is for single-threaded use by one
/// planner run and is not thread-safe. Its reference set comes from the result it was created from and never changes.
/// </summary>
public sealed class OverflowPool
{
    private readonly AutoAssignResult source;
    private readonly List<ContainerSnapshot> remaining;

    /// <summary>Creates a pool. Only <see cref="AutoAssignResult.CreateOverflowPool"/> creates pools.</summary>
    /// <param name="source">The result the pool was created from; it supplies the reference chests.</param>
    /// <param name="initial">The pool's chests in snapshot order.</param>
    internal OverflowPool(AutoAssignResult source, ContainerSnapshot[] initial)
    {
        this.source = source;
        remaining = new List<ContainerSnapshot>(initial);
    }

    /// <summary>Gets the number of chests still in the pool.</summary>
    public int Count => remaining.Count;

    /// <summary>Gets the remaining chests in snapshot order, as a new read-only copy.</summary>
    /// <returns>A new read-only list of the chests still in the pool.</returns>
    public IReadOnlyList<ContainerSnapshot> GetRemaining() => new List<ContainerSnapshot>(remaining).AsReadOnly();

    /// <summary>
    /// Removes and returns the remaining chest nearest to the category's existing chests. Equal distances go to the chest
    /// earliest in snapshot order. When the category has no chest, the earliest remaining chest is returned.
    /// </summary>
    /// <param name="categoryName">The category to claim for, matched ordinally; not null.</param>
    /// <returns>The claimed chest, or null when the pool is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="categoryName"/> is null. The pool is unchanged.</exception>
    public ContainerSnapshot? Claim(string categoryName)
    {
        if (categoryName is null)
        {
            throw new ArgumentNullException(nameof(categoryName));
        }

        if (remaining.Count == 0)
        {
            return null;
        }

        var reference = source.GetCategoryChests(categoryName);
        var best = 0;
        if (reference.Count > 0)
        {
            var bestScore = double.PositiveInfinity;
            for (var i = 0; i < remaining.Count; i++)
            {
                var score = Score(remaining[i], reference);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
        }

        var chosen = remaining[best];
        remaining.RemoveAt(best);
        return chosen;
    }

    private static double Score(ContainerSnapshot chest, IReadOnlyList<ContainerSnapshot> reference)
    {
        var min = double.PositiveInfinity;
        for (var i = 0; i < reference.Count; i++)
        {
            var r = reference[i];
            var dx = (double)chest.Position.X - (double)r.Position.X;
            var dy = (double)chest.Position.Y - (double)r.Position.Y;
            var dz = (double)chest.Position.Z - (double)r.Position.Z;
            var d2 = (dx * dx) + (dy * dy) + (dz * dz);
            if (d2 < min)
            {
                min = d2;
            }
        }

        return min;
    }
}
