namespace BaseStorage.Core.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using BaseStorage.Core.Tests.Repo;

/// <summary>AC-5: BaseStorage.Core must not reference forbidden assembly families.</summary>
public sealed class CoreReferencesTests
{
    private const string CoreAssetsFile = "src/BaseStorage.Core/obj/project.assets.json";

    // GW-U-006
    [Fact]
    public void EveryReferencedAssemblyOfCoreIsAllowed()
    {
        var offenders = new List<string>();
        foreach (AssemblyName reference in typeof(BaseStorage.Core.CoreAssemblyMarker).Assembly.GetReferencedAssemblies())
        {
            string name = reference.Name ?? string.Empty;
            string? family = ForbiddenAssemblies.FamilyOf(name);
            if (family is not null)
            {
                offenders.Add(Bst2001Message.Format(name, family));
            }
        }

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    // GW-U-007
    [Fact]
    public void Bst2001MessageMatchesTheErrorCodesFormat()
    {
        string actual = Bst2001Message.Format("UnityEngine.CoreModule", "Unity");

        Assert.Equal("BST2001: BaseStorage.Core references forbidden assembly 'UnityEngine.CoreModule' (family: Unity).", actual);
    }

    // GW-U-008: static backstop for references the compiler drops because they are unused.
    [Fact]
    public void CoreProjectDeclaresNoForbiddenReferenceItems()
    {
        XDocument project = RepoRoot.LoadXml(ContractPaths.CoreProject);
        var names = new List<string>();
        foreach (XElement item in project.Descendants().Where(e =>
                     e.Name.LocalName is "PackageReference" or "Reference" or "ProjectReference"))
        {
            names.Add(SimpleNameOf(item));
        }

        string assetsPath = RepoRoot.Combine(CoreAssetsFile);
        if (File.Exists(assetsPath))
        {
            names.AddRange(LibraryNames(assetsPath));
        }

        string[] offenders = names.Where(ForbiddenAssemblies.IsForbidden).ToArray();
        Assert.True(offenders.Length == 0, "Forbidden references in Core: " + string.Join(", ", offenders));
    }

    private static string SimpleNameOf(XElement item)
    {
        string include = RepoRoot.Attr(item, "Include");
        if (item.Name.LocalName == "ProjectReference")
        {
            return Path.GetFileNameWithoutExtension(include) ?? string.Empty;
        }

        int comma = include.IndexOf(',');
        return (comma >= 0 ? include[..comma] : include).Trim();
    }

    private static List<string> LibraryNames(string assetsPath)
    {
        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var names = new List<string>();
        if (json.RootElement.TryGetProperty("libraries", out JsonElement libraries))
        {
            foreach (JsonProperty library in libraries.EnumerateObject())
            {
                // Keys look like "BepInEx.Core/5.4.21"; the simple name is the part before the slash.
                names.Add(library.Name.Split('/')[0]);
            }
        }

        return names;
    }
}

/// <summary>
/// Test-side builder for BST2001 lines (contracts/error-codes.md).
/// </summary>
internal static class Bst2001Message
{
    public static string Format(string simpleName, string family) =>
        $"BST2001: BaseStorage.Core references forbidden assembly '{simpleName}' (family: {family}).";
}
