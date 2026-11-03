namespace BaseStorage.Core.Tests.ChestSort;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.ChestSort;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Snapshots.Identities;

/// <summary>Sorting one chest (S-QS-001): merging, order, and that nothing is ever lost or changed.</summary>
public sealed class ChestSortPlannerTests
{
    private static ChestSortEntry Entry(string prefab, int count, int rank = 0, int quality = 1, int max = 50, ItemStackIdentity? identity = null) =>
        new ChestSortEntry(new ItemStackSnapshot(prefab, count, quality, max, identity ?? ItemStackIdentity.Empty), rank, prefab);

    private static ItemStackIdentity Worn(float durability) => new ItemStackIdentity(durability, 7L, "Erik", 0, 0, new Dictionary<string, string>());

    [Fact]
    public void IdenticalStacksMergeAndTheEmptiedStackIsRemoved()
    {
        ChestSortPlan plan = ChestSortPlanner.Plan(new[] { Entry("Wood", 30), Entry("Wood", 30), Entry("Wood", 5) });

        Assert.Equal(new[] { 50, 15 }, plan.Slots.Select(s => s.Count));
        Assert.Equal(new[] { 2 }, plan.Removed);
    }

    [Fact]
    public void CategoryComesBeforeNameAndNameBeforeQuality()
    {
        ChestSortPlan plan = ChestSortPlanner.Plan(new[]
        {
            Entry("Zebra", 1, rank: 0, quality: 1),
            Entry("Apple", 1, rank: 1),
            Entry("Zebra", 1, rank: 0, quality: 3),
        });

        Assert.Equal(new[] { 2, 0, 1 }, plan.Slots.Select(s => s.Source));
    }

    [Fact]
    public void StacksThatDifferInIdentityOrQualityStaySeparate()
    {
        ChestSortPlan plan = ChestSortPlanner.Plan(new[]
        {
            Entry("Sword", 1, max: 1, identity: Worn(10f)),
            Entry("Sword", 1, max: 1, identity: Worn(90f)),
            Entry("Mead", 5, quality: 1),
            Entry("Mead", 5, quality: 2),
        });

        Assert.Equal(4, plan.Slots.Count);
        Assert.Empty(plan.Removed);
    }

    [Fact]
    public void ACraftedStackDoesNotMergeWithAnUncraftedOne()
    {
        ChestSortPlan plan = ChestSortPlanner.Plan(new[] { Entry("Bolt", 5, identity: Worn(1f)), Entry("Bolt", 5) });

        Assert.Equal(2, plan.Slots.Count);
    }

    [Fact]
    public void AnOversizedStackKeepsItsSizeSoTheGroupStillFits()
    {
        ChestSortPlan plan = ChestSortPlanner.Plan(new[] { Entry("Wood", 120), Entry("Wood", 10) });

        Assert.Equal(new[] { 120, 10 }, plan.Slots.Select(s => s.Count));
    }

    [Fact]
    public void AnEmptyChestPlansNothing()
    {
        ChestSortPlan plan = ChestSortPlanner.Plan(Array.Empty<ChestSortEntry>());

        Assert.Empty(plan.Slots);
        Assert.Empty(plan.Removed);
    }

    [Fact]
    public void NullsAreRefused()
    {
        Assert.Throws<ArgumentNullException>(() => ChestSortPlanner.Plan(null!));
        Assert.Throws<ArgumentException>(() => ChestSortPlanner.Plan(new ChestSortEntry[] { null! }));
    }

    [Fact]
    public void ThousandsOfRandomChestsConserveEveryItemAndUseNoExtraSlots()
    {
        var random = new Random(4242);
        string[] items = { "Wood", "Stone", "Flint", "Resin", "Coal" };
        int stacked = 0;
        for (int run = 0; run < 3000; run++)
        {
            var entries = new List<ChestSortEntry>();
            int count = random.Next(0, 40);
            for (int i = 0; i < count; i++)
            {
                entries.Add(Entry(items[random.Next(items.Length)], random.Next(1, 51), rank: random.Next(3), quality: random.Next(1, 3)));
            }

            ChestSortPlan plan = ChestSortPlanner.Plan(entries);

            Assert.Equal(entries.Sum(e => e.Stack.Count), plan.Slots.Sum(s => s.Count));
            Assert.Equal(count, plan.Slots.Count + plan.Removed.Count);
            Assert.Equal(count, plan.Slots.Select(s => s.Source).Concat(plan.Removed).Distinct().Count());
            Assert.All(plan.Slots, s => Assert.InRange(s.Count, 1, 50));
            foreach (ChestSortSlot slot in plan.Slots)
            {
                Assert.True(slot.Count <= 50);
            }

            stacked += plan.Removed.Count;
        }

        Assert.True(stacked > 1000, "The random chests rarely merged, so the test proved little: " + stacked);
    }

    [Fact]
    public void SortingASortedChestChangesNothing()
    {
        ChestSortEntry[] entries = { Entry("Wood", 50, rank: 0), Entry("Wood", 20, rank: 0), Entry("Stone", 7, rank: 1) };

        ChestSortPlan plan = ChestSortPlanner.Plan(entries);

        Assert.Equal(new[] { 0, 1, 2 }, plan.Slots.Select(s => s.Source));
        Assert.Equal(new[] { 50, 20, 7 }, plan.Slots.Select(s => s.Count));
    }
}
