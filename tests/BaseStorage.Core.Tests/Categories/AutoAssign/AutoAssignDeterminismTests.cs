namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-2 and AC-5 (A1, A6, A10, NFR deterministic): resolution is pure, thread-safe and independent of dictionary order.</summary>
public sealed class AutoAssignDeterminismTests
{
    private static readonly uint[] BaseIds = { 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111 };

    // GW-U-504
    [Fact]
    public async Task ConcurrentResolveCallsEachEqualTheSequentialResult()
    {
        ContainerSnapshot[] b = B();
        Dictionary<ContainerId, CategoryAssignment> assignments = Assignments();
        CategoryCatalog catalog = Catalog();
        Dictionary<string, ItemKind> kinds = Kinds();
        AutoAssignResult sequential = AutoAssigner.Resolve(b, assignments, catalog, kinds, 50);
        AssertTable("sequential", sequential, BaseTable50);

        Task<AutoAssignResult>[] tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => AutoAssigner.Resolve(b, assignments, catalog, kinds, 50)))
            .ToArray();
        AutoAssignResult[] results = await Task.WhenAll(tasks);

        foreach (AutoAssignResult result in results)
        {
            AssertSameResult(sequential, result);
        }
    }

    // GW-U-504
    [Fact]
    public async Task ConcurrentReadersOfOneResultAllSeeTheSameChestsAndClaims()
    {
        AutoAssignResult r = ResolveB();

        Task<(uint[] Wood, uint?[] Claims, int Found)>[] tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() =>
            {
                int found = 0;
                foreach (uint id in BaseIds)
                {
                    if (r.Find(Id(id)) is not null)
                    {
                        found++;
                    }
                }

                return (ContainerIds(r.GetCategoryChests("Wood")), ClaimIds(r.CreateOverflowPool(), "Wood", "Wood", "Wood", "Wood"), found);
            }))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        foreach (var (wood, claims, found) in results)
        {
            Assert.Equal(new uint[] { 101, 104, 107, 111 }, wood);
            Assert.Equal(new uint?[] { 105, 108, 109, null }, claims);
            Assert.Equal(11, found);
        }
    }

    // GW-U-507
    [Fact]
    public void ResolvingTwiceOnTheSameInputsGivesTheSameResult()
    {
        ContainerSnapshot[] b = B();
        Dictionary<ContainerId, CategoryAssignment> assignments = Assignments();
        Dictionary<string, ItemKind> kinds = Kinds();

        AutoAssignResult first = AutoAssigner.Resolve(b, assignments, Catalog(), kinds, 50);
        AutoAssignResult second = AutoAssigner.Resolve(b, assignments, Catalog(), kinds, 50);

        AssertSameResult(first, second);
        AssertTable("first call", first, BaseTable50);
    }

    // GW-U-507
    [Fact]
    public void AssignmentsAndKindsInReverseInsertionOrderGiveTheSameResult()
    {
        AutoAssignResult forward = ResolveWith(B(), Assignments(), Catalog(), Kinds());
        var reversedAssignments = new Dictionary<ContainerId, CategoryAssignment>
        {
            [Id(999)] = CategoryAssignment.ForCategory("Stone"),
            [Id(107)] = CategoryAssignment.ForCategory("Wood"),
            [Id(106)] = CategoryAssignment.Keep,
            [Id(101)] = CategoryAssignment.ForCategory("Wood"),
        };
        var reversedKinds = new Dictionary<string, ItemKind>(StringComparer.Ordinal)
        {
            ["Resin"] = ItemKind.Other,
            ["SerpentStew"] = ItemKind.Food,
            ["ArrowWood"] = ItemKind.Ammo,
            ["AxeBronze"] = ItemKind.Tool,
            ["SwordIron"] = ItemKind.Weapon,
        };

        AutoAssignResult reverse = ResolveWith(B(), reversedAssignments, Catalog(), reversedKinds);

        AssertSameResult(forward, reverse);
    }

    // GW-U-507
    [Fact]
    public void ADictionaryWhoseEnumerationIsReversedGivesTheSameResult()
    {
        AutoAssignResult forward = ResolveB();
        var wrappedAssignments = new ReversedDictionary<ContainerId, CategoryAssignment>(Assignments());
        var wrappedKinds = new ReversedDictionary<string, ItemKind>(Kinds());

        AutoAssignResult reversed = ResolveWith(B(), wrappedAssignments, Catalog(), wrappedKinds);

        AssertSameResult(forward, reversed);
    }
}
