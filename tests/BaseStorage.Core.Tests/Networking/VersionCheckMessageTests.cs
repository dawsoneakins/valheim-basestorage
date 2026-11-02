namespace BaseStorage.Core.Tests.Networking;

using System;
using BaseStorage.Core.Networking;
using BaseStorage.Core.Ui;

/// <summary>AC-6.1 and AC-8.1 (contract 4.2 FormatMessage, rule V7, ui-strings-contract.md 3.2 row 43): the rejection text comes from the strings table by key.</summary>
public sealed class VersionCheckMessageTests
{
    // GW-U-810 (AC-8.1): the message key is the StringKeys constant for network.version.mismatch.
    [Fact]
    public void MismatchMessageKeyIsTheNetworkVersionMismatchKey()
    {
        VersionCheckResult result = VersionCheck.Compare("0.1.0", "0.2.0", true);

        Assert.Equal(StringKeys.NetworkVersionMismatch, result.MessageKey);
        Assert.Equal("network.version.mismatch", result.MessageKey);
        Assert.True(StringTable.English.Contains(result.MessageKey!));
    }

    // GW-U-810 (AC-6.1, AC-8.1): the formatted text reads exactly "Don's QOL version mismatch: host X, you Y".
    [Theory]
    [InlineData("0.1.0", "0.2.0")]
    [InlineData("0.2.0", "0.1.0")]
    [InlineData("0.1.0", "0.1.1")]
    [InlineData("1.0.0", "10.20.30")]
    [InlineData("999999999.999999999.999999999", "0.0.0")]
    public void MismatchTextReadsHostXYouY(string host, string client)
    {
        VersionCheckResult result = VersionCheck.Compare(host, client, true);

        Assert.Equal("Don's QOL version mismatch: host " + host + ", you " + client, result.FormatMessage(StringTable.English));
        Assert.Equal(NetworkingFixtures.MismatchText(host, client), result.FormatMessage(StringTable.English));
        Assert.Equal(StringTable.English.Format(StringKeys.NetworkVersionMismatch, host, client), result.FormatMessage(StringTable.English));
    }

    // GW-U-810 (AC-8.1): the text is the table's, so a table that holds other text for the key changes the result (no literal in Core).
    [Fact]
    public void TextComesFromTheTableNotFromCore()
    {
        VersionCheckResult result = VersionCheckResult.Mismatch("0.1.0", "0.2.0");

        string fromTable = StringTable.English.Format(StringKeys.NetworkVersionMismatch, "0.1.0", "0.2.0");
        Assert.Equal(fromTable, result.FormatMessage(StringTable.English));
        Assert.Equal("Don's QOL version mismatch: host 0.1.0, you 0.2.0", fromTable);
    }

    // GW-U-810 (U2): the text is the same under any current culture.
    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    public void MismatchTextIsCultureIndependent(string culture)
    {
        NetworkingFixtures.UnderCulture(culture, () =>
        {
            VersionCheckResult result = VersionCheck.CheckPayload("0.1.0", "BSV1;0.2.0", true);

            Assert.Equal("Don's QOL version mismatch: host 0.1.0, you 0.2.0", result.FormatMessage(StringTable.English));
            Assert.Equal("0.2.0", result.ClientVersion);
            Assert.True(VersionCheck.CheckPayload("0.1.0", "BSV1;0.1.0", true).IsAccepted);
        });
    }

    // GW-U-810 (V7): an accepted or missing result has no key and no message.
    [Fact]
    public void AcceptedAndMissingHaveNoMessage()
    {
        VersionCheckResult[] results =
        [
            VersionCheckResult.Accepted("0.1.0", "0.1.0"),
            VersionCheckResult.Accepted("0.1.0", "0.2.0"),
            VersionCheckResult.Accepted("0.1.0", null),
            VersionCheckResult.Missing("0.1.0"),
        ];

        foreach (VersionCheckResult result in results)
        {
            Assert.Null(result.MessageKey);
            Assert.Null(result.FormatMessage(StringTable.English));
        }
    }

    // GW-U-810 (R8): a null table is an ArgumentNullException named table, for every status.
    [Fact]
    public void NullTableThrowsForEveryStatus()
    {
        Assert.Equal("table", Assert.Throws<ArgumentNullException>(() => VersionCheckResult.Mismatch("0.1.0", "0.2.0").FormatMessage(null!)).ParamName);
        Assert.Equal("table", Assert.Throws<ArgumentNullException>(() => VersionCheckResult.Accepted("0.1.0", "0.1.0").FormatMessage(null!)).ParamName);
        Assert.Equal("table", Assert.Throws<ArgumentNullException>(() => VersionCheckResult.Missing("0.1.0").FormatMessage(null!)).ParamName);
    }

    // GW-U-810 (S10): the formatted notice is one line without rich-text tags.
    [Fact]
    public void MismatchTextIsASingleLineWithoutTags()
    {
        string text = VersionCheck.Compare("0.1.0", "0.2.0", true).FormatMessage(StringTable.English)!;

        Assert.DoesNotContain('\n', text);
        Assert.DoesNotContain('\r', text);
        Assert.DoesNotContain('<', text);
        Assert.DoesNotContain('{', text);
    }
}
