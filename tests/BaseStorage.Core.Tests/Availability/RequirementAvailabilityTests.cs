namespace BaseStorage.Core.Tests.Availability;

using System;
using BaseStorage.Core.Availability;

/// <summary>AC-3 (contract 9.4, R5, R6, R8): RequirementAvailability validation, derived values, and value equality.</summary>
public sealed class RequirementAvailabilityTests
{
    // GW-U-426 (AC-3, 9.4 R8): a null requirement gives ArgumentNullException with ParamName "requirement".
    [Fact]
    public void NullRequirementIsRejected()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new RequirementAvailability(null!, 0, 0));

        Assert.Equal("requirement", ex.ParamName);
    }

    // GW-U-426 (AC-3, 9.4 R8): a negative inInventory or inContainers gives ArgumentOutOfRangeException with the matching ParamName.
    [Theory]
    [InlineData(-1, 0, "inInventory")]
    [InlineData(0, -1, "inContainers")]
    public void NegativeCountsAreRejectedByParamName(int inInventory, int inContainers, string paramName)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RequirementAvailability(new CraftRequirement("Wood", 60), inInventory, inContainers));

        Assert.Equal(paramName, ex.ParamName);
    }

    // GW-U-426 (AC-3, 9.4 derived): (Wood 60, 12, 57) has Total 69, is met, and has Missing 0.
    [Fact]
    public void MetRequirementHasTotalAndNoMissing()
    {
        var row = new RequirementAvailability(new CraftRequirement("Wood", 60), 12, 57);

        Assert.Equal(69, row.Total);
        Assert.True(row.IsMet);
        Assert.Equal(0, row.Missing);
    }

    // GW-U-426 (AC-3, 9.4 derived): (Wood 60, 0, 0) is not met, and Missing is the whole amount of 60.
    [Fact]
    public void EmptyRequirementIsMissingItsWholeAmount()
    {
        var row = new RequirementAvailability(new CraftRequirement("Wood", 60), 0, 0);

        Assert.Equal(0, row.Total);
        Assert.False(row.IsMet);
        Assert.Equal(60, row.Missing);
    }

    // GW-U-426 (AC-3, 9.4 derived): int.MaxValue on both sides saturates Total at int.MaxValue, and the row is met.
    [Fact]
    public void TotalSaturatesAtIntMaxValue()
    {
        var row = new RequirementAvailability(new CraftRequirement("Wood", 60), int.MaxValue, int.MaxValue);

        Assert.Equal(int.MaxValue, row.Total);
        Assert.True(row.IsMet);
        Assert.Equal(0, row.Missing);
    }

    // GW-U-426 (AC-3, 9.4 R5): equality uses the requirement and both counts only, with equal hashes; the operators are null-safe.
    [Fact]
    public void EqualityUsesTheRequirementAndBothCounts()
    {
        var first = new RequirementAvailability(new CraftRequirement("Wood", 60), 12, 57);
        var same = new RequirementAvailability(new CraftRequirement("Wood", 60), 12, 57);
        RequirementAvailability? none = null;

        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.True(first == same);
        Assert.False(first != same);
        Assert.NotEqual(first, new RequirementAvailability(new CraftRequirement("Wood", 60), 12, 56));
        Assert.NotEqual(first, new RequirementAvailability(new CraftRequirement("Wood", 60), 11, 57));
        Assert.NotEqual(first, new RequirementAvailability(new CraftRequirement("Wood", 61), 12, 57));
        Assert.False(first == none);
        Assert.False(none == first);
        Assert.True(first != none);
        Assert.False(first.Equals((RequirementAvailability?)null));
    }
}
