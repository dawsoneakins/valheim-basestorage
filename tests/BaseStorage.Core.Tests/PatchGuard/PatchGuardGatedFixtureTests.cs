namespace BaseStorage.Core.Tests.PatchGuard;

using System;
using System.Linq;

/// <summary>
/// AC-2 (ADR-GWIN-136, contract 8.3 errata 1): with the optional gate, the guard follows a gated wrapper into its same-type Core and
/// checks the Core. Without the gate the rules are unchanged. The gated fixtures are scanned, never executed.
/// </summary>
public sealed class PatchGuardGatedFixtureTests
{
    private const string FakeNamespace = "BaseStorage.Core.Tests.PatchGuard.Fixtures.FakeHarmony";
    private const string Helper = "BaseStorage.Plugin.Patching.PatchSafety";
    private const string GatedNamespace = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Gated";
    private const string Gate = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Gated.StandInCraftFromBaseGate.IsOpen";

    private static string TestAssembly => typeof(PatchGuardScanner).Assembly.Location;

    // GW-U-1825
    [Fact]
    public void GatedCompliantHasNoViolationAndTheBodySetListsOnlyTheWrapper()
    {
        PatchGuardResult result = PatchGuardScanner.Scan(TestAssembly, GatedRules());

        Assert.DoesNotContain(result.Violations, violation => violation.Method == GatedNamespace + ".GatedCompliant.Prefix");

        string[] expected =
        {
            GatedNamespace + ".GatedCompliant.Prefix",
            GatedNamespace + ".GatedCoreWithoutKey.Prefix",
            GatedNamespace + ".GatedExtraCall.Prefix",
            GatedNamespace + ".GatedNoCore.Prefix",
        };
        Assert.Equal(expected.OrderBy(name => name, StringComparer.Ordinal).ToArray(), result.PatchBodies);
        Assert.DoesNotContain(result.PatchBodies, name => name.EndsWith("Core", StringComparison.Ordinal));
    }

    // GW-U-1826
    [Fact]
    public void EachBrokenGatedFixtureIsReportedUnderExactlyItsRule()
    {
        PatchGuardResult result = PatchGuardScanner.Scan(TestAssembly, GatedRules());

        var expected = new[]
        {
            (Method: GatedNamespace + ".GatedCoreWithoutKey.Prefix", Rule: "G3"),
            (Method: GatedNamespace + ".GatedExtraCall.Prefix", Rule: "G2"),
            (Method: GatedNamespace + ".GatedNoCore.Prefix", Rule: "G1"),
        };
        var actual = result.Violations.Select(violation => (violation.Method, violation.Rule)).ToArray();
        Assert.Equal(expected.OrderBy(pair => pair.Method, StringComparer.Ordinal).ToArray(), actual);

        Assert.Equal("does not call PatchSafety", ReasonOf(result, "GatedNoCore.Prefix"));
        Assert.Contains("GatedExtraCall.Extra", ReasonOf(result, "GatedExtraCall.Prefix"), StringComparison.Ordinal);
        Assert.Contains(
            "does not pass patch key '" + GatedNamespace + ".GatedCoreWithoutKey.Prefix'",
            ReasonOf(result, "GatedCoreWithoutKey.Prefix"),
            StringComparison.Ordinal);
    }

    // GW-U-1827
    [Fact]
    public void WithoutTheGateTheCompliantFixtureFailsG1G2AndG3()
    {
        var rules = new PatchGuardRules(FakeNamespace, Helper, GatedNamespace + ".GatedCompliant");

        PatchGuardResult result = PatchGuardScanner.Scan(TestAssembly, rules);

        Assert.Equal(new[] { GatedNamespace + ".GatedCompliant.Prefix" }, result.PatchBodies);
        Assert.Equal(new[] { "G1", "G2", "G3" }, result.Violations.Select(violation => violation.Rule).ToArray());
        Assert.All(result.Violations, violation => Assert.Equal(GatedNamespace + ".GatedCompliant.Prefix", violation.Method));
    }

    private static PatchGuardRules GatedRules() => new(FakeNamespace, Helper, GatedNamespace, Gate);

    private static string ReasonOf(PatchGuardResult result, string methodSuffix) =>
        result.Violations.Single(violation => violation.Method.EndsWith(methodSuffix, StringComparison.Ordinal)).Reason;
}
