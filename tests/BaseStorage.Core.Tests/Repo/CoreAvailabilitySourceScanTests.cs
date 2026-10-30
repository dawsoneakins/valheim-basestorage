namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-4 (contract 9, ADR-GWIN-026 and ADR-GWIN-027): the Availability sources are pure, game-free and do not duplicate eligibility.
/// The scans strip comments and normalise CRLF, and assert the file names they read. They are text checks, not compiler checks.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreAvailabilitySourceScanTests
{
    private const string AvailabilityFolder = "src/BaseStorage.Core/Availability";
    private const string BaseAvailabilityFile = AvailabilityFolder + "/BaseAvailability.cs";

    private const string PlannerFile = AvailabilityFolder + "/ConsumptionPlanner.cs";

    private static readonly string[] ExpectedFiles =
    {
        "AvailabilityResult.cs",
        "BaseAvailability.cs",
        "BaseSnapshot.cs",
        "ConsumptionPlan.cs",
        "ConsumptionPlanner.cs",
        "ConsumptionTake.cs",
        "CraftRequirement.cs",
        "QualityMatch.cs",
        "RequirementAvailability.cs",
    };

    private static readonly string[] PlannerForbiddenTokens =
    {
        "ContainerEligibility",
        "ContainerPurpose",
        "QualityMatch.Exact",
        "QualityMatch.AtLeast",
        "snapshot.Containers",
        "Math.Sqrt",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "DateTime",
        "Stopwatch",
        "Environment.",
        "Random",
        "ILogSink",
        "IClock",
        "IContainerLayer",
        "ISettingsSource",
        "InternalsVisibleTo",
        "UnityEngine",
        "BepInEx",
        "HarmonyLib",
        "Valheim",
        "BaseStorage.Plugin",
    };

    private static readonly string[] EligibilityTokens =
    {
        "Tombstone",
        "Vehicle",
        "AccessDenied",
        "PlayerBuilt",
        "IsKeep",
        "KeepChestsCraftable",
    };

    // GW-U-435 (AC-4, ADR-GWIN-027; amended by ADR-GWIN-029 for S-E003-002): the folder holds exactly the nine contract files, and no file names a
    // clock, RNG, log, settings, Unity, BepInEx, Harmony, Valheim or Plugin type, or re-implements an eligibility rule.
    [Fact]
    public void FolderHoldsExactlyTheNineContractFilesAndNoForbiddenOrDuplicatedRules()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(AvailabilityFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, names);

        foreach (string name in names)
        {
            string code = Strip(RepoRoot.ReadText(AvailabilityFolder + "/" + name));
            Assert.False(string.IsNullOrWhiteSpace(code), name + " was read as empty.");

            foreach (string token in ForbiddenTokens)
            {
                Assert.False(code.Contains(token, StringComparison.Ordinal), name + " must not contain " + token + ".");
            }

            foreach (string token in EligibilityTokens)
            {
                Assert.False(code.Contains(token, StringComparison.Ordinal), name + " must not contain " + token + " (eligibility is reused, not copied).");
            }
        }
    }

    // GW-U-435 (AC-4, ADR-GWIN-026 C4): BaseAvailability calls the eligibility rule once, with the craft purpose, and owns the in-use filter.
    // InUseByOther is read only in BaseAvailability.cs, so the Membership sources keep their GW-U-227 guarantee.
    [Fact]
    public void BaseAvailabilityReusesEligibilityOnceAndOwnsTheInUseFilter()
    {
        string code = Strip(RepoRoot.ReadText(BaseAvailabilityFile));

        Assert.Equal(1, CountOf(code, "ContainerEligibility.Evaluate("));
        Assert.Equal(1, CountOf(code, "ContainerPurpose.CraftAvailability"));
        Assert.True(code.Contains("InUseByOther", StringComparison.Ordinal), "BaseAvailability.cs must read InUseByOther (rule C4).");

        foreach (string name in ExpectedFiles.Where(name => name != "BaseAvailability.cs"))
        {
            string other = Strip(RepoRoot.ReadText(AvailabilityFolder + "/" + name));
            Assert.False(other.Contains("InUseByOther", StringComparison.Ordinal), name + " must not read InUseByOther.");
        }
    }

    // GW-U-460 (AC-15, ADR-GWIN-028 steps 1 and 4): the planner calls BaseAvailability.Compute once and reuses Matches, and copies no eligibility
    // or quality rule. BaseAvailability.Matches is internal, so the planner can call it and nothing outside Core can.
    [Fact]
    public void PlannerReusesComputeAndMatchesAndCopiesNoRule()
    {
        string planner = Strip(RepoRoot.ReadText(PlannerFile));

        Assert.Equal(1, CountOf(planner, "BaseAvailability.Compute("));
        Assert.True(CountOf(planner, "BaseAvailability.Matches(") >= 1, "ConsumptionPlanner.cs must call BaseAvailability.Matches.");
        Assert.True(planner.Contains("CountedContainers", StringComparison.Ordinal), "ConsumptionPlanner.cs must read CountedContainers.");
        foreach (string token in PlannerForbiddenTokens)
        {
            Assert.False(planner.Contains(token, StringComparison.Ordinal), "ConsumptionPlanner.cs must not contain " + token + ".");
        }

        string availability = Strip(RepoRoot.ReadText(BaseAvailabilityFile));
        Assert.True(availability.Contains("internal static bool Matches(", StringComparison.Ordinal), "BaseAvailability.Matches must be internal static.");
    }

    private static int CountOf(string text, string token) => Regex.Matches(text, Regex.Escape(token)).Count;

    /// <summary>Removes block and line comments (including XML doc comments), so only code is matched, and normalises CRLF to LF.</summary>
    private static string Strip(string text)
    {
        string normalised = RepoRoot.Normalise(text);
        string withoutBlocks = Regex.Replace(normalised, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
