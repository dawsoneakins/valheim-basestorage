namespace BaseStorage.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Repo;

/// <summary>GW-U-2555, 2556 and 2562: the RestockKey setting, its catalog row, and the absence of RestockFromKeep (S-QR-002 contract 7, AC-3 and AC-5).</summary>
public sealed class RestockKeySettingTests
{
    private const string Catalog = "src/BaseStorage.Core/Settings/SettingsCatalog.cs";

    /// <summary>GW-U-2555: the catalog row is last, index 20, unbound and client-only, with the contract description; the key is the last SettingKeys constant; the catalog docs say 21 entries.</summary>
    [Fact]
    public void RestockKeyIsTheLastUnboundClientHotkeyRow()
    {
        Assert.Equal(23, SettingsCatalog.All.Count);
        SettingDefinition row = SettingsCatalog.All[20];
        Assert.Equal(SettingSections.Keys, row.Section);
        Assert.Equal("RestockKey", row.Key);
        Assert.Equal(SettingKind.Hotkey, row.Kind);
        Assert.Equal(string.Empty, row.DefaultText);
        Assert.False(row.HostSynced);
        Assert.Equal("Hotkey that starts Restock for the base that contains the player. Unbound by default.", row.Description);

        Assert.Equal("RestockKey", (string?)typeof(SettingKeys).GetField("RestockKey")?.GetRawConstantValue());
        FieldInfo[] constants = typeof(SettingKeys).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral)
            .ToArray();
        Assert.Equal("RestockKey", constants[constants.Length - 1].Name);
        Assert.Contains("21 entries", RepoRoot.ReadText(Catalog), StringComparison.Ordinal);
    }

    /// <summary>GW-U-2556: RestockKey is empty by default and unbound; a source text parses through HotkeyChord; the property is get-only and documented.</summary>
    [Fact]
    public void RestockKeyIsUnboundByDefaultAndParsesThroughHotkeyChord()
    {
        PropertyInfo? property = typeof(BaseStorageSettings).GetProperty("RestockKey");
        Assert.NotNull(property);
        var settings = new BaseStorageSettings(new BaseStorage.Core.Tests.Settings.FakeSettingsSource());
        string? unbound = (string?)property!.GetValue(settings);
        Assert.Equal(string.Empty, unbound);
        Assert.False(HotkeyChord.Parse(unbound).IsBound);

        // Edited by S-QR-002 (ADR-GWIN-176): HotkeyChord.Parse takes the first token as the main key.
        var bound = new BaseStorageSettings(new BaseStorage.Core.Tests.Settings.FakeSettingsSource().WithText("RestockKey", "R + LeftShift"));
        string? text = (string?)property.GetValue(bound);
        Assert.Equal("R + LeftShift", text);
        HotkeyChord chord = HotkeyChord.Parse(text);
        Assert.True(chord.IsBound);
        Assert.Equal("R", chord.MainKey);
        Assert.Equal(new[] { "LeftShift" }, chord.Modifiers);

        Assert.False(property.CanWrite);
        Assert.Contains("Gets the RestockKey hotkey text. Empty means unbound.", RepoRoot.ReadText("src/BaseStorage.Core/Settings/BaseStorageSettings.cs"), StringComparison.Ordinal);
    }

    /// <summary>GW-U-2562: no name, key, property, string or table text contains RestockFromKeep, and the source, the README and the playtest file hold no such text.</summary>
    [Fact]
    public void NoRestockFromKeepSettingExists()
    {
        const string Token = "RestockFromKeep";
        var names = new List<string>();
        names.AddRange(typeof(SettingKeys).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => f.Name));
        names.AddRange(typeof(SettingKeys).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => f.GetRawConstantValue()?.ToString() ?? string.Empty));
        names.AddRange(SettingsCatalog.All.Select(d => d.Key));
        names.AddRange(typeof(BaseStorageSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name));
        Assert.DoesNotContain(names, name => name.Contains(Token, StringComparison.Ordinal));

        var hits = new List<string>();
        foreach (string file in Directory.EnumerateFiles(RepoRoot.Combine("src"), "*", SearchOption.AllDirectories)
                     .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                                    && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                                    && (path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".md", StringComparison.Ordinal))))
        {
            if (File.ReadAllText(file).Contains(Token, StringComparison.Ordinal))
            {
                hits.Add(file);
            }
        }

        foreach (string doc in new[] { "package/README.md", "docs/PLAYTEST-v0.4.3.md" })
        {
            if (RepoRoot.Exists(doc) && RepoRoot.ReadText(doc).Contains(Token, StringComparison.Ordinal))
            {
                hits.Add(doc);
            }
        }

        Assert.True(hits.Count == 0, "RestockFromKeep appears in: " + string.Join(", ", hits));
    }
}
