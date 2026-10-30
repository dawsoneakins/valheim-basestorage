namespace BaseStorage.Core.Tests.Networking.Peers;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Networking;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Tests.Reservations;

/// <summary>
/// AC-6.1 and AC-6.3 (contract version-rpc-contracts.md 3.4 and 6.1, ADR-GWIN-099..101): the peer table decides accept, mismatch,
/// missing after the deadline, disconnect after the grace, RequireSameVersion off, the client side, and does nothing without peers.
/// Time moves only through the manual clock.
/// </summary>
public sealed class PeerVersionTableTests
{
    private const string Host = "0.1.0";
    private const string Other = "0.2.0";

    private static readonly TimeSpan Start = TimeSpan.FromSeconds(100);

    // GW-U-1250 (3.4): the constants are 5 s and 1 s; construction validates its arguments and reads nothing.
    [Fact]
    public void ConstantsAndConstructionFollowTheContract()
    {
        Assert.Equal(TimeSpan.FromSeconds(5), PeerVersionTable.MissingPayloadTimeout);
        Assert.Equal(TimeSpan.FromSeconds(1), PeerVersionTable.DisconnectGrace);

        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        Assert.Equal(Host, table.LocalVersion);
        Assert.Equal(0, table.Count);
        Assert.Empty(table.PeerIds);
        Assert.False(table.HasHostMismatch);
        Assert.Equal(0, clock.Reads);

        Assert.Equal("clock", Assert.Throws<ArgumentNullException>(() => new PeerVersionTable(null!, Host)).ParamName);
        Assert.Equal("localVersion", Assert.Throws<ArgumentNullException>(() => new PeerVersionTable(clock, null!)).ParamName);
        Assert.Equal("localVersion", Assert.Throws<ArgumentException>(() => new PeerVersionTable(clock, "1.0")).ParamName);
        Assert.Equal(ModVersion.Current, new PeerVersionTable(clock, ModVersion.Current).LocalVersion);
    }

    // GW-U-1250 (3.4): Track hands out fresh ids with a deadline from the clock; Forget and Clear remove; Find reports the state.
    [Fact]
    public void TrackForgetClearAndFindKeepTheTableConsistent()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);

        long first = table.Track(false);
        clock.Advance(TimeSpan.FromSeconds(2));
        long second = table.Track(true);

        Assert.Equal(1L, first);
        Assert.Equal(2L, second);
        Assert.Equal(2, table.Count);
        Assert.Equal(new[] { 1L, 2L }, table.PeerIds);

        PeerVersionEntry entry = table.Find(first)!;
        Assert.Equal(first, entry.PeerId);
        Assert.False(entry.RemoteIsServer);
        Assert.Equal(Start + PeerVersionTable.MissingPayloadTimeout, entry.Deadline);
        Assert.Null(entry.Result);
        Assert.Null(entry.DisconnectAt);
        Assert.False(entry.IsDecided);

        PeerVersionEntry server = table.Find(second)!;
        Assert.True(server.RemoteIsServer);
        Assert.Equal(Start + TimeSpan.FromSeconds(2) + PeerVersionTable.MissingPayloadTimeout, server.Deadline);

        Assert.Null(table.Find(3L));
        Assert.True(table.Forget(first));
        Assert.False(table.Forget(first));
        Assert.Equal(new[] { 2L }, table.PeerIds);
        Assert.Equal(3L, table.Track(false));

        table.Clear();
        Assert.Equal(0, table.Count);
        Assert.Empty(table.PeerIds);
        Assert.Equal(4L, table.Track(false));
    }

    // GW-U-1251 (6.1 rows 1-3): a client payload is accepted, refused as a mismatch, or refused as missing when malformed.
    [Fact]
    public void ServerSideReceiveDecidesAcceptMismatchAndMissing()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        long same = table.Track(false);
        long different = table.Track(false);
        long garbage = table.Track(false);

        PeerVersionOutcome accepted = table.Receive(same, VersionHandshake.EncodePayloadBytes(Host), true);
        Assert.Equal(PeerVersionAction.Accept, accepted.Action);
        Assert.Equal(same, accepted.PeerId);
        Assert.False(accepted.RemoteIsServer);
        Assert.Equal(VersionCheckResult.Accepted(Host, Host), accepted.Result);
        Assert.Null(accepted.MessageKey);
        Assert.True(table.Find(same)!.IsDecided);
        Assert.Null(table.Find(same)!.DisconnectAt);

        PeerVersionOutcome mismatch = table.Receive(different, VersionHandshake.EncodePayloadBytes(Other), true);
        Assert.Equal(PeerVersionAction.RefuseMismatch, mismatch.Action);
        Assert.Equal(VersionCheckResult.Mismatch(Host, Other), mismatch.Result);
        Assert.Equal("network.version.mismatch", mismatch.MessageKey);
        Assert.Equal(Start + PeerVersionTable.DisconnectGrace, table.Find(different)!.DisconnectAt);

        PeerVersionOutcome missing = table.Receive(garbage, new byte[] { 0xFF, 0x00 }, true);
        Assert.Equal(PeerVersionAction.RefuseMissing, missing.Action);
        Assert.Equal(VersionCheckResult.Missing(Host), missing.Result);
        Assert.Equal("network.version.missing", missing.MessageKey);
        Assert.Equal(Start + PeerVersionTable.DisconnectGrace, table.Find(garbage)!.DisconnectAt);

        Assert.Equal(PeerVersionAction.RefuseMissing, table.Receive(table.Track(false), null, true).Action);
        Assert.Equal(PeerVersionAction.RefuseMissing, table.Receive(table.Track(false), Array.Empty<byte>(), true).Action);
    }

    // GW-U-1252 (rule P4): no payload before the deadline does nothing; at the deadline the client is refused as missing.
    [Fact]
    public void AClientWithoutAPayloadIsRefusedAsMissingAtTheDeadline()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        long id = table.Track(false);

        Assert.Empty(table.Tick(true));
        clock.Advance(PeerVersionTable.MissingPayloadTimeout - TimeSpan.FromMilliseconds(1));
        Assert.Empty(table.Tick(true));
        Assert.False(table.Find(id)!.IsDecided);

        clock.Advance(TimeSpan.FromMilliseconds(1));
        IReadOnlyList<PeerVersionOutcome> outcomes = table.Tick(true);
        PeerVersionOutcome refused = Assert.Single(outcomes);
        Assert.Equal(id, refused.PeerId);
        Assert.Equal(PeerVersionAction.RefuseMissing, refused.Action);
        Assert.Equal(VersionCheckResult.Missing(Host), refused.Result);
        Assert.Equal(clock.Elapsed + PeerVersionTable.DisconnectGrace, table.Find(id)!.DisconnectAt);

        // A late payload after the decision is ignored.
        Assert.Equal(PeerVersionAction.Ignore, table.Receive(id, VersionHandshake.EncodePayloadBytes(Host), true).Action);
        Assert.Empty(table.Tick(true));
    }

    // GW-U-1253 (rule P5): a refused client yields Disconnect once the grace has passed, and is forgotten.
    [Fact]
    public void ARefusedClientIsDisconnectedAfterTheGraceAndForgotten()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        long id = table.Track(false);
        table.Receive(id, VersionHandshake.EncodePayloadBytes(Other), true);

        Assert.Empty(table.Tick(true));
        clock.Advance(PeerVersionTable.DisconnectGrace - TimeSpan.FromMilliseconds(1));
        Assert.Empty(table.Tick(true));

        clock.Advance(TimeSpan.FromMilliseconds(1));
        PeerVersionOutcome disconnect = Assert.Single(table.Tick(true));
        Assert.Equal(PeerVersionAction.Disconnect, disconnect.Action);
        Assert.Equal(id, disconnect.PeerId);
        Assert.Equal(VersionCheckResult.Mismatch(Host, Other), disconnect.Result);
        Assert.Null(disconnect.MessageKey);
        Assert.Null(table.Find(id));
        Assert.Equal(0, table.Count);
        Assert.Empty(table.Tick(true));
    }

    // GW-U-1253: the missing path disconnects the same way, 1 s after the 5 s deadline.
    [Fact]
    public void AMissingClientIsDisconnectedOneSecondAfterTheDeadline()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        long id = table.Track(false);

        clock.Advance(PeerVersionTable.MissingPayloadTimeout);
        Assert.Equal(PeerVersionAction.RefuseMissing, Assert.Single(table.Tick(true)).Action);
        clock.Advance(PeerVersionTable.DisconnectGrace);
        PeerVersionOutcome disconnect = Assert.Single(table.Tick(true));
        Assert.Equal(PeerVersionAction.Disconnect, disconnect.Action);
        Assert.Equal(VersionCheckResult.Missing(Host), disconnect.Result);
        Assert.Equal(0, table.Count);
    }

    // GW-U-1254 (6.1 rows 4-6): with RequireSameVersion off every client is accepted, with or without a payload.
    [Fact]
    public void RequireSameVersionOffAcceptsEveryClient()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        long different = table.Track(false);
        long garbage = table.Track(false);
        long silent = table.Track(false);

        PeerVersionOutcome mismatchAccepted = table.Receive(different, VersionHandshake.EncodePayloadBytes(Other), false);
        Assert.Equal(PeerVersionAction.Accept, mismatchAccepted.Action);
        Assert.Equal(VersionCheckResult.Accepted(Host, Other), mismatchAccepted.Result);

        PeerVersionOutcome garbageAccepted = table.Receive(garbage, new byte[] { 0xFF }, false);
        Assert.Equal(PeerVersionAction.Accept, garbageAccepted.Action);
        Assert.Equal(VersionCheckResult.Accepted(Host, null), garbageAccepted.Result);

        clock.Advance(PeerVersionTable.MissingPayloadTimeout);
        PeerVersionOutcome silentAccepted = Assert.Single(table.Tick(false));
        Assert.Equal(silent, silentAccepted.PeerId);
        Assert.Equal(PeerVersionAction.Accept, silentAccepted.Action);
        Assert.Equal(VersionCheckResult.Accepted(Host, null), silentAccepted.Result);
        Assert.Null(table.Find(silent)!.DisconnectAt);

        clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Empty(table.Tick(false));
        Assert.Equal(3, table.Count);
    }

    // GW-U-1255 (6.1 rows 7-9): the client side accepts the same host version, leaves on a different one, and remembers the mismatch.
    [Fact]
    public void ClientSideLeavesOnAHostMismatchAndRemembersItForTheErrorScreen()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        long sameHost = table.Track(true);
        PeerVersionOutcome accepted = table.Receive(sameHost, VersionHandshake.EncodePayloadBytes(Host), true);
        Assert.Equal(PeerVersionAction.Accept, accepted.Action);
        Assert.True(accepted.RemoteIsServer);
        Assert.Equal(VersionCheckResult.Accepted(Host, Host), accepted.Result);
        Assert.False(table.HasHostMismatch);
        Assert.Null(table.TakeHostMismatch());

        table.Clear();
        long otherHost = table.Track(true);
        PeerVersionOutcome leave = table.Receive(otherHost, VersionHandshake.EncodePayloadBytes(Other), true);
        Assert.Equal(PeerVersionAction.LeaveMismatch, leave.Action);
        Assert.True(leave.RemoteIsServer);
        Assert.Equal(VersionCheckResult.Mismatch(Other, Host), leave.Result);
        Assert.Equal("network.version.mismatch", leave.MessageKey);
        Assert.True(table.Find(otherHost)!.IsDecided);
        Assert.Null(table.Find(otherHost)!.DisconnectAt);

        Assert.True(table.HasHostMismatch);
        table.Clear();
        Assert.True(table.HasHostMismatch);
        Assert.Equal(VersionCheckResult.Mismatch(Other, Host), table.TakeHostMismatch());
        Assert.False(table.HasHostMismatch);
        Assert.Null(table.TakeHostMismatch());
    }

    // GW-U-1255: with RequireSameVersion off the client stays, but the mismatch is still remembered in case the host refuses.
    [Fact]
    public void ClientSideWithRequireSameVersionOffStaysButRemembersTheMismatch()
    {
        var table = new PeerVersionTable(new ManualClock(Start), Host);
        long id = table.Track(true);

        PeerVersionOutcome stay = table.Receive(id, VersionHandshake.EncodePayloadBytes(Other), false);

        Assert.Equal(PeerVersionAction.Accept, stay.Action);
        Assert.Equal(VersionCheckResult.Accepted(Other, Host), stay.Result);
        Assert.True(table.HasHostMismatch);
        Assert.Equal(VersionCheckResult.Mismatch(Other, Host), table.TakeHostMismatch());
    }

    // GW-U-1255: a server peer never expires (a host without the mod sends nothing) and an unreadable host payload is ignored.
    [Fact]
    public void ServerPeersNeverExpireAndAnUnreadableHostPayloadIsIgnored()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        long id = table.Track(true);

        clock.Advance(TimeSpan.FromHours(1));
        Assert.Empty(table.Tick(true));
        Assert.False(table.Find(id)!.IsDecided);

        PeerVersionOutcome ignored = table.Receive(id, new byte[] { 0x42, 0x53 }, true);
        Assert.Equal(PeerVersionAction.Ignore, ignored.Action);
        Assert.True(ignored.RemoteIsServer);
        Assert.Null(ignored.Result);
        Assert.False(table.Find(id)!.IsDecided);
        Assert.False(table.HasHostMismatch);
    }

    // GW-U-1256 (3.4): unknown and already decided peers are ignored; outcomes come in tracking order.
    [Fact]
    public void UnknownAndDecidedPeersAreIgnoredAndOutcomesFollowTrackingOrder()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);

        PeerVersionOutcome unknown = table.Receive(42L, VersionHandshake.EncodePayloadBytes(Host), true);
        Assert.Equal(PeerVersionAction.Ignore, unknown.Action);
        Assert.Equal(42L, unknown.PeerId);
        Assert.Null(unknown.Result);
        Assert.Null(unknown.MessageKey);

        long a = table.Track(false);
        long b = table.Track(false);
        long c = table.Track(false);
        table.Receive(b, VersionHandshake.EncodePayloadBytes(Host), true);
        Assert.Equal(PeerVersionAction.Ignore, table.Receive(b, VersionHandshake.EncodePayloadBytes(Other), true).Action);
        Assert.True(table.Find(b)!.Result!.IsAccepted);

        clock.Advance(PeerVersionTable.MissingPayloadTimeout);
        IReadOnlyList<PeerVersionOutcome> outcomes = table.Tick(true);
        Assert.Equal(new[] { a, c }, outcomes.Select(o => o.PeerId).ToArray());
        Assert.All(outcomes, o => Assert.Equal(PeerVersionAction.RefuseMissing, o.Action));
    }

    // GW-U-1257 (AC-6.3): without peers (single-player, or the host alone) the table reads no clock and returns the same empty list.
    [Fact]
    public void WithoutPeersTheTickDoesNothingAndReadsNoClock()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);

        IReadOnlyList<PeerVersionOutcome> first = table.Tick(true);
        IReadOnlyList<PeerVersionOutcome> second = table.Tick(false);

        Assert.Empty(first);
        Assert.Same(first, second);
        Assert.Equal(0, clock.Reads);
    }

    // GW-U-1257: an idle tick with only decided peers allocates nothing new (the same empty list comes back).
    [Fact]
    public void AnIdleTickWithDecidedPeersReturnsTheSharedEmptyList()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        table.Receive(table.Track(false), VersionHandshake.EncodePayloadBytes(Host), true);

        IReadOnlyList<PeerVersionOutcome> first = table.Tick(true);
        clock.Advance(TimeSpan.FromHours(1));
        IReadOnlyList<PeerVersionOutcome> second = table.Tick(true);

        Assert.Empty(first);
        Assert.Same(first, second);
    }

    // GW-U-1252: a clock that moves backwards delays the deadline rather than refusing early (ADR-GWIN-100).
    [Fact]
    public void AClockMovingBackwardsNeverRefusesEarly()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        table.Track(false);

        clock.Set(TimeSpan.Zero);
        Assert.Empty(table.Tick(true));
        clock.Set(Start + PeerVersionTable.MissingPayloadTimeout);
        Assert.Single(table.Tick(true));
    }
}
