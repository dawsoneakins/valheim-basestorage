namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AC-1, AC-3 and AC-5 (ADR-GWIN-032, ADR-GWIN-033): text scans of the Plugin hover files. The patches cannot run in tests, so their shape,
/// their absence of base search and their absence of English text are read from the sources. Sources are comment-stripped and CRLF-normalised.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginHoverSourceScanTests
{
    private const string HoverFolder = "src/BaseStorage.Plugin/Hover/";
    private const string ContainerPatch = "src/BaseStorage.Plugin/Hover/ContainerHoverTextPatch.cs";
    private const string StationPatch = "src/BaseStorage.Plugin/Hover/CraftingStationHoverTextPatch.cs";
    private const string HoverLinesFile = "src/BaseStorage.Plugin/Hover/HoverLines.cs";

    private static readonly string[] ForbiddenInHover =
    {
        ".Resolve(", "GetStations", "GetContainers", "BaseResolver", "StationGraph", "CategoryCodec",
        "System.Linq", "new List", "new Dictionary", "MonoBehaviour",
    };

    private static readonly string[] EnglishLiterals = { "[In base", "Not in a base", "[Base:", "[Keep]", "chests]", "station]" };

    // GW-U-538 (AC-1): the chest postfix is one fail-closed call on Container.GetHoverText, and AppendChest asks the inspector for the line.
    [Fact]
    public void TheChestPatchIsOneFailClosedPostfixAndAppendChestUsesTheInspector()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.True(sources.ContainsKey(ContainerPatch), $"{ContainerPatch} is missing.");
        Assert.True(sources.ContainsKey(HoverLinesFile), $"{HoverLinesFile} is missing.");
        string patch = sources[ContainerPatch];
        string lines = sources[HoverLinesFile];

        Assert.Contains("[HarmonyPatch(typeof(Container), PatchTargetNames.Container.GetHoverText)]", patch);
        Assert.Contains("internal const string PatchKey = \"BaseStorage.Plugin.Hover.ContainerHoverTextPatch.Postfix\";", patch);

        string body = PluginSourceScan.MethodBody(
            patch,
            @"private\s+static\s+void\s+Postfix\s*\(\s*Container\s+__instance\s*,\s*ref\s+string\s+__result\s*\)");
        Assert.Equal(1, body.Count(c => c == ';'));
        Assert.Matches(
            @"__result\s*=\s*PatchSafety\.Run\(\s*PatchKey\s*,\s*__instance\s*,\s*__result\s*,\s*static\b[^;]*HoverLines\.AppendChest\(",
            body);

        string append = PluginSourceScan.MethodBody(lines, @"internal\s+static\s+string\s+AppendChest\s*\(");
        Assert.Contains("Containers.Contains(", append);
        Assert.Contains("PersistenceKeys.ChestCategory", append);
        Assert.Contains(".ChestLine(", append);
        Assert.Contains("HoverInspector.Append(", append);

        Assert.DoesNotMatch(@"\?\.|\?\?|\bis\s+(not\s+)?null\b", patch + "\n" + lines);
    }

    // GW-U-545 (AC-3): the station postfix is one fail-closed call on CraftingStation.GetHoverText, and AppendStation builds the options and asks the inspector.
    [Fact]
    public void TheStationPatchIsOneFailClosedPostfixAndAppendStationUsesTheInspector()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.True(sources.ContainsKey(StationPatch), $"{StationPatch} is missing.");
        Assert.True(sources.ContainsKey(HoverLinesFile), $"{HoverLinesFile} is missing.");
        string patch = sources[StationPatch];
        string lines = sources[HoverLinesFile];

        Assert.Contains("[HarmonyPatch(typeof(CraftingStation), PatchTargetNames.CraftingStation.GetHoverText)]", patch);
        Assert.Contains("internal const string PatchKey = \"BaseStorage.Plugin.Hover.CraftingStationHoverTextPatch.Postfix\";", patch);

        string body = PluginSourceScan.MethodBody(
            patch,
            @"private\s+static\s+void\s+Postfix\s*\(\s*CraftingStation\s+__instance\s*,\s*ref\s+string\s+__result\s*\)");
        Assert.Equal(1, body.Count(c => c == ';'));
        Assert.Matches(
            @"__result\s*=\s*PatchSafety\.Run\(\s*PatchKey\s*,\s*__instance\s*,\s*__result\s*,\s*static\b[^;]*HoverLines\.AppendStation\(",
            body);

        string append = PluginSourceScan.MethodBody(lines, @"internal\s+static\s+string\s+AppendStation\s*\(");
        Assert.Contains("Stations.Contains(", append);
        Assert.Contains(".StationLine(", append);
        Assert.Contains("HoverInspector.Append(", append);
        Assert.Contains("new BaseCacheOptions(settings.BaseCacheSeconds, settings.MaxBaseStations)", append);
    }

    // GW-U-557 (AC-5): the hover folder holds exactly three files, none of them searches a base or runs per frame, only HoverLines builds the
    // inspector and it uses the cache, and no English line text is a literal.
    [Fact]
    public void TheHoverFolderHasNoBaseSearchAndOnlyHoverLinesBuildsTheInspector()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        string[] files = sources.Keys
            .Where(key => key.StartsWith(HoverFolder, StringComparison.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { ContainerPatch, StationPatch, HoverLinesFile }, files);

        foreach (string file in files)
        {
            string text = sources[file];
            foreach (string token in ForbiddenInHover)
            {
                Assert.DoesNotContain(token, text);
            }

            Assert.DoesNotMatch(@"\b(Update|LateUpdate|FixedUpdate|OnGUI)\s*\(", text);
            foreach (string literal in EnglishLiterals)
            {
                Assert.DoesNotContain(literal, text);
            }
        }

        Assert.Equal(new[] { HoverLinesFile }, PluginSourceScan.FilesMatching(sources, @"new\s+HoverInspector\("));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"new\s+HoverInspector\("));
        Assert.Matches(@"new\s+HoverInspector\(\s*[\w.]*\.Cache\s*\)", sources[HoverLinesFile]);
    }
}
