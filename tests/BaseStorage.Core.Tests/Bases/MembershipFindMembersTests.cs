namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.MembershipFixtures;

/// <summary>AC-1, AC-2, AC-11 (contract 6, 7.2, M-1..M-11): which loaded containers belong to a base.</summary>
public sealed class MembershipFindMembersTests
{
    // GW-U-200 (AC-1): members of base A (M-1). C2 and C13 are absent; every flag combination C4..C11 is present.
    [Fact]
    public void MembersOfBaseAAreTheContractM1Containers()
    {
        IReadOnlyList<ContainerSnapshot> result = BaseMembership.FindMembers(new[] { SA1, SA2 }, All);

        Assert.Equal(
            new (long, uint)[] { (10, 1), (10, 3), (10, 4), (10, 5), (10, 6), (10, 7), (10, 8), (10, 9), (10, 10), (10, 11), (10, 12) },
            Ids(result));
        AssertSameInstances(new[] { C1, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12 }, result);
    }

    // GW-U-201 (AC-1): M-4, exactly at the build range (20 m horizontal, Y ignored) is not a member (strict rule).
    [Fact]
    public void ContainerExactlyAtBuildRangeIsNotAMember()
    {
        IReadOnlyList<ContainerSnapshot> result = BaseMembership.FindMembers(new[] { SA1 }, new[] { C2 });

        Assert.Empty(result);
    }

    // GW-U-201 (AC-1): M-5, 19.92 m horizontal with Y = 40 is a member, so Y is ignored.
    [Fact]
    public void ContainerJustInsideBuildRangeIsAMember()
    {
        IReadOnlyList<ContainerSnapshot> result = BaseMembership.FindMembers(new[] { SA1 }, new[] { C2Prime });

        AssertSameInstances(new[] { C2Prime }, result);
    }

    // GW-U-201 (AC-11): the same exact-range rule, as the AC-11 boundary case (M-4).
    [Fact]
    public void ExactRangeBoundaryIsNotAMemberForAnyStation()
    {
        IReadOnlyList<ContainerSnapshot> result = BaseMembership.FindMembers(new[] { SA1, SA2 }, new[] { C2 });

        Assert.Empty(result);
    }

    // GW-U-202 (AC-1): M-6, M-7, M-8. Each no-member case returns an empty, non-null, read-only list.
    [Theory]
    [InlineData("M-6")]
    [InlineData("M-7")]
    [InlineData("M-8")]
    public void NoMemberCasesReturnAnEmptyReadOnlyList(string rowId)
    {
        IReadOnlyList<StationSnapshot> stations;
        IReadOnlyList<ContainerSnapshot> containers;
        switch (rowId)
        {
            case "M-6":
                stations = new[] { SA1, SA2 };
                containers = Array.Empty<ContainerSnapshot>();
                break;
            case "M-7":
                stations = Array.Empty<StationSnapshot>();
                containers = All;
                break;
            case "M-8":
                stations = new[] { SZ };
                containers = All;
                break;
            default:
                throw new InvalidOperationException("Unknown row " + rowId);
        }

        IReadOnlyList<ContainerSnapshot> result = BaseMembership.FindMembers(stations, containers);

        Assert.NotNull(result);
        Assert.IsType<ReadOnlyCollection<ContainerSnapshot>>(result);
        Assert.Empty(result);
    }

    // GW-U-203 (AC-2): C3 is a member of base A and of base B (M-1, M-2), and is the same instance in both.
    [Fact]
    public void ContainerInRangeOfTwoBasesIsAMemberOfEach()
    {
        StationGraphResult baseA = StationGraph.FindBase(new[] { SA1, SA2, SB1, SZ }, SA1.Id, 64);
        StationGraphResult baseB = StationGraph.FindBase(new[] { SA1, SA2, SB1, SZ }, SB1.Id, 64);
        Assert.Equal(new[] { SA1, SA2 }, baseA.Stations);
        Assert.Equal(new[] { SB1 }, baseB.Stations);

        IReadOnlyList<ContainerSnapshot> membersA = BaseMembership.FindMembers(baseA.Stations, All);
        IReadOnlyList<ContainerSnapshot> membersB = BaseMembership.FindMembers(baseB.Stations, All);

        Assert.Equal(
            new (long, uint)[] { (10, 1), (10, 3), (10, 4), (10, 5), (10, 6), (10, 7), (10, 8), (10, 9), (10, 10), (10, 11), (10, 12) },
            Ids(membersA));
        Assert.Equal(new (long, uint)[] { (10, 3), (10, 13) }, Ids(membersB));
        Assert.Same(C3, membersA.Single(c => c.Id == C3.Id));
        Assert.Same(C3, membersB.Single(c => c.Id == C3.Id));

        EligibilityResult eligibleB = ContainerEligibility.Evaluate(
            membersB, Assignments(), ContainerPurpose.Organize, new EligibilityOptions(true, true));
        Assert.Equal(new (long, uint)[] { (10, 3), (10, 13) }, Ids(eligibleB.Eligible));
    }

    // GW-U-204 (AC-1): M-3 and M-11. Input order does not matter, and the first snapshot per id decides.
    [Fact]
    public void ReversedAndPermutedInputGiveTheSameOrderedMembers()
    {
        ContainerSnapshot[] reversed = Enumerable.Reverse(All).ToArray();
        ContainerSnapshot[] permuted = { C10, C3, C12, C1, C8, C5, C13, C2, C7, C4, C11, C6, C9 };
        var expected = new (long, uint)[] { (10, 1), (10, 3), (10, 4), (10, 5), (10, 6), (10, 7), (10, 8), (10, 9), (10, 10), (10, 11), (10, 12) };

        Assert.Equal(expected, Ids(BaseMembership.FindMembers(new[] { SA1, SA2 }, reversed)));
        Assert.Equal(expected, Ids(BaseMembership.FindMembers(new[] { SA1, SA2 }, permuted)));
    }

    // GW-U-204 (AC-1): two consecutive calls over the same inputs give equal sequences.
    [Fact]
    public void RepeatedCallsGiveEqualSequences()
    {
        IReadOnlyList<ContainerSnapshot> first = BaseMembership.FindMembers(new[] { SA1, SA2 }, All);
        IReadOnlyList<ContainerSnapshot> second = BaseMembership.FindMembers(new[] { SA1, SA2 }, All);

        Assert.Equal(Ids(first), Ids(second));
    }

    // GW-U-204 (AC-1): M-9, a duplicate station is harmless.
    [Fact]
    public void DuplicateStationDoesNotChangeTheResult()
    {
        IReadOnlyList<ContainerSnapshot> result = BaseMembership.FindMembers(new[] { SA1, SA1 }, new[] { C1 });

        AssertSameInstances(new[] { C1 }, result);
    }

    // GW-U-204 (AC-1): M-10 and M-11, the first snapshot per id decides, so the far C1Far is ignored in either order.
    [Fact]
    public void FirstSnapshotPerIdDecidesMembership()
    {
        IReadOnlyList<ContainerSnapshot> firstIsNear = BaseMembership.FindMembers(new[] { SA1 }, new[] { C1, C1Far });
        IReadOnlyList<ContainerSnapshot> firstIsFar = BaseMembership.FindMembers(new[] { SA1 }, new[] { C1Far, C1 });

        AssertSameInstances(new[] { C1 }, firstIsNear);
        Assert.Empty(firstIsFar);
    }
}
