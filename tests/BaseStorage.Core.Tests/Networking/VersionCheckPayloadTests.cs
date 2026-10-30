namespace BaseStorage.Core.Tests.Networking;

using System;
using System.Text;
using BaseStorage.Core.Networking;
using BaseStorage.Core.Ui;

/// <summary>AC-6.1 (contract 4.3, 4.4 CheckPayload rows, rule V5): the payload entry points decode, then compare; malformed payloads count as missing and never throw.</summary>
public sealed class VersionCheckPayloadTests
{
    // GW-U-808 (AC-6.1): an equal payload is accepted with both versions, as text and as bytes.
    [Fact]
    public void EqualPayloadIsAccepted()
    {
        VersionCheckResult fromText = VersionCheck.CheckPayload("0.1.0", "BSV1;0.1.0", true);
        VersionCheckResult fromBytes = VersionCheck.CheckPayload("0.1.0", VersionHandshake.EncodePayloadBytes("0.1.0"), true);

        Assert.Equal(VersionCheckStatus.Accepted, fromText.Status);
        Assert.Equal("0.1.0", fromText.HostVersion);
        Assert.Equal("0.1.0", fromText.ClientVersion);
        Assert.Equal(fromText, fromBytes);
    }

    // GW-U-808 (AC-6.1): a different payload is a mismatch with the AC-8.1 message when required, and accepted otherwise.
    [Fact]
    public void DifferentPayloadFollowsRequireSameVersion()
    {
        VersionCheckResult rejected = VersionCheck.CheckPayload("0.1.0", "BSV1;0.2.0", true);
        VersionCheckResult accepted = VersionCheck.CheckPayload("0.1.0", "BSV1;0.2.0", false);

        Assert.Equal(VersionCheckStatus.RejectedMismatch, rejected.Status);
        Assert.Equal("0.2.0", rejected.ClientVersion);
        Assert.Equal("BaseStorage version mismatch: host 0.1.0, you 0.2.0", rejected.FormatMessage(StringTable.English));
        Assert.Equal(VersionCheckStatus.Accepted, accepted.Status);
        Assert.Equal("0.2.0", accepted.ClientVersion);
        Assert.Equal(rejected, VersionCheck.CheckPayload("0.1.0", Encoding.ASCII.GetBytes("BSV1;0.2.0"), true));
    }

    // GW-U-808 (V5): a null or malformed text payload is missing when required and accepted (null client) otherwise.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("BSV2;0.1.0")]
    [InlineData("BSV1;0.1.0-rc")]
    [InlineData("c:Wood")]
    [InlineData("0.1.0")]
    public void MalformedTextPayloadCountsAsMissing(string? payload)
    {
        VersionCheckResult rejected = VersionCheck.CheckPayload("0.1.0", payload, true);
        VersionCheckResult accepted = VersionCheck.CheckPayload("0.1.0", payload, false);

        Assert.Equal(VersionCheckResult.Missing("0.1.0"), rejected);
        Assert.Null(rejected.ClientVersion);
        Assert.Null(rejected.MessageKey);
        Assert.Null(rejected.FormatMessage(StringTable.English));
        Assert.Equal(VersionCheckResult.Accepted("0.1.0", null), accepted);
        Assert.True(accepted.IsAccepted);
    }

    // GW-U-808 (V5): a null or malformed byte payload is missing when required and accepted (null client) otherwise.
    [Fact]
    public void MalformedBytePayloadCountsAsMissing()
    {
        byte[]?[] payloads = [null, Array.Empty<byte>(), new byte[] { 0xFF }, new byte[1000], Encoding.UTF8.GetBytes("BSV1;0.1.ö")];

        foreach (byte[]? payload in payloads)
        {
            Assert.Equal(VersionCheckResult.Missing("0.1.0"), VersionCheck.CheckPayload("0.1.0", payload, true));
            Assert.Equal(VersionCheckResult.Accepted("0.1.0", null), VersionCheck.CheckPayload("0.1.0", payload, false));
        }
    }

    // GW-U-808 (V5): every contract 3.2 malformed payload is handled without an exception, in both overloads.
    [Theory]
    [MemberData(nameof(NetworkingFixtures.MalformedPayloadRows), MemberType = typeof(NetworkingFixtures))]
    public void EveryMalformedPayloadIsMissingWithoutThrowing(string payload)
    {
        Assert.Equal(VersionCheckStatus.RejectedMissing, VersionCheck.CheckPayload("0.1.0", payload, true).Status);
        Assert.Equal(VersionCheckStatus.RejectedMissing, VersionCheck.CheckPayload("0.1.0", Encoding.UTF8.GetBytes(payload), true).Status);
    }

    // GW-U-808 (V5, V6): CheckPayload equals Compare over the decoded payload.
    [Theory]
    [InlineData("BSV1;0.1.0", true)]
    [InlineData("BSV1;0.2.0", true)]
    [InlineData("BSV1;0.2.0", false)]
    [InlineData("junk", true)]
    [InlineData("junk", false)]
    public void CheckPayloadEqualsCompareOverTheDecodedVersion(string payload, bool require)
    {
        VersionCheckResult expected = VersionCheck.Compare("0.1.0", VersionHandshake.DecodePayload(payload), require);

        Assert.Equal(expected, VersionCheck.CheckPayload("0.1.0", payload, require));
        Assert.Equal(expected, VersionCheck.CheckPayload("0.1.0", Encoding.ASCII.GetBytes(payload), require));
    }

    // GW-U-808 (V1): the local payload of this build is accepted by a host on the same build.
    [Fact]
    public void LocalPayloadIsAcceptedByTheSameBuild()
    {
        VersionCheckResult result = VersionCheck.CheckPayload(ModVersion.Current, VersionHandshake.LocalPayload, true);

        Assert.True(result.IsAccepted);
        Assert.Equal(ModVersion.Current, result.ClientVersion);
    }
}
