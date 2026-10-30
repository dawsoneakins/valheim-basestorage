namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-10 (contract 3.1, 3.2, 5.8): returned collections are read-only ReadOnlyCollection instances.</summary>
public sealed class AutoAssignImmutabilityTests
{
    // GW-U-532
    [Fact]
    public void ChestsCannotBeWrittenThroughIList()
    {
        AutoAssignResult r = ResolveB();

        Assert.IsType<ReadOnlyCollection<ChestAutoAssignment>>(r.Chests);
        Assert.Throws<NotSupportedException>(() => ((IList<ChestAutoAssignment>)r.Chests).Add(r.Chests[0]));
    }

    // GW-U-532
    [Fact]
    public void OverflowChestsCannotBeWrittenThroughIList()
    {
        AutoAssignResult r = ResolveB();

        Assert.IsType<ReadOnlyCollection<ContainerSnapshot>>(r.OverflowChests);
        Assert.Throws<NotSupportedException>(() => ((IList<ContainerSnapshot>)r.OverflowChests).RemoveAt(0));
    }

    // GW-U-532
    [Fact]
    public void PoolRemainingCopyCannotBeClearedAndThePoolIsUnchanged()
    {
        OverflowPool p = ResolveB().CreateOverflowPool();
        IReadOnlyList<ContainerSnapshot> remaining = p.GetRemaining();

        Assert.IsType<ReadOnlyCollection<ContainerSnapshot>>(remaining);
        Assert.Throws<NotSupportedException>(() => ((IList<ContainerSnapshot>)remaining).Clear());
        Assert.Equal(3, p.Count);
    }

    // GW-U-532
    [Fact]
    public void CategoryChestsCannotBeWrittenThroughIList()
    {
        AutoAssignResult r = ResolveB();
        IReadOnlyList<ContainerSnapshot> wood = r.GetCategoryChests("Wood");

        Assert.IsType<ReadOnlyCollection<ContainerSnapshot>>(wood);
        Assert.Throws<NotSupportedException>(() => ((IList<ContainerSnapshot>)wood).Add(r.OverflowChests[0]));
    }
}
