namespace BaseStorage.PatchTargets.Tests;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Infrastructure;

/// <summary>AC-1: the build-time values captured by MSBuild (contract 4.8.1), and the lazy fixture (ADR-GWIN-015).</summary>
public sealed class BuildValuesTests
{
    // GW-U-104
    [Fact]
    public void ExactlyOneMetadataItemPerKeyIsCompiledIntoTheAssembly()
    {
        AssemblyMetadataAttribute[] items = typeof(BuildValuesTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToArray();

        Assert.Single(items, a => a.Key == "BaseStorage.ValheimDir");
        Assert.Single(items, a => a.Key == "BaseStorage.Configuration");
        Assert.Single(items, a => a.Key == "BaseStorage.PluginAssemblyPath");
    }

    // GW-U-104
    [Fact]
    public void ValheimDirIsNonEmptyAndTheConfigurationMatchesTheAssembly()
    {
        string? configuration = typeof(BuildValuesTests).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;

        Assert.False(string.IsNullOrWhiteSpace(BuildValues.ValheimDir));
        Assert.Equal(configuration, BuildValues.Configuration);
    }

    // GW-U-104
    [Fact]
    public void PluginPathIsRootedWithoutRelativeSegmentsAndPointsAtTheNet462Build()
    {
        string path = BuildValues.PluginAssemblyPath;

        Assert.True(Path.IsPathRooted(path));
        Assert.DoesNotContain(@"\..\", path);
        Assert.DoesNotContain(@"\.\", path);
        Assert.EndsWith(@"\src\BaseStorage.Plugin\bin\" + BuildValues.Configuration + @"\net462\Dons-QOL.dll", path, StringComparison.Ordinal);
    }

    // GW-U-104
    [Fact]
    public void GameDllIsTheValheimDirConcatenatedWithTheManagedPath()
    {
        Assert.Equal(BuildValues.ValheimDir + @"\valheim_Data\Managed\assembly_valheim.dll", BuildValues.GameDll);
    }

    // GW-U-107
    [Fact]
    public void FixtureIsLazyAndSafeWhenSkipped()
    {
        string missing = Path.Combine(Path.GetTempPath(), "bst-missing-" + Guid.NewGuid().ToString("N"));

        using ValheimAssemblyFixture fixture = new(missing, Path.Combine(missing, "assembly_valheim.dll"));

        Assert.False(Directory.Exists(missing));
        fixture.Dispose();
        fixture.Dispose();
    }
}
