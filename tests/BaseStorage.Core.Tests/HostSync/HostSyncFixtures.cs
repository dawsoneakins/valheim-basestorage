namespace BaseStorage.Core.Tests.HostSync;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// Fixtures copied from contract host-sync-contracts.md sections 2.2, 4.3 and 5.2 (ADR-GWIN-084). Expected texts are literals from
/// the contract; the "every value changed" source is derived from the catalog so a new host-synced row is covered automatically.
/// </summary>
internal static class HostSyncFixtures
{
    /// <summary>The contract 4.1 payload tag.</summary>
    public const string Tag = "BSC1;";

    /// <summary>Contract 4.3: the payload text of the defaults with empty category text.</summary>
    public const string DefaultsText = "BSC1;CraftFromBase=true\nExcludeVehicles=true\nKeepChestsCraftable=true\nMaxBaseStations=64\n"
        + "RequireSameVersion=true\nAutoAssignThresholdPct=50\nOrganizeLockSeconds=30\nReserveTimeoutMs=1500\nAutoAssignMixedChests=true\n@0\n";

    /// <summary>Contract 4.3: the category text of D2 (23 code units).</summary>
    public const string CategoryText = "# cats\nWood=Wood,Stone\n";

    /// <summary>Contract 4.3: the payload text of D2.</summary>
    public const string CategoryPayloadText = "BSC1;CraftFromBase=true\nExcludeVehicles=true\nKeepChestsCraftable=true\nMaxBaseStations=64\n"
        + "RequireSameVersion=true\nAutoAssignThresholdPct=50\nOrganizeLockSeconds=30\nReserveTimeoutMs=1500\nAutoAssignMixedChests=true\n@23\n# cats\nWood=Wood,Stone\n";

    /// <summary>The defaults payload D: an empty source and empty category text.</summary>
    public static HostSyncPayload Defaults() => HostSyncPayload.Capture(new FakeSettingsSource(), string.Empty);

    /// <summary>The payload D2: the defaults with <see cref="CategoryText"/>.</summary>
    public static HostSyncPayload WithCategories() => new HostSyncPayload(Defaults().Settings, CategoryText);

    /// <summary>A source whose value differs from the default for every host-synced row, derived from the catalog by kind.</summary>
    public static FakeSettingsSource ChangedSource()
    {
        var source = new FakeSettingsSource();
        foreach (SettingDefinition row in SettingsCatalog.HostSynced)
        {
            switch (row.Kind)
            {
                case SettingKind.Bool:
                    source.WithBool(row.Key, !bool.Parse(row.DefaultText));
                    break;
                case SettingKind.Int:
                    source.WithInt(row.Key, int.Parse(row.DefaultText, CultureInfo.InvariantCulture) + 1);
                    break;
                case SettingKind.ItemList:
                    source.WithText(row.Key, "Zed,Alpha");
                    break;
                default:
                    source.WithText(row.Key, "F9");
                    break;
            }
        }

        return source;
    }

    /// <summary>The payload with every value changed and <see cref="CategoryText"/>.</summary>
    public static HostSyncPayload Changed() => HostSyncPayload.Capture(ChangedSource(), CategoryText);

    /// <summary>Builds one (key, value) pair.</summary>
    public static KeyValuePair<string, string> Pair(string key, string value) => new KeyValuePair<string, string>(key, value);

    /// <summary>The settings of a payload with one value replaced.</summary>
    public static IEnumerable<KeyValuePair<string, string>> With(HostSyncPayload payload, string key, string value) =>
        payload.Settings.Select(pair => pair.Key == key ? Pair(key, value) : pair);

    /// <summary>Replaces the <c>KEY=...\n</c> line of a payload text with another line (with its LF), or removes it when the replacement is null.</summary>
    public static string ReplaceLine(string text, string key, string? replacement)
    {
        int start = text.IndexOf(key + "=", StringComparison.Ordinal);
        if (start < 0)
        {
            throw new ArgumentException("The key is not in the text.", nameof(key));
        }

        int end = text.IndexOf('\n', start) + 1;
        return text.Substring(0, start) + (replacement ?? string.Empty) + text.Substring(end);
    }

    /// <summary>Contract 4.3: payload texts that decode to null, each with a reason.</summary>
    public static IEnumerable<(string Reason, string Text)> MalformedTexts()
    {
        string d = DefaultsText;
        string d2 = CategoryPayloadText;
        string firstLine = "CraftFromBase=true\n";

        yield return ("empty", string.Empty);
        yield return ("tag only", "BSC1;");
        yield return ("version payload", "BSV1;0.1.0");
        yield return ("lowercase tag", "bsc1;" + d.Substring(Tag.Length));
        yield return ("other tag", "BSC2;" + d.Substring(Tag.Length));
        yield return ("tag then LF", "BSC1;\n" + d.Substring(Tag.Length));
        yield return ("missing key", ReplaceLine(d, "MaxBaseStations", null));
        yield return ("non-synced key", ReplaceLine(d, "MaxBaseStations", "MaxBaseStations=64\nBaseCacheSeconds=2\n"));
        yield return ("duplicate", Tag + firstLine + d.Substring(Tag.Length));
        yield return ("swapped order", Tag + "ExcludeVehicles=true\nCraftFromBase=true\n" + d.Substring(Tag.Length + firstLine.Length + "ExcludeVehicles=true\n".Length));
        yield return ("unknown key", ReplaceLine(d, "MaxBaseStations", "Nope=1\n"));
        yield return ("no equals", ReplaceLine(d, "MaxBaseStations", "MaxBaseStations\n"));
        yield return ("key with space", ReplaceLine(d, "MaxBaseStations", "MaxBaseStations =64\n"));
        yield return ("int leading zero", ReplaceLine(d, "MaxBaseStations", "MaxBaseStations=064\n"));
        yield return ("int decimal", ReplaceLine(d, "MaxBaseStations", "MaxBaseStations=1.5\n"));
        yield return ("int empty", ReplaceLine(d, "MaxBaseStations", "MaxBaseStations=\n"));
        yield return ("int plus", ReplaceLine(d, "MaxBaseStations", "MaxBaseStations=+64\n"));
        yield return ("int overflow", ReplaceLine(d, "MaxBaseStations", "MaxBaseStations=99999999999\n"));
        yield return ("int negative zero", ReplaceLine(d, "MaxBaseStations", "MaxBaseStations=-0\n"));
        yield return ("bool capitalised", ReplaceLine(d, "CraftFromBase", "CraftFromBase=True\n"));
        yield return ("bool digit", ReplaceLine(d, "CraftFromBase", "CraftFromBase=1\n"));
        yield return ("bool empty", ReplaceLine(d, "CraftFromBase", "CraftFromBase=\n"));
        yield return ("CRLF lines", d.Replace("\n", "\r\n"));
        yield return ("CR in text", d2.Replace("# cats\n", "# cats\r"));
        yield return ("no length line", d.Substring(0, d.Length - "@0\n".Length));
        yield return ("bare at", d.Substring(0, d.Length - "0\n".Length));
        yield return ("length leading zero", d.Substring(0, d.Length - "0\n".Length) + "00\n");
        yield return ("length negative", d.Substring(0, d.Length - "0\n".Length) + "-1\n");
        yield return ("length letter", d.Substring(0, d.Length - "0\n".Length) + "x\n");
        yield return ("length space", d.Substring(0, d.Length - "0\n".Length) + " 0\n");
        yield return ("length no LF", d.Substring(0, d.Length - 1));
        yield return ("length twice", d + "@0\n");
        yield return ("length short", d2.Replace("@23\n", "@22\n"));
        yield return ("length long", d2.Replace("@23\n", "@24\n"));
        yield return ("trailing byte", d2 + "x");
        yield return ("truncated", d2.Substring(0, d2.Length - 1));
        yield return ("leading space", " " + d);
        yield return ("trailing LF", d + "\n");
        yield return ("over the cap", new string('A', HostSyncCodec.MaxPayloadLength + 1));
    }

    /// <summary>The malformed texts as xunit rows.</summary>
    public static IEnumerable<object[]> MalformedTextRows() => MalformedTexts().Select(row => new object[] { row.Reason, row.Text });
}
