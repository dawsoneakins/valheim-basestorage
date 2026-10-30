namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// AC-1, AC-2, AC-3 and AC-4 (S-FOUND-003, ADR-GWIN-017, ADR-GWIN-018): docs/adr/0002-container-ownership-and-access-model.md
/// has the required shape and content, and every game name it relies on is declared in src/Shared/PatchTargetNames.cs.
/// Both files are read as text (contract 4.1: this project does not compile PatchTargetNames.cs).
/// </summary>
[Trait("Category", "Repo")]
public sealed class Adr0002DocumentTests
{
    private const string AdrFile = "docs/adr/0002-container-ownership-and-access-model.md";
    private const string SharedFile = "src/Shared/PatchTargetNames.cs";

    private static readonly string[] Headings = ["Status", "Context", "Decision", "Consequences"];

    private static readonly string[] HandshakeTerms =
    [
        "ZNetView.ClaimOwnership", "RPC_RequestOpen", "RPC_OpenResponse", "RPC_RequestStack", "RPC_StackResponse",
        "ZDOVars.s_items", "Inventory.Save", "ZDO.SetOwner", "ForceSendZDO",
    ];

    private static readonly string[] AccessTerms =
    [
        "Container.CheckAccess", "PrivacySetting", "Private", "PrivateArea.CheckAccess", "m_checkGuardStone",
        "ZDOVars.s_inUse", "SetInUse", "IsInUse", "InventoryGui.UpdateContainer", "GetPlayerList",
    ];

    private static readonly string[] ReservationTerms = ["ReserveTimeoutMs", "InventoryGui.Show", "RPC_OpenResponse"];

    /// <summary>The four existing Container RPCs (contract 4.3) plus the 54 method and field values that ADR-GWIN-018 adds.</summary>
    private static readonly string[] RequiredBullets =
    [
        "Container.RPC_RequestOpen", "Container.RPC_OpenResponse", "Container.RPC_RequestStack", "Container.RPC_StackResponse",
        "Container.Interact",
        "Container.CheckAccess",
        "Container.IsInUse",
        "Container.SetInUse",
        "Container.UpdateUseVisual",
        "Container.CheckForChanges",
        "Container.OnContainerChanged",
        "Container.Save",
        "Container.Load",
        "Container.m_nview",
        "Container.m_inventory",
        "Container.m_inUse",
        "Container.m_privacy",
        "Container.m_checkGuardStone",
        "Container.m_piece",
        "Container.m_wagon",
        "ZNetView.IsValid",
        "ZNetView.IsOwner",
        "ZNetView.ClaimOwnership",
        "ZNetView.GetZDO",
        "ZNetView.InvokeRPC",
        "ZDO.IsOwner",
        "ZDO.GetOwner",
        "ZDO.SetOwner",
        "ZDO.get_DataRevision",
        "ZDO.GetInt",
        "ZDO.GetByteArray",
        "ZDO.Set",
        "ZDO.m_uid",
        "ZDOID.get_UserID",
        "ZDOVars.s_inUse",
        "ZDOVars.s_items",
        "ZDOMan.GetSessionID",
        "ZDOMan.ForceSendZDO",
        "ZDOMan.RemovePeer",
        "ZDOMan.ReleaseZDOS",
        "ZDOMan.ReleaseNearbyZDOS",
        "ZDOMan.IsInPeerActiveArea",
        "ZNet.GetUID",
        "ZNet.GetPeer",
        "ZNet.IsServer",
        "ZNet.GetPlayerList",
        "PrivateArea.CheckAccess",
        "Piece.GetCreator",
        "Vagon.InUse",
        "Inventory.Save",
        "InventoryGui.Show",
        "InventoryGui.Hide",
        "InventoryGui.CloseContainer",
        "InventoryGui.UpdateContainer",
        "InventoryGui.IsContainerOpen",
        "InventoryGui.m_currentContainer",
        "Game.GetPlayerProfile",
        "PlayerProfile.GetPlayerID",
    ];

    private static readonly Regex BuildLine = new(
        @"Verified against \x60assembly_valheim\.dll\x60 from Steam build \d+ \(SHA-256 \x60[0-9a-f]{64}\x60\)",
        RegexOptions.CultureInvariant);

    private static readonly Regex Bullet = new(@"^- \x60([A-Za-z0-9_]+)\.([A-Za-z0-9_]+)\x60$", RegexOptions.CultureInvariant);

    private static readonly Regex ClassHeader = new(@"^[ \t]*public static class (\w+)[ \t]*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex MisspelledRpc = new(@"RPC_OpenRespons(?![A-Za-z])", RegexOptions.CultureInvariant);

    // GW-U-145 (AC-1)
    [Fact]
    public void AdrShapeAndHandshakeContentMatchTheContract()
    {
        string path = RepoRoot.Combine(AdrFile);
        Assert.True(File.Exists(path), $"Required repo file is missing: {AdrFile}");

        byte[] bytes = File.ReadAllBytes(path);
        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        Assert.False(hasBom, "ADR 0002 must not start with a UTF-8 byte order mark.");
        Assert.DoesNotContain((byte)'\r', bytes);

        string text = Encoding.UTF8.GetString(bytes);
        string[] lines = text.Split('\n');
        string firstLine = lines.First(line => line.Trim().Length > 0);
        Assert.StartsWith("# ADR 0002: ", firstLine, StringComparison.Ordinal);

        string[] h2 = lines
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .Select(line => line.TrimEnd())
            .ToArray();
        Assert.Equal(Headings.Select(h => "## " + h).ToArray(), h2);

        Assert.Contains("Accepted", SectionBody(lines, "Status"), StringComparison.Ordinal);

        foreach (string term in HandshakeTerms)
        {
            Assert.Contains(term, text, StringComparison.Ordinal);
        }

        Assert.True(BuildLine.IsMatch(text), "ADR 0002 must cite the build id and SHA-256 in the 'Verified against' line.");
        Assert.Contains("(checked by the contract test)", text, StringComparison.Ordinal);
        Assert.Contains("(inspected only; the contract test cannot check this)", text, StringComparison.Ordinal);
        Assert.False(MisspelledRpc.IsMatch(text), "ADR 0002 must spell the RPC as RPC_OpenResponse, not RPC_OpenRespons.");
    }

    // GW-U-145 (AC-2)
    [Fact]
    public void AdrAccessAndInUseContentIsPresent()
    {
        string text = RepoRoot.ReadText(AdrFile);

        foreach (string term in AccessTerms)
        {
            Assert.Contains(term, text, StringComparison.Ordinal);
        }

        Assert.Contains("ward", text, StringComparison.OrdinalIgnoreCase);
    }

    // GW-U-145 (AC-3)
    [Fact]
    public void AdrReservationContentIsPresent()
    {
        string[] lines = RepoRoot.ReadText(AdrFile).Split('\n');
        string decision = SectionBody(lines, "Decision");
        string consequences = SectionBody(lines, "Consequences");

        foreach (string term in ReservationTerms)
        {
            Assert.Contains(term, decision, StringComparison.Ordinal);
        }

        Assert.Contains("timeout", decision, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("disconnect", decision, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prefix", decision, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transactional", consequences, StringComparison.OrdinalIgnoreCase);
    }

    // GW-U-146
    [Fact]
    public void EveryReliedOnGameNameIsDeclaredAndEveryAddedNameIsInTheAdr()
    {
        string[] lines = RepoRoot.ReadText(AdrFile).Split('\n');
        List<(string Type, string Member)> bullets = ReliedOnBullets(lines);
        Assert.True(bullets.Count >= 58, $"ADR 0002 must list at least 58 relied-on game names; it lists {bullets.Count}.");

        string shared = RepoRoot.ReadText(SharedFile);
        Dictionary<string, string> blocks = ClassBlocks(shared);
        Assert.True(blocks.Count >= 16, $"{SharedFile} must hold at least 16 'public static class' blocks; it holds {blocks.Count}.");

        foreach ((string type, string member) in bullets)
        {
            Assert.True(
                DeclaresValue(blocks, type, member),
                $"ADR 0002 relies on {type}.{member}, but PatchTargetNames.{type} has no constant with value \"{member}\".");
        }

        HashSet<string> listed = bullets.Select(b => b.Type + "." + b.Member).ToHashSet(StringComparer.Ordinal);
        string[] missing = RequiredBullets.Where(name => !listed.Contains(name)).ToArray();
        Assert.True(missing.Length == 0, "ADR 0002 has no relied-on bullet for: " + string.Join(", ", missing));
    }

    /// <summary>The text under the heading, up to the next '## ' heading.</summary>
    private static string SectionBody(string[] lines, string heading)
    {
        int start = Array.FindIndex(lines, line => line.TrimEnd() == "## " + heading);
        Assert.True(start >= 0, $"ADR 0002 has no '## {heading}' heading.");

        int next = Array.FindIndex(lines, start + 1, line => line.StartsWith("## ", StringComparison.Ordinal));
        int end = next < 0 ? lines.Length : next;
        return string.Join("\n", lines[(start + 1)..end]);
    }

    /// <summary>The '- `Type.member`' bullets after '**Relied-on game names.**', ending at a blank line that is followed by a non-bullet.</summary>
    private static List<(string Type, string Member)> ReliedOnBullets(string[] lines)
    {
        int header = Array.FindIndex(lines, line => line.Trim() == "**Relied-on game names.**");
        Assert.True(header >= 0, "ADR 0002 has no '**Relied-on game names.**' list.");

        List<(string Type, string Member)> result = [];
        for (int i = header + 1; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd();
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                Match match = Bullet.Match(line);
                Assert.True(match.Success, $"ADR 0002 relied-on bullet must be '- `<Type>.<member>`': {line}");
                result.Add((match.Groups[1].Value, match.Groups[2].Value));
                continue;
            }

            bool blankBeforeBullet = line.Length == 0 && i + 1 < lines.Length && lines[i + 1].StartsWith("- ", StringComparison.Ordinal);
            if (!blankBeforeBullet)
            {
                break;
            }
        }

        return result;
    }

    /// <summary>Each 'public static class' block of PatchTargetNames, keyed by its class name, from its header to the next header.</summary>
    private static Dictionary<string, string> ClassBlocks(string shared)
    {
        MatchCollection headers = ClassHeader.Matches(shared);
        Dictionary<string, string> blocks = new(StringComparer.Ordinal);
        for (int i = 0; i < headers.Count; i++)
        {
            int start = headers[i].Index;
            int end = i + 1 < headers.Count ? headers[i + 1].Index : shared.Length;
            blocks[headers[i].Groups[1].Value] = shared.Substring(start, end - start);
        }

        return blocks;
    }

    /// <summary>True when the block whose TypeName is <paramref name="type"/> declares a string constant with value <paramref name="member"/>.</summary>
    private static bool DeclaresValue(Dictionary<string, string> blocks, string type, string member)
    {
        string? block = blocks.Values.FirstOrDefault(b => b.Contains("const string TypeName = \"" + type + "\";", StringComparison.Ordinal));
        if (block is null)
        {
            return false;
        }

        return Regex.IsMatch(block, "const string \\w+ = \"" + Regex.Escape(member) + "\";", RegexOptions.CultureInvariant);
    }
}
