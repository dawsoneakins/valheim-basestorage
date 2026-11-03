namespace BaseStorage.Core.Tests.Feeding;

using BaseStorage.Core.Feeding;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Feeding.FeedFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// Keep chests and the source rule (FM12, FM13, FM15, contract 4 and 7.5). Each KC row is run with both values of KeepChestsCraftable (AC-4).
/// </summary>
public sealed class FeedKeepChestTests
{
    // GW-U-2671. An expected chest of 0 means the planner returns null. The Theory is keyed by the KeepChestsCraftable value.
    [Theory]
    [InlineData(true, "KC1", 1, 10)]
    [InlineData(false, "KC2", 2, 10)]
    [InlineData(false, "KC3", 0, 0)]
    [InlineData(true, "KC4", 1, 10)]
    [InlineData(true, "KC5", 1, 10)]
    [InlineData(false, "KC6", 0, 0)]
    [InlineData(false, "KC12", 2, 3)]
    [InlineData(true, "KC13", 1, 10)]
    public void FM12_KeepChestsGiveOnlyWhenKeepChestsCraftable(bool keepChestsCraftable, string id, int expectedChest, int expectedAmount)
    {
        KeepRow row = KeepRowOf(id, keepChestsCraftable);
        FeedOrder? order = Next7(row.Station, row.Selection, row.Chests, row.Assignments, row.Options);

        // KC6: the excluded Keep chest holds 10, which does not count toward the keep of 25, so the spare is -15 and nothing is taken.
        ExpectedOrder? expected = expectedChest == 0 ? null : new ExpectedOrder((uint)expectedChest, row.Selection.Entries[0].PrefabName, expectedAmount, row.Station.Shape == FeedStationShape.FuelOnly);
        Assert.True(Matches(expected, order), $"{id} with KeepChestsCraftable {keepChestsCraftable}: got {Describe(order)}, expected {expected}.");
    }

    // GW-U-2672
    [Fact]
    public void FM12_OnlyAKeepMarkExcludesAChest()
    {
        // KC7: a category-assigned chest is a source even with KeepChestsCraftable off.
        FeedOrder? category = Next7(KeepRowOf("KC7").Station, KeepRowOf("KC7").Selection, KeepRowOf("KC7").Chests, KeepRowOf("KC7").Assignments, Opt(false));
        Assert.True(Matches(new ExpectedOrder(1, "CopperOre", 10, false), category), "KC7: " + Describe(category));

        // KC8: no assignment at all is not Keep.
        FeedOrder? none = Next7(KeepRowOf("KC8").Station, KeepRowOf("KC8").Selection, KeepRowOf("KC8").Chests, KeepRowOf("KC8").Assignments, Opt(false));
        Assert.True(Matches(new ExpectedOrder(1, "CopperOre", 10, false), none), "KC8: " + Describe(none));

        // An assignment for an id that is not among the chests changes nothing.
        ContainerSnapshot[] chests = { C(1, 1f, 0f, 4, S("CopperOre", 30)), C(2, 9f, 0f, 4, S("CopperOre", 30)) };
        FeedOrder? stray = Next7(
            FeedStationState.SmelterClass(0, 10, 20f, 20f, "Coal", Ores),
            Items("CopperOre"),
            chests,
            Assign((9, CategoryAssignment.Keep)),
            Opt(false));
        Assert.True(Matches(new ExpectedOrder(1, "CopperOre", 10, false), stray), "a stray assignment: " + Describe(stray));
    }

    // GW-U-2673
    [Fact]
    public void FM12_EligibilityUsesCraftAvailabilityRules()
    {
        // KC9: a tombstone is excluded, so chest 2 gives.
        KeepRow tombstone = KeepRowOf("KC9");
        Assert.True(Matches(new ExpectedOrder(2, "CopperOre", 10, false), Next7(tombstone.Station, tombstone.Selection, tombstone.Chests, tombstone.Assignments, tombstone.Options)), "KC9");

        // KC10: a vehicle is excluded when ExcludeVehicles is on, so chest 2 gives.
        KeepRow vehicle = KeepRowOf("KC10");
        Assert.True(Matches(new ExpectedOrder(2, "CopperOre", 10, false), Next7(vehicle.Station, vehicle.Selection, vehicle.Chests, vehicle.Assignments, vehicle.Options)), "KC10");

        // KC11: the same vehicle gives when ExcludeVehicles is off.
        KeepRow allowed = KeepRowOf("KC11");
        Assert.True(Matches(new ExpectedOrder(1, "CopperOre", 10, false), Next7(allowed.Station, allowed.Selection, allowed.Chests, allowed.Assignments, allowed.Options)), "KC11");
    }

    // GW-U-2674
    [Theory]
    [InlineData("KC1")]
    [InlineData("KC2")]
    [InlineData("KC3")]
    [InlineData("KC4")]
    [InlineData("KC5")]
    [InlineData("KC6")]
    [InlineData("KC7")]
    [InlineData("KC8")]
    [InlineData("KC9")]
    [InlineData("KC10")]
    [InlineData("KC11")]
    [InlineData("KC12")]
    [InlineData("KC13")]
    public void FM13_TheNewOverloadEqualsTheFilteredFiveArgumentCall(string id)
    {
        KeepRow row = KeepRowOf(id);
        ContainerSnapshot[] sources = SourcesOf(row.Chests, row.Assignments, row.Options);

        FeedOrder? fiveArgument = Next5(row.Station, row.Selection, sources);
        FeedOrder? sevenArgument = Next7(row.Station, row.Selection, row.Chests, row.Assignments, row.Options);
        AssertSameOrder(fiveArgument, sevenArgument);
    }

    // FM15 (no contract row of its own; contract 0.2 FM15 is pinned here by name)
    [Fact]
    public void FM15_SmelterDefaultKeepsKeepChestsFeeding()
    {
        // With KeepChestsCraftable on, every chest v0.4.0 used is still a source, so the result equals the unfiltered 5-argument call.
        foreach (string id in new[] { "KC1", "KC4", "KC5", "KC13" })
        {
            KeepRow row = KeepRowOf(id, true);
            FeedOrder? v040 = Next5(row.Station, row.Selection, row.Chests);
            FeedOrder? current = Next7(row.Station, row.Selection, row.Chests, row.Assignments, Opt(true));
            AssertSameOrder(v040, current);
        }

        KeepRow kc1 = KeepRowOf("KC1", true);
        Assert.True(Matches(new ExpectedOrder(1, "CopperOre", 10, false), Next7(kc1.Station, kc1.Selection, kc1.Chests, kc1.Assignments, Opt(true))), "KC1 default");
    }
}
