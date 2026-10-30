namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-10 (spec NFR performance): a worst-case base gives the contracted counts. This is a correctness check, not a timing check.</summary>
public sealed class AutoAssignLargeBaseTests
{
    // GW-U-536
    [Fact]
    public void WorstCaseBaseGivesTheContractCountsAndDistinctClaimsThenNull()
    {
        ContainerSnapshot[] before = LargeBase();
        ContainerSnapshot[] containers = LargeBase();

        AutoAssignResult r = ResolveWith(containers, Persisted());

        Assert.Equal(10, r.Chests.Count(c => c.Outcome == AutoAssignOutcome.Assigned && c.CategoryName == "Metals & Ores"));
        Assert.Equal(10, r.Chests.Count(c => c.Outcome == AutoAssignOutcome.SourceOnly));
        // The 20 empty unassigned chests are 3001..3020 in snapshot order (derived from the layout of LargeBase).
        Assert.Equal(IdRange(3001, 20), ContainerIds(r.OverflowChests));

        OverflowPool pool = r.CreateOverflowPool();
        ContainerSnapshot?[] claims = new ContainerSnapshot?[21];
        for (int i = 0; i < claims.Length; i++)
        {
            claims[i] = pool.Claim("Metals & Ores");
        }

        for (int i = 0; i < 20; i++)
        {
            Assert.NotNull(claims[i]);
        }

        Assert.Equal(20, claims.Take(20).Select(claim => claim!.Id.Id).Distinct().Count());
        Assert.Null(claims[20]);

        Assert.Equal(before.Length, containers.Length);
        for (int i = 0; i < before.Length; i++)
        {
            Assert.Equal(before[i], containers[i]);
        }
    }

    /// <summary>
    /// The worst-case base, in snapshot order: 20 persisted chests (1001..1020, Wood, Stone, Keep in turn), 20 unassigned 8 x 4 chests
    /// (2001..2020; the first 10 hold 17 CopperOre and 15 Wood stacks, the rest 8 stacks each of four categories), and 20 empty
    /// unassigned chests (3001..3020). A fresh array per call.
    /// </summary>
    private static ContainerSnapshot[] LargeBase()
    {
        var chests = new List<ContainerSnapshot>();

        for (int i = 0; i < 20; i++)
        {
            chests.Add(C((uint)(1001 + i), i, 0));
        }

        for (int i = 0; i < 20; i++)
        {
            ItemStackSnapshot[] stacks = i < 10
                ? Mix(("CopperOre", 17), ("Wood", 15))
                : Mix(("CopperOre", 8), ("Wood", 8), ("Stone", 8), ("CookedMeat", 8));
            chests.Add(Big((uint)(2001 + i), 100 + i, 0, stacks));
        }

        for (int i = 0; i < 20; i++)
        {
            chests.Add(C((uint)(3001 + i), 200 + i, 0));
        }

        return chests.ToArray();
    }

    /// <summary>The persisted assignments of the first 20 chests of <see cref="LargeBase"/>: Wood, Stone, Keep in turn.</summary>
    private static Dictionary<ContainerId, CategoryAssignment> Persisted()
    {
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        for (int i = 0; i < 20; i++)
        {
            CategoryAssignment value = (i % 3) switch
            {
                0 => CategoryAssignment.ForCategory("Wood"),
                1 => CategoryAssignment.ForCategory("Stone"),
                _ => CategoryAssignment.Keep,
            };
            assignments[Id((uint)(1001 + i))] = value;
        }

        return assignments;
    }

    /// <summary>Stacks of one item each, repeated by count, for each (prefab, count) part in order.</summary>
    private static ItemStackSnapshot[] Mix(params (string Prefab, int Count)[] parts) =>
        parts.SelectMany(part => Enumerable.Repeat(S(part.Prefab, 1), part.Count)).ToArray();

    /// <summary>The consecutive serial numbers from a start.</summary>
    private static uint[] IdRange(uint start, int count) => Enumerable.Range(0, count).Select(i => start + (uint)i).ToArray();
}
