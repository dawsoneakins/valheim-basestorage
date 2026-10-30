namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// AC-2 and AC-7 (ADR-GWIN-020 boot wiring, ADR-GWIN-021 linked helper): the reporter is created and the helper initialised
/// exactly once, in Boot(), before PatchApplier.ApplyAll. The helper file stays BCL and Core only.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginSafetySourceScanTests
{
    private const string HelperFile = "src/BaseStorage.Plugin/Patching/PatchSafety.cs";
    private const string ReporterConstruction = @"new\s+RateLimitedErrorReporter\s*\(";
    private const string InitializeCall = @"PatchSafety\.Initialize\s*\(";

    // GW-U-138 (a)
    [Fact]
    public void ReporterIsConstructedAndTheHelperInitialisedExactlyOnceInBoot()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();

        Assert.Equal(1, PluginSourceScan.CountMatches(sources, ReporterConstruction));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, InitializeCall));
        Assert.Equal(new[] { PluginSourceScan.EntryPoint }, PluginSourceScan.FilesMatching(sources, ReporterConstruction));
        Assert.Equal(new[] { PluginSourceScan.EntryPoint }, PluginSourceScan.FilesMatching(sources, InitializeCall));

        string boot = PluginSourceScan.MethodBody(sources[PluginSourceScan.EntryPoint], @"void\s+Boot\s*\(\s*\)");
        Assert.True(Regex.IsMatch(boot, ReporterConstruction), "Boot() does not construct the RateLimitedErrorReporter.");
        Assert.True(Regex.IsMatch(boot, InitializeCall), "Boot() does not call PatchSafety.Initialize.");
    }

    // GW-U-138 (b)
    [Fact]
    public void BootWiresTheReporterAfterTheSinkAndBeforeThePatchesAreApplied()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        string boot = PluginSourceScan.MethodBody(sources[PluginSourceScan.EntryPoint], @"void\s+Boot\s*\(\s*\)");

        int sinkIndex = IndexOf(boot, @"new\s+BepInExLogSink\s*\(\s*Logger");
        int initializeIndex = IndexOf(boot, @"PatchSafety\s*\.\s*Initialize\s*\(\s*new\s+RateLimitedErrorReporter\s*\(\s*sink\s*\)\s*\)");
        int applyIndex = IndexOf(boot, @"PatchApplier\s*\.\s*ApplyAll\s*\(");

        Assert.True(sinkIndex < initializeIndex, "PatchSafety.Initialize(new RateLimitedErrorReporter(sink)) must follow the BepInExLogSink.");
        Assert.True(initializeIndex < applyIndex, "PatchSafety.Initialize must run before PatchApplier.ApplyAll.");
    }

    // GW-U-138 (c)
    [Fact]
    public void PatchSafetyIsAnInternalStaticClassWithOnlyBclAndCoreUsings()
    {
        string code = PluginSourceScan.StripComments(RepoRoot.ReadText(HelperFile));

        Assert.Matches(@"namespace\s+BaseStorage\.Plugin\.Patching\s*;", code);
        Assert.Matches(@"internal\s+static\s+class\s+PatchSafety\b", code);
        Assert.DoesNotMatch(@"using\s+(HarmonyLib|BepInEx|UnityEngine)\b", code);
    }

    // GW-U-138 (d)
    [Fact]
    public void CoreTestsCompileTheHelperSourceAndDoNotReferenceThePlugin()
    {
        XDocument project = RepoRoot.LoadXml(ContractPaths.CoreTestsProject);

        string[] compiles = RepoRoot.Named(project, "Compile")
            .Select(element => RepoRoot.Attr(element, "Include").Replace('\\', '/'))
            .ToArray();
        Assert.Contains(compiles, include => include.EndsWith("src/BaseStorage.Plugin/Patching/PatchSafety.cs", StringComparison.Ordinal));

        string[] references = RepoRoot.Named(project, "ProjectReference")
            .Select(element => RepoRoot.Attr(element, "Include").Replace('\\', '/'))
            .ToArray();
        Assert.DoesNotContain(references, include => include.Contains("BaseStorage.Plugin", StringComparison.Ordinal));
    }

    private static int IndexOf(string text, string pattern)
    {
        Match match = Regex.Match(text, pattern);
        Assert.True(match.Success, $"Boot() does not contain {pattern}.");
        return match.Index;
    }
}
