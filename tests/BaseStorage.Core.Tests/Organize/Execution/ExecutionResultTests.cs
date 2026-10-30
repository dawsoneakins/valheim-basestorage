namespace BaseStorage.Core.Tests.Organize.Execution;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Locking;
using BaseStorage.Core.Tests.Settings;
using static BaseStorage.Core.Tests.Organize.Execution.ExecutionHarness;

/// <summary>
/// Story AC-5 (contract 3.2; 5.1 result row; 5.4): the result records moved, skipped with reason and no room counts, is readable at any
/// time, and the constructor validates its arguments and reads nothing.
/// </summary>
public sealed class ExecutionResultTests
{
    // GW-U-930 (AC-5, contract 5.1): the plan-derived counts of base E are available before the first step and unchanged after the run.
    [Fact]
    public void PlanDerivedCountsAreAvailableBeforeAndAfterTheRun()
    {
        ExecutionHarness h = Named("E");

        OrganizeExecutionResult before = h.Result;
        Assert.Equal(ExecutionStatus.NotStarted, before.Status);
        Assert.Equal(ExecutionStopReason.None, before.StopReason);
        Assert.Equal(2, before.NoRoomStacks);
        Assert.Equal(6L, before.NoRoomItems);
        Assert.Equal(3, before.SkippedChests);
        Assert.Same(h.Plan.Skipped, before.Skipped);
        Assert.Equal(1, before.CountSkipped(SkipReason.Keep));
        Assert.Equal(1, before.CountSkipped(SkipReason.InUse));
        Assert.Equal(1, before.CountSkipped(SkipReason.Ineligible));
        Assert.Equal(0, before.CountSkipped(SkipReason.InvalidContents));
        Assert.Equal(0, before.CountSkipped((SkipReason)99));
        Assert.Equal(3, before.ChestsPlanned);
        Assert.Equal(0, before.MovesCompleted);
        Assert.Equal(6, before.MovesRemaining);

        h.Run(20);

        OrganizeExecutionResult after = h.Result;
        Assert.NotSame(before, after);
        Assert.Equal(2, after.NoRoomStacks);
        Assert.Equal(6L, after.NoRoomItems);
        Assert.Equal(3, after.SkippedChests);
        Assert.Equal(1, after.CountSkipped(SkipReason.InUse));
        Assert.Equal("Erik", after.Skipped.Single(s => s.Kind == SkipReason.InUse).PlayerName);
        Assert.Equal(6, after.MovesCompleted);
        Assert.Equal(127L, after.ItemsMoved);
        Assert.Equal(ExecutionStatus.NotStarted, before.Status);
    }

    // GW-U-931 (AC-5): a mid-run read reports Running with the progress so far, and the stopped read after a fault keeps the counts.
    [Fact]
    public void AMidRunReadReportsProgress()
    {
        ExecutionHarness h = Named("E", movesPerFrame: 1, arrange: fake => fake.InjectFault(OrganizeFixtures.Id(208), FakeFault.DestinationFull));
        h.Executor.Step();
        h.Executor.Step();

        OrganizeExecutionResult running = h.Result;
        Assert.Equal(ExecutionStatus.Running, running.Status);
        Assert.Equal(ExecutionStopReason.None, running.StopReason);
        Assert.Equal(1, running.MovesCompleted);
        Assert.Equal(5, running.MovesRemaining);
        Assert.Equal(1, running.ChestsCompleted);

        h.Run(1);

        OrganizeExecutionResult stopped = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, stopped.Status);
        Assert.Equal(ExecutionStopReason.DestinationFull, stopped.StopReason);
        Assert.Equal(4, stopped.MovesCompleted);
        Assert.Equal(2, stopped.MovesRemaining);
        Assert.Equal(4, stopped.StacksMoved);
        Assert.Equal(2, stopped.NoRoomStacks);
        Assert.Equal(3, stopped.SkippedChests);
    }

    // GW-U-932 (AC-5): the all-skipped SkipCombined base reports every skip kind, completes at once and never touches the layer.
    [Fact]
    public void SkipKindsAreCountedPerReason()
    {
        ExecutionHarness h = Named("SkipCombined");

        OrganizeExecutionResult result = h.Result;

        Assert.Equal(16, result.SkippedChests);
        Assert.Equal(3, result.CountSkipped(SkipReason.Keep));
        Assert.Equal(5, result.CountSkipped(SkipReason.InUse));
        Assert.Equal(6, result.CountSkipped(SkipReason.Ineligible));
        Assert.Equal(2, result.CountSkipped(SkipReason.InvalidContents));
        Assert.Equal(0, result.NoRoomStacks);
        Assert.Equal(0, result.ChestsPlanned);

        h.Run(20);

        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);
        Assert.Empty(h.Recorder.Calls);
        h.AssertUntouched(501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516);
    }

    // GW-U-940 (AC-5, contract 3.1, 5.4): the constructor validates its four arguments in order with the C# parameter names.
    [Fact]
    public void ConstructorValidatesItsArgumentsInOrder()
    {
        OrganizePlan plan = OrganizeFixtures.Run(OrganizeFixtures.E());
        var layer = new FakeContainerLayer();
        var clock = new CountingClock();
        var settings = new BaseStorageSettings(new FakeSettingsSource());

        Assert.Equal("plan", Assert.Throws<ArgumentNullException>(() => new OrganizeExecutor(null!, layer, clock, settings)).ParamName);
        Assert.Equal("layer", Assert.Throws<ArgumentNullException>(() => new OrganizeExecutor(plan, null!, clock, settings)).ParamName);
        Assert.Equal("clock", Assert.Throws<ArgumentNullException>(() => new OrganizeExecutor(plan, layer, null!, settings)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new OrganizeExecutor(plan, layer, clock, null!)).ParamName);
        Assert.Equal("plan", Assert.Throws<ArgumentNullException>(() => new OrganizeExecutor(null!, null!, null!, null!)).ParamName);
    }

    // GW-U-940 (AC-5, contract 5.4): the constructor reads nothing from the clock, the settings or the layer, and exposes the plan.
    [Fact]
    public void ConstructorReadsNothing()
    {
        OrganizePlan plan = OrganizeFixtures.Run(OrganizeFixtures.E());
        var fake = new FakeContainerLayer();
        var recorder = new RecordingContainerLayer(fake);
        var clock = new CountingClock();
        var source = new FakeSettingsSource();

        var executor = new OrganizeExecutor(plan, recorder, clock, new BaseStorageSettings(source));

        Assert.Same(plan, executor.Plan);
        Assert.Equal(0, clock.Reads);
        Assert.Equal(0, source.IntCalls);
        Assert.Empty(recorder.Calls);
        Assert.Equal(ExecutionStatus.NotStarted, executor.Status);
        Assert.False(executor.IsComplete);
    }

    // GW-U-941 (AC-5, contract section 1, 2, 3): the exported surface of the namespace is exactly the four contracted types with the
    // contracted members, get-only properties, no public result constructor and the contracted enum values.
    [Fact]
    public void ExportedSurfaceIsExactlyTheContract()
    {
        const string ns = "BaseStorage.Core.Organize.Execution";
        string[] exported = typeof(OrganizeExecutor).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == ns)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                ns + ".ExecutionStatus",
                ns + ".ExecutionStopReason",
                ns + ".OrganizeExecutionResult",
                ns + ".OrganizeExecutor",
            },
            exported);

        const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        Assert.Equal(
            new[] { "Abort", "IsComplete", "Plan", "Result", "Status", "Step" },
            typeof(OrganizeExecutor).GetMembers(PublicDeclared).Where(m => m is not ConstructorInfo && !m.Name.StartsWith("get_", StringComparison.Ordinal))
                .Select(m => m.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        ConstructorInfo ctor = Assert.Single(typeof(OrganizeExecutor).GetConstructors());
        Assert.Equal(
            new[] { typeof(OrganizePlan), typeof(IContainerLayer), typeof(IClock), typeof(BaseStorageSettings) },
            ctor.GetParameters().Select(p => p.ParameterType).ToArray());

        Assert.Equal(
            new[]
            {
                "ChestsCompleted", "ChestsPlanned", "CountSkipped", "FailedChest", "FailedMove", "ItemsMoved", "MovesCompleted", "MovesRemaining",
                "NoRoomItems", "NoRoomStacks", "Skipped", "SkippedChests", "StacksMoved", "Status", "StopReason", "UnrecoveredItems",
            },
            typeof(OrganizeExecutionResult).GetMembers(PublicDeclared).Where(m => m is not ConstructorInfo && !m.Name.StartsWith("get_", StringComparison.Ordinal))
                .Select(m => m.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.Empty(typeof(OrganizeExecutionResult).GetConstructors());
        foreach (Type type in new[] { typeof(OrganizeExecutor), typeof(OrganizeExecutionResult) })
        {
            Assert.True(type.IsSealed);
            Assert.All(type.GetProperties(PublicDeclared), property => Assert.Null(property.SetMethod));
        }

        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(ExecutionStatus)));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(ExecutionStopReason)));
        Assert.Equal(new[] { "NotStarted", "Running", "Completed", "Stopped" }, Enum.GetNames(typeof(ExecutionStatus)));
        Assert.Equal(new[] { 0, 1, 2, 3 }, Enum.GetValues(typeof(ExecutionStatus)).Cast<int>().ToArray());
        Assert.Equal(
            new[] { "None", "ReservationRefused", "ReservationTimedOut", "ReservationLost", "ChestUnavailable", "ChestChanged", "DestinationFull", "RollbackFailed", "Aborted" },
            Enum.GetNames(typeof(ExecutionStopReason)));
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, Enum.GetValues(typeof(ExecutionStopReason)).Cast<int>().ToArray());
    }
}
