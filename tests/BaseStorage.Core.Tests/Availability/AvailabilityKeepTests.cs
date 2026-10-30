namespace BaseStorage.Core.Tests.Availability;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;

/// <summary>AC-2 (contract 9.6 C2, C3, C4 and the 9.7 rows A6-A8, plus assignment lookup): the Keep rule and the in-use exclusion.</summary>
public sealed class AvailabilityKeepTests
{
    // GW-U-410 (AC-2, 9.7 A7): with Keep off, chest 63 is excluded from every count, so Wood is 12 / 17 / 29 with Missing 31.
    [Fact]
    public void KeepOffExcludesTheKeepChestFromEveryCount()
    {
        World world = Create();

        AvailabilityResult result = BaseAvailability.Compute(Snapshot(world, false), Requirements());

        Assert.Equal(KeepOffRows, result.Requirements.Select(Counts).ToArray());
        Assert.Equal(new uint[] { 42, 88 }, Ids(result.CountedContainers));
        Assert.Equal(new uint[] { 57 }, Ids(result.SkippedInUse));
    }

    // GW-U-411 (AC-2, 9.7 A6 and A8): with Keep on, chest 63 is counted, LeatherScraps InContainers 10 comes only from it,
    // and FindFirstMissing with Keep off is the Wood row (12, 17).
    [Fact]
    public void KeepOnCountsTheKeepChestAndFirstMissingFollowsKeepOff()
    {
        World world = Create();

        AvailabilityResult keepOn = BaseAvailability.Compute(Snapshot(world, true), Requirements());
        RequirementAvailability? firstMissing = BaseAvailability.FindFirstMissing(Snapshot(world, false), Requirements());

        Assert.Contains(63u, Ids(keepOn.CountedContainers));
        Assert.Equal(10, keepOn.Requirements[2].InContainers);
        Assert.Equal(new RequirementAvailability(new CraftRequirement("Wood", 60), 12, 17), firstMissing);
    }

    // GW-U-412 (AC-2, C3 and C4): a PlayerBuilt | InUseByOther container assigned Keep is in SkippedInUse and contributes 0 when Keep is craftable.
    [Fact]
    public void KeepAndInUseContainerIsSkippedWhenKeepIsCraftable()
    {
        World world = Create();
        ContainerSnapshot keepInUse = Box(90u, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, Stack("Wood", 25, 1, 50));
        var assignments = new Dictionary<ContainerId, CategoryAssignment> { [keepInUse.Id] = CategoryAssignment.Keep };
        BaseSnapshot snapshot = Custom(world.Inventory, new[] { keepInUse }, assignments, true, true);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("Wood", 60) });

        Assert.Equal(new uint[] { 90 }, Ids(result.SkippedInUse));
        Assert.Empty(result.CountedContainers);
        Assert.Equal(0, result.Requirements[0].InContainers);
        Assert.Equal(12, result.Requirements[0].InInventory);
    }

    // GW-U-412 (AC-2, C3 and C4): the same container is in neither list when Keep is not craftable, and InInventory is unchanged.
    [Fact]
    public void KeepAndInUseContainerIsInNeitherListWhenKeepIsNotCraftable()
    {
        World world = Create();
        ContainerSnapshot keepInUse = Box(90u, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, Stack("Wood", 25, 1, 50));
        var assignments = new Dictionary<ContainerId, CategoryAssignment> { [keepInUse.Id] = CategoryAssignment.Keep };
        BaseSnapshot snapshot = Custom(world.Inventory, new[] { keepInUse }, assignments, true, false);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("Wood", 60) });

        Assert.Empty(result.SkippedInUse);
        Assert.Empty(result.CountedContainers);
        Assert.Equal(0, result.Requirements[0].InContainers);
        Assert.Equal(12, result.Requirements[0].InInventory);
    }

    // GW-U-413 (AC-2, assignment lookup a): category "Ores" on container 42 is not Keep, so 42 counts under both settings (CopperOre 30).
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CategoryAssignmentIsNotKeepAndCountsUnderBothSettings(bool keepChestsCraftable)
    {
        World world = Create();

        AvailabilityResult result = BaseAvailability.Compute(Snapshot(world, keepChestsCraftable), new[] { new CraftRequirement("CopperOre", 31) });

        Assert.Equal(30, result.Requirements[0].InContainers);
        Assert.Contains(42u, Ids(result.CountedContainers));
    }

    // GW-U-413 (AC-2, assignment lookup b): with no assignments, 63 is not Keep, so it counts with Keep off too (Wood includes its 40).
    [Fact]
    public void MissingAssignmentMeansNotKeep()
    {
        World world = Create();
        BaseSnapshot snapshot = Custom(world.Inventory, world.Containers, NoAssignments(), true, false);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement("Wood", 60) });

        Assert.Equal(new uint[] { 42, 63, 88 }, Ids(result.CountedContainers));
        Assert.Equal((12, 57, 69, true, 0), Counts(result.Requirements[0]));
    }

    // GW-U-413 (AC-2, assignment lookup c): a Keep assignment for an id that is not in containers is ignored and does not throw.
    [Fact]
    public void AssignmentForUnknownIdIsIgnored()
    {
        World world = Create();
        world.Assignments[Id(999u)] = CategoryAssignment.Keep;
        BaseSnapshot snapshot = Custom(world.Inventory, world.Containers, world.Assignments, true, true);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, Requirements());

        Assert.Equal(new uint[] { 42, 63, 88 }, Ids(result.CountedContainers));
        Assert.Equal((12, 57, 69, true, 0), Counts(result.Requirements[0]));
    }
}
