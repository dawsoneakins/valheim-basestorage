namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Restock.Run;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;
using static BaseStorage.Core.Tests.Restock.Run.RunFixtures;

/// <summary>GW-U-2589..2598, 2601, 2604, 2608 and 2609: the executor over the recording layer (S-QR-002 contract section 5, RR6 to RR11).</summary>
public sealed class RestockExecutorTests
{
    /// <summary>GW-U-2589: every add and remove falls between its Req and Release; a Pending answer that resolves to Granted runs the moves after the grant.</summary>
    [Fact]
    public void RR11_MovesHappenOnlyInsideAGrantedReservation()
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.SetReservation(801, ReservationStatus.Pending, ReservationStatus.Pending, ReservationStatus.Granted);
        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);

        Drive(executor, clock);

        Assert.Equal(
            new[]
            {
                "Req 801", "Add t0 30", "Remove 801#0 30", "Add t1 20", "Remove 801#1 20", "Release 801",
                "Req 802", "Add t0 10", "Remove 802#0 10", "Release 802",
            },
            layer.Writes());
        Assert.Equal(1, layer.Writes().Count(line => line == "Req 801"));
        Assert.Equal(1, layer.Writes().Count(line => line == "Release 801"));
        Assert.Equal(1, layer.Writes().Count(line => line == "Req 802"));
        Assert.Equal(1, layer.Writes().Count(line => line == "Release 802"));
    }

    /// <summary>GW-U-2590: a denied, lost, unanswered or timed-out chest, or a missing chest, is skipped; its moves count as left and the run continues.</summary>
    /// <param name="case">The reservation answer, or Missing for a chest the live base no longer has.</param>
    [Theory]
    [InlineData("Denied")]
    [InlineData("Lost")]
    [InlineData("None")]
    [InlineData("Pending")]
    [InlineData("Missing")]
    public void RR11_ADoubtfulChestIsSkippedAndTheRunContinues(string @case)
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        IEnumerable<ContainerSnapshot> chests = @case == "Missing" ? Chests().Where(c => c.Id != Id(801)) : Chests();
        var layer = new FakeRestockLayer(Carried(), chests);
        switch (@case)
        {
            case "Denied": layer.SetReservation(801, ReservationStatus.Denied); break;
            case "Lost": layer.SetReservation(801, ReservationStatus.Lost); break;
            case "None": layer.SetReservation(801, ReservationStatus.None); break;
            case "Pending": layer.SetReservation(801, ReservationStatus.Pending); break;
        }

        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);
        Drive(executor, clock);

        Assert.DoesNotContain("Remove 801#0 30", layer.Writes());
        Assert.DoesNotContain("Remove 801#1 20", layer.Writes());
        Assert.Contains("Add t0 10", layer.Writes());
        Assert.Equal(10, executor.Result.MovedItems);
        Assert.Equal(1, executor.Result.ToppedUpStacks);
        Assert.Equal(50, executor.Result.LeftBehind);
        Assert.Equal("Restocked 1 stack, 10 items; 50 items left behind", RestockRunText.Summary(executor.Result));
    }

    /// <summary>GW-U-2595: an add that adds nothing never removes from the chest; t1 still moves and the line counts the 40 items left.</summary>
    [Fact]
    public void RR6_AnUnconfirmedAddNeverTouchesTheChest()
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.SetMode(0, RestockAddMode.Refuse);
        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);

        Drive(executor, clock);

        Assert.Contains("Add t0 30", layer.Writes());
        Assert.Contains("Add t0 10", layer.Writes());
        Assert.DoesNotContain("Remove 801#0 30", layer.Writes());
        Assert.DoesNotContain("Remove 802#0 10", layer.Writes());
        Assert.Contains("Remove 801#1 20", layer.Writes());
        Assert.Equal(30, layer.Chest(801)[0]!.Count);
        Assert.Equal(50, layer.Chest(802)[0]!.Count);
        Assert.Equal(20, executor.Result.MovedItems);
        Assert.Equal(1, executor.Result.ToppedUpStacks);
        Assert.Equal(40, executor.Result.LeftDuringRun);
        Assert.Equal("Restocked 1 stack, 20 items; 40 items left behind", RestockRunText.Summary(executor.Result));
    }

    /// <summary>GW-U-2596: the accounting follows the re-read: a true return with no items gives no remove, and a false return with every item gives the full remove.</summary>
    /// <param name="mode">The add mode of t0.</param>
    [Theory]
    [InlineData("ReturnsTrueAddsNothing")]
    [InlineData("ReturnsFalseAddsAll")]
    public void RR6_AccountingUsesTheReReadNotTheReturnValue(string mode)
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.SetMode(0, mode == "ReturnsTrueAddsNothing" ? RestockAddMode.ReturnsTrueAddsNothing : RestockAddMode.ReturnsFalseAddsAll);
        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);

        Drive(executor, clock);

        List<string> calls = layer.Calls;
        for (int r = 0; r < calls.Count; r++)
        {
            if (!calls[r].StartsWith("Remove ", StringComparison.Ordinal))
            {
                continue;
            }

            int count = int.Parse(calls[r].Split(' ')[2], System.Globalization.CultureInfo.InvariantCulture);
            int? delta = layer.ConfirmedDelta(r);
            Assert.True(
                delta is not null && delta >= count,
                "The remove " + calls[r] + " is not confirmed by a re-read of at least " + count + " (delta " + (delta?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none") + ").");
        }

        if (mode == "ReturnsTrueAddsNothing")
        {
            Assert.DoesNotContain("Remove 801#0 30", layer.Writes());
            Assert.DoesNotContain("Remove 802#0 10", layer.Writes());
        }
        else
        {
            Assert.Contains("Remove 801#0 30", layer.Writes());
        }
    }

    /// <summary>GW-U-2598: a doubtful re-read stops the run before any remove, and every unattempted item counts as left.</summary>
    /// <param name="mode">The doubt: a null re-read, a changed identity, a negative delta or a delta above count.</param>
    [Theory]
    [InlineData("Vanish")]
    [InlineData("Corrupt")]
    [InlineData("Shrink")]
    [InlineData("Overfill")]
    public void RR8_ADoubtfulReReadStopsTheRunWithoutRemoving(string mode)
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.SetMode(0, Enum.Parse<RestockAddMode>(mode));
        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);

        Drive(executor, clock);

        Assert.Equal(ExecutionStatus.Stopped, executor.Status);
        Assert.Equal(RestockStopReason.ConfirmFailed, executor.Result.StopReason);
        Assert.DoesNotContain("Remove 801#0 30", layer.Writes());
        Assert.Equal(0, executor.Result.UnrecoveredItems);
        Assert.Equal(0, executor.Result.MovedItems);
        Assert.Equal(60, executor.Result.LeftBehind);
        Assert.Equal("Nothing restocked; 60 items left behind; stopped early", RestockRunText.Summary(executor.Result));
    }

    /// <summary>GW-U-2598: a re-read that shows no change counts as left, and the run does not stop.</summary>
    [Fact]
    public void RR8_ZeroDeltaCountsAsLeftWithoutStopping()
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.SetMode(0, RestockAddMode.ReturnsTrueAddsNothing);
        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);

        Drive(executor, clock);

        Assert.Equal(ExecutionStatus.Completed, executor.Status);
        Assert.Equal(RestockStopReason.None, executor.Result.StopReason);
        Assert.Equal(40, executor.Result.LeftDuringRun);
    }

    /// <summary>GW-U-2601: a partial merge is counted exactly: 25 merged and removed from 801, 5 left, and the items are conserved.</summary>
    [Fact]
    public void RR10_APartialMergeThenFailConservesEveryItem()
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.SetMode(0, RestockAddMode.PartialOnce, 25);
        RestockPlan plan = Plan(Carried(), Chests());
        RestockExecutor executor = Executor(plan, layer, clock);

        Drive(executor, clock);

        Assert.Equal(
            new[]
            {
                "Req 801", "Add t0 30", "Remove 801#0 25", "Add t1 20", "Remove 801#1 20", "Release 801",
                "Req 802", "Add t0 10", "Remove 802#0 10", "Release 802",
            },
            layer.Writes());
        Assert.Equal(55, executor.Result.MovedItems);
        Assert.Equal(2, executor.Result.ToppedUpStacks);
        Assert.Equal(5, executor.Result.LeftDuringRun);
        Assert.Equal(plan.MovedItems, executor.Result.MovedItems + executor.Result.LeftDuringRun);
        Assert.Equal("Restocked 2 stacks, 55 items; 5 items left behind", RestockRunText.Summary(executor.Result));
        Assert.Equal(45, layer.Carried[0]!.Count);
        Assert.Equal(25, layer.Carried[1]!.Count);
        Assert.Equal(5, layer.Chest(801)[0]!.Count);
        Assert.Equal(40, layer.Chest(802)[0]!.Count);
        Assert.Null(layer.Chest(801)[1]);
    }

    /// <summary>GW-U-2604: a failed remove is rolled back; the chest's other move is skipped, and the run continues at the next chest.</summary>
    [Fact]
    public void RR9_AFailedRemoveIsRolledBackOrReportedAsUnrecovered()
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.FailRemoveOnce(801, 0, ContainerOperationResult.ContainerChanged);
        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);

        Drive(executor, clock);

        Assert.Equal(
            new[]
            {
                "Req 801", "Add t0 30", "Remove 801#0 30", "Unadd t0 30", "Release 801",
                "Req 802", "Add t0 10", "Remove 802#0 10", "Release 802",
            },
            layer.Writes());
        Assert.Equal(20, layer.Carried[0]!.Count);
        Assert.Equal(10, executor.Result.MovedItems);
        Assert.Equal(1, executor.Result.ToppedUpStacks);
        Assert.Equal(50, executor.Result.LeftDuringRun);
        Assert.Equal("Restocked 1 stack, 10 items; 50 items left behind", RestockRunText.Summary(executor.Result));
    }

    /// <summary>
    /// GW-U-2604 (part): a NotReserved remove is rolled back like any failed remove, and the rest of its chest is left behind.
    /// The OwnerLost step of contract 5.3.7 is not asserted here: the executor's reservation state is private (see the row note).
    /// </summary>
    [Fact]
    public void RR9_ANotReservedRemoveIsRolledBackAndSkipsTheRestOfTheChest()
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.FailRemoveOnce(801, 0, ContainerOperationResult.NotReserved);
        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);

        Drive(executor, clock);

        Assert.Equal(
            new[]
            {
                "Req 801", "Add t0 30", "Remove 801#0 30", "Unadd t0 30", "Release 801",
                "Req 802", "Add t0 10", "Remove 802#0 10", "Release 802",
            },
            layer.Writes());
        Assert.DoesNotContain("Add t1 20", layer.Writes());
        Assert.Equal(20, layer.Carried[0]!.Count);
        Assert.Equal(10, executor.Result.MovedItems);
        Assert.Equal(50, executor.Result.LeftDuringRun);
        Assert.Equal(ExecutionStatus.Completed, executor.Status);
        Assert.Equal("Restocked 1 stack, 10 items; 50 items left behind", RestockRunText.Summary(executor.Result));
    }

    /// <summary>GW-U-2604: a failing rollback is reported: the unrecovered items are 30, the run stops, and nothing is restocked.</summary>
    [Fact]
    public void RR9_AFailingRollbackStopsWithUnrecoveredItems()
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.FailRemoveOnce(801, 0, ContainerOperationResult.ContainerChanged);
        layer.RollbackFails = true;
        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);

        Drive(executor, clock);

        Assert.Equal(RestockStopReason.RollbackFailed, executor.Result.StopReason);
        Assert.Equal(30, executor.Result.UnrecoveredItems);
        Assert.Equal("Nothing restocked; 60 items left behind; stopped early", RestockRunText.Summary(executor.Result));
    }

    /// <summary>GW-U-2608: a player stack that changed before the first add (by count or by identity) moves nothing; its moves count as left and the other chest's move runs.</summary>
    /// <param name="change">The change: "count" or "identity".</param>
    [Theory]
    [InlineData("count")]
    [InlineData("identity")]
    public void RR7_AStalePlayerStackMovesNothingAndIsLeftBehind(string change)
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.MutateCarried(0, change == "count" ? S("Wood", 11) : WoodWl2(10));
        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);

        Drive(executor, clock);

        Assert.DoesNotContain(layer.Writes(), line => line.StartsWith("Add t0", StringComparison.Ordinal));
        Assert.DoesNotContain("Req 802", layer.Writes());
        Assert.Contains("Remove 801#1 20", layer.Writes());
        Assert.Equal(1, executor.Result.StaleStacks);
        Assert.Equal(20, executor.Result.MovedItems);
        Assert.Equal(1, executor.Result.ToppedUpStacks);
        Assert.Equal(40, executor.Result.LeftBehind);
        Assert.Equal(30, layer.Chest(801)[0]!.Count);
        Assert.Equal(50, layer.Chest(802)[0]!.Count);
        Assert.Equal("Restocked 1 stack, 20 items; 40 items left behind", RestockRunText.Summary(executor.Result));
    }

    /// <summary>GW-U-2609: every difference marks the target stale once; a chest with a stale and a live target is still reserved and only the live move runs.</summary>
    /// <param name="change">The difference: "null", "prefab", "quality", "max", "identity" or "count".</param>
    [Theory]
    [InlineData("null")]
    [InlineData("prefab")]
    [InlineData("quality")]
    [InlineData("max")]
    [InlineData("identity")]
    [InlineData("count")]
    public void RR7_AnyDifferenceMarksTheTargetStaleOnce(string change)
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        ItemStackSnapshot? replacement = change switch
        {
            "null" => null,
            "prefab" => S("Stone", 10),
            "quality" => S("Wood", 10, 50, 2),
            "max" => S("Wood", 10, 100, 1),
            "identity" => WoodWl2(10),
            _ => S("Wood", 11),
        };
        layer.MutateCarried(0, replacement);
        RestockExecutor executor = Executor(Plan(Carried(), Chests()), layer, clock);

        Drive(executor, clock);

        Assert.Equal(1, executor.Result.StaleStacks);
        Assert.Contains("Req 801", layer.Writes());
        Assert.DoesNotContain(layer.Writes(), line => line.StartsWith("Add t0", StringComparison.Ordinal));
        Assert.Contains("Add t1 20", layer.Writes());
        Assert.Equal(20, executor.Result.MovedItems);
        Assert.Equal(40, executor.Result.LeftBehind);
    }

    /// <summary>GW-U-2608 (RR13): LeftBehind is the plan's left-behind count plus the items left during the run.</summary>
    [Fact]
    public void RR13_LeftBehindCountsThePlanAndTheRunTogether()
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        layer.SetMode(0, RestockAddMode.PartialOnce, 25);
        RestockPlan plan = Plan(Carried(), Chests());
        RestockExecutor executor = Executor(plan, layer, clock);

        Drive(executor, clock);

        Assert.Equal(plan.LeftBehind + executor.Result.LeftDuringRun, executor.Result.LeftBehind);
    }
    private static RestockExecutor Executor(RestockPlan plan, FakeRestockLayer layer, ManualClock clock)
    {
        var settings = new FakeSettingsSource()
            .WithInt(SettingKeys.MovesPerFrame, 2)
            .WithInt(SettingKeys.ReserveTimeoutMs, 1500);
        return new RestockExecutor(plan, layer, clock, new BaseStorageSettings(settings));
    }

    private static void Drive(RestockExecutor executor, ManualClock clock)
    {
        int steps = 0;
        while (!executor.IsComplete)
        {
            Assert.True(steps < 10000, "The executor did not complete within 10000 steps.");
            executor.Step();
            clock.Advance(TimeSpan.FromMilliseconds(100));
            steps++;
        }
    }
}
