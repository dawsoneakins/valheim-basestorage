namespace BaseStorage.Core.Tests.Repo;

using System;

/// <summary>
/// AC-11: Core stays a single netstandard2.0 target with no package or assembly references, and Directory.Build.props
/// keeps Nullable enabled (contract section 0, R1 and R2). The existing Core tests are not edited by this story.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreTargetUnchangedTests
{
    // GW-U-046
    [Fact]
    public void CoreHasExactlyOneNetstandard20Target()
    {
        Assert.Equal(new[] { "netstandard2.0" }, RepoRoot.ValuesOf(ContractPaths.CoreProject, "TargetFramework"));
        Assert.Empty(RepoRoot.ValuesOf(ContractPaths.CoreProject, "TargetFrameworks"));
    }

    // GW-U-046
    [Fact]
    public void CoreHasNoPackageOrAssemblyReferences()
    {
        Assert.Empty(RepoRoot.ValuesOf(ContractPaths.CoreProject, "PackageReference"));
        Assert.Empty(RepoRoot.ValuesOf(ContractPaths.CoreProject, "Reference"));
    }

    // GW-U-046
    [Fact]
    public void DirectoryBuildPropsStillEnablesNullable()
    {
        Assert.Equal(new[] { "enable" }, RepoRoot.ValuesOf(ContractPaths.DirectoryBuildProps, "Nullable"));
    }
}
