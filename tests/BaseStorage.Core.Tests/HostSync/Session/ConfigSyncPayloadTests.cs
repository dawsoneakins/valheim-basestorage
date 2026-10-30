namespace BaseStorage.Core.Tests.HostSync.Session;

using BaseStorage.Core.Categories;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.HostSync.Session;

/// <summary>S-E006-004 AC-1 (contract 2.2, 4.2 V2 and R4, rule S8): the host payload bytes, and the client catalog equal to the host's parse.</summary>
public sealed class ConfigSyncPayloadTests
{
    // GW-U-1406 (2.2): the host payload is the 250-byte contract literal H (S-A-001 adds one line).
    [Fact]
    public void TheHostPayloadIsTheContractLiteral()
    {
        byte[]? bytes = new ConfigSyncSession(ConfigSyncFixtures.HostSource()).BuildHostPayload(ConfigSyncFixtures.HostText);

        Assert.NotNull(bytes);
        Assert.Equal(250, ConfigSyncFixtures.HLiteral().Length);
        Assert.Equal(ConfigSyncFixtures.HLiteral(), bytes);
    }

    // GW-U-1406: BuildHostPayload(null) encodes the local values with the empty category text.
    [Fact]
    public void ANullTextEncodesTheLocalValuesWithTheEmptyCategoryText()
    {
        byte[]? bytes = new ConfigSyncSession(ConfigSyncFixtures.Local()).BuildHostPayload(null);

        Assert.NotNull(bytes);
        Assert.Equal(HostSyncCodec.EncodeBytes(HostSyncPayload.Capture(ConfigSyncFixtures.Local(), string.Empty)), bytes);
    }

    // GW-U-1406: a text that makes the payload exceed HostSyncCodec.MaxPayloadLength gives null.
    [Fact]
    public void AnOversizeTextGivesNoPayload()
    {
        byte[]? bytes = new ConfigSyncSession(ConfigSyncFixtures.Local()).BuildHostPayload(new string('a', HostSyncCodec.MaxPayloadLength));

        Assert.Null(bytes);
    }

    // GW-U-1406: on a client with an applied host payload, BuildHostPayload encodes the local source, not the layered view.
    [Fact]
    public void AClientBuildsFromItsLocalValuesNotFromTheHostLayer()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);

        byte[]? bytes = session.BuildHostPayload(ConfigSyncFixtures.HostText);

        Assert.NotNull(bytes);
        Assert.Equal(HostSyncCodec.EncodeBytes(HostSyncPayload.Capture(ConfigSyncFixtures.Local(), ConfigSyncFixtures.HostText)), bytes);
    }

    // GW-U-1428 (S8): the client catalog equals CategoryFile.Parse of the host text, for the hostText, an empty (missing) file and a CRLF text with a comment line.
    [Theory]
    [InlineData("Wood: Wood\nOres: CopperOre, TinOre\n")]
    [InlineData("")]
    [InlineData("# host file\r\nWood: Wood\r\nOres: CopperOre, TinOre\r\n")]
    public void TheClientCatalogEqualsTheHostParsedCatalog(string hostFileText)
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        byte[]? bytes = new ConfigSyncSession(ConfigSyncFixtures.HostSource()).BuildHostPayload(hostFileText);
        Assert.NotNull(bytes);

        ConfigSyncOutcome applied = session.Receive(1, bytes, ConfigSyncMode.Client);

        Assert.Equal(ConfigSyncAction.Applied, applied.Action);
        CategoryCatalog expected = CategoryFile.Parse(hostFileText).Catalog;
        int expectedCount = expected.Categories.Count;
        Assert.Equal(expectedCount, applied.CategoryCount);
        Assert.Equal(expected.CategoryNames, session.Categories.CategoryNames);
        for (int i = 0; i < expectedCount; i++)
        {
            Assert.Equal(expected.Categories[i].Name, session.Categories.Categories[i].Name);
            Assert.Equal(expected.Categories[i].Prefabs, session.Categories.Categories[i].Prefabs);
        }
    }
}
