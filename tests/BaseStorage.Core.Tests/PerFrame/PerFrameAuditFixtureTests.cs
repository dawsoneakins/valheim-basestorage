namespace BaseStorage.Core.Tests.PerFrame;

using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;

/// <summary>
/// AC-3 and AC-4 (S-E009-001, ADR-GWIN-161, contract quality-audit 3.5 rows F1 to F4): the audit discovers exactly the frame methods of the
/// compliant fixtures and reports each violating fixture under its category. The fixtures are scanned through their metadata, never executed.
/// </summary>
public sealed class PerFrameAuditFixtureTests
{
    private const string ViolatingNamespace = "BaseStorage.Core.Tests.PerFrame.Fixtures.Violating.";
    private const string CompliantNamespace = "BaseStorage.Core.Tests.PerFrame.Fixtures.Compliant.";

    private static string TestAssembly => typeof(PerFrameAuditScanner).Assembly.Location;

    // GW-U-2352 (AC-3, QA2.5 F2): the compliant fixtures pass, and the discovery is exact (allowed members, nested types, direct only, exact names).
    [Fact]
    public void CompliantFixturesPassAndDiscoveryIsExact()
    {
        PerFrameAuditResult result = PerFrameAuditScanner.Scan(TestAssembly, CompliantNamespace);

        string[] expected =
        {
            CompliantNamespace + "CheapUpdate.Update",
            CompliantNamespace + "Outer+Inner.Update",
            CompliantNamespace + "TicksInLateUpdate.LateUpdate",
        };
        Assert.Equal(expected, result.AuditedMethods);
        Assert.Empty(result.Violations);
    }

    // GW-U-2353 (AC-3, QA2.5 F3, QA2.6): the failure message names every offender, in the contract's exact text.
    [Fact]
    public void TheFailureMessageNamesEveryOffender()
    {
        PerFrameAuditResult result = PerFrameAuditScanner.Scan(TestAssembly, ViolatingNamespace);

        string expected = string.Join(
            "\n",
            "Per-frame audit scanned 4 Update/LateUpdate methods and found 4 violations.",
            ViolatingNamespace + "PlansInStaticUpdate.Update calls BaseStorage.Core.Availability.ConsumptionPlanner.Plan (planner) directly; move it out of the frame loop (AC-9.2).",
            ViolatingNamespace + "ReadsSnapshotsInUpdate.Update calls BaseStorage.Core.Bases.Resolution.IBaseSnapshotSource.GetStations (cache rebuild) directly; move it out of the frame loop (AC-9.2).",
            ViolatingNamespace + "RebuildsCacheInLateUpdate.LateUpdate calls BaseStorage.Core.Bases.Resolution.BaseCache.Resolve (cache rebuild) directly; move it out of the frame loop (AC-9.2).",
            ViolatingNamespace + "ResolvesInUpdate.Update calls BaseStorage.Core.Bases.Resolution.BaseResolver.Resolve (base resolver) directly; move it out of the frame loop (AC-9.2).");

        Assert.Equal(expected, PerFrameAuditScanner.FormatViolations(result));
    }

    // GW-U-2354 (AC-3, QA2.5 F4, QA2.3): the forbidden set is the contract table, in order, and the frame names are exactly Update and LateUpdate.
    [Fact]
    public void TheForbiddenSetIsTheContractTable()
    {
        PerFrameForbiddenCall[] expected =
        {
            new(typeof(BaseResolver).FullName!, nameof(BaseResolver.Resolve), "base resolver"),
            new(typeof(StationGraph).FullName!, nameof(StationGraph.FindBase), "base resolver"),
            new(typeof(BaseMembership).FullName!, nameof(BaseMembership.FindMembers), "base resolver"),
            new(typeof(BaseCache).FullName!, nameof(BaseCache.Resolve), "cache rebuild"),
            new(typeof(IBaseSnapshotSource).FullName!, nameof(IBaseSnapshotSource.GetStations), "cache rebuild"),
            new(typeof(IBaseSnapshotSource).FullName!, nameof(IBaseSnapshotSource.GetContainers), "cache rebuild"),
            new("BaseStorage.Plugin.World.GameSnapshotSource", "GetStations", "cache rebuild"),
            new("BaseStorage.Plugin.World.GameSnapshotSource", "GetContainers", "cache rebuild"),
            new(typeof(OrganizePlanner).FullName!, nameof(OrganizePlanner.Plan), "planner"),
            new(typeof(DepositPlanner).FullName!, nameof(DepositPlanner.Plan), "planner"),
            new(typeof(ConsumptionPlanner).FullName!, nameof(ConsumptionPlanner.Plan), "planner"),
            new(typeof(AutoAssigner).FullName!, nameof(AutoAssigner.Resolve), "planner"),
        };

        Assert.Equal(expected, PerFrameAuditScanner.ForbiddenCalls);
        Assert.Equal(new[] { "Update", "LateUpdate" }, PerFrameAuditScanner.FrameMethodNames);
    }

    // GW-U-2355 (AC-4, QA2.5 F1): each violating fixture is caught under its own category, and only those four are reported.
    [Fact]
    public void EachViolatingFixtureIsReportedUnderItsCategory()
    {
        PerFrameAuditResult result = PerFrameAuditScanner.Scan(TestAssembly, ViolatingNamespace);

        string[] expectedMethods =
        {
            ViolatingNamespace + "PlansInStaticUpdate.Update",
            ViolatingNamespace + "ReadsSnapshotsInUpdate.Update",
            ViolatingNamespace + "RebuildsCacheInLateUpdate.LateUpdate",
            ViolatingNamespace + "ResolvesInUpdate.Update",
        };
        Assert.Equal(expectedMethods, result.AuditedMethods);

        PerFrameViolation[] expectedViolations =
        {
            new(ViolatingNamespace + "PlansInStaticUpdate.Update", "BaseStorage.Core.Availability.ConsumptionPlanner.Plan", "planner"),
            new(ViolatingNamespace + "ReadsSnapshotsInUpdate.Update", "BaseStorage.Core.Bases.Resolution.IBaseSnapshotSource.GetStations", "cache rebuild"),
            new(ViolatingNamespace + "RebuildsCacheInLateUpdate.LateUpdate", "BaseStorage.Core.Bases.Resolution.BaseCache.Resolve", "cache rebuild"),
            new(ViolatingNamespace + "ResolvesInUpdate.Update", "BaseStorage.Core.Bases.Resolution.BaseResolver.Resolve", "base resolver"),
        };
        Assert.Equal(expectedViolations, result.Violations);
    }
}
