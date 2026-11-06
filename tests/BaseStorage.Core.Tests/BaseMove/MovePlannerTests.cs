namespace BaseStorage.Core.Tests.BaseMove;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>Base-move spec sections 2 and 3: the portal rule, and the planner over plain snapshots.</summary>
public sealed class MovePlannerTests
{
    private static readonly ItemPortalTraits Free = new ItemPortalTraits(0, true);
    private static readonly ItemPortalTraits Ore = new ItemPortalTraits(0, false);
    private static readonly ItemPortalTraits Sealed = new ItemPortalTraits(1000, true);

    private static CategoryAssignment Wood => CategoryAssignment.ForCategory("Wood");

    private static CategoryAssignment Metals => CategoryAssignment.ForCategory("Metals & Ores");

    [Theory]
    [InlineData(0, true, false, true)]
    [InlineData(0, false, false, false)]
    [InlineData(0, false, true, true)]
    [InlineData(999, false, true, true)]
    [InlineData(1000, true, false, false)]
    [InlineData(1000, true, true, false)]
    [InlineData(5000, false, true, false)]
    public void PortalRuleMirrorsTheGame(int tier, bool teleportable, bool teleportAll, bool expected) =>
        Assert.Equal(expected, MovePortalRule.CanMove(new ItemPortalTraits(tier, teleportable), teleportAll));

    [Fact]
    public void OreStaysAndIsReportedWhenThePortalRuleIsOn()
    {
        var plan = Plan(
            new[] { C(1, 0, 0, 4, S("Wood", 10), S("CopperOre", 20)) },
            new[] { C(2, 900, 900, 4), C(3, 900, 901, 4) },
            assignments: Assign((2, Wood), (3, Metals)));

        var move = Assert.Single(plan.Moves);
        Assert.Equal("Wood", move.PrefabName);
        Assert.Equal(10, move.Count);
        Assert.Equal(Id(2), move.Destination);
        var stayed = Assert.Single(plan.Stayed);
        Assert.Equal("CopperOre", stayed.Stack.PrefabName);
        Assert.Equal(1, stayed.StackIndex);
        Assert.Equal(new[] { "CopperOre" }, plan.StayedPrefabs());
        Assert.Equal(1, plan.MovedStacks);
    }

    [Fact]
    public void OreMovesWhenTeleportAllIsOn()
    {
        var plan = Plan(
            new[] { C(1, 0, 0, 4, S("Wood", 10), S("CopperOre", 20)) },
            new[] { C(2, 900, 900, 4), C(3, 900, 901, 4) },
            teleportAll: true,
            assignments: Assign((2, Wood), (3, Metals)));

        Assert.Equal(2, plan.Moves.Count);
        Assert.Empty(plan.Stayed);
    }

    [Fact]
    public void AnItemTheGameNeverLetsThroughStaysEvenWithTeleportAll()
    {
        var plan = Plan(
            new[] { C(1, 0, 0, 4, S("Sealed", 1, maxStack: 1)) },
            new[] { C(2, 900, 900, 4) },
            teleportAll: true);

        Assert.Empty(plan.Moves);
        Assert.Equal("Sealed", Assert.Single(plan.Stayed).Stack.PrefabName);
    }

    [Fact]
    public void ASourceChestsOwnCategoryDoesNotKeepItsItems()
    {
        var plan = Plan(
            new[] { C(1, 0, 0, 4, S("Wood", 10)) },
            new[] { C(2, 900, 900, 4) },
            assignments: Assign((1, Wood), (2, Wood)));

        Assert.Equal(10, Assert.Single(plan.Moves).Count);
    }

    [Fact]
    public void WhatDoesNotFitStaysAndIsReported()
    {
        var plan = Plan(
            new[] { C(1, 0, 0, 4, S("Wood", 50), S("Wood", 50), S("Wood", 30)) },
            new[] { C(2, 900, 900, 2) },
            assignments: Assign((2, Wood)));

        Assert.Equal(100, plan.Moves.Sum(m => m.Count));
        var left = Assert.Single(plan.NoRoom);
        Assert.Equal(30, left.Count);
        Assert.Equal(2, left.SourceStackIndex);
    }

    [Fact]
    public void KeepChestsOnEitherSideAreLeftAlone()
    {
        var plan = Plan(
            new[] { C(1, 0, 0, 4, S("Wood", 10)), C(4, 0, 1, 4, S("Stone", 5)) },
            new[] { C(2, 900, 900, 4, S("Wood", 1)), C(3, 900, 901, 4) },
            assignments: Assign((1, CategoryAssignment.Keep), (2, CategoryAssignment.Keep), (3, Wood)));

        Assert.Equal(Id(1), Assert.Single(plan.SkippedSources).Id);
        Assert.Equal(Id(2), Assert.Single(plan.SkippedDestinations).Id);
        Assert.DoesNotContain(plan.Moves, m => m.Source == Id(1) || m.Destination == Id(2));
    }

    [Fact]
    public void AChestAnotherPlayerHasOpenIsLeftAlone()
    {
        var plan = Plan(
            new[] { C(1, 0, 0, 4, S("Wood", 10)) },
            new[] { C(2, 900, 900, 4) },
            inUse: InUse((1, "Erik")));

        Assert.Empty(plan.Moves);
        Assert.Equal("Erik", Assert.Single(plan.SkippedSources).PlayerName);
    }

    [Fact]
    public void AccessDeniedChestsAreLeftAlone()
    {
        var plan = Plan(
            new[] { CF(1, 0, 0, 4, ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied, S("Wood", 10)) },
            new[] { C(2, 900, 900, 4) });

        Assert.Empty(plan.Moves);
        Assert.Equal(SkipReason.Ineligible, Assert.Single(plan.SkippedSources).Kind);
    }

    [Fact]
    public void AChestHoldingAnUnknownItemNeitherGivesNorReceives()
    {
        var plan = Plan(
            new[] { C(1, 0, 0, 4, S("Wood", 10), S("RemovedModItem", 1)) },
            new[] { C(2, 900, 900, 4, S("RemovedModItem", 1)), C(3, 900, 901, 4) },
            assignments: Assign((2, Wood), (3, Wood)));

        Assert.Empty(plan.Moves);
        var source = Assert.Single(plan.SkippedSources);
        Assert.Equal(MovePlanner.ReasonUnknownItem, source.Reason);
        Assert.Equal(SkipReason.InvalidContents, source.Kind);
        Assert.Equal(Id(2), Assert.Single(plan.SkippedDestinations).Id);
    }

    [Fact]
    public void AChestOnBothSidesIsRefused()
    {
        ContainerSnapshot[] a = { C(1, 0, 0, 4) };
        Assert.True(MovePlanner.Overlaps(a, new[] { C(1, 0, 0, 4) }));
        Assert.False(MovePlanner.Overlaps(a, new[] { C(2, 0, 0, 4) }));
        Assert.Throws<ArgumentException>(() => Plan(a, new[] { C(1, 0, 0, 4) }));
    }

    [Fact]
    public void TheFingerprintRepeatsAndNoticesAChange()
    {
        ContainerSnapshot[] destination = { C(2, 900, 900, 4) };
        var first = Plan(new[] { C(1, 0, 0, 4, S("Wood", 10)) }, destination, assignments: Assign((2, Wood)));
        var same = Plan(new[] { C(1, 0, 0, 4, S("Wood", 10)) }, destination, assignments: Assign((2, Wood)));
        var changedCount = Plan(new[] { C(1, 0, 0, 4, S("Wood", 11)) }, destination, assignments: Assign((2, Wood)));
        var changedChest = Plan(new[] { C(7, 0, 0, 4, S("Wood", 10)) }, destination, assignments: Assign((2, Wood)));

        Assert.Equal(first.Fingerprint, same.Fingerprint);
        Assert.NotEqual(first.Fingerprint, changedCount.Fingerprint);
        Assert.NotEqual(first.Fingerprint, changedChest.Fingerprint);
    }

    [Fact]
    public void MovedStacksCountsPerChestNotPerIndex()
    {
        var plan = Plan(
            new[] { C(1, 0, 0, 4, S("Wood", 10)), C(4, 0, 1, 4, S("Wood", 10)) },
            new[] { C(2, 900, 900, 4) },
            assignments: Assign((2, Wood)));

        Assert.Equal(2, plan.MovedStacks);
    }

    [Fact]
    public void ConservationCatchesALostOrChangedItem()
    {
        ContainerSnapshot[] before = { C(1, 0, 0, 4, S("Wood", 10)), C(2, 9, 9, 4, S("Wood", 5)) };
        Assert.True(MoveConservation.Holds(before, new[] { C(1, 0, 0, 4), C(2, 9, 9, 4, S("Wood", 15)) }));
        Assert.False(MoveConservation.Holds(before, new[] { C(1, 0, 0, 4), C(2, 9, 9, 4, S("Wood", 14)) }));
        Assert.False(MoveConservation.Holds(before, new[] { C(1, 0, 0, 4), C(2, 9, 9, 4, S("Wood", 16)) }));
        Assert.False(MoveConservation.Holds(before, new[] { C(1, 0, 0, 4), C(2, 9, 9, 4, S("Wood", 15, quality: 2)) }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RandomMovesKeepEveryItemAndRespectTheChests(bool teleportAll)
    {
        string[] prefabs = { "Wood", "Stone", "CopperOre", "Copper", "CookedMeat", "SwordIron", "Sealed" };
        var traits = Traits();
        var totalMoved = 0L;
        var totalStayed = 0L;
        for (var seed = 0; seed < 400; seed++)
        {
            var random = new Random(seed);
            var sources = RandomChests(random, 100, prefabs);
            var destinations = RandomChests(random, 200, prefabs);
            var assignments = new Dictionary<ContainerId, CategoryAssignment>();
            foreach (var chest in sources.Concat(destinations))
            {
                var roll = random.Next(4);
                if (roll == 0)
                {
                    assignments[chest.Id] = CategoryAssignment.Keep;
                }
                else if (roll == 1)
                {
                    assignments[chest.Id] = random.Next(2) == 0 ? Wood : Metals;
                }
            }

            var plan = Plan(sources, destinations, teleportAll, assignments);

            var skippedSources = new HashSet<ContainerId>(plan.SkippedSources.Select(s => s.Id));
            var skippedDestinations = new HashSet<ContainerId>(plan.SkippedDestinations.Select(s => s.Id));
            foreach (var source in sources)
            {
                for (var i = 0; i < source.Contents.Count; i++)
                {
                    var stack = source.Contents[i];
                    var moved = plan.Moves.Where(m => m.Source == source.Id && m.SourceStackIndex == i).Sum(m => m.Count);
                    var noRoom = plan.NoRoom.Where(n => n.Source == source.Id && n.SourceStackIndex == i).Sum(n => n.Count);
                    var stayed = plan.Stayed.Any(s => s.Source == source.Id && s.StackIndex == i);
                    if (skippedSources.Contains(source.Id))
                    {
                        Assert.Equal(0, moved + noRoom);
                        Assert.False(stayed);
                    }
                    else if (MovePortalRule.CanMove(traits[stack.PrefabName], teleportAll))
                    {
                        Assert.Equal(stack.Count, moved + noRoom);
                        Assert.False(stayed);
                    }
                    else
                    {
                        Assert.Equal(0, moved + noRoom);
                        Assert.True(stayed);
                    }

                    totalMoved += moved;
                    totalStayed += stayed ? stack.Count : 0;
                }
            }

            foreach (var destination in destinations)
            {
                var received = plan.Moves.Where(m => m.Destination == destination.Id).ToArray();
                if (skippedDestinations.Contains(destination.Id))
                {
                    Assert.Empty(received);
                }

                var slots = destination.Contents.Count;
                foreach (var group in received.GroupBy(m => new { m.PrefabName, m.Quality, m.MaxStack }))
                {
                    var room = destination.Contents
                        .Where(s => s.PrefabName == group.Key.PrefabName && s.Quality == group.Key.Quality && s.MaxStack == group.Key.MaxStack)
                        .Sum(s => s.MaxStack - s.Count);
                    var over = group.Sum(m => m.Count) - room;
                    if (over > 0)
                    {
                        slots += (over + group.Key.MaxStack - 1) / group.Key.MaxStack;
                    }
                }

                Assert.True(slots <= destination.SlotCount, "chest " + destination.Id.Id + " over capacity at seed " + seed);
            }

            Assert.All(plan.Moves, m => Assert.DoesNotContain(sources, s => s.Id == m.Destination));
        }

        Assert.True(totalMoved > 0);
        if (!teleportAll)
        {
            Assert.True(totalStayed > 0);
        }
    }

    private static Dictionary<string, ItemPortalTraits> Traits() => new(StringComparer.Ordinal)
    {
        ["Wood"] = Free,
        ["Stone"] = Free,
        ["CopperOre"] = Ore,
        ["Copper"] = Ore,
        ["CookedMeat"] = Free,
        ["SwordIron"] = Free,
        ["Sealed"] = Sealed,
    };

    private static MovePlan Plan(
        ContainerSnapshot[] sources,
        ContainerSnapshot[] destinations,
        bool teleportAll = false,
        Dictionary<ContainerId, CategoryAssignment>? assignments = null,
        Dictionary<ContainerId, string>? inUse = null,
        Dictionary<string, ItemPortalTraits>? traits = null) =>
        MovePlanner.Plan(
            sources,
            destinations,
            assignments ?? Assign(),
            inUse ?? InUse(),
            Catalog(),
            Kinds(),
            traits ?? Traits(),
            teleportAll,
            Options(),
            DefaultThreshold);

    private static ContainerSnapshot[] RandomChests(Random random, uint firstId, string[] prefabs)
    {
        var chests = new ContainerSnapshot[random.Next(1, 6)];
        for (var c = 0; c < chests.Length; c++)
        {
            var slots = random.Next(1, 9);
            var stacks = new List<ItemStackSnapshot>();
            var used = random.Next(0, slots + 1);
            for (var s = 0; s < used; s++)
            {
                var prefab = prefabs[random.Next(prefabs.Length)];
                var maxStack = prefab is "SwordIron" or "Sealed" ? 1 : 50;
                stacks.Add(S(prefab, random.Next(1, maxStack + 1), maxStack));
            }

            chests[c] = C(firstId + (uint)c, c, firstId, slots, stacks.ToArray());
        }

        return chests;
    }
}
