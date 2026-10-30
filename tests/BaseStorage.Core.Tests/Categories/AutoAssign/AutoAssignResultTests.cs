namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-1 and AC-5 (contract 3.1, 5.2 expression table): the result shape and the category lookups.</summary>
public sealed class AutoAssignResultTests
{
    // GW-U-502
    [Fact]
    public void ResultShapeFollowsTheContractExpressionTable()
    {
        ContainerSnapshot[] b = B();
        AutoAssignResult r = ResolveWith(b, Assignments());

        Assert.Equal(50, r.AutoAssignThresholdPct);
        Assert.Equal(new uint[] { 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111 }, EntryIds(r.Chests));
        for (int i = 0; i < b.Length; i++)
        {
            Assert.Same(b[i], r.Chests[i].Container);
            Assert.Equal(b[i].Id, r.Chests[i].Id);
        }

        Assert.Same(r.Chests[3], r.Find(Id(104)));
        Assert.Equal(AutoAssignOutcome.Assigned, r.Find(Id(104))!.Outcome);
        Assert.Null(r.Find(Id(999)));
        Assert.Null(r.Find(default));
    }

    // GW-U-518
    [Theory]
    [InlineData("Wood", new uint[] { 101, 104, 107, 111 })]
    [InlineData("Metals & Ores", new uint[] { 102 })]
    [InlineData("wood", new uint[] { })]
    [InlineData("Ammo", new uint[] { })]
    [InlineData("Keep", new uint[] { })]
    [InlineData("", new uint[] { })]
    public void ExistingChestsOfACategoryAreListedInSnapshotOrder(string category, uint[] expected)
    {
        AutoAssignResult r = ResolveB();

        IReadOnlyList<ContainerSnapshot> first = r.GetCategoryChests(category);
        IReadOnlyList<ContainerSnapshot> second = r.GetCategoryChests(category);

        Assert.Equal(expected, ContainerIds(first));
        Assert.NotSame(first, second);
    }

    // GW-U-518
    [Fact]
    public void GetCategoryChestsRejectsANullCategoryName()
    {
        AutoAssignResult r = ResolveB();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => r.GetCategoryChests(null!));

        Assert.Equal("categoryName", ex.ParamName);
    }
}
