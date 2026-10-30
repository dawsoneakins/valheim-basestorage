namespace BaseStorage.Core.Ui;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Composes the Organize preview text (spec AC-4.7, AC-5.3) and the end-of-run summary line (AC-4.8, AC-8.1) from the strings
/// table (ui-strings contract section 10). Every number is formatted by the table with the invariant culture, every clause
/// whose count is zero is omitted, and every result is one trimmed line.
/// </summary>
public static class OrganizeSummaryFormatter
{
    /// <summary>The reason groups in display order: the actionable in-use group first, then the enum order.</summary>
    private static readonly SkipReason[] GroupOrder = { SkipReason.InUse, SkipReason.Keep, SkipReason.Ineligible, SkipReason.InvalidContents };

    /// <summary>Builds the full preview line: the preview header, then the body (contract 10.1 P1).</summary>
    /// <param name="plan">The plan to summarise; not null.</param>
    /// <param name="catalog">The catalog the plan was made with; it orders the claimed-chest sentences. Not null.</param>
    /// <returns>One trimmed line, for example <c>Organize base: 214 stacks to move across 23 chests. 2 chests will be claimed for Metals &amp; Ores. 4 stacks have no room. 1 chest skipped (in use by Erik).</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="plan"/> or <paramref name="catalog"/> is null.</exception>
    public static string Preview(OrganizePlan plan, CategoryCatalog catalog)
    {
        string body = PreviewBody(plan, catalog);
        StringTable table = StringTable.English;
        return Finish(table.Format(StringKeys.OrganizePreviewLine, table.Get(StringKeys.OrganizePreviewHeader), body));
    }

    /// <summary>Builds the preview body: the sentences after the header (contract 10.1 P2 to P6).</summary>
    /// <param name="plan">The plan to summarise; not null.</param>
    /// <param name="catalog">The catalog the plan was made with; it orders the claimed-chest sentences. Not null.</param>
    /// <returns>One trimmed line of sentences separated by one space; <c>Nothing to move.</c> leads when the plan has no moves.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plan"/> or <paramref name="catalog"/> is null.</exception>
    public static string PreviewBody(OrganizePlan plan, CategoryCatalog catalog)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        StringTable table = StringTable.English;
        var sentences = new List<string>();

        int moves = plan.Moves.Count;
        int destinations = CountDestinations(plan.Moves);
        if (moves == 0)
        {
            sentences.Add(table.Get(StringKeys.OrganizePreviewBodyNothing));
        }
        else
        {
            string key = StringKeys.OrganizePreviewBodyMove + "." + Plural.Form(moves) + "." + Plural.Form(destinations);
            sentences.Add(table.Format(key, ClauseArgs(moves, destinations, 0, null, 0, 0, null)));
        }

        foreach (KeyValuePair<string, int> claim in ClaimsByCategory(plan.ClaimedOverflow, catalog))
        {
            string name = SingleLine.Of(claim.Key);
            sentences.Add(table.FormatPlural(StringKeys.OrganizePreviewBodyClaim, claim.Value, ClauseArgs(moves, destinations, claim.Value, name, 0, 0, null)));
        }

        int rules = plan.AutoAssign.MixedAssigned.Count;
        if (rules > 0)
        {
            sentences.Add(table.FormatPlural(StringKeys.OrganizePreviewBodyRules, rules, rules));
        }

        int noRoom = plan.NoRoom.Count;
        if (noRoom > 0)
        {
            sentences.Add(table.FormatPlural(StringKeys.OrganizePreviewBodyNoRoom, noRoom, ClauseArgs(moves, destinations, 0, null, noRoom, 0, null)));
        }

        if (noRoom > 0)
        {
            sentences.Add(table.Get(HasFreeSlot(plan) ? StringKeys.OrganizePreviewBodyReasonCategory : StringKeys.OrganizePreviewBodyReasonFull));
        }

        var entries = new OrganizeSkipEntry[plan.Skipped.Count];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = OrganizeSkipEntry.From(plan.Skipped[i]);
        }

        foreach (SkipReason kind in GroupOrder)
        {
            int count = CountKind(entries, kind);
            if (count == 0)
            {
                continue;
            }

            string? names = kind == SkipReason.InUse ? JoinNames(table, entries) : null;
            string sentence = names is not null
                ? table.FormatPlural(StringKeys.OrganizePreviewBodySkipped, count, ClauseArgs(moves, destinations, 0, null, noRoom, count, names))
                : table.FormatPlural(
                    StringKeys.OrganizePreviewBodySkippedReason,
                    count,
                    ClauseArgs(moves, destinations, 0, null, noRoom, count, ReasonText(table, kind, null)));
            sentences.Add(sentence);
        }

        return Finish(string.Join(" ", sentences.ToArray()));
    }

    private static bool HasFreeSlot(OrganizePlan plan)
    {
        for (int i = 0; i < plan.Layouts.Count; i++)
        {
            ChestLayout layout = plan.Layouts[i];
            if (layout.Stacks.Count < layout.Container.SlotCount)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds the one end-of-run MessageHud line (contract 10.2).</summary>
    /// <param name="report">What the run did; not null.</param>
    /// <returns>One trimmed line without markup, for example <c>Organized 214 stacks; 1 chest skipped (in use by Erik); 4 stacks had no room</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is null.</exception>
    public static string Summary(OrganizeRunReport report)
    {
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        StringTable table = StringTable.English;
        var fragments = new List<string>();

        fragments.Add(report.MovedStacks == 0
            ? table.Get(StringKeys.MessageOrganizeSummaryMovedNone)
            : table.FormatPlural(StringKeys.MessageOrganizeSummaryMoved, report.MovedStacks, report.MovedStacks));

        if (report.StoppedEarly)
        {
            fragments.Add(report.StopReason is null
                ? table.Get(StringKeys.MessageOrganizeSummaryStoppedNoReason)
                : table.Format(StringKeys.MessageOrganizeSummaryStopped, SingleLine.Of(report.StopReason)));
        }

        var entries = new OrganizeSkipEntry[report.Skipped.Count];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = report.Skipped[i];
        }

        foreach (SkipReason kind in GroupOrder)
        {
            int count = CountKind(entries, kind);
            if (count == 0)
            {
                continue;
            }

            string? names = kind == SkipReason.InUse ? JoinNames(table, entries) : null;
            fragments.Add(table.FormatPlural(StringKeys.MessageOrganizeSummarySkipped, count, count, ReasonText(table, kind, names)));
        }

        if (report.NoRoomStacks > 0)
        {
            fragments.Add(table.FormatPlural(StringKeys.MessageOrganizeSummaryNoRoom, report.NoRoomStacks, report.NoRoomStacks));
        }

        return Finish(string.Join(table.Get(StringKeys.MessageOrganizeSummaryJoin), fragments.ToArray()));
    }

    /// <summary>The 7-element clause argument array of contract 3.2 ({0} to {6}).</summary>
    private static object?[] ClauseArgs(int moves, int destinations, int claimed, string? category, int noRoom, int skipped, string? names) =>
        new object?[] { moves, destinations, claimed, category, noRoom, skipped, names };

    private static string Finish(string line) => SingleLine.Of(line).Trim();

    private static int CountDestinations(IReadOnlyList<PlannedMove> moves)
    {
        var seen = new HashSet<ContainerId>();
        for (int i = 0; i < moves.Count; i++)
        {
            seen.Add(moves[i].Destination);
        }

        return seen.Count;
    }

    /// <summary>Counts the claimed chests per category, ordered by catalog position; names the catalog lacks follow in first-claim order.</summary>
    private static List<KeyValuePair<string, int>> ClaimsByCategory(IReadOnlyList<ChestLayout> claimed, CategoryCatalog catalog)
    {
        var counts = new List<KeyValuePair<string, int>>();
        var indexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < claimed.Count; i++)
        {
            string name = claimed[i].CategoryName ?? string.Empty;
            if (indexByName.TryGetValue(name, out int at))
            {
                counts[at] = new KeyValuePair<string, int>(name, counts[at].Value + 1);
            }
            else
            {
                indexByName.Add(name, counts.Count);
                counts.Add(new KeyValuePair<string, int>(name, 1));
            }
        }

        // Stable insertion sort by catalog position; unknown names rank last and keep their first-claim order.
        for (int i = 1; i < counts.Count; i++)
        {
            KeyValuePair<string, int> item = counts[i];
            int rank = Rank(catalog, item.Key);
            int j = i - 1;
            while (j >= 0 && Rank(catalog, counts[j].Key) > rank)
            {
                counts[j + 1] = counts[j];
                j--;
            }

            counts[j + 1] = item;
        }

        return counts;
    }

    private static int Rank(CategoryCatalog catalog, string name)
    {
        int index = catalog.IndexOf(name);
        return index < 0 ? int.MaxValue : index;
    }

    private static int CountKind(OrganizeSkipEntry[] entries, SkipReason kind)
    {
        int count = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].Kind == kind)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Joins the distinct known player names of the in-use entries in first-seen order; null when none is known.</summary>
    private static string? JoinNames(StringTable table, OrganizeSkipEntry[] entries)
    {
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < entries.Length; i++)
        {
            string? name = entries[i].PlayerName;
            if (entries[i].Kind == SkipReason.InUse && name is not null && seen.Add(name))
            {
                names.Add(SingleLine.Of(name));
            }
        }

        return names.Count == 0 ? null : string.Join(table.Get(StringKeys.OrganizeNamesJoin), names.ToArray());
    }

    /// <summary>The reason fragment of a group: in use by the named players, in use with no name known, Keep, not eligible or unclear contents.</summary>
    private static string ReasonText(StringTable table, SkipReason kind, string? names)
    {
        switch (kind)
        {
            case SkipReason.InUse:
                return names is null
                    ? table.Get(StringKeys.OrganizeSkipReasonInUseUnknown)
                    : table.Format(StringKeys.OrganizeSkipReasonInUse, names);
            case SkipReason.Keep:
                return table.Get(StringKeys.OrganizeSkipReasonKeep);
            case SkipReason.InvalidContents:
                return table.Get(StringKeys.OrganizeSkipReasonInvalidContents);
            default:
                return table.Get(StringKeys.OrganizeSkipReasonIneligible);
        }
    }
}
