namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Persistence;

/// <summary>
/// AC-4 and contract section 5: the ADR for the persistent keys is in docs/adr/ and matches the contract (ADR-GWIN-012, ADR-GWIN-013).
/// </summary>
[Trait("Category", "Repo")]
public sealed class AdrDocumentTests
{
    private const string AdrDirectory = "docs/adr";
    private const string AdrFile = "docs/adr/0001-basestorage-keys-ignored-by-vanilla.md";

    private static readonly Regex FileNamePattern = new(@"^\d{4}-[a-z0-9]+(-[a-z0-9]+)*\.md$", RegexOptions.CultureInvariant);

    private static readonly string[] Headings = ["Status", "Context", "Decision", "Consequences"];

    private static readonly string[] Keys = ["basestorage.cat", "basestorage.lock", "basestorage.fav"];

    private static readonly string[] RequiredPhrases =
    [
        "vanilla", "ignore", "uninstall", "intact", "reinstall", "no value", "throw",
    ];

    // GW-U-085
    [Fact]
    public void Adr0001MatchesContract()
    {
        string path = RepoRoot.Combine(AdrFile);
        Assert.True(File.Exists(path), $"Required repo file is missing: {AdrFile}");

        byte[] bytes = File.ReadAllBytes(path);
        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        Assert.False(hasBom, "ADR 0001 must not start with a UTF-8 byte order mark.");
        Assert.DoesNotContain((byte)'\r', bytes);

        string text = Encoding.UTF8.GetString(bytes);
        string[] lines = text.Split('\n');
        string firstLine = lines.First(line => line.Trim().Length > 0);
        Assert.StartsWith("# ADR 0001: ", firstLine, StringComparison.Ordinal);

        int previous = -1;
        foreach (string heading in Headings)
        {
            int index = Array.FindIndex(lines, line => line.TrimEnd() == "## " + heading);
            Assert.True(index >= 0, $"ADR 0001 has no '## {heading}' heading.");
            Assert.True(index > previous, $"'## {heading}' must come after the heading before it.");
            previous = index;
        }

        int statusIndex = Array.FindIndex(lines, line => line.TrimEnd() == "## Status");
        int nextHeading = Array.FindIndex(lines, statusIndex + 1, line => line.StartsWith("## ", StringComparison.Ordinal));
        int end = nextHeading < 0 ? lines.Length : nextHeading;
        string statusBody = string.Join("\n", lines[(statusIndex + 1)..end]);
        Assert.Contains("Accepted", statusBody, StringComparison.Ordinal);

        foreach (string key in Keys)
        {
            Assert.Contains(key, text, StringComparison.Ordinal);
        }

        foreach (string phrase in RequiredPhrases)
        {
            Assert.Contains(phrase, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    // GW-U-086
    [Fact]
    public void AdrDirectoryConventionAndKeyList()
    {
        string directory = RepoRoot.Combine(AdrDirectory);
        Assert.True(Directory.Exists(directory), $"{AdrDirectory} is missing.");

        string[] names = Directory.GetFiles(directory).Select(f => Path.GetFileName(f) ?? string.Empty).ToArray();
        string[] badNames = names.Where(name => !FileNamePattern.IsMatch(name)).ToArray();
        Assert.True(badNames.Length == 0, "ADR file names must match NNNN-kebab-case.md: " + string.Join(", ", badNames));
        Assert.Contains("0001-basestorage-keys-ignored-by-vanilla.md", names, StringComparer.Ordinal);

        string text = RepoRoot.ReadText(AdrFile);
        string[] allKeys = ((IEnumerable)PersistenceContract.Static("PersistenceKeys", "All")!).Cast<string>().ToArray();
        foreach (string key in allKeys)
        {
            Assert.Contains(key, text, StringComparison.Ordinal);
        }
    }
}
