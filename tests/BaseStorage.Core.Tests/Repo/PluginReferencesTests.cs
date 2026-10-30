namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;
using System.Xml.Linq;

/// <summary>AC-9, AC-11: Plugin game references, pinned packages and the BS1001 guard.</summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class PluginReferencesTests
{
    private const string ManagedFolder = @"$(ValheimDir)\valheim_Data\Managed\";

    private static readonly string[] GameAssemblies =
    [
        "assembly_valheim",
        "assembly_utils",
        "assembly_guiutils",
        "UnityEngine",
        "UnityEngine.CoreModule",
        "UnityEngine.UIModule", // Edited by S-E004-012 (ADR-GWIN-136): Canvas for SA7/SA9 root canvas and scaleFactor (contract station-actions SA7, SA9).
        "UnityEngine.UI",
        "UnityEngine.PhysicsModule",
        "Unity.TextMeshPro",
    ];

    // GW-U-014
    // Edited by S-E004-012 (ADR-GWIN-136): eight becomes nine, with UnityEngine.UIModule for Canvas (contract station-actions SA7, SA9).
    [Fact]
    public void PluginReferencesExactlyTheNineContractGameAssemblies()
    {
        XElement[] references = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.PluginProject), "Reference").ToArray();
        string[] names = references.Select(r => RepoRoot.Attr(r, "Include")).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(GameAssemblies.OrderBy(n => n, StringComparer.Ordinal).ToArray(), names);

        foreach (XElement reference in references)
        {
            string name = RepoRoot.Attr(reference, "Include");
            Assert.Equal(ManagedFolder + name + ".dll", ChildValue(reference, "HintPath"));
            Assert.Equal("false", PrivateFlag(reference));
        }
    }

    // GW-U-016
    [Fact]
    public void OnlyAssemblyValheimIsPublicized()
    {
        foreach (XElement reference in RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.PluginProject), "Reference"))
        {
            string name = RepoRoot.Attr(reference, "Include");
            string publicize = RepoRoot.Attr(reference, "Publicize");
            if (publicize.Length == 0)
            {
                publicize = ChildValue(reference, "Publicize");
            }

            bool isPublicized = string.Equals(publicize, "true", StringComparison.OrdinalIgnoreCase);
            Assert.Equal(name == "assembly_valheim", isPublicized);
        }
    }

    // GW-U-016
    [Theory]
    [InlineData("BepInEx.Core", "5.4.21")]
    [InlineData("Microsoft.NETFramework.ReferenceAssemblies", "1.0.3")]
    [InlineData("BepInEx.AssemblyPublicizer.MSBuild", "0.4.2")]
    public void PluginPinsTheContractPackageVersions(string id, string version)
    {
        XElement[] matches = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.PluginProject), "PackageReference")
            .Where(p => RepoRoot.Attr(p, "Include") == id)
            .ToArray();

        Assert.Single(matches);
        string actual = RepoRoot.Attr(matches[0], "Version");
        if (actual.Length == 0)
        {
            actual = ChildValue(matches[0], "Version");
        }

        Assert.Equal(version, actual);
    }

    // GW-I-002
    [Fact]
    public void BuildFailsWithBs1001WhenAssemblyValheimIsMissing()
    {
        using var emptyGame = new TempFolder();
        string expectedPath = emptyGame.Combine("valheim_Data", "Managed", "assembly_valheim.dll");

        DotnetResult result = DotnetProcess.Msbuild(
            RepoRoot.Combine(ContractPaths.PluginProject),
            ["-t:BaseStorageVerifyValheimDir", "-p:ValheimDir=" + emptyGame.Root]);

        Assert.NotEqual(0, result.ExitCode);
        string expected = "error BS1001: Valheim not found: '" + expectedPath
            + "' does not exist. Set VALHEIM_DIR or create Directory.Build.user.props with <ValheimDir>.";
        Assert.Contains(expected, RepoRoot.Normalise(result.Output));
    }

    // GW-I-002
    [Fact]
    public void DesignTimeBuildSkipsTheBs1001Guard()
    {
        using var emptyGame = new TempFolder();

        DotnetResult result = DotnetProcess.Msbuild(
            RepoRoot.Combine(ContractPaths.PluginProject),
            ["-t:BaseStorageVerifyValheimDir", "-p:ValheimDir=" + emptyGame.Root, "-p:DesignTimeBuild=true"]);

        Assert.Equal(0, result.ExitCode);
    }

    private static string ChildValue(XElement element, string localName) =>
        element.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value.Trim() ?? string.Empty;

    private static string PrivateFlag(XElement reference)
    {
        string child = ChildValue(reference, "Private");
        string value = child.Length > 0 ? child : RepoRoot.Attr(reference, "Private");
        return value.ToLowerInvariant();
    }
}
