namespace BaseStorage.Core.Tests.HostSync;

using System;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>AC-6.2, AC-6.3, principle 5 (contract 5.2; rules H5 and H6): host values while a payload is applied, local values otherwise, and a malformed payload changes nothing.</summary>
public sealed class LayeredSettingsSourceTests
{
    private static FakeSettingsSource Local() =>
        new FakeSettingsSource().WithBool("CraftFromBase", true).WithInt("MaxBaseStations", 64).WithInt("BaseCacheSeconds", 2).WithText("OrganizeKey", "Q");

    private static HostSyncPayload Host() =>
        HostSyncPayload.Capture(new FakeSettingsSource().WithBool("CraftFromBase", false).WithInt("MaxBaseStations", 3), "Wood=Wood\n");

    // GW-U-1008 (AC-6.3): with no host payload every key is answered by the local source, through the typed read path too.
    [Fact]
    public void WithoutAHostEveryKeyIsLocal()
    {
        FakeSettingsSource local = Local();
        var view = new LayeredSettingsSource(local);
        var settings = new BaseStorageSettings(view);

        Assert.False(view.HasHost);
        Assert.Null(view.Host);
        Assert.True(settings.CraftFromBase);
        Assert.Equal(64, settings.MaxBaseStations);
        Assert.Equal(2, settings.BaseCacheSeconds);
        Assert.Equal("Q", settings.OrganizeKey);
        Assert.False(view.TryGetInt("ReserveTimeoutMs", out _));
        Assert.Equal(1500, settings.ReserveTimeoutMs);
        Assert.True(local.BoolCalls > 0 && local.IntCalls > 0 && local.TextCalls > 0);
    }

    // GW-U-1008 (AC-6.2): while a host payload is applied every host-synced key answers from it, including the host's defaults.
    [Fact]
    public void WithAHostEveryHostSyncedKeyIsTheHostValue()
    {
        var view = new LayeredSettingsSource(Local());
        var settings = new BaseStorageSettings(view);
        HostSyncPayload host = Host();

        view.ApplyHost(host);

        Assert.True(view.HasHost);
        Assert.Same(host, view.Host);
        Assert.False(settings.CraftFromBase);
        Assert.Equal(3, settings.MaxBaseStations);
        Assert.Equal(1500, settings.ReserveTimeoutMs);
        Assert.True(view.TryGetInt("ReserveTimeoutMs", out int reserve));
        Assert.Equal(1500, reserve);
        Assert.True(view.TryGetBool("RequireSameVersion", out bool require));
        Assert.True(require);
        Assert.Equal("Wood=Wood\n", view.Host!.CategoryFileText);
    }

    // GW-U-1008 (H6): while a host payload is applied non-synced keys stay local, and the local source is not consulted for synced keys.
    [Fact]
    public void WithAHostNonSyncedKeysStayLocalAndSyncedKeysSkipLocal()
    {
        FakeSettingsSource local = Local();
        var view = new LayeredSettingsSource(local);
        var settings = new BaseStorageSettings(view);
        view.ApplyHost(Host());
        int boolsBefore = local.BoolCalls;
        int intsBefore = local.IntCalls;

        Assert.False(settings.CraftFromBase);
        Assert.Equal(3, settings.MaxBaseStations);
        Assert.Equal(boolsBefore, local.BoolCalls);
        Assert.Equal(intsBefore, local.IntCalls);

        Assert.Equal(2, settings.BaseCacheSeconds);
        Assert.Equal("Q", settings.OrganizeKey);
        Assert.Equal(SettingsCatalog.DepositNeverItemsDefault, settings.DepositNeverItems);
        Assert.False(settings.DebugLogging);
        Assert.Equal(intsBefore + 1, local.IntCalls);
    }

    // GW-U-1008 (H6): a read with the wrong kind for a synced key is false and does not fall back to local.
    [Fact]
    public void WrongKindForASyncedKeyIsFalse()
    {
        FakeSettingsSource local = Local().WithText("CraftFromBase", "oops").WithBool("MaxBaseStations", true);
        var view = new LayeredSettingsSource(local);
        view.ApplyHost(Host());
        int textsBefore = local.TextCalls;
        int boolsBefore = local.BoolCalls;

        Assert.False(view.TryGetInt("CraftFromBase", out int asInt));
        Assert.Equal(0, asInt);
        Assert.False(view.TryGetText("MaxBaseStations", out string asText));
        Assert.Equal(string.Empty, asText);
        Assert.False(view.TryGetBool("MaxBaseStations", out bool asBool));
        Assert.False(asBool);
        Assert.Equal(textsBefore, local.TextCalls);
        Assert.Equal(boolsBefore, local.BoolCalls);
    }

    // GW-U-1008 (AC-6.2): clearing returns every key to local; clearing twice is harmless; a new host can be applied afterwards.
    [Fact]
    public void ClearHostReturnsToLocal()
    {
        var view = new LayeredSettingsSource(Local());
        var settings = new BaseStorageSettings(view);
        view.ApplyHost(Host());

        view.ClearHost();

        Assert.False(view.HasHost);
        Assert.Null(view.Host);
        Assert.True(settings.CraftFromBase);
        Assert.Equal(64, settings.MaxBaseStations);

        view.ClearHost();
        Assert.False(view.HasHost);

        Assert.True(view.TryApplyHost(HostSyncCodec.EncodeBytes(Host())));
        Assert.Equal(3, settings.MaxBaseStations);
    }

    // GW-U-1008 (5.2): TryApplyHost with a valid text or bytes applies the decoded payload and replaces an earlier host.
    [Fact]
    public void TryApplyHostAppliesAValidPayload()
    {
        var view = new LayeredSettingsSource(Local());
        var settings = new BaseStorageSettings(view);
        HostSyncPayload other = HostSyncFixtures.Changed();

        Assert.True(view.TryApplyHost(HostSyncCodec.Encode(Host())));
        Assert.Equal(3, settings.MaxBaseStations);

        Assert.True(view.TryApplyHost(HostSyncCodec.Encode(other)));
        Assert.Equal(other, view.Host);
        Assert.Equal(65, settings.MaxBaseStations);

        Assert.True(view.TryApplyHost(HostSyncCodec.EncodeBytes(Host())));
        Assert.Equal(Host(), view.Host);
    }

    // GW-U-1009 (principle 5, H5): a malformed payload is rejected and the active layer stays local.
    [Theory]
    [MemberData(nameof(HostSyncFixtures.MalformedTextRows), MemberType = typeof(HostSyncFixtures))]
    public void MalformedPayloadLeavesLocalActive(string reason, string payload)
    {
        var view = new LayeredSettingsSource(Local());
        var settings = new BaseStorageSettings(view);

        Assert.False(view.TryApplyHost(payload));
        Assert.False(view.TryApplyHost(System.Text.Encoding.UTF8.GetBytes(payload)));

        Assert.False(view.HasHost);
        Assert.Null(view.Host);
        Assert.True(settings.CraftFromBase);
        Assert.Equal(64, settings.MaxBaseStations);
        Assert.NotEmpty(reason);
    }

    // GW-U-1009 (principle 5, H5): a malformed payload is rejected and the previously applied host stays active.
    [Theory]
    [MemberData(nameof(HostSyncFixtures.MalformedTextRows), MemberType = typeof(HostSyncFixtures))]
    public void MalformedPayloadLeavesThePreviousHostActive(string reason, string payload)
    {
        var view = new LayeredSettingsSource(Local());
        var settings = new BaseStorageSettings(view);
        HostSyncPayload host = Host();
        view.ApplyHost(host);

        Assert.False(view.TryApplyHost(payload));
        Assert.False(view.TryApplyHost(System.Text.Encoding.UTF8.GetBytes(payload)));

        Assert.True(view.HasHost);
        Assert.Same(host, view.Host);
        Assert.False(settings.CraftFromBase);
        Assert.Equal(3, settings.MaxBaseStations);
        Assert.NotEmpty(reason);
    }

    // GW-U-1009 (H5): null text, null bytes and invalid bytes are rejected without throwing.
    [Fact]
    public void NullAndInvalidBytesAreRejected()
    {
        var view = new LayeredSettingsSource(Local());

        Assert.False(view.TryApplyHost((string?)null));
        Assert.False(view.TryApplyHost((byte[]?)null));
        Assert.False(view.TryApplyHost(new byte[] { 0xFF }));
        Assert.False(view.TryApplyHost(Array.Empty<byte>()));
        Assert.False(view.HasHost);
    }

    // GW-U-1009 (R8, 5.2): a null key is false without consulting local; null arguments throw with the parameter name.
    [Fact]
    public void NullKeyAndNullArgumentsAreHandled()
    {
        FakeSettingsSource local = Local();
        var view = new LayeredSettingsSource(local);

        Assert.False(view.TryGetBool(null!, out bool b));
        Assert.False(b);
        Assert.False(view.TryGetInt(null!, out int i));
        Assert.Equal(0, i);
        Assert.False(view.TryGetText(null!, out string t));
        Assert.Equal(string.Empty, t);
        Assert.Equal(0, local.BoolCalls + local.IntCalls + local.TextCalls);

        Assert.Equal("local", Assert.Throws<ArgumentNullException>(() => new LayeredSettingsSource(null!)).ParamName);
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() => view.ApplyHost(null!)).ParamName);
        Assert.False(view.HasHost);
    }

    // GW-U-1009 (H6): nothing is ever written to the local source; a local source that throws for unknown keys is not reached for synced keys.
    [Fact]
    public void LocalSourceIsNeverWrittenAndNotReachedForSyncedKeys()
    {
        var local = new FakeSettingsSource { Throws = new InvalidOperationException("local must not be read") };
        var view = new LayeredSettingsSource(local);
        view.ApplyHost(Host());

        Assert.True(view.TryGetBool("CraftFromBase", out bool craft));
        Assert.False(craft);
        Assert.True(view.TryGetInt("MaxBaseStations", out int max));
        Assert.Equal(3, max);
        Assert.Equal(0, local.BoolCalls + local.IntCalls + local.TextCalls);
    }
}
