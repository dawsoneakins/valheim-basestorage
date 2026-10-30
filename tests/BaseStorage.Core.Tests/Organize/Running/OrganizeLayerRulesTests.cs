namespace BaseStorage.Core.Tests.Organize.Running;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize;

/// <summary>GW-U-1370..1373 (AC-1, contract 4.2): the status table, the fit rule and the source lookup of the game layer.</summary>
public sealed class OrganizeLayerRulesTests
{
    // GW-U-1370: the status table, every row.
    [Theory]
    [InlineData(true, null, ReservationFailure.None, ReservationStatus.Denied)]
    [InlineData(true, ReservationState.Granted, ReservationFailure.None, ReservationStatus.Denied)]
    [InlineData(false, null, ReservationFailure.None, ReservationStatus.None)]
    [InlineData(false, ReservationState.Requesting, ReservationFailure.None, ReservationStatus.Pending)]
    [InlineData(false, ReservationState.Granted, ReservationFailure.None, ReservationStatus.Granted)]
    [InlineData(false, ReservationState.Writing, ReservationFailure.None, ReservationStatus.Granted)]
    [InlineData(false, ReservationState.Releasing, ReservationFailure.None, ReservationStatus.Lost)]
    [InlineData(false, ReservationState.Completed, ReservationFailure.None, ReservationStatus.None)]
    [InlineData(false, ReservationState.Failed, ReservationFailure.Denied, ReservationStatus.Denied)]
    [InlineData(false, ReservationState.Failed, ReservationFailure.InUseByOther, ReservationStatus.Denied)]
    [InlineData(false, ReservationState.Failed, ReservationFailure.Timeout, ReservationStatus.Pending)]
    [InlineData(false, ReservationState.Failed, ReservationFailure.OwnerLost, ReservationStatus.Lost)]
    [InlineData(false, ReservationState.Failed, ReservationFailure.RequesterDisconnected, ReservationStatus.Lost)]
    [InlineData(false, ReservationState.Failed, ReservationFailure.Cancelled, ReservationStatus.Lost)]
    [InlineData(false, ReservationState.Failed, ReservationFailure.None, ReservationStatus.Lost)]
    [InlineData(false, ReservationState.Failed, (ReservationFailure)42, ReservationStatus.Lost)]
    [InlineData(false, (ReservationState)99, ReservationFailure.None, ReservationStatus.Lost)]
    public void StatusOfFollowsTheTable(bool busy, ReservationState? state, ReservationFailure failure, ReservationStatus expected)
    {
        Assert.Equal(expected, OrganizeLayerRules.StatusOf(state, failure, busy));
    }

    // GW-U-1371: the fit rule counts free slots and same-item partial stacks only.
    [Theory]
    [InlineData(2, 0, 0, 100, true)]
    [InlineData(2, 0, 0, 101, false)]
    [InlineData(1, 30, 0, 20, true)]
    [InlineData(1, 30, 0, 21, false)]
    [InlineData(2, 30, 0, 70, true)]
    [InlineData(2, 30, 0, 71, false)]
    [InlineData(1, 30, 2, 20, false)]
    [InlineData(1, 50, 0, 1, false)]
    public void CanFitCountsFreeSlotsAndSameItemRoom(int slots, int woodCount, int woodQuality, int add, bool expected)
    {
        var contents = woodCount == 0
            ? Array.Empty<ItemStackSnapshot>()
            : new[] { new ItemStackSnapshot("Wood", woodCount, Math.Max(1, woodQuality), 50) };
        bool quality1 = woodQuality <= 1;
        var items = new ItemStackSnapshot("Wood", add, 1, 50);

        bool fits = OrganizeLayerRules.CanFit(contents, slots, items);

        Assert.Equal(quality1 || contents.Length == 0 ? expected : false || expected, fits);
    }

    // GW-U-1371: a different prefab or quality never counts as room, and more stacks than slots never fits.
    [Fact]
    public void CanFitIgnoresOtherItemsAndRejectsOverfullChests()
    {
        var stone = new ItemStackSnapshot("Stone", 10, 1, 50);
        var woodQ2 = new ItemStackSnapshot("Wood", 10, 2, 50);
        var wood = new ItemStackSnapshot("Wood", 40, 1, 50);

        Assert.False(OrganizeLayerRules.CanFit(new[] { stone }, 1, wood));
        Assert.False(OrganizeLayerRules.CanFit(new[] { woodQ2 }, 1, wood));
        Assert.True(OrganizeLayerRules.CanFit(new[] { woodQ2 }, 2, wood));
        Assert.False(OrganizeLayerRules.CanFit(new[] { stone, wood }, 1, new ItemStackSnapshot("Wood", 1, 1, 50)));
        Assert.True(OrganizeLayerRules.CanFit(new[] { OrganizeFixtures.S("Wood", 50) }, 2, new ItemStackSnapshot("Wood", 50, 1, 50)));
        Assert.Equal("contents", Assert.Throws<ArgumentNullException>(() => OrganizeLayerRules.CanFit(null!, 1, wood)).ParamName);
        Assert.Equal("items", Assert.Throws<ArgumentNullException>(() => OrganizeLayerRules.CanFit(Array.Empty<ItemStackSnapshot>(), 1, null!)).ParamName);
    }

    // GW-U-1372: the source lookup is the first stack of the item with enough items.
    [Fact]
    public void IndexOfSourceFindsTheFirstStackWithEnoughItems()
    {
        var contents = new[]
        {
            OrganizeFixtures.S("Stone", 10),
            OrganizeFixtures.S("Wood", 5),
            OrganizeFixtures.S("Wood", 30, quality: 2),
            OrganizeFixtures.S("Wood", 30),
            OrganizeFixtures.S("Wood", 40, maxStack: 20),
        };

        Assert.Equal(3, OrganizeLayerRules.IndexOfSource(contents, "Wood", 1, 50, 10));
        Assert.Equal(1, OrganizeLayerRules.IndexOfSource(contents, "Wood", 1, 50, 5));
        Assert.Equal(2, OrganizeLayerRules.IndexOfSource(contents, "Wood", 2, 50, 30));
        Assert.Equal(4, OrganizeLayerRules.IndexOfSource(contents, "Wood", 1, 20, 1));
        Assert.Equal(-1, OrganizeLayerRules.IndexOfSource(contents, "Wood", 1, 50, 31));
        Assert.Equal(-1, OrganizeLayerRules.IndexOfSource(contents, "wood", 1, 50, 1));
        Assert.Equal(-1, OrganizeLayerRules.IndexOfSource(Array.Empty<ItemStackSnapshot>(), "Wood", 1, 50, 1));
        Assert.Equal("contents", Assert.Throws<ArgumentNullException>(() => OrganizeLayerRules.IndexOfSource(null!, "Wood", 1, 50, 1)).ParamName);
        Assert.Equal("prefabName", Assert.Throws<ArgumentNullException>(() => OrganizeLayerRules.IndexOfSource(contents, null!, 1, 50, 1)).ParamName);
    }

    // GW-U-1373: the operation label.
    [Fact]
    public void TheOperationLabelIsOrganize()
    {
        Assert.Equal("organize", OrganizeLayerRules.Operation);
    }
}
