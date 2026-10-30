namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-5 (contract 3.2, 5.5): claim order by nearest reference chest, one chest per claim, and pool independence.</summary>
public sealed class OverflowPoolClaimTests
{
    // GW-U-513
    [Theory]
    [InlineData("Wood", new uint[] { 105, 108, 109 })]
    [InlineData("Metals & Ores", new uint[] { 108, 105, 109 })]
    [InlineData("Gear", new uint[] { 108, 105, 109 })]
    [InlineData("Ammo", new uint[] { 105, 108, 109 })]
    [InlineData("Keep", new uint[] { 105, 108, 109 })]
    [InlineData("", new uint[] { 105, 108, 109 })]
    [InlineData("wood", new uint[] { 105, 108, 109 })]
    public void ClaimsGoNearestFirstAndTheCountDrops(string category, uint[] expected)
    {
        OverflowPool pool = ResolveB().CreateOverflowPool();
        uint?[] claims = new uint?[4];
        int[] counts = new int[4];

        for (int i = 0; i < claims.Length; i++)
        {
            claims[i] = pool.Claim(category)?.Id.Id;
            counts[i] = pool.Count;
        }

        Assert.Equal(new uint?[] { expected[0], expected[1], expected[2], null }, claims);
        Assert.Equal(new[] { 2, 1, 0, 0 }, counts);
    }

    // GW-U-514
    [Fact]
    public void EachClaimReturnsExactlyOneChestAndRemovesItFromThePool()
    {
        AutoAssignResult r = ResolveB();
        OverflowPool p = r.CreateOverflowPool();
        Assert.Equal(3, p.Count);
        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(p.GetRemaining()));

        ContainerSnapshot? first = p.Claim("Wood");
        Assert.Same(r.OverflowChests[0], first);
        Assert.Equal(2, p.Count);
        Assert.Equal(new uint[] { 108, 109 }, ContainerIds(p.GetRemaining()));

        Assert.Equal(new uint?[] { 108 }, ClaimIds(p, "Metals & Ores"));
        Assert.Equal(1, p.Count);
        Assert.Equal(new uint[] { 109 }, ContainerIds(p.GetRemaining()));

        Assert.Equal(new uint?[] { 109 }, ClaimIds(p, "Wood"));
        Assert.Equal(0, p.Count);
        Assert.Empty(p.GetRemaining());

        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(r.OverflowChests));
        Assert.Equal(3, r.CreateOverflowPool().Count);
    }

    // GW-U-519
    [Fact]
    public void PoolsFromOneResultGiveTheSameChestsForTheSameClaimSequence()
    {
        AutoAssignResult r = ResolveB();
        OverflowPool p1 = r.CreateOverflowPool();
        OverflowPool p2 = r.CreateOverflowPool();

        Assert.Equal(new uint?[] { 105, 108, 109 }, ClaimIds(p1, "Wood", "Gear", "Wood"));
        Assert.Equal(new uint?[] { 105, 108, 109 }, ClaimIds(p2, "Wood", "Gear", "Wood"));
    }

    // GW-U-519
    [Fact]
    public void ClaimingFromOnePoolLeavesAnotherPoolAndTheResultUnchanged()
    {
        AutoAssignResult r = ResolveB();
        OverflowPool p1 = r.CreateOverflowPool();
        OverflowPool p2 = r.CreateOverflowPool();

        _ = p1.Claim("Wood");

        Assert.Equal(3, p2.Count);
        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(r.OverflowChests));
    }

    // GW-U-519
    [Fact]
    public void GetRemainingReturnsANewCopyOnEachCall()
    {
        OverflowPool p = ResolveB().CreateOverflowPool();

        IReadOnlyList<ContainerSnapshot> first = p.GetRemaining();
        IReadOnlyList<ContainerSnapshot> second = p.GetRemaining();

        Assert.NotSame(first, second);
    }

    // GW-U-519
    [Fact]
    public void ClaimWithANullNameThrowsAndLeavesThePoolUnchanged()
    {
        OverflowPool p = ResolveB().CreateOverflowPool();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => p.Claim(null!));

        Assert.Equal("categoryName", ex.ParamName);
        Assert.Equal(3, p.Count);
        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(p.GetRemaining()));
    }
}
