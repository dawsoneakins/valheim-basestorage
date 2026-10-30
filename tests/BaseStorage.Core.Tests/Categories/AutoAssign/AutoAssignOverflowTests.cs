namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-4 (contract 2.1 Overflow, 4.3 step 2, 5.2, 5.7): empty unassigned chests go to the overflow pool with no category.</summary>
public sealed class AutoAssignOverflowTests
{
    // GW-U-510
    [Fact]
    public void EmptyUnassignedChestsAreOverflowWithNoCategoryAndPooled()
    {
        ContainerSnapshot[] b = B();
        AutoAssignResult r = ResolveWith(b, Assignments());

        AssertChest("chest 105", AutoAssignOutcome.Overflow, null, At(r, 105));
        AssertChest("chest 108", AutoAssignOutcome.Overflow, null, At(r, 108));
        AssertChest("chest 109", AutoAssignOutcome.Overflow, null, At(r, 109));
        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(r.OverflowChests));
        Assert.Same(b[4], r.OverflowChests[0]);
        Assert.Same(b[7], r.OverflowChests[1]);
        Assert.Same(b[8], r.OverflowChests[2]);
        foreach (uint id in new uint[] { 105, 108, 109 })
        {
            Assert.DoesNotContain(id, ContainerIds(r.GetCategoryChests("Wood")));
        }

        OverflowPool pool = r.CreateOverflowPool();
        Assert.Equal(3, pool.Count);
        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(pool.GetRemaining()));
    }

    // GW-U-511
    [Theory]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(70)]
    [InlineData(76)]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void EmptyChestsArePooledWhateverTheThreshold(int threshold)
    {
        AutoAssignResult r = ResolveB(threshold);

        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(r.OverflowChests));
        foreach (uint id in new uint[] { 105, 108, 109 })
        {
            AssertChest("threshold " + threshold + ", chest " + id, AutoAssignOutcome.Overflow, null, At(r, id));
        }
    }

    // GW-U-511
    [Fact]
    public void SingleEmptyChestAtTheMinimumThresholdIsOverflowNotAssigned()
    {
        AutoAssignResult r = ResolveOne(C(201, 0, 0), int.MinValue);

        AssertChest("empty chest at int.MinValue", AutoAssignOutcome.Overflow, null, At(r, 201));
    }

    // GW-U-512
    [Fact]
    public void EmptyInputGivesAnEmptyResultAndAnEmptyPool()
    {
        AutoAssignResult r = AutoAssigner.Resolve(
            Array.Empty<ContainerSnapshot>(),
            new Dictionary<ContainerId, CategoryAssignment>(),
            CategoryCatalog.Empty,
            new Dictionary<string, ItemKind>(),
            50);

        Assert.Empty(r.Chests);
        Assert.Empty(r.OverflowChests);
        Assert.Equal(0, r.CreateOverflowPool().Count);
    }

    // GW-U-512
    [Fact]
    public void ThreeEmptyChestsAllOverflowInSnapshotOrderNotPositionOrIdOrder()
    {
        ContainerSnapshot[] containers = { C(701, 5, 0), C(702, 1, 0), C(703, 3, 0) };

        AutoAssignResult r = ResolveWith(containers);

        AssertChest("chest 701", AutoAssignOutcome.Overflow, null, At(r, 701));
        AssertChest("chest 702", AutoAssignOutcome.Overflow, null, At(r, 702));
        AssertChest("chest 703", AutoAssignOutcome.Overflow, null, At(r, 703));
        Assert.Equal(new uint[] { 701, 702, 703 }, ContainerIds(r.OverflowChests));
    }
}
