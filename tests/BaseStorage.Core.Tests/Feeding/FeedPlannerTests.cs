namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>What goes into a station next, from which chest, and how much is left alone.</summary>
public sealed class FeedPlannerTests
{
    private static readonly WorldPosition Station = new WorldPosition(0f, 30f, 0f);
    private static readonly ContainerId[] NoSkip = Array.Empty<ContainerId>();
    private static readonly string[] Ores = { "CopperOre", "TinOre" };

    private static FeedStationState Smelter(int queued = 0, float fuel = 20f) =>
        FeedStationState.SmelterClass(queued, 10, fuel, 20, "Coal", Ores);

    private static FeedOrder? Next(FeedStationState state, FeedSelection selection, params ContainerSnapshot[] chests) =>
        FeedPlanner.Next(state, selection, chests, Station, NoSkip);

    [Fact]
    public void AnEmptyStationTakesFuelFirst()
    {
        FeedOrder order = Next(Smelter(fuel: 0f), Selection("CopperOre", "Coal"), C(1, 1f, 0f, 4, S("CopperOre", 30), S("Coal", 30)))!;

        Assert.Equal("Coal", order.PrefabName);
        Assert.True(order.IsFuel);
        Assert.Equal(20, order.Amount);
    }

    [Fact]
    public void AStationWithFullFuelTakesItsInput()
    {
        FeedOrder order = Next(Smelter(), Selection("CopperOre", "Coal"), C(1, 1f, 0f, 4, S("CopperOre", 30), S("Coal", 30)))!;

        Assert.Equal("CopperOre", order.PrefabName);
        Assert.False(order.IsFuel);
        Assert.Equal(10, order.Amount);
    }

    [Fact]
    public void OnlyTheRoomLeftIsTaken() =>
        Assert.Equal(3, Next(Smelter(queued: 7), Selection("CopperOre"), C(1, 1f, 0f, 4, S("CopperOre", 30)))!.Amount);

    [Fact]
    public void AFullStationTakesNothing() =>
        Assert.Null(Next(Smelter(queued: 10), Selection("CopperOre", "Coal"), C(1, 1f, 0f, 4, S("CopperOre", 30), S("Coal", 30))));

    [Fact]
    public void APartBurntFuelDoesNotCountAsRoomForAWholeOne() =>
        Assert.Equal(0, Smelter(fuel: 19.5f).FuelRoom);

    [Fact]
    public void AStationThatBurnsNothingHasNoFuelRoom() =>
        Assert.Equal(0, FeedStationState.SmelterClass(0, 10, 0f, 0, null, Ores).FuelRoom);

    [Fact]
    public void AnItemTheStationCannotProcessIsIgnored() =>
        Assert.Null(Next(Smelter(), Selection("Wood"), C(1, 1f, 0f, 4, S("Wood", 30))));

    [Fact]
    public void AnItemNotChosenIsLeftAlone() =>
        Assert.Null(Next(Smelter(), Selection("TinOre"), C(1, 1f, 0f, 4, S("CopperOre", 30))));

    [Fact]
    public void InputsGoInTheOrderThePlayerChose() =>
        Assert.Equal("TinOre", Next(Smelter(), Selection("TinOre", "CopperOre"), C(1, 1f, 0f, 4, S("CopperOre", 30), S("TinOre", 30)))!.PrefabName);

    [Fact]
    public void WhenTheFirstInputRunsOutTheNextOneIsUsed() =>
        Assert.Equal("CopperOre", Next(Smelter(), Selection("TinOre", "CopperOre"), C(1, 1f, 0f, 4, S("CopperOre", 30)))!.PrefabName);

    [Fact]
    public void TheKeepAmountStaysInTheBase()
    {
        FeedSelection selection = FeedSelection.Empty.With("CopperOre", 25);

        Assert.Equal(5, Next(Smelter(), selection, C(1, 1f, 0f, 4, S("CopperOre", 30)))!.Amount);
    }

    [Fact]
    public void TheKeepAmountCountsTheWholeBaseNotOneChest()
    {
        FeedSelection selection = FeedSelection.Empty.With("CopperOre", 30);

        FeedOrder? order = Next(Smelter(), selection, C(1, 1f, 0f, 4, S("CopperOre", 20)), C(2, 9f, 0f, 4, S("CopperOre", 12)));

        Assert.Equal(2, order!.Amount);
    }

    [Fact]
    public void NothingIsTakenWhenOnlyTheKeepAmountIsLeft() =>
        Assert.Null(Next(Smelter(), FeedSelection.Empty.With("CopperOre", 30), C(1, 1f, 0f, 4, S("CopperOre", 30))));

    [Fact]
    public void TheNearestChestGivesFirst() =>
        Assert.Equal(Id(2), Next(Smelter(), Selection("CopperOre"), C(1, 50f, 0f, 4, S("CopperOre", 30)), C(2, 2f, 0f, 4, S("CopperOre", 30)))!.Chest);

    [Fact]
    public void OneChestCannotGiveMoreThanItHolds() =>
        Assert.Equal(4, Next(Smelter(), Selection("CopperOre"), C(1, 1f, 0f, 4, S("CopperOre", 4)), C(2, 9f, 0f, 4, S("CopperOre", 30)))!.Amount);

    [Fact]
    public void StacksInOneChestAddUp() =>
        Assert.Equal(7, Next(Smelter(), Selection("CopperOre"), C(1, 1f, 0f, 4, S("CopperOre", 3), S("CopperOre", 4)))!.Amount);

    [Fact]
    public void AChestInUseIsPassedOver()
    {
        ContainerSnapshot inUse = CF(1, 1f, 0f, 4, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("CopperOre", 30));

        Assert.Equal(Id(2), Next(Smelter(), Selection("CopperOre"), inUse, C(2, 9f, 0f, 4, S("CopperOre", 30)))!.Chest);
    }

    [Fact]
    public void AChestThatDeniesAccessDoesNotCount()
    {
        ContainerSnapshot denied = CF(1, 1f, 0f, 4, ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied, S("CopperOre", 30));

        Assert.Null(Next(Smelter(), Selection("CopperOre"), denied));
        Assert.Equal(0, FeedPlanner.CountInBase(new[] { denied }, "CopperOre"));
    }

    [Fact]
    public void AChestInUseStillCountsTowardTheKeepAmount()
    {
        ContainerSnapshot inUse = CF(1, 1f, 0f, 4, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("CopperOre", 30));

        Assert.Equal(30, FeedPlanner.CountInBase(new[] { inUse }, "CopperOre"));
    }

    [Fact]
    public void AChestOnTheSkipListIsPassedOver()
    {
        ContainerSnapshot[] chests = { C(1, 1f, 0f, 4, S("CopperOre", 30)), C(2, 9f, 0f, 4, S("CopperOre", 30)) };

        FeedOrder? order = FeedPlanner.Next(Smelter(), Selection("CopperOre"), chests, Station, new List<ContainerId> { Id(1) });

        Assert.Equal(Id(2), order!.Chest);
    }

    [Fact]
    public void NoChestsMeansNothingToDo() =>
        Assert.Null(Next(Smelter(queued: 0, fuel: 0f), Selection("CopperOre", "Coal")));

    [Fact]
    public void WhenFuelCannotBeFoundTheInputStillGoesIn() =>
        Assert.Equal("CopperOre", Next(Smelter(fuel: 0f), Selection("CopperOre", "Coal"), C(1, 1f, 0f, 4, S("CopperOre", 30)))!.PrefabName);

    private static FeedSelection Selection(params string[] prefabs)
    {
        FeedSelection selection = FeedSelection.Empty;
        foreach (string prefab in prefabs)
        {
            selection = selection.With(prefab, 0);
        }

        return selection;
    }
}
