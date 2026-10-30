namespace BaseStorage.Core.Tests.HostSync.Session;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Tests.Logging;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// S-E006-004 AC-6 (rules S6, N7; ADR-GWIN-121): the N7 routing replayed over RecordingSink behind the real DebugGatedLogSink. The
/// Route method below has the same statements that GW-U-1421 pins in PluginConfigSync, so the two cannot drift apart.
/// </summary>
public sealed class ConfigSyncLogRoutingTests
{
    private const string AppliedLine = "Config sync: applied host settings from peer 1, 9 settings, 2 categories";
    private const string MalformedLine = "Config sync: payload from peer 1 is malformed, local settings stay active";

    private static readonly string[] DetailLines =
    {
        "Config sync: CraftFromBase = false (host)",
        "Config sync: ExcludeVehicles = true (host)",
        "Config sync: KeepChestsCraftable = true (host)",
        "Config sync: MaxBaseStations = 3 (host)",
        "Config sync: RequireSameVersion = true (host)",
        "Config sync: AutoAssignThresholdPct = 50 (host)",
        "Config sync: OrganizeLockSeconds = 30 (host)",
        "Config sync: ReserveTimeoutMs = 1500 (host)",
        "Config sync: AutoAssignMixedChests = true (host)",
    };

    // GW-U-1420 (AC-6, debug off): an applied sync writes exactly one Info line and no Debug line.
    [Fact]
    public void WithDebugOffAnAppliedSyncWritesOneInfoLineAndNoDebugLine()
    {
        var recording = new RecordingSink();
        var sink = new DebugGatedLogSink(recording, () => false);

        Route(sink, Applied());

        Assert.Equal(new[] { (LogSeverity.Info, AppliedLine) }, recording.Entries);
    }

    // GW-U-1420 (AC-6, debug on): one Info line, then the nine per-setting Debug lines in catalog order, the last ending "(host)".
    [Fact]
    public void WithDebugOnTheNineSettingLinesFollowTheInfoLine()
    {
        var recording = new RecordingSink();
        var sink = new DebugGatedLogSink(recording, () => true);

        Route(sink, Applied());

        var expected = new List<(LogSeverity Severity, string Message)> { (LogSeverity.Info, AppliedLine) };
        expected.AddRange(DetailLines.Select(line => (LogSeverity.Debug, line)));
        Assert.Equal(expected, recording.Entries);
    }

    // GW-U-1420 (AC-6): a malformed payload writes one Warning line and no Info line.
    [Fact]
    public void AMalformedPayloadWritesOneWarningAndNoInfoLine()
    {
        var recording = new RecordingSink();
        var sink = new DebugGatedLogSink(recording, () => false);

        Route(sink, Malformed());

        Assert.Equal(new[] { (LogSeverity.Warning, MalformedLine) }, recording.Entries);
    }

    // GW-U-1420 (AC-6): a host Send (M5) and a solo tick (M9) write no Info line, even with Debug on.
    [Fact]
    public void AHostSendAndASoloTickWriteNoInfoLine()
    {
        var recording = new RecordingSink();
        var sink = new DebugGatedLogSink(recording, () => true);
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        ConfigSyncOutcome send = session.OnVersionOutcome(ConfigSyncFixtures.HostAccept(), ConfigSyncMode.LocalWorld);
        Route(sink, send);
        LogPolicy.Write(sink, LogCategory.Detail, ConfigSyncMessages.Sent(send.PeerId, 223));
        Assert.Equal(ConfigSyncTick.ReloadLocalCategories, session.Tick(ConfigSyncMode.LocalWorld));
        LogPolicy.Write(sink, LogCategory.Detail, ConfigSyncMessages.CategoryFileUnavailable(ConfigSyncFixtures.CategoryPath));

        Assert.Equal(ConfigSyncAction.Send, send.Action);
        Assert.Equal(new[] { LogSeverity.Debug, LogSeverity.Debug }, recording.Entries.Select(entry => entry.Severity));
    }

    /// <summary>The N7 routing: the line, the Warning, the category guard, and the per-setting Debug loop, as PluginConfigSync writes them.</summary>
    private static void Route(ILogSink sink, ConfigSyncOutcome outcome)
    {
        string? line = ConfigSyncMessages.Format(outcome);
        if (line is not null)
        {
            if (ConfigSyncMessages.IsWarning(outcome))
            {
                sink.Log(LogSeverity.Warning, line);
            }
            else if (LogPolicy.IsEnabled(sink, ConfigSyncMessages.CategoryOf(outcome)))
            {
                LogPolicy.Write(sink, ConfigSyncMessages.CategoryOf(outcome), line);
            }
        }

        if (outcome.Action == ConfigSyncAction.Applied && outcome.Payload is not null && LogPolicy.IsEnabled(sink, LogCategory.Detail))
        {
            foreach (KeyValuePair<string, string> pair in outcome.Payload.Settings)
            {
                LogPolicy.Write(sink, LogCategory.Detail, ConfigSyncMessages.SettingDetail(pair.Key, pair.Value));
            }
        }
    }

    private static ConfigSyncOutcome Applied()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        return session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);
    }

    private static ConfigSyncOutcome Malformed()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        return session.Receive(1, ConfigSyncFixtures.Utf8("BSC1;garbage"), ConfigSyncMode.Client);
    }
}
