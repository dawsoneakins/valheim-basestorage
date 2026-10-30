namespace BaseStorage.Core.Tests.Repo;

using System;

/// <summary>AC-8: XML documentation is generated for Core only, and CS1591 is not suppressed (ADR-GWIN-006).</summary>
[Trait("Category", "Repo")]
public sealed class CoreDocumentationSetupTests
{
    // GW-U-041
    [Fact]
    public void CoreProjectGeneratesTheDocumentationFile()
    {
        Assert.Equal(new[] { "true" }, RepoRoot.ValuesOf(ContractPaths.CoreProject, "GenerateDocumentationFile"));
    }

    // GW-U-041
    [Theory]
    [InlineData(ContractPaths.DirectoryBuildProps)]
    [InlineData(ContractPaths.CoreTestsProject)]
    [InlineData(ContractPaths.PatchTargetsProject)]
    public void SharedAndTestProjectsDoNotGenerateDocumentation(string file)
    {
        Assert.Empty(RepoRoot.ValuesOf(file, "GenerateDocumentationFile"));
    }

    // GW-U-041
    [Fact]
    public void CoreProjectDoesNotSuppressCS1591()
    {
        string[] noWarn = RepoRoot.ValuesOf(ContractPaths.CoreProject, "NoWarn");

        Assert.DoesNotContain(noWarn, value => value.Contains("1591", StringComparison.Ordinal));
    }
}
