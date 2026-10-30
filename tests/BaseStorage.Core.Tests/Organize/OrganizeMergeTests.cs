namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Linq;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-543 to GW-U-545: merge first, never above the max stack, and only the same item merges (AC-2).</summary>
public sealed class OrganizeMergeTests
{
    // GW-U-543
    [Fact]
    public void PartialStackInALaterChestBeatsAnEmptySlotInAnEarlierOne()
    {
        OrganizePlan plan = Run(Merge());

        Assert.Equal(
            new[]
            {
                Move(303, 0, 302, "Stone", 1, 50, 5, "Stone"),
                Move(303, 0, 301, "Stone", 1, 50, 7, "Stone"),
                Move(303, 1, 304, "Wood", 1, 50, 2, "Wood"),
            },
            MovesOf(plan));
        Assert.Equal(302u, plan.Moves[0].Destination.Id);
        Assert.Empty(plan.Skipped);
        Assert.Empty(plan.ClaimedOverflow);
    }

    // GW-U-544
    [Fact]
    public void MoveOneTopsUpTheStoneStackOfChest202BeforeItsFreeSlotIsUsed()
    {
        PlanInput input = E();
        OrganizePlan plan = Run(input);

        Assert.Equal(Move(201, 1, 202, "Stone", 1, 50, 10, "Stone"), MovesOf(plan)[0]);
        PlanReplay.ReplayResult replay = PlanReplay.Replay(input.Containers, plan);
        Assert.Equal(
            new[] { Describe("Stone", 30, 1, 50), Describe("Flint", 5, 1, 50), Describe("Stone", 25, 1, 50) },
            replay.Steps[0][Id(202)]);
    }

    // GW-U-544
    [Fact]
    public void NoWorkingStackIsEverAboveItsMaxStackInTheMergeBase()
    {
        PlanInput input = Merge();
        OrganizePlan plan = Run(input);

        PlanReplay.ReplayResult replay = PlanReplay.Replay(input.Containers, plan);

        Assert.Equal(plan.Moves.Count, replay.Steps.Count);
        Assert.Equal(
            new[] { Describe("Stone", 50, 1, 50), Describe("Stone", 7, 1, 50) },
            replay.Final[Id(301)].Select(stack => Describe(stack)));
    }

    // GW-U-545 (derived)
    [Theory]
    [InlineData("other-quality")]
    [InlineData("other-max")]
    [InlineData("same-item")]
    public void OnlyTheSameItemMergesIntoAnExistingStack(string name)
    {
        OrganizePlan plan = Run(ItemCase(name));

        (MoveRow Move, LayoutRow Layout) expected = name switch
        {
            "other-quality" => (
                Move(1602, 0, 1601, "Stone", 2, 50, 5, "Stone"),
                Layout(1601, ChestRole.Category, "Stone", S("Stone", 40, 50, 1), S("Stone", 5, 50, 2))),
            "other-max" => (
                Move(1602, 0, 1601, "Stone", 1, 30, 5, "Stone"),
                Layout(1601, ChestRole.Category, "Stone", S("Stone", 5, 30, 1), S("Stone", 40, 50, 1))),
            "same-item" => (
                Move(1602, 0, 1601, "Stone", 1, 50, 5, "Stone"),
                Layout(1601, ChestRole.Category, "Stone", S("Stone", 45, 50, 1))),
            _ => throw new ArgumentException("Unknown item case: " + name, nameof(name)),
        };

        Assert.Equal(new[] { expected.Move }, MovesOf(plan));
        Assert.Equal(expected.Layout, LayoutRowOf(plan.FindLayout(Id(1601))!));
    }

    /// <summary>A Stone chest holding Stone40 (quality 1, max 50), and a Wood-assigned source chest holding one incoming Stone stack.</summary>
    private static PlanInput ItemCase(string name)
    {
        ItemStackSnapshot incoming = name switch
        {
            "other-quality" => S("Stone", 5, 50, 2),
            "other-max" => S("Stone", 5, 30, 1),
            "same-item" => S("Stone", 5, 50, 1),
            _ => throw new ArgumentException("Unknown item case: " + name, nameof(name)),
        };

        return Make(
            new[]
            {
                C(1601, 0, 0, 2, S("Stone", 40)),
                C(1602, 2, 0, 2, incoming),
            },
            Assign((1601, CategoryAssignment.ForCategory("Stone")), (1602, CategoryAssignment.ForCategory("Wood"))));
    }
}
