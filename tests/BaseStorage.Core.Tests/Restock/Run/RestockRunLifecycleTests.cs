namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize;
using BaseStorage.Core.Tests.Organize.Running;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;
using static BaseStorage.Core.Tests.Restock.Run.RunFixtures;

/// <summary>GW-U-2614 and 2628: one summary line per run, and no idle work with bounded steps (S-QR-002 contract 4.4, RR12 and RR14).</summary>
public sealed class RestockRunLifecycleTests
{
    /// <summary>GW-U-2614: every scenario of 10.2, 10.6 to 10.10, 10.12 and 10.13 ends with exactly one TopLeft summary notice and one Info line with the same text.</summary>
    /// <param name="scenario">The scenario name.</param>
    [Theory]
    [InlineData("keep-false")]
    [InlineData("keep-true")]
    [InlineData("refuse")]
    [InlineData("partial")]
    [InlineData("remove-fail-rolled-back")]
    [InlineData("remove-fail-rollback-fails")]
    [InlineData("denied")]
    [InlineData("corrupt")]
    [InlineData("nothing")]
    public void RR12_EveryRunEndsInOneLine(string scenario)
    {
        RunHarness harness = Harness(keepChestsCraftable: scenario == "keep-true");
        harness.Port.RestockArrange = fake => Arrange(fake, scenario);
        if (scenario == "nothing")
        {
            harness.Port.Carried = new System.Collections.Generic.List<BaseStorage.Core.Restock.CarriedStack> { H(S("Wood", 50)), U(S("Stone", 10)) };
        }

        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));
        RunToEnd(harness);

        string line = Assert.Single(Summaries(harness));
        Assert.Equal(new[] { line }, InfoLines(harness));
        Assert.Equal(OrganizeNoticePlacement.TopLeft, harness.Port.Notices.Single(n => n.Key == "message.restock.summary").Placement);
    }

    /// <summary>GW-U-2614: a world that goes away mid-run shows no notice and no Info line.</summary>
    [Fact]
    public void RR12_AWorldGoneTickShowsNoLine()
    {
        RunHarness harness = Harness();
        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));

        harness.Port.InWorld = false;
        RunToEnd(harness);

        Assert.Empty(Summaries(harness));
        Assert.Empty(InfoLines(harness));
    }

    /// <summary>GW-U-2614: a tick that throws ends the run with one "stopped early" line and releases the lock.</summary>
    [Fact]
    public void RR12_AThrowingTickEndsWithOneStoppedLineAndReleasesTheLock()
    {
        RunHarness harness = Harness();
        harness.Port.RestockArrange = fake => fake.ThrowOnCall = new InvalidOperationException("boom");
        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));

        _ = Record.Exception(() => harness.Coordinator.Tick());

        string line = Assert.Single(Summaries(harness));
        Assert.EndsWith("; stopped early", line, StringComparison.Ordinal);
        Assert.Equal(new[] { line }, InfoLines(harness));
        Assert.Empty(harness.Store.Contents());
        Assert.Equal(OrganizeRunState.Idle, harness.Coordinator.State);
    }

    /// <summary>GW-U-2628: 1,000 idle ticks allocate nothing and call no port member; a running restock adds at most max(1, MovesPerFrame) stacks per tick.</summary>
    /// <param name="moves">The MovesPerFrame value.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void RR14_NoIdleWorkAndAtMostTheBudgetPerTick(int moves)
    {
        var input = OrganizeFixtures.Make(Chests(), Keep803());
        RunHarness idle = RunHarness.Make(input, movesPerFrame: moves);
        idle.Port.Carried = Carried();
        int carried = idle.Port.CarriedCalls;
        int chests = idle.Port.ChestCalls;
        int gathers = idle.Port.GatherCalls.Count;
        int layers = idle.Port.LayerCalls.Count;
        int notices = idle.Port.Notices.Count;

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            idle.Coordinator.Tick();
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(carried, idle.Port.CarriedCalls);
        Assert.Equal(chests, idle.Port.ChestCalls);
        Assert.Equal(gathers, idle.Port.GatherCalls.Count);
        Assert.Equal(layers, idle.Port.LayerCalls.Count);
        Assert.Equal(notices, idle.Port.Notices.Count);

        RunHarness running = RunHarness.Make(input, movesPerFrame: moves);
        running.Port.Carried = Carried();
        Assert.Equal(OrganizeStartOutcome.Started, running.Coordinator.StartRestock(Player));
        int bound = Math.Max(1, moves);
        int ticks = 0;
        while (running.Coordinator.State != OrganizeRunState.Idle)
        {
            Assert.True(ticks++ < 500, "The restock did not end within 500 ticks.");
            int addsBefore = AddLines(running);
            running.Coordinator.Tick();
            Assert.True(AddLines(running) - addsBefore <= bound, "One tick added more than " + bound + " stacks.");
        }
    }

    /// <summary>GW-U-2598 (coordinator): a doubtful re-read writes one Error line, "Restock stopped: ConfirmFailed; unrecovered items: 0", beside its summary.</summary>
    [Fact]
    public void RR8_AConfirmFailedRunWritesOneErrorLine()
    {
        RunHarness harness = Harness();
        harness.Port.RestockArrange = fake => Arrange(fake, "corrupt");
        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));
        RunToEnd(harness);

        Assert.Equal(new[] { "Restock stopped: ConfirmFailed; unrecovered items: 0" }, ErrorLines(harness));
        Assert.Equal("Nothing restocked; 60 items left behind; stopped early", Assert.Single(Summaries(harness)));
    }

    /// <summary>GW-U-2604 (coordinator): a failed remove whose rollback fails writes one Error line with the 30 unrecovered items.</summary>
    [Fact]
    public void RR9_ARollbackFailureWritesOneErrorLineWithThirtyUnrecovered()
    {
        RunHarness harness = Harness();
        harness.Port.RestockArrange = fake => Arrange(fake, "remove-fail-rollback-fails");
        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));
        RunToEnd(harness);

        Assert.Equal(new[] { "Restock stopped: RollbackFailed; unrecovered items: 30" }, ErrorLines(harness));
        Assert.Equal("Nothing restocked; 60 items left behind; stopped early", Assert.Single(Summaries(harness)));
    }

    private static List<string> ErrorLines(RunHarness harness) =>
        harness.Sink.Entries.Where(e => e.Severity == LogSeverity.Error).Select(e => e.Message).ToList();

    private static int AddLines(RunHarness harness) =>
        harness.Port.RestockFake is null ? 0 : harness.Port.RestockFake.Calls.Count(line => line.StartsWith("Add ", StringComparison.Ordinal));

    private static void Arrange(FakeRestockLayer fake, string scenario)
    {
        switch (scenario)
        {
            case "refuse": fake.SetMode(0, RestockAddMode.Refuse); break;
            case "partial": fake.SetMode(0, RestockAddMode.PartialOnce, 25); break;
            case "remove-fail-rolled-back": fake.FailRemoveOnce(801, 0, BaseStorage.Core.Abstractions.ContainerOperationResult.ContainerChanged); break;
            case "remove-fail-rollback-fails":
                fake.FailRemoveOnce(801, 0, BaseStorage.Core.Abstractions.ContainerOperationResult.ContainerChanged);
                fake.RollbackFails = true;
                break;
            case "denied": fake.SetReservation(801, BaseStorage.Core.Abstractions.ReservationStatus.Denied); break;
            case "corrupt": fake.SetMode(0, RestockAddMode.Corrupt); break;
        }
    }
}
