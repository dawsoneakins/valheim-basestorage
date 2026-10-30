namespace BaseStorage.Core.Tests.Networking.Peers;

using System;
using BaseStorage.Core.Networking;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Ui;

/// <summary>
/// AC-6.1 and AC-8.1 (contract version-rpc-contracts.md 2.3 and 3.3, ADR-GWIN-101): the notice text of a refused or left peer
/// comes from the strings table by key, reads the fixed AC text for a mismatch and the new missing text for a client without the
/// mod, and is the same under every culture.
/// </summary>
public sealed class PeerVersionOutcomeTests
{
    private const string Host = "0.1.0";
    private const string Other = "0.2.0";

    private static readonly TimeSpan Start = TimeSpan.FromSeconds(10);

    // GW-U-1258 (2.3): the new key is the last StringKeys constant and holds the contracted English text.
    [Fact]
    public void TheMissingKeyIsInTheTableWithTheContractText()
    {
        Assert.Equal("network.version.missing", StringKeys.NetworkVersionMissing);
        Assert.True(StringTable.English.Contains(StringKeys.NetworkVersionMissing));
        Assert.Equal(
            "BaseStorage version mismatch: host {0}, joining player has no BaseStorage",
            StringTable.English.Get(StringKeys.NetworkVersionMissing));
        Assert.Equal(
            "BaseStorage version mismatch: host 0.1.0, joining player has no BaseStorage",
            StringTable.English.Format(StringKeys.NetworkVersionMissing, Host));
    }

    // GW-U-1258 (3.3): the mismatch outcome formats the fixed AC text "BaseStorage version mismatch: host X, you Y".
    [Theory]
    [InlineData("0.1.0", "0.2.0")]
    [InlineData("0.2.0", "0.1.0")]
    [InlineData("0.1.0", "0.1.1")]
    [InlineData("10.20.30", "1.0.0")]
    public void ARefusedMismatchFormatsHostXYouY(string host, string client)
    {
        var table = new PeerVersionTable(new ManualClock(Start), host);
        PeerVersionOutcome outcome = table.Receive(table.Track(false), VersionHandshake.EncodePayloadBytes(client), true);

        Assert.Equal(PeerVersionAction.RefuseMismatch, outcome.Action);
        Assert.Equal(StringKeys.NetworkVersionMismatch, outcome.MessageKey);
        Assert.Equal("BaseStorage version mismatch: host " + host + ", you " + client, outcome.FormatMessage(StringTable.English));
        Assert.Equal(outcome.Result!.FormatMessage(StringTable.English), outcome.FormatMessage(StringTable.English));
    }

    // GW-U-1258 (3.3): the client's own mismatch reads the same text with the host first, so both sides see the same line.
    [Fact]
    public void ALeftMismatchFormatsTheSameTextAsTheHostSees()
    {
        var hostTable = new PeerVersionTable(new ManualClock(Start), Host);
        var clientTable = new PeerVersionTable(new ManualClock(Start), Other);

        PeerVersionOutcome refused = hostTable.Receive(hostTable.Track(false), VersionHandshake.EncodePayloadBytes(Other), true);
        PeerVersionOutcome left = clientTable.Receive(clientTable.Track(true), VersionHandshake.EncodePayloadBytes(Host), true);

        Assert.Equal(PeerVersionAction.LeaveMismatch, left.Action);
        Assert.Equal(StringKeys.NetworkVersionMismatch, left.MessageKey);
        Assert.Equal("BaseStorage version mismatch: host 0.1.0, you 0.2.0", left.FormatMessage(StringTable.English));
        Assert.Equal(refused.FormatMessage(StringTable.English), left.FormatMessage(StringTable.English));
        Assert.Equal(refused.FormatMessage(StringTable.English), clientTable.TakeHostMismatch()!.FormatMessage(StringTable.English));
    }

    // GW-U-1258 (3.3): a refused missing payload formats the missing text with the host version only.
    [Fact]
    public void ARefusedMissingPayloadFormatsTheMissingText()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        long garbage = table.Track(false);
        long silent = table.Track(false);

        PeerVersionOutcome malformed = table.Receive(garbage, new byte[] { 0x01 }, true);
        clock.Advance(PeerVersionTable.MissingPayloadTimeout);
        IReadOnlyList<PeerVersionOutcome> outcomes = table.Tick(true);
        PeerVersionOutcome timedOut = Assert.Single(outcomes, o => o.Action == PeerVersionAction.RefuseMissing);
        Assert.Equal(PeerVersionAction.Disconnect, Assert.Single(outcomes, o => o.PeerId == garbage).Action);

        Assert.Equal(silent, timedOut.PeerId);
        foreach (PeerVersionOutcome outcome in new[] { malformed, timedOut })
        {
            Assert.Equal(PeerVersionAction.RefuseMissing, outcome.Action);
            Assert.Equal(StringKeys.NetworkVersionMissing, outcome.MessageKey);
            Assert.Equal("BaseStorage version mismatch: host 0.1.0, joining player has no BaseStorage", outcome.FormatMessage(StringTable.English));
        }
    }

    // GW-U-1258 (V7): accept, disconnect and ignore have no key and no text; a null table throws for every action.
    [Fact]
    public void AcceptDisconnectAndIgnoreHaveNoNoticeAndANullTableThrows()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, Host);
        PeerVersionOutcome accepted = table.Receive(table.Track(false), VersionHandshake.EncodePayloadBytes(Host), true);
        PeerVersionOutcome ignored = table.Receive(99L, null, true);
        long refused = table.Track(false);
        PeerVersionOutcome mismatch = table.Receive(refused, VersionHandshake.EncodePayloadBytes(Other), true);
        clock.Advance(PeerVersionTable.DisconnectGrace);
        PeerVersionOutcome disconnect = Assert.Single(table.Tick(true));

        foreach (PeerVersionOutcome outcome in new[] { accepted, ignored, disconnect })
        {
            Assert.Null(outcome.MessageKey);
            Assert.Null(outcome.FormatMessage(StringTable.English));
        }

        foreach (PeerVersionOutcome outcome in new[] { accepted, ignored, disconnect, mismatch })
        {
            Assert.Equal("table", Assert.Throws<ArgumentNullException>(() => outcome.FormatMessage(null!)).ParamName);
        }
    }

    // GW-U-1258 (U2): both notice texts are the same under invariant, en-US, de-DE, tr-TR and ar-SA.
    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    public void NoticeTextsAreCultureIndependent(string culture)
    {
        NetworkingFixtures.UnderCulture(culture, () =>
        {
            var table = new PeerVersionTable(new ManualClock(Start), Host);
            PeerVersionOutcome mismatch = table.Receive(table.Track(false), VersionHandshake.EncodePayloadBytes(Other), true);
            PeerVersionOutcome missing = table.Receive(table.Track(false), null, true);

            Assert.Equal("BaseStorage version mismatch: host 0.1.0, you 0.2.0", mismatch.FormatMessage(StringTable.English));
            Assert.Equal("BaseStorage version mismatch: host 0.1.0, joining player has no BaseStorage", missing.FormatMessage(StringTable.English));
        });
    }

    // GW-U-1258 (S10): both texts are one line without tags, braces or a dollar sign.
    [Fact]
    public void NoticeTextsAreSingleLinesWithoutMarkup()
    {
        var table = new PeerVersionTable(new ManualClock(Start), Host);
        string mismatch = table.Receive(table.Track(false), VersionHandshake.EncodePayloadBytes(Other), true).FormatMessage(StringTable.English)!;
        string missing = table.Receive(table.Track(false), null, true).FormatMessage(StringTable.English)!;

        foreach (string text in new[] { mismatch, missing })
        {
            Assert.DoesNotContain('\n', text);
            Assert.DoesNotContain('\r', text);
            Assert.DoesNotContain('<', text);
            Assert.DoesNotContain('{', text);
            Assert.DoesNotContain('$', text);
        }
    }
}
