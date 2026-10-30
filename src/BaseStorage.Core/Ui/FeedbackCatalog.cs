namespace BaseStorage.Core.Ui;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Organize.Run;

/// <summary>The closed catalog of user-action outcomes and their one MessageHud line (S-E008-001).</summary>
public static class FeedbackCatalog
{
    private static readonly ReadOnlyCollection<string> None = new ReadOnlyCollection<string>(Array.Empty<string>());

    private static readonly string[] SummaryKeys =
    [
        StringKeys.MessageOrganizeSummaryMoved,
        StringKeys.MessageOrganizeSummaryMovedNone,
        StringKeys.MessageOrganizeSummarySkipped,
        StringKeys.MessageOrganizeSummaryNoRoom,
        StringKeys.MessageOrganizeSummaryStopped,
        StringKeys.MessageOrganizeSummaryStoppedNoReason,
        StringKeys.MessageOrganizeSummaryJoin,
        StringKeys.OrganizeSkipReasonInUse,
        StringKeys.OrganizeSkipReasonInUseUnknown,
        StringKeys.OrganizeSkipReasonKeep,
        StringKeys.OrganizeSkipReasonIneligible,
        StringKeys.OrganizeSkipReasonInvalidContents,
        StringKeys.OrganizeNamesJoin,
    ];

    private static readonly string[] StopReasonKeys =
    [
        StringKeys.OrganizeStopReasonReservationRefused,
        StringKeys.OrganizeStopReasonReservationTimedOut,
        StringKeys.OrganizeStopReasonReservationLost,
        StringKeys.OrganizeStopReasonChestUnavailable,
        StringKeys.OrganizeStopReasonChestChanged,
        StringKeys.OrganizeStopReasonDestinationFull,
        StringKeys.OrganizeStopReasonRollbackFailed,
        StringKeys.OrganizeStopReasonAborted,
        StringKeys.OrganizeStopReasonLockLost,
    ];

    private static readonly ReadOnlyCollection<FeedbackLine> Lines = Build();

    private static readonly Dictionary<FeedbackOutcome, FeedbackLine> ByOutcome = IndexOutcomes();

    private static readonly Dictionary<string, OrganizeNoticePlacement> ByKey = IndexKeys();

    /// <summary>Gets one line per defined outcome, in ascending numeric order of the outcome.</summary>
    public static IReadOnlyList<FeedbackLine> All => Lines;

    /// <summary>Gets the row of an outcome.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The catalog row.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the outcome is not defined; the parameter name is <c>outcome</c>.</exception>
    public static FeedbackLine For(FeedbackOutcome outcome)
    {
        if (!ByOutcome.TryGetValue(outcome, out FeedbackLine? line))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        return line;
    }

    /// <summary>Gets the placement every Mod row using this key shares.</summary>
    /// <param name="key">The strings-table key.</param>
    /// <returns>The shared placement; <see cref="OrganizeNoticePlacement.Centre"/> for a key no Mod row uses.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the key is null; the parameter name is <c>key</c>.</exception>
    public static OrganizeNoticePlacement PlacementOf(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return ByKey.TryGetValue(key, out OrganizeNoticePlacement placement) ? placement : OrganizeNoticePlacement.Centre;
    }

    private static ReadOnlyCollection<string> List(params string[] keys) => new ReadOnlyCollection<string>(keys);

    private static FeedbackLine Mod(FeedbackOutcome outcome, FeedbackAction action, string key, OrganizeNoticePlacement placement, ReadOnlyCollection<string> fragments) =>
        new FeedbackLine(outcome, action, FeedbackLineOwner.Mod, key, placement, fragments);

    private static FeedbackLine Other(FeedbackOutcome outcome, FeedbackAction action, FeedbackLineOwner owner) =>
        new FeedbackLine(outcome, action, owner, null, null, None);

    private static Dictionary<FeedbackOutcome, FeedbackLine> IndexOutcomes()
    {
        var map = new Dictionary<FeedbackOutcome, FeedbackLine>();
        foreach (FeedbackLine line in Lines)
        {
            map[line.Outcome] = line;
        }

        return map;
    }

    private static Dictionary<string, OrganizeNoticePlacement> IndexKeys()
    {
        var map = new Dictionary<string, OrganizeNoticePlacement>(StringComparer.Ordinal);
        foreach (FeedbackLine line in Lines)
        {
            if (line.Key != null && line.Placement.HasValue)
            {
                map[line.Key] = line.Placement.Value;
            }
        }

        return map;
    }

    private static ReadOnlyCollection<FeedbackLine> Build()
    {
        ReadOnlyCollection<string> category = List(StringKeys.ChestCategoryAuto, StringKeys.ChestCategoryKeep);
        ReadOnlyCollection<string> organizeSummary = List(SummaryKeys);
        ReadOnlyCollection<string> organizeStopped = List([.. SummaryKeys, .. StopReasonKeys]);
        ReadOnlyCollection<string> stopReasons = List(StopReasonKeys);
        return new ReadOnlyCollection<FeedbackLine>(
        [
            Mod(FeedbackOutcome.CraftCreated, FeedbackAction.Craft, StringKeys.MessageCraftUsedBase, OrganizeNoticePlacement.TopLeft, None),
            Mod(FeedbackOutcome.CraftReturned, FeedbackAction.Craft, StringKeys.MessageCraftMaterialsMoved, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.CraftFailed, FeedbackAction.Craft, StringKeys.MessageCraftMaterialsMoved, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.CraftPreFlightFailed, FeedbackAction.Craft, StringKeys.MessageCraftMaterialsMoved, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.CraftUnavailable, FeedbackAction.Craft, StringKeys.MessageCraftMaterialsMoved, OrganizeNoticePlacement.Centre, None),
            Other(FeedbackOutcome.CraftAbandoned, FeedbackAction.Craft, FeedbackLineOwner.Superseded),
            Mod(FeedbackOutcome.BuildPlaced, FeedbackAction.Build, StringKeys.MessageCraftUsedBase, OrganizeNoticePlacement.TopLeft, None),
            Other(FeedbackOutcome.BuildReturned, FeedbackAction.Build, FeedbackLineOwner.Vanilla),
            Mod(FeedbackOutcome.BuildFailed, FeedbackAction.Build, StringKeys.MessageCraftMaterialsMoved, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.BuildPreFlightFailed, FeedbackAction.Build, StringKeys.MessageCraftMaterialsMoved, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.BuildUnavailable, FeedbackAction.Build, StringKeys.MessageCraftMaterialsMoved, OrganizeNoticePlacement.Centre, None),
            Other(FeedbackOutcome.BuildAbandoned, FeedbackAction.Build, FeedbackLineOwner.Superseded),
            Other(FeedbackOutcome.BuildPlacementRefused, FeedbackAction.Build, FeedbackLineOwner.Vanilla),
            Mod(FeedbackOutcome.CategoryApplied, FeedbackAction.CategoryChange, StringKeys.MessageCategorySet, OrganizeNoticePlacement.TopLeft, category),
            Mod(FeedbackOutcome.CategoryDenied, FeedbackAction.CategoryChange, StringKeys.MessageCategoryRefused, OrganizeNoticePlacement.Centre, List(StringKeys.CategoryRefusalDenied)),
            Mod(FeedbackOutcome.CategoryInUse, FeedbackAction.CategoryChange, StringKeys.MessageCategoryRefused, OrganizeNoticePlacement.Centre, List(StringKeys.CategoryRefusalInUse, StringKeys.PlayerUnknown)),
            Mod(FeedbackOutcome.CategoryTimeout, FeedbackAction.CategoryChange, StringKeys.MessageCategoryRefused, OrganizeNoticePlacement.Centre, List(StringKeys.CategoryRefusalTimeout)),
            Mod(FeedbackOutcome.CategoryUnavailable, FeedbackAction.CategoryChange, StringKeys.MessageCategoryRefused, OrganizeNoticePlacement.Centre, List(StringKeys.CategoryRefusalUnavailable)),
            Mod(FeedbackOutcome.CategoryUnchanged, FeedbackAction.CategoryChange, StringKeys.MessageCategorySet, OrganizeNoticePlacement.TopLeft, category),
            Other(FeedbackOutcome.CategoryAlreadyPending, FeedbackAction.CategoryChange, FeedbackLineOwner.Coalesced),
            Other(FeedbackOutcome.CategoryChestGone, FeedbackAction.CategoryChange, FeedbackLineOwner.TargetGone),
            Mod(FeedbackOutcome.OrganizeCompleted, FeedbackAction.Organize, StringKeys.MessageOrganizeRunSummary, OrganizeNoticePlacement.TopLeft, organizeSummary),
            Mod(FeedbackOutcome.OrganizeStopped, FeedbackAction.Organize, StringKeys.MessageOrganizeRunSummary, OrganizeNoticePlacement.TopLeft, organizeStopped),
            Mod(FeedbackOutcome.OrganizeCancelled, FeedbackAction.Organize, StringKeys.MessageOrganizeCancelled, OrganizeNoticePlacement.TopLeft, None),
            Mod(FeedbackOutcome.OrganizeNotInBase, FeedbackAction.Organize, StringKeys.MessageBaseNotInBase, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.OrganizeNoChests, FeedbackAction.Organize, StringKeys.StationActionsDisabledNoChests, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.OrganizeAlreadyActive, FeedbackAction.Organize, StringKeys.MessageBaseInProgress, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.OrganizeLockRefused, FeedbackAction.Organize, StringKeys.MessageBaseInProgress, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.OrganizeConfirmRefused, FeedbackAction.Organize, StringKeys.MessageBaseInProgress, OrganizeNoticePlacement.Centre, None),
            Other(FeedbackOutcome.OrganizeNoPlayer, FeedbackAction.Organize, FeedbackLineOwner.NoHud),
            Other(FeedbackOutcome.OrganizeWorldGone, FeedbackAction.Organize, FeedbackLineOwner.NoHud),
            Mod(FeedbackOutcome.DepositCompleted, FeedbackAction.Deposit, StringKeys.MessageDepositSummary, OrganizeNoticePlacement.TopLeft, None),
            Mod(FeedbackOutcome.DepositStopped, FeedbackAction.Deposit, StringKeys.MessageDepositSummaryStopped, OrganizeNoticePlacement.TopLeft, stopReasons),
            Mod(FeedbackOutcome.DepositCancelled, FeedbackAction.Deposit, StringKeys.MessageDepositCancelled, OrganizeNoticePlacement.TopLeft, None),
            Mod(FeedbackOutcome.DepositNotInBase, FeedbackAction.Deposit, StringKeys.MessageBaseNotInBase, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.DepositNoChests, FeedbackAction.Deposit, StringKeys.StationActionsDisabledNoChests, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.DepositAlreadyActive, FeedbackAction.Deposit, StringKeys.MessageBaseInProgress, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.DepositLockRefused, FeedbackAction.Deposit, StringKeys.MessageBaseInProgress, OrganizeNoticePlacement.Centre, None),
            Mod(FeedbackOutcome.DepositConfirmRefused, FeedbackAction.Deposit, StringKeys.MessageBaseInProgress, OrganizeNoticePlacement.Centre, None),
            Other(FeedbackOutcome.DepositNoPlayer, FeedbackAction.Deposit, FeedbackLineOwner.NoHud),
            Other(FeedbackOutcome.DepositWorldGone, FeedbackAction.Deposit, FeedbackLineOwner.NoHud),
            Mod(FeedbackOutcome.FavouriteAdded, FeedbackAction.Favourite, StringKeys.MessageFavouriteOn, OrganizeNoticePlacement.TopLeft, None),
            Mod(FeedbackOutcome.FavouriteRemoved, FeedbackAction.Favourite, StringKeys.MessageFavouriteOff, OrganizeNoticePlacement.TopLeft, None),
        ]);
    }
}
