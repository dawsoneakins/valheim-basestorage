namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-7, AC-8 and AC-9 (S-E009-001, ADR-GWIN-163, contract quality-audit 5.1 to 5.6): CHANGELOG.md, package/README.md and
/// docs/PLAYTEST.md agree with the 0.1.0 feature table in ReleaseFeatures, and the release version equals Version.props. Every fact
/// collects all its problems and asserts once. Every fact also runs its parser on an LF sample and on the same sample with CRLF.
/// </summary>
[Trait("Category", "Repo")]
public sealed class ReleaseDocsConsistencyTests
{
    private const string ChangelogFile = "CHANGELOG.md";
    private const string PackageReadmeFile = "package/README.md";
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string ManifestFile = "package/manifest.json";
    private const string ReleaseHeadingPattern = @"^## \[0\.1\.0\] - (\d{4}-\d{2}-\d{2}|Unreleased)$";
    private const string ReleaseVersionForSamples = "0.1.0";
    private const string UnreleasedHeading = "## [Unreleased]";
    private const string AddedHeading = "### Added";
    private const string DeveloperHeading = "### Developer";
    private const string ChangedHeading = "### Changed";
    private const string WhatItDoesHeading = "## What it does";
    private const string VersionPlaceholderLine = "\"version_number\": \"__VERSION__\"";
    private static readonly string[] UnreleasedSubsectionHeadings = { "### Added", "### Changed", "### Developer", "### Fixed" };

    private static readonly string[] PackageReadmeHeadings =
    {
        "## What it does", "## Installation", "## Configuration", "## Categories file", "## Chest categories",
        "## Station buttons", "## Console command", "## Known limits", "## Uninstall",
    };

    private static readonly string[] WhatItDoesLeads =
    {
        "**Craft and build from your chests.**", "**Chest categories.**", "**Organize base.**", "**Put away inventory.**", "**See your base.**",
    };

    private static readonly string[] PlaytestStructuralHeadings =
    {
        "## Setup", "## Solo", "## Host plus one friend", "## Acceptance script (AC-2.1 to AC-6.3)",
        "## Station feeder and Keep chests (S-QF-001, R4)",
    };

    private const string LayoutSample =
        "# Changelog\n\n## [Unreleased]\n\nNo changes yet.\n\n## [0.1.0] - 2026-10-29\n\n### Added\n\n- Bases: stations.\n### Developer\n\n- Quality audit: sample.\n\n### Changed\n\n- Crafting and building with materials from base chests now confirm with one short message, and every action ends in exactly one message.\n";

    private const string BulletSample =
        "# Changelog\n\n## [0.1.0] - 2026-10-29\n\n### Added\n\n- Bases: stations.\n\n### Developer\n\n- Quality audit: sample.\n\n### Changed\n\n- Crafting and building with materials from base chests now confirm with one short message, and every action ends in exactly one message.\n";

    private const string PackageSample =
        "# BaseStorage\n\n## What it does\n\n- **See your base.** Hover a chest.\n\n## Uninstall\n\nRemove the file.\n";

    private const string PlaytestSample =
        "# PLAYTEST\n\n## Setup\n\n## Feedback messages (AC-8.1, AC-7.3)\n\n## Stray section\n";

    private const string UnreleasedChangedSample =
        "# Changelog\n\n## [Unreleased]\n\n### Changed\n\n- Organize gives mixed chests a category.\n\n## [0.1.0] - 2026-10-29\n\n### Added\n\n- Bases: stations.\n\n### Developer\n\n- Quality audit: sample.\n\n### Changed\n\n- Crafting and building with materials from base chests now confirm with one short message, and every action ends in exactly one message.\n";

    private const string VersionSample =
        "# Changelog\n\n## [Unreleased]\n\nNo changes yet.\n\n## [0.1.0] - 2026-10-29\n";

    private const string ManifestSample =
        "{\n  \"version_number\": \"__VERSION__\"\n}\n";

    // GW-U-2370 (AC-7, QA4.1, QA4.6): the CHANGELOG has the release layout of contract 5.1.
    [Fact]
    public void TheChangelogHasTheReleaseLayout()
    {
        AssertSameProblemsOnBothLineEndings(ChangelogLayoutProblems, LayoutSample);
        AssertNoProblems(ChangelogLayoutProblems(RepoRoot.ReadText(ChangelogFile)));
    }

    // GW-U-2457 (AC-11, ADR-GWIN-166): [Unreleased] may hold ### Changed bullets, on LF and CRLF; the 0.1.0 rules are unchanged.
    [Fact]
    public void TheUnreleasedSectionMayHoldSubsectionBullets()
    {
        AssertSameProblemsOnBothLineEndings(ChangelogLayoutProblems, UnreleasedChangedSample);
        AssertNoProblems(ChangelogLayoutProblems(UnreleasedChangedSample));
        AssertNoProblems(ChangelogLayoutProblems(UnreleasedChangedSample.Replace("\n", "\r\n", StringComparison.Ordinal)));
    }

    // GW-U-2371 (AC-7, QA4.5): the release heading version equals Version.props, and the manifest keeps the placeholder.
    [Fact]
    public void TheReleaseVersionMatchesVersionProps()
    {
        AssertSameProblemsOnBothLineEndings(text => VersionProblems(text, ManifestSample, ReleaseVersionForSamples), VersionSample);
        AssertNoProblems(VersionProblems(RepoRoot.ReadText(ChangelogFile), RepoRoot.ReadText(ManifestFile), ReleaseVersionForSamples)); // The changelog stops at 0.1.0; v0.2 is in docs/RELEASE-NOTES-v0.2.md.
    }

    // GW-U-2372 (AC-7, QA4.2, QA4.6): every user-facing bullet is a shipped feature, in table order, and no Developer bullet is one.
    [Fact]
    public void EveryUserFacingBulletIsAShippedFeatureInTableOrder()
    {
        AssertSameProblemsOnBothLineEndings(BulletProblems, BulletSample);
        AssertNoProblems(BulletProblems(RepoRoot.ReadText(ChangelogFile)));
    }

    // GW-U-2373 (AC-8, QA4.4): package/README.md describes every feature and nothing else.
    [Fact]
    public void PackageReadmeDescribesEveryFeatureAndNothingElse()
    {
        AssertSameProblemsOnBothLineEndings(PackageReadmeProblems, PackageSample);
        AssertNoProblems(PackageReadmeProblems(RepoRoot.ReadText(PackageReadmeFile)));
    }

    // GW-U-2374 (AC-9, QA4.6): docs/PLAYTEST.md has a section for every feature and no other.
    [Fact]
    public void PlaytestHasASectionForEveryFeatureAndNoOther()
    {
        AssertSameProblemsOnBothLineEndings(PlaytestProblems, PlaytestSample);
        AssertNoProblems(PlaytestProblems(RepoRoot.ReadText(PlaytestFile)));
    }

    /// <summary>The body of '## [Unreleased]' when it holds only the line 'No changes yet.'.</summary>
    private static bool IsNoChangesBody(string[] body) => body.Length == 1 && body[0] == "No changes yet.";

    /// <summary>
    /// The body of '## [Unreleased]' when its first line is one of the four subsection headings and every line is such a heading or a '- '
    /// bullet (ADR-GWIN-166, S-A-001). The 0.1.0 rules are not affected.
    /// </summary>
    private static bool IsUnreleasedSubsectionBody(string[] body) =>
        body.Length > 0
        && Array.IndexOf(UnreleasedSubsectionHeadings, body[0]) >= 0
        && body.All(line => Array.IndexOf(UnreleasedSubsectionHeadings, line) >= 0 || line.StartsWith("- ", StringComparison.Ordinal));

    /// <summary>Problems with the 0.1.0 layout of contract 5.1. The rules name the offending line.</summary>
    private static List<string> ChangelogLayoutProblems(string text)
    {
        var problems = new List<string>();
        string[] lines = Lines(text);
        void Problem(string rule, string found) => problems.Add($"CHANGELOG.md: {rule} (found '{found}')");

        if (!lines.Contains("# Changelog"))
        {
            Problem("the title line '# Changelog' is missing", "(none)");
        }

        int unreleased = Array.IndexOf(lines, UnreleasedHeading);
        if (unreleased < 0)
        {
            Problem("the '## [Unreleased]' heading is missing", "(none)");
        }
        else
        {
            string[] body = lines[(unreleased + 1)..NextLevel2(lines, unreleased)].Where(line => line.Length > 0).ToArray();
            if (!IsNoChangesBody(body) && !IsUnreleasedSubsectionBody(body))
            {
                Problem(
                    "'## [Unreleased]' must hold either only the line 'No changes yet.' or '### Added', '### Changed', '### Developer' and '### Fixed' headings with '- ' bullets",
                    body.FirstOrDefault() ?? "(none)");
            }
        }

        string[] releases = lines.Where(line => Regex.IsMatch(line, ReleaseHeadingPattern)).ToArray();
        if (releases.Length != 1)
        {
            Problem("exactly one '## [0.1.0] - <yyyy-mm-dd or Unreleased>' heading is required", releases.Length == 0 ? "(none)" : string.Join(" | ", releases));
        }

        foreach (string heading in lines.Where(line => line.StartsWith("## ", StringComparison.Ordinal)))
        {
            if (heading != UnreleasedHeading && !Regex.IsMatch(heading, ReleaseHeadingPattern))
            {
                Problem("no other '## ' heading is allowed", heading);
            }
        }

        int release = Array.FindIndex(lines, line => Regex.IsMatch(line, ReleaseHeadingPattern));
        if (release >= 0)
        {
            int end = NextLevel2(lines, release);
            string[] subsections = lines[(release + 1)..end].Where(line => line.StartsWith("### ", StringComparison.Ordinal)).ToArray();
            string[] expected = { AddedHeading, DeveloperHeading, ChangedHeading };
            if (!subsections.SequenceEqual(expected, StringComparer.Ordinal))
            {
                Problem("the 0.1.0 subsections must be '### Added', '### Developer' and '### Changed', in that order", subsections.Length == 0 ? "(none)" : string.Join(" | ", subsections));
            }
        }

        return problems;
    }

    /// <summary>Problems with the release heading version and the manifest placeholder.</summary>
    private static List<string> VersionProblems(string changelog, string manifest, string version)
    {
        var problems = new List<string>();
        string? heading = Lines(changelog).FirstOrDefault(line => Regex.IsMatch(line, ReleaseHeadingPattern));
        if (heading is null)
        {
            problems.Add("CHANGELOG.md has no '## [<version>] - <yyyy-mm-dd or Unreleased>' release heading");
        }
        else
        {
            string headingVersion = Regex.Match(heading, @"^## \[([^\]]+)\]").Groups[1].Value;
            if (headingVersion != version)
            {
                problems.Add($"CHANGELOG.md release heading is '{headingVersion}', but Version.props is '{version}'");
            }
        }

        if (!manifest.Contains(VersionPlaceholderLine, StringComparison.Ordinal))
        {
            problems.Add("package/manifest.json no longer holds " + VersionPlaceholderLine);
        }

        return problems;
    }

    /// <summary>Problems with the mapping of the CHANGELOG 0.1.0 bullets to the feature table.</summary>
    private static List<string> BulletProblems(string changelog)
    {
        var problems = new List<string>();
        IReadOnlyList<ReleaseFeature> features = ReleaseFeatures.All;
        Dictionary<string, List<string>> sections = ReleaseSubsections(Lines(changelog));
        List<string> added = Bucket(sections, AddedHeading);
        List<string> changed = Bucket(sections, ChangedHeading);
        List<string> developer = Bucket(sections, DeveloperHeading);
        List<string> userFacing = added.Concat(changed).ToList();

        int previous = -1;
        foreach (string bullet in added)
        {
            List<int> matches = MatchingFeatures(features, bullet);
            if (!ExactlyOneMatch(matches, bullet, problems))
            {
                continue;
            }

            int index = matches[0];
            if (index < previous || features[index].Id == "QF18")
            {
                problems.Add(OutOfTableOrder(features[index]));
            }

            previous = Math.Max(previous, index);
        }

        foreach (string bullet in changed)
        {
            List<int> matches = MatchingFeatures(features, bullet);
            if (!ExactlyOneMatch(matches, bullet, problems))
            {
                continue;
            }

            if (features[matches[0]].Id != "QF18")
            {
                problems.Add(OutOfTableOrder(features[matches[0]]));
            }
        }

        foreach (string bullet in developer)
        {
            if (MatchingFeatures(features, bullet).Count > 0)
            {
                problems.Add("Developer bullet describes a feature: '" + Head(bullet) + "'");
            }
        }

        foreach (ReleaseFeature feature in features)
        {
            foreach (string lead in feature.ChangelogLeads)
            {
                int count = userFacing.Count(bullet => bullet.StartsWith(lead, StringComparison.Ordinal));
                if (count == 0)
                {
                    problems.Add($"{feature.Id} ({feature.Name}): no CHANGELOG bullet starts with '{lead}'");
                }
                else if (count > 1)
                {
                    problems.Add($"{feature.Id} ({feature.Name}): more than one CHANGELOG bullet starts with '{lead}'");
                }
            }
        }

        return problems.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>Problems with package/README.md: every literal present, and only the allowed leads and headings.</summary>
    private static List<string> PackageReadmeProblems(string text)
    {
        var problems = new List<string>();
        foreach (ReleaseFeature feature in ReleaseFeatures.All)
        {
            foreach (string literal in feature.ReadmeLiterals)
            {
                if (!text.Contains(literal, StringComparison.Ordinal))
                {
                    problems.Add($"package/README.md does not describe {feature.Id} ({feature.Name}): missing '{literal}'");
                }
            }
        }

        bool inWhatItDoes = false;
        foreach (string line in Lines(text))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                inWhatItDoes = line == WhatItDoesHeading;
                if (!PackageReadmeHeadings.Contains(line, StringComparer.Ordinal))
                {
                    problems.Add($"package/README.md describes no shipped feature: '{line}'");
                }
            }
            else if (inWhatItDoes && line.StartsWith("- **", StringComparison.Ordinal))
            {
                string lead = BoldLead(line);
                if (!WhatItDoesLeads.Contains(lead, StringComparer.Ordinal))
                {
                    problems.Add($"package/README.md describes no shipped feature: '{lead}'");
                }
            }
        }

        return problems.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>Problems with docs/PLAYTEST.md: every feature heading once, and every level-2 heading is a feature or structural.</summary>
    private static List<string> PlaytestProblems(string text)
    {
        var problems = new List<string>();
        string[] lines = Lines(text);
        foreach (ReleaseFeature feature in ReleaseFeatures.All)
        {
            foreach (string heading in feature.PlaytestHeadings)
            {
                int count = lines.Count(line => line == heading);
                if (count == 0)
                {
                    problems.Add($"docs/PLAYTEST.md has no '{heading}' section for {feature.Id} ({feature.Name})");
                }
                else if (count > 1)
                {
                    problems.Add($"docs/PLAYTEST.md has '{heading}' more than once, for {feature.Id} ({feature.Name})");
                }
            }
        }

        HashSet<string> allowed = ReleaseFeatures.All
            .SelectMany(feature => feature.PlaytestHeadings)
            .Concat(PlaytestStructuralHeadings)
            .ToHashSet(StringComparer.Ordinal);
        foreach (string line in lines.Where(line => line.StartsWith("## ", StringComparison.Ordinal)))
        {
            if (!allowed.Contains(line))
            {
                problems.Add($"docs/PLAYTEST.md section '{line}' maps to no shipped feature");
            }
        }

        return problems.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>The bullets of each subsection of the 0.1.0 release, keyed by the subsection heading.</summary>
    private static Dictionary<string, List<string>> ReleaseSubsections(string[] lines)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        int release = Array.FindIndex(lines, line => Regex.IsMatch(line, ReleaseHeadingPattern));
        if (release < 0)
        {
            return result;
        }

        int end = NextLevel2(lines, release);
        string current = string.Empty;
        for (int i = release + 1; i < end; i++)
        {
            string line = lines[i];
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                current = line;
                result[current] = new List<string>();
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) && result.TryGetValue(current, out List<string>? bullets))
            {
                bullets.Add(line);
            }
        }

        return result;
    }

    private static List<string> Bucket(Dictionary<string, List<string>> sections, string heading) =>
        sections.TryGetValue(heading, out List<string>? bullets) ? bullets : new List<string>();

    private static List<int> MatchingFeatures(IReadOnlyList<ReleaseFeature> features, string bullet) =>
        Enumerable.Range(0, features.Count)
            .Where(index => features[index].ChangelogLeads.Any(lead => bullet.StartsWith(lead, StringComparison.Ordinal)))
            .ToList();

    private static bool ExactlyOneMatch(List<int> matches, string bullet, List<string> problems)
    {
        if (matches.Count == 0)
        {
            problems.Add("CHANGELOG 0.1.0 bullet maps to no shipped feature: '" + Head(bullet) + "'");
            return false;
        }

        if (matches.Count > 1)
        {
            problems.Add("CHANGELOG 0.1.0 bullet maps to more than one shipped feature: '" + Head(bullet) + "'");
            return false;
        }

        return true;
    }

    private static string OutOfTableOrder(ReleaseFeature feature) =>
        $"{feature.Id} ({feature.Name}): CHANGELOG bullet out of table order";

    private static string Head(string bullet) => bullet.Length <= 60 ? bullet : bullet.Substring(0, 60);

    private static string BoldLead(string line)
    {
        Match match = Regex.Match(line, @"^- (\*\*[^*]+\*\*)");
        return match.Success ? match.Groups[1].Value : line;
    }

    private static int NextLevel2(string[] lines, int after)
    {
        for (int i = after + 1; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("## ", StringComparison.Ordinal))
            {
                return i;
            }
        }

        return lines.Length;
    }

    private static string[] Lines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(line => line.TrimEnd(' ', '\t')).ToArray();

    private static void AssertNoProblems(List<string> problems)
    {
        List<string> distinct = problems.Distinct(StringComparer.Ordinal).ToList();
        Assert.True(distinct.Count == 0, string.Join("\n", distinct));
    }

    /// <summary>The parser gives the same problems on the LF sample and on the same sample with CRLF line endings.</summary>
    private static void AssertSameProblemsOnBothLineEndings(Func<string, List<string>> parse, string lfSample)
    {
        List<string> lf = parse(lfSample);
        List<string> crlf = parse(lfSample.Replace("\n", "\r\n", StringComparison.Ordinal));
        Assert.True(
            lf.SequenceEqual(crlf, StringComparer.Ordinal),
            "The parser gives different problems on CRLF text:\n" + string.Join("\n", crlf));
    }
}
