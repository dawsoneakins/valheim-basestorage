namespace BaseStorage.Core.Tests.Availability;

using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;

/// <summary>AC-1 (contract 9.6 C1, C2, C4 and the 9.7 rows A1-A5, c57 variant): the counts of the worked example.</summary>
public sealed class AvailabilityCountTests
{
    // GW-U-400 (AC-1, 9.7 A1-A5): Compute(keepOn, reqs) answers five requirements in input order, with the contract counts.
    [Fact]
    public void WorkedExampleCountsEachRequirementInInputOrder()
    {
        World world = Create();

        AvailabilityResult result = BaseAvailability.Compute(Snapshot(world, true), Requirements());

        Assert.Equal(5, result.Requirements.Count);
        Assert.Equal(KeepOnRows, result.Requirements.Select(Counts).ToArray());
    }

    // GW-U-401 (AC-1, 9.7 C2 and C4): the counted list is [42, 63, 88], the skipped list is [57], and tombstone 70 is in neither.
    [Fact]
    public void CountedAndSkippedListsHoldTheContractIds()
    {
        World world = Create();

        AvailabilityResult result = BaseAvailability.Compute(Snapshot(world, true), Requirements());

        Assert.Equal(new uint[] { 42, 63, 88 }, Ids(result.CountedContainers));
        Assert.Equal(new uint[] { 57 }, Ids(result.SkippedInUse));
        Assert.DoesNotContain(70u, Ids(result.CountedContainers));
        Assert.DoesNotContain(70u, Ids(result.SkippedInUse));
    }

    // GW-U-402 (AC-1, 9.7 c57 variant): c57 without InUseByOther is counted in full, so Wood is 12 / 80 / 92 and Bronze is 0 / 4 / 4.
    [Fact]
    public void ContainerWithoutInUseFlagIsCountedInFull()
    {
        World world = Create();
        ContainerSnapshot c57Free = Make(
            57u, 18f, 31.25f, -836.5f, 5, 2, ContainerFlags.PlayerBuilt,
            Stack("Wood", 23, 1, 50),
            Stack("Bronze", 4, 1, 30));
        ContainerSnapshot[] containers = { world.C88, c57Free, world.C42, world.C70, world.C63 };
        BaseSnapshot snapshot = Custom(world.Inventory, containers, world.Assignments, true, true);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, Requirements());

        Assert.Equal((12, 80, 92, true, 0), Counts(result.Requirements[0]));
        Assert.Equal((0, 4, 4, true, 0), Counts(result.Requirements[4]));
        Assert.Equal(new uint[] { 42, 57, 63, 88 }, Ids(result.CountedContainers));
        Assert.Empty(result.SkippedInUse);
    }
}
