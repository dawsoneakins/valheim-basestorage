namespace BaseStorage.Core.Tests.Availability;

using System;
using BaseStorage.Core.Availability;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;

/// <summary>AC-3 (contract 9.1, 9.2, 9.6 C5 and the 9.7 rows Q1-Q7): the quality rule, against container 88 (SwordBronze at quality 1 and 3).</summary>
public sealed class AvailabilityQualityTests
{
    // GW-U-421 (AC-3, 9.7 Q1-Q6): each quality rule counts the matching SwordBronze stacks on keepOn.
    [Theory]
    [InlineData(QualityMatch.Any, 2, 1, 2, true, 0)]
    [InlineData(QualityMatch.Exact, 1, 1, 1, true, 0)]
    [InlineData(QualityMatch.Exact, 1, 2, 0, false, 1)]
    [InlineData(QualityMatch.AtLeast, 2, 2, 1, false, 1)]
    [InlineData(QualityMatch.AtLeast, 1, 3, 1, true, 0)]
    [InlineData(QualityMatch.AtLeast, 1, 4, 0, false, 1)]
    public void QualityRuleCountsTheMatchingStacks(QualityMatch match, int amount, int quality, int total, bool met, int missing)
    {
        World world = Create();
        var requirement = new CraftRequirement("SwordBronze", amount, match, quality);

        AvailabilityResult result = BaseAvailability.Compute(Snapshot(world, true), new[] { requirement });

        RequirementAvailability row = result.Requirements[0];
        Assert.Equal(total, row.Total);
        Assert.Equal(met, row.IsMet);
        Assert.Equal(missing, row.Missing);
    }

    // GW-U-421 (AC-3, 9.7 Q7): Any with a quality other than 1 is rejected, because a quality with Any is never meaningful.
    [Fact]
    public void AnyWithQualityOtherThanOneIsRejected()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CraftRequirement("SwordBronze", 1, QualityMatch.Any, 3));

        Assert.Equal("quality", ex.ParamName);
    }
}
