namespace BaseStorage.Core.Tests.Networking;

using System;
using BaseStorage.Core.Networking;
using BaseStorage.Core.Ui;

/// <summary>AC-6.1 (contract 4.3 decision table, 4.4 rows 1-7, rules V3 and V6): Compare accepts, rejects with both versions, honours RequireSameVersion.</summary>
public sealed class VersionCheckCompareTests
{
    // GW-U-805 (AC-6.1): equal versions are accepted whatever RequireSameVersion says, and both versions are reported.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EqualVersionsAreAccepted(bool requireSameVersion)
    {
        VersionCheckResult result = VersionCheck.Compare("0.1.0", "0.1.0", requireSameVersion);

        Assert.Equal(VersionCheckStatus.Accepted, result.Status);
        Assert.True(result.IsAccepted);
        Assert.Equal("0.1.0", result.HostVersion);
        Assert.Equal("0.1.0", result.ClientVersion);
        Assert.Null(result.MessageKey);
        Assert.Null(result.FormatMessage(StringTable.English));
    }

    // GW-U-805 (AC-6.1, V1): the build version compared with itself is accepted.
    [Fact]
    public void TheBuildVersionAgreesWithItself()
    {
        VersionCheckResult result = VersionCheck.Compare(ModVersion.Current, ModVersion.Current, true);

        Assert.True(result.IsAccepted);
        Assert.Equal(ModVersion.Current, result.HostVersion);
        Assert.Equal(ModVersion.Current, result.ClientVersion);
    }

    // GW-U-806 (AC-6.1): a different version is rejected with both versions when RequireSameVersion is true.
    [Fact]
    public void DifferentVersionIsRejectedWithBothVersionsWhenRequired()
    {
        VersionCheckResult result = VersionCheck.Compare("0.1.0", "0.2.0", true);

        Assert.Equal(VersionCheckStatus.RejectedMismatch, result.Status);
        Assert.False(result.IsAccepted);
        Assert.Equal("0.1.0", result.HostVersion);
        Assert.Equal("0.2.0", result.ClientVersion);
        Assert.Equal("network.version.mismatch", result.MessageKey);
        Assert.Equal("BaseStorage version mismatch: host 0.1.0, you 0.2.0", result.FormatMessage(StringTable.English));
    }

    // GW-U-806 (V3): equality is ordinal on the whole string, so a one-character difference is a mismatch.
    [Theory]
    [InlineData("0.1.0", "0.1.1")]
    [InlineData("0.1.0", "1.1.0")]
    [InlineData("1.0.0", "10.0.0")]
    [InlineData("10.0.0", "1.0.0")]
    public void OneCharacterDifferenceIsAMismatch(string host, string client)
    {
        VersionCheckResult result = VersionCheck.Compare(host, client, true);

        Assert.Equal(VersionCheckStatus.RejectedMismatch, result.Status);
        Assert.Equal(NetworkingFixtures.MismatchText(host, client), result.FormatMessage(StringTable.English));
    }

    // GW-U-806 (AC-6.1): a different version is accepted, with both versions reported, when RequireSameVersion is false.
    [Fact]
    public void DifferentVersionIsAcceptedWhenNotRequired()
    {
        VersionCheckResult result = VersionCheck.Compare("0.1.0", "0.2.0", false);

        Assert.Equal(VersionCheckStatus.Accepted, result.Status);
        Assert.True(result.IsAccepted);
        Assert.Equal("0.1.0", result.HostVersion);
        Assert.Equal("0.2.0", result.ClientVersion);
        Assert.Null(result.MessageKey);
        Assert.Null(result.FormatMessage(StringTable.English));
    }

    // GW-U-807 (AC-6.1): a client without a usable version is rejected as missing when RequireSameVersion is true.
    [Fact]
    public void MissingClientVersionIsRejectedWhenRequired()
    {
        VersionCheckResult result = VersionCheck.Compare("0.1.0", null, true);

        Assert.Equal(VersionCheckStatus.RejectedMissing, result.Status);
        Assert.False(result.IsAccepted);
        Assert.Equal("0.1.0", result.HostVersion);
        Assert.Null(result.ClientVersion);
        Assert.Null(result.MessageKey);
        Assert.Null(result.FormatMessage(StringTable.English));
    }

    // GW-U-807 (AC-6.1): a client without a usable version is accepted when RequireSameVersion is false.
    [Fact]
    public void MissingClientVersionIsAcceptedWhenNotRequired()
    {
        VersionCheckResult result = VersionCheck.Compare("0.1.0", null, false);

        Assert.Equal(VersionCheckStatus.Accepted, result.Status);
        Assert.True(result.IsAccepted);
        Assert.Equal("0.1.0", result.HostVersion);
        Assert.Null(result.ClientVersion);
        Assert.Null(result.MessageKey);
    }

    // GW-U-805..807 (V6): Compare is a pure function: the same arguments give equal results every time.
    [Theory]
    [InlineData("0.1.0", "0.1.0", true)]
    [InlineData("0.1.0", "0.2.0", true)]
    [InlineData("0.1.0", "0.2.0", false)]
    [InlineData("0.1.0", null, true)]
    [InlineData("0.1.0", null, false)]
    public void CompareIsDeterministic(string host, string? client, bool require)
    {
        Assert.Equal(VersionCheck.Compare(host, client, require), VersionCheck.Compare(host, client, require));
    }

    // GW-U-809 (R8): a null host version is an ArgumentNullException named hostVersion, for every overload.
    [Fact]
    public void NullHostVersionThrowsArgumentNull()
    {
        Assert.Equal("hostVersion", Assert.Throws<ArgumentNullException>(() => VersionCheck.Compare(null!, "0.1.0", true)).ParamName);
        Assert.Equal("hostVersion", Assert.Throws<ArgumentNullException>(() => VersionCheck.CheckPayload(null!, "BSV1;0.1.0", true)).ParamName);
        Assert.Equal("hostVersion", Assert.Throws<ArgumentNullException>(() => VersionCheck.CheckPayload(null!, new byte[] { 0x42 }, true)).ParamName);
    }

    // GW-U-809 (R8): an ill-formed host version is an ArgumentException named hostVersion, for every overload.
    [Theory]
    [InlineData("1.0")]
    [InlineData("")]
    [InlineData("01.0.0")]
    public void IllFormedHostVersionThrowsArgument(string host)
    {
        Assert.Equal("hostVersion", Assert.Throws<ArgumentException>(() => VersionCheck.Compare(host, "0.1.0", true)).ParamName);
        Assert.Equal("hostVersion", Assert.Throws<ArgumentException>(() => VersionCheck.CheckPayload(host, "BSV1;0.1.0", true)).ParamName);
        Assert.Equal("hostVersion", Assert.Throws<ArgumentException>(() => VersionCheck.CheckPayload(host, (byte[]?)null, false)).ParamName);
    }

    // GW-U-809 (R8): a non-null ill-formed client version is an ArgumentException named clientVersion, for both RequireSameVersion values.
    [Theory]
    [InlineData("0.1", true)]
    [InlineData("", false)]
    [InlineData("0.1.0-rc", true)]
    [InlineData(" 0.1.0", false)]
    public void IllFormedClientVersionThrowsArgument(string client, bool require)
    {
        Assert.Equal("clientVersion", Assert.Throws<ArgumentException>(() => VersionCheck.Compare("0.1.0", client, require)).ParamName);
    }
}
