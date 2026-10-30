namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Linq;
using BaseStorage.PatchTargets.Tests.Checking;

/// <summary>
/// GW-U-1817 to GW-U-1819 (AC-2; contract craft-from-base-toggle 7.2 and 7.3): the checker run on the offline fixtures of
/// Fixtures/ToggleGate. The checker reads this test assembly's own file through PEReader and never loads it. Unconditional.
/// </summary>
public sealed class ToggleGateCheckerTests
{
    private const string Ns = "BaseStorage.PatchTargets.Tests.Fixtures.ToggleGate.";
    private const string Gate = Ns + "FixtureGate";
    private const string Call = "BST2030: E3 patch method '";

    private static readonly string TestDll = typeof(ToggleGateCheckerTests).Assembly.Location;

    private static readonly ToggleGateRules FixtureRules = new(
        [Ns + "E3"],
        Ns + "FixtureE3PatchAttribute",
        Gate,
        "IsOpen",
        [Ns + "E3.GatedBool", Ns + "E3.Missing"]);

    // GW-U-1817 (AC-2): the fixture run reports exactly the eight contracted lines, in ordinal order. An ungated, capturing, late or discarded gate fails.
    [Fact]
    public void TheFixtureRunReportsExactlyTheEightContractedLines()
    {
        string[] lines = Run().Diagnostics.ToArray();

        Assert.Equal(8, lines.Length);
        Assert.StartsWith(Call + Ns + "E3.CapturingGate.Prefix' does not call " + Gate + ".IsOpen() as its first instruction (found: newobj ", lines[0], StringComparison.Ordinal);
        Assert.Equal(
            new[]
            {
                Call + Ns + "E3.LateGate.Prefix' does not call " + Gate + ".IsOpen() as its first instruction (found: call " + Ns + "FixtureWork::Touch).",
                Call + Ns + "E3.Ungated.Prefix' does not call " + Gate + ".IsOpen() as its first instruction (found: call " + Ns + "FixtureWork::Touch).",
                "BST2031: E3 patch method '" + Ns + "E3.DiscardedGate.Prefix' does not branch on the " + Gate + ".IsOpen() result.",
                "BST2032: E3 patch class '" + Ns + "E3.Unmarked' is in an E3 namespace but lacks [FixtureE3Patch].",
                "BST2033: E3 patch class '" + Ns + "E3.TranspilerOnly' must declare a Prefix or Postfix and no Transpiler, Finalizer or ILManipulator (found: Transpiler).",
                "BST2034: class '" + Ns + "Elsewhere.MarkerOnly' carries [FixtureE3Patch] but no HarmonyLib.HarmonyPatch attribute.",
                "BST2035: expected E3 patch class '" + Ns + "E3.Missing' was not found.",
            },
            lines[1..]);
        Assert.Equal(lines.OrderBy(line => line, StringComparer.Ordinal).ToArray(), lines);
    }

    // GW-U-1818 (AC-2, D2, D1): discovery finds the marked Elsewhere class, ignores the unmarked non-E3 one, and checks both void-pair methods.
    [Fact]
    public void DiscoveryFindsTheMarkedElsewhereClassAndCheckesBothVoidPairMethods()
    {
        ToggleGateReport report = Run();

        Assert.Contains(Ns + "Elsewhere.MarkedGated", report.E3Classes);
        Assert.DoesNotContain(Ns + "Elsewhere.NotE3", report.E3Classes);
        Assert.Contains(Ns + "E3.GatedVoidPair.Prefix", report.CheckedMethods);
        Assert.Contains(Ns + "E3.GatedVoidPair.Postfix", report.CheckedMethods);
    }

    // GW-U-1819 (AC-2): no class of Fixtures.Patches is an E3 class, so the Harmony reader tests stay independent.
    [Fact]
    public void NoFixturePatchesClassIsAnE3Class()
    {
        ToggleGateReport report = Run();

        Assert.DoesNotContain(report.E3Classes, name => name.StartsWith("BaseStorage.PatchTargets.Tests.Fixtures.Patches.", StringComparison.Ordinal));
    }

    private static ToggleGateReport Run() => ToggleGateChecker.Check(TestDll, FixtureRules);
}
