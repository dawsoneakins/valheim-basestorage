namespace BaseStorage.Core.Tests.Repo;

using System.Linq;
using System.Xml.Linq;

/// <summary>AC-10: nuget.config lists exactly the two contract feeds, after a clear.</summary>
[Trait("Category", "Repo")]
public sealed class NuGetConfigTests
{
    // GW-U-015
    [Fact]
    public void NuGetConfigClearsAndListsExactlyTheNugetOrgAndBepInExSources()
    {
        XElement sources = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.NuGetConfig), "packageSources").Single();
        XElement[] children = sources.Elements().ToArray();

        Assert.Equal("clear", children.FirstOrDefault()?.Name.LocalName ?? string.Empty);
        Assert.Single(children, e => e.Name.LocalName == "clear");

        (string Key, string Value)[] actual = children
            .Where(e => e.Name.LocalName == "add")
            .Select(e => (Key: RepoRoot.Attr(e, "key"), Value: RepoRoot.Attr(e, "value")))
            .ToArray();

        Assert.Equal(
            new[]
            {
                ("nuget.org", "https://api.nuget.org/v3/index.json"),
                ("BepInEx", "https://nuget.bepinex.dev/v3/index.json"),
            },
            actual);
    }

    // GW-U-015
    [Fact]
    public void NuGetConfigMapsBepInExPackagesToTheBepInExFeedAndEverythingElseToNugetOrg()
    {
        XElement mapping = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.NuGetConfig), "packageSourceMapping").Single();

        (string Key, string[] Patterns)[] actual = mapping.Elements()
            .Where(e => e.Name.LocalName == "packageSource")
            .Select(e => (
                Key: RepoRoot.Attr(e, "key"),
                Patterns: e.Elements().Where(p => p.Name.LocalName == "package").Select(p => RepoRoot.Attr(p, "pattern")).ToArray()))
            .ToArray();

        Assert.Equal(2, actual.Length);
        Assert.Equal(new[] { "BepInEx.*" }, actual.Single(a => a.Key == "BepInEx").Patterns);
        Assert.Equal(new[] { "*" }, actual.Single(a => a.Key == "nuget.org").Patterns);
    }
}
