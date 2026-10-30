namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Linq;
using BaseStorage.Core.Bases.Fill;
using BaseStorage.Core.Snapshots;

/// <summary>
/// GW-U-2244..2245 (AC-5, base-fill contract 3.1 range, BF5 and BF7): a sweep over every small count pair, checked against an independent
/// decimal reference (ADR-GWIN-157), and the non-empty base boundaries. Compile-red until BaseFill exists.
/// </summary>
public sealed class BaseFillRangeTests
{
    // GW-U-2244 (AC-5, BF5, BF7, 3.1 range property): for every t in 1..400 and o in 0..t, the value stays in 0..100, is 0 when o is 0,
    // is 100 when o equals t, never decreases as o grows, and equals the decimal reference with a half away from zero.
    [Fact]
    public void PercentStaysInRangeAndNeverDecreases()
    {
        for (int t = 1; t <= 400; t++)
        {
            int previous = -1;
            for (int o = 0; o <= t; o++)
            {
                int percent = new BaseFill(o, t).Percent;

                Assert.InRange(percent, 0, 100);
                if (o == 0)
                {
                    Assert.Equal(0, percent);
                }

                if (o == t)
                {
                    Assert.Equal(100, percent);
                }

                if (percent < previous)
                {
                    Assert.Fail("Percent fell from " + previous + " to " + percent + " at " + o + "/" + t + ".");
                }

                Assert.Equal((int)Math.Round(100m * o / t, MidpointRounding.AwayFromZero), percent);
                previous = percent;
            }
        }
    }

    // GW-U-2245 (AC-5, BF7 consequences): 1 stack in 1000 slots shows 0% full, and 999 stacks in 1000 slots shows 100% full. Both stay in 0..100.
    [Fact]
    public void ANonEmptyBaseBelowHalfAPercentShowsZeroAndANearlyFullBaseShowsOneHundred()
    {
        ContainerSnapshot[] nearlyEmpty = Enumerable.Range(1, 100).Select(serial => Chest((uint)serial, 5, 2, serial == 100 ? 1 : 0)).ToArray();
        BaseFill low = BaseFill.FromContainers(nearlyEmpty);
        Assert.Equal((1, 1000, 0), (low.OccupiedSlots, low.TotalSlots, low.Percent));

        ContainerSnapshot[] nearlyFull = Enumerable.Range(1, 100).Select(serial => Chest((uint)serial, 5, 2, serial <= 99 ? 10 : 9)).ToArray();
        BaseFill high = BaseFill.FromContainers(nearlyFull);
        Assert.Equal((999, 1000, 100), (high.OccupiedSlots, high.TotalSlots, high.Percent));

        Assert.InRange(low.Percent, 0, 100);
        Assert.InRange(high.Percent, 0, 100);
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
