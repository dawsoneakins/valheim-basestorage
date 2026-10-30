namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using Xunit.Abstractions;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// S-A-002: the planner's retry passes on the stress bases of the 2026-10-30 playtest, 60 chests of 24 slots with about 40 percent
/// assigned. v0.1 left 499 stacks with no room at 70 percent full and 782 at near full.
/// </summary>
public sealed class OrganizeRetryPassTests
{
    private const int Categories = 14;
    private const int ItemsPerCategory = 8;
    private readonly ITestOutputHelper output;

    public OrganizeRetryPassTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void AStackWithNoRoomMovesOnceALaterStackFreesASlot()
    {
        var catalog = CatalogOf();
        var first = C(1, 0f, 0f, 1, S("Item0_0", 5));
        var second = C(2, 2f, 0f, 1, S("Item1_0", 5));
        var third = C(3, 4f, 0f, 2);
        var assignments = Assign(
            (1, CategoryAssignment.ForCategory("Cat2")),
            (2, CategoryAssignment.ForCategory("Cat0")),
            (3, CategoryAssignment.ForCategory("Cat1")));
        var chests = new[] { first, second, third };

        OrganizePlan plan = Plan(chests, assignments, catalog);

        PlanReplay.Replay(chests, plan);
        Assert.Empty(plan.NoRoom);
        Assert.Equal(2, plan.Moves.Count);
    }

    // A known limit: two full chests that each hold the other's items need a free slot somewhere, and there is none.
    [Fact]
    public void TwoFullChestsThatMustSwapReportNoRoom()
    {
        var catalog = CatalogOf();
        var chestA = C(1, 0f, 0f, 1, S("Item0_0", 5));
        var chestB = C(2, 2f, 0f, 1, S("Item1_0", 5));
        var assignments = Assign((1, CategoryAssignment.ForCategory("Cat1")), (2, CategoryAssignment.ForCategory("Cat0")));

        OrganizePlan plan = Plan(new[] { chestA, chestB }, assignments, catalog);

        Assert.Equal(2, plan.NoRoom.Count);
        Assert.Empty(plan.Moves);
    }

    [Theory]
    [InlineData(70, 200)]
    [InlineData(96, 100)]
    public void TheStressBaseEndsWithLittleLeftOver(int fillPercent, int limit)
    {
        var catalog = CatalogOf();
        var chests = StressBase(fillPercent, 42);
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        var random = new Random(7);
        foreach (ContainerSnapshot chest in chests)
        {
            if (random.Next(100) < 40)
            {
                assignments[chest.Id] = CategoryAssignment.ForCategory("Cat" + random.Next(Categories));
            }
        }

        OrganizePlan plan = Plan(chests, assignments, catalog);
        PlanReplay.Replay(chests, plan);

        output.WriteLine($"fill {fillPercent}%: moves {plan.Moves.Count}, no room {plan.NoRoom.Count}");
        Assert.True(plan.NoRoom.Count < limit, $"{plan.NoRoom.Count} stacks had no room at {fillPercent}%");
    }

    [Fact]
    public void RetriesDoNotChangeWhatAFullBaseCannotMove()
    {
        var catalog = CatalogOf();
        var chestA = C(1, 0f, 0f, 1, S("Item0_0", 5));
        var chestB = C(2, 2f, 0f, 1, S("Item1_0", 5));
        var chestC = C(3, 4f, 0f, 1, S("Item1_1", 5));
        var assignments = Assign(
            (1, CategoryAssignment.ForCategory("Cat1")),
            (2, CategoryAssignment.ForCategory("Cat1")),
            (3, CategoryAssignment.ForCategory("Cat1")));

        OrganizePlan plan = Plan(new[] { chestA, chestB, chestC }, assignments, catalog);

        Assert.Single(plan.NoRoom);
        Assert.Empty(plan.Moves);
    }

    private static OrganizePlan Plan(
        IReadOnlyList<ContainerSnapshot> chests,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        CategoryCatalog catalog) =>
        OrganizePlanner.Plan(chests, assignments, new Dictionary<ContainerId, string>(), catalog, new Dictionary<string, ItemKind>(), Options(), 50, true);

    private static CategoryCatalog CatalogOf()
    {
        var text = new StringBuilder();
        for (int c = 0; c < Categories; c++)
        {
            text.Append("Cat").Append(c).Append(": ");
            text.AppendLine(string.Join(", ", Enumerable.Range(0, ItemsPerCategory).Select(i => $"Item{c}_{i}")));
        }

        return CategoryFile.Parse(text.ToString()).Catalog;
    }

    private static ContainerSnapshot[] StressBase(int fillPercent, int seed)
    {
        var random = new Random(seed);
        var chests = new ContainerSnapshot[60];
        for (int c = 0; c < chests.Length; c++)
        {
            int count = Math.Min(24, (int)Math.Round(24 * (fillPercent / 100.0) * (0.85 + (random.NextDouble() * 0.3))));
            var stacks = new ItemStackSnapshot[count];
            for (int s = 0; s < count; s++)
            {
                stacks[s] = S($"Item{random.Next(Categories)}_{random.Next(ItemsPerCategory)}", 1 + random.Next(50));
            }

            chests[c] = C((uint)(c + 1), c * 2f, 0f, 24, stacks);
        }

        return chests;
    }
}
