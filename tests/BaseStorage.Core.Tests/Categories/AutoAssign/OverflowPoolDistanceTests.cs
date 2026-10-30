namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System.Collections.Generic;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-5 (A11; derived, ADR-GWIN-037): the distance is a squared 3-D Euclidean double, minimised over the reference set.</summary>
public sealed class OverflowPoolDistanceTests
{
    // GW-U-517
    [Fact]
    public void DistanceIsTheMinimumOverTheReferenceSetNotTheSum()
    {
        // 601 (0, 0) and 602 (100, 0) are persisted Wood. Pool: 603 (50, 0) and 604 (95, 0).
        // Minimum squared distances: 603 = min(2500, 2500) = 2500; 604 = min(9025, 25) = 25. 604 is claimed first.
        // A sum over the reference set would give 603 = 5000 and 604 = 9050, and would pick 603 first.
        ContainerSnapshot[] containers =
        {
            C(601, 0, 0, S("Stone", 50)),
            C(602, 100, 0, S("Stone", 50)),
            C(603, 50, 0),
            C(604, 95, 0),
        };
        var assignments = new Dictionary<ContainerId, CategoryAssignment>
        {
            [Id(601)] = CategoryAssignment.ForCategory("Wood"),
            [Id(602)] = CategoryAssignment.ForCategory("Wood"),
        };
        AutoAssignResult r = ResolveWith(containers, assignments);

        Assert.Equal(new uint?[] { 604, 603 }, ClaimIds(r.CreateOverflowPool(), "Wood", "Wood"));
    }

    // GW-U-517
    [Fact]
    public void YAxisCountsInTheDistance()
    {
        // 611 at (0, 30, 0) is persisted Wood. Pool: 612 at (0, 34, 0), squared distance 16; 613 at (3, 30, 0), squared 9.
        // If the Y axis were ignored, 612 would be at distance 0 and would be claimed first.
        ContainerSnapshot[] containers =
        {
            CY(611, 0, 30, 0, S("Stone", 50)),
            CY(612, 0, 34, 0),
            CY(613, 3, 30, 0),
        };
        var assignments = new Dictionary<ContainerId, CategoryAssignment>
        {
            [Id(611)] = CategoryAssignment.ForCategory("Wood"),
        };
        AutoAssignResult r = ResolveWith(containers, assignments);

        Assert.Equal(new uint?[] { 613, 612 }, ClaimIds(r.CreateOverflowPool(), "Wood", "Wood"));
    }

    // GW-U-517
    [Fact]
    public void LargeCoordinatesGiveFiniteDistancesWithoutAnException()
    {
        // 623 at (1e30, 30, 0) is persisted Wood. Pool: 621 at (-1e30, 30, 0) and 622 at (9e29, 30, 0).
        // Squared distances in double: 622 is (1e29) squared, 1e58; 621 is (2e30) squared, 4e60. 622 is claimed first.
        ContainerSnapshot[] containers =
        {
            CY(623, 1e30f, 30f, 0f, S("Stone", 50)),
            CY(621, -1e30f, 30f, 0f),
            CY(622, 9e29f, 30f, 0f),
        };
        var assignments = new Dictionary<ContainerId, CategoryAssignment>
        {
            [Id(623)] = CategoryAssignment.ForCategory("Wood"),
        };
        AutoAssignResult r = ResolveWith(containers, assignments);

        Assert.Equal(new uint?[] { 622, 621 }, ClaimIds(r.CreateOverflowPool(), "Wood", "Wood"));
    }
}
