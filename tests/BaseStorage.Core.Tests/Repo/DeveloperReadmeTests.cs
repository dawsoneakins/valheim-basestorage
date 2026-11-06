namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-7.4 (spec S-E007-004, ADR-GWIN-078): the root README.md is the developer README. It exists, has the required
/// sections in order, quotes the three gate commands verbatim from spec section 4, explains the three ValheimDir
/// sources in the order Directory.Build.props implements them, documents the opt-in DeployToGame and
/// PackageThunderstore properties, the patch-target contract tests, the BepInEx log, and points at the other docs.
/// The file is read as text; nothing is built or run.
/// </summary>
[Trait("Category", "Repo")]
public sealed class DeveloperReadmeTests
{
    private const string ReadmeFile = "README.md";

    private static readonly string[] HeadingsInOrder =
    {
        "## Repository layout",
        "## Prerequisites",
        "## Setting VALHEIM_DIR",
        "## Build, test and format",
        "## Patch-target contract tests (AC-1.4)",
        "## Deploy to the game",
        "## Thunderstore package",
        "## Reading the BepInEx log",
        "## More",
    };

    private static readonly string[] GateCommands =
    {
        "dotnet build BaseStorage.sln -c Release --nologo -warnaserror",
        "dotnet test  BaseStorage.sln -c Release --nologo",
        "dotnet format BaseStorage.sln --verify-no-changes",
    };

    private static readonly string[] ZipEntries =
    {
        "manifest.json", "icon.png", "README.md", "CHANGELOG.md", "plugins/Dons-QOL.dll", "plugins/Dons-QOL.Core.dll",
    };

    // GW-U-970 (AC-7.4): the file exists at the repo root, is titled BaseStorage, links the spec, and uses LF line endings.
    [Fact]
    public void TheReadmeExistsAtTheRootWithTitleSpecLinkAndLfEndings()
    {
        Assert.True(RepoRoot.Exists(ReadmeFile), "README.md is missing from the repo root.");
        string raw = File.ReadAllText(RepoRoot.Combine(ReadmeFile));

        Assert.DoesNotContain("\r", raw, StringComparison.Ordinal);
        Assert.StartsWith("# Don's QOL\n", raw, StringComparison.Ordinal);
        Assert.EndsWith("\n", raw, StringComparison.Ordinal);
        Assert.Contains("(docs/SPEC.md)", raw, StringComparison.Ordinal);
    }

    // GW-U-971 (AC-7.4): every required section heading is present, exactly once, in this order.
    [Fact]
    public void TheRequiredHeadingsArePresentInOrder()
    {
        string[] headings = Text().Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .ToArray();

        int cursor = -1;
        foreach (string heading in HeadingsInOrder)
        {
            Assert.Equal(1, headings.Count(h => h == heading));
            int index = Array.IndexOf(headings, heading);
            Assert.True(index > cursor, $"Heading '{heading}' is out of order in README.md.");
            cursor = index;
        }
    }

    // GW-U-972 (AC-7.4): the three gate command lines appear verbatim, as whole lines, inside the build section.
    [Fact]
    public void TheThreeGateCommandsAppearVerbatimAsWholeLines()
    {
        string section = Section("## Build, test and format");
        string[] lines = section.Split('\n').Select(l => l.TrimEnd()).ToArray();

        foreach (string command in GateCommands)
        {
            Assert.Contains(command, lines);
        }

        Assert.DoesNotContain("DeployToGame", section, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PackageThunderstore", section, StringComparison.OrdinalIgnoreCase);
    }

    // GW-U-973 (AC-7.4): the VALHEIM_DIR section lists the user props first, the environment variable second and the
    // Directory.Build.props default third, and the default it quotes is the one Directory.Build.props declares.
    [Fact]
    public void TheValheimDirSectionListsTheThreeSourcesInPropsOrder()
    {
        string section = Section("## Setting VALHEIM_DIR");
        string[] defaults = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.DirectoryBuildProps), "ValheimDir")
            .Where(e => RepoRoot.Attr(e, "Condition") == "'$(ValheimDir)' == ''")
            .Select(e => e.Value.Trim())
            .ToArray();
        string propsDefault = Assert.Single(defaults);

        int userProps = section.IndexOf("Directory.Build.user.props", StringComparison.Ordinal);
        int envVar = section.IndexOf("`VALHEIM_DIR` environment variable", StringComparison.Ordinal);
        int fallback = section.IndexOf(propsDefault, StringComparison.Ordinal);

        Assert.True(userProps >= 0, "The VALHEIM_DIR section does not mention Directory.Build.user.props.");
        Assert.True(envVar >= 0, "The VALHEIM_DIR section does not mention the VALHEIM_DIR environment variable.");
        Assert.True(fallback >= 0, $"The VALHEIM_DIR section does not quote the Directory.Build.props default '{propsDefault}'.");
        Assert.True(userProps < envVar && envVar < fallback, "The three ValheimDir sources are not in Directory.Build.props precedence order.");
        Assert.Contains("<ValheimDir>", section, StringComparison.Ordinal);
    }

    // GW-U-974 (AC-7.4): the prerequisites name the .NET 10 SDK, the game and BepInExPack_Valheim.
    [Fact]
    public void ThePrerequisitesNameTheSdkTheGameAndBepInExPack()
    {
        string section = Section("## Prerequisites");

        Assert.Contains(".NET 10 SDK", section, StringComparison.Ordinal);
        Assert.Contains("Valheim installed", section, StringComparison.Ordinal);
        Assert.Contains("BepInExPack_Valheim", section, StringComparison.Ordinal);
        Assert.Contains("BS1001", section, StringComparison.Ordinal);
    }

    // GW-U-975 (AC-7.4, section 4): the deploy section has the opt-in command, the destination, the BepInEx guard and
    // the rule that the gates never pass the property.
    [Fact]
    public void TheDeploySectionDocumentsTheOptInTargetAndItsGuard()
    {
        string section = Section("## Deploy to the game");

        Assert.Contains("dotnet build BaseStorage.sln -c Release --nologo -p:DeployToGame=true", section, StringComparison.Ordinal);
        Assert.Contains(@"BepInEx\plugins\Dons-QOL\", section, StringComparison.Ordinal);
        Assert.Contains("already exists", section, StringComparison.Ordinal);
        Assert.Contains("BS1002", section, StringComparison.Ordinal);
        Assert.Contains("gates never pass", section, StringComparison.Ordinal);
    }

    // GW-U-976 (AC-7.1, AC-7.4): the package section has the opt-in command, the zip path and all six entries.
    [Fact]
    public void ThePackageSectionDocumentsTheCommandTheZipAndItsSixEntries()
    {
        string section = Section("## Thunderstore package");

        Assert.Contains("dotnet build BaseStorage.sln -c Release --nologo -p:PackageThunderstore=true", section, StringComparison.Ordinal);
        Assert.Contains("dist/Dons-QOL-<version>.zip", section, StringComparison.Ordinal);
        Assert.Contains("BS1004", section, StringComparison.Ordinal);
        foreach (string entry in ZipEntries)
        {
            Assert.Contains("`" + entry + "`", section, StringComparison.Ordinal);
        }
    }

    // GW-U-977 (AC-1.4, AC-7.4): the contract-test section names the project, the command that runs only it, the skip
    // codes, the constants file to edit after a game update, and the ADR step.
    [Fact]
    public void TheContractTestSectionCoversRunningAndReRunningAfterAGameUpdate()
    {
        string section = Section("## Patch-target contract tests (AC-1.4)");

        Assert.Contains("dotnet test tests/BaseStorage.PatchTargets.Tests -c Release --nologo", section, StringComparison.Ordinal);
        Assert.Contains("BaseStorage.PatchTargets.Tests", section, StringComparison.Ordinal);
        Assert.Contains("BST2002", section, StringComparison.Ordinal);
        Assert.Contains("BST2004", section, StringComparison.Ordinal);
        Assert.Contains("After a game update", section, StringComparison.Ordinal);
        Assert.Contains("src/Shared/PatchTargetNames.cs", section, StringComparison.Ordinal);
        Assert.Contains("docs/adr/", section, StringComparison.Ordinal);
        Assert.Contains("names the missing member", section, StringComparison.Ordinal);
    }

    // GW-U-978 (AC-1.6, AC-7.4): the log section names LogOutput.log, the Info tag, the load line, and both Debug switches.
    [Fact]
    public void TheLogSectionNamesTheFileTheLoadLineAndBothDebugSwitches()
    {
        string section = Section("## Reading the BepInEx log");

        Assert.Contains("LogOutput.log", section, StringComparison.Ordinal);
        Assert.Contains("[Info   :BaseStorage]", section, StringComparison.Ordinal);
        Assert.Contains("Dons-QOL <version> loaded", section, StringComparison.Ordinal);
        Assert.Contains("DebugLogging", section, StringComparison.Ordinal);
        Assert.Contains("LogLevels", section, StringComparison.Ordinal);
        Assert.Contains("Debug logging", section, StringComparison.Ordinal);
    }

    // GW-U-979 (AC-7.4): the pointers at the end link PLAYTEST.md, the ADR folder, the changelog, the spec and the
    // player README, and every relative link in the file resolves to a repo path.
    [Fact]
    public void ThePointersLinkTheOtherDocsAndEveryLinkResolves()
    {
        string text = Text();
        string more = Section("## More");

        foreach (string target in new[] { "docs/PLAYTEST.md", "docs/adr/", "CHANGELOG.md", "docs/SPEC.md", "package/README.md" })
        {
            Assert.Contains("](" + target + ")", more, StringComparison.Ordinal);
        }

        foreach (Match link in Regex.Matches(text, @"\]\(([^)]+)\)"))
        {
            string target = link.Groups[1].Value;
            string path = RepoRoot.Combine(target.TrimEnd('/'));
            Assert.True(File.Exists(path) || Directory.Exists(path), $"README.md links '{target}', which does not exist in the repo.");
        }
    }

    private static string Text() => RepoRoot.ReadText(ReadmeFile);

    private static string Section(string heading)
    {
        string text = Text();
        int start = text.IndexOf("\n" + heading + "\n", StringComparison.Ordinal);
        Assert.True(start >= 0, $"README.md has no '{heading}' section.");
        start += 1;

        int next = text.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? text.Substring(start) : text.Substring(start, next - start);
    }
}
