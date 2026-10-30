namespace BaseStorage.Core.Tests.HostSync;

using System;
using System.Collections.Generic;
using System.Text;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>AC-6.2 (contract 4.1 to 4.3; rules H2 to H4): encoding is the contract text, and every payload round-trips as text and as bytes.</summary>
public sealed class HostSyncCodecEncodeTests
{
    // GW-U-1004 (4.3): the tag, the cap and the exact text of the defaults and of D2.
    [Fact]
    public void EncodingGivesTheContractText()
    {
        Assert.Equal("BSC1;", HostSyncCodec.PayloadTag);
        Assert.Equal(1048576, HostSyncCodec.MaxPayloadLength);
        Assert.Equal(HostSyncFixtures.DefaultsText, HostSyncCodec.Encode(HostSyncFixtures.Defaults()));
        Assert.Equal(HostSyncFixtures.CategoryPayloadText, HostSyncCodec.Encode(HostSyncFixtures.WithCategories()));
    }

    // GW-U-1004 (H4): the byte form is the UTF-8 encoding of the text, without a byte order mark.
    [Fact]
    public void BytesAreUtf8WithoutBom()
    {
        HostSyncPayload payload = new HostSyncPayload(HostSyncFixtures.Defaults().Settings, "ö﻿\t");

        byte[] bytes = HostSyncCodec.EncodeBytes(payload);
        byte[] defaults = HostSyncCodec.EncodeBytes(HostSyncFixtures.Defaults());

        Assert.Equal(Encoding.UTF8.GetBytes(HostSyncCodec.Encode(payload)), bytes);
        Assert.Equal(Encoding.ASCII.GetBytes(HostSyncFixtures.DefaultsText), defaults);
        Assert.Equal((byte)'B', bytes[0]);
        Assert.Equal((byte)'B', defaults[0]);
    }

    // GW-U-1004 (R8): null throws with the parameter name on both encoders.
    [Fact]
    public void EncodersRejectNull()
    {
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() => HostSyncCodec.Encode(null!)).ParamName);
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() => HostSyncCodec.EncodeBytes(null!)).ParamName);
    }

    // GW-U-1005 (AC-6.2): the full table round-trips with defaults and with every value changed, as text and as bytes.
    [Theory]
    [MemberData(nameof(RoundTripRows))]
    public void PayloadRoundTripsAsTextAndBytes(string reason, HostSyncPayload payload)
    {
        string text = HostSyncCodec.Encode(payload);
        byte[] bytes = HostSyncCodec.EncodeBytes(payload);

        HostSyncPayload? fromText = HostSyncCodec.Decode(text);
        HostSyncPayload? fromBytes = HostSyncCodec.Decode(bytes);

        Assert.NotNull(fromText);
        Assert.NotNull(fromBytes);
        Assert.Equal(payload, fromText);
        Assert.Equal(payload, fromBytes);
        Assert.Equal(payload.CategoryFileText, fromText!.CategoryFileText);
        Assert.Equal(text, HostSyncCodec.Encode(fromText));
        Assert.Equal(bytes, HostSyncCodec.EncodeBytes(fromBytes!));
        Assert.DoesNotContain("\r", text, StringComparison.Ordinal);
        Assert.NotEmpty(reason);
    }

    public static IEnumerable<object[]> RoundTripRows()
    {
        IReadOnlyList<KeyValuePair<string, string>> defaults = HostSyncFixtures.Defaults().Settings;

        yield return ["defaults, empty text", HostSyncFixtures.Defaults()];
        yield return ["defaults, category text", HostSyncFixtures.WithCategories()];
        yield return ["every value changed", HostSyncFixtures.Changed()];
        yield return ["CRLF text normalised", new HostSyncPayload(defaults, "a\r\nb\rc\n")];
        yield return ["BOM, tab, blank line, no final newline", new HostSyncPayload(defaults, "﻿# x\t\n\nWood=Wood")];
        yield return ["multi-byte UTF-8", new HostSyncPayload(defaults, "ö﻿\t日本語 😀")];
        yield return ["text that looks like a payload", new HostSyncPayload(defaults, HostSyncFixtures.DefaultsText)];
        yield return ["text with @ and = lines", new HostSyncPayload(defaults, "@0\n@\n=\nMaxBaseStations=1\n")];
        yield return ["only newlines", new HostSyncPayload(defaults, "\n\n\n")];
        yield return ["long text", new HostSyncPayload(defaults, new string('x', 100000))];
        yield return ["negative int", new HostSyncPayload(HostSyncFixtures.With(HostSyncFixtures.Defaults(), "MaxBaseStations", "-7"), string.Empty)];
        yield return ["int extremes", HostSyncPayload.Capture(new FakeSettingsSource().WithInt("MaxBaseStations", int.MaxValue).WithInt("ReserveTimeoutMs", int.MinValue), string.Empty)];
    }

    // GW-U-1005 (H8): a payload exactly at the cap decodes; one code unit longer is refused.
    [Fact]
    public void PayloadAtTheCapDecodesAndOneOverDoesNot()
    {
        IReadOnlyList<KeyValuePair<string, string>> defaults = HostSyncFixtures.Defaults().Settings;
        int textLength = HostSyncCodec.MaxPayloadLength - HostSyncFixtures.DefaultsText.Length - 16;
        HostSyncPayload atCap = Build(textLength);
        while (HostSyncCodec.Encode(atCap).Length < HostSyncCodec.MaxPayloadLength)
        {
            atCap = Build(++textLength);
        }

        string text = HostSyncCodec.Encode(atCap);
        Assert.Equal(HostSyncCodec.MaxPayloadLength, text.Length);
        Assert.Equal(atCap, HostSyncCodec.Decode(text));
        Assert.Equal(atCap, HostSyncCodec.Decode(HostSyncCodec.EncodeBytes(atCap)));

        HostSyncPayload over = Build(textLength + 1);
        Assert.Equal(HostSyncCodec.MaxPayloadLength + 1, HostSyncCodec.Encode(over).Length);
        Assert.Null(HostSyncCodec.Decode(HostSyncCodec.Encode(over)));
        Assert.Null(HostSyncCodec.Decode(HostSyncCodec.EncodeBytes(over)));

        HostSyncPayload Build(int length) => new HostSyncPayload(defaults, new string('c', length));
    }

    // GW-U-1005 (H1): the encoded text names every host-synced key exactly once, in catalog order, and no other catalog key.
    [Fact]
    public void EncodedTextCoversExactlyTheHostSyncedKeys()
    {
        string text = HostSyncCodec.Encode(HostSyncFixtures.Changed());
        int position = 0;

        foreach (SettingDefinition row in SettingsCatalog.HostSynced)
        {
            int index = text.IndexOf("\n" + row.Key + "=", StringComparison.Ordinal);
            if (index < 0)
            {
                index = text.IndexOf(HostSyncCodec.PayloadTag + row.Key + "=", StringComparison.Ordinal);
            }

            Assert.True(index >= position, row.Key + " is missing or out of order.");
            Assert.Equal(index, text.LastIndexOf(row.Key + "=", StringComparison.Ordinal) - (text[index] == '\n' ? 1 : HostSyncCodec.PayloadTag.Length));
            position = index;
        }

        foreach (SettingDefinition row in SettingsCatalog.All)
        {
            if (!row.HostSynced)
            {
                Assert.DoesNotContain(row.Key + "=", text, StringComparison.Ordinal);
            }
        }
    }
}
