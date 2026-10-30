namespace BaseStorage.Core.Tests.HostSync.Session;

using System;
using System.Text;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.Networking;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// Fixtures copied from contract config-sync-contracts.md 2.2 and 4.4 (ADR-GWIN-121). The local and host sources, the host text,
/// the local text and the literal H are the contract values. Handshake outcomes come from a real PeerVersionTable (S-E006-002), so a
/// change to the Peers semantics breaks these tests too. Every call builds fresh objects, so no test shares state.
/// </summary>
internal static class ConfigSyncFixtures
{
    /// <summary>Contract 4.4 hostText.</summary>
    public const string HostText = "Wood: Wood\nOres: CopperOre, TinOre\n";

    /// <summary>Contract 4.4 localText.</summary>
    public const string LocalText = "Stone: Stone\n";

    /// <summary>Contract 2.2: the 250 UTF-8 bytes of H, as text (S-A-001 adds the AutoAssignMixedChests line).</summary>
    public const string HText = "BSC1;CraftFromBase=false\nExcludeVehicles=true\nKeepChestsCraftable=true\nMaxBaseStations=3\n"
        + "RequireSameVersion=true\nAutoAssignThresholdPct=50\nOrganizeLockSeconds=30\nReserveTimeoutMs=1500\nAutoAssignMixedChests=true\n@35\nWood: Wood\nOres: CopperOre, TinOre\n";

    /// <summary>The category file path used in the contract 5.2 M9 example.</summary>
    public const string CategoryPath = @"C:\V\BepInEx\config\BaseStorage.categories.txt";

    private static readonly TimeSpan Start = TimeSpan.FromSeconds(100);

    /// <summary>Contract 4.4 local: CraftFromBase true, MaxBaseStations 64, BaseCacheSeconds 2.</summary>
    public static FakeSettingsSource Local() =>
        new FakeSettingsSource().WithBool("CraftFromBase", true).WithInt("MaxBaseStations", 64).WithInt("BaseCacheSeconds", 2);

    /// <summary>Contract 4.4 hostSource: CraftFromBase false, MaxBaseStations 3.</summary>
    public static FakeSettingsSource HostSource() => new FakeSettingsSource().WithBool("CraftFromBase", false).WithInt("MaxBaseStations", 3);

    /// <summary>Contract 4.4 H: the encoding of the host source with the host text, built by the S-E006-003 codec.</summary>
    public static byte[] H() => HostSyncCodec.EncodeBytes(HostSyncPayload.Capture(HostSource(), HostText));

    /// <summary>The contract 2.2 literal H as bytes (250 bytes).</summary>
    public static byte[] HLiteral() => Encoding.UTF8.GetBytes(HText);

    /// <summary>The UTF-8 bytes of a text.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The bytes.</returns>
    public static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    /// <summary>Contract 4.4 hostAccept: a host table tracks a client, which sends its "0.1.0" version, so the outcome is Accept.</summary>
    /// <returns>The outcome.</returns>
    public static PeerVersionOutcome HostAccept()
    {
        var table = new PeerVersionTable(new ManualClock(Start), "0.1.0");
        long id = table.Track(false);
        return table.Receive(id, VersionHandshake.EncodePayloadBytes("0.1.0"), true);
    }

    /// <summary>Contract 4.4 lenientNone: a host table tracks a client that never sends a payload; past the deadline the outcome is Accept with no client version.</summary>
    /// <returns>The outcome.</returns>
    public static PeerVersionOutcome LenientNone()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, "0.1.0");
        table.Track(false);
        clock.Advance(PeerVersionTable.MissingPayloadTimeout + TimeSpan.FromSeconds(1));
        return table.Tick(false)[0];
    }

    /// <summary>Contract 4.4 refused: a host table receives a client version "0.2.0", so the outcome is RefuseMismatch.</summary>
    /// <returns>The outcome.</returns>
    public static PeerVersionOutcome Refused()
    {
        var table = new PeerVersionTable(new ManualClock(Start), "0.1.0");
        long id = table.Track(false);
        return table.Receive(id, VersionHandshake.EncodePayloadBytes("0.2.0"), true);
    }

    /// <summary>A host refusal past its grace: the outcome is Disconnect.</summary>
    /// <returns>The outcome.</returns>
    public static PeerVersionOutcome Disconnect()
    {
        var clock = new ManualClock(Start);
        var table = new PeerVersionTable(clock, "0.1.0");
        long id = table.Track(false);
        table.Receive(id, VersionHandshake.EncodePayloadBytes("0.2.0"), true);
        clock.Advance(PeerVersionTable.DisconnectGrace + TimeSpan.FromSeconds(1));
        return table.Tick(true)[0];
    }

    /// <summary>Contract 4.4 clientAccept: a client table tracks the host, whose "0.1.0" version is accepted with RemoteIsServer.</summary>
    /// <returns>The outcome.</returns>
    public static PeerVersionOutcome ClientAccept()
    {
        var table = new PeerVersionTable(new ManualClock(Start), "0.1.0");
        long id = table.Track(true);
        return table.Receive(id, VersionHandshake.EncodePayloadBytes("0.1.0"), true);
    }

    /// <summary>Contract 4.4 clientLeave: a client table tracks the host, whose "0.2.0" version gives LeaveMismatch.</summary>
    /// <returns>The outcome.</returns>
    public static PeerVersionOutcome ClientLeave()
    {
        var table = new PeerVersionTable(new ManualClock(Start), "0.1.0");
        long id = table.Track(true);
        return table.Receive(id, VersionHandshake.EncodePayloadBytes("0.2.0"), true);
    }

    /// <summary>An outcome for a peer the table does not know: Ignore.</summary>
    /// <returns>The outcome.</returns>
    public static PeerVersionOutcome UnknownPeer()
    {
        var table = new PeerVersionTable(new ManualClock(Start), "0.1.0");
        return table.Receive(99, null, true);
    }

    /// <summary>The handshake outcome with the given contract 4.4 name.</summary>
    /// <param name="name">One of hostAccept, lenientNone, refused, clientAccept, clientLeave, disconnect or ignore.</param>
    /// <returns>The outcome.</returns>
    public static PeerVersionOutcome Named(string name) => name switch
    {
        "hostAccept" => HostAccept(),
        "lenientNone" => LenientNone(),
        "refused" => Refused(),
        "clientAccept" => ClientAccept(),
        "clientLeave" => ClientLeave(),
        "disconnect" => Disconnect(),
        "ignore" => UnknownPeer(),
        _ => throw new ArgumentException("Unknown outcome name.", nameof(name)),
    };

    /// <summary>The malformed payload bytes with the given name (contract 4.4 and the R3 matrix).</summary>
    /// <param name="name">null, empty, 0xFF, garbage, truncated, wrong tag or oversize.</param>
    /// <returns>The bytes, possibly null.</returns>
    public static byte[]? MalformedBytes(string name) => name switch
    {
        "null" => null,
        "empty" => Array.Empty<byte>(),
        "0xFF" => new byte[] { 0xFF },
        "garbage" => Utf8("BSC1;garbage"),
        "truncated" => HLiteral()[..^1],
        "wrong tag" => Utf8("BSC2;" + HText.Substring(5)),
        "oversize" => new byte[HostSyncCodec.MaxPayloadLength + 1],
        _ => throw new ArgumentException("Unknown malformed case.", nameof(name)),
    };
}
