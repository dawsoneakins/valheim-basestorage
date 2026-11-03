namespace BaseStorage.Core.Tests.Snapshots.Identities;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots.Identities;
using static BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures;

/// <summary>S-QR-001 AC-2: ItemStackIdentity value semantics, storage and validation (contract 2.1, 2.3).</summary>
public sealed class ItemStackIdentityTests
{
    // GW-U-2480
    [Fact]
    public void EmptyIsAllZeroAndASingleInstance()
    {
        Assert.True(ItemStackIdentity.Empty.IsEmpty);
        Assert.True(ReferenceEquals(ItemStackIdentity.Empty, ItemStackIdentity.Empty));

        var zero = new ItemStackIdentity(0f, 0L, "", 0, 0, NoData());
        Assert.True(zero == ItemStackIdentity.Empty);
        Assert.Equal(ItemStackIdentity.Empty.GetHashCode(), zero.GetHashCode());
        Assert.True(zero.IsEmpty);

        var negativeZero = new ItemStackIdentity(-0f, 0L, "", 0, 0, NoData());
        Assert.True(negativeZero == ItemStackIdentity.Empty);
        Assert.Equal(ItemStackIdentity.Empty.GetHashCode(), negativeZero.GetHashCode());

        ItemStackIdentity wl2 = Wl2();
        Assert.True(wl2 != ItemStackIdentity.Empty);
        Assert.False(wl2.IsEmpty);
    }

    // GW-U-2481
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void EachIdentityFieldTakesPartInEquality(int caseIndex)
    {
        (ItemStackIdentity left, ItemStackIdentity right, bool equal) = Pair(caseIndex);

        if (equal)
        {
            Assert.True(left == right);
            Assert.False(left != right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }
        else
        {
            Assert.True(left != right);
            Assert.False(left == right);
        }

        Assert.True((ItemStackIdentity?)null == null);
        Assert.True(left != null);
        Assert.False(left.Equals(null));
    }

    // GW-U-2482
    [Fact]
    public void CustomDataIsSortedCopiedAndReadOnly()
    {
        var forward = new ItemStackIdentity(100f, 8812345012L, "Erik", 1, 0, new[] { KV("rune", "frost"), KV("dyed", "red") });
        var reverse = new ItemStackIdentity(100f, 8812345012L, "Erik", 1, 0, new[] { KV("dyed", "red"), KV("rune", "frost") });

        Assert.True(forward == reverse);
        Assert.Equal(forward.GetHashCode(), reverse.GetHashCode());
        Assert.Equal(new[] { KV("dyed", "red"), KV("rune", "frost") }, forward.CustomData);

        var source = new Dictionary<string, string> { ["a"] = "1" };
        var copied = new ItemStackIdentity(0f, 0L, "", 0, 0, source);
        source["b"] = "2";
        Assert.Single(copied.CustomData);

        Assert.Throws<NotSupportedException>(() => ((IList<KeyValuePair<string, string>>)forward.CustomData).Add(KV("x", "y")));
    }

    // GW-U-2483
    [Theory]
    [InlineData(0, typeof(ArgumentOutOfRangeException), "durability")]
    [InlineData(1, typeof(ArgumentOutOfRangeException), "durability")]
    [InlineData(2, typeof(ArgumentNullException), "crafterName")]
    [InlineData(3, typeof(ArgumentNullException), "customData")]
    [InlineData(4, typeof(ArgumentException), "customData")]
    [InlineData(5, typeof(ArgumentException), "customData")]
    [InlineData(6, typeof(ArgumentException), "customData")]
    [InlineData(7, null, null)]
    public void IdentityValidationRejectsOnlyMeaninglessValues(int caseIndex, Type? expectedType, string? paramName)
    {
        Exception? thrown = Record.Exception(() => Build(caseIndex));

        if (expectedType is null)
        {
            Assert.Null(thrown);
            return;
        }

        Assert.NotNull(thrown);
        Assert.Equal(expectedType, thrown!.GetType());
        Assert.Equal(paramName, ((ArgumentException)thrown).ParamName);
    }

    private static (ItemStackIdentity Left, ItemStackIdentity Right, bool Equal) Pair(int caseIndex) => caseIndex switch
    {
        0 => (Worn(), new ItemStackIdentity(37.25f, 0L, "", 0, 0, NoData()), false),
        1 => (Erik(), new ItemStackIdentity(100f, 8812345013L, "Erik", 1, 0, ErikData()), false),
        2 => (Erik(), new ItemStackIdentity(100f, 8812345012L, "erik", 1, 0, ErikData()), false),
        3 => (Erik(), new ItemStackIdentity(100f, 8812345012L, "Erik", 2, 0, ErikData()), false),
        4 => (Wl2(), new ItemStackIdentity(0f, 0L, "", 0, 3, NoData()), false),
        5 => (Erik(), new ItemStackIdentity(100f, 8812345012L, "Erik", 1, 0, new[] { KV("rune", "frost") }), false),
        6 => (Erik(), new ItemStackIdentity(100f, 8812345012L, "Erik", 1, 0, new[] { KV("rune", "Frost"), KV("dyed", "red") }), false),
        7 => (Erik(), Erik(), true),
        _ => throw new ArgumentOutOfRangeException(nameof(caseIndex)),
    };

    private static ItemStackIdentity Build(int caseIndex) => caseIndex switch
    {
        0 => new ItemStackIdentity(float.NaN, 0L, "", 0, 0, NoData()),
        1 => new ItemStackIdentity(float.NegativeInfinity, 0L, "", 0, 0, NoData()),
        2 => new ItemStackIdentity(0f, 0L, null!, 0, 0, NoData()),
        3 => new ItemStackIdentity(0f, 0L, "", 0, 0, null!),
        4 => new ItemStackIdentity(0f, 0L, "", 0, 0, new[] { KV("a", "1"), KV("a", "2") }),
        5 => new ItemStackIdentity(0f, 0L, "", 0, 0, new[] { new KeyValuePair<string, string>(null!, "1") }),
        6 => new ItemStackIdentity(0f, 0L, "", 0, 0, new[] { KV("a", null!) }),
        7 => new ItemStackIdentity(-5f, -1L, "  ", -3, -1, new[] { KV("", "") }),
        _ => throw new ArgumentOutOfRangeException(nameof(caseIndex)),
    };
}
