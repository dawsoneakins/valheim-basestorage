namespace BaseStorage.Core.Tests.Organize;

using BaseStorage.Core.Organize;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-546 and GW-U-547: a full category claims an overflow chest, one at a time (AC-3).</summary>
public sealed class OrganizeOverflowTests
{
    // GW-U-546
    [Fact]
    public void FullWoodCategoryClaimsChest208AfterTheStackFillsChest201()
    {
        OrganizePlan plan = Run(E());

        Assert.Equal(Move(209, 1, 201, "Wood", 1, 50, 10, "Wood"), MovesOf(plan)[3]);
        Assert.Equal(Move(209, 1, 208, "Wood", 1, 50, 35, "Wood"), MovesOf(plan)[4]);
        Assert.Equal(208u, plan.ClaimedOverflow[0].Id.Id);
        Assert.Equal(ChestRole.ClaimedOverflow, plan.ClaimedOverflow[0].Role);
        Assert.Equal("Wood", plan.ClaimedOverflow[0].CategoryName);
    }

    // GW-U-546
    [Fact]
    public void EmptyGearChestIsClaimedForTheSwordAndHoldsOnlyTheSword()
    {
        OrganizePlan plan = Run(Pair702());

        Assert.Equal(new[] { Move(702, 0, 703, "SwordIron", 1, 1, 1, "Gear") }, MovesOf(plan));
        Assert.Equal(
            Layout(703, ChestRole.ClaimedOverflow, "Gear", S("SwordIron", 1, 1)),
            LayoutRowOf(plan.FindLayout(Id(703))!));
    }

    // GW-U-547
    [Fact]
    public void OverflowBaseGivesTheContractMovesNoRoomAndLayouts()
    {
        OrganizePlan plan = Run(Overflow());

        Assert.Equal(
            new[]
            {
                Move(402, 0, 404, "Wood", 1, 50, 50, "Wood"),
                Move(402, 1, 403, "Wood", 1, 50, 50, "Wood"),
                Move(402, 2, 405, "Stone", 1, 50, 1, "Stone"),
            },
            MovesOf(plan));
        Assert.Equal(new[] { NoRoom(402, 3, "Flint", 1, 50, 1, "Stone") }, NoRoomOf(plan));
        Assert.Equal(
            new[]
            {
                Layout(401, ChestRole.Category, "Wood", S("Wood", 50)),
                Layout(402, ChestRole.Source, null, S("Flint", 1)),
                Layout(403, ChestRole.ClaimedOverflow, "Wood", S("Wood", 50)),
                Layout(404, ChestRole.ClaimedOverflow, "Wood", S("Wood", 50)),
                Layout(405, ChestRole.ClaimedOverflow, "Stone", S("Stone", 1)),
            },
            LayoutsOf(plan));
    }
}
