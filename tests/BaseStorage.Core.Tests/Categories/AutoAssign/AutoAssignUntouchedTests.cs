namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System.Collections.Generic;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-8 (A8, A9; contract 4.1, 4.3 step 1, 5.2 rows 101, 106, 107): persisted chests are untouched, whatever they hold or how they are flagged.</summary>
public sealed class AutoAssignUntouchedTests
{
    private static readonly string[] AllCategoryNames =
    {
        "Metals & Ores", "Wood", "Stone", "Cooked Food", "Trophies", "Gear", "Misc", "Ammo", "Keep", string.Empty,
    };

    // GW-U-524
    [Theory]
    [InlineData(101u, AutoAssignOutcome.AlreadyAssigned, "Wood")]
    [InlineData(106u, AutoAssignOutcome.Keep, null)]
    [InlineData(107u, AutoAssignOutcome.AlreadyAssigned, "Wood")]
    public void PersistedChestsAreReportedUntouchedAndNeverPooled(uint id, AutoAssignOutcome outcome, string? name)
    {
        AutoAssignResult r = ResolveB();

        AssertChest("chest " + id, outcome, name, At(r, id));
        Assert.DoesNotContain(id, ContainerIds(r.OverflowChests));
    }

    // GW-U-524
    [Fact]
    public void KeepChest106IsInNoCategoryResult()
    {
        AutoAssignResult r = ResolveB();

        foreach (string name in AllCategoryNames)
        {
            Assert.DoesNotContain(106u, ContainerIds(r.GetCategoryChests(name)));
        }
    }

    // GW-U-525
    [Fact]
    public void KeepChestFullOfWoodGivesKeep()
    {
        ContainerSnapshot chest = C(201, 0, 0, S("Wood", 10), S("Wood", 10));

        AutoAssignResult r = ResolveWith(new[] { chest }, Persist(201, CategoryAssignment.Keep));

        AssertChest("Keep chest full of Wood", AutoAssignOutcome.Keep, null, At(r, 201));
    }

    // GW-U-525
    [Fact]
    public void PersistedStoneChestHoldingOnlyWoodIsAlreadyAssignedToStone()
    {
        ContainerSnapshot chest = C(201, 0, 0, S("Wood", 10));

        AutoAssignResult r = ResolveWith(new[] { chest }, Persist(201, CategoryAssignment.ForCategory("Stone")));

        AssertChest("persisted Stone holding Wood", AutoAssignOutcome.AlreadyAssigned, "Stone", At(r, 201));
    }

    // GW-U-525
    [Fact]
    public void PersistedNameOutsideTheCatalogIsKeptVerbatim()
    {
        ContainerSnapshot chest = C(201, 0, 0, S("Wood", 1));

        AutoAssignResult r = ResolveWith(new[] { chest }, Persist(201, CategoryAssignment.ForCategory("Iron")));

        AssertChest("persisted Iron outside the catalog", AutoAssignOutcome.AlreadyAssigned, "Iron", At(r, 201));
    }

    // GW-U-525
    [Theory]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    public void EmptyKeepAndEmptyAssignedChestsAreNeverPooledAtAnyThreshold(int threshold)
    {
        ContainerSnapshot[] containers = { C(201, 0, 0), C(202, 0, 0) };
        var assignments = new Dictionary<ContainerId, CategoryAssignment>
        {
            [Id(201)] = CategoryAssignment.Keep,
            [Id(202)] = CategoryAssignment.ForCategory("Wood"),
        };

        AutoAssignResult r = ResolveWith(containers, assignments, threshold: threshold);

        AssertChest("empty Keep at threshold " + threshold, AutoAssignOutcome.Keep, null, At(r, 201));
        AssertChest("empty assigned at threshold " + threshold, AutoAssignOutcome.AlreadyAssigned, "Wood", At(r, 202));
        Assert.Empty(r.OverflowChests);
    }

    // GW-U-526
    [Theory]
    [InlineData(ContainerFlags.None)]
    [InlineData(ContainerFlags.PlayerBuilt)]
    [InlineData(ContainerFlags.AccessDenied | ContainerFlags.InUseByOther)]
    [InlineData(ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle | ContainerFlags.Tombstone | ContainerFlags.AccessDenied | ContainerFlags.InUseByOther)]
    public void FlagsAreNotReadAndAForeignIdIsIgnored(ContainerFlags flags)
    {
        ContainerSnapshot[] containers =
        {
            Flagged(601, flags, S("Stone", 1)),
            Flagged(602, flags),
            Flagged(603, flags),
            Flagged(604, flags, S("Wood", 1), S("Wood", 1)),
        };
        var assignments = new Dictionary<ContainerId, CategoryAssignment>
        {
            [Id(601)] = CategoryAssignment.ForCategory("Wood"),
            [Id(602)] = CategoryAssignment.Keep,
            [Id(999)] = CategoryAssignment.ForCategory("Stone"),
        };

        AutoAssignResult r = ResolveWith(containers, assignments);

        string label = flags.ToString();
        AssertChest(label + ", persisted Wood", AutoAssignOutcome.AlreadyAssigned, "Wood", At(r, 601));
        AssertChest(label + ", persisted Keep", AutoAssignOutcome.Keep, null, At(r, 602));
        AssertChest(label + ", unassigned empty", AutoAssignOutcome.Overflow, null, At(r, 603));
        AssertChest(label + ", unassigned all Wood", AutoAssignOutcome.Assigned, "Wood", At(r, 604));
        Assert.Null(r.Find(Id(999)));
        Assert.Equal(4, r.Chests.Count);
    }

    private static Dictionary<ContainerId, CategoryAssignment> Persist(uint id, CategoryAssignment value) =>
        new Dictionary<ContainerId, CategoryAssignment> { [Id(id)] = value };
}
