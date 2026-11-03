namespace BaseStorage.Core.Tests.Restock;

using System.Collections.Generic;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;

/// <summary>S-QR-001 AC-4 (contract 7.3, rule Q7): the fixed source order of restock.</summary>
public sealed class RestockSourceOrderTests
{
    // GW-U-2500 (Q7)
    [Fact]
    public void Q07_NearestChestFirst()
    {
        var carried = new List<CarriedStack> { H(S("Wood", 40)) };
        var chests = new List<ContainerSnapshot> { C(911, 5, 0, 1, S("Wood", 50)), C(912, 1, 0, 1, S("Wood", 50)) };

        RestockPlan plan = RestockPlanner.Plan(carried, Origin(), chests, NoAssign(), Craftable());

        Assert.Equal(new[] { Row(912, 0, 0, 10) }, Shape(plan));
    }

    // GW-U-2501 (Q7)
    [Fact]
    public void Q07_NearestBeatsFullest()
    {
        var carried = new List<CarriedStack> { H(S("Wood", 30)) };
        var chests = new List<ContainerSnapshot> { C(913, 1, 0, 1, S("Wood", 5)), C(914, 2, 0, 1, S("Wood", 50)) };

        RestockPlan plan = RestockPlanner.Plan(carried, Origin(), chests, NoAssign(), Craftable());

        Assert.Equal(new[] { Row(913, 0, 0, 5), Row(914, 0, 0, 15) }, Shape(plan));
    }

    // GW-U-2502 (Q7)
    [Fact]
    public void Q07_EqualDistanceTiesGoToSnapshotOrder()
    {
        var carried = new List<CarriedStack> { H(S("Wood", 40)) };

        var forward = new List<ContainerSnapshot> { C(915, 4, 3, 1, S("Wood", 30)), C(916, 3, 4, 1, S("Wood", 30)) };
        RestockPlan first = RestockPlanner.Plan(carried, Origin(), forward, NoAssign(), Craftable());
        Assert.Equal(new[] { Row(915, 0, 0, 10) }, Shape(first));

        var reversed = new List<ContainerSnapshot> { C(916, 3, 4, 1, S("Wood", 30)), C(915, 4, 3, 1, S("Wood", 30)) };
        RestockPlan second = RestockPlanner.Plan(carried, Origin(), reversed, NoAssign(), Craftable());
        Assert.Equal(new[] { Row(916, 0, 0, 10) }, Shape(second));
    }

    // GW-U-2503 (Q7, A11 uses Y and double)
    [Fact]
    public void Q07_DistanceIncludesHeight()
    {
        var carried = new List<CarriedStack> { H(S("Wood", 40)) };
        var chests = new List<ContainerSnapshot>
        {
            new ContainerSnapshot(Id(917), new WorldPosition(1f, 40f, 0f), 1, 1, new[] { S("Wood", 50) }, ContainerFlags.PlayerBuilt),
            C(918, 3, 0, 1, S("Wood", 50)),
        };

        RestockPlan plan = RestockPlanner.Plan(carried, Origin(), chests, NoAssign(), Craftable());

        Assert.Equal(new[] { Row(918, 0, 0, 10) }, Shape(plan));
    }

    // GW-U-2504 (Q7)
    [Fact]
    public void Q07_FullestStackFirstThenLowerIndex()
    {
        var carried = new List<CarriedStack> { H(S("Wood", 10)) };
        var chests = new List<ContainerSnapshot> { C(919, 1, 0, 3, S("Wood", 5), S("Wood", 30), S("Wood", 30)) };

        RestockPlan plan = RestockPlanner.Plan(carried, Origin(), chests, NoAssign(), Craftable());

        Assert.Equal(new[] { Row(919, 1, 0, 30), Row(919, 2, 0, 10) }, Shape(plan));
    }

    // GW-U-2505 (Q7, the order uses input counts)
    [Fact]
    public void Q07_OrderIsComputedOnceFromInputCounts()
    {
        var carried = new List<CarriedStack> { H(S("Wood", 45)), H(S("Wood", 25)) };
        var chests = new List<ContainerSnapshot> { C(920, 1, 0, 2, S("Wood", 20), S("Wood", 18)) };

        RestockPlan plan = RestockPlanner.Plan(carried, Origin(), chests, NoAssign(), Craftable());

        Assert.Equal(new[] { Row(920, 0, 0, 5), Row(920, 0, 1, 15), Row(920, 1, 1, 10) }, Shape(plan));
    }
}
