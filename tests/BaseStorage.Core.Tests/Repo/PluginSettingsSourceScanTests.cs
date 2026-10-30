namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// AC-1, AC-2, AC-5, AC-6, AC-7 (contract 6.1 to 6.3, rules P1 to P4; ADR-GWIN-014/015). Textual scans of the Plugin sources,
/// with comments stripped. The Plugin cannot be loaded here, so GW-I-011 proves the compiled binding separately. Each scan
/// asserts that it read the entry point and the binding file, so an absent file fails and never passes.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginSettingsSourceScanTests
{
    private const string BindPattern = @"\.Bind\s*\(\s*(\w+)\.Section\s*,\s*\1\.Key\s*,[^;]*,\s*\1\.Description\s*\)";
    private const string ConstructorPattern = @"BepInExSettingsSource\s*\(\s*ConfigFile\s+\w+\s*\)";

    private static readonly string[] SectionNames = { "General", "Organize", "Network", "Keys", "Debug" };

    // GW-U-110 (AC-1, contract 6.1)
    [Fact]
    public void TheBindingFileIsAnInternalSealedSourceThatLoopsOverTheCatalogAndTakesAConfigFile()
    {
        string text = BindingText(ScannedSources());

        Assert.Contains("namespace BaseStorage.Plugin.Settings;", text);
        Assert.True(
            Regex.IsMatch(text, @"internal\s+sealed\s+class\s+BepInExSettingsSource\s*:\s*ISettingsSource\b"),
            "The binding file does not declare internal sealed class BepInExSettingsSource : ISettingsSource.");
        Assert.True(
            Regex.IsMatch(text, @"foreach\s*\([^)]*\bin\s+SettingsCatalog\.All\s*\)"),
            "The binding file does not loop over SettingsCatalog.All.");
        Assert.True(Regex.IsMatch(text, ConstructorPattern), "The binding constructor does not take a ConfigFile.");
    }

    // GW-U-110 (P4): the binding file holds no string literal at all, so no key, section or default is written inline.
    [Fact]
    public void TheBindingFileHoldsNoStringLiteral()
    {
        string text = BindingText(ScannedSources());

        Assert.False(text.Contains('"'), "The binding file contains a string literal.");
    }

    // GW-U-110 (P4): no key or section name appears as a quoted literal anywhere in the Plugin.
    [Fact]
    public void NoKeyOrSectionNameAppearsAsAQuotedLiteralInThePlugin()
    {
        Dictionary<string, string> sources = ScannedSources();
        IEnumerable<string> names = Section7Table.Rows.Select(row => row.Key).Concat(SectionNames);
        string pattern = "\"(" + string.Join("|", names.Select(Regex.Escape)) + ")\"";

        Assert.Empty(PluginSourceScan.FilesMatching(sources, pattern));
    }

    // GW-U-110 (contract 6.1): no second ConfigFile is created.
    [Fact]
    public void NoConfigFileIsConstructedInThePlugin()
    {
        Assert.Empty(PluginSourceScan.FilesMatching(ScannedSources(), @"new\s+ConfigFile\s*\("));
    }

    // GW-U-111 (AC-1, contract 6.1): one Bind per kind, each with its own ConfigEntry type and default parse.
    [Fact]
    public void EachKindBindsWithItsOwnConfigEntryTypeAndItsDefaultParse()
    {
        string text = BindingText(ScannedSources());

        Assert.Equal(4, Regex.Matches(text, BindPattern).Count);
        Assert.True(Regex.IsMatch(text, @"bool\.Parse\(\s*\w+\.DefaultText\s*\)"), "Bool does not parse its default with bool.Parse.");
        Assert.True(
            Regex.IsMatch(text, @"int\.Parse\(\s*\w+\.DefaultText\s*,\s*CultureInfo\.InvariantCulture\s*\)"),
            "Int does not parse its default with int.Parse and the invariant culture.");
        Assert.Contains("ConfigEntry<bool>", text);
        Assert.Contains("ConfigEntry<int>", text);
        Assert.Contains("ConfigEntry<string>", text);
        Assert.Contains("ConfigEntry<KeyboardShortcut>", text);
        Assert.Contains(".Value.Serialize()", text);
        Assert.DoesNotContain("AcceptableValue", text);
    }

    // GW-U-111 (AC-2): every Bind passes the catalog Description as its last argument, so no text is written inline.
    [Fact]
    public void EveryBindPassesTheCatalogDescriptionAsItsLastArgument()
    {
        string text = BindingText(ScannedSources());

        int binds = Regex.Matches(text, @"\.Bind\s*\(").Count;
        Assert.Equal(4, binds);
        Assert.Equal(binds, Regex.Matches(text, BindPattern).Count);
    }

    // GW-U-112 (AC-5, P2): KeyboardShortcut.Empty is the one unbound default, inside the Hotkey branch.
    [Fact]
    public void KeyboardShortcutEmptyIsTheUnboundDefaultOfTheHotkeyBranchOnly()
    {
        Dictionary<string, string> sources = ScannedSources();
        string text = BindingText(sources);

        Assert.Single(Regex.Matches(text, @"\bKeyboardShortcut\.Empty\b"));

        int hotkeyCase = text.IndexOf("case SettingKind.Hotkey", StringComparison.Ordinal);
        int empty = text.IndexOf("KeyboardShortcut.Empty", StringComparison.Ordinal);
        Assert.True(hotkeyCase >= 0 && empty > hotkeyCase, "KeyboardShortcut.Empty is not inside the case SettingKind.Hotkey branch.");
        Assert.DoesNotContain("case SettingKind.", text.Substring(hotkeyCase + 1, empty - hotkeyCase - 1));

        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"new\s+KeyboardShortcut\s*\("));
    }

    // GW-U-112 (AC-6, P2): no key is bound. Every KeyCode is None, and ConfigEntry<KeyboardShortcut> lives only in the binding.
    [Fact]
    public void NoKeyIsBoundAnywhereAndTheHotkeyEntryTypeIsOnlyInTheBindingFile()
    {
        Dictionary<string, string> sources = ScannedSources();

        // The favourites click handler reads the Alt modifier (S-E004-014 contract T1, ADR-GWIN-051); it binds no key.
        const string favouriteClicksFile = "src/BaseStorage.Plugin/Favourites/FavouriteClicks.cs";
        Dictionary<string, string> outsideClicks = sources
            .Where(pair => pair.Key != favouriteClicksFile)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        Assert.Equal(0, PluginSourceScan.CountMatches(outsideClicks, @"\bKeyCode\.(?!None\b)\w+"));
        Assert.True(sources.ContainsKey(favouriteClicksFile), $"Scan did not read {favouriteClicksFile}.");
        Assert.Empty(Regex.Matches(sources[favouriteClicksFile], @"\bKeyCode\.(?!None\b|LeftAlt\b|RightAlt\b)\w+"));
        Assert.Equal(new[] { PluginSourceScan.SettingsFile }, PluginSourceScan.FilesMatching(sources, @"ConfigEntry<KeyboardShortcut>"));
    }

    // GW-U-113 (AC-7, P1): the BepInEx config tokens appear only in the binding file.
    [Fact]
    public void BepInExConfigTokensAppearOnlyInTheBindingFile()
    {
        Dictionary<string, string> sources = ScannedSources();
        string[] patterns =
        {
            @"\bConfigEntry\b",
            @"\bConfigEntryBase\b",
            @"\bConfigFile\b",
            @"\bBoxedValue\b",
            @"\.Bind\s*\(",
            @"\bKeyboardShortcut\b",
            @"using\s+BepInEx\.Configuration\s*;",
        };

        foreach (string pattern in patterns)
        {
            string[] files = PluginSourceScan.FilesMatching(sources, pattern);
            Assert.True(
                files.Length == 0 || files.SequenceEqual(new[] { PluginSourceScan.SettingsFile }),
                $"{pattern} appears in: {string.Join(", ", files)}");
        }
    }

    // GW-U-113 (AC-6): KeyboardShortcut is named only in the binding file, so no other file can declare a hotkey setting.
    [Fact]
    public void KeyboardShortcutIsNamedOnlyInTheBindingFile()
    {
        string[] files = PluginSourceScan.FilesMatching(ScannedSources(), @"\bKeyboardShortcut\b");

        Assert.True(
            files.Length == 0 || files.SequenceEqual(new[] { PluginSourceScan.SettingsFile }),
            $"KeyboardShortcut appears in: {string.Join(", ", files)}");
    }

    // GW-U-114 (AC-7, P3): one construction of the binding and of the settings, both in the entry point, and no Config access elsewhere.
    [Fact]
    public void TheBindingAndTheSettingsAreConstructedOnceInTheEntryPoint()
    {
        Dictionary<string, string> sources = ScannedSources();
        string entry = sources[PluginSourceScan.EntryPoint];

        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"new\s+BepInExSettingsSource\s*\(\s*Config\s*\)"));
        Assert.Equal(new[] { PluginSourceScan.EntryPoint }, PluginSourceScan.FilesMatching(sources, @"new\s+BepInExSettingsSource\s*\(\s*Config\s*\)"));

        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"new\s+BaseStorageSettings\s*\("));
        Assert.Equal(new[] { PluginSourceScan.EntryPoint }, PluginSourceScan.FilesMatching(sources, @"new\s+BaseStorageSettings\s*\("));

        Assert.Single(Regex.Matches(entry, @"\bConfig\b"));
        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"\bConfig\s*[.\[]"));
    }

    // GW-U-115 (AC-7, contract 6.3): Boot binds the log sink, then the settings, then Harmony, and Settings is assigned once.
    [Fact]
    public void BootBindsSettingsAfterTheLogSinkAndBeforeHarmony()
    {
        string entry = ScannedSources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");

        int sink = boot.IndexOf("new BepInExLogSink(Logger)", StringComparison.Ordinal);
        int settingsSource = boot.IndexOf("new BepInExSettingsSource(Config)", StringComparison.Ordinal);
        int settings = boot.IndexOf("new BaseStorageSettings(", StringComparison.Ordinal);
        int harmony = boot.IndexOf("new Harmony(PluginGuid)", StringComparison.Ordinal);
        Assert.True(sink >= 0, "Boot does not construct the log sink.");
        Assert.True(settingsSource > sink, "Boot does not bind the settings source after the log sink.");
        Assert.True(settings > settingsSource, "Boot does not construct BaseStorageSettings after the settings source.");
        Assert.True(harmony > settings, "Boot does not create Harmony after the settings.");

        Assert.True(
            Regex.IsMatch(entry, @"internal\s+static\s+BaseStorageSettings\?\s+Settings\s*\{\s*get;\s*private\s+set;\s*\}"),
            "BaseStoragePlugin does not declare internal static BaseStorageSettings? Settings { get; private set; }.");
        Assert.Single(Regex.Matches(entry, @"\bSettings\s*=(?!=)"));
        Assert.Contains("Settings =", boot);
    }

    // GW-U-116 (AC-7, contract 5.1): the three TryGet methods never throw, and a null key returns false.
    [Fact]
    public void TheTryGetMethodsNeverThrowAndReturnFalseForANullKey()
    {
        string text = BindingText(ScannedSources());

        foreach (string signature in new[] { @"bool\s+TryGetBool\s*\(", @"bool\s+TryGetInt\s*\(", @"bool\s+TryGetText\s*\(" })
        {
            string body = PluginSourceScan.MethodBody(text, signature);

            Assert.False(Regex.IsMatch(body, @"\bthrow\b"), signature + " throws after construction.");
            Assert.True(Regex.IsMatch(body, @"key\s*(==\s*null|is\s+null)"), signature + " does not test the key for null.");
            Assert.True(Regex.IsMatch(body, @"return\s+false\s*;"), signature + " does not return false for a null key.");
        }

        string constructorBody = PluginSourceScan.MethodBody(text, ConstructorPattern);
        Assert.Equal(
            Regex.Matches(text, @"\bthrow\b").Count,
            Regex.Matches(constructorBody, @"\bthrow\b").Count);
    }

    // GW-U-110 setup helper: scans the Plugin and checks that the two files every settings scan needs were read.
    private static Dictionary<string, string> ScannedSources()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.True(sources.ContainsKey(PluginSourceScan.EntryPoint), $"Scan did not read {PluginSourceScan.EntryPoint}.");
        Assert.True(sources.ContainsKey(PluginSourceScan.SettingsFile), $"Scan did not read {PluginSourceScan.SettingsFile}.");
        return sources;
    }

    private static string BindingText(Dictionary<string, string> sources) => sources[PluginSourceScan.SettingsFile];
}
