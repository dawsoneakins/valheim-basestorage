namespace BaseStorage.Core.Tests.Organize;

using System.Linq;
using BaseStorage.Core.Organize;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-548 to GW-U-550: nearest first against a fixed reference set, one category per claimed chest, one claim at a time (AC-4).</summary>
public sealed class OrganizeClaimOrderTests
{
    // GW-U-548
    [Fact]
    public void OverflowClaimsTakeTheNearestChestAgainstAFixedReferenceSet()
    {
        OrganizePlan plan = Run(Overflow());

        Assert.Equal(new uint[] { 404, 403, 405 }, plan.ClaimedOverflow.Select(l => l.Id.Id));
        Assert.Equal(404u, plan.Moves[0].Destination.Id);
        Assert.Equal(403u, plan.Moves[1].Destination.Id);
    }

    // GW-U-548
    [Fact]
    public void WoodClaimsChest208EvenThoughItIsLaterAndMiscTakesTheEarliestRemainingChest()
    {
        OrganizePlan plan = Run(E());

        Assert.Equal(new uint[] { 208, 204 }, plan.ClaimedOverflow.Select(l => l.Id.Id));
    }

    // GW-U-549
    [Fact]
    public void ChestClaimedForMiscKeepsItsFreeSlotForMiscOnlyAndCookedMeatIsNoRoom()
    {
        OrganizePlan plan = Run(E());

        Assert.Contains(NoRoomOf(plan), e => e == NoRoom(209, 3, "CookedMeat", 1, 50, 5, "Cooked Food"));
        Assert.All(plan.Moves.Where(m => m.Destination == Id(204)), m => Assert.Equal("Misc", m.CategoryName));
    }

    // GW-U-549
    [Fact]
    public void WithoutChest405StoneAndFlintAreNoRoomAndClaimedWoodChestsStayWood()
    {
        OrganizePlan plan = Run(Overflow(includeChest405: false));

        Assert.Equal(
            new[]
            {
                NoRoom(402, 2, "Stone", 1, 50, 1, "Stone"),
                NoRoom(402, 3, "Flint", 1, 50, 1, "Stone"),
            },
            NoRoomOf(plan));
        Assert.All(
            plan.Moves.Where(m => m.Destination == Id(403) || m.Destination == Id(404)),
            m => Assert.Equal("Wood", m.CategoryName));
    }

    // GW-U-549
    [Fact]
    public void EveryClaimedChestHasOneCategoryInEveryFixture()
    {
        foreach (string name in AllFixtureNames)
        {
            OrganizePlan plan = Run(Named(name));

            AssertClaimedMovesMatchLayouts(plan);
            Assert.All(plan.ClaimedOverflow, layout => Assert.NotNull(layout.CategoryName));
        }
    }

    // GW-U-550
    [Fact]
    public void ANewClaimHappensOnlyWhenThePreviousClaimIsFull()
    {
        OrganizePlan plan = Run(Chained());

        Assert.Equal(
            new[]
            {
                Move(1402, 0, 1403, "Wood", 1, 50, 50, "Wood"),
                Move(1402, 1, 1403, "Wood", 1, 50, 50, "Wood"),
                Move(1402, 2, 1404, "Wood", 1, 50, 50, "Wood"),
            },
            MovesOf(plan));
        Assert.Equal(new uint[] { 1403, 1404 }, plan.ClaimedOverflow.Select(l => l.Id.Id));
    }

    // GW-U-550
    [Fact]
    public void WithoutTheSecondOverflowChestTheThirdStackIsNoRoomWithItsFullCount()
    {
        OrganizePlan plan = Run(Chained(includeChestB: false));

        Assert.Equal(
            new[]
            {
                Move(1402, 0, 1403, "Wood", 1, 50, 50, "Wood"),
                Move(1402, 1, 1403, "Wood", 1, 50, 50, "Wood"),
            },
            MovesOf(plan));
        Assert.Equal(new[] { NoRoom(1402, 2, "Wood", 1, 50, 50, "Wood") }, NoRoomOf(plan));
        Assert.Equal(new uint[] { 1403 }, plan.ClaimedOverflow.Select(l => l.Id.Id));
    }
}
