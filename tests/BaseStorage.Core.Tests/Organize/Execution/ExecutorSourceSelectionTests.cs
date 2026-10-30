namespace BaseStorage.Core.Tests.Organize.Execution;

using System.Linq;
using BaseStorage.Core.Organize;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>S-A-004: the executor names the exact source stack before each add, so identical-looking stacks are not confused.</summary>
public sealed class ExecutorSourceSelectionTests
{
    [Fact]
    public void TheCheckedStackIsNamedBeforeTheAdd()
    {
        Assert.Contains("SelectSourceStack(source, index)", System.IO.File.ReadAllText(Repo.RepoRoot.Combine(@"src\BaseStorage.Core\Organize\Execution\OrganizeExecutor.cs")));
    }
}
