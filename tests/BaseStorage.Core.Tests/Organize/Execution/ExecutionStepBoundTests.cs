namespace BaseStorage.Core.Tests.Organize.Execution;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.Execution.ExecutionHarness;

/// <summary>Story AC-3 (contract X6; 5.3): each step completes at most MovesPerFrame moves, read once per step, with a floor of 1.</summary>
public sealed class ExecutionStepBoundTests
{
    // GW-U-920 (AC-3, contract 5.3 row 1): with MovesPerFrame 3 on base E every step returns at most 3 and batch 209 takes two writing steps.
    [Fact]
    public void BoundOfThreeOnBaseESplitsTheLastBatchOverTwoSteps()
    {
        ExecutionHarness h = Named("E", movesPerFrame: 3);

        List<int> steps = h.Run(3);

        Assert.Equal(new[] { 0, 1, 1, 3, 1 }, steps);
        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);
        Assert.Equal(6, h.Result.MovesCompleted);
        Assert.Equal(127L, h.Result.ItemsMoved);
        h.AssertLiveTotalsMatchLayouts();
        h.AssertConserved();
    }

    // GW-U-921 (AC-3, contract 5.3 row 2): the 60 x 32 perf base runs under the bound of 20 on every step and completes.
    [Fact]
    public void BoundOfTwentyHoldsOnEveryStepOfThePerfBase()
    {
        ExecutionHarness h = Named("Perf", movesPerFrame: 20);
        int moves = h.Plan.Moves.Count;
        Assert.True(moves > 100, "The perf base must have many moves for this row to mean anything.");

        List<int> steps = h.Run(20);

        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);
        Assert.Equal(moves, steps.Sum());
        Assert.Equal(20, steps.Max());
        Assert.True(steps.Count(s => s > 0) >= (moves + 19) / 20, "Too few positive steps for the bound.");
        h.AssertLiveTotalsMatchLayouts();
        h.AssertConserved();
    }

    // GW-U-922 (AC-3, contract 5.3 row 3): the bound is read once per step, so a changed setting applies to the next step.
    [Fact]
    public void TheBoundIsReadOnEveryStep()
    {
        ExecutionHarness h = Named("E", movesPerFrame: 1);
        Assert.Equal(0, h.Executor.Step());
        Assert.Equal(1, h.Executor.Step());
        Assert.Equal(1, h.Executor.Step());
        int readsBefore = h.Source.IntCalls;

        h.Source.WithInt(SettingKeys.MovesPerFrame, 20);
        int done = h.Executor.Step();

        Assert.Equal(4, done);
        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);
        Assert.True(h.Source.IntCalls > readsBefore, "The step did not read the settings.");
    }

    // GW-U-923 (AC-3, contract 5.3 row 4): a bound below 1 is treated as 1 and the run still completes.
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ABoundBelowOneIsTreatedAsOne(int movesPerFrame)
    {
        ExecutionHarness h = Named("E", movesPerFrame: movesPerFrame);

        List<int> steps = h.Run(1);

        Assert.Equal(1, steps.Max());
        Assert.Equal(6, steps.Sum());
        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);
        h.AssertConserved();
    }

    // GW-U-924 (AC-3, contract 5.3 row 5): a plan without moves completes on the first step with no layer call.
    [Fact]
    public void AnEmptyPlanCompletesOnTheFirstStep()
    {
        ExecutionHarness h = For(OrganizeFixtures.Make(new ContainerSnapshot[0]));

        Assert.Equal(0, h.Executor.Step());

        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);
        Assert.True(h.Executor.IsComplete);
        Assert.Equal(0, h.Result.ChestsPlanned);
        Assert.Equal(0, h.Result.MovesCompleted);
        Assert.Empty(h.Recorder.Calls);
    }

    // GW-U-924 (AC-3): a plan whose only work is in-chest compaction (no moves) completes without touching the layer.
    [Fact]
    public void APlanWithOnlyCompactionCompletesWithoutLayerCalls()
    {
        ExecutionHarness h = For(OrganizeFixtures.Compaction("601"));
        Assert.Empty(h.Plan.Moves);

        Assert.Equal(0, h.Executor.Step());

        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);
        Assert.Empty(h.Recorder.Calls);
        h.AssertUntouched(601);
    }
}
