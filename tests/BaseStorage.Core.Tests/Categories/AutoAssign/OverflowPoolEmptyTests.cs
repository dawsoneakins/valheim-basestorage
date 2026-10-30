namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-6 (contract 3.2 step 2, 5.5 steps 4-5 and 7, 5.7): an empty pool returns nothing and changes nothing.</summary>
public sealed class OverflowPoolEmptyTests
{
    // GW-U-520
    [Fact]
    public void ClaimingFromADrainedPoolReturnsNothingAndChangesNothing()
    {
        AutoAssignResult r = ResolveB();
        OverflowPool p = r.CreateOverflowPool();
        Assert.Equal(new uint?[] { 105, 108, 109 }, ClaimIds(p, "Wood", "Metals & Ores", "Wood"));

        Assert.Equal(new uint?[] { null, null }, ClaimIds(p, "Wood", "Wood"));
        Assert.Null(p.Claim("Stone"));
        Assert.Equal(0, p.Count);
        Assert.Empty(p.GetRemaining());
        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(r.OverflowChests));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => p.Claim(null!));
        Assert.Equal("categoryName", ex.ParamName);
    }

    // GW-U-521
    [Fact]
    public void PoolFromTheEmptyInputResultStartsEmpty()
    {
        AutoAssignResult r = AutoAssigner.Resolve(
            Array.Empty<ContainerSnapshot>(),
            new Dictionary<ContainerId, CategoryAssignment>(),
            CategoryCatalog.Empty,
            new Dictionary<string, ItemKind>(),
            50);
        OverflowPool p = r.CreateOverflowPool();

        Assert.Null(p.Claim("Wood"));
        Assert.Equal(0, p.Count);
    }

    // GW-U-521
    [Fact]
    public void ABaseWithNoEmptyUnassignedChestGivesAnEmptyPoolThatClaimsNothing()
    {
        ContainerSnapshot[] b = B();
        ContainerSnapshot[] containers = { b[0], b[5], b[6] };

        AutoAssignResult r = ResolveWith(containers, Assignments());

        Assert.Empty(r.OverflowChests);
        OverflowPool p = r.CreateOverflowPool();
        Assert.Equal(new uint?[] { null, null }, ClaimIds(p, "Wood", "Wood"));
        Assert.Equal(0, p.Count);
    }

    // GW-U-521
    [Fact]
    public void ASecondPoolFromThatResultAlsoStartsWithCountZero()
    {
        ContainerSnapshot[] b = B();
        ContainerSnapshot[] containers = { b[0], b[5], b[6] };
        AutoAssignResult r = ResolveWith(containers, Assignments());
        _ = r.CreateOverflowPool();

        Assert.Equal(0, r.CreateOverflowPool().Count);
    }
}
