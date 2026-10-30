namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Linq;
using BaseStorage.Core.Bases.Fill;
using BaseStorage.Core.Snapshots;

/// <summary>
/// GW-U-2247 (AC-2, R8, base-fill contract 2.1, 2.2, 3.2 and 3.3): argument validation with the exact exception type and ParamName, checked
/// in the contract order. Compile-red until BaseFill exists.
/// </summary>
public sealed class BaseFillArgumentTests
{
    // GW-U-2247 (2.1, 3.2): the five constructor rows, including (-1, -1) and (1, 0), which report occupiedSlots.
    [Theory]
    [InlineData(-1, 10, "occupiedSlots")]
    [InlineData(0, -1, "totalSlots")]
    [InlineData(-1, -1, "occupiedSlots")]
    [InlineData(11, 10, "occupiedSlots")]
    [InlineData(1, 0, "occupiedSlots")]
    public void ConstructorRejectsInvalidCountsWithTheParamNameInOrder(int occupied, int total, string paramName)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new BaseFill(occupied, total); });

        Assert.Equal(paramName, ex.ParamName);
    }

    // GW-U-2247 (2.2, 3.3 null row): a null sequence throws ArgumentNullException named containers.
    [Fact]
    public void FromContainersRejectsANullSequenceWithItsName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { _ = BaseFill.FromContainers(null!); });

        Assert.Equal("containers", ex.ParamName);
    }

    // GW-U-2247 (2.2, 3.3 null-element row): a null element throws ArgumentException named containers, not the null-argument subclass.
    [Fact]
    public void FromContainersRejectsANullElementWithItsName()
    {
        ContainerSnapshot[] containers = { Chest(1, 5, 2, 1), null! };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => { _ = BaseFill.FromContainers(containers); });

        Assert.Equal("containers", ex.ParamName);
    }

    // GW-U-2247 (2.2, 3.3 overflow row): 32769 chests of 256 by 256 slots total more than int.MaxValue, so the call throws named containers.
    [Fact]
    public void FromContainersRejectsATotalAboveIntMaxValueWithItsName()
    {
        ContainerSnapshot[] huge = Enumerable.Range(1, 32769)
            .Select(serial => new ContainerSnapshot(
                new ContainerId(1L, (uint)serial),
                new WorldPosition(0f, 0f, 0f),
                256,
                256,
                Array.Empty<ItemStackSnapshot>(),
                ContainerFlags.PlayerBuilt))
            .ToArray();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => { _ = BaseFill.FromContainers(huge); });

        Assert.Equal("containers", ex.ParamName);
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
