namespace BaseStorage.Core.Tests.Snapshots.Identities;

using System;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Snapshots.Identities;
using static BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures;

/// <summary>S-QR-001 AC-2: the additive ItemStackSnapshot identity, its equality and its constructor validation (contract 2.2, 2.3).</summary>
public sealed class ItemStackSnapshotIdentityTests
{
    // GW-U-2484
    [Fact]
    public void TheFourArgumentConstructorYieldsTheEmptyIdentity()
    {
        var fourArgument = new ItemStackSnapshot("Wood", 17, 1, 50);
        Assert.True(ReferenceEquals(fourArgument.Identity, ItemStackIdentity.Empty));

        var fiveArgument = new ItemStackSnapshot("Wood", 17, 1, 50, ItemStackIdentity.Empty);
        Assert.True(fourArgument == fiveArgument);
        Assert.Equal(fourArgument.GetHashCode(), fiveArgument.GetHashCode());

        ItemStackIdentity wl2 = Wl2();
        var withWl2 = new ItemStackSnapshot("Wood", 17, 1, 50, wl2);
        Assert.True(ReferenceEquals(withWl2.Identity, wl2));

        var wl2Copy = new ItemStackSnapshot("Wood", 17, 1, 50, new ItemStackIdentity(0f, 0L, "", 0, 2, NoData()));
        Assert.True(withWl2 == wl2Copy);
        Assert.Equal(withWl2.GetHashCode(), wl2Copy.GetHashCode());
    }

    // GW-U-2485
    [Fact]
    public void AStackSwappedForADifferentIdentityComparesAsChanged()
    {
        var wood17 = new ItemStackSnapshot("Wood", 17, 1, 50);
        var wood17Wl2 = new ItemStackSnapshot("Wood", 17, 1, 50, Wl2());
        Assert.True(wood17 != wood17Wl2);
        Assert.False(wood17 == wood17Wl2);

        var position = new WorldPosition(0f, 30f, 0f);
        var id = new ContainerId(3141592653L, 901u);
        var before = new ContainerSnapshot(id, position, 4, 1, new[] { wood17 }, ContainerFlags.PlayerBuilt);
        var after = new ContainerSnapshot(id, position, 4, 1, new[] { wood17Wl2 }, ContainerFlags.PlayerBuilt);
        Assert.True(before != after);
        Assert.False(before == after);
    }

    // GW-U-2486
    [Fact]
    public void FiveArgumentValidationRunsTheV1ChecksFirst()
    {
        ItemStackIdentity wl2 = Wl2();

        var nullIdentity = Assert.Throws<ArgumentNullException>(() => new ItemStackSnapshot("Wood", 17, 1, 50, null!));
        Assert.Equal("identity", nullIdentity.ParamName);

        var emptyName = Assert.Throws<ArgumentException>(() => new ItemStackSnapshot("", 0, 1, 50, null!));
        Assert.Equal("prefabName", emptyName.ParamName);

        var zeroCount = Assert.Throws<ArgumentOutOfRangeException>(() => new ItemStackSnapshot("Wood", 0, 1, 50, wl2));
        Assert.Equal("count", zeroCount.ParamName);

        var overFull = new ItemStackSnapshot("Wood", 75, 1, 50, wl2);
        Assert.Equal(75, overFull.Count);
        Assert.True(ReferenceEquals(overFull.Identity, wl2));
    }
}
