namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Globalization;
using BaseStorage.Plugin.World;

/// <summary>
/// AC-6 (contract 6.6, ADR-GWIN-030, ADR-GWIN-031): SnapshotMemo, linked from src/BaseStorage.Plugin/World/SnapshotMemo.cs,
/// keeps the snapshot of each live item across completed builds, drops items that were not kept, and allocates nothing once
/// warmed up. The step table is the literal contract 6.6 table.
/// </summary>
public sealed class SnapshotMemoTests
{
    // GW-U-518
    [Fact]
    public void ContractStepTableHoldsAcrossBuildsRestartsAndMisuse()
    {
        var memo = new SnapshotMemo<object, string>();
        var chestA = new object();
        var chestB = new object();
        string snapA = new string('A', 1);
        string snapB = new string('B', 1);

        // Step 1
        memo.Begin();
        Assert.Null(memo.Previous(chestA));

        // Step 2
        memo.Keep(chestA, snapA);
        memo.Keep(chestB, snapB);
        memo.End();
        Assert.Equal(2, memo.Count);

        // Step 3
        memo.Begin();
        Assert.Same(snapA, memo.Previous(chestA));

        // Step 4 (chest B is not kept: unloaded)
        memo.Keep(chestA, snapA);
        memo.End();
        Assert.Equal(1, memo.Count);

        // Step 5
        memo.Begin();
        Assert.Null(memo.Previous(chestB));

        // Step 6 (restart keeps the previous set)
        memo.Begin();
        memo.Keep(chestB, snapB);
        memo.Begin();
        Assert.Same(snapA, memo.Previous(chestA));

        // Step 7 (the first End completes the build, the second has no build)
        memo.End();
        Assert.Throws<InvalidOperationException>(() => memo.End());

        // Step 8
        Assert.Throws<InvalidOperationException>(() => new SnapshotMemo<object, string>().Previous(chestA));
    }

    // GW-U-519
    [Theory]
    [InlineData("item")]
    [InlineData("snapshot")]
    public void KeepRejectsANullArgumentByParameterName(string argument)
    {
        var memo = new SnapshotMemo<object, string>();
        memo.Begin();

        ArgumentNullException ex = argument == "item"
            ? Assert.Throws<ArgumentNullException>(() => memo.Keep(null!, "s"))
            : Assert.Throws<ArgumentNullException>(() => memo.Keep(new object(), null!));

        Assert.Equal(argument, ex.ParamName);
    }

    // GW-U-519
    [Theory]
    [InlineData("Keep")]
    [InlineData("End")]
    public void KeepAndEndOutsideABuildThrowInvalidOperation(string operation)
    {
        var memo = new SnapshotMemo<object, string>();
        Action call = operation == "Keep" ? () => memo.Keep(new object(), "s") : () => memo.End();

        Assert.Throws<InvalidOperationException>(call);
    }

    // GW-U-519
    [Fact]
    public void PreviousOfNullInsideABuildIsNull()
    {
        var memo = new SnapshotMemo<object, string>();
        memo.Begin();

        Assert.Null(memo.Previous(null!));
    }

    // GW-U-519
    [Fact]
    public void ALaterKeepForTheSameItemReplacesTheEarlierOne()
    {
        var memo = new SnapshotMemo<object, string>();
        var item = new object();

        memo.Begin();
        memo.Keep(item, "first");
        memo.Keep(item, "second");
        memo.End();

        Assert.Equal(1, memo.Count);
        memo.Begin();
        Assert.Equal("second", memo.Previous(item));
        memo.End();
    }

    // GW-U-519
    [Fact]
    public void TwoItemsThatCompareEqualKeepSeparateSnapshots()
    {
        var memo = new SnapshotMemo<object, string>();
        var first = new AlwaysEqual();
        var second = new AlwaysEqual();

        memo.Begin();
        memo.Keep(first, "A");
        memo.Keep(second, "B");
        memo.End();

        Assert.Equal(2, memo.Count);
        memo.Begin();
        Assert.Equal("A", memo.Previous(first));
        Assert.Equal("B", memo.Previous(second));
        memo.End();
    }

    // GW-U-519
    [Fact]
    public void ThirdBuildAfterWarmUpAllocatesNothing()
    {
        var memo = new SnapshotMemo<object, string>();
        var items = new object[64];
        var snapshots = new string[64];
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new object();
            snapshots[i] = i.ToString(CultureInfo.InvariantCulture);
        }

        RunBuild(memo, items, snapshots);
        RunBuild(memo, items, snapshots);

        long before = GC.GetAllocatedBytesForCurrentThread();
        RunBuild(memo, items, snapshots);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0L, allocated);
    }

    private static void RunBuild(SnapshotMemo<object, string> memo, object[] items, string[] snapshots)
    {
        memo.Begin();
        for (int i = 0; i < items.Length; i++)
        {
            _ = memo.Previous(items[i]);
            memo.Keep(items[i], snapshots[i]);
        }

        memo.End();
    }

    private sealed class AlwaysEqual
    {
        public override bool Equals(object? obj) => true;

        public override int GetHashCode() => 0;
    }
}
