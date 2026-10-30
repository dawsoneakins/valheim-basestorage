namespace BaseStorage.Core.Tests.Availability;

using BaseStorage.Core.Availability;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;

/// <summary>AC-3 (contract 9.6 C7, C8 and the 9.7 requirement rows A1-A5): several requirement types in one request.</summary>
public sealed class AvailabilityRequestTests
{
    // GW-U-420 (AC-3, 9.7 A1-A5): each requirement is answered in input order (R5), and AllMet is false.
    [Fact]
    public void EachRequirementTypeIsAnsweredInInputOrder()
    {
        World world = Create();
        CraftRequirement[] reqs = Requirements();

        AvailabilityResult result = BaseAvailability.Compute(Snapshot(world, true), reqs);

        RequirementAvailability[] expected =
        {
            new RequirementAvailability(new CraftRequirement("Wood", 60), 12, 57),
            new RequirementAvailability(new CraftRequirement("Resin", 5), 5, 0),
            new RequirementAvailability(new CraftRequirement("LeatherScraps", 15), 3, 10),
            new RequirementAvailability(new CraftRequirement("CopperOre", 31), 0, 30),
            new RequirementAvailability(new CraftRequirement("Bronze", 2), 0, 0),
        };
        Assert.Equal(expected, result.Requirements);
        for (int i = 0; i < reqs.Length; i++)
        {
            Assert.Equal(reqs[i], result.Requirements[i].Requirement);
        }

        Assert.False(result.AllMet);
    }
}
