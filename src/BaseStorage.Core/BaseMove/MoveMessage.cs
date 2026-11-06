namespace BaseStorage.Core.BaseMove;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BaseStorage.Core.Compendium;
using BaseStorage.Core.Snapshots;

/// <summary>
/// One message of a move and its text form, sent over one string RPC: <c>BSM1;kind;id;refusal;player;station;playerId;name;destination;bases;summary;revisions</c>.
/// Free text is escaped, so no field can contain a separator. Anything malformed decodes to null. Pure.
/// </summary>
public sealed class MoveMessage
{
    /// <summary>The tag every payload starts with.</summary>
    public const string Tag = "BSM1";

    /// <summary>The name of the one RPC a move uses.</summary>
    public const string RpcName = "BaseStorage_Move";

    private const int FieldCount = 11;
    private const int BaseFieldCount = 9;
    private static readonly char[] FieldSeparator = { ';' };
    private static readonly char[] ListSeparator = { ',' };
    private static readonly char[] PairSeparator = { ':' };

    /// <summary>Gets the message kind.</summary>
    public MoveMessageKind Kind { get; set; }

    /// <summary>Gets the request id.</summary>
    public int RequestId { get; set; }

    /// <summary>Gets the refusal, or None.</summary>
    public MoveRefusal Refusal { get; set; }

    /// <summary>Gets the player a refusal names, or null.</summary>
    public string? PlayerName { get; set; }

    /// <summary>Gets the id of a station in the source base, as a chest-shaped id.</summary>
    public ContainerId SourceStation { get; set; }

    /// <summary>Gets the requester's player id.</summary>
    public long RequesterPlayerId { get; set; }

    /// <summary>Gets the destination base's name, or null.</summary>
    public string? DestinationName { get; set; }

    /// <summary>Gets the id of a station in the destination base.</summary>
    public ContainerId DestinationStation { get; set; }

    /// <summary>Gets the bases of a list reply.</summary>
    public IReadOnlyList<BaseEntry> Bases { get; set; } = Array.Empty<BaseEntry>();

    /// <summary>Gets the plan's summary, or null.</summary>
    public MoveSummary? Summary { get; set; }

    /// <summary>Gets the chest revisions of a confirm or a done.</summary>
    public IReadOnlyList<MoveChestRevision> Revisions { get; set; } = Array.Empty<MoveChestRevision>();

    /// <summary>Decodes a payload.</summary>
    /// <param name="payload">The payload.</param>
    /// <returns>The message, or null when the payload is malformed.</returns>
    public static MoveMessage? Decode(string? payload)
    {
        if (payload is null)
        {
            return null;
        }

        var fields = payload.Split(FieldSeparator);
        if (fields.Length != FieldCount || !string.Equals(fields[0], Tag, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var kind = int.Parse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture);
            var refusal = int.Parse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture);
            if (!Enum.IsDefined(typeof(MoveMessageKind), kind) || !Enum.IsDefined(typeof(MoveRefusal), refusal))
            {
                return null;
            }

            return new MoveMessage
            {
                Kind = (MoveMessageKind)kind,
                RequestId = int.Parse(fields[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                Refusal = (MoveRefusal)refusal,
                PlayerName = fields[4].Length == 0 ? null : Uri.UnescapeDataString(fields[4]),
                SourceStation = fields[5].Length == 0 ? default : DecodeId(fields[5]),
                RequesterPlayerId = long.Parse(fields[6], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                DestinationName = fields[7].Length == 0 ? null : Uri.UnescapeDataString(fields[7]),
                DestinationStation = DecodeDestination(fields[8], out IReadOnlyList<BaseEntry> bases),
                Bases = bases,
                Summary = fields[9].Length == 0 ? null : DecodeSummary(fields[9]),
                Revisions = DecodeRevisions(fields[10]),
            };
        }
        catch (FormatException)
        {
            return null;
        }
        catch (OverflowException)
        {
            return null;
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>Encodes this message.</summary>
    /// <returns>The payload.</returns>
    public string Encode()
    {
        var text = new StringBuilder(Tag);
        text.Append(';').Append(((int)Kind).ToString(CultureInfo.InvariantCulture));
        text.Append(';').Append(RequestId.ToString(CultureInfo.InvariantCulture));
        text.Append(';').Append(((int)Refusal).ToString(CultureInfo.InvariantCulture));
        text.Append(';').Append(Escape(PlayerName));
        text.Append(';').Append(SourceStation.IsNone ? string.Empty : EncodeId(SourceStation));
        text.Append(';').Append(RequesterPlayerId.ToString(CultureInfo.InvariantCulture));
        text.Append(';').Append(Escape(DestinationName));
        text.Append(';').Append(DestinationStation.IsNone ? string.Empty : EncodeId(DestinationStation)).Append('#').Append(EncodeBases(Bases));
        text.Append(';').Append(Summary is null ? string.Empty : EncodeSummary(Summary));
        text.Append(';').Append(EncodeRevisions(Revisions));
        return text.ToString();
    }

    private static string Escape(string? value) => string.IsNullOrEmpty(value) ? string.Empty : Uri.EscapeDataString(value);

    private static string EncodeId(ContainerId id) =>
        id.UserId.ToString(CultureInfo.InvariantCulture) + ":" + id.Id.ToString(CultureInfo.InvariantCulture);

    private static ContainerId DecodeId(string text)
    {
        var parts = text.Split(PairSeparator);
        return new ContainerId(
            long.Parse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
            uint.Parse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture));
    }

    private static ContainerId DecodeDestination(string text, out IReadOnlyList<BaseEntry> bases)
    {
        int split = text.IndexOf('#');
        if (split < 0)
        {
            throw new FormatException("The destination field has two parts.");
        }

        bases = DecodeBases(text.Substring(split + 1));
        return split == 0 ? default : DecodeId(text.Substring(0, split));
    }

    private static string EncodeBases(IReadOnlyList<BaseEntry> bases)
    {
        var parts = new string[bases.Count];
        for (var i = 0; i < parts.Length; i++)
        {
            BaseEntry entry = bases[i];
            var stations = new string[entry.Stations.Count];
            for (var j = 0; j < stations.Length; j++)
            {
                stations[j] = EncodeId(entry.Stations[j]);
            }

            parts[i] = string.Join(
                "|",
                EncodeId(entry.Station),
                Escape(entry.Name),
                entry.Biome.HasValue ? ((int)entry.Biome.Value).ToString(CultureInfo.InvariantCulture) : string.Empty,
                ((int)Math.Round(entry.X)).ToString(CultureInfo.InvariantCulture),
                ((int)Math.Round(entry.Z)).ToString(CultureInfo.InvariantCulture),
                entry.Chests.ToString(CultureInfo.InvariantCulture),
                entry.UsedSlots.ToString(CultureInfo.InvariantCulture),
                entry.TotalSlots.ToString(CultureInfo.InvariantCulture),
                string.Join("+", stations));
        }

        return string.Join(",", parts);
    }

    private static IReadOnlyList<BaseEntry> DecodeBases(string text)
    {
        var parts = text.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);
        var bases = new BaseEntry[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            var fields = parts[i].Split('|');
            if (fields.Length != BaseFieldCount)
            {
                throw new FormatException("A base has nine fields.");
            }

            CompendiumBiome? biome = null;
            if (fields[2].Length > 0)
            {
                var value = int.Parse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture);
                biome = Enum.IsDefined(typeof(CompendiumBiome), value) ? (CompendiumBiome)value : (CompendiumBiome?)null;
            }

            var stationTexts = fields[8].Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            var stations = new ContainerId[stationTexts.Length];
            for (var j = 0; j < stations.Length; j++)
            {
                stations[j] = DecodeId(stationTexts[j]);
            }

            bases[i] = new BaseEntry
            {
                Station = DecodeId(fields[0]),
                Name = fields[1].Length == 0 ? string.Empty : Uri.UnescapeDataString(fields[1]),
                Biome = biome,
                X = int.Parse(fields[3], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                Z = int.Parse(fields[4], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                Chests = int.Parse(fields[5], NumberStyles.None, CultureInfo.InvariantCulture),
                UsedSlots = int.Parse(fields[6], NumberStyles.None, CultureInfo.InvariantCulture),
                TotalSlots = int.Parse(fields[7], NumberStyles.None, CultureInfo.InvariantCulture),
                Stations = stations,
            };
        }

        return bases;
    }

    private static string EncodeNames(IReadOnlyList<string> names)
    {
        var parts = new string[names.Count];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = Uri.EscapeDataString(names[i]);
        }

        return string.Join(",", parts);
    }

    private static IReadOnlyList<string> DecodeNames(string text)
    {
        var parts = text.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = Uri.UnescapeDataString(parts[i]);
        }

        return parts;
    }

    private static string EncodeSummary(MoveSummary summary)
    {
        var ids = new string[summary.ChangedChests.Count];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = EncodeId(summary.ChangedChests[i]);
        }

        return summary.MovedStacks.ToString(CultureInfo.InvariantCulture)
            + "/" + summary.StayedStacks.ToString(CultureInfo.InvariantCulture)
            + "/" + summary.NoRoomStacks.ToString(CultureInfo.InvariantCulture)
            + "/" + summary.SkippedChests.ToString(CultureInfo.InvariantCulture)
            + "/" + EncodeNames(summary.StayedPrefabs)
            + "/" + string.Join(",", ids);
    }

    private static MoveSummary DecodeSummary(string text)
    {
        var parts = text.Split('/');
        if (parts.Length != 6)
        {
            throw new FormatException("A summary has six parts.");
        }

        var idTexts = parts[5].Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);
        var ids = new ContainerId[idTexts.Length];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = DecodeId(idTexts[i]);
        }

        return new MoveSummary(
            int.Parse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture),
            int.Parse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture),
            int.Parse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture),
            int.Parse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture),
            DecodeNames(parts[4]),
            ids);
    }

    private static string EncodeRevisions(IReadOnlyList<MoveChestRevision> revisions)
    {
        var parts = new string[revisions.Count];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = EncodeId(revisions[i].Id) + ":" + revisions[i].Revision.ToString(CultureInfo.InvariantCulture);
        }

        return string.Join(",", parts);
    }

    private static IReadOnlyList<MoveChestRevision> DecodeRevisions(string text)
    {
        var parts = text.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);
        var revisions = new MoveChestRevision[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            var pair = parts[i].Split(PairSeparator);
            revisions[i] = new MoveChestRevision(
                new ContainerId(
                    long.Parse(pair[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                    uint.Parse(pair[1], NumberStyles.None, CultureInfo.InvariantCulture)),
                uint.Parse(pair[2], NumberStyles.None, CultureInfo.InvariantCulture));
        }

        return revisions;
    }
}
