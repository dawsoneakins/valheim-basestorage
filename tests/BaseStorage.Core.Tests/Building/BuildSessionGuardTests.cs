namespace BaseStorage.Core.Tests.Building;

using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1608, GW-U-1616 and GW-U-1626 (AC-2, AC-6, NFR; contract 5.4 and BF8, BF11): the gate, the guard and the idle tick.</summary>
public sealed class BuildSessionGuardTests
{
    // GW-U-1608 (5.4 step 2, AC-2): with the gate closed, the attempt is vanilla and silent, and nothing is started.
    [Fact]
    public void AClosedGateIsVanillaAndSilent()
    {
        var rig = new BuildRig();
        rig.Session.EnterGuard();

        bool run = rig.Session.OnTryPlacePiece(null, rig.Plan());

        Assert.True(run);
        Assert.False(rig.Session.Guarded);
        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Null(rig.Session.LastEnd);
        Assert.Equal(0, rig.Session.Token);
        Assert.Empty(rig.BuildGame.Calls);
        Assert.Empty(rig.BuildGame.Notices);
        Assert.Empty(rig.Reservations.Calls);
    }

    // GW-U-1608 (5.4 step 2): after a closed gate a following OnPlacePiece(false) is not guarded and returns true.
    [Fact]
    public void AfterAClosedGateAPieceCreationIsNotGuarded()
    {
        var rig = new BuildRig();
        rig.Session.EnterGuard();
        rig.Session.OnTryPlacePiece(null, rig.Plan());

        Assert.True(rig.Session.OnPlacePiece(false));
    }

    // GW-U-1616 (5.4 OnPlacePiece step 1, AfterTryPlacePiece step 1, EnterGuard): during Committing the piece creation is
    // marked and let through, the result of AfterTryPlacePiece is returned unchanged, and EnterGuard changes nothing.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DuringTheCommitTheGuardStepsAreVanilla(bool placed)
    {
        var rig = new BuildRig();
        bool? placeAnswer = null;
        bool? pieceCreated = null;
        bool? afterAnswer = null;
        bool? guardedAfterEnter = null;
        rig.BuildGame.CommitBody = _ =>
        {
            rig.Session.EnterGuard();
            guardedAfterEnter = rig.Session.Guarded;
            placeAnswer = rig.Session.OnPlacePiece(false);
            pieceCreated = rig.Session.PieceCreated;
            afterAnswer = rig.Session.AfterTryPlacePiece(placed);
        };

        rig.RunS1();

        Assert.False(guardedAfterEnter);
        Assert.True(placeAnswer);
        Assert.True(pieceCreated);
        Assert.Equal(placed, afterAnswer);
    }

    // GW-U-1616 (T14): EnterGuard then a refused creation is suppressed; AfterTryPlacePiece(true) then returns false and clears both flags.
    [Fact]
    public void AGuardedCreationWithoutItsPaymentIsSuppressed()
    {
        var rig = new BuildRig();
        rig.Session.EnterGuard();

        Assert.False(rig.Session.OnPlacePiece(false));
        Assert.True(rig.Session.Suppressed);
        Assert.False(rig.Session.AfterTryPlacePiece(true));

        Assert.False(rig.Session.Guarded);
        Assert.False(rig.Session.Suppressed);
        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(21, rig.World.Total("Wood"));
        Assert.Equal(4, rig.World.Total("Resin"));
    }

    // GW-U-1616 (T15): a creation that was paid for is let through while guarded, and AfterTryPlacePiece(true) returns true.
    [Fact]
    public void APaidCreationIsLetThroughWhileGuarded()
    {
        var rig = new BuildRig();
        rig.Session.EnterGuard();

        Assert.True(rig.Session.OnPlacePiece(true));
        Assert.True(rig.Session.AfterTryPlacePiece(true));
        Assert.Empty(rig.BuildGame.Calls);
    }

    // GW-U-1616 (5.4 Tick step 1): Tick clears both the guard and the suppression.
    [Fact]
    public void TickClearsTheGuardAndTheSuppression()
    {
        var rig = new BuildRig();
        rig.Session.EnterGuard();
        rig.Session.OnPlacePiece(false);

        rig.Session.Tick();

        Assert.False(rig.Session.Guarded);
        Assert.False(rig.Session.Suppressed);
    }

    // GW-U-1626 (T16, BF11, NFR performance): an idle tick makes no game call, writes no line and allocates nothing.
    [Fact]
    public void AnIdleTickMakesNoGameCallWritesNoLineAndAllocatesNothing()
    {
        var rig = new BuildRig();
        rig.Session.Tick();

        long before = GC.GetAllocatedBytesForCurrentThread();
        rig.Session.Tick();
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Empty(rig.BuildGame.Calls);
        Assert.Empty(rig.Sink.Entries);
        Assert.Equal(before, after);
    }
}
