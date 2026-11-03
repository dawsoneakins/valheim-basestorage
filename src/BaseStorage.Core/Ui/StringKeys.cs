namespace BaseStorage.Core.Ui;

/// <summary>One constant per key of the English strings table; plural families use their base key.</summary>
public static class StringKeys
{
    /// <summary>Plural base key of the chest hover line naming the stations in its base.</summary>
    public const string HoverChestInBase = "hover.chest.in-base";

    /// <summary>Chest hover line for a chest that is not in a base.</summary>
    public const string HoverChestNotInBase = "hover.chest.not-in-base";

    /// <summary>Chest hover line segment naming the chest category.</summary>
    public const string HoverChestCategory = "hover.chest.category";

    /// <summary>Chest hover line segment hinting at the category cycle key.</summary>
    public const string HoverChestCycleHint = "hover.chest.cycle-hint";

    /// <summary>Plural base key of the station hover line naming the chests in its base.</summary>
    public const string HoverStationBaseChests = "hover.station.base-chests";

    /// <summary>Label of the organize button on the station panel.</summary>
    public const string StationActionsOrganizeButton = "station.actions.organize-button";

    /// <summary>Label of the deposit button on the station panel.</summary>
    public const string StationActionsDepositButton = "station.actions.deposit-button";

    /// <summary>Base key of the station panel status line; it has text of its own and plural variants.</summary>
    public const string StationActionsStatusLine = "station.actions.status-line";

    /// <summary>Status line suffix naming how full the base is.</summary>
    public const string StationActionsStatusLineFill = "station.actions.status-line-fill";

    /// <summary>Reason shown while another player is organizing.</summary>
    public const string StationActionsDisabledInProgress = "station.actions.disabled-in-progress";

    /// <summary>Reason shown when the base has no chests.</summary>
    public const string StationActionsDisabledNoChests = "station.actions.disabled-no-chests";

    /// <summary>Category label for an automatically categorised chest.</summary>
    public const string ChestCategoryAuto = "chest.category.auto";

    /// <summary>Category label for a chest that is left alone.</summary>
    public const string ChestCategoryKeep = "chest.category.keep";

    /// <summary>Tooltip of the chest category button.</summary>
    public const string ChestCategoryTooltip = "chest.category.tooltip";

    /// <summary>Header of the organize preview.</summary>
    public const string OrganizePreviewHeader = "organize.preview.header";

    /// <summary>Header of the deposit preview.</summary>
    public const string DepositPreviewHeader = "deposit.preview.header";

    /// <summary>Plural base key of the organize preview clause for stacks to move.</summary>
    public const string OrganizePreviewBodyMove = "organize.preview.body.move";

    /// <summary>Plural base key of the organize preview clause for chests to claim.</summary>
    public const string OrganizePreviewBodyClaim = "organize.preview.body.claim";

    /// <summary>Plural base key of the organize preview clause for stacks with no room.</summary>
    public const string OrganizePreviewBodyNoRoom = "organize.preview.body.no-room";

    /// <summary>Plural base key of the organize preview clause for skipped chests.</summary>
    public const string OrganizePreviewBodySkipped = "organize.preview.body.skipped";

    /// <summary>Notice summarising a finished organize.</summary>
    public const string MessageOrganizeSummary = "message.organize.summary";

    /// <summary>Notice summarising a finished deposit.</summary>
    public const string MessageDepositSummary = "message.deposit.summary";

    /// <summary>Notice shown when an organize is cancelled.</summary>
    public const string MessageOrganizeCancelled = "message.organize.cancelled";

    /// <summary>Notice shown when an organize stops early.</summary>
    public const string MessageOrganizeStopped = "message.organize.stopped";

    /// <summary>Notice shown when crafting materials were moved.</summary>
    public const string MessageCraftMaterialsMoved = "message.craft.materials-moved";

    /// <summary>Notice shown when the player is not in a base.</summary>
    public const string MessageBaseNotInBase = "message.base.not-in-base";

    /// <summary>Notice shown while another player is organizing.</summary>
    public const string MessageBaseInProgress = "message.base.in-progress";

    /// <summary>Notice confirming a chest category change.</summary>
    public const string MessageCategorySet = "message.category.set";

    /// <summary>Notice confirming an item was marked as favourite.</summary>
    public const string MessageFavouriteOn = "message.favourite.on";

    /// <summary>Notice confirming an item is no longer a favourite.</summary>
    public const string MessageFavouriteOff = "message.favourite.off";

    /// <summary>Notice shown when host and client versions differ.</summary>
    public const string NetworkVersionMismatch = "network.version.mismatch";

    /// <summary>Organize preview line: the header followed by the body (S-E004-009).</summary>
    public const string OrganizePreviewLine = "organize.preview.line";

    /// <summary>Organize preview sentence shown when the plan has no moves.</summary>
    public const string OrganizePreviewBodyNothing = "organize.preview.body.nothing";

    /// <summary>Plural base key of the organize preview clause for chests skipped for a reason other than a named player.</summary>
    public const string OrganizePreviewBodySkippedReason = "organize.preview.body.skipped-reason";

    /// <summary>Skip reason fragment naming the players who have chests open.</summary>
    public const string OrganizeSkipReasonInUse = "organize.skip-reason.in-use";

    /// <summary>Skip reason fragment for chests in use by players whose names are unknown.</summary>
    public const string OrganizeSkipReasonInUseUnknown = "organize.skip-reason.in-use-unknown";

    /// <summary>Skip reason fragment for Keep chests.</summary>
    public const string OrganizeSkipReasonKeep = "organize.skip-reason.keep";

    /// <summary>Skip reason fragment for chests that are not eligible.</summary>
    public const string OrganizeSkipReasonIneligible = "organize.skip-reason.ineligible";

    /// <summary>Skip reason fragment for chests with unclear contents.</summary>
    public const string OrganizeSkipReasonInvalidContents = "organize.skip-reason.invalid-contents";

    /// <summary>Separator between player names in a skip reason.</summary>
    public const string OrganizeNamesJoin = "organize.names-join";

    /// <summary>Plural base key of the end-of-run summary fragment for stacks moved.</summary>
    public const string MessageOrganizeSummaryMoved = "message.organize.summary-moved";

    /// <summary>End-of-run summary fragment when no stack moved.</summary>
    public const string MessageOrganizeSummaryMovedNone = "message.organize.summary-moved-none";

    /// <summary>Plural base key of the end-of-run summary fragment for chests skipped for one reason.</summary>
    public const string MessageOrganizeSummarySkipped = "message.organize.summary-skipped";

    /// <summary>Plural base key of the end-of-run summary fragment for stacks that had no room.</summary>
    public const string MessageOrganizeSummaryNoRoom = "message.organize.summary-no-room";

    /// <summary>End-of-run summary fragment for a run that stopped early with a reason.</summary>
    public const string MessageOrganizeSummaryStopped = "message.organize.summary-stopped";

    /// <summary>End-of-run summary fragment for a run that stopped early without a reason.</summary>
    public const string MessageOrganizeSummaryStoppedNoReason = "message.organize.summary-stopped-no-reason";

    /// <summary>Separator between the fragments of the end-of-run summary line.</summary>
    public const string MessageOrganizeSummaryJoin = "message.organize.summary-join";

    /// <summary>Label used when a player's name is empty or unknown.</summary>
    public const string PlayerUnknown = "player.unknown";

    /// <summary>Notice shown when a chest category change was refused; the argument is the reason fragment (S-E004-003).</summary>
    public const string MessageCategoryRefused = "message.category.refused";

    /// <summary>Reason fragment: vanilla access denies the player or the owner refused.</summary>
    public const string CategoryRefusalDenied = "category.refusal.denied";

    /// <summary>Reason fragment naming the player who has the chest open.</summary>
    public const string CategoryRefusalInUse = "category.refusal.in-use";

    /// <summary>Reason fragment: the chest owner did not answer in time.</summary>
    public const string CategoryRefusalTimeout = "category.refusal.timeout";

    /// <summary>Reason fragment: the chest is gone, busy or the world is not available.</summary>
    public const string CategoryRefusalUnavailable = "category.refusal.unavailable";

    /// <summary>Notice shown to the host when a joining player has no BaseStorage payload.</summary>
    public const string NetworkVersionMissing = "network.version.missing";

    /// <summary>Label of the Category button of the open-chest panel; the argument is the current value's label (S-E004-004).</summary>
    public const string ChestCategoryButton = "chest.category.button";

    /// <summary>Notice carrying the composed end-of-run Organize summary line (S-E004-011).</summary>
    public const string MessageOrganizeRunSummary = "message.organize.run-summary";

    /// <summary>Stop reason: a chest refused the reservation.</summary>
    public const string OrganizeStopReasonReservationRefused = "organize.stop-reason.reservation-refused";

    /// <summary>Stop reason: a chest owner did not answer in time.</summary>
    public const string OrganizeStopReasonReservationTimedOut = "organize.stop-reason.reservation-timed-out";

    /// <summary>Stop reason: a chest changed owner during the run.</summary>
    public const string OrganizeStopReasonReservationLost = "organize.stop-reason.reservation-lost";

    /// <summary>Stop reason: a chest is no longer available.</summary>
    public const string OrganizeStopReasonChestUnavailable = "organize.stop-reason.chest-unavailable";

    /// <summary>Stop reason: a chest changed during the run.</summary>
    public const string OrganizeStopReasonChestChanged = "organize.stop-reason.chest-changed";

    /// <summary>Stop reason: a destination chest was full.</summary>
    public const string OrganizeStopReasonDestinationFull = "organize.stop-reason.destination-full";

    /// <summary>Stop reason: a confirmed add could not be taken back.</summary>
    public const string OrganizeStopReasonRollbackFailed = "organize.stop-reason.rollback-failed";

    /// <summary>Stop reason: the run was cancelled.</summary>
    public const string OrganizeStopReasonAborted = "organize.stop-reason.aborted";

    /// <summary>Stop reason: the base lock was lost or taken over.</summary>
    public const string OrganizeStopReasonLockLost = "organize.stop-reason.lock-lost";

    /// <summary>Notice shown when a previewed Deposit is cancelled (S-E004-015).</summary>
    public const string MessageDepositCancelled = "message.deposit.cancelled";

    /// <summary>Notice shown when a Deposit stopped early; {0} moved, {1} no room, {2} the stop reason (S-E004-015).</summary>
    public const string MessageDepositSummaryStopped = "message.deposit.summary-stopped";

    /// <summary>Notice shown when a craft or build used materials from base chests (S-E008-001).</summary>
    public const string MessageCraftUsedBase = "message.craft.used-base";

    /// <summary>Console reply when an Organize started without a preview (S-E010-002).</summary>
    public const string ConsoleOrganizeStarted = "console.organize.started";

    /// <summary>Console reply when the Organize preview is open (S-E010-002).</summary>
    public const string ConsoleOrganizePreview = "console.organize.preview";

    /// <summary>Console reply when there is no local player (S-E010-002).</summary>
    public const string ConsoleOrganizeNoPlayer = "console.organize.no-player";

    /// <summary>Console usage line of the basestorage command (S-E010-002).</summary>
    public const string ConsoleBasestorageUsage = "console.basestorage.usage";

    /// <summary>Console description of the basestorage command shown by help (S-E010-002).</summary>
    public const string ConsoleBasestorageDescription = "console.basestorage.description";

    /// <summary>The plural-key prefix of the preview sentence that counts the chests that will get a category (S-A-003).</summary>
    public const string OrganizePreviewBodyRules = "organize.preview.body.rules";

    /// <summary>The preview reason when no usable chest has a free slot (S-A-003).</summary>
    public const string OrganizePreviewBodyReasonFull = "organize.preview.body.reason.full";

    /// <summary>The preview reason when free slots exist but not in the categories that need them (S-A-003).</summary>
    public const string OrganizePreviewBodyReasonCategory = "organize.preview.body.reason.category";

    /// <summary>The category line of an item tooltip (S-B-001): {0} is the category name.</summary>
    public const string TooltipCategory = "tooltip.category";

    /// <summary>The BaseStorage button and menu title (S-C-001).</summary>
    public const string MenuButton = "menu.button";

    /// <summary>The menu close button.</summary>
    public const string MenuClose = "menu.close";

    /// <summary>The menu header: {0} stations, {1} chests, {2} percent full.</summary>
    public const string MenuHeader = "menu.header";

    /// <summary>One chest row: {0} percent full, {1} metres away, {2} a state word.</summary>
    public const string MenuRow = "menu.row";

    /// <summary>The row state of a chest another player has open.</summary>
    public const string MenuInUse = "menu.in-use";

    /// <summary>The row state of a chest the player may not open.</summary>
    public const string MenuNoAccess = "menu.no-access";

    /// <summary>The Take all button of a chest row.</summary>
    public const string MenuTakeAll = "menu.take-all";

    /// <summary>The Place stacks button of a chest row.</summary>
    public const string MenuPlaceStacks = "menu.place-stacks";

    /// <summary>The hint at the end of a chest row that it opens the chest.</summary>
    public const string MenuOpen = "menu.open";

    /// <summary>The menu button that makes the open chest glow.</summary>
    public const string MenuHighlight = "menu.highlight";

    /// <summary>The menu button that shows the open chest to the other players.</summary>
    public const string MenuShare = "menu.share";

    /// <summary>The hover hint that opens a station's feed panel.</summary>
    public const string FeedHover = "feed.hover";

    /// <summary>Label of the Sort button of the open-chest panel.</summary>
    public const string ChestSort = "chest.sort";

    /// <summary>Label of the Name button of the open-chest panel.</summary>
    public const string ChestName = "chest.name";

    /// <summary>Title of the text entry that names a chest.</summary>
    public const string ChestNameTopic = "chest.name.topic";

    /// <summary>Notice after a chest is sorted; the argument is the stack count.</summary>
    public const string MessageChestSorted = "message.chest.sorted";

    /// <summary>Notice when a chest could not be held for sorting.</summary>
    public const string MessageChestSortBusy = "message.chest.sort-busy";

    /// <summary>Hint shown in panels while a gamepad is in use.</summary>
    public const string MenuGamepadHint = "menu.gamepad.hint";

    /// <summary>Title of the text entry that fills the search box.</summary>
    public const string MenuSearchTopic = "menu.search.topic";

    /// <summary>Line on a station that is kept fed.</summary>
    public const string FeedKept = "feed.kept";

    /// <summary>Feed panel status of a station that only burns fuel.</summary>
    public const string FeedStatusFuelOnly = "feed.status.fuel-only";

    /// <summary>The feed panel's item column.</summary>
    public const string FeedHeaderItem = "feed.header.item";

    /// <summary>The feed panel's on and off column.</summary>
    public const string FeedHeaderFed = "feed.header.fed";

    /// <summary>The feed panel's reserve column.</summary>
    public const string FeedHeaderKeep = "feed.header.keep";

    /// <summary>What a station holds, for a station that burns fuel.</summary>
    public const string FeedStatus = "feed.status";

    /// <summary>What a station holds, for a station that burns nothing.</summary>
    public const string FeedStatusNoFuel = "feed.status.no-fuel";

    /// <summary>How many of an item the base holds.</summary>
    public const string FeedInBase = "feed.in-base";

    /// <summary>A station is kept fed with this item.</summary>
    public const string FeedOn = "feed.on";

    /// <summary>A station is not kept fed with this item.</summary>
    public const string FeedOff = "feed.off";

    /// <summary>Lowers the amount left in the base.</summary>
    public const string FeedLess = "feed.less";

    /// <summary>Raises the amount left in the base.</summary>
    public const string FeedMore = "feed.more";

    /// <summary>The menu button that makes a left click mark favourites.</summary>
    public const string MenuMark = "menu.mark";

    /// <summary>The placeholder of the menu's search box.</summary>
    public const string MenuSearch = "menu.search";

    /// <summary>The menu header during a search.</summary>
    public const string MenuSearchHeader = "menu.search.header";

    /// <summary>One matching item in a chest.</summary>
    public const string MenuHit = "menu.hit";

    /// <summary>One matching item, and how many other items match.</summary>
    public const string MenuHitMore = "menu.hit.more";

    /// <summary>The search header when one item was found, before the chest count.</summary>
    public const string MenuSearchHeaderOne = "menu.search.header.single";

    /// <summary>Key base (plural form appended) of the chest count in the search header.</summary>
    public const string MenuSearchChests = "menu.search.chests";

    /// <summary>The list's message when a search finds nothing.</summary>
    public const string MenuSearchNone = "menu.search.none";

    /// <summary>The search header when a search finds nothing.</summary>
    public const string MenuSearchNoneHeader = "menu.search.none.header";

    /// <summary>The label of the button that empties the search box.</summary>
    public const string MenuSearchClear = "menu.search.clear";

    /// <summary>A chest's distance on a search result.</summary>
    public const string MenuDistance = "menu.distance";

    /// <summary>Key base (plural form appended) of the preview clause for stacks going to a related chest.</summary>
    public const string OrganizePreviewBodySpill = "organize.preview.body.spill";

    /// <summary>The tooltip line of a favourite item.</summary>
    public const string TooltipFavourite = "tooltip.favourite";

    /// <summary>The label of the Restock button.</summary>
    public const string MenuRestock = "menu.restock";

    /// <summary>The Restock result line notice; the one argument is the composed line.</summary>
    public const string MessageRestockSummary = "message.restock.summary";

    /// <summary>Key base (two plural forms appended) of the Restock moved head: stacks, then items.</summary>
    public const string MessageRestockMoved = "message.restock.moved";

    /// <summary>The Restock line when there was nothing to move and nothing left behind.</summary>
    public const string MessageRestockNothing = "message.restock.nothing";

    /// <summary>The Restock line head when nothing moved but items were left or the run stopped.</summary>
    public const string MessageRestockNothingMoved = "message.restock.nothing-moved";

    /// <summary>Key base (plural form appended) of the items left behind part of the Restock line.</summary>
    public const string MessageRestockLeftBehind = "message.restock.left-behind";

    /// <summary>The Restock line part for a run that stopped early.</summary>
    public const string MessageRestockStopped = "message.restock.stopped";

    /// <summary>The notice when the player's inventory could not be read for a Restock.</summary>
    public const string MessageRestockCaptureFailed = "message.restock.capture-failed";
}
