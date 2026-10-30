namespace BaseStorage.Core.Tests.Ui;

using System;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Ui;

/// <summary>
/// The S-E008-001 feedback oracle: contracts/feedback-audit-contracts.md v1 sections 2.2, 2.4, 3.3 and 6.1, copied verbatim.
/// Typed as test-side literals, so the catalog is never its own oracle (ADR-GWIN-153). A fixture holder, not a test class.
/// </summary>
internal static class FeedbackFixtures
{
    /// <summary>
    /// One contract 2.2 row. Value is the numeric value of the outcome; Owner, Action and Outcome are the enum member names;
    /// Key and Placement are null for every non-Mod row; Args builds the example arguments; Exact is the example line (Mod rows only).
    /// </summary>
    internal sealed record Row(
        int Value,
        string Outcome,
        string Action,
        string Owner,
        string? Key,
        OrganizeNoticePlacement? Placement,
        string[] Fragments,
        Func<object?[]> Args,
        string? Exact,
        string Trigger);

    private const string Done = "message.craft.used-base";
    private const string Materials = "message.craft.materials-moved";
    private const string CategorySet = "message.category.set";
    private const string CategoryRefused = "message.category.refused";
    private const string OrganizeSummary = "message.organize.run-summary";
    private const string OrganizeCancelled = "message.organize.cancelled";
    private const string NotInBase = "message.base.not-in-base";
    private const string NoChests = "station.actions.disabled-no-chests";
    private const string InProgress = "message.base.in-progress";
    private const string DepositSummary = "message.deposit.summary";
    private const string DepositStopped = "message.deposit.summary-stopped";
    private const string DepositCancelled = "message.deposit.cancelled";
    private const string FavouriteOn = "message.favourite.on";
    private const string FavouriteOff = "message.favourite.off";

    private static readonly string[] NoFragments = [];

    /// <summary>Contract 2.4: the 13 Organize summary keys, in order (rows 30 and 31).</summary>
    private static readonly string[] SummaryFragments =
    [
        "message.organize.summary-moved",
        "message.organize.summary-moved-none",
        "message.organize.summary-skipped",
        "message.organize.summary-no-room",
        "message.organize.summary-stopped",
        "message.organize.summary-stopped-no-reason",
        "message.organize.summary-join",
        "organize.skip-reason.in-use",
        "organize.skip-reason.in-use-unknown",
        "organize.skip-reason.keep",
        "organize.skip-reason.ineligible",
        "organize.skip-reason.invalid-contents",
        "organize.names-join",
    ];

    /// <summary>Contract 2.4: the 9 stop-reason keys, in StringKeys order (row 31 appends them, row 51 lists them only).</summary>
    private static readonly string[] StopReasonKeys =
    [
        "organize.stop-reason.reservation-refused",
        "organize.stop-reason.reservation-timed-out",
        "organize.stop-reason.reservation-lost",
        "organize.stop-reason.chest-unavailable",
        "organize.stop-reason.chest-changed",
        "organize.stop-reason.destination-full",
        "organize.stop-reason.rollback-failed",
        "organize.stop-reason.aborted",
        "organize.stop-reason.lock-lost",
    ];

    private static readonly string[] OrganizeStoppedFragments = [.. SummaryFragments, .. StopReasonKeys];

    private static Func<object?[]> Values(params object?[] values) => () => values;

    private static Row Mod(int value, string outcome, string action, string key, OrganizeNoticePlacement placement, string[] fragments, Func<object?[]> args, string exact, string trigger) =>
        new(value, outcome, action, "Mod", key, placement, fragments, args, exact, trigger);

    private static Row Other(int value, string outcome, string action, string owner, string trigger) =>
        new(value, outcome, action, owner, null, null, NoFragments, Values(), null, trigger);

    /// <summary>The 43 rows of contract 2.2 in ascending numeric order, with the 6.1 trigger of each.</summary>
    internal static readonly Row[] Rows =
    [
        Mod(0, "CraftCreated", "Craft", Done, OrganizeNoticePlacement.TopLeft, NoFragments, Values(), "Used materials from base chests",
            "Solo: at a Workbench in a base, keep the Wood for a recipe only in a base chest and craft it. Then craft five more quickly; the line must not pile up."),
        Mod(1, "CraftReturned", "Craft", Materials, OrganizeNoticePlacement.Centre, NoFragments, Values(), "Materials moved, try again",
            "Unit test only: craft-from-base S10 (vanilla declines at commit)."),
        Mod(2, "CraftFailed", "Craft", Materials, OrganizeNoticePlacement.Centre, NoFragments, Values(), "Materials moved, try again",
            "Host plus one friend: as Craft from base, Host plus one friend, step 3 (the host takes the Wood during the wide reservation window)."),
        Mod(3, "CraftPreFlightFailed", "Craft", Materials, OrganizeNoticePlacement.Centre, NoFragments, Values(), "Materials moved, try again",
            "Unit test only: craft-from-base S7 (pre-flight at start)."),
        Mod(4, "CraftUnavailable", "Craft", Materials, OrganizeNoticePlacement.Centre, NoFragments, Values(), "Materials moved, try again",
            "Unit test only: craft-from-base S5 (no plan at craft time)."),
        Other(5, "CraftAbandoned", "Craft", "Superseded", "Unit test only: craft-from-base S8 (panel closed while waiting)."),

        Mod(10, "BuildPlaced", "Build", Done, OrganizeNoticePlacement.TopLeft, NoFragments, Values(), "Used materials from base chests",
            "Solo: in a base, keep the Wood only in a base chest and place ten wood floor pieces quickly with the Hammer; one line, not ten stacked."),
        Other(11, "BuildReturned", "Build", "Vanilla", "Unit test only: build-from-base T12 (vanilla declines after the take)."),
        Mod(12, "BuildFailed", "Build", Materials, OrganizeNoticePlacement.Centre, NoFragments, Values(), "Materials moved, try again",
            "Host plus one friend: as Build from base, Host plus one friend, step 3."),
        Mod(13, "BuildPreFlightFailed", "Build", Materials, OrganizeNoticePlacement.Centre, NoFragments, Values(), "Materials moved, try again",
            "Unit test only: build-from-base T11 (pre-flight while waiting)."),
        Mod(14, "BuildUnavailable", "Build", Materials, OrganizeNoticePlacement.Centre, NoFragments, Values(), "Materials moved, try again",
            "Unit test only: build-from-base T5 (no plan at place time)."),
        Other(15, "BuildAbandoned", "Build", "Superseded", "Unit test only: build-from-base T8 (place mode left while waiting)."),
        Other(16, "BuildPlacementRefused", "Build", "Vanilla",
            "Solo: in a base, with the Wood only in a base chest, try to place a piece where the ghost is red (overlapping another piece)."),

        Mod(20, "CategoryApplied", "CategoryChange", CategorySet, OrganizeNoticePlacement.TopLeft, ["chest.category.auto", "chest.category.keep"],
            Values("Metals & Ores"), "Chest category: Metals & Ores",
            "Solo: open a base chest and click the Category button until it shows `Metals & Ores`."),
        Mod(21, "CategoryDenied", "CategoryChange", CategoryRefused, OrganizeNoticePlacement.Centre, ["category.refusal.denied"],
            Values("no access to this chest"), "Chest category not changed: no access to this chest",
            "Host plus one friend: the friend binds `CycleCategoryKey`, hovers a chest inside a ward the friend is not permitted on, and presses the key."),
        Mod(22, "CategoryInUse", "CategoryChange", CategoryRefused, OrganizeNoticePlacement.Centre, ["category.refusal.in-use", "player.unknown"],
            Values("in use by Erik"), "Chest category not changed: in use by Erik",
            "Host plus one friend: the host (Erik) keeps a chest open; the friend hovers it and presses `CycleCategoryKey`."),
        Mod(23, "CategoryTimeout", "CategoryChange", CategoryRefused, OrganizeNoticePlacement.Centre, ["category.refusal.timeout"],
            Values("no answer from the chest owner"), "Chest category not changed: no answer from the chest owner",
            "Unit test only: chest-category-contracts CC9 (the owner never answers)."),
        Mod(24, "CategoryUnavailable", "CategoryChange", CategoryRefused, OrganizeNoticePlacement.Centre, ["category.refusal.unavailable"],
            Values("the chest is not available"), "Chest category not changed: the chest is not available",
            "Unit test only: chest-category-contracts, no reservation route (`Begin` returns null)."),
        Mod(25, "CategoryUnchanged", "CategoryChange", CategorySet, OrganizeNoticePlacement.TopLeft, ["chest.category.auto", "chest.category.keep"],
            Values("Keep"), "Chest category: Keep",
            "Unit test only: FA4 (the chest already holds the requested value)."),
        Other(26, "CategoryAlreadyPending", "CategoryChange", "Coalesced",
            "Host plus one friend: the friend presses `CycleCategoryKey` twice quickly on a host-owned chest; only the first request's line appears."),
        Other(27, "CategoryChestGone", "CategoryChange", "TargetGone", "Unit test only: FS6 source fact (null chest or invalid view)."),

        Mod(30, "OrganizeCompleted", "Organize", OrganizeSummary, OrganizeNoticePlacement.TopLeft, SummaryFragments,
            () => new object?[] { OrganizeSummaryFormatter.Summary(new OrganizeRunReport(37, Array.Empty<OrganizeSkipEntry>(), 4, false, null)) },
            "Organized 37 stacks; 4 stacks had no room",
            "Solo: at a station in a base with stacks to move, click Organize base and confirm."),
        Mod(31, "OrganizeStopped", "Organize", OrganizeSummary, OrganizeNoticePlacement.TopLeft, OrganizeStoppedFragments,
            () => new object?[] { OrganizeSummaryFormatter.Summary(new OrganizeRunReport(12, Array.Empty<OrganizeSkipEntry>(), 0, true, "chest full")) },
            "Organized 12 stacks; stopped early: chest full",
            "Unit test only: organize-runner section 8 row 9 (destination full)."),
        Mod(32, "OrganizeCancelled", "Organize", OrganizeCancelled, OrganizeNoticePlacement.TopLeft, NoFragments, Values(), "Organize cancelled",
            "Solo: click Organize base, then No in the preview."),
        Mod(33, "OrganizeNotInBase", "Organize", NotInBase, OrganizeNoticePlacement.Centre, NoFragments, Values(), "Not in a base",
            "Solo: bind `OrganizeKey`, stand outside every base and press it."),
        Mod(34, "OrganizeNoChests", "Organize", NoChests, OrganizeNoticePlacement.Centre, NoFragments, Values(), "No chests in this base",
            "Solo: bind `OrganizeKey`, stand in a base whose chests are all Keep, and press it."),
        Mod(35, "OrganizeAlreadyActive", "Organize", InProgress, OrganizeNoticePlacement.Centre, NoFragments, Values("Erik"), "Organize in progress by Erik",
            "Unit test only: organize-runner section 8 row 8 (Start while a run is active)."),
        Mod(36, "OrganizeLockRefused", "Organize", InProgress, OrganizeNoticePlacement.Centre, NoFragments, Values("Astrid"), "Organize in progress by Astrid",
            "Host plus one friend: Astrid (the friend) opens an Organize preview; the host binds `OrganizeKey`, stands in the same base and presses it."),
        Mod(37, "OrganizeConfirmRefused", "Organize", InProgress, OrganizeNoticePlacement.Centre, NoFragments, Values("Astrid"), "Organize in progress by Astrid",
            "Unit test only: organize-runner section 8 row 13 (Confirm with the lock taken meanwhile)."),
        Other(38, "OrganizeNoPlayer", "Organize", "NoHud", "Unit test only: organize-runner row 2 (no player)."),
        Other(39, "OrganizeWorldGone", "Organize", "NoHud", "Unit test only: organize-runner row 11 (world gone)."),

        Mod(50, "DepositCompleted", "Deposit", DepositSummary, OrganizeNoticePlacement.TopLeft, NoFragments, Values(4, 2), "Put away: 4 stacks moved, 2 no room",
            "Solo: at a station in a base, with stacks in the inventory, click Put away inventory (confirm if the preview shows)."),
        Mod(51, "DepositStopped", "Deposit", DepositStopped, OrganizeNoticePlacement.TopLeft, StopReasonKeys, Values(0, 0, "chest full"),
            "Put away: 0 stacks moved, 0 no room; stopped early: chest full",
            "Unit test only: GW-U-1911 (destination full)."),
        Mod(52, "DepositCancelled", "Deposit", DepositCancelled, OrganizeNoticePlacement.TopLeft, NoFragments, Values(), "Put away cancelled",
            "Solo: with `DepositPreview = true`, click Put away inventory, then No."),
        Mod(53, "DepositNotInBase", "Deposit", NotInBase, OrganizeNoticePlacement.Centre, NoFragments, Values(), "Not in a base",
            "Solo: bind `DepositKey`, stand outside every base and press it."),
        Mod(54, "DepositNoChests", "Deposit", NoChests, OrganizeNoticePlacement.Centre, NoFragments, Values(), "No chests in this base",
            "Solo: bind `DepositKey`, stand in a base whose chests are all Keep, and press it."),
        Mod(55, "DepositAlreadyActive", "Deposit", InProgress, OrganizeNoticePlacement.Centre, NoFragments, Values("Erik"), "Organize in progress by Erik",
            "Unit test only: GW-U-1913 (already active)."),
        Mod(56, "DepositLockRefused", "Deposit", InProgress, OrganizeNoticePlacement.Centre, NoFragments, Values("Astrid"), "Organize in progress by Astrid",
            "Host plus one friend: Astrid opens an Organize preview; the host binds `DepositKey`, stands in the same base and presses it."),
        Mod(57, "DepositConfirmRefused", "Deposit", InProgress, OrganizeNoticePlacement.Centre, NoFragments, Values("Astrid"), "Organize in progress by Astrid",
            "Unit test only: GW-U-1915."),
        Other(58, "DepositNoPlayer", "Deposit", "NoHud", "Unit test only: GW-U-1901, GW-U-1902."),
        Other(59, "DepositWorldGone", "Deposit", "NoHud", "Unit test only: GW-U-1916."),

        Mod(70, "FavouriteAdded", "Favourite", FavouriteOn, OrganizeNoticePlacement.TopLeft, NoFragments, Values("Wood"), "Favourite: Wood",
            "Solo: hold Alt and left-click a Wood stack in the inventory."),
        Mod(71, "FavouriteRemoved", "Favourite", FavouriteOff, OrganizeNoticePlacement.TopLeft, NoFragments, Values("Wood"), "No longer favourite: Wood",
            "Solo: hold Alt and left-click the same Wood stack again."),
    ];
}
