namespace BaseStorage.Core.Tests.HostSync.Session;

using System;
using System.Globalization;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Logging;

/// <summary>S-E006-004 AC-6 (contract 5.1 and 5.2, rules S6 and N7): the log lines are contract literals, culture-free, and routed by severity and category.</summary>
public sealed class ConfigSyncMessagesTests
{
    // GW-U-1418 (M1-M4): the lines of the outcomes are the contract literals; None and Send give null.
    [Fact]
    public void OutcomeLinesMatchTheContractLiterals()
    {
        Assert.Equal("Config sync: applied host settings from peer 1, 9 settings, 2 categories", ConfigSyncMessages.Format(Applied()));
        Assert.Equal("Config sync: ignored payload from peer 1, not connected to a host", ConfigSyncMessages.Format(RejectedNotClient()));
        Assert.Equal("Config sync: ignored payload from peer 2, no accepted version handshake with that host", ConfigSyncMessages.Format(RejectedNoAcceptedHost()));
        Assert.Equal("Config sync: payload from peer 1 is malformed, local settings stay active", ConfigSyncMessages.Format(Malformed()));
        Assert.Null(ConfigSyncMessages.Format(ConfigSyncOutcome.None));
        Assert.Null(ConfigSyncMessages.Format(SendOutcome()));
    }

    // GW-U-1418 (M5-M9): the Debug and Warning lines are the contract literals.
    [Fact]
    public void PayloadAndSettingLinesMatchTheContractLiterals()
    {
        Assert.Equal("Config sync: sent host settings to peer 3, 223 bytes", ConfigSyncMessages.Sent(3, 223));
        Assert.Equal("Config sync: host settings for peer 3 exceed 1048576 bytes and were not sent", ConfigSyncMessages.TooLarge(3));
        Assert.Equal("Config sync: CraftFromBase = false (host)", ConfigSyncMessages.SettingDetail("CraftFromBase", "false"));
        Assert.Equal("Config sync: host settings cleared, local settings apply", ConfigSyncMessages.Cleared());
        Assert.Equal(
            "Config sync: category file " + ConfigSyncFixtures.CategoryPath + " is missing or unreadable, using no categories",
            ConfigSyncMessages.CategoryFileUnavailable(ConfigSyncFixtures.CategoryPath));
    }

    // GW-U-1418 (AC-6): every line starts with "Config sync: " and holds no CR or LF.
    [Fact]
    public void EveryLineStartsWithThePrefixAndHoldsNoLineBreak()
    {
        string?[] lines =
        {
            ConfigSyncMessages.Format(Applied()),
            ConfigSyncMessages.Format(Malformed()),
            ConfigSyncMessages.Sent(3, 223),
            ConfigSyncMessages.TooLarge(3),
            ConfigSyncMessages.SettingDetail("CraftFromBase", "false"),
            ConfigSyncMessages.Cleared(),
            ConfigSyncMessages.CategoryFileUnavailable(ConfigSyncFixtures.CategoryPath),
        };

        foreach (string? line in lines)
        {
            Assert.NotNull(line);
            Assert.StartsWith("Config sync: ", line, StringComparison.Ordinal);
            Assert.False(line.Contains('\r') || line.Contains('\n'));
        }
    }

    // GW-U-1418 (AC-6): the lines do not depend on the current culture, with a four-digit peer id, under five cultures.
    [Fact]
    public void TheLinesAreIdenticalUnderFiveCultures()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            foreach (string name in new[] { "en-US", "de-DE", "ar-SA", "tr-TR", "fr-FR" })
            {
                CultureInfo.CurrentCulture = new CultureInfo(name);
                Assert.Equal("Config sync: sent host settings to peer 1234, 223 bytes", ConfigSyncMessages.Sent(1234, 223));
                Assert.Equal("Config sync: host settings for peer 1234 exceed 1048576 bytes and were not sent", ConfigSyncMessages.TooLarge(1234));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // GW-U-1418 (AC-6): null arguments are refused with the parameter names outcome, key, value and path.
    [Fact]
    public void NullArgumentsAreRefusedWithTheirParameterNames()
    {
        Assert.Equal("outcome", Assert.Throws<ArgumentNullException>(() => ConfigSyncMessages.Format(null!)).ParamName);
        Assert.Equal("outcome", Assert.Throws<ArgumentNullException>(() => ConfigSyncMessages.IsWarning(null!)).ParamName);
        Assert.Equal("outcome", Assert.Throws<ArgumentNullException>(() => ConfigSyncMessages.CategoryOf(null!)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => ConfigSyncMessages.SettingDetail(null!, "x")).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => ConfigSyncMessages.SettingDetail("k", null!)).ParamName);
        Assert.Equal("path", Assert.Throws<ArgumentNullException>(() => ConfigSyncMessages.CategoryFileUnavailable(null!)).ParamName);
    }

    // GW-U-1419 (AC-6): only Rejected+Malformed is a warning; ConfigSync is the category of Applied and Detail of every other outcome; LogPolicy maps ConfigSync to Info and Detail to Debug.
    [Fact]
    public void RoutingFollowsTheContractAndTheLogPolicy()
    {
        Assert.True(ConfigSyncMessages.IsWarning(Malformed()));
        Assert.False(ConfigSyncMessages.IsWarning(Applied()));
        Assert.False(ConfigSyncMessages.IsWarning(RejectedNotClient()));
        Assert.False(ConfigSyncMessages.IsWarning(RejectedNoAcceptedHost()));
        Assert.False(ConfigSyncMessages.IsWarning(ConfigSyncOutcome.None));
        Assert.False(ConfigSyncMessages.IsWarning(SendOutcome()));

        Assert.Equal(LogCategory.ConfigSync, ConfigSyncMessages.CategoryOf(Applied()));
        Assert.Equal(LogCategory.Detail, ConfigSyncMessages.CategoryOf(Malformed()));
        Assert.Equal(LogCategory.Detail, ConfigSyncMessages.CategoryOf(RejectedNotClient()));
        Assert.Equal(LogCategory.Detail, ConfigSyncMessages.CategoryOf(RejectedNoAcceptedHost()));
        Assert.Equal(LogCategory.Detail, ConfigSyncMessages.CategoryOf(ConfigSyncOutcome.None));
        Assert.Equal(LogCategory.Detail, ConfigSyncMessages.CategoryOf(SendOutcome()));

        Assert.Equal(LogSeverity.Info, LogPolicy.SeverityOf(LogCategory.ConfigSync));
        Assert.Equal(LogSeverity.Debug, LogPolicy.SeverityOf(LogCategory.Detail));
    }

    private static ConfigSyncOutcome Applied()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        return session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);
    }

    private static ConfigSyncOutcome RejectedNotClient() =>
        new ConfigSyncSession(ConfigSyncFixtures.Local()).Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.LocalWorld);

    private static ConfigSyncOutcome RejectedNoAcceptedHost()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.Client);
        return session.Receive(2, ConfigSyncFixtures.H(), ConfigSyncMode.Client);
    }

    private static ConfigSyncOutcome Malformed()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        return session.Receive(1, ConfigSyncFixtures.Utf8("BSC1;garbage"), ConfigSyncMode.Client);
    }

    private static ConfigSyncOutcome SendOutcome() =>
        new ConfigSyncSession(ConfigSyncFixtures.Local()).OnVersionOutcome(ConfigSyncFixtures.HostAccept(), ConfigSyncMode.LocalWorld);
}
