namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-4 (contracts/plugin-boot.md section 3, D1-D2, ADR-GWIN-012/013): docs/PLAYTEST.md gains a load-check section with the
/// exact expected log line and steps. The version is never hard-coded as the expected value.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestLoadCheckTests
{
    private const string Doc = "docs/PLAYTEST.md";
    private const string Title = "# BaseStorage playtest checklist";
    private const string LoadCheckHeading = "## Load check";

    // GW-U-089
    [Fact]
    public void LoadCheckHeadingComesImmediatelyAfterSetupAndBeforeSolo()
    {
        string text = RepoRoot.ReadText(Doc);

        Assert.Equal(Title, text.Split('\n').Select(line => line.TrimEnd()).First(line => line.Trim().Length > 0));

        string[] headings = Headings(text);
        int setup = Array.IndexOf(headings, "## Setup");
        int loadCheck = Array.IndexOf(headings, LoadCheckHeading);
        int solo = Array.IndexOf(headings, "## Solo");
        int host = Array.IndexOf(headings, "## Host plus one friend");

        Assert.True(setup >= 0 && loadCheck >= 0 && solo >= 0 && host >= 0, "A required heading is missing from docs/PLAYTEST.md.");
        Assert.True(setup < loadCheck && loadCheck < solo && solo < host, "The headings of docs/PLAYTEST.md are out of order.");
        Assert.Equal(setup + 1, loadCheck);
    }

    // GW-U-090
    [Fact]
    public void LoadCheckNamesTheExactLineAndTheVersionPropsSourceAndHasNumberedSteps()
    {
        string section = LoadCheckSection(RepoRoot.ReadText(Doc));

        Assert.Contains("`Dons-QOL <version> loaded`", section, StringComparison.Ordinal);
        Assert.Contains("Version.props", section, StringComparison.Ordinal);
        Assert.True(Regex.Matches(section, @"^\d+\. ", RegexOptions.Multiline).Count >= 5, "The load check needs at least 5 numbered steps.");
    }

    // GW-U-090
    [Fact]
    public void LoadCheckStepsCoverBuildDeployLaunchAndTheLogFile()
    {
        string section = LoadCheckSection(RepoRoot.ReadText(Doc));

        string[] fragments =
        {
            "dotnet build BaseStorage.sln -c Release -p:DeployToGame=true",
            "Dons-QOL.dll",
            "Dons-QOL.Core.dll",
            @"BepInEx\plugins\Dons-QOL\",
            "main menu",
            "LogOutput.log",
        };
        foreach (string fragment in fragments)
        {
            Assert.Contains(fragment, section, StringComparison.Ordinal);
        }
    }

    // GW-U-090
    [Fact]
    public void LoadCheckStatesTheExpectedResultsAndWhatToRecordOnFailure()
    {
        string section = LoadCheckSection(RepoRoot.ReadText(Doc));

        Assert.Contains("exactly one", section, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("failed to apply", section, StringComparison.Ordinal);
        Assert.Contains("failed to load", section, StringComparison.Ordinal);
        Assert.Contains("patch name", section, StringComparison.OrdinalIgnoreCase);
    }

    // GW-U-091
    [Fact]
    public void LoadCheckNeverHardCodesTheCurrentVersionOutsideAnExampleLine()
    {
        string section = LoadCheckSection(RepoRoot.ReadText(Doc));
        string version = RepoRoot.ValuesOf(ContractPaths.VersionProps, "Version").Single();
        string versionPattern = @"(?<![\d.])" + Regex.Escape(version) + @"(?![\d.])";

        var problems = new List<string>();
        foreach (string line in section.Split('\n'))
        {
            bool isExample = line.Contains("example", StringComparison.OrdinalIgnoreCase);
            if (Regex.IsMatch(line, @"BaseStorage \d+\.\d+\.\d+ loaded") && !isExample)
            {
                problems.Add("A concrete 'Dons-QOL x.y.z loaded' line is not marked as an example: " + line.Trim());
            }

            if (Regex.IsMatch(line, versionPattern) && !isExample)
            {
                problems.Add("The current version appears outside an example line: " + line.Trim());
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    // GW-U-117 (AC-1, contract 6.4): the load check tells the tester where the settings file is and what to look for.
    [Fact]
    public void LoadCheckNamesTheSettingsFileItsFiveSectionsItsEighteenKeysAndTheThreeHotkeys()
    {
        string section = LoadCheckSection(RepoRoot.ReadText(Doc));

        Assert.Contains("dawso.valheim.basestorage.cfg", section, StringComparison.Ordinal);
        Assert.Contains(@"BepInEx\config\", section, StringComparison.Ordinal);
        foreach (string heading in new[] { "[General]", "[Organize]", "[Network]", "[Keys]", "[Debug]" })
        {
            Assert.Contains(heading, section, StringComparison.Ordinal);
        }

        Assert.True(Regex.IsMatch(section, @"\b19\b"), "The load check does not state the number of settings (19).");
        foreach (string hotkey in new[] { "CycleCategoryKey", "OrganizeKey", "DepositKey" })
        {
            Assert.Contains(hotkey, section, StringComparison.Ordinal);
        }
    }

    private static string[] Headings(string text) =>
        text.Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .ToArray();

    // The section from its heading up to, not including, the next "## " heading.
    private static string LoadCheckSection(string text)
    {
        string[] lines = text.Split('\n');
        int start = Array.FindIndex(lines, line => line.TrimEnd() == LoadCheckHeading);
        Assert.True(start >= 0, "docs/PLAYTEST.md has no '## Load check' section.");

        int end = lines.Length;
        for (int i = start + 1; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("## ", StringComparison.Ordinal))
            {
                end = i;
                break;
            }
        }

        return string.Join("\n", lines[start..end]);
    }
}
