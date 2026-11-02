namespace BaseStorage.Core.Tests.Repo;

using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-1 and AC-2 and AC-3 (contracts/plugin-boot.md section 2, ADR-GWIN-012/013): the Plugin entry point is checked by text
/// scans of its tracked sources, because the Plugin cannot be loaded by tests.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginBootSourceScanTests
{
    // GW-U-081
    [Fact]
    public void PluginIdentityIsDeclaredOnceWithTheContractedConstants()
    {
        var sources = PluginSourceScan.Sources();
        string main = sources[PluginSourceScan.EntryPoint];

        Assert.Equal(new[] { PluginSourceScan.EntryPoint }, PluginSourceScan.FilesMatching(sources, @"\[BepInPlugin\("));
        Assert.Matches(@"\[BepInPlugin\(\s*PluginGuid\s*,\s*PluginName\s*,\s*PluginVersion\s*\)\]", main);
        Assert.Matches(@"namespace\s+BaseStorage\.Plugin\s*;", main);
        Assert.Matches(@"public\s+sealed\s+class\s+BaseStoragePlugin\s*:\s*BaseUnityPlugin\b", main);
        Assert.Matches(@"public\s+const\s+string\s+PluginGuid\s*=\s*""dawso\.valheim\.basestorage""\s*;", main);
        Assert.Matches(@"public\s+const\s+string\s+PluginName\s*=\s*""Don's QOL""\s*;", main);
    }

    // GW-U-081
    [Fact]
    public void PluginDeclaresNoDependencyProcessOrIncompatibilityAttributes()
    {
        var sources = PluginSourceScan.Sources();

        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"\bBepIn(Dependency|Process|Incompatibility)\b"));
    }

    // GW-U-084
    [Fact]
    public void OneHarmonyInstanceIsCreatedInTheEntryPointAndNothingElsePatches()
    {
        var sources = PluginSourceScan.Sources();
        string main = sources[PluginSourceScan.EntryPoint];

        Assert.Equal(
            new[] { PluginSourceScan.EntryPoint },
            PluginSourceScan.FilesMatching(sources, @"new\s+(?:HarmonyLib\.)?Harmony\s*\("));
        Assert.Matches(@"new\s+(?:HarmonyLib\.)?Harmony\s*\(\s*PluginGuid\s*\)", main);
        Assert.Matches(@"private\s+(?:readonly\s+)?(?:HarmonyLib\.)?Harmony\??\s+\w+", main);
        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"\bPatchAll\s*\("));
        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"CreateAndPatchAll"));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"PatchApplier\.ApplyAll\s*\("));
    }

    // GW-U-1530 (S-E003-005, RD8, ADR-GWIN-127): the requirement display is initialized exactly once, between the crafting session and the patches.
    [Fact]
    public void BootInitializesTheRequirementDisplayOnceBetweenCraftingAndThePatches()
    {
        Assert.Equal(1, PluginSourceScan.CountMatches(PluginSourceScan.Sources(), @"PluginRequirementDisplay\.Initialize\("));

        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");
        Assert.Contains("PluginRequirementDisplay.Initialize(", boot, StringComparison.Ordinal);
    }

    // GW-U-1157 (S-E003-004, contract 5.3 and CR3): Boot creates the crafting session after the writer and before the patches.
    [Fact]
    public void BootInitializesTheCraftingSessionBetweenTheWriterAndThePatches()
    {
        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");

        Assert.Equal(1, PluginSourceScan.CountMatches(PluginSourceScan.Sources(), @"PluginCrafting\.Initialize\("));
        int writer = boot.IndexOf("ChestCategoryWriter.Initialize(", StringComparison.Ordinal);
        int crafting = boot.IndexOf("PluginCrafting.Initialize(", StringComparison.Ordinal);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);
        Assert.True(writer >= 0 && writer < crafting && crafting < apply, "Boot must call PluginCrafting.Initialize after ChestCategoryWriter.Initialize and before PatchApplier.ApplyAll.");
    }

    // GW-U-085
    [Fact]
    public void DiscoveryUsesDeclaredHarmonyPatchAttributeOrderedOrdinally()
    {
        var sources = PluginSourceScan.Sources();
        string discovery = sources[PluginSourceScan.DiscoveryFile];

        Assert.Matches(@"internal\s+static\s+Type\[\]\s+FindPatchClasses\s*\(\s*Assembly\s+\w+\s*\)", discovery);
        Assert.Matches(@"IsDefined\(\s*typeof\(HarmonyPatch\)\s*,\s*inherit\s*:\s*false\s*\)", discovery);
        Assert.Matches(@"OrderBy\(\s*\w+\s*=>\s*\w+\.FullName\s*,\s*StringComparer\.Ordinal\s*\)", discovery);
    }

    // GW-U-085
    [Fact]
    public void DiscoveryFilterKeepsStaticPatchClasses()
    {
        var sources = PluginSourceScan.Sources();
        string discovery = sources[PluginSourceScan.DiscoveryFile];

        // ADR-GWIN-012: a bare !x.IsAbstract would drop static classes (IL abstract sealed). Only the paired form is allowed.
        Assert.DoesNotMatch(@"!\s*\w+\.IsAbstract\b", discovery);
    }

    // GW-U-085
    [Fact]
    public void HarmonyPatchUnitWrapsOneClassProcessorWithoutCatchingExceptions()
    {
        var sources = PluginSourceScan.Sources();
        string unit = sources[PluginSourceScan.UnitFile];

        Assert.Matches(@"internal\s+sealed\s+class\s+HarmonyPatchUnit\s*:\s*IPatchUnit\b", unit);
        Assert.Matches(@"\bName\b[^;]*\bFullName\b", unit);
        Assert.Matches(@"CreateClassProcessor\s*\([^)]*\)\s*\.Patch\s*\(\s*\)", unit);
        Assert.Equal(new[] { PluginSourceScan.UnitFile }, PluginSourceScan.FilesMatching(sources, @"CreateClassProcessor\s*\("));
        Assert.DoesNotMatch(@"\btry\b|\bcatch\b", unit);
    }

    // GW-U-086
    [Fact]
    public void BootRunsTheStepsInContractOrderInsideANoInliningMethod()
    {
        var sources = PluginSourceScan.Sources();
        string main = sources[PluginSourceScan.EntryPoint];
        string body = PluginSourceScan.MethodBody(
            main,
            @"\[MethodImpl\(MethodImplOptions\.NoInlining\)\]\s*(?:private\s+)?void\s+Boot\s*\(\s*\)");

        string[] steps =
        {
            @"new\s+BepInExLogSink\s*\(\s*Logger\b",
            @"new\s+Harmony\s*\(\s*PluginGuid\s*\)",
            @"PatchDiscovery\.FindPatchClasses\s*\(\s*typeof\s*\(\s*BaseStoragePlugin\s*\)\s*\.Assembly\s*\)",
            @"PatchApplier\.ApplyAll\s*\(",
            @"LogPolicy\.Write\s*\(\s*sink\s*,\s*LogCategory\.Load\s*,\s*BootMessages\.Loaded\s*\(\s*PluginVersion\s*\)\s*\)",
        };

        int position = 0;
        foreach (string step in steps)
        {
            Match match = Regex.Match(body.Substring(position), step);
            Assert.True(match.Success, "Boot() does not contain, in this order, the step: " + step);
            position += match.Index + match.Length;
        }

        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"BootMessages\.Loaded\s*\("));
        Assert.Matches(@"void\s+Awake\s*\(\s*\)\s*\{[^}]*try\s*\{\s*Boot\s*\(\s*\)\s*;", main);
    }

    // GW-U-087
    [Fact]
    public void BepInExLogSinkMapsEverySeverityToItsBepInExCall()
    {
        var sources = PluginSourceScan.Sources();
        string sink = sources[PluginSourceScan.SinkFile];

        Assert.Matches(@"internal\s+sealed\s+class\s+BepInExLogSink\s*:\s*ILogSink\b", sink);
        Assert.Matches(@"\bManualLogSource\b", sink);
        Assert.Matches(@"(?s)case\s+LogSeverity\.Debug\s*:(?:(?!case\s).)*?LogDebug\s*\(", sink);
        Assert.Matches(@"(?s)case\s+LogSeverity\.Info\s*:(?:(?!case\s).)*?LogInfo\s*\(", sink);
        Assert.Matches(@"(?s)case\s+LogSeverity\.Warning\s*:(?:(?!case\s).)*?LogWarning\s*\(", sink);
        Assert.Matches(@"(?s)(?:case\s+LogSeverity\.Error\s*:|default\s*:)(?:(?!case\s).)*?LogError\s*\(", sink);
        Assert.Matches(@"catch\s*\(\s*Exception\b", sink);
    }

    // GW-U-087
    [Fact]
    public void BepInExLogSinkIsWrappedInTheDebugGateAtTheEntryPoint()
    {
        var sources = PluginSourceScan.Sources();
        string main = sources[PluginSourceScan.EntryPoint];
        string sink = sources[PluginSourceScan.SinkFile];

        Assert.Matches(@"new\s+DebugGatedLogSink\s*\(\s*new\s+BepInExLogSink\s*\(\s*Logger\s*\)\s*,", main);
        Assert.Matches(@"IsEnabled\s*\(", sink);
        Assert.DoesNotMatch(@"\bdebugEnabled\b", sink);
    }

    // GW-U-088
    [Fact]
    public void AwakeNeverThrowsAndLogsTheLoadFailureOnce()
    {
        var sources = PluginSourceScan.Sources();
        string main = sources[PluginSourceScan.EntryPoint];

        Assert.Matches(
            @"try\s*\{\s*Boot\s*\(\s*\)\s*;\s*\}\s*catch\s*\(\s*Exception\s+(\w+)\s*\)\s*\{\s*Logger\.LogError\s*\(\s*BootMessages\.LoadFailed\s*\(\s*PluginVersion\s*,\s*\1\s*\)\s*\)\s*;\s*\}",
            main);
        Assert.DoesNotMatch(@"\bthrow\b", main);
        Assert.DoesNotMatch(@"\bOnDestroy\b", main);
    }

    // GW-U-1642 (ADR-GWIN-132): Boot initializes the building session after the crafting session and before the patches.
    [Fact]
    public void BootInitializesBuildingAfterCraftingAndBeforeThePatches()
    {
        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");

        Assert.Equal(1, PluginSourceScan.CountMatches(PluginSourceScan.Sources(), @"PluginBuilding\.Initialize\("));
        int crafting = boot.IndexOf("PluginCrafting.Initialize(", StringComparison.Ordinal);
        int building = boot.IndexOf("PluginBuilding.Initialize(", StringComparison.Ordinal);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);
        Assert.True(crafting >= 0 && crafting < building && building < apply, "Boot must call PluginBuilding.Initialize after PluginCrafting.Initialize and before PatchApplier.ApplyAll.");
    }
}
