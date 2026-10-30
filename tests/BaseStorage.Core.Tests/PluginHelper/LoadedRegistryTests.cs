namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Collections.Generic;
using BaseStorage.Plugin.World;

/// <summary>
/// AC-1, AC-2, AC-3 and AC-6 (contract section 2, ADR-GWIN-031): LoadedRegistry, linked from
/// src/BaseStorage.Plugin/World/LoadedRegistry.cs, is keyed by reference identity, raises one callback per effective
/// change, and copies into a caller buffer. Expected values are the literal contract table and are never computed by the
/// registry under test. The seeded model check uses Random(20261026) (ADR-GWIN-031).
/// </summary>
public sealed class LoadedRegistryTests
{
    // GW-U-500
    [Fact]
    public void ContractWalkThroughStepsOneFourFiveAndTen()
    {
        var events = new List<string>();
        var reg = new LoadedRegistry<object, string>(() => events.Add("added"), () => events.Add("removed"));
        var chestA = new object();
        var chestB = new object();

        // Step 1
        Assert.True(reg.Add(chestA, "chest A"));
        Assert.Equal(1, reg.Count);
        Assert.Equal(1, reg.Version);
        Assert.Equal(new[] { "added" }, events);

        // Step 4
        Assert.True(reg.Add(chestB, "chest B"));
        Assert.Equal(2, reg.Count);
        Assert.Equal(2, reg.Version);
        Assert.Equal(new[] { "added", "added" }, events);

        // Step 5
        Assert.True(reg.Remove(chestA));
        Assert.Equal(1, reg.Count);
        Assert.Equal(3, reg.Version);
        Assert.Equal(new[] { "added", "added", "removed" }, events);

        // Step 10 (re-load)
        Assert.True(reg.Add(chestA, "chest A"));
        Assert.Equal(2, reg.Count);
        Assert.Equal(4, reg.Version);
        Assert.Equal(new[] { "added", "added", "removed", "added" }, events);
    }

    // GW-U-501
    [Fact]
    public void CallbacksSeeTheNewStateBecauseTheStateIsUpdatedFirst()
    {
        var key = new object();
        var onAddedSeen = new List<(int Count, bool Contains)>();
        var onRemovedSeen = new List<(int Count, bool Contains)>();
        LoadedRegistry<object, string> reg = null!;
        reg = new LoadedRegistry<object, string>(
            () => onAddedSeen.Add((reg.Count, reg.Contains(key))),
            () => onRemovedSeen.Add((reg.Count, reg.Contains(key))));

        reg.Add(key, "x");
        reg.Remove(key);

        Assert.Equal(new[] { (1, true) }, onAddedSeen.ToArray());
        Assert.Equal(new[] { (0, false) }, onRemovedSeen.ToArray());
    }

    // GW-U-501
    [Fact]
    public void AThrowingAddCallbackKeepsTheChangeAndPropagatesTheSameException()
    {
        var key = new object();
        var boom = new InvalidOperationException("add failed");
        var reg = new LoadedRegistry<object, string>(() => throw boom, () => { });

        InvalidOperationException caught = Assert.Throws<InvalidOperationException>(() => reg.Add(key, "x"));

        Assert.Same(boom, caught);
        Assert.True(reg.Contains(key));
        Assert.Equal(1, reg.Version);
    }

    // GW-U-501
    [Fact]
    public void AThrowingRemoveCallbackKeepsTheChangeAndPropagatesTheSameException()
    {
        var key = new object();
        var boom = new InvalidOperationException("remove failed");
        var reg = new LoadedRegistry<object, string>(() => { }, () => throw boom);
        reg.Add(key, "x");

        InvalidOperationException caught = Assert.Throws<InvalidOperationException>(() => reg.Remove(key));

        Assert.Same(boom, caught);
        Assert.False(reg.Contains(key));
        Assert.Equal(2, reg.Version);
    }

    // GW-U-502
    [Fact]
    public void NullOnAddedIsRejectedByParameterName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new LoadedRegistry<object, string>(null!, () => { }));

        Assert.Equal("onAdded", ex.ParamName);
    }

    // GW-U-502
    [Fact]
    public void NullOnRemovedIsRejectedByParameterName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new LoadedRegistry<object, string>(() => { }, null!));

        Assert.Equal("onRemoved", ex.ParamName);
    }

    // GW-U-502
    [Fact]
    public void AValidRegistryStartsEmptyAndCopiesNothingIntoTheBuffer()
    {
        var reg = new LoadedRegistry<object, string>(() => { }, () => { });
        var buffer = new List<string> { "stale" };

        Assert.Equal(0, reg.Count);
        Assert.Equal(0, reg.Version);
        reg.CopyItemsTo(buffer);

        Assert.Empty(buffer);
    }

    // GW-U-506
    [Fact]
    public void AStationRegistryRaisesOneEventPerPlaceAndRemove()
    {
        int added = 0;
        int removed = 0;
        var station = new object();
        var reg = new LoadedRegistry<object, object>(() => added++, () => removed++);

        Assert.True(reg.Add(station, station));
        Assert.Equal(1, added);
        Assert.Same(station, reg.Find(station));

        Assert.True(reg.Remove(station));
        Assert.Equal(1, removed);
    }

    // GW-U-506
    [Fact]
    public void AnInterleavedSecondStationKeepsExactCountsAndTheExactLiveSet()
    {
        int added = 0;
        int removed = 0;
        var first = new object();
        var second = new object();
        var reg = new LoadedRegistry<object, object>(() => added++, () => removed++);

        Assert.True(reg.Add(first, first));
        Assert.True(reg.Add(second, second));
        Assert.True(reg.Remove(first));
        Assert.True(reg.Add(first, first));
        Assert.True(reg.Remove(second));

        Assert.Equal(3, added);
        Assert.Equal(2, removed);
        Assert.Equal(1, reg.Count);
        var live = new List<object>();
        reg.CopyItemsTo(live);
        Assert.Equal(new[] { first }, live.ToArray());
    }

    // GW-U-509
    [Theory]
    [InlineData("duplicate Awake (step 2)")]
    [InlineData("duplicate item (step 3)")]
    [InlineData("duplicate destroy (step 6)")]
    [InlineData("unrelated destroy (step 7)")]
    [InlineData("null key on Add (step 8)")]
    [InlineData("null item on Add (step 8)")]
    [InlineData("null key on Remove (step 8)")]
    public void DuplicateAndNoOpCallsChangeNothing(string step)
    {
        var events = new List<string>();
        var chestA = new object();
        var chestB = new object();
        var reg = new LoadedRegistry<object, string>(() => events.Add("added"), () => events.Add("removed"));
        Replay(reg, chestA, chestB, PrecedingStepCount(step));

        int count = reg.Count;
        int version = reg.Version;
        string[] eventsBefore = events.ToArray();
        string? findBefore = reg.Find(chestA);

        Assert.False(Call(reg, step, chestA, chestB));
        Assert.Equal(count, reg.Count);
        Assert.Equal(version, reg.Version);
        Assert.Equal(eventsBefore, events.ToArray());
        Assert.Same(findBefore, reg.Find(chestA));
        if (step == "duplicate item (step 3)")
        {
            Assert.Equal("chest A", reg.Find(chestA));
        }
    }

    // GW-U-510
    [Fact]
    public void ObjectsThatCompareEqualAreStillTwoDistinctKeys()
    {
        var reg = new LoadedRegistry<object, string>(() => { }, () => { });

        Assert.True(reg.Add(new AlwaysEqual(), "a"));
        Assert.True(reg.Add(new AlwaysEqual(), "b"));

        Assert.Equal(2, reg.Count);
    }

    // GW-U-510
    [Fact]
    public void ContainsAndFindOfNullAreFalseAndNull()
    {
        var reg = new LoadedRegistry<object, string>(() => { }, () => { });
        reg.Add(new AlwaysEqual(), "a");

        Assert.False(reg.Contains(null!));
        Assert.Null(reg.Find(null!));
    }

    // GW-U-510
    [Fact]
    public void TheSameInstanceAddedTwiceIsRejectedTheSecondTime()
    {
        var reg = new LoadedRegistry<object, string>(() => { }, () => { });
        var same = new AlwaysEqual();

        Assert.True(reg.Add(same, "first"));
        Assert.False(reg.Add(same, "second"));
        Assert.Equal("first", reg.Find(same));
    }

    // GW-U-511
    [Fact]
    public void SeededModelCheckAgreesWithAReferenceSetAcrossFiveThousandOperations()
    {
        var random = new Random(20261026);
        var keys = new object[8];
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i] = new object();
        }

        int addEvents = 0;
        int removeEvents = 0;
        var reg = new LoadedRegistry<object, string>(() => addEvents++, () => removeEvents++);
        var model = new HashSet<object>(ReferenceEqualityComparer.Instance);
        int expectedAdds = 0;
        int expectedRemoves = 0;

        for (int operation = 0; operation < 5000; operation++)
        {
            object key = keys[random.Next(keys.Length)];
            bool isAdd = random.NextDouble() < 0.6;

            bool changed;
            bool actual;
            if (isAdd)
            {
                changed = model.Add(key);
                actual = reg.Add(key, "item");
                if (changed)
                {
                    expectedAdds++;
                }
            }
            else
            {
                changed = model.Remove(key);
                actual = reg.Remove(key);
                if (changed)
                {
                    expectedRemoves++;
                }
            }

            Assert.Equal(changed, actual);
            Assert.Equal(model.Count, reg.Count);
            foreach (object candidate in keys)
            {
                Assert.Equal(model.Contains(candidate), reg.Contains(candidate));
            }

            Assert.Equal(expectedAdds + expectedRemoves, reg.Version);
            Assert.Equal(expectedAdds, addEvents);
            Assert.Equal(expectedRemoves, removeEvents);
        }
    }

    // GW-U-517
    [Fact]
    public void CopyItemsToClearsTheBufferAndHoldsOnlyTheLiveItems()
    {
        var reg = new LoadedRegistry<object, string>(() => { }, () => { });
        var chestA = new object();
        var chestB = new object();
        reg.Add(chestA, "chest A");
        reg.Add(chestB, "chest B");
        reg.Remove(chestA);
        var buffer = new List<string> { "stale" };

        reg.CopyItemsTo(buffer);

        Assert.Equal(new[] { "chest B" }, buffer.ToArray());
    }

    // GW-U-517
    [Fact]
    public void CopyItemsToRejectsANullBufferByParameterName()
    {
        var reg = new LoadedRegistry<object, string>(() => { }, () => { });

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => reg.CopyItemsTo(null!));

        Assert.Equal("buffer", ex.ParamName);
    }

    // GW-U-517
    [Fact]
    public void SteadyStateCallsAllocateNothingWhenTheBufferHasCapacity()
    {
        var reg = new LoadedRegistry<object, string>(() => { }, () => { });
        var keys = new object[64];
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i] = new object();
            reg.Add(keys[i], "item");
        }

        var buffer = new List<string>(64);
        reg.CopyItemsTo(buffer);

        Assert.Equal(0L, Allocated(() => reg.CopyItemsTo(buffer)));
        Assert.Equal(0L, Allocated(() => reg.Contains(keys[10])));
        Assert.Equal(0L, Allocated(() => reg.Find(keys[10])));
        Assert.Equal(0L, Allocated(() =>
        {
            reg.Remove(keys[10]);
            reg.Add(keys[10], "item");
        }));
    }

    private sealed class AlwaysEqual
    {
        public override bool Equals(object? obj) => true;

        public override int GetHashCode() => 0;
    }

    private static int PrecedingStepCount(string step) => step switch
    {
        "duplicate Awake (step 2)" => 1,
        "duplicate item (step 3)" => 2,
        "duplicate destroy (step 6)" => 5,
        "unrelated destroy (step 7)" => 6,
        _ => 7,
    };

    private static void Replay(LoadedRegistry<object, string> reg, object chestA, object chestB, int upTo)
    {
        if (upTo >= 1)
        {
            reg.Add(chestA, "chest A");
        }

        if (upTo >= 2)
        {
            reg.Add(chestA, "chest A");
        }

        if (upTo >= 3)
        {
            reg.Add(chestA, "chest A again");
        }

        if (upTo >= 4)
        {
            reg.Add(chestB, "chest B");
        }

        if (upTo >= 5)
        {
            reg.Remove(chestA);
        }

        if (upTo >= 6)
        {
            reg.Remove(chestA);
        }

        if (upTo >= 7)
        {
            reg.Remove(new object());
        }
    }

    private static bool Call(LoadedRegistry<object, string> reg, string step, object chestA, object chestB) => step switch
    {
        "duplicate Awake (step 2)" => reg.Add(chestA, "chest A"),
        "duplicate item (step 3)" => reg.Add(chestA, "chest A again"),
        "duplicate destroy (step 6)" => reg.Remove(chestA),
        "unrelated destroy (step 7)" => reg.Remove(new object()),
        "null key on Add (step 8)" => reg.Add(null!, "x"),
        "null item on Add (step 8)" => reg.Add(chestA, null!),
        "null key on Remove (step 8)" => reg.Remove(null!),
        _ => throw new ArgumentOutOfRangeException(nameof(step)),
    };

    private static long Allocated(Action action)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
