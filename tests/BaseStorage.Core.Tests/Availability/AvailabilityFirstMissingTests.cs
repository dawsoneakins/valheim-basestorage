namespace BaseStorage.Core.Tests.Availability;

using System;
using System.Linq;
using BaseStorage.Core.Availability;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;

/// <summary>AC-3 (contract 9.6 C7, C8, 9.7 rows F1-F5): the first missing requirement, and zero or repeated requirements.</summary>
public sealed class AvailabilityFirstMissingTests
{
    // GW-U-423 (AC-3, 9.7 F1): the first missing requirement is LeatherScraps (index 2), not CopperOre or Bronze.
    [Fact]
    public void FirstMissingIsTheLowestIndexUnmetRequirement()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);

        RequirementAvailability? first = BaseAvailability.FindFirstMissing(snapshot, Requirements());

        Assert.Equal(new RequirementAvailability(new CraftRequirement("LeatherScraps", 15), 3, 10), first);
        Assert.Equal(BaseAvailability.Compute(snapshot, Requirements()).FirstMissing, first);
    }

    // GW-U-423 (AC-3, 9.7 F2): when every requirement is exactly met, FindFirstMissing is null and AllMet is true.
    [Fact]
    public void ExactlyMetRequirementsHaveNoFirstMissing()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);
        CraftRequirement[] reqs = { new CraftRequirement("Wood", 69), new CraftRequirement("Resin", 5) };

        RequirementAvailability? first = BaseAvailability.FindFirstMissing(snapshot, reqs);

        Assert.Null(first);
        Assert.True(BaseAvailability.Compute(snapshot, reqs).AllMet);
        Assert.Equal(BaseAvailability.Compute(snapshot, reqs).FirstMissing, first);
    }

    // GW-U-423 (AC-3, 9.7 F3): an empty request has no requirements, no first missing, and is all met.
    [Fact]
    public void EmptyRequestIsAllMet()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);
        CraftRequirement[] empty = Array.Empty<CraftRequirement>();

        AvailabilityResult result = BaseAvailability.Compute(snapshot, empty);

        Assert.Empty(result.Requirements);
        Assert.Null(result.FirstMissing);
        Assert.True(result.AllMet);
        Assert.Null(BaseAvailability.FindFirstMissing(snapshot, empty));
    }

    // GW-U-423 (AC-3, 9.7 F4): a zero-amount Wood line is met, so the first missing one is the Bronze row (0, 0).
    [Fact]
    public void ZeroAmountLineIsSkippedWhenFindingTheFirstMissing()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);
        CraftRequirement[] reqs = { CraftRequirement.ForLevel("Wood", 2, 0, 2, 1), new CraftRequirement("Bronze", 2) };

        RequirementAvailability? first = BaseAvailability.FindFirstMissing(snapshot, reqs);

        Assert.Equal(new RequirementAvailability(new CraftRequirement("Bronze", 2), 0, 0), first);
        Assert.Equal(BaseAvailability.Compute(snapshot, reqs).FirstMissing, first);
    }

    // GW-U-424 (AC-3, 9.7 F5, C7): two Wood 60 requirements do not deplete each other, so both see 12 / 57 / 69 and both are met.
    [Fact]
    public void RepeatedRequirementEachSeesTheFullPool()
    {
        World world = Create();
        CraftRequirement[] reqs = { new CraftRequirement("Wood", 60), new CraftRequirement("Wood", 60) };

        AvailabilityResult result = BaseAvailability.Compute(Snapshot(world, true), reqs);

        Assert.Equal(
            new (int InInventory, int InContainers, int Total, bool IsMet, int Missing)[]
            {
                (12, 57, 69, true, 0),
                (12, 57, 69, true, 0),
            },
            result.Requirements.Select(Counts).ToArray());
    }

    // GW-U-424 (AC-3, C8): a zero amount of an absent prefab is 0 / 0, met, Missing 0, and is never FirstMissing.
    [Fact]
    public void ZeroAmountIsAlwaysMetAndNeverFirstMissing()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);
        CraftRequirement[] reqs = { new CraftRequirement("Silver", 0), new CraftRequirement("Bronze", 2) };

        AvailabilityResult result = BaseAvailability.Compute(snapshot, reqs);

        Assert.Equal((0, 0, 0, true, 0), Counts(result.Requirements[0]));
        Assert.Equal(new RequirementAvailability(new CraftRequirement("Bronze", 2), 0, 0), result.FirstMissing);
        Assert.Null(BaseAvailability.FindFirstMissing(snapshot, new[] { new CraftRequirement("Silver", 0) }));
    }
}
