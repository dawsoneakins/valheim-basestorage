namespace BaseStorage.Core.Tests.Availability;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;

/// <summary>AC-4 (contract 9.5, 9.6 NFR, V4, V8): argument validation of Compute and FindFirstMissing, and determinism with no mutation.</summary>
public sealed class BaseAvailabilityArgumentTests
{
    // GW-U-432 (AC-4, 9.5): a null snapshot gives ArgumentNullException with ParamName "snapshot", for both methods.
    [Fact]
    public void NullSnapshotIsRejectedByBothMethods()
    {
        CraftRequirement[] reqs = { new CraftRequirement("Wood", 1) };

        ArgumentNullException compute = Assert.Throws<ArgumentNullException>(() => BaseAvailability.Compute(null!, reqs));
        ArgumentNullException firstMissing = Assert.Throws<ArgumentNullException>(() => BaseAvailability.FindFirstMissing(null!, reqs));

        Assert.Equal("snapshot", compute.ParamName);
        Assert.Equal("snapshot", firstMissing.ParamName);
    }

    // GW-U-432 (AC-4, 9.5): null requirements give ArgumentNullException with ParamName "requirements", for both methods.
    [Fact]
    public void NullRequirementsAreRejectedByBothMethods()
    {
        BaseSnapshot snapshot = Snapshot(Create(), true);

        ArgumentNullException compute = Assert.Throws<ArgumentNullException>(() => BaseAvailability.Compute(snapshot, null!));
        ArgumentNullException firstMissing = Assert.Throws<ArgumentNullException>(() => BaseAvailability.FindFirstMissing(snapshot, null!));

        Assert.Equal("requirements", compute.ParamName);
        Assert.Equal("requirements", firstMissing.ParamName);
    }

    // GW-U-432 (AC-4, 9.5 V4): a null element in requirements gives ArgumentException with ParamName "requirements", for both methods.
    [Fact]
    public void NullRequirementElementIsRejectedByBothMethods()
    {
        BaseSnapshot snapshot = Snapshot(Create(), true);
        CraftRequirement[] reqs = { null! };

        ArgumentException compute = Assert.Throws<ArgumentException>(() => BaseAvailability.Compute(snapshot, reqs));
        ArgumentException firstMissing = Assert.Throws<ArgumentException>(() => BaseAvailability.FindFirstMissing(snapshot, reqs));

        Assert.Equal("requirements", compute.ParamName);
        Assert.Equal("requirements", firstMissing.ParamName);
    }

    // GW-U-434 (AC-4, 9.6 NFR, V8): two calls give the same answer, the inputs are unchanged afterwards, and another thread agrees.
    [Fact]
    public async Task RepeatedAndCrossThreadCallsAgreeAndLeaveInputsUnchanged()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);
        CraftRequirement[] reqs = Requirements();

        AvailabilityResult first = BaseAvailability.Compute(snapshot, reqs);
        AvailabilityResult second = BaseAvailability.Compute(snapshot, reqs);
        AvailabilityResult other = await Task.Run(() => BaseAvailability.Compute(snapshot, reqs));

        Assert.Equal(first.Requirements, second.Requirements);
        Assert.Equal(Ids(first.CountedContainers), Ids(second.CountedContainers));
        Assert.Equal(Ids(first.SkippedInUse), Ids(second.SkippedInUse));
        Assert.Equal(first.Requirements, other.Requirements);
        Assert.Equal(Ids(first.CountedContainers), Ids(other.CountedContainers));
        Assert.Equal(Ids(first.SkippedInUse), Ids(other.SkippedInUse));
    }

    // GW-U-434 (AC-4, V8): the requirements, containers, assignments and every container are equal to fresh copies after the calls.
    [Fact]
    public void InputsAreEqualToFreshCopiesAfterCompute()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);
        CraftRequirement[] reqs = Requirements();

        BaseAvailability.Compute(snapshot, reqs);
        BaseAvailability.Compute(snapshot, reqs);

        World fresh = Create();
        Assert.Equal(Requirements(), reqs);
        Assert.Equal(fresh.Containers, world.Containers);
        Assert.Equal(fresh.Inventory, world.Inventory);
        Assert.Equal(fresh.C42, world.C42);
        Assert.Equal(fresh.C57, world.C57);
        Assert.Equal(fresh.C63, world.C63);
        Assert.Equal(fresh.C70, world.C70);
        Assert.Equal(fresh.C88, world.C88);
        Assert.Equal(fresh.Assignments.Count, world.Assignments.Count);
        foreach (KeyValuePair<ContainerId, CategoryAssignment> pair in fresh.Assignments)
        {
            Assert.Equal(pair.Value, world.Assignments[pair.Key]);
        }
    }
}
