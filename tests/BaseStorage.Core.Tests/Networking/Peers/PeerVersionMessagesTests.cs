namespace BaseStorage.Core.Tests.Networking.Peers;

using System;
using BaseStorage.Core.Networking;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Tests.Reservations;

/// <summary>
/// AC-6.1 (contract version-rpc-contracts.md 4, L1-L5): the Info log line of each handshake outcome, invariant under culture,
/// with no strings-table wording.
/// </summary>
public sealed class PeerVersionMessagesTests
{
    private const string Host = "0.1.0";
    private const string Other = "0.2.0";

    private static readonly TimeSpan Start = TimeSpan.FromSeconds(5);

    // GW-U-1261 (L1-L5): each action has its line; Ignore has none; null throws.
    [Fact]
    public void EachActionFormatsItsContractLine()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        PeerVersionOutcome accepted = table.Receive(table.Track(false), VersionHandshake.EncodePayloadBytes(Host), true);
        PeerVersionOutcome mismatch = table.Receive(table.Track(false), VersionHandshake.EncodePayloadBytes(Other), true);
        PeerVersionOutcome missing = table.Receive(table.Track(false), null, true);
        PeerVersionOutcome ignored = table.Receive(77L, null, true);
        clock.Advance(PeerVersionTable.DisconnectGrace);
        PeerVersionOutcome disconnect = table.Tick(true)[0];

        var client = new PeerVersionTable(new ManualClock(Start), Other);
        PeerVersionOutcome leave = client.Receive(client.Track(true), VersionHandshake.EncodePayloadBytes(Host), true);

        Assert.Equal("Version handshake: peer 1 accepted, host 0.1.0, client 0.1.0", PeerVersionMessages.Format(accepted));
        Assert.Equal("Version handshake: peer 2 refused, versions differ, host 0.1.0, client 0.2.0", PeerVersionMessages.Format(mismatch));
        Assert.Equal("Version handshake: peer 3 refused, no BaseStorage payload, host 0.1.0", PeerVersionMessages.Format(missing));
        Assert.Equal("Version handshake: peer 2 disconnected after refusal", PeerVersionMessages.Format(disconnect));
        Assert.Equal("Version handshake: peer 1 is the host and versions differ, leaving, host 0.1.0, client 0.2.0", PeerVersionMessages.Format(leave));
        Assert.Null(PeerVersionMessages.Format(ignored));
        Assert.Equal("outcome", Assert.Throws<ArgumentNullException>(() => PeerVersionMessages.Format(null!)).ParamName);
    }

    // GW-U-1261 (L1): an accepted client without a payload (RequireSameVersion off) reads "client none".
    [Fact]
    public void AnAcceptedClientWithoutAPayloadReadsNone()
    {
        var table = new PeerVersionTable(new ManualClock(Start), Host);
        PeerVersionOutcome accepted = table.Receive(table.Track(false), null, false);

        Assert.Equal("Version handshake: peer 1 accepted, host 0.1.0, client none", PeerVersionMessages.Format(accepted));
    }

    // GW-U-1261 (U2): the lines are the same under every culture, including a large peer id.
    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    public void LinesAreCultureIndependent(string culture)
    {
        NetworkingFixtures.UnderCulture(culture, () =>
        {
            var table = new PeerVersionTable(new ManualClock(Start), Host);
            PeerVersionOutcome? last = null;
            for (int i = 0; i < 1234; i++)
            {
                last = table.Receive(table.Track(false), VersionHandshake.EncodePayloadBytes(Other), true);
            }

            Assert.Equal("Version handshake: peer 1234 refused, versions differ, host 0.1.0, client 0.2.0", PeerVersionMessages.Format(last!));
        });
    }

    // GW-U-1261 (U6): no line contains strings-table wording or a line break.
    [Fact]
    public void LinesHoldNoNoticeWordingAndNoLineBreak()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        PeerVersionOutcome mismatch = table.Receive(table.Track(false), VersionHandshake.EncodePayloadBytes(Other), true);
        PeerVersionOutcome missing = table.Receive(table.Track(false), null, true);

        foreach (string line in new[] { PeerVersionMessages.Format(mismatch)!, PeerVersionMessages.Format(missing)! })
        {
            Assert.DoesNotContain("version mismatch", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("you ", line, StringComparison.Ordinal);
            Assert.DoesNotContain('\n', line);
            Assert.StartsWith("Version handshake: peer ", line, StringComparison.Ordinal);
        }
    }
}
