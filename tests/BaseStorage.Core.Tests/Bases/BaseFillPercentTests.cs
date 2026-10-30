namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Linq;
using BaseStorage.Core.Bases.Fill;
using BaseStorage.Core.Snapshots;

/// <summary>
/// GW-U-2240..2243 (AC-3..AC-6, base-fill contract 3.1, 3.3, BF7 and BF8): the percentage of a constructed value and of a sequence of
/// containers. Zero slots, an empty base, a full base and the rounding table. Compile-red until BaseFill exists (ADR-GWIN-157).
/// </summary>
public sealed class BaseFillPercentTests
{
    // GW-U-2240 (AC-6, BF8, 3.1 row 0/0, 3.3 row 1): zero eligible slots give 0 without dividing; the default value is (0, 0).
    [Fact]
    public void ZeroSlotsGiveZeroPercentWithoutDividing()
    {
        Assert.Equal(0, new BaseFill(0, 0).Percent);

        BaseFill none = default;
        Assert.Equal(0, none.OccupiedSlots);
        Assert.Equal(0, none.TotalSlots);
        Assert.Equal(0, none.Percent);
        Assert.Equal(new BaseFill(0, 0), none);
        Assert.Equal(new BaseFill(0, 0).GetHashCode(), none.GetHashCode());

        BaseFill fromNone = BaseFill.FromContainers(new ContainerSnapshot[0]);
        Assert.Equal((0, 0, 0), (fromNone.OccupiedSlots, fromNone.TotalSlots, fromNone.Percent));
    }

    // GW-U-2241 (AC-3, 3.1 row 0/30, 3.3 rows 2 and 1-chest empty): an empty base is 0% full.
    [Fact]
    public void AnEmptyBaseIsZeroPercentFull()
    {
        Assert.Equal(0, new BaseFill(0, 30).Percent);

        BaseFill one = BaseFill.FromContainers(new[] { Chest(1, 5, 2, 0) });
        Assert.Equal((0, 10, 0), (one.OccupiedSlots, one.TotalSlots, one.Percent));

        BaseFill three = BaseFill.FromContainers(new[] { Chest(1, 5, 2, 0), Chest(2, 5, 2, 0), Chest(3, 5, 2, 0) });
        Assert.Equal((0, 30, 0), (three.OccupiedSlots, three.TotalSlots, three.Percent));
    }

    // GW-U-2242 (AC-4, 3.1 row 30/30, 3.3 rows 3 and 8): a full base is 100% full, and more stacks than slots is clamped (BF3).
    [Fact]
    public void AFullBaseIsOneHundredPercentFull()
    {
        Assert.Equal(100, new BaseFill(30, 30).Percent);

        BaseFill one = BaseFill.FromContainers(new[] { Chest(1, 5, 2, 10) });
        Assert.Equal((10, 10, 100), (one.OccupiedSlots, one.TotalSlots, one.Percent));

        BaseFill clamped = BaseFill.FromContainers(new[] { Chest(1, 5, 2, 12) });
        Assert.Equal((10, 10, 100), (clamped.OccupiedSlots, clamped.TotalSlots, clamped.Percent));

        BaseFill three = BaseFill.FromContainers(new[] { Chest(1, 5, 2, 10), Chest(2, 5, 2, 10), Chest(3, 5, 2, 10) });
        Assert.Equal((30, 30, 100), (three.OccupiedSlots, three.TotalSlots, three.Percent));
    }

    // GW-U-2243 (AC-5, BF7, 3.1): the rounding table, nearest whole percent with an exact half rounding up.
    [Theory]
    [InlineData(1, 3, 33)]
    [InlineData(2, 3, 67)]
    [InlineData(1, 8, 13)]
    [InlineData(3, 200, 2)]
    [InlineData(1, 200, 1)]
    [InlineData(199, 200, 100)]
    [InlineData(1, 1000, 0)]
    [InlineData(999, 1000, 100)]
    [InlineData(1, 30, 3)]
    [InlineData(2, 30, 7)]
    [InlineData(7, 24, 29)]
    [InlineData(263, 410, 64)]
    [InlineData(1, int.MaxValue, 0)]
    [InlineData(int.MaxValue - 1, int.MaxValue, 100)]
    [InlineData(int.MaxValue, int.MaxValue, 100)]
    public void PercentRoundsToTheNearestWholePercentAndHalfUp(int occupied, int total, int expected)
    {
        Assert.Equal(expected, new BaseFill(occupied, total).Percent);
    }

    // GW-U-2243 (AC-5, the spec example): one slot of three is 33% full.
    [Fact]
    public void OneOfThreeIsThirtyThree()
    {
        Assert.Equal(33, new BaseFill(1, 3).Percent);
    }

    private static ContainerSnapshot Chest(uint serial, int width, int height, int stacks) =>
        new ContainerSnapshot(
            new ContainerId(1L, serial),
            new WorldPosition(serial * 2f, 0f, 0f),
            width,
            height,
            Enumerable.Range(0, stacks).Select(_ => new ItemStackSnapshot("Wood", 10, 1, 50)).ToArray(),
            ContainerFlags.PlayerBuilt);
}
