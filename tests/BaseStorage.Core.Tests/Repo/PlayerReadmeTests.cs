namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.Settings;

/// <summary>
/// GW-U-2000..GW-U-2010 (S-E007-002, AC-1..AC-8; ADR-GWIN-150, ADR-GWIN-151): package/README.md documents every config key,
/// its Configuration table row matches SettingsCatalog, each player-facing section has its required content, and no heading
/// is empty. Red until the implement phase fills the skeleton sections.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlayerReadmeTests
{
    private const string Readme = ThunderstorePackage.Readme;
    private const string ConfigurationTitle = "Configuration";
    private const string CraftFromBaseParagraphStart = "`CraftFromBase` (under `[General]`";
    private const string CraftFromBaseRowStart = "| `CraftFromBase` |";
    private const string TextFenceOpen = "```text";

    private static readonly Regex HeadingLine = new(@"^#{1,6} ", RegexOptions.CultureInvariant);

    // GW-U-2000 (AC-7.1): every SettingsCatalog key appears backticked in package/README.md. All missing keys are reported at once.
    [Fact]
    public void EveryCatalogKeyIsDocumentedInTheReadme()
    {
        string[] missing = MissingKeys(ReadReadme());

        Assert.True(missing.Length == 0, $"package/README.md is missing config keys: {string.Join(", ", missing)}");
    }

    // GW-U-2001 (AC-7.1): the public const string fields of SettingKeys and the catalog keys form the same non-empty set.
    [Fact]
    public void SettingKeysAndCatalogNameTheSameNonEmptyKeySet()
    {
        HashSet<string> constants = typeof(SettingKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetValue(null)!)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> catalog = SettingsCatalog.All
            .Select(definition => definition.Key)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(constants);
        Assert.NotEmpty(catalog);
        Assert.True(
            constants.SetEquals(catalog),
            $"SettingKeys constants and SettingsCatalog keys differ. Constants only: {Describe(constants, catalog)}. Catalog only: {Describe(catalog, constants)}.");
    }

    // GW-U-2002 (AC-7.2): CRLF is normalised. A CRLF copy of the README gives the same key check as the LF file.
    [Fact]
    public void CrlfCheckoutsNormaliseToTheSameKeyCheck()
    {
        string readme = ReadReadme();
        Assert.DoesNotContain("\r", readme);

        string firstKey = SettingsCatalog.All[0].Key;
        string syntheticLine = RepoRoot.Normalise("Key line: `" + firstKey + "`\r\n");
        Assert.DoesNotContain("\r", syntheticLine);
        Assert.Contains("`" + firstKey + "`", syntheticLine);

        string crlfCopy = RepoRoot.Normalise(readme.Replace("\n", "\r\n"));
        Assert.Equal(readme, crlfCopy);

        string[] missing = MissingKeys(crlfCopy);
        Assert.True(missing.Length == 0, $"package/README.md is missing config keys: {string.Join(", ", missing)}");
    }

    // GW-U-2003 (AC-7.3, NFR): the Configuration table has one row per catalog entry with its default and host-sync flag,
    // and the table comes after the CraftFromBase paragraph.
    [Fact]
    public void ConfigurationTableMatchesCatalogDefaultsAndHostSync()
    {
        string configuration = Section(ReadReadme(), ConfigurationTitle);
        string[] lines = configuration.Split('\n');
        var missing = new List<string>();

        foreach (SettingDefinition definition in SettingsCatalog.All)
        {
            string defaultText = string.IsNullOrEmpty(definition.DefaultText) ? "unbound" : "`" + definition.DefaultText + "`";
            string hostSynced = definition.HostSynced ? "yes" : "no";
            string prefix = "| `" + definition.Key + "` | `[" + definition.Section + "]` | " + defaultText + " | " + hostSynced + " | ";
            if (!lines.Any(line => line.StartsWith(prefix, StringComparison.Ordinal)))
            {
                missing.Add(definition.Key);
            }
        }

        Assert.True(
            missing.Count == 0,
            $"package/README.md Configuration table has no row matching the catalog for: {string.Join(", ", missing)}");

        int paragraph = configuration.IndexOf(CraftFromBaseParagraphStart, StringComparison.Ordinal);
        int firstRow = configuration.IndexOf(CraftFromBaseRowStart, StringComparison.Ordinal);
        Assert.True(paragraph >= 0, "The CraftFromBase paragraph must be inside ## Configuration.");
        Assert.True(firstRow > paragraph, "The Configuration table must come after the CraftFromBase paragraph.");
    }

    // GW-U-2004 (AC-1): What it does is written and names crafting, building, chest categories and the station buttons.
    [Fact]
    public void WhatItDoesNamesCraftingBuildingChestCategoriesAndStationButtons()
    {
        string section = Section(ReadReadme(), "What it does");
        Assert.False(string.IsNullOrWhiteSpace(section), "package/README.md ## What it does must have body text.");

        string[] terms = ["craft", "build", "chest categor", "Organize base", "Put away inventory"];
        string[] missing = terms.Where(term => !section.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray();

        Assert.True(missing.Length == 0, $"package/README.md ## What it does must name: {string.Join(", ", missing)}");
    }

    // GW-U-2005 (AC-2): Installation has the Host and Friends lead-ins in that order, the BepInEx steps and the same-version note.
    [Fact]
    public void InstallationCoversHostFriendsAndSameVersion()
    {
        string section = Section(ReadReadme(), "Installation");
        int host = section.IndexOf("**Host**", StringComparison.Ordinal);
        int friends = section.IndexOf("**Friends**", StringComparison.Ordinal);
        Assert.True(host >= 0, "package/README.md ## Installation must have a **Host** lead-in.");
        Assert.True(friends > host, "package/README.md ## Installation must have a **Friends** lead-in after **Host**.");

        string[] terms = ["BepInExPack_Valheim", "BepInEx/plugins", "same BaseStorage version", "`RequireSameVersion`"];
        string[] missing = terms.Where(term => !section.Contains(term, StringComparison.Ordinal)).ToArray();

        Assert.True(missing.Length == 0, $"package/README.md ## Installation must contain: {string.Join(", ", missing)}");
    }

    // GW-U-2006 (AC-3): Categories file names the file, the format and the reserved names, and its first text example parses
    // with CategoryFile.Parse to zero issues and at least one category.
    [Fact]
    public void CategoriesFileSectionDocumentsTheFormatAndItsExampleParses()
    {
        string section = Section(ReadReadme(), "Categories file");
        string[] terms = [DefaultCategoryFile.FileName, "#", "Auto", "Keep"];
        string[] missing = terms.Where(term => !section.Contains(term, StringComparison.Ordinal)).ToArray();
        Assert.True(missing.Length == 0, $"package/README.md ## Categories file must contain: {string.Join(", ", missing)}");

        int fence = section.IndexOf(TextFenceOpen, StringComparison.Ordinal);
        Assert.True(fence >= 0, "package/README.md ## Categories file must have a ```text example.");
        Match example = Regex.Match(
            section.Substring(fence),
            @"^```text[ \t]*\n(.*?)^```[ \t]*$",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(example.Success, "The ```text example in ## Categories file must be closed by a ``` line.");

        CategoryParseResult result = CategoryFile.Parse(example.Groups[1].Value);
        Assert.True(
            result.Issues.Count == 0,
            $"The ## Categories file example has parse issues: {string.Join("; ", result.Issues.Select(issue => issue.LineNumber + ": " + issue.LineText))}");
        Assert.NotEmpty(result.Catalog.Categories);
    }

    // GW-U-2007 (AC-4): Known limits names every section 8 limit, case-insensitively.
    [Fact]
    public void KnownLimitsNamesEverySection8Limit()
    {
        string section = Section(ReadReadme(), "Known limits");
        string[] terms =
        [
            "console", "crossplay", "dedicated server", "peer-hosted", "collect", "cart", "ship",
            "sorting table", "inventory mods",
        ];
        string[] missing = terms.Where(term => !section.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray();

        Assert.True(missing.Length == 0, $"package/README.md ## Known limits must name: {string.Join(", ", missing)}");
    }

    // GW-U-2008 (AC-5): Known limits says there are no gamepad bindings and mentions the gamepad cursor.
    [Fact]
    public void KnownLimitsStatesTheGamepadLimits()
    {
        string section = Section(ReadReadme(), "Known limits");
        string[] terms = ["gamepad control", "hotkeys stay keyboard only"];
        string[] missing = terms.Where(term => !section.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray();

        Assert.True(missing.Length == 0, $"package/README.md ## Known limits must contain: {string.Join(", ", missing)}");
    }

    // GW-U-2009 (AC-6): Uninstall has body text and names the plugins folder.
    [Fact]
    public void UninstallNamesThePluginsFolder()
    {
        string section = Section(ReadReadme(), "Uninstall");
        Assert.False(string.IsNullOrWhiteSpace(section), "package/README.md ## Uninstall must have body text.");
        Assert.Contains("BepInEx/plugins", section, StringComparison.Ordinal);
    }

    // GW-U-2010 (AC-8): outside fenced code, every heading (the H1 included) has body text before the next heading or the end
    // of the file, and each skeleton section occurs exactly once.
    [Fact]
    public void EveryHeadingHasBodyTextAndSkeletonSectionsOccurOnce()
    {
        string[] lines = ReadReadme().Split('\n');
        var headings = new List<int>();
        bool inFence = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (!inFence && HeadingLine.IsMatch(lines[i]))
            {
                headings.Add(i);
            }
        }

        var empty = new List<string>();
        for (int h = 0; h < headings.Count; h++)
        {
            int end = h + 1 < headings.Count ? headings[h + 1] : lines.Length;
            bool hasBody = false;
            for (int k = headings[h] + 1; k < end; k++)
            {
                if (lines[k].Trim().Length > 0)
                {
                    hasBody = true;
                    break;
                }
            }

            if (!hasBody)
            {
                empty.Add(lines[headings[h]].Trim());
            }
        }

        Assert.True(empty.Count == 0, $"package/README.md has headings with no body text: {string.Join(", ", empty)}");

        string[] skeleton = ["## What it does", "## Installation", "## Configuration", "## Categories file", "## Known limits", "## Uninstall"];
        foreach (string heading in skeleton)
        {
            int count = lines.Count(line => line.TrimEnd() == heading);
            Assert.True(count == 1, $"package/README.md must contain '{heading}' exactly once, found {count}.");
        }
    }

    private static string ReadReadme() => RepoRoot.ReadText(Readme);

    private static string[] MissingKeys(string readme) =>
        SettingsCatalog.All
            .Select(definition => definition.Key)
            .Where(key => !readme.Contains("`" + key + "`", StringComparison.Ordinal))
            .ToArray();

    private static string Describe(IEnumerable<string> values, IEnumerable<string> other)
    {
        string[] only = values.Except(other, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        return only.Length == 0 ? "none" : string.Join(", ", only);
    }

    /// <summary>
    /// The body of the single "## heading" section: the text after its heading line, up to the next "## " heading or the end.
    /// </summary>
    private static string Section(string readme, string heading)
    {
        MatchCollection matches = Regex.Matches(readme, "^## " + Regex.Escape(heading) + @"[ \t]*$", RegexOptions.Multiline);
        Assert.True(matches.Count == 1, $"package/README.md must have exactly one '## {heading}' heading, found {matches.Count}.");

        int bodyStart = matches[0].Index + matches[0].Length;
        Match next = Regex.Match(readme.Substring(bodyStart), "^## ", RegexOptions.Multiline);
        int bodyEnd = next.Success ? bodyStart + next.Index : readme.Length;
        return readme.Substring(bodyStart, bodyEnd - bodyStart);
    }
}
