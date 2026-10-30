namespace BaseStorage.Core.Tests.Repo;

using System.Linq;

/// <summary>AC-8: the user props override file stays out of git.</summary>
[Trait("Category", "Repo")]
public sealed class RepositoryHygieneTests
{
    // GW-U-013
    [Fact]
    public void GitignoreListsTheUserPropsFile()
    {
        string[] lines = RepoRoot.ReadText(ContractPaths.GitIgnore)
            .Split('\n')
            .Select(line => line.Trim())
            .ToArray();

        Assert.Contains("Directory.Build.user.props", lines);
    }
}
