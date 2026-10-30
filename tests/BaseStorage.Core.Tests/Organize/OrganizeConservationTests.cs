namespace BaseStorage.Core.Tests.Organize;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-563 to GW-U-566: replayable moves, conservation, layout invariants and exact huge-limit totals (AC-11).</summary>
public sealed class OrganizeConservationTests
{
    // GW-U-563
    [Theory]
    [InlineData("E")]
    [InlineData("Merge")]
    [InlineData("Overflow")]
    [InlineData("OverflowNo405")]
    [InlineData("Chained")]
    [InlineData("Solo702")]
    [InlineData("Pair702")]
    public void MovesReplayStepByStepWithinCapacity(string name)
    {
        PlanInput input = Named(name);
        OrganizePlan plan = Run(input);

        PlanReplay.ReplayResult replay = PlanReplay.Replay(input.Containers, plan);

        Assert.Equal(plan.Moves.Count, replay.Steps.Count);
        foreach (ContainerSnapshot container in input.Containers)
        {
            if (replay.Peak.TryGetValue(container.Id, out int peak))
            {
                Assert.True(peak <= container.SlotCount, name + ": chest " + container.Id.Id + " exceeded its slot count");
            }
        }
    }

    // GW-U-563
    [Fact]
    public void PeakSlotUseOfBaseEStaysWithinEachChestsSlots()
    {
        PlanInput input = E();
        PlanReplay.ReplayResult replay = PlanReplay.Replay(input.Containers, Run(input));

        Assert.InRange(replay.Peak[Id(201)], 0, 2);
        Assert.InRange(replay.Peak[Id(202)], 0, 4);
        Assert.InRange(replay.Peak[Id(208)], 0, 1);
        Assert.InRange(replay.Peak[Id(204)], 0, 2);
    }

    // GW-U-564
    [Fact]
    public void BaseETotalsAreConservedPerItemAndAcrossMovesAndNoRoom()
    {
        PlanInput input = E();
        OrganizePlan plan = Run(input);
        IEnumerable<ItemStackSnapshot> usable = input.Containers
            .Where(c => plan.FindLayout(c.Id) is not null)
            .SelectMany(c => c.Contents);

        var expectedTotals = new Dictionary<string, long>
        {
            ["Wood"] = 135,
            ["Stone"] = 95,
            ["Flint"] = 12,
            ["Resin"] = 12,
            ["CookedMeat"] = 5,
            ["TrophyDeer"] = 1,
        };
        AssertTotals(expectedTotals, PrefabTotals(usable));
        AssertTotals(expectedTotals, PrefabTotals(plan.Layouts.SelectMany(l => l.Stacks)));

        AssertTotals(
            new Dictionary<string, long> { ["Wood"] = 105, ["Stone"] = 10, ["Resin"] = 12 },
            MovedCounts(plan));
        AssertTotals(
            new Dictionary<string, long> { ["CookedMeat"] = 5, ["TrophyDeer"] = 1 },
            NoRoomCounts(plan));
        Assert.Equal(127L, plan.Moves.Sum(m => (long)m.Count));
        Assert.Equal(6L, plan.NoRoom.Sum(e => (long)e.Count));
    }

    // GW-U-564
    [Fact]
    public void MergeBaseTotalsAreConservedAndMisplacedSixteenIsFourteenPlusTwo()
    {
        PlanInput input = Merge();
        OrganizePlan plan = Run(input);
        IEnumerable<ItemStackSnapshot> usable = input.Containers
            .Where(c => plan.FindLayout(c.Id) is not null)
            .SelectMany(c => c.Contents);

        var expectedTotals = new Dictionary<string, long> { ["Stone"] = 107, ["Wood"] = 51, ["CopperOre"] = 1 };
        AssertTotals(expectedTotals, PrefabTotals(usable));
        AssertTotals(expectedTotals, PrefabTotals(plan.Layouts.SelectMany(l => l.Stacks)));

        Assert.Equal(14L, plan.Moves.Sum(m => (long)m.Count));
        Assert.Equal(2L, plan.NoRoom.Sum(e => (long)e.Count));
    }

    // GW-U-564
    [Theory]
    [InlineData("E")]
    [InlineData("Merge")]
    [InlineData("Overflow")]
    public void ReplayedEndStateMatchesTheLayoutTotalsPerChest(string name)
    {
        PlanInput input = Named(name);
        OrganizePlan plan = Run(input);

        PlanReplay.ReplayResult replay = PlanReplay.Replay(input.Containers, plan);

        foreach (ChestLayout layout in plan.Layouts)
        {
            if (layout.Role == ChestRole.Source)
            {
                Assert.Equal(Join(layout.Stacks), Join(replay.Final[layout.Id]));
            }
            else
            {
                AssertTotals(ItemTotals(layout.Stacks), ItemTotals(replay.Final[layout.Id]));
            }
        }
    }

    // GW-U-565
    [Fact]
    public void EveryLayoutRespectsTheSlotCountAndTheStackLimits()
    {
        foreach (string name in AllFixtureNames)
        {
            OrganizePlan plan = Run(Named(name));

            foreach (ChestLayout layout in plan.Layouts)
            {
                Assert.True(layout.Stacks.Count <= layout.Container.SlotCount, name + ": layout exceeds its slot count");
                foreach (ItemStackSnapshot stack in layout.Stacks)
                {
                    Assert.True(stack.Count >= 1 && stack.Count <= stack.MaxStack, name + ": stack outside 1..MaxStack");
                }

                if (layout.Role == ChestRole.ClaimedOverflow)
                {
                    Assert.NotEmpty(layout.Stacks);
                }
            }
        }
    }

    // GW-U-565
    [Fact]
    public void EveryMoveHasDistinctEndsAndItsCategoryAndRoleMatchTheDestinationInEveryFixture()
    {
        foreach (string name in AllFixtureNames)
        {
            OrganizePlan plan = Run(Named(name));

            foreach (PlannedMove move in plan.Moves)
            {
                Assert.NotEqual(move.Source, move.Destination);
                Assert.True(move.Count >= 1, name + ": move count must be at least 1");

                ChestLayout? destination = plan.FindLayout(move.Destination);
                Assert.NotNull(destination);
                Assert.Equal(destination!.CategoryName, move.CategoryName);
                Assert.True(
                    destination.Role == ChestRole.Category || destination.Role == ChestRole.ClaimedOverflow,
                    name + ": a move destination must be a category or claimed overflow chest");
            }
        }
    }

    // GW-U-566
    [Fact]
    public void HugeStackLimitsGiveTheExactLayoutAndConserveTheLongTotal()
    {
        PlanInput input = Huge();
        OrganizePlan plan = Run(input);

        Assert.Equal(new[] { Move(1102, 0, 1101, "Wood", 1, int.MaxValue, 1, "Wood") }, MovesOf(plan));
        Assert.Equal(
            Layout(1101, ChestRole.Category, "Wood", S("Wood", int.MaxValue, int.MaxValue), S("Wood", 1852516354, int.MaxValue)),
            LayoutRowOf(plan.FindLayout(Id(1101))!));
        Assert.Equal(4000000001L, plan.Layouts.Sum(l => l.Stacks.Sum(s => (long)s.Count)));
    }

    /// <summary>The count of moved items per prefab name.</summary>
    private static Dictionary<string, long> MovedCounts(OrganizePlan plan)
    {
        var counts = new Dictionary<string, long>();
        foreach (PlannedMove move in plan.Moves)
        {
            counts[move.PrefabName] = counts.GetValueOrDefault(move.PrefabName) + move.Count;
        }

        return counts;
    }

    /// <summary>The count of no-room items per prefab name.</summary>
    private static Dictionary<string, long> NoRoomCounts(OrganizePlan plan)
    {
        var counts = new Dictionary<string, long>();
        foreach (NoRoomEntry entry in plan.NoRoom)
        {
            counts[entry.PrefabName] = counts.GetValueOrDefault(entry.PrefabName) + entry.Count;
        }

        return counts;
    }
}
