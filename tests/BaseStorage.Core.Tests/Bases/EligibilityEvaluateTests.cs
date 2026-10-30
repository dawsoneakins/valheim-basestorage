namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.MembershipFixtures;

/// <summary>AC-9, AC-11 (contract 5, 7.3, 7.5): Evaluate over the base A members, column by column, with exact expected lists.</summary>
public sealed class EligibilityEvaluateTests
{
    private static readonly (long UserId, uint Id)[] Column1Eligible = { (10, 1), (10, 3), (10, 9), (10, 12) };

    private static readonly (long UserId, uint Id, ExclusionReason Reason)[] Column1Excluded =
    {
        (10, 4, ExclusionReason.Vehicle),
        (10, 5, ExclusionReason.Tombstone),
        (10, 6, ExclusionReason.AccessDenied),
        (10, 7, ExclusionReason.NotPlayerBuilt),
        (10, 8, ExclusionReason.Keep),
        (10, 10, ExclusionReason.Tombstone),
        (10, 11, ExclusionReason.Vehicle),
    };

    // GW-U-214 (AC-11): column 1, Organize with (true, true), exactly as the contract lists it, with the same instances.
    [Fact]
    public void ColumnOneOrganizeWithBothSettingsOnIsExact()
    {
        EligibilityResult result = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), ContainerPurpose.Organize, new EligibilityOptions(true, true));

        Assert.Equal(Column1Eligible, Ids(result.Eligible));
        Assert.Equal(Column1Excluded, Excl(result));
        AssertSameInstances(new[] { C1, C3, C9, C12 }, result.Eligible);
    }

    // GW-U-215 (AC-11): column 3, CraftAvailability with (true, true), exactly as the contract lists it.
    [Fact]
    public void ColumnThreeCraftWithBothSettingsOnIsExact()
    {
        EligibilityResult result = ContainerEligibility.Evaluate(
            MembersOfA(), Assignments(), ContainerPurpose.CraftAvailability, new EligibilityOptions(true, true));

        Assert.Equal(new (long, uint)[] { (10, 1), (10, 3), (10, 8), (10, 9), (10, 12) }, Ids(result.Eligible));
        Assert.Equal(
            new (long, uint, ExclusionReason)[]
            {
                (10, 4, ExclusionReason.Vehicle),
                (10, 5, ExclusionReason.Tombstone),
                (10, 6, ExclusionReason.AccessDenied),
                (10, 7, ExclusionReason.NotPlayerBuilt),
                (10, 10, ExclusionReason.Tombstone),
                (10, 11, ExclusionReason.Vehicle),
            },
            Excl(result));
    }

    // GW-U-216 (AC-11): columns 4 and 2, both settings off, for CraftAvailability and Organize. The two columns hold the same lists.
    [Theory]
    [InlineData(ContainerPurpose.CraftAvailability)]
    [InlineData(ContainerPurpose.Organize)]
    public void BothSettingsOffGiveTheSameListsForBothPurposes(ContainerPurpose purpose)
    {
        EligibilityResult result = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), purpose, new EligibilityOptions(false, false));

        Assert.Equal(new (long, uint)[] { (10, 1), (10, 3), (10, 4), (10, 9), (10, 12) }, Ids(result.Eligible));
        Assert.Equal(
            new (long, uint, ExclusionReason)[]
            {
                (10, 5, ExclusionReason.Tombstone),
                (10, 6, ExclusionReason.AccessDenied),
                (10, 7, ExclusionReason.NotPlayerBuilt),
                (10, 8, ExclusionReason.Keep),
                (10, 10, ExclusionReason.Tombstone),
                (10, 11, ExclusionReason.NotPlayerBuilt),
            },
            Excl(result));
    }

    // GW-U-217 (AC-11, M8): E-1 (PB) and E-11 (PB plus InUseByOther) are eligible for both purposes and all four option pairs.
    [Theory]
    [InlineData(ContainerPurpose.Organize, true, true)]
    [InlineData(ContainerPurpose.Organize, true, false)]
    [InlineData(ContainerPurpose.Organize, false, true)]
    [InlineData(ContainerPurpose.Organize, false, false)]
    [InlineData(ContainerPurpose.CraftAvailability, true, true)]
    [InlineData(ContainerPurpose.CraftAvailability, true, false)]
    [InlineData(ContainerPurpose.CraftAvailability, false, true)]
    [InlineData(ContainerPurpose.CraftAvailability, false, false)]
    public void InUseByOtherDoesNotExcludeAPlayerBuiltContainer(ContainerPurpose purpose, bool excludeVehicles, bool keepChestsCraftable)
    {
        var options = new EligibilityOptions(excludeVehicles, keepChestsCraftable);

        Assert.Null(ContainerEligibility.GetExclusionReason(Container(ContainerFlags.PlayerBuilt), false, purpose, options));
        Assert.Null(ContainerEligibility.GetExclusionReason(
            Container(ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther), false, purpose, options));

        EligibilityResult result = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), purpose, options);
        Assert.Contains(C9, result.Eligible);
        Assert.Contains(C1, result.Eligible);
        Assert.Contains(C3, result.Eligible);
        Assert.Contains(C12, result.Eligible);
    }

    // GW-U-218 (AC-11, X-1, X-2): an empty container list gives non-null, empty, read-only lists.
    [Fact]
    public void EmptyContainerListGivesEmptyReadOnlyResults()
    {
        EligibilityResult organize = ContainerEligibility.Evaluate(
            Array.Empty<ContainerSnapshot>(), new Dictionary<ContainerId, CategoryAssignment>(), ContainerPurpose.Organize, new EligibilityOptions(true, true));
        EligibilityResult craft = ContainerEligibility.Evaluate(
            Array.Empty<ContainerSnapshot>(), Assignments(), ContainerPurpose.CraftAvailability, default);

        foreach (EligibilityResult result in new[] { organize, craft })
        {
            Assert.NotNull(result.Eligible);
            Assert.NotNull(result.Excluded);
            Assert.IsType<ReadOnlyCollection<ContainerSnapshot>>(result.Eligible);
            Assert.IsType<ReadOnlyCollection<ContainerExclusion>>(result.Excluded);
            Assert.Empty(result.Eligible);
            Assert.Empty(result.Excluded);
        }
    }

    // GW-U-219 (AC-11, X-3, M5, M6): the first snapshot per id decides, and column 1 is deterministic under input order, repetition and dictionary order.
    [Fact]
    public void FirstSnapshotPerIdDecidesAndTheResultIsDeterministic()
    {
        EligibilityResult near = ContainerEligibility.Evaluate(
            new[] { C1, C1Far }, new Dictionary<ContainerId, CategoryAssignment>(), ContainerPurpose.Organize, new EligibilityOptions(true, true));
        AssertSameInstances(new[] { C1 }, near.Eligible);
        Assert.Empty(near.Excluded);

        ContainerSnapshot[] reversed = Enumerable.Reverse(MembersOfA()).ToArray();
        ContainerSnapshot[] permuted = { C12, C4, C9, C1, C10, C6, C3, C11, C8, C5, C7 };
        var options = new EligibilityOptions(true, true);

        EligibilityResult fromReversed = ContainerEligibility.Evaluate(reversed, Assignments(), ContainerPurpose.Organize, options);
        EligibilityResult fromPermuted = ContainerEligibility.Evaluate(permuted, Assignments(), ContainerPurpose.Organize, options);
        EligibilityResult again = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), ContainerPurpose.Organize, options);
        EligibilityResult reversedDictionary = ContainerEligibility.Evaluate(MembersOfA(), AssignmentsReversed(), ContainerPurpose.Organize, options);

        foreach (EligibilityResult result in new[] { fromReversed, fromPermuted, again, reversedDictionary })
        {
            Assert.Equal(Column1Eligible, Ids(result.Eligible));
            Assert.Equal(Column1Excluded, Excl(result));
        }
    }
}
