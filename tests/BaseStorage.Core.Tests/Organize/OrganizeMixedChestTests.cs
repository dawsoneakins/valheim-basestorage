namespace BaseStorage.Core.Tests.Organize;

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>
/// S-A-001 (AC-1, AC-2, AC-3, AC-8, AC-12; mixed-chest-category-contracts.md 3.1, 3.2 and 6.8): the Organize planner with the
/// mixed-chest step on base E, the v0.1 result with it off, its determinism, and the timing budget with it on.
/// </summary>
[Collection(OrganizeTimingCollection.Name)]
public sealed class OrganizeMixedChestTests
{
    /// <summary>The skipped rows of base E (organize-plan 6.2), the same with the step on or off.</summary>
    private static SkipRow[] SkippedRowsOfE() => new[]
    {
        Skip(205, SkipReason.Keep, "Keep", null, ExclusionReason.Keep),
        Skip(206, SkipReason.InUse, "In use by Erik", "Erik", null),
        Skip(207, SkipReason.Ineligible, "Ineligible: tombstone", null, ExclusionReason.Tombstone),
    };

    /// <summary>The v0.1 moves of base E (organize-plan 6.2 M1 to M6), in order.</summary>
    private static MoveRow[] V01MovesOfE() => new[]
    {
        Move(201, 1, 202, "Stone", 1, 50, 10, "Stone"),
        Move(203, 0, 201, "Wood", 1, 50, 20, "Wood"),
        Move(209, 0, 201, "Wood", 1, 50, 40, "Wood"),
        Move(209, 1, 201, "Wood", 1, 50, 10, "Wood"),
        Move(209, 1, 208, "Wood", 1, 50, 35, "Wood"),
        Move(209, 2, 204, "Resin", 1, 50, 12, "Misc"),
    };

    // GW-U-2402 (AC-1, 6.8 true): the exact moves, no-room, claimed overflow, skipped and layouts of base E with the step.
    [Fact]
    public void BaseEWithTheStepPlansTheContractMovesAndLayouts()
    {
        OrganizePlan plan = Run(E(), true);

        Assert.Equal(
            new[]
            {
                Move(201, 1, 202, "Stone", 1, 50, 10, "Stone"),
                Move(203, 0, 201, "Wood", 1, 50, 20, "Wood"),
                Move(209, 2, 204, "Resin", 1, 50, 12, "Misc"),
                Move(209, 3, 208, "CookedMeat", 1, 50, 5, "Cooked Food"),
            },
            MovesOf(plan));
        Assert.Equal(new[] { NoRoom(209, 4, "TrophyDeer", 1, 20, 1, "Trophies") }, NoRoomOf(plan));
        Assert.Equal(new uint[] { 204, 208 }, plan.ClaimedOverflow.Select(layout => layout.Id.Id));
        Assert.Equal(SkippedRowsOfE(), SkipsOf(plan));
        Assert.Equal(
            new[]
            {
                Layout(201, ChestRole.Category, "Wood", S("Wood", 50)),
                Layout(202, ChestRole.Category, "Stone", S("Flint", 5), S("Stone", 50), S("Stone", 5)),
                Layout(203, ChestRole.Category, "Stone", S("Flint", 7), S("Stone", 40)),
                Layout(204, ChestRole.ClaimedOverflow, "Misc", S("Resin", 12)),
                Layout(208, ChestRole.ClaimedOverflow, "Cooked Food", S("CookedMeat", 5)),
                Layout(209, ChestRole.Category, "Wood", S("TrophyDeer", 1, 20), S("Wood", 50), S("Wood", 35)),
            },
            LayoutsOf(plan));

        Assert.DoesNotContain(plan.Moves, move => move.Source == Id(209) && (move.SourceStackIndex == 0 || move.SourceStackIndex == 1));
        Assert.Equal(47, plan.Moves.Sum(move => move.Count));
        Assert.Equal(1, plan.NoRoom.Sum(entry => entry.Count));
    }

    // GW-U-2407 (AC-2, MC3, 6.8): skipped chests add neither demand nor capacity, so 209 is the single Wood pick.
    [Fact]
    public void SkippedChestsAddNeitherDemandNorCapacity()
    {
        OrganizePlan plan = Run(E(), true);

        Assert.Equal(new uint[] { 201, 202, 203, 204, 208, 209 }, plan.AutoAssign.Chests.Select(chest => chest.Id.Id));
        Assert.Equal(AutoAssignOutcome.Assigned, plan.AutoAssign.Find(Id(209))!.Outcome);
        Assert.Equal("Wood", plan.AutoAssign.Find(Id(209))!.CategoryName);
        Assert.Equal(ChestRole.Category, plan.FindLayout(Id(209))!.Role);
    }

    // GW-U-2413 (AC-3, 6.8): the planner with the step is deterministic on the same input and on a fresh base E.
    [Fact]
    public void PlanningWithTheStepIsDeterministic()
    {
        PlanInput input = E();

        OrganizePlan first = Run(input, true);
        OrganizePlan second = Run(input, true);
        OrganizePlan fresh = Run(E(), true);

        AssertSamePlan(first, second);
        AssertSamePlan(first, fresh);
    }

    // GW-U-2440 (AC-8, 3.1, 6.8 false): the 8-parameter overload with false and the 7-parameter overload both give the v0.1 plan.
    [Fact]
    public void FalseGivesTheV01PlanOnBaseEFromBothOverloads()
    {
        PlanInput input = E();
        OrganizePlan eightWithFalse = OrganizePlanner.Plan(
            input.Containers,
            input.Assignments,
            input.InUseBy,
            input.Catalog,
            input.Kinds,
            input.Options,
            input.Threshold,
            false);
        OrganizePlan sevenParameter = OrganizePlanner.Plan(
            input.Containers,
            input.Assignments,
            input.InUseBy,
            input.Catalog,
            input.Kinds,
            input.Options,
            input.Threshold);

        Assert.Equal(V01MovesOfE(), MovesOf(eightWithFalse));
        Assert.Equal(new uint[] { 208, 204 }, eightWithFalse.ClaimedOverflow.Select(layout => layout.Id.Id));
        Assert.Equal(ChestRole.Source, eightWithFalse.FindLayout(Id(209))!.Role);
        AssertSamePlan(sevenParameter, eightWithFalse);
    }

    // GW-U-2463 (AC-12, MC11, P12): 60 chests of 32 slots with the step plan in under 50 ms, measured as the minimum of five calls.
    [Fact]
    public void SixtyChestsWithTheStepPlanInUnderFiftyMilliseconds()
    {
        PlanInput input = PerfBase();
        Assert.Equal(60, input.Containers.Length);

        _ = Run(input, true);

        var milliseconds = new List<double>(5);
        OrganizePlan? last = null;
        for (int i = 0; i < 5; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            last = Run(input, true);
            stopwatch.Stop();
            milliseconds.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        double minimum = milliseconds.Min();
        Assert.True(
            minimum < 50.0,
            "Minimum of 5 timed Plan calls with the step was " + minimum.ToString("F3", CultureInfo.InvariantCulture) + " ms.");
        Assert.NotNull(last);
    }
}
