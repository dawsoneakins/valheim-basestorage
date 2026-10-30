namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Tests.Organize;
using BaseStorage.Core.Ui;

/// <summary>S-E004-009 AC-1: the Organize preview text (spec AC-4.7, AC-5.3; ui-strings contract 10.1 and 10.3).</summary>
public sealed class OrganizePreviewFormatterTests
{
    // U+0085, U+2028 and U+2029 are written as char casts: C# treats them as newlines inside source text.
    private static readonly char[] BreakCharacters = ['\r', '\n', (char)0x85, (char)0x2028, (char)0x2029];

    private static CategoryCatalog Catalog => OrganizeFixtures.Catalog();

    // GW-U-980 (AC-1): the spec AC-4.7 example, built through the planner, renders verbatim.
    [Fact]
    public void GoldenPlanRendersTheSpecExampleLine()
    {
        OrganizePlan plan = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.Golden());

        Assert.Equal(214, plan.Moves.Count);
        Assert.Equal(23, plan.Moves.Select(move => move.Destination).Distinct().Count());
        Assert.Equal(2, plan.ClaimedOverflow.Count);
        Assert.All(plan.ClaimedOverflow, layout => Assert.Equal("Metals & Ores", layout.CategoryName));
        Assert.Equal(4, plan.NoRoom.Count);
        Assert.Single(plan.Skipped);
        Assert.Equal(SkipReason.InUse, plan.Skipped[0].Kind);
        Assert.Equal("Erik", plan.Skipped[0].PlayerName);

        Assert.Equal(OrganizeSummaryFixtures.GoldenPreview, OrganizeSummaryFormatter.Preview(plan, Catalog));
        Assert.Equal(OrganizeSummaryFixtures.GoldenBody, OrganizeSummaryFormatter.PreviewBody(plan, Catalog));
    }

    // GW-U-981 (AC-1): every clause in its singular form, and the in-use group without a name precedes the Keep group.
    [Fact]
    public void SingularPlanUsesTheOneFormOfEveryClause()
    {
        OrganizePlan plan = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.Singular());

        Assert.Single(plan.Moves);
        Assert.Single(plan.ClaimedOverflow);
        Assert.Single(plan.NoRoom);
        Assert.Equal(2, plan.Skipped.Count);

        Assert.Equal(OrganizeSummaryFixtures.SingularBody, OrganizeSummaryFormatter.PreviewBody(plan, Catalog));
    }

    // GW-U-981 (AC-1): zero moves gives the nothing sentence; zero claims, no-room and skips omit their sentences.
    [Fact]
    public void EmptyPlanRendersNothingToMove()
    {
        OrganizePlan plan = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.Empty());

        Assert.Equal(OrganizeSummaryFixtures.EmptyPreview, OrganizeSummaryFormatter.Preview(plan, Catalog));
        Assert.Equal("Nothing to move.", OrganizeSummaryFormatter.PreviewBody(plan, Catalog));
    }

    // GW-U-982 (AC-1): skipped chests are grouped by reason, in use first; names are distinct, in first-seen order.
    [Fact]
    public void SkippedChestsAreGroupedByReasonWithDistinctNames()
    {
        OrganizePlan plan = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.Groups());

        Assert.Empty(plan.Moves);
        Assert.Equal(7, plan.Skipped.Count);

        Assert.Equal(OrganizeSummaryFixtures.GroupsBody, OrganizeSummaryFormatter.PreviewBody(plan, Catalog));
    }

    // GW-U-983 (AC-1): one claimed sentence per category, in catalog order even when the claim order differs.
    [Fact]
    public void ClaimedSentencesFollowCatalogOrder()
    {
        OrganizePlan plan = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.TwoCategories());

        Assert.Equal(2, plan.ClaimedOverflow.Count);
        Assert.Equal("Wood", plan.ClaimedOverflow[0].CategoryName);
        Assert.Equal("Metals & Ores", plan.ClaimedOverflow[1].CategoryName);

        Assert.Equal(OrganizeSummaryFixtures.TwoCategoriesBody, OrganizeSummaryFormatter.PreviewBody(plan, Catalog));
    }

    // GW-U-983 (AC-1): a category the catalog no longer lists sorts after the known ones.
    [Fact]
    public void UnknownCategoriesSortAfterKnownOnes()
    {
        OrganizePlan plan = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.TwoCategories());
        CategoryCatalog woodOnly = CategoryFile.Parse("Wood: Wood\n").Catalog;

        Assert.Equal(
            "2 stacks to move across 2 chests. 1 chest will be claimed for Wood. 1 chest will be claimed for Metals & Ores.",
            OrganizeSummaryFormatter.PreviewBody(plan, woodOnly));
    }

    // GW-U-984 (AC-1, AC-3): player names are single-lined, and no result carries a line break or outer whitespace.
    [Fact]
    public void PlayerNamesAreSingleLinedAndResultsAreOneTrimmedLine()
    {
        OrganizePlan broken = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.BrokenName());

        Assert.Equal("Nothing to move. 1 chest skipped (in use by Erik the Red).", OrganizeSummaryFormatter.PreviewBody(broken, Catalog));

        foreach (OrganizeFixtures.PlanInput input in new[]
                 {
                     OrganizeSummaryFixtures.Golden(), OrganizeSummaryFixtures.Singular(), OrganizeSummaryFixtures.Groups(),
                     OrganizeSummaryFixtures.TwoCategories(), OrganizeSummaryFixtures.BrokenName(), OrganizeSummaryFixtures.Empty(),
                 })
        {
            OrganizePlan plan = OrganizeSummaryFixtures.Plan(input);
            foreach (string text in new[] { OrganizeSummaryFormatter.Preview(plan, Catalog), OrganizeSummaryFormatter.PreviewBody(plan, Catalog) })
            {
                Assert.Equal(-1, text.IndexOfAny(BreakCharacters));
                Assert.Equal(text.Trim(), text);
                Assert.NotEqual(string.Empty, text);
            }
        }
    }

    // GW-U-985 (AC-3): the same text under de-DE, tr-TR and en-US (contract U2).
    [Theory]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void PreviewIsInvariantAcrossCultures(string cultureName)
    {
        CultureInfo savedCurrent = CultureInfo.CurrentCulture;
        CultureInfo savedUi = CultureInfo.CurrentUICulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            OrganizePlan plan = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.Golden());
            Assert.Equal(OrganizeSummaryFixtures.GoldenPreview, OrganizeSummaryFormatter.Preview(plan, Catalog));
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCurrent;
            CultureInfo.CurrentUICulture = savedUi;
        }
    }

    // GW-U-986 (AC-3): null arguments throw with the parameter name (R11).
    [Fact]
    public void NullArgumentsThrow()
    {
        OrganizePlan plan = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.Empty());

        Assert.Equal("plan", Assert.Throws<ArgumentNullException>(() => OrganizeSummaryFormatter.Preview(null!, Catalog)).ParamName);
        Assert.Equal("catalog", Assert.Throws<ArgumentNullException>(() => OrganizeSummaryFormatter.Preview(plan, null!)).ParamName);
        Assert.Equal("plan", Assert.Throws<ArgumentNullException>(() => OrganizeSummaryFormatter.PreviewBody(null!, Catalog)).ParamName);
        Assert.Equal("catalog", Assert.Throws<ArgumentNullException>(() => OrganizeSummaryFormatter.PreviewBody(plan, null!)).ParamName);
    }
}
