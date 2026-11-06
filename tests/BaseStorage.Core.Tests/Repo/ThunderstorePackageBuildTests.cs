namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

/// <summary>
/// AC-7.1 (S-E007-001): the PackageThunderstore target, run against temp folders, writes dist/Dons-QOL-version.zip
/// with exactly the contracted entries, is off by default, and a real Release build with the property produces the zip (ADR-GWIN-069).
/// </summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class ThunderstorePackageBuildTests
{
    // GW-I-850
    [Fact]
    public void PackageTargetWritesTheZipWithExactlyTheSixEntries()
    {
        using var gameFolder = new TempFolder();
        using var outputFolder = new TempFolder();
        using var distFolder = new TempFolder();
        using var objFolder = new TempFolder();
        WriteFakeOutputs(outputFolder);
        string version = ThunderstorePackage.ExpectedVersion();

        DotnetResult result = Package(gameFolder, outputFolder, distFolder, objFolder, packageProperty: true);

        Assert.True(result.ExitCode == 0, "PackageThunderstore failed:" + Environment.NewLine + Tail(result.Output));
        string zipPath = distFolder.Combine(ThunderstorePackage.ZipFileName(version));
        Assert.Equal(new[] { ThunderstorePackage.ZipFileName(version) }, Listing(distFolder.Root));
        Assert.Contains(ThunderstorePackage.Bs1004Prefix + zipPath + "'.", RepoRoot.Normalise(result.Output));
        Assert.DoesNotContain(": warning ", result.Output);
        Assert.Empty(Listing(gameFolder.Root));

        using ZipArchive zip = ZipFile.OpenRead(zipPath);
        Assert.Equal(ThunderstorePackage.ZipEntries, FileEntries(zip));
        ThunderstorePackage.AssertManifest(ReadEntry(zip, "manifest.json"), version);
        Assert.Equal(File.ReadAllBytes(RepoRoot.Combine(ThunderstorePackage.Readme)), ReadEntryBytes(zip, "README.md"));
        Assert.Equal(File.ReadAllBytes(RepoRoot.Combine(ThunderstorePackage.Changelog)), ReadEntryBytes(zip, "CHANGELOG.md"));
        Assert.Equal(File.ReadAllBytes(RepoRoot.Combine(ThunderstorePackage.Icon)), ReadEntryBytes(zip, "icon.png"));
        Assert.Equal("fake plugin", ReadEntry(zip, "plugins/Dons-QOL.dll"));
        Assert.Equal("fake core", ReadEntry(zip, "plugins/Dons-QOL.Core.dll"));
    }

    // GW-I-851
    [Fact]
    public void PackageThunderstoreIsOffByDefault()
    {
        DotnetResult result = DotnetProcess.Msbuild(
            RepoRoot.Combine(ContractPaths.PluginProject),
            ["-getProperty:" + ThunderstorePackage.PropertyName]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("false", result.Output.Trim());
    }

    // GW-I-851
    [Fact]
    public void PackageTargetWithoutThePropertyWritesNothing()
    {
        using var gameFolder = new TempFolder();
        using var outputFolder = new TempFolder();
        using var distFolder = new TempFolder();
        using var objFolder = new TempFolder();
        WriteFakeOutputs(outputFolder);
        string distRoot = distFolder.Combine("dist");

        DotnetResult result = Package(gameFolder, outputFolder, distFolder, objFolder, packageProperty: false, distSubfolder: "dist");

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("BS1004", result.Output);
        Assert.False(Directory.Exists(distRoot), "The dist folder must not be created without the property.");
        Assert.Empty(Listing(objFolder.Root));
        Assert.Empty(Listing(gameFolder.Root));
    }

    // GW-I-852
    [Fact]
    public void RealReleaseBuildWithThePropertyProducesTheZipWithPePlugins()
    {
        using var distFolder = new TempFolder();
        string version = ThunderstorePackage.ExpectedVersion();

        DotnetResult result = DotnetProcess.Run(
            [
                "build",
                RepoRoot.Combine(ContractPaths.PluginProject),
                "-c", "Release",
                "--nologo",
                "-nodeReuse:false",
                "-p:" + ThunderstorePackage.PropertyName + "=true",
                "-p:BaseStorageDistDir=" + distFolder.Root + Path.DirectorySeparatorChar,
            ],
            RepoRoot.FullPath);

        Assert.True(result.ExitCode == 0, "dotnet build with PackageThunderstore=true failed:" + Environment.NewLine + Tail(result.Output));
        string zipPath = distFolder.Combine(ThunderstorePackage.ZipFileName(version));
        Assert.True(File.Exists(zipPath), $"{zipPath} was not produced.");

        using ZipArchive zip = ZipFile.OpenRead(zipPath);
        Assert.Equal(ThunderstorePackage.ZipEntries, FileEntries(zip));
        ThunderstorePackage.AssertManifest(ReadEntry(zip, "manifest.json"), version);
        foreach (string dll in new[] { "plugins/Dons-QOL.dll", "plugins/Dons-QOL.Core.dll" })
        {
            byte[] bytes = ReadEntryBytes(zip, dll);
            Assert.True(bytes.Length > 1024, dll + " is too small to be a PE file.");
            Assert.Equal((byte)'M', bytes[0]);
            Assert.Equal((byte)'Z', bytes[1]);
        }
    }

    private static DotnetResult Package(
        TempFolder gameFolder,
        TempFolder outputFolder,
        TempFolder distFolder,
        TempFolder objFolder,
        bool packageProperty,
        string? distSubfolder = null)
    {
        string dist = distSubfolder is null ? distFolder.Root : distFolder.Combine(distSubfolder);
        var arguments = new List<string>
        {
            "-t:" + ThunderstorePackage.TargetName,
            "-p:Configuration=Release",
            "-p:ValheimDir=" + gameFolder.Root,
            "-p:OutDir=" + outputFolder.Root + Path.DirectorySeparatorChar,
            "-p:IntermediateOutputPath=" + objFolder.Root + Path.DirectorySeparatorChar,
            "-p:BaseStorageDistDir=" + dist + Path.DirectorySeparatorChar,
        };
        if (packageProperty)
        {
            arguments.Add("-p:" + ThunderstorePackage.PropertyName + "=true");
        }

        return DotnetProcess.Msbuild(RepoRoot.Combine(ContractPaths.PluginProject), arguments);
    }

    private static void WriteFakeOutputs(TempFolder outputFolder)
    {
        File.WriteAllText(outputFolder.Combine("Dons-QOL.dll"), "fake plugin");
        File.WriteAllText(outputFolder.Combine("Dons-QOL.Core.dll"), "fake core");
    }

    private static string[] Listing(string root) =>
        Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories)
            .Select(entry => Path.GetRelativePath(root, entry))
            .OrderBy(entry => entry, StringComparer.Ordinal)
            .ToArray();

    private static string[] FileEntries(ZipArchive zip) =>
        zip.Entries
            .Select(entry => entry.FullName)
            .Where(name => !name.EndsWith('/'))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static byte[] ReadEntryBytes(ZipArchive zip, string name)
    {
        ZipArchiveEntry? entry = zip.GetEntry(name);
        Assert.True(entry is not null, $"Zip entry is missing: {name}");
        using Stream stream = entry!.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string ReadEntry(ZipArchive zip, string name) =>
        Encoding.UTF8.GetString(ReadEntryBytes(zip, name));

    private static string Tail(string output) =>
        string.Join(Environment.NewLine, output.Split('\n').TakeLast(25));
}
