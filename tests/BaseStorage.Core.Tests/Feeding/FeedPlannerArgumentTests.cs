namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Feeding.FeedFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>Validation order and purity of the 7-argument <c>FeedPlanner.Next</c> (contract 3.4, 0.1, 7.6).</summary>
public sealed class FeedPlannerArgumentTests
{
    // GW-U-2675. Contract 7.6 has no row ids, so V1 to V7 number its rows in order.
    [Theory]
    [InlineData("V1", "station", true)]
    [InlineData("V2", "selection", true)]
    [InlineData("V3", "chests", true)]
    [InlineData("V4", "chests", false)]
    [InlineData("V5", "assignments", true)]
    [InlineData("V6", "assignments", false)]
    [InlineData("V7", "skip", true)]
    public void FeedPlannerOverloadValidatesInContractOrder(string id, string paramName, bool isNull)
    {
        ContainerSnapshot c1 = C(1, 1f, 0f, 4, S("CopperOre", 30));
        var options = new EligibilityOptions(true, true);
        FeedStationState smelter = FeedStationState.SmelterClass(0, 10, 20f, 20f, "Coal", Ores);
        var none = new Dictionary<ContainerId, CategoryAssignment>();

        Action call = id switch
        {
            "V1" => () => FeedPlanner.Next(null!, FeedSelection.Empty, new[] { c1 }, none, options, StationAt, NoSkip),
            "V2" => () => FeedPlanner.Next(smelter, null!, new[] { c1 }, none, options, StationAt, NoSkip),
            "V3" => () => FeedPlanner.Next(smelter, FeedSelection.Empty, null!, none, options, StationAt, NoSkip),
            "V4" => () => FeedPlanner.Next(smelter, FeedSelection.Empty, new ContainerSnapshot[] { null! }, none, options, StationAt, NoSkip),
            "V5" => () => FeedPlanner.Next(smelter, FeedSelection.Empty, new[] { c1 }, null!, options, StationAt, NoSkip),
            "V6" => () => FeedPlanner.Next(smelter, FeedSelection.Empty, new[] { c1 }, new Dictionary<ContainerId, CategoryAssignment> { [Id(9)] = null! }, options, StationAt, NoSkip),
            "V7" => () => FeedPlanner.Next(smelter, FeedSelection.Empty, new[] { c1 }, none, options, StationAt, null!),
            _ => throw new ArgumentException("Unknown row: " + id, nameof(id)),
        };

        Type expectedType = isNull ? typeof(ArgumentNullException) : typeof(ArgumentException);
        Exception? thrown = Record.Exception(call);
        Assert.True(
            thrown is not null && thrown.GetType() == expectedType,
            $"{id}: expected {expectedType.Name}, got {thrown?.GetType().Name ?? "no exception"}.");
        Assert.Equal(paramName, ((ArgumentException)thrown!).ParamName);
    }

    [Fact]
    public void AllNullArgumentsReportStationFirst()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() =>
            FeedPlanner.Next(null!, null!, null!, null!, default, StationAt, null!));
        Assert.Equal("station", error.ParamName);
    }

    [Fact]
    public void DuplicateChestIdsAreNotRejected()
    {
        ContainerSnapshot first = C(1, 1f, 0f, 4, S("CopperOre", 30));
        ContainerSnapshot second = C(1, 9f, 0f, 4, S("CopperOre", 30));

        FeedOrder? order = Next7(FeedStationState.SmelterClass(0, 10, 20f, 20f, "Coal", Ores), Items("CopperOre"), new[] { first, second });

        Assert.True(order is not null, "Duplicate chest ids gave no order.");
        Assert.Equal(10, order!.Amount);
    }

    // GW-U-2676
    [Fact]
    public void TheOverloadIsPureAndMutatesNoInput()
    {
        ContainerSnapshot c1 = C(1, 1f, 0f, 4, S("CopperOre", 30));
        ContainerSnapshot c2 = C(2, 9f, 0f, 4, S("CopperOre", 30));
        ContainerSnapshot[] chests = { c1, c2 };
        Dictionary<ContainerId, CategoryAssignment> assignments = Assign((1, CategoryAssignment.Keep));
        ContainerId[] skip = { Id(9) };
        FeedSelection selection = Sel(("CopperOre", 25));
        FeedStationState station = FeedStationState.SmelterClass(0, 10, 20f, 20f, "Coal", Ores);

        FeedOrder? first = Next7(station, selection, chests, assignments, Opt(false), skip);
        FeedOrder? second = Next7(station, selection, chests, assignments, Opt(false), skip);
        AssertSameOrder(first, second);

        Assert.Same(c1, chests[0]);
        Assert.Same(c2, chests[1]);
        Assert.Single(assignments);
        Assert.True(assignments[Id(1)].IsKeep);
        Assert.Equal(new[] { Id(9) }, skip);
        Assert.Single(selection.Entries);
        Assert.Equal(25, selection.Entries[0].Keep);

        // The result changes only through the options: KC1 gives chest 1, KC2 gives chest 2.
        FeedOrder? craftable = Next7(station, Items("CopperOre"), chests, assignments, Opt(true));
        FeedOrder? notCraftable = Next7(station, Items("CopperOre"), chests, assignments, Opt(false));
        Assert.Equal(Id(1), craftable!.Chest);
        Assert.Equal(Id(2), notCraftable!.Chest);
    }

    // FM14 (no contract row of its own; contract 0.2 FM14 is pinned here by name)
    [Fact]
    public void FM14_AnOrderMovesOnlyTheNamedItems()
    {
        ContainerSnapshot nearest = C(3, 0f, 0f, 4, S("CopperOre", 30), S("TinOre", 5));
        ContainerSnapshot keep = C(2, 9f, 0f, 4, S("CopperOre", 30));
        ContainerSnapshot source = C(1, 1f, 0f, 4, S("CopperOre", 30));
        ContainerSnapshot[] chests = { source, keep, nearest };
        Dictionary<ContainerId, CategoryAssignment> assignments = Assign((2, CategoryAssignment.Keep));
        ContainerId[] skip = { Id(3) };

        FeedOrder? order = Next7(FeedStationState.SmelterClass(0, 10, 20f, 20f, "Coal", Ores), Items("CopperOre"), chests, assignments, Opt(false), skip);

        // Chest 2 is a Keep chest excluded with KeepChestsCraftable off, and chest 3 is skipped, so only chest 1 may give.
        Assert.True(order is not null, "FM14 gave no order.");
        Assert.Equal(Id(1), order!.Chest);
        Assert.Equal("CopperOre", order.PrefabName);
        Assert.Equal(10, order.Amount);
        Assert.Equal(30, source.Contents[0].Count);
        Assert.Equal(30, keep.Contents[0].Count);
        Assert.Equal(30, nearest.Contents[0].Count);
    }
}
