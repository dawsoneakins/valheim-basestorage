namespace BaseStorage.Core.Tests.Repo;

using System.Linq;

/// <summary>AC-16: the Core test project carries the coverage collector.</summary>
[Trait("Category", "Repo")]
public sealed class CoverageSetupTests
{
    // GW-U-022
    [Fact]
    public void CoreTestsProjectReferencesCoverletCollector()
    {
        string[] ids = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.CoreTestsProject), "PackageReference")
            .Select(e => RepoRoot.Attr(e, "Include"))
            .ToArray();

        Assert.Contains("coverlet.collector", ids);
    }
}
