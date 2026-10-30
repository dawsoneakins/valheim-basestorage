namespace BaseStorage.Core.Ui;

using System.Collections.Generic;
using BaseStorage.Core.Organize.Run;

/// <summary>One row of the feedback catalog (immutable).</summary>
public sealed class FeedbackLine
{
    internal FeedbackLine(
        FeedbackOutcome outcome,
        FeedbackAction action,
        FeedbackLineOwner owner,
        string? key,
        OrganizeNoticePlacement? placement,
        IReadOnlyList<string> fragmentKeys)
    {
        Outcome = outcome;
        Action = action;
        Owner = owner;
        Key = key;
        Placement = placement;
        FragmentKeys = fragmentKeys;
    }

    /// <summary>Gets the outcome this row describes.</summary>
    public FeedbackOutcome Outcome { get; }

    /// <summary>Gets the user action the outcome belongs to.</summary>
    public FeedbackAction Action { get; }

    /// <summary>Gets who shows the one line of the outcome.</summary>
    public FeedbackLineOwner Owner { get; }

    /// <summary>Gets the one strings-table key; non-null exactly when <see cref="Owner"/> is <see cref="FeedbackLineOwner.Mod"/>.</summary>
    public string? Key { get; }

    /// <summary>Gets where the line shows; non-null exactly when <see cref="Owner"/> is <see cref="FeedbackLineOwner.Mod"/>.</summary>
    public OrganizeNoticePlacement? Placement { get; }

    /// <summary>Gets the keys whose text fills the arguments; empty, never null, for rows without fragments and for every non-Mod row.</summary>
    public IReadOnlyList<string> FragmentKeys { get; }
}
