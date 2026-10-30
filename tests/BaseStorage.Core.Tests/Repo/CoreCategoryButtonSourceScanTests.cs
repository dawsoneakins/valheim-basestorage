namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1306 (contract S-E004-004 section 6.1, Core purity): the CategoryButton folder holds exactly the layout file, which names no game
/// or UI type (raw text, comments included), reads no time and does no I/O, imports only System, holds no string literal and no static
/// mutable field. Each scan asserts the file it read, so an empty folder cannot pass.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreCategoryButtonSourceScanTests
{
    private const string Folder = "src/BaseStorage.Core/CategoryButton";
    private const string LayoutFile = "CategoryButtonLayout.cs";

    private static readonly string[] GameTokens =
    {
        "UnityEngine", "BepInEx", "HarmonyLib", "assembly_valheim", "MonoBehaviour", "RectTransform", "Button", "InventoryGui",
        "ZDO", "ZNetView", "Container", "Player",
    };

    private static readonly string[] TimeAndIoTokens =
    {
        "DateTime", "Stopwatch", "TickCount", "Thread", "Task", "Timer", "Sleep", "System.IO", "Console", "lock", "CultureInfo",
    };

    // GW-U-1306: the folder holds exactly the one contracted file.
    [Fact]
    public void ScanReadsExactlyTheLayoutFile()
    {
        Assert.Equal(new[] { LayoutFile }, Files());
    }

    // GW-U-1306: no game or UI token in the raw text, comments included.
    [Fact]
    public void FileHasNoGameTokens()
    {
        string text = RepoRoot.ReadText(Folder + "/" + LayoutFile);
        string[] offenders = GameTokens.Where(token => Regex.IsMatch(text, @"\b" + Regex.Escape(token) + @"\b")).ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-1306: no system-time, threading, culture or I/O token in code, and only System is imported.
    [Fact]
    public void FileReadsNoSystemTimeDoesNoIoAndImportsOnlySystem()
    {
        string code = PluginSourceScan.StripComments(RepoRoot.ReadText(Folder + "/" + LayoutFile));
        string[] offenders = TimeAndIoTokens.Where(token => Regex.IsMatch(code, @"\b" + Regex.Escape(token) + @"\b")).ToArray();
        string[] usings = Regex.Matches(code, @"^\s*using\s+([^;]+);", RegexOptions.Multiline).Select(m => m.Groups[1].Value.Trim()).ToArray();

        Assert.Empty(offenders);
        Assert.Equal(new[] { "System" }, usings);
        Assert.Contains("namespace BaseStorage.Core.CategoryButton;", code, StringComparison.Ordinal);
    }

    // GW-U-1306: no string literal (names go through nameof) and no static mutable field.
    [Fact]
    public void FileHoldsNoStringLiteralAndNoStaticMutableField()
    {
        string code = PluginSourceScan.StripComments(RepoRoot.ReadText(Folder + "/" + LayoutFile));

        Assert.DoesNotContain("\"", code, StringComparison.Ordinal);
        Assert.Contains("nameof(width)", code, StringComparison.Ordinal);
        Assert.Contains("nameof(gap)", code, StringComparison.Ordinal);
        Assert.Contains("nameof(outerX)", code, StringComparison.Ordinal);
        foreach (Match field in Regex.Matches(code, @"^\s*(?:public|internal|private)\s+static\s+(?!class\b)[^;(]*;", RegexOptions.Multiline))
        {
            Assert.True(
                Regex.IsMatch(field.Value, @"\b(const|readonly)\b"),
                "Static field is neither const nor readonly: " + field.Value.Trim());
        }
    }

    private static string[] Files() =>
        Directory.GetFiles(RepoRoot.Combine(Folder), "*.cs").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray()!;
}
