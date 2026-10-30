namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Fill;
using BaseStorage.Core.Snapshots;

/// <summary>
/// GW-U-2246 (AC-2, BF3, BF4, base-fill contract 3.3): FromContainers over the contract table rows, the single-pass sequence and the
/// stack-size rule. Compile-red until BaseFill exists.
/// </summary>
public sealed class BaseFillFromContainersTests
{
    // GW-U-2246 (BF3, BF4, 3.3 rows 4 to 9): the table rows, including the same chest id twice, which is not de-duplicated.
    [Theory]
    [InlineData("two empty chests and one with 1 stack", 1, 30, 3)]
    [InlineData("two chests with 1 stack and one empty chest", 2, 30, 7)]
    [InlineData("a 5 by 2 chest with 10 stacks and a 6 by 4 empty chest", 10, 34, 29)]
    [InlineData("one 1 by 3 chest with 1 stack", 1, 3, 33)]
    [InlineData("the same chest id twice, 3 stacks each", 6, 20, 30)]
    public void FromContainersMatchesTheContractTable(string shape, int occupied, int total, int percent)
    {
        BaseFill fill = BaseFill.FromContainers(Shape(shape));

        Assert.Equal((occupied, total, percent), (fill.OccupiedSlots, fill.TotalSlots, fill.Percent));
    }

    // GW-U-2246 (BF4, 3.3 single-pass row): the sequence is enumerated once. One chest with 4 stacks gives (4, 10, 40); the iterator sees
    // two MoveNext calls, one that yields the chest and one that ends the sequence.
    [Fact]
    public void ASinglePassSequenceIsEnumeratedOnce()
    {
        var source = new CountingSequence(new[] { Chest(1, 5, 2, 4) });

        BaseFill fill = BaseFill.FromContainers(source);

        Assert.Equal((4, 10, 40), (fill.OccupiedSlots, fill.TotalSlots, fill.Percent));
        Assert.Equal(2, source.MoveNextCalls);
    }

    // GW-U-2246 (BF3, 3.3 note): a stack's count never affects the result, so one stack in a 5 by 2 chest is 10% whether it holds 1 or 50 items.
    [Fact]
    public void AOneStackChestGivesTenPercentWhateverTheStackSize()
    {
        BaseFill small = BaseFill.FromContainers(new[] { SingleStack(1) });
        BaseFill large = BaseFill.FromContainers(new[] { SingleStack(50) });

        Assert.Equal(10, small.Percent);
        Assert.Equal(10, large.Percent);
        Assert.Equal((1, 10), (small.OccupiedSlots, small.TotalSlots));
        Assert.Equal((1, 10), (large.OccupiedSlots, large.TotalSlots));
    }

    private static ContainerSnapshot[] Shape(string name) => name switch
    {
        "two empty chests and one with 1 stack" => new[] { Chest(1, 5, 2, 0), Chest(2, 5, 2, 0), Chest(3, 5, 2, 1) },
        "two chests with 1 stack and one empty chest" => new[] { Chest(1, 5, 2, 1), Chest(2, 5, 2, 1), Chest(3, 5, 2, 0) },
        "a 5 by 2 chest with 10 stacks and a 6 by 4 empty chest" => new[] { Chest(1, 5, 2, 10), Chest(2, 6, 4, 0) },
        "one 1 by 3 chest with 1 stack" => new[] { Chest(1, 1, 3, 1) },
        "the same chest id twice, 3 stacks each" => new[] { Chest(1, 5, 2, 3), Chest(1, 5, 2, 3) },
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown shape."),
    };

    private static ContainerSnapshot SingleStack(int count) =>
        new ContainerSnapshot(
            new ContainerId(1L, 1u),
            new WorldPosition(0f, 0f, 0f),
            5,
            2,
            new[] { new ItemStackSnapshot("Wood", count, 1, 50) },
            ContainerFlags.PlayerBuilt);

    private static ContainerSnapshot Chest(uint serial, int width, int height, int stacks) =>
        new ContainerSnapshot(
            new ContainerId(1L, serial),
            new WorldPosition(serial * 2f, 0f, 0f),
            width,
            height,
            Enumerable.Range(0, stacks).Select(_ => new ItemStackSnapshot("Wood", 10, 1, 50)).ToArray(),
            ContainerFlags.PlayerBuilt);

    /// <summary>A sequence that is written as a yield iterator and counts every MoveNext call made on it.</summary>
    private sealed class CountingSequence : IEnumerable<ContainerSnapshot>
    {
        private readonly ContainerSnapshot[] items;

        public CountingSequence(ContainerSnapshot[] items)
        {
            this.items = items;
        }

        /// <summary>The number of MoveNext calls made so far, across every enumeration.</summary>
        public int MoveNextCalls { get; private set; }

        public IEnumerator<ContainerSnapshot> GetEnumerator()
        {
            foreach (ContainerSnapshot item in items)
            {
                MoveNextCalls++;
                yield return item;
            }

            MoveNextCalls++;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
