namespace BaseStorage.Core.Tests.Repo;

using System.Linq;
using System.Xml.Linq;

/// <summary>
/// AC-1: the shape of the PatchTargets.Tests project (build-layout.yml section 1, ADR-GWIN-015). Read from the csproj
/// text; no build runs here.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PatchTargetsProjectTests
{
    private const string MetadataLoadContext = "System.Reflection.MetadataLoadContext";

    // GW-U-105
    [Fact]
    public void MetadataLoadContextIsTheOnlyNewPackageAndIsPinnedAtVersion10014()
    {
        XElement[] packages = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.PatchTargetsProject), "PackageReference").ToArray();

        XElement mlc = Assert.Single(packages, p => RepoRoot.Attr(p, "Include") == MetadataLoadContext);
        Assert.Equal("10.0.14", RepoRoot.Attr(mlc, "Version"));

        string[] others = packages
            .Where(p => RepoRoot.Attr(p, "Include") != MetadataLoadContext)
            .Select(p => RepoRoot.Attr(p, "Include") + "=" + RepoRoot.Attr(p, "Version"))
            .ToArray();
        Assert.Equal(new[] { "Microsoft.NET.Test.Sdk=17.14.1", "xunit=2.9.3", "xunit.runner.visualstudio=3.1.4" }, others);
    }

    // GW-U-105
    [Fact]
    public void ForbiddenPackagesProjectReferencesAndWriteTargetsAreAbsent()
    {
        XDocument doc = RepoRoot.LoadXml(ContractPaths.PatchTargetsProject);
        string[] includes = RepoRoot.Named(doc, "PackageReference").Select(p => RepoRoot.Attr(p, "Include")).ToArray();

        Assert.DoesNotContain("BepInEx.Core", includes);
        Assert.DoesNotContain("HarmonyX", includes);
        Assert.DoesNotContain("Lib.Harmony", includes);
        Assert.DoesNotContain("Xunit.SkippableFact", includes);
        Assert.Empty(RepoRoot.Named(doc, "ProjectReference"));
        Assert.Empty(RepoRoot.Named(doc, "Target"));
        Assert.Empty(RepoRoot.Named(doc, "Exec"));
        Assert.Empty(RepoRoot.Named(doc, "Copy"));
        Assert.Equal("net10.0", RepoRoot.Named(doc, "TargetFramework").Single().Value.Trim());
    }

    // GW-U-105
    [Fact]
    public void ThreeAssemblyMetadataItemsCarryTheBuildTimeValues()
    {
        XElement[] items = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.PatchTargetsProject), "AssemblyMetadata").ToArray();

        Assert.Equal(3, items.Length);

        XElement valheim = Assert.Single(items, i => RepoRoot.Attr(i, "Include") == "BaseStorage.ValheimDir");
        Assert.Contains("$(ValheimDir)", RepoRoot.Attr(valheim, "Value"));

        XElement configuration = Assert.Single(items, i => RepoRoot.Attr(i, "Include") == "BaseStorage.Configuration");
        Assert.Contains("$(Configuration)", RepoRoot.Attr(configuration, "Value"));

        XElement plugin = Assert.Single(items, i => RepoRoot.Attr(i, "Include") == "BaseStorage.PluginAssemblyPath");
        Assert.Contains(@"src\BaseStorage.Plugin\bin\$(Configuration)\net462\Dons-QOL.dll", RepoRoot.Attr(plugin, "Value"));
    }
}
