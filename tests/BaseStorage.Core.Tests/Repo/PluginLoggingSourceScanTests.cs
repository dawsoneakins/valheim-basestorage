namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// S-E001-005 Plugin wiring (log-policy-contract.md v1, section 5, P1-P6; ADR-GWIN-022, ADR-GWIN-023). Tests never load the
/// Plugin, so these are text scans of its sources, comments removed.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginLoggingSourceScanTests
{
    private const string UiFile = "src/BaseStorage.Plugin/Ui/VanillaButtonFactory.cs";
    private const string UiFolder = "src/BaseStorage.Plugin/Ui/";
    private const string BootMarker = @"\[MethodImpl\(MethodImplOptions\.NoInlining\)\]\s*(?:private\s+)?void\s+Boot\s*\(\s*\)";

    // GW-U-216
    [Fact]
    public void InfoIsWrittenOnlyThroughThePolicyAndTheSinkSwitch()
    {
        Dictionary<string, string> tree = SourceTree();
        bool IsAllowed(string file) =>
            file.StartsWith("src/BaseStorage.Core/Logging/", StringComparison.Ordinal)
            || file == PluginSourceScan.SinkFile;

        Assert.Empty(Offenders(tree, @"\bLogSeverity\.Info\b", IsAllowed));
        Assert.Empty(Offenders(tree, @"\.LogInfo\s*\(", file => file == PluginSourceScan.SinkFile));
        Assert.Empty(Offenders(tree, @"\.LogMessage\s*\(", file => file == PluginSourceScan.SinkFile));
    }

    // GW-U-217
    [Fact]
    public void BootWritesTheLoadLineThroughThePolicyExactlyOnce()
    {
        Dictionary<string, string> sources = Sources();
        string main = sources[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(main, BootMarker);

        Assert.Equal(
            1,
            CountOf(boot, @"LogPolicy\.Write\s*\(\s*sink\s*,\s*LogCategory\.Load\s*,\s*BootMessages\.Loaded\s*\(\s*PluginVersion\s*\)\s*\)"));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"BootMessages\.Loaded\s*\("));

        string awake = PluginSourceScan.MethodBody(main, @"void\s+Awake\s*\(\s*\)");
        Assert.Matches(@"Logger\.LogError\s*\(\s*BootMessages\.LoadFailed\s*\(\s*PluginVersion\s*,\s*\w+\s*\)\s*\)", awake);
    }

    // GW-U-218
    [Fact]
    public void BootWrapsTheSinkOnceInTheDebugGateAndPublishesItThroughLogSink()
    {
        Dictionary<string, string> sources = Sources();
        string main = sources[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(main, BootMarker);

        Assert.Equal(
            new[] { PluginSourceScan.EntryPoint },
            PluginSourceScan.FilesMatching(sources, @"new\s+DebugGatedLogSink\s*\("));
        Assert.Equal(
            new[] { PluginSourceScan.EntryPoint },
            PluginSourceScan.FilesMatching(sources, @"new\s+BepInExLogSink\s*\("));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"new\s+DebugGatedLogSink\s*\("));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"new\s+BepInExLogSink\s*\("));

        Match gateDeclaration = Regex.Match(
            boot,
            @"var\s+(\w+)\s*=\s*new\s+DebugGatedLogSink\s*\(\s*new\s+BepInExLogSink\s*\(\s*Logger\s*\)\s*,");
        Assert.True(gateDeclaration.Success, "Boot() does not declare the gate over new BepInExLogSink(Logger).");
        string gate = gateDeclaration.Groups[1].Value;
        Assert.Matches(@"\(\)\s*=>\s*Settings\?\.DebugLogging\s*==\s*true", boot);

        Assert.Matches(@"internal\s+static\s+ILogSink\?\s+LogSink\s*\{\s*get;\s*private\s+set;\s*\}", main);
        Assert.Equal(1, CountOf(main, @"\bLogSink\s*=(?!=)"));

        int gateIndex = gateDeclaration.Index;
        Match publish = Regex.Match(boot, @"\bLogSink\s*=(?!=)");
        Assert.True(publish.Success, "Boot() does not assign LogSink.");
        Match apply = Regex.Match(boot, @"PatchApplier\.ApplyAll\s*\(");
        Assert.True(apply.Success, "Boot() does not call PatchApplier.ApplyAll.");
        Assert.True(gateIndex < publish.Index, "LogSink is assigned before the gate is constructed.");
        Assert.True(publish.Index < apply.Index, "LogSink is assigned after PatchApplier.ApplyAll.");

        Assert.Matches(
            @"PatchSafety\.Initialize\s*\(\s*new\s+RateLimitedErrorReporter\s*\(\s*" + Regex.Escape(gate) + @"\s*\)\s*\)",
            boot);
        Assert.Matches(@"PatchApplier\.ApplyAll\s*\(\s*units\s*,\s*" + Regex.Escape(gate) + @"\s*\)", boot);
    }

    // GW-U-219
    [Fact]
    public void TheInnerSinkDoesNotGateDebugItself()
    {
        Dictionary<string, string> sources = Sources();
        string sink = sources[PluginSourceScan.SinkFile];

        Assert.DoesNotMatch(@"\bdebugEnabled\b", sink);
        Assert.Matches(@"internal\s+BepInExLogSink\s*\(\s*ManualLogSource\s+\w+\s*\)", sink);
        Assert.DoesNotMatch(@"BepInExLogSink\s*\([^)]*\bbool\b", sink);

        Match isEnabled = Regex.Match(
            sink,
            @"bool\s+IsEnabled\s*\(\s*LogSeverity\s+\w+\s*\)\s*(?:=>[^;]*;|\{[^}]*\})");
        Assert.True(isEnabled.Success, "BepInExLogSink does not declare IsEnabled(LogSeverity).");
        Assert.DoesNotMatch(@"LogSeverity\.Debug", isEnabled.Value);
        Assert.Matches(@"\btrue\b", isEnabled.Value);
    }

    // GW-U-220
    [Fact]
    public void UiFallbackLinesGoThroughTheGatedPolicyAndKeepTheirOncePerIdRule()
    {
        Dictionary<string, string> sources = Sources();

        Assert.Equal(new[] { PluginSourceScan.SinkFile }, PluginSourceScan.FilesMatching(sources, @"\.LogDebug\s*\("));
        foreach (KeyValuePair<string, string> pair in sources.Where(p => p.Key.StartsWith(UiFolder, StringComparison.Ordinal)))
        {
            Assert.DoesNotMatch(@"CreateLogSource", pair.Value);
            Assert.DoesNotMatch(@"ManualLogSource", pair.Value);
        }

        string ui = sources[UiFile];
        string logOnce = PluginSourceScan.MethodBody(ui, @"void\s+LogOnce\s*\(\s*string\s+\w+\s*,\s*string\s+\w+\s*\)");
        Match check = Regex.Match(logOnce, @"LogPolicy\.IsEnabled\s*\(\s*BaseStoragePlugin\.LogSink\s*,\s*LogCategory\.UiFallback\s*\)");
        Match mark = Regex.Match(logOnce, @"Logged\.Add\s*\(");
        Match write = Regex.Match(
            logOnce,
            @"LogPolicy\.Write\s*\(\s*BaseStoragePlugin\.LogSink\s*,\s*LogCategory\.UiFallback\s*,\s*""\[""\s*\+\s*id\s*\+\s*""\]\s*""\s*\+\s*");
        Assert.True(check.Success, "LogOnce does not check LogPolicy.IsEnabled(BaseStoragePlugin.LogSink, LogCategory.UiFallback).");
        Assert.True(mark.Success, "LogOnce does not add to the Logged set.");
        Assert.True(write.Success, "LogOnce does not write through LogPolicy.Write with the [id] prefix.");
        Assert.True(check.Index < mark.Index, "LogOnce marks the id before it checks whether the line is enabled.");
        Assert.True(mark.Index < write.Index, "LogOnce writes the line before it marks the id.");

        // Edited by S-E004-012 (ADR-GWIN-136): plus CreateText's B5 template-missing line (contract 4), 3 + 1 = 4.
        Assert.Equal(4, CountOf(ui, @"(?<!void\s)\bLogOnce\s*\("));
        Assert.Contains("LogOnce(id, \"template missing; control not created\");", ui, StringComparison.Ordinal);
        Assert.Contains("LogOnce(id, \"fallback: no label component\");", ui, StringComparison.Ordinal);
        Assert.Contains("LogOnce(IdOf(button.name), \"fallback: no label component\");", ui, StringComparison.Ordinal);
    }

    private static Dictionary<string, string> Sources()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.True(sources.ContainsKey(UiFile), $"Scan did not read {UiFile}.");
        return sources;
    }

    /// <summary>Every .cs file under src/, bin and obj excluded, with comments removed. Used for the repo-wide Info rule.</summary>
    private static Dictionary<string, string> SourceTree()
    {
        string folder = RepoRoot.Combine("src");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(RepoRoot.FullPath, file).Replace('\\', '/');
            if (relative.Split('/').Any(segment => segment is "bin" or "obj"))
            {
                continue;
            }

            result[relative] = PluginSourceScan.StripComments(RepoRoot.ReadText(relative));
        }

        Assert.True(result.ContainsKey(PluginSourceScan.EntryPoint), $"Scan did not read {PluginSourceScan.EntryPoint}.");
        Assert.True(result.ContainsKey(PluginSourceScan.SinkFile), $"Scan did not read {PluginSourceScan.SinkFile}.");
        return result;
    }

    private static int CountOf(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static string[] Offenders(Dictionary<string, string> tree, string pattern, Func<string, bool> isAllowed) =>
        tree.Where(pair => !isAllowed(pair.Key) && Regex.IsMatch(pair.Value, pattern))
            .Select(pair => pair.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
}
