namespace BaseStorage.Core.Tests.Networking;

using System;
using System.Text;
using BaseStorage.Core.Networking;

/// <summary>AC-6.1 (contract 3.1, 3.2, rule V4): encoding the handshake payload and the round trip through both decoders.</summary>
public sealed class VersionHandshakeEncodeTests
{
    // GW-U-802 (V4): the tag and the encoded form of the contract examples.
    [Fact]
    public void TagAndEncodedExamplesMatchTheContract()
    {
        Assert.Equal("BSV1;", VersionHandshake.PayloadTag);
        Assert.Equal("BSV1;0.1.0", VersionHandshake.EncodePayload("0.1.0"));
        Assert.Equal("BSV1;10.20.30", VersionHandshake.EncodePayload("10.20.30"));
    }

    // GW-U-802 (V1, V4): the local payload is the tag followed by the build version from Version.props.
    [Fact]
    public void LocalPayloadIsTheTagAndTheBuildVersion()
    {
        Assert.Equal("BSV1;" + ModVersion.Current, VersionHandshake.LocalPayload);
        Assert.Equal("BSV1;" + NetworkingFixtures.VersionPropsVersion(), VersionHandshake.LocalPayload);
        Assert.Equal(ModVersion.Current, VersionHandshake.DecodePayload(VersionHandshake.LocalPayload));
    }

    // GW-U-802 (V4): the byte form is the ASCII bytes of the text, without a byte order mark.
    [Fact]
    public void PayloadBytesAreTheAsciiBytesWithoutBom()
    {
        byte[] bytes = VersionHandshake.EncodePayloadBytes("0.1.0");

        Assert.Equal(new byte[] { 0x42, 0x53, 0x56, 0x31, 0x3B, 0x30, 0x2E, 0x31, 0x2E, 0x30 }, bytes);
        Assert.Equal(Encoding.ASCII.GetBytes("BSV1;0.1.0"), bytes);
        Assert.Equal(new UTF8Encoding(false).GetBytes(VersionHandshake.EncodePayload("0.1.0")), bytes);
    }

    // GW-U-802 (R8): a null version is an ArgumentNullException on both encoders.
    [Fact]
    public void NullVersionThrowsArgumentNull()
    {
        Assert.Equal("version", Assert.Throws<ArgumentNullException>(() => VersionHandshake.EncodePayload(null!)).ParamName);
        Assert.Equal("version", Assert.Throws<ArgumentNullException>(() => VersionHandshake.EncodePayloadBytes(null!)).ParamName);
    }

    // GW-U-802 (R8): an ill-formed version is an ArgumentException on both encoders.
    [Theory]
    [MemberData(nameof(NetworkingFixtures.IllFormedRows), MemberType = typeof(NetworkingFixtures))]
    public void IllFormedVersionThrowsArgument(string version)
    {
        Assert.Equal("version", Assert.Throws<ArgumentException>(() => VersionHandshake.EncodePayload(version)).ParamName);
        Assert.Equal("version", Assert.Throws<ArgumentException>(() => VersionHandshake.EncodePayloadBytes(version)).ParamName);
    }

    // GW-U-803 (round trip P1): Decode(Encode(v)) == v for text and for bytes, and the payload is ASCII within the length bound.
    [Theory]
    [MemberData(nameof(NetworkingFixtures.WellFormedRows), MemberType = typeof(NetworkingFixtures))]
    public void EveryWellFormedVersionRoundTrips(string version)
    {
        string payload = VersionHandshake.EncodePayload(version);
        byte[] bytes = VersionHandshake.EncodePayloadBytes(version);

        Assert.StartsWith(VersionHandshake.PayloadTag, payload, StringComparison.Ordinal);
        Assert.InRange(payload.Length, 10, 34);
        Assert.All(payload, c => Assert.True(c < 0x80, "payload must be ASCII"));
        Assert.Equal(payload.Length, bytes.Length);
        Assert.Equal(Encoding.ASCII.GetBytes(payload), bytes);
        Assert.Equal(version, VersionHandshake.DecodePayload(payload));
        Assert.Equal(version, VersionHandshake.DecodePayload(bytes));
    }

    // GW-U-803 (round trip P1): a decoded payload re-encodes to the same text (canonical form).
    [Theory]
    [InlineData("BSV1;0.1.0")]
    [InlineData("BSV1;999999999.999999999.999999999")]
    [InlineData("BSV1;0.0.0")]
    public void DecodedPayloadReEncodesToItself(string payload)
    {
        string? version = VersionHandshake.DecodePayload(payload);

        Assert.NotNull(version);
        Assert.Equal(payload, VersionHandshake.EncodePayload(version!));
        Assert.Equal(Encoding.ASCII.GetBytes(payload), VersionHandshake.EncodePayloadBytes(version!));
    }
}
