namespace BaseStorage.Core.Tests.Simulation;

using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize;
using Xunit.Sdk;

/// <summary>
/// GW-U-1457 (S-E005-004, AC-5.2, ADR-GWIN-114): the ownership authority the simulation runs on models the vanilla handshake with one
/// holder per chest: a race ends in one grant and one denial, a release lets the next request through, a lost owner fails the holder,
/// a vanilla UI open denies others and fast-paths the opener, a disconnect leaves requests unanswered until the server reassigns,
/// an unreachable owner never answers, a former owner is granted without a request, and a write by a non-holder is refused.
/// </summary>
public sealed class ReservationAuthorityTests
{
    private static readonly ContainerId ChestA = new ContainerId(3141592653L, 201u);
    private static readonly ContainerId ChestB = new ContainerId(3141592653L, 202u);

    // GW-U-1457
    [Fact]
    public void TwoRequestsOnOneChestEndInOneGrantAndOneInUseDenialAndTheReleaseLetsTheOtherThrough()
    {
        (ReservationAuthority authority, List<string> log) = Make(denyPct: 0);
        authority.Tick(0);
        authority.Request(0, ChestA);
        authority.Request(1, ChestA);
        Assert.Equal(ReservationStatus.Pending, authority.Status(0, ChestA));
        Assert.Equal(ReservationStatus.Pending, authority.Status(1, ChestA));
        Assert.False(authority.IsInUseByOther(1, ChestA));

        authority.Tick(1);
        Assert.Equal(ReservationStatus.Granted, authority.Status(0, ChestA));
        Assert.Equal(ReservationStatus.Denied, authority.Status(1, ChestA));
        Assert.Equal(0, authority.HolderOf(ChestA));
        Assert.Equal(ReservationAuthority.HoldKind.Reservation, authority.KindOf(ChestA));
        Assert.Equal(ReservationAuthority.SessionOf(0), authority.OwnerOf(ChestA));
        Assert.True(authority.IsInUseByOther(1, ChestA));
        Assert.False(authority.IsInUseByOther(0, ChestA));
        Assert.Equal(1, authority.Grants);
        Assert.Equal(1, authority.Denials);
        authority.AssertExclusive();

        // A third request while held fails the pre-check at once, without a round trip.
        authority.Request(2, ChestA);
        Assert.Equal(ReservationStatus.Denied, authority.Status(2, ChestA));
        Assert.Equal(2, authority.Denials);

        authority.Release(0, ChestA);
        Assert.Equal(ReservationStatus.None, authority.Status(0, ChestA));
        Assert.Equal(-1, authority.HolderOf(ChestA));
        Assert.Equal(ReservationAuthority.SessionOf(0), authority.OwnerOf(ChestA));
        Assert.False(authority.IsInUseByOther(1, ChestA));

        authority.Request(1, ChestA);
        Assert.Equal(ReservationStatus.Pending, authority.Status(1, ChestA));
        authority.Tick(2);
        Assert.Equal(ReservationStatus.Granted, authority.Status(1, ChestA));
        Assert.Equal(1, authority.HolderOf(ChestA));
        Assert.Contains("t1 grant Erik #201", log);
        Assert.Contains("t1 deny-in-use Astrid #201 holder Erik", log);
        Assert.Contains("t1 deny-precheck Bjorn #201 holder Erik", log);
        Assert.Contains("t1 release Erik #201", log);
    }

    // GW-U-1457
    [Fact]
    public void AFormerOwnerIsGrantedWithoutARequestAndTheServerCanTakeAHoldAway()
    {
        (ReservationAuthority authority, _) = Make(denyPct: 0);
        authority.Tick(0);
        authority.Request(0, ChestA);
        authority.Tick(1);
        authority.Release(0, ChestA);

        authority.Request(0, ChestA);
        Assert.Equal(ReservationStatus.Granted, authority.Status(0, ChestA));
        Assert.Equal(1, authority.LocalOwnerGrants);
        Assert.True(authority.AllowWrite(0, ChestA));

        Assert.True(authority.LoseOwner(ChestA));
        Assert.Equal(ReservationStatus.Lost, authority.Status(0, ChestA));
        Assert.Equal(-1, authority.HolderOf(ChestA));
        Assert.Equal(ReservationAuthority.GhostOwner, authority.OwnerOf(ChestA));
        Assert.False(authority.AllowWrite(0, ChestA));
        Assert.Equal(1, authority.LostHolds);

        // The other peer's request now goes to the ghost owner and is granted; the loser restarts from Lost.
        authority.Request(1, ChestA);
        authority.Request(0, ChestA);
        Assert.Equal(ReservationStatus.Pending, authority.Status(0, ChestA));
        authority.Tick(2);
        Assert.Equal(ReservationStatus.Granted, authority.Status(1, ChestA));
        Assert.Equal(ReservationStatus.Denied, authority.Status(0, ChestA));
        Assert.False(authority.LoseOwner(ChestB) || authority.HolderOf(ChestB) >= 0);
        authority.AssertExclusive();
    }

    // GW-U-1457
    [Fact]
    public void AVanillaUiOpenDeniesOthersFastPathsTheOpenerAndClosesOnlyAfterItsReservationIsReleased()
    {
        (ReservationAuthority authority, _) = Make(denyPct: 0);
        authority.Tick(0);
        Assert.True(authority.OpenUi(1, ChestA, 3));
        Assert.False(authority.OpenUi(0, ChestA, 3));
        Assert.Equal(ReservationAuthority.HoldKind.UiOpen, authority.KindOf(ChestA));
        Assert.True(authority.IsInUseByOther(0, ChestA));
        Assert.False(authority.IsInUseByOther(1, ChestA));

        authority.Request(0, ChestA);
        Assert.Equal(ReservationStatus.Denied, authority.Status(0, ChestA));

        authority.Request(1, ChestA);
        Assert.Equal(ReservationStatus.Granted, authority.Status(1, ChestA));
        Assert.Equal(1, authority.SelfOpenGrants);
        Assert.True(authority.AllowWrite(1, ChestA));

        authority.Tick(3);
        authority.Tick(4);
        Assert.Equal(1, authority.HolderOf(ChestA));
        Assert.Equal(ReservationAuthority.HoldKind.UiOpen, authority.KindOf(ChestA));

        authority.Release(1, ChestA);
        Assert.Equal(1, authority.HolderOf(ChestA));
        authority.Tick(5);
        Assert.Equal(-1, authority.HolderOf(ChestA));
        Assert.Equal(ReservationAuthority.HoldKind.None, authority.KindOf(ChestA));
        Assert.Equal(ReservationAuthority.SessionOf(1), authority.OwnerOf(ChestA));
        Assert.Equal(1, authority.UiOpens);
    }

    // GW-U-1457
    [Fact]
    public void ADisconnectedHolderNoLongerBlocksButRequestsGoUnansweredUntilTheServerReassigns()
    {
        (ReservationAuthority authority, List<string> log) = Make(denyPct: 0, reassignDelay: 5);
        authority.Tick(0);
        authority.Request(0, ChestA);
        authority.Tick(1);
        Assert.Equal(ReservationStatus.Granted, authority.Status(0, ChestA));

        authority.DisconnectPeer(0);
        Assert.Equal(ReservationStatus.None, authority.Status(0, ChestA));
        Assert.False(authority.HasAnyReservation(0));
        Assert.False(authority.IsInUseByOther(1, ChestA));
        Assert.Equal(1, authority.Disconnects);

        authority.Request(1, ChestA);
        authority.Tick(2);
        Assert.Equal(ReservationStatus.Pending, authority.Status(1, ChestA));
        Assert.Equal(1, authority.NoResponses);
        Assert.Contains("t2 no-response Astrid #201 owner away", log);

        authority.Release(1, ChestA);
        authority.Tick(6);
        Assert.Equal(ReservationAuthority.GhostOwner, authority.OwnerOf(ChestA));
        authority.Request(1, ChestA);
        authority.Tick(7);
        Assert.Equal(ReservationStatus.Granted, authority.Status(1, ChestA));
        Assert.True(authority.HasAnyReservation(1));
    }

    // GW-U-1457
    [Fact]
    public void AnUnreachableOwnerNeverAnswersAndAccessDenialsComeFromTheOwner()
    {
        (ReservationAuthority authority, _) = Make(denyPct: 0);
        authority.Tick(0);
        Assert.True(authority.SetOwnerAway(ChestA, 3));
        Assert.False(authority.SetOwnerAway(ChestA, 3));
        authority.Request(0, ChestA);
        authority.Tick(1);
        Assert.Equal(ReservationStatus.Pending, authority.Status(0, ChestA));
        Assert.Equal(1, authority.NoResponses);
        authority.Tick(3);
        Assert.Equal(ReservationStatus.Pending, authority.Status(0, ChestA));
        authority.Release(0, ChestA);
        authority.Request(0, ChestA);
        authority.Tick(4);
        Assert.Equal(ReservationStatus.Granted, authority.Status(0, ChestA));

        (ReservationAuthority denying, List<string> log) = Make(denyPct: 100);
        denying.Tick(0);
        denying.Request(0, ChestB);
        denying.Tick(1);
        Assert.Equal(ReservationStatus.Denied, denying.Status(0, ChestB));
        Assert.Equal(-1, denying.HolderOf(ChestB));
        Assert.Contains("t1 deny-access Erik #202", log);
        Assert.False(denying.AllowWrite(0, ChestB));
    }

    // GW-U-1457
    [Fact]
    public void TheExclusivityAssertionFiresOnAWriteByANonHolder()
    {
        (ReservationAuthority authority, _) = Make(denyPct: 0);
        authority.Tick(0);
        authority.Request(0, ChestA);
        authority.Tick(1);
        Assert.True(authority.AllowWrite(0, ChestA));
        Assert.False(authority.AllowWrite(1, ChestA));
        authority.AssertExclusive();

        // Only a broken model can grant two peers at once; the authority's own grant path refuses it.
        authority.Release(0, ChestA);
        authority.Request(1, ChestA);
        authority.Tick(2);
        Assert.True(authority.AllowWrite(1, ChestA));
        Assert.False(authority.AllowWrite(0, ChestA));
        Assert.False(authority.OpenUi(0, ChestA, 1));
        authority.AssertExclusive();
    }

    private static (ReservationAuthority Authority, List<string> Log) Make(int denyPct, int reassignDelay = 20)
    {
        var log = new List<string>();
        var authority = new ReservationAuthority(new FuzzRandom(42UL), 3, 1, denyPct, reassignDelay, log);
        authority.Register(ChestA);
        authority.Register(ChestB);
        return (authority, log);
    }
}
