namespace BaseStorage.Core.Tests.Building;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Tests.Crafting;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Plugin.Building;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1617 to GW-U-1622 (AC-7; contract 5.9 T2 to T5, T7, T8, T11 and 5.4 Tick steps 3 to 5): nothing placed, nothing consumed, one notice.</summary>
public sealed class BuildSessionFailureTests
{
    private const string MaterialsMoved = "message.craft.materials-moved";

    // GW-U-1617 (T2, AC-7): a denied grant ends Failed with one notice and no commit; the world is unchanged.
    [Fact]
    public void ADeniedGrantEndsFailedWithOneNoticeAndNoCommit()
    {
        var rig = new BuildRig();
        var before = rig.World.Views().ToArray();
        rig.StartFixture();

        rig.Driver.OnOpenResponse(rig.Chest7, false);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.Failed, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.BuildGame.Notices);
        Assert.DoesNotContain("Commit", rig.BuildGame.Calls);
        Assert.Equal(21, rig.World.Total("Wood"));
        Assert.Equal(4, rig.World.Total("Resin"));
        ConservationAssert.Holds(before, rig.World.Views());
        Assert.All(new[] { CraftFixtures.C5, CraftFixtures.C7 }, id => Assert.Equal(ReservationStatus.None, rig.StatusOf(id)));
    }

    // GW-U-1618 (T3, AC-7): a chest that changed before the grant fails with ContentsChanged; only the external move differs.
    [Fact]
    public void AChestMovedBeforeTheGrantFailsWithContentsChanged()
    {
        var rig = new BuildRig();
        rig.StartFixture();
        rig.World.MoveOut(CraftFixtures.C5, 0, 1);
        var before = rig.World.Views().ToArray();

        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.Failed, rig.Session.LastEnd);
        Assert.Contains("Build 1: Failed (ContentsChanged at 3141592653:5)", rig.Sink.Entries.Select(entry => entry.Message));
        Assert.Equal(new[] { MaterialsMoved }, rig.BuildGame.Notices);
        Assert.DoesNotContain("Commit", rig.BuildGame.Calls);
        Assert.Equal(20, rig.World.Total("Wood"));
        Assert.Equal(4, rig.World.Total("Resin"));
        ConservationAssert.Holds(before, rig.World.Views());
    }

    // GW-U-1619 (T4, AC-7): one millisecond short of the reserve timeout the build is still pending.
    [Fact]
    public void OneMillisecondShortOfTheTimeoutTheBuildIsStillPending()
    {
        var rig = new BuildRig();
        rig.StartFixture();

        rig.Clock.Advance(TimeSpan.FromMilliseconds(1499));
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        Assert.Empty(rig.BuildGame.Notices);
    }

    // GW-U-1619 (T4, AC-7): at the reserve timeout the build fails with ReservationFailed, one notice, and the world unchanged.
    [Fact]
    public void AtTheTimeoutTheBuildFailsWithReservationFailed()
    {
        var rig = new BuildRig();
        rig.StartFixture();
        var before = rig.World.Views().ToArray();

        rig.Clock.Advance(TimeSpan.FromMilliseconds(1500));
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.Failed, rig.Session.LastEnd);
        Assert.Contains("Build 1: Failed (ReservationFailed at 2718281828:7)", rig.Sink.Entries.Select(entry => entry.Message));
        Assert.Equal(new[] { MaterialsMoved }, rig.BuildGame.Notices);
        ConservationAssert.Holds(before, rig.World.Views());
        Assert.Equal(21, rig.World.Total("Wood"));
    }

    // GW-U-1620 (T5, AC-7): no plan at place time is Unavailable, with one notice, and the executor is never called.
    [Fact]
    public void NoPlanAtPlaceTimeIsUnavailableWithOneNotice()
    {
        var rig = new BuildRig();
        rig.Session.EnterGuard();

        bool run = rig.Session.OnTryPlacePiece(rig.Selection, null);

        Assert.False(run);
        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.Unavailable, rig.Session.LastEnd);
        Assert.Equal(1, rig.Session.Token);
        Assert.False(rig.Session.Guarded);
        Assert.Equal(new[] { MaterialsMoved }, rig.BuildGame.Notices);
        Assert.Empty(rig.Reservations.Calls);
    }

    // GW-U-1621 (T7, AC-7): a placement refused at the click is vanilla's to show; no notice, nothing started, the guard holds.
    [Fact]
    public void APlacementRefusedAtTheClickShowsNoNoticeAndStartsNothing()
    {
        var rig = new BuildRig();
        rig.BuildGame.PreFlightResult = false;

        Assert.True(rig.TryPlace(rig.Plan()));

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.PlacementRefused, rig.Session.LastEnd);
        Assert.True(rig.Session.Guarded);
        Assert.Empty(rig.BuildGame.Notices);
        Assert.Empty(rig.Reservations.Calls);

        Assert.False(rig.Session.OnPlacePiece(false));
        Assert.False(rig.Session.AfterTryPlacePiece(true));
        Assert.Equal(21, rig.World.Total("Wood"));
        Assert.Equal(4, rig.World.Total("Resin"));
    }

    // GW-U-1622 (T8, AC-7): an abandoned build ends silently with c5 released and the c7 request cancelled, and no commit.
    [Fact]
    public void AnAbandonedBuildEndsSilentlyAndReleasesEverything()
    {
        var rig = new BuildRig();
        rig.StartFixture();
        rig.BuildGame.StillSelected = false;

        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.Abandoned, rig.Session.LastEnd);
        Assert.Empty(rig.BuildGame.Notices);
        Assert.DoesNotContain("Commit", rig.BuildGame.Calls);
        Assert.All(new[] { CraftFixtures.C5, CraftFixtures.C7 }, id => Assert.Equal(ReservationStatus.None, rig.StatusOf(id)));
    }

    // GW-U-1622 (T11, AC-7): a pre-flight that fails while waiting ends PreFlightFailed, with one notice, cancelled and no commit.
    [Fact]
    public void APreFlightFailureWhileWaitingEndsWithOneNoticeAndCancels()
    {
        var rig = new BuildRig();
        rig.StartFixture();
        rig.BuildGame.PreFlightResult = false;

        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.PreFlightFailed, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.BuildGame.Notices);
        Assert.DoesNotContain("Commit", rig.BuildGame.Calls);
        Assert.All(new[] { CraftFixtures.C5, CraftFixtures.C7 }, id => Assert.Equal(ReservationStatus.None, rig.StatusOf(id)));
    }

    // GW-U-1622 (Tick step 4 wins over step 5): an abandonment and a failed pre-flight together end Abandoned, silently.
    [Fact]
    public void AbandonmentWinsOverPreFlightWhileWaiting()
    {
        var rig = new BuildRig();
        rig.StartFixture();
        rig.BuildGame.StillSelected = false;
        rig.BuildGame.PreFlightResult = false;

        rig.Session.Tick();

        Assert.Equal(BuildEnd.Abandoned, rig.Session.LastEnd);
        Assert.Empty(rig.BuildGame.Notices);
    }

    // GW-U-1622 (Tick step 3): a Poll that throws propagates; the next Tick handles the faulted result as Failed with one notice.
    [Fact]
    public void AFaultedPollPropagatesAndTheNextTickEndsFailed()
    {
        var rig = new BuildRig();
        rig.StartFixture();
        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.World.ThrowOnTake = new InvalidOperationException("The take failed.");

        Assert.Throws<InvalidOperationException>(() => rig.Session.Tick());
        rig.World.ThrowOnTake = null;
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.Failed, rig.Session.LastEnd);
        Assert.Equal(new[] { MaterialsMoved }, rig.BuildGame.Notices);
        Assert.DoesNotContain("Commit", rig.BuildGame.Calls);
    }
}
