namespace BaseStorage.Core.Tests.PatchGuard;

using System;
using System.Linq;

/// <summary>
/// AC-8 and AC-9: the guard discovers every patch body in the fixtures, passes the compliant ones, reports each violating
/// one under exactly its rule, and names every offender in its failure message. The fixtures are scanned, never executed.
/// </summary>
public sealed class PatchGuardFixtureTests
{
    private const string FakeNamespace = "BaseStorage.Core.Tests.PatchGuard.Fixtures.FakeHarmony";
    private const string Helper = "BaseStorage.Plugin.Patching.PatchSafety";
    private const string CompliantNamespace = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant";
    private const string ViolatingNamespace = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Violating";

    private static string TestAssembly => typeof(PatchGuardScanner).Assembly.Location;

    // GW-U-139
    [Fact]
    public void DiscoveryFindsExactlyTheConventionalMarkedAndNestedPatchBodies()
    {
        PatchGuardResult result = PatchGuardScanner.Scan(TestAssembly, Rules(CompliantNamespace));

        string[] expected =
        {
            CompliantNamespace + ".BoolPrefixPatch.Prefix",
            CompliantNamespace + ".FinalizerPatch.Finalizer",
            CompliantNamespace + ".ILManipulatorPatch.ILManipulator",
            CompliantNamespace + ".MethodLevelPatch.Postfix",
            CompliantNamespace + ".OddNamePatch.Gatekeeper",
            CompliantNamespace + ".Outer+InnerPatch.Postfix",
            CompliantNamespace + ".RefResultPostfixPatch.Postfix",
            CompliantNamespace + ".RefResultPrefixPatch.Prefix",
            CompliantNamespace + ".TranspilerPatch.Transpiler",
            CompliantNamespace + ".VoidPostfixPatch.Postfix",
        };
        Assert.Equal(expected.OrderBy(name => name, StringComparer.Ordinal).ToArray(), result.PatchBodies);
    }

    // GW-U-140
    [Fact]
    public void CompliantFixturesPassTheGuardAndAreNotEmpty()
    {
        PatchGuardResult result = PatchGuardScanner.Scan(TestAssembly, Rules(CompliantNamespace));

        Assert.True(result.Violations.Count == 0, PatchGuardScanner.FormatViolations(result));
        Assert.True(result.PatchBodies.Count >= 9, "The compliant scan found " + result.PatchBodies.Count + " patch bodies; at least 9 are required.");
    }

    // GW-U-141
    [Fact]
    public void EachViolatingFixtureIsReportedUnderExactlyItsRule()
    {
        PatchGuardResult result = PatchGuardScanner.Scan(TestAssembly, Rules(ViolatingNamespace));

        var expected = new[]
        {
            (Method: ViolatingNamespace + ".NoHelperPatch.Postfix", Rule: "G1"),
            (Method: ViolatingNamespace + ".AttributeNoHelperPatch.AfterUpdate", Rule: "G1"),
            (Method: ViolatingNamespace + ".TranspilerUsesRunPatch.Transpiler", Rule: "G1"),
            (Method: ViolatingNamespace + ".CallOutsidePatch.Prefix", Rule: "G2"),
            (Method: ViolatingNamespace + ".OwnTryCatchPatch.Postfix", Rule: "G2"),
            (Method: ViolatingNamespace + ".StaticReadPatch.Prefix", Rule: "G2"),
            (Method: ViolatingNamespace + ".WrongKeyPatch.Postfix", Rule: "G3"),
        };
        var actual = result.Violations.Select(violation => (violation.Method, violation.Rule)).ToArray();
        Assert.Equal(expected.OrderBy(pair => pair.Method, StringComparer.Ordinal).ToArray(), actual);

        Assert.Equal("does not call PatchSafety", ReasonOf(result, "NoHelperPatch.Postfix"));
        Assert.Equal("transpiler must use PatchSafety.RunTranspiler", ReasonOf(result, "TranspilerUsesRunPatch.Transpiler"));
        Assert.Contains("System.Object.ToString", ReasonOf(result, "CallOutsidePatch.Prefix"), StringComparison.Ordinal);
        Assert.Contains("leave", ReasonOf(result, "OwnTryCatchPatch.Postfix"), StringComparison.Ordinal);
        Assert.Contains("ldsfld", ReasonOf(result, "StaticReadPatch.Prefix"), StringComparison.Ordinal);
        Assert.Contains(
            "does not pass patch key '" + ViolatingNamespace + ".WrongKeyPatch.Postfix'",
            ReasonOf(result, "WrongKeyPatch.Postfix"),
            StringComparison.Ordinal);
    }

    // GW-U-142
    [Fact]
    public void FailureMessageNamesEachOffenderOnItsOwnLine()
    {
        PatchGuardResult result = PatchGuardScanner.Scan(TestAssembly, Rules(ViolatingNamespace));

        string message = PatchGuardScanner.FormatViolations(result).Replace("\r\n", "\n");
        string[] lines = message.Split('\n');
        string header = lines[0];
        Assert.Contains(result.PatchBodies.Count + " patch bodies", header, StringComparison.Ordinal);
        Assert.Contains("7 violations", header, StringComparison.Ordinal);

        string[] body = lines.Skip(1).ToArray();
        Assert.Equal(7, body.Length);
        Assert.Equal(body.OrderBy(line => line, StringComparer.Ordinal).ToArray(), body);
        foreach (string line in body)
        {
            Assert.Matches(@"^BaseStorage\.Core\.Tests\.PatchGuard\.Fixtures\.Violating\.[^:]+: .+$", line);
        }

        string[] methods =
        {
            ViolatingNamespace + ".NoHelperPatch.Postfix",
            ViolatingNamespace + ".AttributeNoHelperPatch.AfterUpdate",
            ViolatingNamespace + ".TranspilerUsesRunPatch.Transpiler",
            ViolatingNamespace + ".CallOutsidePatch.Prefix",
            ViolatingNamespace + ".OwnTryCatchPatch.Postfix",
            ViolatingNamespace + ".StaticReadPatch.Prefix",
            ViolatingNamespace + ".WrongKeyPatch.Postfix",
        };
        foreach (string method in methods)
        {
            Assert.Contains(method, message, StringComparison.Ordinal);
        }
    }

    // GW-U-143
    [Fact]
    public void G1ReportsEveryCompliantBodyWhenTheHelperNameIsWrong()
    {
        var rules = new PatchGuardRules(FakeNamespace, "Nope.PatchSafety", CompliantNamespace);

        PatchGuardResult result = PatchGuardScanner.Scan(TestAssembly, rules);

        string[] g1 = result.Violations.Where(violation => violation.Rule == "G1").Select(violation => violation.Method).ToArray();
        Assert.True(result.PatchBodies.Count >= 9, "The compliant scan found too few patch bodies to prove G1.");
        Assert.Equal(result.PatchBodies, g1);
    }

    // GW-U-143
    [Fact]
    public void PrefixThatMatchesNoTypeGivesZeroBodiesAndZeroViolations()
    {
        PatchGuardResult result = PatchGuardScanner.Scan(TestAssembly, Rules("BaseStorage.Nope.Nothing"));

        Assert.Empty(result.PatchBodies);
        Assert.Empty(result.Violations);
    }

    // GW-U-143
    [Fact]
    public void IlReaderDecodesEveryMethodBodyInTheTestAssembly()
    {
        (int methods, int instructions) = IlInstructionReader.DecodeAll(TestAssembly);

        Assert.True(methods >= 10, "Only " + methods + " method bodies were decoded.");
        Assert.True(instructions > methods, "Decoding produced too few instructions.");
    }

    private static PatchGuardRules Rules(string typeNamespacePrefix) =>
        new(FakeNamespace, Helper, typeNamespacePrefix);

    private static string ReasonOf(PatchGuardResult result, string methodSuffix) =>
        result.Violations.Single(violation => violation.Method.EndsWith(methodSuffix, StringComparison.Ordinal)).Reason;
}
