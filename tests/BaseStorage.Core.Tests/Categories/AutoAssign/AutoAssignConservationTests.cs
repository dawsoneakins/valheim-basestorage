namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-1 (A2, spec NFR conservation): resolution only reads its inputs, and the result is by value.</summary>
public sealed class AutoAssignConservationTests
{
    // GW-U-503
    [Fact]
    public void ResolveNeverChangesTheContainersOrTheAssignments()
    {
        ContainerSnapshot[] b = B();
        ContainerSnapshot[] before = B();
        Dictionary<ContainerId, CategoryAssignment> assignments = Assignments();

        AutoAssignResult r = ResolveWith(b, assignments);

        for (int i = 0; i < b.Length; i++)
        {
            Assert.Equal(before[i], b[i]);
        }

        Assert.Equal(4, assignments.Count);
        Assert.Equal(CategoryAssignment.ForCategory("Wood"), assignments[Id(101)]);
        Assert.Equal(CategoryAssignment.Keep, assignments[Id(106)]);
        Assert.Equal(CategoryAssignment.ForCategory("Wood"), assignments[Id(107)]);
        Assert.Equal(CategoryAssignment.ForCategory("Stone"), assignments[Id(999)]);
        AssertTable("resolved B", r, BaseTable50);
    }

    // GW-U-503
    [Fact]
    public void TheResultDoesNotChangeWhenTheCallerEditsItsInputsAfterwards()
    {
        ContainerSnapshot[] b = B();
        Dictionary<ContainerId, CategoryAssignment> assignments = Assignments();
        Dictionary<string, ItemKind> kinds = Kinds();
        AutoAssignResult r = ResolveWith(b, assignments, Catalog(), kinds);

        b[1] = C(102, 2, 0);
        assignments[Id(105)] = CategoryAssignment.Keep;
        kinds["SwordIron"] = ItemKind.Other;

        AssertTable("after the caller's edits", r, BaseTable50);
        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(r.OverflowChests));
        Assert.Equal(new uint?[] { 108 }, ClaimIds(r.CreateOverflowPool(), "Gear"));
    }

    // GW-U-503
    [Fact]
    public void ResolveOverAReadOnlyWrapperOfTheAssignmentsGivesTheSameResult()
    {
        var wrapped = new ReadOnlyDictionary<ContainerId, CategoryAssignment>(Assignments());

        AutoAssignResult r = ResolveWith(B(), wrapped);

        AssertTable("read-only wrapper", r, BaseTable50);
    }
}
