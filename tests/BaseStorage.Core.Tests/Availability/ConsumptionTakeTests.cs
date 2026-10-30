namespace BaseStorage.Core.Tests.Availability;

using System;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;

/// <summary>AC-11 and AC-15 (ADR-GWIN-028, ConsumptionTake): argument validation, value equality and the null-safe operators.</summary>
public sealed class ConsumptionTakeTests
{
    // GW-U-458 (AC-11): a default container id is an ArgumentException that names "containerId".
    [Fact]
    public void DefaultContainerIdIsRejected()
    {
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => new ConsumptionTake(default, 0, Stack("Wood", 5, 1, 50), 1, 0));

        Assert.Equal("containerId", thrown.ParamName);
    }

    // GW-U-458 (AC-11): a negative stack index is an ArgumentOutOfRangeException that names "stackIndex".
    [Fact]
    public void NegativeStackIndexIsRejected()
    {
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConsumptionTake(Id(1), -1, Stack("Wood", 5, 1, 50), 1, 0));

        Assert.Equal("stackIndex", thrown.ParamName);
    }

    // GW-U-458 (AC-11): a null stack is an ArgumentNullException that names "stack".
    [Fact]
    public void NullStackIsRejected()
    {
        ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(
            () => new ConsumptionTake(Id(1), 0, null!, 1, 0));

        Assert.Equal("stack", thrown.ParamName);
    }

    // GW-U-458 (AC-11): a count of 0, or more than the stack holds (6 of 5), is an ArgumentOutOfRangeException that names "count".
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void CountOutsideOneToTheStackCountIsRejected(int count)
    {
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConsumptionTake(Id(1), 0, Stack("Wood", 5, 1, 50), count, 0));

        Assert.Equal("count", thrown.ParamName);
    }

    // GW-U-458 (AC-11): a count equal to the stack count is valid, and is the value that is read back.
    [Fact]
    public void CountEqualToTheStackCountIsValid()
    {
        var take = new ConsumptionTake(Id(1), 0, Stack("Wood", 5, 1, 50), 5, 0);

        Assert.Equal(5, take.Count);
    }

    // GW-U-458 (AC-11): a negative requirement index is an ArgumentOutOfRangeException that names "requirementIndex".
    [Fact]
    public void NegativeRequirementIndexIsRejected()
    {
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConsumptionTake(Id(1), 0, Stack("Wood", 5, 1, 50), 1, -1));

        Assert.Equal("requirementIndex", thrown.ParamName);
    }

    // GW-U-458 (AC-11): two takes built from equal parts are equal, with equal hashes, even when the stack is a different but equal instance.
    [Fact]
    public void EqualPartsGiveEqualTakesAndHashes()
    {
        var first = new ConsumptionTake(Id(5), 1, Stack("Wood", 8, 1, 50), 3, 0);
        var second = new ConsumptionTake(Id(5), 1, Stack("Wood", 8, 1, 50), 3, 0);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first == second);
        Assert.False(first != second);
    }

    // GW-U-458 (AC-11): inequality holds for each field on its own: container id, stack index, stack quality, count and requirement index.
    [Fact]
    public void EachFieldAffectsEquality()
    {
        var baseline = new ConsumptionTake(Id(5), 1, Stack("Wood", 8, 1, 50), 3, 0);

        Assert.NotEqual(baseline, new ConsumptionTake(Id(6), 1, Stack("Wood", 8, 1, 50), 3, 0));
        Assert.NotEqual(baseline, new ConsumptionTake(Id(5), 2, Stack("Wood", 8, 1, 50), 3, 0));
        Assert.NotEqual(baseline, new ConsumptionTake(Id(5), 1, Stack("Wood", 8, 2, 50), 3, 0));
        Assert.NotEqual(baseline, new ConsumptionTake(Id(5), 1, Stack("Wood", 8, 1, 50), 2, 0));
        Assert.NotEqual(baseline, new ConsumptionTake(Id(5), 1, Stack("Wood", 8, 1, 50), 3, 1));
    }

    // GW-U-458 (AC-11): the equality operators are null-safe on both sides, and Equals(null) is false.
    [Fact]
    public void OperatorsAndEqualsAreNullSafe()
    {
        var take = new ConsumptionTake(Id(5), 1, Stack("Wood", 8, 1, 50), 3, 0);
        ConsumptionTake? absent = null;
        ConsumptionTake? alsoAbsent = null;

        Assert.True(absent == alsoAbsent);
        Assert.False(absent != alsoAbsent);
        Assert.False(take == absent);
        Assert.True(take != absent);
        Assert.False(absent == take);
        Assert.True(absent != take);
        Assert.False(take.Equals((ConsumptionTake?)null));
    }

    // GW-U-458 (AC-11): every property returns the value it was built with.
    [Fact]
    public void PropertiesRoundTrip()
    {
        ItemStackSnapshot stack = Stack("Wood", 8, 1, 50);

        var take = new ConsumptionTake(Id(5), 1, stack, 3, 2);

        Assert.Equal(Id(5), take.ContainerId);
        Assert.Equal(1, take.StackIndex);
        Assert.Same(stack, take.Stack);
        Assert.Equal(3, take.Count);
        Assert.Equal(2, take.RequirementIndex);
    }
}
