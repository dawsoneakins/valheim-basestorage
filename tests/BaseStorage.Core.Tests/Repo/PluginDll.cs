namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;

/// <summary>
/// Locates the built Plugin dll (src/BaseStorage.Plugin/bin/&lt;cfg&gt;/net462/Dons-QOL.dll) and builds the Plugin through
/// DotnetProcess when it is missing (ADR-GWIN-013 amendment). Extracted from GW-I-010 and shared with GW-I-120.
/// Not a test class.
/// </summary>
public static class PluginDll
{
    /// <summary>Returns the full path of the built Plugin dll for the configuration, building the Plugin first when it is missing.</summary>
    /// <param name="configurationName">The build configuration, for example Release.</param>
    /// <returns>The full path of the existing Plugin dll.</returns>
    public static string EnsureBuilt(string configurationName)
    {
        string relative = Path.Combine(PluginSourceScan.PluginFolder, "bin", configurationName, "net462", "Dons-QOL.dll");
        string path = RepoRoot.Combine(relative);
        if (!File.Exists(path))
        {
            // dotnet test BaseStorage.sln does not build the Plugin (no test project references it), so build it here (ADR-GWIN-013 amendment).
            DotnetResult build = DotnetProcess.Run(
                new[] { "build", RepoRoot.Combine(ContractPaths.PluginProject), "-c", configurationName, "--nologo", "-nodeReuse:false" },
                RepoRoot.FullPath);
            Assert.True(
                build.ExitCode == 0,
                $"dotnet build of the Plugin failed with exit code {build.ExitCode}:{Environment.NewLine}{Tail(build.Output)}");
        }

        Assert.True(File.Exists(path), $"{relative} is still missing after building the Plugin.");
        return path;
    }

    private static string Tail(string output) =>
        string.Join(Environment.NewLine, output.Split('\n').TakeLast(20));
}
