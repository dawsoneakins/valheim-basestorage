namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Ui;

/// <summary>
/// S-FOUND-001 AC-3: textual scans of src/BaseStorage.Plugin (ADR-GWIN-010, ADR-GWIN-011). The Plugin is net462 and
/// cannot be loaded by tests, so the rules P-S1..P-S4 are checked as source text. Files are read CRLF-normalised,
/// bin/ and obj/ are excluded, and // comments are stripped before matching. Every scan first asserts that the
/// three Ui files were read, so a scan with no files to read cannot pass.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginUiSourceScanTests
{
    private const string PluginDirectory = "src/BaseStorage.Plugin";
    private const string NoticesFile = "Ui/Notices.cs";
    private const string PlacementFile = "Ui/NoticePlacement.cs";
    private const string FactoryFile = "Ui/VanillaButtonFactory.cs";
    private const string BaseStorageLiteral = "\"BaseStorage.\"";
    private const string BaseStorageUiLiteral = "\"BaseStorage.Ui\"";

    private static readonly string[] RequiredUiFiles = [NoticesFile, PlacementFile, FactoryFile];
    private static readonly string[] AllowedStringParameters = ["key", "id", "labelKey"];

    // Regular expression for a C# string literal on one line: "(?:[^"\\\n]|\\.)*"
    private static readonly Regex LiteralPattern = new(@"""(?:[^""\\\n]|\\.)*""", RegexOptions.CultureInvariant);

    // GW-U-061
    [Fact]
    public void UiFilesDeclareTheContractNamespace()
    {
        Dictionary<string, string> sources = PluginSources();

        foreach (string file in RequiredUiFiles)
        {
            Assert.Contains("namespace BaseStorage.Plugin.Ui;", sources[file]);
        }
    }

    // GW-U-061
    [Fact]
    public void NoticesAndVanillaButtonFactoryAreEachDeclaredInExactlyOneFile()
    {
        Dictionary<string, string> sources = PluginSources();

        Assert.Equal(new[] { NoticesFile }, FilesMatching(sources, @"internal\s+static\s+class\s+Notices\b"));
        Assert.Equal(new[] { FactoryFile }, FilesMatching(sources, @"internal\s+static\s+class\s+VanillaButtonFactory\b"));
    }

    // GW-U-061
    [Fact]
    public void NoticePlacementDeclaresExactlyCenterAndTopLeft()
    {
        string text = PluginSources()[PlacementFile];

        Match declaration = Regex.Match(text, @"enum\s+NoticePlacement\s*\{(?<body>[^}]*)\}");
        Assert.True(declaration.Success, "NoticePlacement.cs does not declare enum NoticePlacement.");

        string[] members = declaration.Groups["body"].Value
            .Split(',')
            .Select(m => m.Trim())
            .Where(m => m.Length > 0)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "Center", "TopLeft" }, members);
    }

    // GW-U-061
    [Fact]
    public void ContractSignaturesArePresent()
    {
        Dictionary<string, string> sources = PluginSources();

        Assert.Matches(
            @"public\s+static\s+void\s+Show\s*\(\s*NoticePlacement\s+placement\s*,\s*string\s+key\s*,\s*params\s+object\?\[\]\?\s+args\s*\)",
            sources[NoticesFile]);
        Assert.Matches(
            @"public\s+static\s+Button\?\s+Create\s*\(\s*Button\?\s+template\s*,\s*Transform\s+parent\s*,\s*string\s+id\s*,\s*string\s+labelKey\s*,\s*UnityAction\s+onClick\s*\)",
            sources[FactoryFile]);
        Assert.Matches(
            @"public\s+static\s+void\s+SetLabel\s*\(\s*Button\s+button\s*,\s*string\s+labelKey\s*,\s*params\s+object\?\[\]\?\s+args\s*\)",
            sources[FactoryFile]);
    }

    // GW-U-061
    [Fact]
    public void NoOtherPublicOrInternalMethodTakesAStringBeyondKeyIdAndLabelKey()
    {
        Dictionary<string, string> sources = PluginSources();
        var violations = new List<string>();

        foreach (string file in new[] { NoticesFile, FactoryFile })
        {
            foreach (Match method in Regex.Matches(
                         sources[file],
                         @"\b(?:public|internal)\s+(?:static\s+)?[\w<>\[\]?.]+\s+(?<name>\w+)\s*\((?<params>[^)]*)\)"))
            {
                foreach (string parameter in method.Groups["params"].Value.Split(',').Select(p => p.Trim()))
                {
                    Match stringParameter = Regex.Match(parameter, @"^(?:params\s+)?string\??\s+(?<name>\w+)$");
                    if (stringParameter.Success && !AllowedStringParameters.Contains(stringParameter.Groups["name"].Value))
                    {
                        violations.Add(file + ": " + method.Groups["name"].Value + "(" + parameter + ")");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    // GW-U-062
    [Fact]
    public void ShowMessageOccursExactlyOnceInTheNoticeHelper()
    {
        Dictionary<string, string> sources = PluginSources();

        Assert.Equal(new[] { NoticesFile }, FilesContaining(sources, ".ShowMessage("));
        Assert.Equal(1, OccurrencesIn(sources, ".ShowMessage("));
    }

    // GW-U-062
    [Fact]
    public void PlayerMessagePathOccursNowhere()
    {
        Assert.Empty(FilesContaining(PluginSources(), ".Message(MessageHud.MessageType"));
    }

    // GW-U-063
    [Fact]
    public void InstantiateOccursOnlyInTheButtonFactory()
    {
        Dictionary<string, string> sources = PluginSources();
        const string markerFile = "Favourites/FavouriteMarkers.cs";

        Assert.Equal(new[] { markerFile, FactoryFile }, FilesContaining(sources, "Instantiate("));
        Assert.True(sources.ContainsKey(markerFile), $"Plugin file is missing: {PluginDirectory}/{markerFile}");
        Assert.Single(Regex.Matches(sources[markerFile], @"Instantiate\("));
        Assert.Contains("Instantiate(element.m_queued,", sources[markerFile], StringComparison.Ordinal);
    }

    // GW-U-063
    [Fact]
    public void ButtonComponentsAndGameObjectsAreNotCreatedOutsideTheFactory()
    {
        Dictionary<string, string> sources = PluginSources();

        Assert.Empty(FilesContaining(sources, "AddComponent<Button>"));
        Assert.Equal(new[] { "Ui/VanillaButtonFactory.cs" }, FilesContaining(sources, "new GameObject("));
    }

    // GW-U-063
    [Fact]
    public void FactoryReplacesTheClickEventWithAFreshOne()
    {
        Assert.Contains("new Button.ButtonClickedEvent()", PluginSources()[FactoryFile]);
    }

    // GW-U-064
    [Fact]
    public void HelperAndFactoryStringLiteralsAreNamePrefixesOrDebugLines()
    {
        Dictionary<string, string> sources = PluginSources();
        var violations = new List<string>();

        foreach (string file in new[] { NoticesFile, FactoryFile })
        {
            foreach ((int line, string lineText, string literal) in LiteralsOf(sources[file]))
            {
                if (literal == BaseStorageLiteral || literal == BaseStorageUiLiteral)
                {
                    continue;
                }

                if (lineText.Contains("LogOnce(", StringComparison.Ordinal)
                    || lineText.Contains("LogDebug(", StringComparison.Ordinal)
                    || lineText.Contains("LogPolicy.Write(", StringComparison.Ordinal))
                {
                    continue;
                }

                violations.Add(file + ":" + line + ": " + literal);
            }
        }

        Assert.Empty(violations);
    }

    // GW-U-064
    [Fact]
    public void HelperAndFactoryHaveNoInterpolatedStrings()
    {
        Dictionary<string, string> sources = PluginSources();

        Assert.DoesNotContain("$\"", sources[NoticesFile]);
        Assert.DoesNotContain("$\"", sources[FactoryFile]);
    }

    // GW-U-064
    [Fact]
    public void HelperAndFactoryRouteTheirTextThroughTheStringTable()
    {
        Dictionary<string, string> sources = PluginSources();

        Assert.Contains("SingleLine.Of(StringTable.English.Format(", sources[NoticesFile]);
        Assert.Contains("SingleLine.Of(StringTable.English.Format(", sources[FactoryFile]);
    }

    // GW-U-065
    [Fact]
    public void NoPluginLiteralEqualsOrContainsAContractEnglishText()
    {
        Dictionary<string, string> sources = PluginSources();
        var violations = new List<string>();

        foreach ((string file, string text) in sources)
        {
            foreach ((int line, string _, string literal) in LiteralsOf(text))
            {
                string inner = literal.Substring(1, literal.Length - 2);
                foreach ((string _, string english) in UiFixtures.EnglishTable)
                {
                    bool equal = inner == english;
                    bool contains = english.Length >= 8 && inner.Contains(english, StringComparison.Ordinal);
                    if (equal || contains)
                    {
                        violations.Add(file + ":" + line + ": " + literal + " vs \"" + english + "\"");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    /// <summary>
    /// Every *.cs file under src/BaseStorage.Plugin (bin/ and obj/ excluded), keyed by its path relative to the
    /// Plugin folder with forward slashes. CRLF-normalised, with // comments stripped. Fails unless the three Ui files were read.
    /// </summary>
    private static Dictionary<string, string> PluginSources()
    {
        string root = RepoRoot.Combine(PluginDirectory);
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);

        if (Directory.Exists(root))
        {
            foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (relative.Split('/').Any(segment => segment == "bin" || segment == "obj"))
                {
                    continue;
                }

                sources[relative] = StripLineComments(RepoRoot.Normalise(File.ReadAllText(file)));
            }
        }

        foreach (string required in RequiredUiFiles)
        {
            Assert.True(sources.ContainsKey(required), $"Plugin Ui file is missing: {PluginDirectory}/{required}");
        }

        return sources;
    }

    [Theory]
    [InlineData("var a = 1; // note", "var a = 1; ")]
    [InlineData("var u = \"http://x\";", "var u = \"http://x\";")]
    [InlineData("var c = '\"'; // tail", "var c = '\"'; ")]
    [InlineData("var e = \"a\\\"//b\"; // tail", "var e = \"a\\\"//b\"; ")]
    [InlineData("// whole line", "")]
    [InlineData("var n = 1;", "var n = 1;")]
    public void StripLineRemovesOnlyRealTrailingComments(string line, string expected)
    {
        Assert.Equal(expected, StripLine(line));
    }

    private static string StripLineComments(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (string line in text.Split('\n'))
        {
            builder.Append(StripLine(line)).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>Removes a // comment that starts outside a string or char literal.</summary>
    private static string StripLine(string line)
    {
        bool inString = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inString)
            {
                if (c == '\\')
                {
                    i++;
                }
                else if (c == '"')
                {
                    inString = false;
                }
            }
            else if (c == '"')
            {
                inString = true;
            }
            else if (c == '\'')
            {
                i++;
                if (i < line.Length && line[i] == '\\')
                {
                    i++;
                }

                i++;
            }
            else if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                return line.Substring(0, i);
            }
        }

        return line;
    }

    /// <summary>Every string literal on each line, with its 1-based line number and the whole line text.</summary>
    private static IEnumerable<(int Line, string LineText, string Literal)> LiteralsOf(string text)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (Match match in LiteralPattern.Matches(lines[i]))
            {
                yield return (i + 1, lines[i], match.Value);
            }
        }
    }

    private static string[] FilesContaining(Dictionary<string, string> sources, string needle) =>
        sources.Where(s => s.Value.Contains(needle, StringComparison.Ordinal))
            .Select(s => s.Key)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToArray();

    private static string[] FilesMatching(Dictionary<string, string> sources, string pattern) =>
        sources.Where(s => Regex.IsMatch(s.Value, pattern))
            .Select(s => s.Key)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToArray();

    private static int OccurrencesIn(Dictionary<string, string> sources, string needle) =>
        sources.Values.Sum(text => Regex.Matches(text, Regex.Escape(needle)).Count);
}
