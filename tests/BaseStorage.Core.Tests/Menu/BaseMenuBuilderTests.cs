namespace BaseStorage.Core.Tests.Menu;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Menu;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>S-C-002: the chest list sorts, filters and measures without touching the game.</summary>
public sealed class BaseMenuBuilderTests
{
    private static readonly WorldPosition Origin = new WorldPosition(0f, 0f, 0f);

    [Fact]
    public void RowsSortByRuleThenDistance()
    {
        var far = C(1, 20f, 0f, 4, S("Wood", 1));
        var near = C(2, 2f, 0f, 4, S("Wood", 1));
        var stone = C(3, 1f, 0f, 4, S("Stone", 1));
        var assignments = Assign(
            (1, CategoryAssignment.ForCategory("Wood")),
            (2, CategoryAssignment.ForCategory("Wood")),
            (3, CategoryAssignment.ForCategory("Stone")));

        IReadOnlyList<BaseMenuRow> rows = BaseMenuBuilder.Rows(new[] { far, near, stone }, assignments, Origin, null, StringTable.English);

        Assert.Equal(new[] { stone.Id, near.Id, far.Id }, rows.Select(r => r.Chest.Id));
    }

    [Fact]
    public void FillPercentCountsSlotsNotItems()
    {
        var chest = C(1, 0f, 0f, 4, S("Wood", 50));

        BaseMenuRow row = Assert.Single(BaseMenuBuilder.Rows(new[] { chest }, Assign(), Origin, null, StringTable.English));

        Assert.Equal(25, row.FillPercent);
    }

    [Fact]
    public void AFilterKeepsOneRule()
    {
        var wood = C(1, 0f, 0f, 4);
        var stone = C(2, 0f, 0f, 4);
        var assignments = Assign((1, CategoryAssignment.ForCategory("Wood")), (2, CategoryAssignment.ForCategory("Stone")));

        BaseMenuRow row = Assert.Single(BaseMenuBuilder.Rows(new[] { wood, stone }, assignments, Origin, "Wood", StringTable.English));

        Assert.Equal(wood.Id, row.Chest.Id);
    }

    [Fact]
    public void TheBaseFillIsZeroWithNoChests() =>
        Assert.Equal(0, BaseMenuBuilder.FillPercent(new ContainerSnapshot[0]));

    [Fact]
    public void TheBaseFillPoolsEveryChest() =>
        Assert.Equal(50, BaseMenuBuilder.FillPercent(new[] { C(1, 0f, 0f, 2, S("Wood", 1), S("Stone", 1)), C(2, 0f, 0f, 2) }));

    [Theory]
    [InlineData(2, 0f, 0f)] // already in view, so the list stays put
    [InlineData(2, 10f, 10f)]
    [InlineData(3, 0f, 55f)] // the first row that does not fit whole
    [InlineData(5, 0f, 115f)] // below the view, centred
    [InlineData(5, 300f, 115f)] // above the view, centred
    [InlineData(0, 300f, 0f)] // stops at the top
    [InlineData(19, 0f, 500f)] // stops at the bottom
    public void ScrollingToARowShowsIt(int index, float current, float expected) =>
        Assert.Equal(expected, BaseMenuBuilder.ScrollOffset(index, 20, 30f, 100f, current));

    [Fact]
    public void AShortListDoesNotScroll() =>
        Assert.Equal(0f, BaseMenuBuilder.ScrollOffset(2, 3, 30f, 100f, 0f));

    [Fact]
    public void AccessAndInUseFlagsReachTheRow()
    {
        var denied = CF(1, 0f, 0f, 4, ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied);
        var used = CF(2, 0f, 0f, 4, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther);

        IReadOnlyList<BaseMenuRow> rows = BaseMenuBuilder.Rows(new[] { denied, used }, Assign(), Origin, null, StringTable.English);

        Assert.True(rows.Single(r => r.Chest.Id == denied.Id).AccessDenied);
        Assert.True(rows.Single(r => r.Chest.Id == used.Id).InUse);
    }
}
