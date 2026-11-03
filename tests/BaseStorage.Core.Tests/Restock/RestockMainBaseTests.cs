namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;
using static BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures;

/// <summary>S-QR-001 AC-3, AC-4, AC-5 (contract 7.1): the main base, plans a and b, with their exact moves, totals and conservation.</summary>
public sealed class RestockMainBaseTests
{
    private static readonly string[] UsableChests = { "c801", "c802", "c803", "c805" };

    // GW-U-2496 (plan a)
    [Fact]
    public void MainBaseTopsUpHotbarAndFavouriteStacks()
    {
        List<CarriedStack> k = MainCarried();
        RestockPlan a = RestockPlanner.Plan(k, Origin(), MainChests(), AssignR(), Craftable());

        Assert.Equal(
            new[] { Row(803, 0, 0, 40), Row(802, 2, 1, 12), Row(801, 1, 5, 20), Row(805, 0, 7, 5) },
            Shape(a));
        Assert.Equal(new[] { 50, 17, 10, 3, 1, 100, 20, 50, 10, 48 }, a.CarriedAfter.Select(s => s.Count).ToArray());
        for (int t = 0; t < k.Count; t++)
        {
            if (t is 2 or 3 or 4 or 6 or 8 or 9)
            {
                Assert.True(ReferenceEquals(a.CarriedAfter[t], k[t].Stack), "starred or untouched entry " + t + " is not the input instance");
            }
        }

        Assert.Equal(77, a.MovedItems);
        Assert.Equal(4, a.ToppedUpStacks);
    }

    // GW-U-2508 (plan b, KeepChestsCraftable off)
    [Fact]
    public void MainBaseWithoutKeepUsesTheNextSources()
    {
        RestockPlan a = RestockPlanner.Plan(MainCarried(), Origin(), MainChests(), AssignR(), Craftable());
        RestockPlan b = RestockPlanner.Plan(MainCarried(), Origin(), MainChests(), AssignR(), NotCraftable());

        Assert.Equal(
            new[]
            {
                Row(802, 1, 0, 12), Row(802, 0, 0, 8), Row(801, 0, 0, 20),
                Row(802, 2, 1, 12), Row(801, 1, 5, 20), Row(805, 0, 7, 5),
            },
            Shape(b));
        for (int t = 0; t < a.CarriedAfter.Count; t++)
        {
            Assert.True(a.CarriedAfter[t] == b.CarriedAfter[t], "CarriedAfter[" + t + "] differs between plan a and plan b");
        }

        Assert.Equal(77, b.MovedItems);
        Assert.Equal(4, b.ToppedUpStacks);
        Assert.Equal(75, b.RemainingRoom);
        Assert.Equal(30, b.LeftInUse);
        Assert.Equal(20, b.LeftDifferentIdentity);
        Assert.Equal(50, b.LeftBehind);
    }

    // GW-U-2514 (Q9, conservation on the main base)
    [Fact]
    public void Q09_MainBasePlanConservesEveryStackKey()
    {
        List<CarriedStack> k = MainCarried();
        List<ContainerSnapshot> r = MainChests();
        RestockPlan a = RestockPlanner.Plan(k, Origin(), r, AssignR(), Craftable());

        var slots = new List<Slot>();
        for (int t = 0; t < k.Count; t++)
        {
            slots.Add(new Slot("t" + t, 0, k[t].Stack));
        }

        for (int c = 0; c < r.Count; c++)
        {
            for (int s = 0; s < r[c].Contents.Count; s++)
            {
                slots.Add(new Slot("c" + r[c].Id.Id, s, r[c].Contents[s]));
            }
        }

        foreach (RestockMove move in a.Moves)
        {
            Slot source = Find(slots, "c" + move.Source.Id, move.SourceStackIndex);
            Slot target = Find(slots, "t" + move.TargetIndex, 0);
            source.Count -= move.Count;
            target.Count += move.Count;
            Assert.True(source.Count >= 0, "A source working count fell below 0.");
            Assert.True(target.Count <= target.Stack.MaxStack, "A carried stack went over its MaxStack.");
        }

        var usable = slots.Where(s => s.Owner.StartsWith("t", StringComparison.Ordinal) || UsableChests.Contains(s.Owner)).ToList();
        AssertKeyTotal(usable, S("Wood", 1, 50), 120);
        AssertKeyTotal(usable, SI("Wood", 1, Wl2()), 65);
        AssertKeyTotal(usable, S("Resin", 1, 50), 65);
        AssertKeyTotal(usable, SI("Resin", 1, Wl2()), 20);
        AssertKeyTotal(usable, S("ArrowWood", 1, 100), 110);

        foreach (Slot probe in slots)
        {
            int before = slots.Where(s => RestockOracle.SameKey(s.Stack, probe.Stack)).Sum(s => s.Initial);
            int after = slots.Where(s => RestockOracle.SameKey(s.Stack, probe.Stack)).Sum(s => s.Count);
            Assert.True(before == after, "The total of " + probe.Stack.PrefabName + " changed from " + before + " to " + after + ".");
        }

        Assert.Equal(0, Find(slots, "c802", 2).Count);
    }

    // GW-U-2515 (plan a, left-behind totals)
    [Fact]
    public void MainBaseReportsWhatStayedBehind()
    {
        RestockPlan a = RestockPlanner.Plan(MainCarried(), Origin(), MainChests(), AssignR(), Craftable());

        Assert.Equal(75, a.RemainingRoom);
        Assert.Equal(30, a.LeftInUse);
        Assert.Equal(20, a.LeftDifferentIdentity);
        Assert.Equal(50, a.LeftBehind);
    }

    private static void AssertKeyTotal(List<Slot> usable, ItemStackSnapshot key, int expected)
    {
        int before = usable.Where(s => RestockOracle.SameKey(s.Stack, key)).Sum(s => s.Initial);
        int after = usable.Where(s => RestockOracle.SameKey(s.Stack, key)).Sum(s => s.Count);
        Assert.Equal(expected, before);
        Assert.Equal(expected, after);
    }

    private static Slot Find(List<Slot> slots, string owner, int index) =>
        slots.Single(s => s.Owner == owner && s.Index == index);

    /// <summary>One stack in the conservation model: a carried entry or a chest stack, with its working count.</summary>
    private sealed class Slot
    {
        internal Slot(string owner, int index, ItemStackSnapshot stack)
        {
            Owner = owner;
            Index = index;
            Stack = stack;
            Initial = stack.Count;
            Count = stack.Count;
        }

        internal string Owner { get; }

        internal int Index { get; }

        internal ItemStackSnapshot Stack { get; }

        internal int Initial { get; }

        internal int Count { get; set; }
    }
}
