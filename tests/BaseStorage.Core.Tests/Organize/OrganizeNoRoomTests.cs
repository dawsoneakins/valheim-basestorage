namespace BaseStorage.Core.Tests.Organize;

using System.Linq;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-551 and GW-U-552: no-room entries are exact and stay in their source chest (AC-5).</summary>
public sealed class OrganizeNoRoomTests
{
    // GW-U-551
    [Fact]
    public void BaseENoRoomIsExactInProcessingOrder()
    {
        OrganizePlan plan = Run(E());

        Assert.Equal(
            new[]
            {
                NoRoom(209, 3, "CookedMeat", 1, 50, 5, "Cooked Food"),
                NoRoom(209, 4, "TrophyDeer", 1, 20, 1, "Trophies"),
            },
            NoRoomOf(plan));
    }

    // GW-U-551
    [Fact]
    public void MergeBaseNoRoomIsTheRemainderOfWoodAndTheCopperOre()
    {
        OrganizePlan plan = Run(Merge());

        Assert.Equal(
            new[]
            {
                NoRoom(303, 1, "Wood", 1, 50, 1, "Wood"),
                NoRoom(303, 2, "CopperOre", 1, 50, 1, "Metals & Ores"),
            },
            NoRoomOf(plan));
    }

    // GW-U-551
    [Fact]
    public void SoloChest702HasThreeNoRoomEntriesAndNoMoves()
    {
        OrganizePlan plan = Run(Solo702());

        Assert.Equal(
            new[]
            {
                NoRoom(702, 0, "SwordIron", 1, 1, 1, "Gear"),
                NoRoom(702, 1, "Resin", 1, 50, 3, "Misc"),
                NoRoom(702, 2, "Wood", 1, 50, 2, "Wood"),
            },
            NoRoomOf(plan));
        Assert.Empty(plan.Moves);
    }

    // GW-U-551
    [Fact]
    public void NoRoomEntriesHaveUniqueSourceStacksAndPositiveCountsInEveryFixture()
    {
        foreach (string name in AllFixtureNames)
        {
            OrganizePlan plan = Run(Named(name));

            int distinct = plan.NoRoom.Select(e => (e.Source.Id, e.SourceStackIndex)).Distinct().Count();
            Assert.Equal(plan.NoRoom.Count, distinct);
            Assert.All(plan.NoRoom, e => Assert.True(e.Count >= 1, name + ": no-room count must be at least 1"));
        }
    }

    // GW-U-552
    [Fact]
    public void BaseENoRoomItemsStayInChest209InInputOrder()
    {
        OrganizePlan plan = Run(E());

        Assert.Equal(
            Layout(209, ChestRole.Source, null, S("CookedMeat", 5), S("TrophyDeer", 1, 20)),
            LayoutRowOf(plan.FindLayout(Id(209))!));
    }

    // GW-U-552
    [Fact]
    public void MergeBaseRemainderStaysInChest303AsAStackWithItsRemainingCount()
    {
        OrganizePlan plan = Run(Merge());

        Assert.Equal(
            Layout(303, ChestRole.Source, null, S("Wood", 1), S("CopperOre", 1)),
            LayoutRowOf(plan.FindLayout(Id(303))!));
    }

    // GW-U-552 (derived)
    [Fact]
    public void MisplacedStoneInAWoodChestWithNoRoomStaysAndTheChestIsCompacted()
    {
        PlanInput input = Make(
            new[] { C(1701, 0, 0, 2, S("Wood", 10), S("Stone", 5)) },
            Assign((1701, CategoryAssignment.ForCategory("Wood"))));
        OrganizePlan plan = Run(input);

        Assert.Empty(plan.Moves);
        Assert.Equal(new[] { NoRoom(1701, 1, "Stone", 1, 50, 5, "Stone") }, NoRoomOf(plan));
        Assert.Equal(
            Layout(1701, ChestRole.Category, "Wood", S("Stone", 5), S("Wood", 10)),
            LayoutRowOf(plan.FindLayout(Id(1701))!));
    }
}
