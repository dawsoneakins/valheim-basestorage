namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Restock.Run;
using BaseStorage.Core.Ui;
using BaseStorage.Core.Tests.Repo;

/// <summary>GW-U-2617: the IL dependencies and the XML docs of Restock.Run (S-QR-002 contract section 1 and 3, R9), with the GW-U-2344 pattern.</summary>
public sealed class RestockRunDependencyTests
{
    private const string RunNamespace = "BaseStorage.Core.Restock.Run";
    private const string RunFolder = "src/BaseStorage.Core/Restock/Run";

    private static readonly string[] AllowedSystemNamespaces =
    {
        "System",
        "System.Collections.Generic",
        "System.Collections.ObjectModel",
        "System.Linq",
        "System.Runtime.CompilerServices",
    };

    private static readonly string[] BannedSystemTypes =
    {
        "System.DateTime",
        "System.DateTimeOffset",
        "System.Environment",
        "System.Random",
        "System.Diagnostics.Stopwatch",
    };

    private static readonly string[] BannedSystemNamespacePrefixes = { "System.IO", "System.Threading", "System.Net" };

    // Contract section 1: Abstractions, Snapshots (and Identities), Restock, Reservations, Settings and Ui, plus the Run namespace itself.
    private static readonly string[] AllowedCoreNamespaces =
    {
        "BaseStorage.Core.Abstractions",
        "BaseStorage.Core.Snapshots",
        "BaseStorage.Core.Snapshots.Identities",
        "BaseStorage.Core.Restock",
        RunNamespace,
        "BaseStorage.Core.Reservations",
        "BaseStorage.Core.Settings",
        "BaseStorage.Core.Ui",
    };

    // Contract section 1: Organize.Execution contributes ExecutionStatus only.
    private static readonly string[] AllowedCoreTypes = { "BaseStorage.Core.Organize.Execution.ExecutionStatus" };

    /// <summary>GW-U-2617: the IL of Restock.Run uses only the allowed types, no clock, environment, random, file, thread or network type, the walk reaches the reservation and string table types (non-vacuous), and every public member has an XML doc.</summary>
    [Fact]
    public void RestockRunUsesOnlyAllowedTypesAndIsDocumented()
    {
        RunIlWalk.Walk walk = RunIlWalk.Collect(typeof(RestockExecutor).Assembly, type => type.Namespace == RunNamespace);
        Assert.NotEmpty(walk.Owners);

        var leaves = walk.Types.SelectMany(use => RunIlWalk.Leaves(use.Type).Select(leaf => (Leaf: leaf, use.Origin))).ToList();

        string[] offenders = leaves
            .Where(item => !IsAllowed(item.Leaf))
            .Select(item => (item.Leaf.FullName ?? "(unnamed)") + " used by " + item.Origin)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(offenders);

        string[] forbidden = leaves
            .Where(item => ForbiddenAssemblies.FamilyOf(item.Leaf.Assembly.GetName().Name ?? string.Empty) is not null)
            .Select(item => (item.Leaf.FullName ?? "(unnamed)") + " from " + item.Leaf.Assembly.GetName().Name + " used by " + item.Origin)
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(forbidden);

        HashSet<Type> collected = leaves.Select(item => item.Leaf).ToHashSet();
        Assert.Contains(typeof(ChestReservations<>), collected);
        Assert.Contains(typeof(StringTable), collected);

        var undocumented = new List<string>();
        foreach (string path in Directory.GetFiles(RepoRoot.Combine(RunFolder), "*.cs"))
        {
            string[] lines = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("public ", StringComparison.Ordinal))
                {
                    continue;
                }

                int previous = i - 1;
                while (previous >= 0 && lines[previous].Trim().StartsWith("[", StringComparison.Ordinal))
                {
                    previous--;
                }

                if (previous < 0 || !lines[previous].Trim().StartsWith("///", StringComparison.Ordinal))
                {
                    undocumented.Add(Path.GetFileName(path) + ":" + (i + 1));
                }
            }
        }

        Assert.True(undocumented.Count == 0, "Public members without an XML doc: " + string.Join(", ", undocumented));
    }

    private static bool IsAllowed(Type leaf)
    {
        string ns = leaf.Namespace ?? string.Empty;
        string full = leaf.FullName ?? string.Empty;

        if (RunIlWalk.IsCoverletTracker(full) || leaf.IsGenericParameter)
        {
            return true;
        }

        if (full.StartsWith("BaseStorage.Core", StringComparison.Ordinal))
        {
            return AllowedCoreTypes.Contains(full, StringComparer.Ordinal) || AllowedCoreNamespaces.Contains(ns, StringComparer.Ordinal);
        }

        if (BannedSystemTypes.Contains(full, StringComparer.Ordinal))
        {
            return false;
        }

        if (BannedSystemNamespacePrefixes.Any(prefix => ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal)))
        {
            return false;
        }

        return AllowedSystemNamespaces.Contains(ns, StringComparer.Ordinal);
    }
}
