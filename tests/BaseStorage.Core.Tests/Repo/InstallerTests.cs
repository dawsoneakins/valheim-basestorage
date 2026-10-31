namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

/// <summary>
/// S-D-001 and S-D-002: runs the real scripts under Windows PowerShell against a fake game folder and a fake BepInEx zip. No network,
/// and the real game folder is never used.
/// </summary>
public sealed class InstallerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "bs-installer-" + Guid.NewGuid().ToString("N"));
    private readonly string game;
    private readonly string kit;
    private readonly string packZip;

    public InstallerTests()
    {
        game = Path.Combine(root, "Steam", "steamapps", "common", "Valheim");
        kit = Path.Combine(root, "kit");
        Directory.CreateDirectory(game);
        Directory.CreateDirectory(Path.Combine(kit, "plugin"));
        File.WriteAllText(Path.Combine(game, "valheim.exe"), "fake");
        File.WriteAllText(Path.Combine(kit, "plugin", "BaseStorage.dll"), "new plugin");
        File.WriteAllText(Path.Combine(kit, "plugin", "BaseStorage.Core.dll"), "new core");
        File.Copy(RepoRoot.Combine(@"installer\install.ps1"), Path.Combine(kit, "install.ps1"));
        File.Copy(RepoRoot.Combine(@"installer\uninstall.ps1"), Path.Combine(kit, "uninstall.ps1"));

        packZip = Path.Combine(root, "pack.zip");
        using ZipArchive zip = ZipFile.Open(packZip, ZipArchiveMode.Create);
        Add(zip, "BepInExPack_Valheim/winhttp.dll", "doorstop");
        Add(zip, "BepInExPack_Valheim/BepInEx/core/BepInEx.dll", "bepinex");
        Add(zip, "manifest.json", "{}");
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void AFreshInstallAddsBepInExAndThePlugin()
    {
        (int code, string output) = Install();

        Assert.True(code == 0, output);
        Assert.True(File.Exists(Path.Combine(game, "winhttp.dll")));
        Assert.True(File.Exists(Path.Combine(game, "BepInEx", "core", "BepInEx.dll")));
        Assert.Equal("new plugin", File.ReadAllText(Plugin("BaseStorage.dll")));
        Assert.False(File.Exists(Path.Combine(game, "manifest.json")));
    }

    [Fact]
    public void WithoutAPluginFolderTheModIsFetchedAndChecked()
    {
        Directory.Delete(Path.Combine(kit, "plugin"), true);
        string modZip = ModZip();

        (int code, string output) = Run("install.ps1", $"-GameDir \"{game}\" -PackZip \"{packZip}\" -ExpectedHash {Hash(packZip)} -ModZip \"{modZip}\" -ModHash {Hash(modZip)} -NoPrompt");

        Assert.True(code == 0, output);
        Assert.Equal("zipped plugin", File.ReadAllText(Plugin("BaseStorage.dll")));
    }

    [Fact]
    public void AWrongModHashChangesNothingInTheGameFolder()
    {
        Directory.Delete(Path.Combine(kit, "plugin"), true);

        (int code, string output) = Run("install.ps1", $"-GameDir \"{game}\" -PackZip \"{packZip}\" -ExpectedHash {Hash(packZip)} -ModZip \"{ModZip()}\" -ModHash DEADBEEF -NoPrompt");

        Assert.Equal(1, code);
        Assert.Contains("does not match", output, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(game, "BepInEx")));
    }

    [Fact]
    public void AWrongHashInstallsNothing()
    {
        (int code, string output) = Run("install.ps1", $"-GameDir \"{game}\" -PackZip \"{packZip}\" -ExpectedHash DEADBEEF -NoPrompt");

        Assert.Equal(1, code);
        Assert.Contains("does not match", output, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(game, "winhttp.dll")));
        Assert.False(Directory.Exists(Path.Combine(game, "BepInEx", "plugins")));
    }

    [Fact]
    public void RunningTwiceBacksUpWhatItReplaces()
    {
        Install();
        File.WriteAllText(Plugin("BaseStorage.dll"), "old plugin");

        (int code, string _) = Install();

        Assert.Equal(0, code);
        Assert.Equal("new plugin", File.ReadAllText(Plugin("BaseStorage.dll")));
        string backup = Assert.Single(Directory.GetDirectories(Path.Combine(game, "BepInEx"), "BaseStorage-backup-*"));
        Assert.Equal("old plugin", File.ReadAllText(Path.Combine(backup, "BaseStorage.dll")));
    }

    [Fact]
    public void ALibraryFolderIsFoundThroughSteam()
    {
        string steam = Path.Combine(root, "OtherSteam");
        string library = Path.Combine(steam, "steamapps");
        Directory.CreateDirectory(library);
        string escaped = Path.Combine(root, "Steam").Replace(@"\", @"\\", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(library, "libraryfolders.vdf"), "\"libraryfolders\"\n{\n\t\"0\"\n\t{\n\t\t\"path\"\t\t\"" + escaped + "\"\n\t}\n}\n");

        (int code, string output) = Run("install.ps1", $"-SteamRoot \"{steam}\" -PackZip \"{packZip}\" -ExpectedHash {Hash(packZip)} -NoPrompt");

        Assert.Equal(0, code);
        Assert.Contains(game, output.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ALibraryOnAMissingDriveIsSkipped()
    {
        string steam = SteamListing(MissingDrive() + @":\\OldLibrary", Path.Combine(root, "Steam").Replace(@"\", @"\\", StringComparison.Ordinal));

        (int code, string output) = Run("install.ps1", $"-SteamRoot \"{steam}\" -PackZip \"{packZip}\" -ExpectedHash {Hash(packZip)} -NoPrompt");

        Assert.True(code == 0, output);
        Assert.Equal("new plugin", File.ReadAllText(Plugin("BaseStorage.dll")));
    }

    [Fact]
    public void ASteamRootOnAMissingDriveEndsInTheNormalMessage()
    {
        (int code, string output) = Run("install.ps1", $"-SteamRoot \"{MissingDrive()}:\\Steam\" -PackZip \"{packZip}\" -ExpectedHash {Hash(packZip)} -NoPrompt");

        Assert.Equal(1, code);
        Assert.Contains("could not find Valheim", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Join-Path", output, StringComparison.Ordinal);
    }

    [Fact]
    public void UninstallSkipsALibraryOnAMissingDrive()
    {
        Install();
        string steam = SteamListing(MissingDrive() + @":\\OldLibrary", Path.Combine(root, "Steam").Replace(@"\", @"\\", StringComparison.Ordinal));

        (int code, string output) = Run("uninstall.ps1", $"-SteamRoot \"{steam}\" -NoPrompt");

        Assert.True(code == 0, output);
        Assert.False(File.Exists(Plugin("BaseStorage.dll")));
    }

    [Fact]
    public void UninstallRemovesThePluginAndLeavesBepInExUnlessAsked()
    {
        Install();

        (int code, string _) = Run("uninstall.ps1", $"-GameDir \"{game}\" -NoPrompt");

        Assert.Equal(0, code);
        Assert.False(File.Exists(Plugin("BaseStorage.dll")));
        Assert.True(File.Exists(Path.Combine(game, "winhttp.dll")));
    }

    [Fact]
    public void UninstallCanRemoveBepInExButKeepsTheConfig()
    {
        Install();
        Directory.CreateDirectory(Path.Combine(game, "BepInEx", "config"));
        File.WriteAllText(Path.Combine(game, "BepInEx", "config", "keep.cfg"), "mine");

        (int code, string _) = Run("uninstall.ps1", $"-GameDir \"{game}\" -NoPrompt -RemoveBepInEx");

        Assert.Equal(0, code);
        Assert.False(File.Exists(Path.Combine(game, "winhttp.dll")));
        Assert.False(File.Exists(Path.Combine(game, "BepInEx", "core", "BepInEx.dll")));
        Assert.True(File.Exists(Path.Combine(game, "BepInEx", "config", "keep.cfg")));
    }

    [Theory]
    [InlineData(@"installer\install.ps1")]
    [InlineData(@"installer\uninstall.ps1")]
    public void TheScriptsNeverNameTheSaveFolders(string relative)
    {
        string text = File.ReadAllText(RepoRoot.Combine(relative));

        Assert.DoesNotContain("IronGate", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("worlds_local", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userdata", text, StringComparison.OrdinalIgnoreCase);
    }

    private static void Add(ZipArchive zip, string name, string text)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open());
        writer.Write(text);
    }

    private static char MissingDrive()
    {
        for (char letter = 'Z'; letter >= 'D'; letter--)
        {
            if (!Directory.Exists(letter + @":\"))
            {
                return letter;
            }
        }

        throw new InvalidOperationException("every drive letter is in use");
    }

    private string SteamListing(params string[] escapedPaths)
    {
        string steam = Path.Combine(root, "OtherSteam");
        string library = Path.Combine(steam, "steamapps");
        Directory.CreateDirectory(library);
        string body = string.Empty;
        for (int i = 0; i < escapedPaths.Length; i++)
        {
            body += "\t\"" + i + "\"\n\t{\n\t\t\"path\"\t\t\"" + escapedPaths[i] + "\"\n\t}\n";
        }

        File.WriteAllText(Path.Combine(library, "libraryfolders.vdf"), "\"libraryfolders\"\n{\n" + body + "}\n");
        return steam;
    }

    private string ModZip()
    {
        string path = Path.Combine(root, "mod.zip");
        using ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create);
        Add(zip, "BaseStorage.dll", "zipped plugin");
        Add(zip, "BaseStorage.Core.dll", "zipped core");
        return path;
    }

    private static string Hash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private string Plugin(string name) => Path.Combine(game, "BepInEx", "plugins", "BaseStorage", name);

    private (int Code, string Output) Install() =>
        Run("install.ps1", $"-GameDir \"{game}\" -PackZip \"{packZip}\" -ExpectedHash {Hash(packZip)} -NoPrompt");

    private (int Code, string Output) Run(string script, string arguments)
    {
        var start = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{Path.Combine(kit, script)}\" {arguments} -IgnoreRunningGame")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using Process process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output + error.GetAwaiter().GetResult());
    }
}
