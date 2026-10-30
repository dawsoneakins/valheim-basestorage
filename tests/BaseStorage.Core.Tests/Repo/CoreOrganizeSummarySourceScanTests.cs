namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// S-E004-009 AC-3: the preview and summary composer sources name no game, Plugin, wall-clock or culture type, and keep every
/// user-facing word in the strings table. Sources are comment-stripped and CRLF-normalised.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreOrganizeSummarySourceScanTests
{
    private static readonly string[] Files =
    {
        "src/BaseStorage.Core/Ui/OrganizeSummaryFormatter.cs",
        "src/BaseStorage.Core/Ui/OrganizeRunReport.cs",
        "src/BaseStorage.Core/Ui/OrganizeSkipEntry.cs",
    };

    private static readonly string[] ForbiddenNames =
    {
        "UnityEngine", "BepInEx", "HarmonyLib", "Valheim", "ZDO", "ZNet", "BaseStorage.Plugin", "System.Linq", "System.IO",
        "System.Threading", "DateTime", "Stopwatch", "Random", "CultureInfo", "ToLower", "ToUpper", "ILogSink", "IClock",
    };

    private static readonly string[] UserFacingWords =
    {
        "stack", "chest", "skipped", "no room", "Organized", "Organize base", "Nothing", "in use", "claimed", "eligible", "unclear", "stopped", "Keep",
    };

    private static readonly string[] ExpectedStringKeys =
    {
        "OrganizePreviewLine", "OrganizePreviewHeader", "OrganizePreviewBodyNothing", "OrganizePreviewBodyRules", "OrganizePreviewBodyReasonFull", "OrganizePreviewBodyReasonCategory", "OrganizePreviewBodyMove", "OrganizePreviewBodyClaim",
        "OrganizePreviewBodyNoRoom", "OrganizePreviewBodySkipped", "OrganizePreviewBodySkippedReason", "OrganizeSkipReasonInUse",
        "OrganizeSkipReasonInUseUnknown", "OrganizeSkipReasonKeep", "OrganizeSkipReasonIneligible", "OrganizeSkipReasonInvalidContents",
        "OrganizeNamesJoin", "MessageOrganizeSummaryMoved", "MessageOrganizeSummaryMovedNone", "MessageOrganizeSummarySkipped",
        "MessageOrganizeSummaryNoRoom", "MessageOrganizeSummaryStopped", "MessageOrganizeSummaryStoppedNoReason", "MessageOrganizeSummaryJoin",
    };

    private static readonly Regex StringLiteral = new("\"(?:[^\"\\\\]|\\\\.)*\"", RegexOptions.CultureInvariant);

    // GW-U-993 (AC-3): the three files exist, and none names a game, Plugin, wall-clock or culture type.
    [Fact]
    public void TheComposerSourcesNameNoGameOrCultureType()
    {
        foreach (string file in Files)
        {
            string text = PluginSourceScan.StripComments(RepoRoot.ReadText(file));
            Assert.False(string.IsNullOrWhiteSpace(text), $"The scan read an empty {file}.");

            foreach (string name in ForbiddenNames)
            {
                Assert.DoesNotContain(name, text);
            }
        }
    }

    // GW-U-993 (AC-3): no string literal in the three files carries a user-facing word; wording lives in EnglishStrings only.
    [Fact]
    public void NoStringLiteralCarriesUserFacingWording()
    {
        var violations = Files
            .SelectMany(file => StringLiteral.Matches(PluginSourceScan.StripComments(RepoRoot.ReadText(file)))
                .Select(match => file + ": " + match.Value))
            .Where(literal => UserFacingWords.Any(word => literal.Substring(literal.IndexOf(": ", StringComparison.Ordinal) + 2).Contains(word, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(violations);
    }

    // GW-U-993 (AC-3): the formatter reads exactly the contract section 10 keys, so the strings-table coverage is not vacuous.
    [Fact]
    public void TheFormatterReadsExactlyTheContractKeys()
    {
        string text = PluginSourceScan.StripComments(RepoRoot.ReadText(Files[0]));

        string[] used = Regex.Matches(text, @"StringKeys\.(\w+)")
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedStringKeys.OrderBy(name => name, StringComparer.Ordinal).ToArray(), used);
        Assert.DoesNotContain("StringKeys.", PluginSourceScan.StripComments(RepoRoot.ReadText(Files[1])));
        Assert.DoesNotContain("StringKeys.", PluginSourceScan.StripComments(RepoRoot.ReadText(Files[2])));
    }

    // GW-U-993 (AC-3): every static field is readonly or const.
    [Fact]
    public void EveryStaticFieldIsReadonlyOrConst()
    {
        foreach (string file in Files)
        {
            string text = PluginSourceScan.StripComments(RepoRoot.ReadText(file));

            string[] mutableStatics = Regex.Matches(
                    text,
                    @"(?m)^\s*(?:(?:public|private|internal|protected)\s+)?static\s+(?!readonly\b|const\b|class\b|sealed\b)[\w<>\[\],?\s]+\s+\w+\s*(?:=[^;]*)?;")
                .Select(match => match.Value.Trim())
                .ToArray();

            Assert.Empty(mutableStatics);
        }
    }
}
