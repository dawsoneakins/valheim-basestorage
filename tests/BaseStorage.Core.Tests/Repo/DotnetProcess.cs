namespace BaseStorage.Core.Tests.Repo;

using System.Diagnostics;
using System.IO;
using System.Linq;

/// <summary>
/// Shared xunit collection so MSBuild process tests never run in parallel (ADR-GWIN-004).
/// </summary>
[CollectionDefinition("MSBuildProcess", DisableParallelization = true)]
public sealed class MSBuildProcessCollection
{
}

public sealed record DotnetResult(int ExitCode, string Output);

/// <summary>
/// Runs the dotnet CLI with a 120 s limit. The child environment has VALHEIM_DIR and related
/// MSBuild variables stripped unless the caller sets them, and node reuse is disabled.
/// </summary>
public static class DotnetProcess
{
    private static readonly TimeSpan ProcessLimit = TimeSpan.FromSeconds(120);
    private static readonly string[] StrippedVariables =
        ["VALHEIM_DIR", "MSBUILD_EXE_PATH", "MSBuildSDKsPath", "MSBuildExtensionsPath"];

    public static DotnetResult Msbuild(
        string projectPath,
        IEnumerable<string> arguments,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var args = new List<string> { "msbuild", projectPath, "-nologo" };
        args.AddRange(arguments);
        args.Add("-nodeReuse:false");
        string workingDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath))!;
        return Run(args, workingDirectory, environment);
    }

    public static DotnetResult Run(
        IEnumerable<string> arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var info = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        foreach (string name in StrippedVariables)
        {
            info.Environment.Remove(name);
        }

        info.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        if (environment is not null)
        {
            foreach (KeyValuePair<string, string> pair in environment)
            {
                info.Environment[pair.Key] = pair.Value;
            }
        }

        using Process process = Process.Start(info) ?? throw new InvalidOperationException("dotnet could not be started.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(ProcessLimit))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("dotnet " + string.Join(" ", arguments) + " exceeded 120 seconds.");
        }

        process.WaitForExit();
        return new DotnetResult(process.ExitCode, stdout.Result + stderr.Result);
    }
}

/// <summary>
/// A per-test folder under the OS temp directory. It is deleted on dispose. Tests never use the real ValheimDir.
/// </summary>
public sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Root = Path.Combine(Path.GetTempPath(), "bs-gwin-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string Combine(params string[] parts) =>
        Path.Combine(new[] { Root }.Concat(parts).ToArray());

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
