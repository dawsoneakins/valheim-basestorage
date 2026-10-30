namespace BaseStorage.Core.Tests.Availability;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;

/// <summary>AC-4 (contract 9.5, R8, V1 to V3, V7): BaseSnapshot validation, defensive copies and read-only views.</summary>
public sealed class BaseSnapshotTests
{
    // GW-U-431 (AC-4, 9.5 R8): a null playerInventory, containers or assignments gives ArgumentNullException with the matching ParamName.
    [Fact]
    public void NullArgumentsAreRejectedByParamName()
    {
        World world = Create();
        var options = new EligibilityOptions(true, true);

        ArgumentNullException inventory = Assert.Throws<ArgumentNullException>(
            () => new BaseSnapshot(null!, world.Containers, world.Assignments, options));
        ArgumentNullException containers = Assert.Throws<ArgumentNullException>(
            () => new BaseSnapshot(world.Inventory, null!, world.Assignments, options));
        ArgumentNullException assignments = Assert.Throws<ArgumentNullException>(
            () => new BaseSnapshot(world.Inventory, world.Containers, null!, options));

        Assert.Equal("playerInventory", inventory.ParamName);
        Assert.Equal("containers", containers.ParamName);
        Assert.Equal("assignments", assignments.ParamName);
    }

    // GW-U-431 (AC-4, 9.5 V1): a container with the inventory's id gives ArgumentException with ParamName "containers".
    [Fact]
    public void InventoryIdInContainersIsRejected()
    {
        World world = Create();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new BaseSnapshot(world.Inventory, new[] { world.C42, world.Inventory }, world.Assignments, new EligibilityOptions(true, true)));

        Assert.Equal("containers", ex.ParamName);
    }

    // GW-U-431 (AC-4, 9.5 V2): a null element in containers gives ArgumentException with ParamName "containers".
    [Fact]
    public void NullContainerElementIsRejected()
    {
        World world = Create();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new BaseSnapshot(world.Inventory, new ContainerSnapshot[] { world.C42, null! }, world.Assignments, new EligibilityOptions(true, true)));

        Assert.Equal("containers", ex.ParamName);
    }

    // GW-U-431 (AC-4, 9.5 V3): a null assignment value gives ArgumentException with ParamName "assignments".
    [Fact]
    public void NullAssignmentValueIsRejected()
    {
        World world = Create();
        var assignments = new Dictionary<ContainerId, CategoryAssignment> { [world.C42.Id] = null! };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new BaseSnapshot(world.Inventory, world.Containers, assignments, new EligibilityOptions(true, true)));

        Assert.Equal("assignments", ex.ParamName);
    }

    // GW-U-431 (AC-4, 9.5): a duplicate container id is allowed, and both entries are kept.
    [Fact]
    public void DuplicateContainerIdsAreAllowed()
    {
        World world = Create();

        BaseSnapshot snapshot = new BaseSnapshot(world.Inventory, new[] { world.C42, world.C42 }, world.Assignments, new EligibilityOptions(true, true));

        Assert.Equal(2, snapshot.Containers.Count);
    }

    // GW-U-431 (AC-4, 9.5): Options round-trips, and PlayerInventory is the same reference that was passed in.
    [Fact]
    public void OptionsRoundTripAndInventoryIsTheSameReference()
    {
        World world = Create();
        var options = new EligibilityOptions(false, true);

        BaseSnapshot snapshot = new BaseSnapshot(world.Inventory, world.Containers, world.Assignments, options);

        Assert.Equal(options, snapshot.Options);
        Assert.Same(world.Inventory, snapshot.PlayerInventory);
    }

    // GW-U-433 (AC-4, 9.5 V7): the snapshot copies its inputs, so clearing the caller's list and removing a key have no effect on the answer.
    [Fact]
    public void LaterChangesToTheCallersCollectionsHaveNoEffect()
    {
        World world = Create();
        var list = new List<ContainerSnapshot>(world.Containers);
        Dictionary<ContainerId, CategoryAssignment> assignments = world.Assignments;
        var snapshot = new BaseSnapshot(world.Inventory, list, assignments, new EligibilityOptions(true, true));
        list.Clear();
        assignments.Remove(world.C63.Id);

        AvailabilityResult result = BaseAvailability.Compute(snapshot, Requirements());

        Assert.Equal(5, snapshot.Containers.Count);
        Assert.Equal(KeepOnRows, result.Requirements.Select(Counts).ToArray());
    }

    // GW-U-433 (AC-4, 9.5 read-only views): writes through a cast IList or IDictionary throw NotSupportedException.
    [Fact]
    public void ReadOnlyViewsRejectWritesThroughACast()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);
        AvailabilityResult result = BaseAvailability.Compute(snapshot, Requirements());

        Assert.Throws<NotSupportedException>(() => ((IList<ContainerSnapshot>)snapshot.Containers).Add(world.C42));
        Assert.Throws<NotSupportedException>(() => ((IList<RequirementAvailability>)result.Requirements).Add(result.Requirements[0]));
        Assert.Throws<NotSupportedException>(() => ((IList<ContainerSnapshot>)result.CountedContainers).Add(world.C42));
        Assert.Throws<NotSupportedException>(() => ((IList<ContainerSnapshot>)result.SkippedInUse).Add(world.C57));
        Assert.Throws<NotSupportedException>(
            () => ((IDictionary<ContainerId, CategoryAssignment>)snapshot.Assignments).Add(Id(999u), CategoryAssignment.Keep));
    }

    // GW-U-433 (AC-4, 9.5 defensive copy): a requirement list changed after Compute returns does not change the result.
    [Fact]
    public void RequirementListChangedAfterComputeDoesNotChangeTheResult()
    {
        World world = Create();
        var list = new List<CraftRequirement> { new CraftRequirement("Wood", 60) };
        AvailabilityResult result = BaseAvailability.Compute(Snapshot(world, true), list);

        list[0] = new CraftRequirement("Resin", 5);
        list.Add(new CraftRequirement("Bronze", 2));

        Assert.Single(result.Requirements);
        Assert.Equal(new RequirementAvailability(new CraftRequirement("Wood", 60), 12, 57), result.Requirements[0]);
    }
}
