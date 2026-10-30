namespace BaseStorage.Core.Tests.HostSync;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>AC-6.2 (contract 2.1, 2.2; rules H1 to H3): capturing, building, reading and comparing a host-sync payload.</summary>
public sealed class HostSyncPayloadTests
{
    // GW-U-1000 (H1): capturing from an empty source gives every host-synced row with its default text, in catalog order.
    [Fact]
    public void CaptureFromAnEmptySourceGivesEveryHostSyncedDefaultInCatalogOrder()
    {
        HostSyncPayload defaults = HostSyncFixtures.Defaults();

        Assert.Equal(SettingsCatalog.HostSynced.Count, defaults.Settings.Count);
        for (int i = 0; i < defaults.Settings.Count; i++)
        {
            Assert.Equal(SettingsCatalog.HostSynced[i].Key, defaults.Settings[i].Key);
            Assert.Equal(SettingsCatalog.HostSynced[i].DefaultText, defaults.Settings[i].Value);
        }

        Assert.Equal(new[] { "true", "true", "true", "64", "true", "50", "30", "1500", "true" }, defaults.Settings.Select(pair => pair.Value));
        Assert.Equal(string.Empty, defaults.CategoryFileText);
    }

    // GW-U-1000 (H1, H2): capturing reads the source by kind and formats canonically; every changed value differs from its default.
    [Fact]
    public void CaptureReadsEveryHostSyncedValueFromTheSource()
    {
        FakeSettingsSource source = new FakeSettingsSource().WithBool("CraftFromBase", false).WithInt("MaxBaseStations", 3);

        HostSyncPayload payload = HostSyncPayload.Capture(source, "x");

        Assert.Equal("false", payload.ValueText("CraftFromBase"));
        Assert.Equal("3", payload.ValueText("MaxBaseStations"));
        Assert.Equal("true", payload.ValueText("ExcludeVehicles"));
        Assert.Equal("x", payload.CategoryFileText);

        HostSyncPayload changed = HostSyncFixtures.Changed();
        foreach (SettingDefinition row in SettingsCatalog.HostSynced)
        {
            Assert.NotEqual(row.DefaultText, changed.ValueText(row.Key));
            Assert.True(HostSyncValueText.IsCanonical(row.Kind, changed.ValueText(row.Key)), row.Key + " is not canonical.");
        }
    }

    // GW-U-1000 (H1): capturing never calls a setter on the source and only reads host-synced keys with the getter of their kind.
    [Fact]
    public void CaptureOnlyReadsTheSource()
    {
        var source = new FakeSettingsSource();
        int bools = SettingsCatalog.HostSynced.Count(row => row.Kind == SettingKind.Bool);
        int ints = SettingsCatalog.HostSynced.Count(row => row.Kind == SettingKind.Int);
        int texts = SettingsCatalog.HostSynced.Count - bools - ints;

        HostSyncPayload.Capture(source, string.Empty);

        Assert.Equal(bools, source.BoolCalls);
        Assert.Equal(ints, source.IntCalls);
        Assert.Equal(texts, source.TextCalls);
    }

    // GW-U-1000 (R8): Capture validates its arguments.
    [Fact]
    public void CaptureValidatesArguments()
    {
        Assert.Equal("source", Assert.Throws<ArgumentNullException>(() => HostSyncPayload.Capture(null!, string.Empty)).ParamName);
        Assert.Equal("categoryFileText", Assert.Throws<ArgumentNullException>(() => HostSyncPayload.Capture(new FakeSettingsSource(), null!)).ParamName);
    }

    // GW-U-1000 (2.2): ValueText and TryGetValueText answer host-synced keys only.
    [Fact]
    public void ValueTextAnswersHostSyncedKeysOnly()
    {
        HostSyncPayload defaults = HostSyncFixtures.Defaults();

        Assert.Equal("64", defaults.ValueText("MaxBaseStations"));
        Assert.True(defaults.TryGetValueText("MaxBaseStations", out string text));
        Assert.Equal("64", text);

        Assert.False(defaults.TryGetValueText("BaseCacheSeconds", out string missing));
        Assert.Equal(string.Empty, missing);
        Assert.False(defaults.TryGetValueText(null!, out _));
        Assert.False(defaults.TryGetValueText(string.Empty, out _));
        Assert.False(defaults.TryGetValueText("maxbasestations", out _));

        Assert.Equal("key", Assert.Throws<ArgumentException>(() => defaults.ValueText("BaseCacheSeconds")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => defaults.ValueText("Nope")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => defaults.ValueText(null!)).ParamName);
    }

    // GW-U-1001 (H3): the constructor normalises CRLF and lone CR to LF and keeps everything else byte-exact.
    [Theory]
    [InlineData("a\r\nb\rc\n", "a\nb\nc\n")]
    [InlineData("\r\n", "\n")]
    [InlineData("\r", "\n")]
    [InlineData("\r\r\n", "\n\n")]
    [InlineData("", "")]
    [InlineData("﻿# x\t\n\nWood=Wood", "﻿# x\t\n\nWood=Wood")]
    [InlineData("ö﻿\t", "ö﻿\t")]
    [InlineData("no newline", "no newline")]
    public void ConstructorNormalisesLineEndingsToLf(string input, string expected)
    {
        var payload = new HostSyncPayload(HostSyncFixtures.Defaults().Settings, input);

        Assert.Equal(expected, payload.CategoryFileText);
        Assert.DoesNotContain("\r", payload.CategoryFileText, StringComparison.Ordinal);
    }

    // GW-U-1001 (2.2): any input order is accepted and the stored order is the catalog order.
    [Fact]
    public void ConstructorAcceptsAnyOrderAndStoresCatalogOrder()
    {
        HostSyncPayload defaults = HostSyncFixtures.Defaults();

        var reversed = new HostSyncPayload(defaults.Settings.Reverse(), string.Empty);

        Assert.Equal(defaults, reversed);
        Assert.Equal(defaults.Settings.Select(pair => pair.Key), reversed.Settings.Select(pair => pair.Key));
    }

    // GW-U-1001 (R8): the constructor rejects null arguments with the parameter name.
    [Fact]
    public void ConstructorRejectsNullArguments()
    {
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new HostSyncPayload(null!, string.Empty)).ParamName);
        Assert.Equal("categoryFileText", Assert.Throws<ArgumentNullException>(() => new HostSyncPayload(HostSyncFixtures.Defaults().Settings, null!)).ParamName);
    }

    // GW-U-1001 (R8, H1, H2): missing, extra, duplicate, non-synced, null and non-canonical entries throw ArgumentException("settings").
    [Theory]
    [MemberData(nameof(InvalidSettingsRows))]
    public void ConstructorRejectsInvalidSettings(string reason, IEnumerable<KeyValuePair<string, string>> settings)
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new HostSyncPayload(settings, string.Empty));

        Assert.Equal("settings", error.ParamName);
        Assert.NotEmpty(reason);
    }

    public static IEnumerable<object[]> InvalidSettingsRows()
    {
        IReadOnlyList<KeyValuePair<string, string>> defaults = HostSyncFixtures.Defaults().Settings;
        HostSyncPayload payload = HostSyncFixtures.Defaults();

        yield return ["missing key", defaults.Take(defaults.Count - 1)];
        yield return ["empty", Array.Empty<KeyValuePair<string, string>>()];
        yield return ["non-synced key", defaults.Concat([HostSyncFixtures.Pair("BaseCacheSeconds", "2")])];
        yield return ["unknown key", defaults.Concat([HostSyncFixtures.Pair("Nope", "1")])];
        yield return ["duplicate", defaults.Concat([HostSyncFixtures.Pair("CraftFromBase", "true")])];
        yield return ["duplicate with other value", defaults.Concat([HostSyncFixtures.Pair("CraftFromBase", "false")])];
        yield return ["null key", defaults.Take(defaults.Count - 1).Concat([new KeyValuePair<string, string>(null!, "true")])];
        yield return ["null value", HostSyncFixtures.With(payload, "CraftFromBase", null!)];
        yield return ["int leading zero", HostSyncFixtures.With(payload, "MaxBaseStations", "064")];
        yield return ["bool capitalised", HostSyncFixtures.With(payload, "CraftFromBase", "True")];
        yield return ["bool digit", HostSyncFixtures.With(payload, "CraftFromBase", "1")];
        yield return ["int blank", HostSyncFixtures.With(payload, "MaxBaseStations", "")];
        yield return ["int with LF", HostSyncFixtures.With(payload, "MaxBaseStations", "64\n")];
        yield return ["case of key", defaults.Take(defaults.Count - 1).Concat([HostSyncFixtures.Pair("reservetimeoutms", "1500")])];
    }

    // GW-U-1002 (R5, R6): value equality over settings and category text, with consistent hash codes and null handling.
    [Fact]
    public void EqualityIsByValue()
    {
        HostSyncPayload a = HostSyncFixtures.Defaults();
        HostSyncPayload b = HostSyncPayload.Capture(new FakeSettingsSource(), string.Empty);
        var differentText = new HostSyncPayload(a.Settings, "x");
        var differentValue = new HostSyncPayload(HostSyncFixtures.With(a, "MaxBaseStations", "65"), string.Empty);

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        Assert.False(a.Equals(differentText));
        Assert.True(a != differentText);
        Assert.False(a == differentValue);
        Assert.NotEqual(a, differentValue);

        Assert.False(a.Equals(null));
        Assert.False(a.Equals((object?)null));
        Assert.False(a.Equals("BSC1;"));
        Assert.True((HostSyncPayload?)null == (HostSyncPayload?)null);
        Assert.False(a == null);
        Assert.False(null == a);
        Assert.True(a != null);
    }

    // GW-U-1002 (R4): every public property is get-only, every instance field is readonly, and Settings is read-only.
    [Fact]
    public void PayloadIsImmutable()
    {
        Type type = typeof(HostSyncPayload);

        Assert.True(type.IsSealed);
        Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance), property => Assert.Null(property.SetMethod));
        Assert.All(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), field => Assert.True(field.IsInitOnly, field.Name + " is not readonly."));

        HostSyncPayload payload = HostSyncFixtures.Defaults();
        Assert.True(payload.Settings is not IList<KeyValuePair<string, string>> list || list.IsReadOnly, "Settings is a writable list.");
        Assert.False(string.IsNullOrEmpty(payload.ToString()));
    }
}
