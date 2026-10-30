namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;

/// <summary>
/// GW-U-1467 (S-E005-004, AC-4, ADR-GWIN-114 decision 8): the S-E005-004 simulation is never skipped and never filtered. No file under
/// tests/BaseStorage.Core.Tests/Simulation/ names a skip, a trait filter, an environment gate, a conditional compile or a thread primitive,
/// and neither the test project nor the solution adds a test filter. The scan strips comments and normalises CRLF. It is a text check.
/// </summary>
[Trait("Category", "Repo")]
public sealed class SimulationGateScanTests
{
    private const string SimulationFolder = "tests/BaseStorage.Core.Tests/Simulation";

    private static readonly string[] ForbiddenInSimulation =
    {
        "Skip =",
        "SkippableFact",
        "[Trait(",
        "Environment.GetEnvironmentVariable",
        "#if",
        "Thread",
        "Task.Run",
        "Parallel.",
    };

    // Edited by S-E004-015 (ADR-GWIN-143): "Trait" matched the contracted file name DepositTraitRules.cs in the csproj; the filter form is "Trait=".
    private static readonly string[] FilterTokens = { "Filter", "Trait=", "Skip", "RunSettings" };

    private static readonly string[] GateFiles =
    {
        "BaseStorage.sln",
        "tests/BaseStorage.Core.Tests/BaseStorage.Core.Tests.csproj",
    };

    // GW-U-1467: no Simulation file skips, filters, gates or spawns threads, and the gate files add no filter.
    [Fact]
    public void TheSimulationIsNeverSkippedOrFilteredByTheGate()
    {
        string[] files = Directory.EnumerateFiles(RepoRoot.Combine(SimulationFolder), "*.cs", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(RepoRoot.FullPath, file).Replace('\\', '/'))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(files);

        foreach (string file in files)
        {
            string text = PluginSourceScan.StripComments(RepoRoot.ReadText(file));
            foreach (string token in ForbiddenInSimulation)
            {
                Assert.False(text.Contains(token, StringComparison.Ordinal), file + " contains " + token + ".");
            }
        }

        foreach (string gateFile in GateFiles)
        {
            string text = RepoRoot.ReadText(gateFile);
            foreach (string token in FilterTokens)
            {
                Assert.False(text.Contains(token, StringComparison.OrdinalIgnoreCase), gateFile + " adds a test filter (" + token + ").");
            }
        }
    }
}
