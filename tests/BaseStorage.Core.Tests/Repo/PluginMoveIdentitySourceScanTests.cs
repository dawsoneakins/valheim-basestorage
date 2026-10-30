namespace BaseStorage.Core.Tests.Repo;

using System.IO;

/// <summary>
/// S-A-004: a move copies the exact stack the executor checked. Two stacks with the same item, quality and count can differ in
/// durability or crafter, so looking one up by those values could swap their data or lose it.
/// </summary>
public class PluginMoveIdentitySourceScanTests
{
    [Theory]
    [InlineData(@"src\BaseStorage.Plugin\Organize\GameOrganizeContainerLayer.cs")]
    [InlineData(@"src\BaseStorage.Plugin\Deposit\GameDepositContainerLayer.cs")]
    public void MovesDoNotCopyFromALookupOrAPrefabDefault(string relative)
    {
        string source = File.ReadAllText(RepoRoot.Combine(relative));

        Assert.Contains("SelectSourceStack(ContainerId source, int stackIndex)", source);
        Assert.DoesNotContain("IndexOfSource(", source);
        Assert.DoesNotContain("template = PrefabTemplate(", source);
        Assert.DoesNotContain("template = GameOrganizeContainerLayer.PrefabTemplate(", source);
    }
}
