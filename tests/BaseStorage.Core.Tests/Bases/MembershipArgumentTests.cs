namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.MembershipFixtures;

/// <summary>AC-12 (ADR-GWIN-022, contract M2, M3, M4, X-4..X-15): argument rules, pinned check order, and no mutation.</summary>
public sealed class MembershipArgumentTests
{
    // GW-U-221 (AC-12, M3, X-4): a null stations list.
    [Fact]
    public void NullStationsListThrowsArgumentNullForStations()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            BaseMembership.FindMembers(null!, Array.Empty<ContainerSnapshot>());
        });

        Assert.Equal("stations", ex.ParamName);
    }

    // GW-U-221 (AC-12, M3, X-5): a null containers list.
    [Fact]
    public void NullContainersListThrowsArgumentNullForContainers()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            BaseMembership.FindMembers(Array.Empty<StationSnapshot>(), null!);
        });

        Assert.Equal("containers", ex.ParamName);
    }

    // GW-U-221 (AC-12, M3, X-6): a null station element.
    [Fact]
    public void NullStationElementThrowsArgumentExceptionForStations()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            BaseMembership.FindMembers(new StationSnapshot[] { SA1, null! }, Array.Empty<ContainerSnapshot>());
        });

        Assert.Equal("stations", ex.ParamName);
    }

    // GW-U-221 (AC-12, M3, X-7): a null container element.
    [Fact]
    public void NullContainerElementThrowsArgumentExceptionForContainers()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            BaseMembership.FindMembers(Array.Empty<StationSnapshot>(), new ContainerSnapshot[] { C1, null! });
        });

        Assert.Equal("containers", ex.ParamName);
    }

    // GW-U-221 (ADR-GWIN-022 pinned order): stations is checked before containers, so a null station element wins over a null list.
    [Fact]
    public void StationElementIsCheckedBeforeContainersNull()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            BaseMembership.FindMembers(new StationSnapshot[] { null! }, null!);
        });

        Assert.Equal("stations", ex.ParamName);
    }

    // GW-U-221 (ADR-GWIN-022 pinned order): a duplicate does not stop the scan, so a later null element still throws.
    [Fact]
    public void NullElementAfterADuplicateStillThrows()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            BaseMembership.FindMembers(new[] { SA1 }, new ContainerSnapshot[] { C1, C1Far, null! });
        });

        Assert.Equal("containers", ex.ParamName);
    }

    // GW-U-222 (AC-12, M3, X-8): a null containers list to Evaluate.
    [Fact]
    public void EvaluateNullContainersThrowsArgumentNullForContainers()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            ContainerEligibility.Evaluate(null!, new Dictionary<ContainerId, CategoryAssignment>(), ContainerPurpose.Organize, default);
        });

        Assert.Equal("containers", ex.ParamName);
    }

    // GW-U-222 (AC-12, M3, X-9): a null assignments dictionary to Evaluate.
    [Fact]
    public void EvaluateNullAssignmentsThrowsArgumentNullForAssignments()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            ContainerEligibility.Evaluate(Array.Empty<ContainerSnapshot>(), null!, ContainerPurpose.Organize, default);
        });

        Assert.Equal("assignments", ex.ParamName);
    }

    // GW-U-222 (AC-12, M3, X-10): a null value in the assignments dictionary, under a key that is not in the list.
    [Fact]
    public void EvaluateNullAssignmentValueThrowsArgumentExceptionForAssignments()
    {
        var assignments = new Dictionary<ContainerId, CategoryAssignment> { [new ContainerId(99, 99)] = null! };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            ContainerEligibility.Evaluate(new[] { C1 }, assignments, ContainerPurpose.Organize, default);
        });

        Assert.Equal("assignments", ex.ParamName);
    }

    // GW-U-222 (AC-12, M4, X-11): an undefined purpose to Evaluate, above and below the defined range.
    [Theory]
    [InlineData(7)]
    [InlineData(-1)]
    public void EvaluateUndefinedPurposeThrowsArgumentOutOfRangeForPurpose(int raw)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            ContainerEligibility.Evaluate(
                Array.Empty<ContainerSnapshot>(), new Dictionary<ContainerId, CategoryAssignment>(), (ContainerPurpose)raw, default);
        });

        Assert.Equal("purpose", ex.ParamName);
    }

    // GW-U-222 (AC-12, M3, X-12): a null container to GetExclusionReason.
    [Fact]
    public void GetExclusionReasonNullContainerThrowsArgumentNullForContainer()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            ContainerEligibility.GetExclusionReason(null!, false, ContainerPurpose.Organize, default);
        });

        Assert.Equal("container", ex.ParamName);
    }

    // GW-U-222 (AC-12, M4, X-13): an undefined purpose to GetExclusionReason.
    [Fact]
    public void GetExclusionReasonUndefinedPurposeThrowsArgumentOutOfRangeForPurpose()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            ContainerEligibility.GetExclusionReason(C1, false, (ContainerPurpose)(-1), default);
        });

        Assert.Equal("purpose", ex.ParamName);
    }

    // GW-U-222 (ADR-GWIN-022 pinned order): the container null check comes before the purpose check.
    [Fact]
    public void GetExclusionReasonChecksContainerBeforePurpose()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            ContainerEligibility.GetExclusionReason(null!, false, (ContainerPurpose)7, default);
        });

        Assert.Equal("container", ex.ParamName);
    }

    // GW-U-222 (ADR-GWIN-022 pinned order): a null element beats an undefined purpose and a null assignments dictionary.
    [Fact]
    public void EvaluateNullElementIsCheckedBeforePurposeAndAssignments()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            ContainerEligibility.Evaluate(new ContainerSnapshot[] { null! }, null!, (ContainerPurpose)7, default);
        });

        Assert.Equal("containers", ex.ParamName);
    }

    // GW-U-222 (ADR-GWIN-022 pinned order): a null assignments dictionary beats an undefined purpose.
    [Fact]
    public void EvaluateNullAssignmentsIsCheckedBeforePurpose()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            ContainerEligibility.Evaluate(Array.Empty<ContainerSnapshot>(), null!, (ContainerPurpose)7, default);
        });

        Assert.Equal("assignments", ex.ParamName);
    }

    // GW-U-223 (AC-12, M2, X-15): FindMembers and Evaluate leave their inputs unchanged and return fresh read-only lists.
    [Fact]
    public void FindMembersAndEvaluateDoNotMutateTheirInputs()
    {
        StationSnapshot[] stations = { SA1, SA2 };
        ContainerSnapshot[] containers = All;
        Dictionary<ContainerId, CategoryAssignment> assignments = Assignments();
        int assignmentCount = assignments.Count;

        IReadOnlyList<ContainerSnapshot> members = BaseMembership.FindMembers(stations, containers);
        EligibilityResult result = ContainerEligibility.Evaluate(members, assignments, ContainerPurpose.Organize, new EligibilityOptions(true, true));

        Assert.Equal(2, stations.Length);
        Assert.Same(SA1, stations[0]);
        Assert.Same(SA2, stations[1]);
        Assert.Equal(13, containers.Length);
        for (int i = 0; i < All.Length; i++)
        {
            Assert.Same(All[i], containers[i]);
        }

        Assert.Equal(assignmentCount, assignments.Count);
        Assert.Equal(CategoryAssignment.Keep, assignments[C8.Id]);
        Assert.Equal(CategoryAssignment.Keep, assignments[C10.Id]);
        Assert.Equal(CategoryAssignment.Keep, assignments[C11.Id]);
        Assert.Equal(CategoryAssignment.ForCategory("Ores"), assignments[C12.Id]);
        Assert.Equal(CategoryAssignment.Keep, assignments[new ContainerId(99, 99)]);

        Assert.IsType<ReadOnlyCollection<ContainerSnapshot>>(members);
        Assert.NotSame(containers, members);
        Assert.Throws<NotSupportedException>(() =>
        {
            ((IList<ContainerSnapshot>)members).Add(C1);
        });

        Assert.IsType<ReadOnlyCollection<ContainerSnapshot>>(result.Eligible);
        Assert.IsType<ReadOnlyCollection<ContainerExclusion>>(result.Excluded);
        Assert.Throws<NotSupportedException>(() =>
        {
            ((IList<ContainerSnapshot>)result.Eligible).Add(C1);
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            ((IList<ContainerExclusion>)result.Excluded).Clear();
        });
    }
}
