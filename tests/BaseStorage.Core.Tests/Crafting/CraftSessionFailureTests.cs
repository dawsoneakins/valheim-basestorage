namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1120 to GW-U-1126 (AC-4; contract 5.6 S2-S11 and the Tick step order of 5.6).</summary>
public sealed class CraftSessionFailureTests
{
    private const string MaterialsMoved = "message.craft.materials-moved";

    // GW-U-1120 (S2 deny): Erik answers no; the craft fails with one notice, no Commit, and nothing consumed.
    [Fact]
    public void ADeniedChestEndsFailedWithOneNoticeAndConsumesNothing()
    {
        var rig = new CraftRig();
        IEnumerable<ContainerSnapshot> before = rig.World.Views().ToArray();
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, rig.Plan()));
        rig.Session.Tick();

        rig.Driver.OnOpenResponse(rig.Chest7, false);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.Failed, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.CraftGame.Notices);
        Assert.DoesNotContain("Commit", rig.CraftGame.Calls);
        ConservationAssert.Holds(before, rig.World.Views());
        Assert.Equal(21, rig.World.Total("Wood"));
        Assert.Equal(4, rig.World.Total("Resin"));
        Assert.All(new[] { CraftFixtures.C5, CraftFixtures.C7 }, id => Assert.Equal(ReservationStatus.None, rig.StatusOf(id)));
    }

    // GW-U-1121 (S3 moved): c5 loses 1 Wood before the grant; the craft fails ContentsChanged at c5 with one notice.
    [Fact]
    public void AChestMovedBeforeTheGrantFailsContentsChangedAtThatChest()
    {
        var rig = new CraftRig();
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, rig.Plan()));
        rig.Session.Tick();
        rig.World.MoveOut(CraftFixtures.C5, 0, 1);

        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.Failed, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.CraftGame.Notices);
        Assert.DoesNotContain("Commit", rig.CraftGame.Calls);
        Assert.Contains("Craft 1: Failed (ContentsChanged at 3141592653:5)", rig.Sink.Entries.Select(entry => entry.Message));
        Assert.Equal(20, rig.World.Total("Wood"));
    }

    // GW-U-1122 (S4 timeout): at 1499 ms the craft is still Executing.
    [Fact]
    public void AtOneMillisecondBeforeTheTimeoutTheCraftIsStillExecuting()
    {
        var rig = new CraftRig();
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, rig.Plan()));
        rig.Session.Tick();

        rig.Clock.Advance(TimeSpan.FromMilliseconds(1499));
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        Assert.Empty(rig.CraftGame.Notices);
    }

    // GW-U-1122 (S4 timeout): at 1500 ms the craft ends Failed (ReservationFailed at c7) with one notice and the world unchanged.
    [Fact]
    public void ATimeoutEndsFailedWithOneNotice()
    {
        var rig = new CraftRig();
        IEnumerable<ContainerSnapshot> before = rig.World.Views().ToArray();
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, rig.Plan()));
        rig.Session.Tick();

        rig.Clock.Advance(TimeSpan.FromMilliseconds(CraftFixtures.ReserveTimeoutMs));
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.Failed, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.CraftGame.Notices);
        Assert.Contains("Craft 1: Failed (ReservationFailed at 2718281828:7)", rig.Sink.Entries.Select(entry => entry.Message));
        ConservationAssert.Holds(before, rig.World.Views());
    }

    // GW-U-1123 (S5 unavailable): no plan gives false, Unavailable, Token 1, one notice and the executor never called.
    [Fact]
    public void AMissingPlanEndsUnavailableWithoutStartingTheExecutor()
    {
        var rig = new CraftRig();

        Assert.False(rig.Session.OnDoCrafting(rig.Selection, null));

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.Unavailable, rig.Session.LastEnd);
        Assert.Equal(1, rig.Session.Token);
        Assert.Equal(new[] { MaterialsMoved }, rig.CraftGame.Notices);
        Assert.Empty(rig.Reservations.Calls);
    }

    // GW-U-1123 (S7 pre-flight at start): a failed pre-flight gives PreFlightFailed with one notice, no executor and the world unchanged.
    [Fact]
    public void AFailedPreFlightAtTheStartEndsPreFlightFailed()
    {
        var rig = new CraftRig();
        IEnumerable<ContainerSnapshot> before = rig.World.Views().ToArray();
        rig.CraftGame.PreFlightResult = false;

        Assert.False(rig.Session.OnDoCrafting(rig.Selection, rig.Plan()));

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.PreFlightFailed, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.CraftGame.Notices);
        Assert.Empty(rig.Reservations.Calls);
        Assert.Equal(before, rig.World.Views());
    }

    // GW-U-1124 (S8 abandoned): the selection changes; the craft ends Abandoned silently and releases both chests.
    [Fact]
    public void AnAbandonedCraftEndsSilentlyAndReleasesBothChests()
    {
        var rig = new CraftRig();
        IEnumerable<ContainerSnapshot> before = rig.World.Views().ToArray();
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, rig.Plan()));
        rig.Session.Tick();

        rig.CraftGame.StillSelected = false;
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.Abandoned, rig.Session.LastEnd);
        Assert.Empty(rig.CraftGame.Notices);
        Assert.DoesNotContain("Commit", rig.CraftGame.Calls);
        Assert.All(new[] { CraftFixtures.C5, CraftFixtures.C7 }, id => Assert.Equal(ReservationStatus.None, rig.StatusOf(id)));
        Assert.Equal(before, rig.World.Views());
    }

    // GW-U-1124 (S9 repeated click): the same selection while Executing returns false and stays Executing.
    [Fact]
    public void ARepeatedClickWhileExecutingIsIgnored()
    {
        var rig = new CraftRig();
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, plan));
        rig.Session.Tick();

        Assert.False(rig.Session.OnDoCrafting(rig.Selection, plan));

        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        Assert.Equal(1, rig.Session.Token);
    }

    // GW-U-1124 (S9 different selection): the first craft ends Abandoned and a new craft starts without InvalidOperationException (CX10).
    [Fact]
    public void ADifferentSelectionEndsTheFirstAbandonedAndStartsTheNext()
    {
        var rig = new CraftRig();
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, plan));
        rig.Session.Tick();
        var other = new CraftSelection(rig.Recipe, null, rig.Station, 1, 2);

        Exception? thrown = Record.Exception(() => rig.Session.OnDoCrafting(other, plan));

        Assert.Null(thrown);
        Assert.Equal(2, rig.Session.Token);
        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        Assert.Equal(CraftEnd.Abandoned, rig.Session.LastEnd);
        Assert.Empty(rig.CraftGame.Notices);
    }

    // GW-U-1125 (S10 vanilla declines at commit): ReturnTaken puts the four stacks back; one notice; totals conserved.
    [Fact]
    public void AVanillaDeclineAtCommitReturnsTheTakenStacks()
    {
        var rig = new CraftRig();
        rig.CraftGame.CommitBody = _ => { };

        rig.RunS1();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.Returned, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.CraftGame.Notices);
        Assert.Contains("ReturnTaken", rig.CraftGame.Calls);
        Assert.Contains("Craft 1: Returned, 4 stack(s) put back", rig.Sink.Entries.Select(entry => entry.Message));
        Assert.Equal(21, rig.World.Total("Wood"));
        Assert.Equal(4, rig.World.Total("Resin"));
    }

    // GW-U-1125 (S11 commit throws): the exception propagates out of Tick, ReturnTaken still ran, the end is Returned with one notice.
    [Fact]
    public void ACommitThatThrowsStillReturnsTheTakenStacks()
    {
        var rig = new CraftRig();
        rig.CraftGame.CommitBody = _ => throw new InvalidOperationException("commit");
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, rig.Plan()));
        rig.Session.Tick();
        rig.Grant(rig.Chest7);
        rig.Driver.Tick();

        Assert.Throws<InvalidOperationException>(() => rig.Session.Tick());

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.Returned, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.CraftGame.Notices);
        Assert.Contains("ReturnTaken", rig.CraftGame.Calls);
        Assert.Equal(21, rig.World.Total("Wood"));
        Assert.Equal(4, rig.World.Total("Resin"));
    }

    // GW-U-1126 (step 4 before step 5): abandonment wins over a failing pre-flight.
    [Fact]
    public void AbandonmentWinsOverPreFlight()
    {
        var rig = new CraftRig();
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, rig.Plan()));
        rig.Session.Tick();
        rig.CraftGame.Calls.Clear();
        rig.CraftGame.StillSelected = false;
        rig.CraftGame.PreFlightResult = false;

        rig.Session.Tick();

        Assert.Equal(CraftEnd.Abandoned, rig.Session.LastEnd);
        Assert.DoesNotContain("PreFlight", rig.CraftGame.Calls);
    }

    // GW-U-1126 (step 5): a pre-flight failure mid-execution cancels, ends PreFlightFailed with one notice and releases both chests.
    [Fact]
    public void AMidExecutionPreFlightFailureCancelsAndReleasesBothChests()
    {
        var rig = new CraftRig();
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, rig.Plan()));
        rig.Session.Tick();
        rig.CraftGame.PreFlightResult = false;

        rig.Session.Tick();

        Assert.Equal(CraftEnd.PreFlightFailed, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.CraftGame.Notices);
        Assert.All(new[] { CraftFixtures.C5, CraftFixtures.C7 }, id => Assert.Equal(ReservationStatus.None, rig.StatusOf(id)));
    }

    // GW-U-1126 (step 3): a Poll that throws propagates, and the next Tick handles the Faulted result as Failed with one notice.
    [Fact]
    public void AFaultedPollIsHandledAsFailedByTheNextTick()
    {
        var rig = new CraftRig();
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, rig.Plan()));
        rig.Session.Tick();
        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.World.ThrowOnTake = new InvalidOperationException("take");

        Assert.Throws<InvalidOperationException>(() => rig.Session.Tick());
        rig.World.ThrowOnTake = null;
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.Failed, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.CraftGame.Notices);
    }
}
