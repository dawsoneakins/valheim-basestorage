namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Collections.Generic;

/// <summary>
/// Expected values for the S-FOUND-001 strings table and formatter, copied verbatim from
/// contracts/ui-strings-contract.md v1 (sections 2.3, 3.2, 4 and 5.3). This is an independent copy:
/// tests never read expected text from BaseStorage.Core (ADR-GWIN-011). A fixture holder, not a test class.
/// </summary>
public static class UiFixtures
{
    /// <summary>Contract 3.2: the keys and their exact English text, in table order (row 64 is S-E005-002 section 2.6).</summary>
    public static readonly (string Key, string Text)[] EnglishTable =
    [
        ("hover.chest.in-base.one", "[In base: 1 station]"),
        ("hover.chest.in-base.other", "[In base: {0} stations]"),
        ("hover.chest.not-in-base", "[Not in a base]"),
        ("hover.chest.category", "[{0}]"),
        ("hover.chest.cycle-hint", "[<color=yellow><b>{0}</b></color>] Category"),
        ("hover.station.base-chests.one", "[Base: 1 chest]"),
        ("hover.station.base-chests.other", "[Base: {0} chests]"),
        ("station.actions.organize-button", "Organize base"),
        ("station.actions.deposit-button", "Put away inventory"),
        ("station.actions.status-line", "Base: <color=orange>{0}</color> stations, <color=orange>{1}</color> chests"),
        ("station.actions.status-line.one.one", "Base: <color=orange>1</color> station, <color=orange>1</color> chest"),
        ("station.actions.status-line.one.other", "Base: <color=orange>1</color> station, <color=orange>{1}</color> chests"),
        ("station.actions.status-line.other.one", "Base: <color=orange>{0}</color> stations, <color=orange>1</color> chest"),
        ("station.actions.status-line.other.other", "Base: <color=orange>{0}</color> stations, <color=orange>{1}</color> chests"),
        ("station.actions.status-line-fill", ", <color=orange>{0}%</color> full"),
        ("station.actions.disabled-in-progress", "Organize in progress by {0}"),
        ("station.actions.disabled-no-chests", "No chests in this base"),
        ("chest.category.auto", "Auto"),
        ("chest.category.keep", "Keep"),
        ("chest.category.tooltip", "Category. Left-click: next. Right-click: previous."),
        ("organize.preview.header", "Organize base"),
        ("deposit.preview.header", "Put away inventory"),
        ("organize.preview.body.move.one.one", "1 stack to move across 1 chest."),
        ("organize.preview.body.move.one.other", "1 stack to move across {1} chests."),
        ("organize.preview.body.move.other.one", "{0} stacks to move across 1 chest."),
        ("organize.preview.body.move.other.other", "{0} stacks to move across {1} chests."),
        ("organize.preview.body.claim.one", "1 chest will be claimed for {3}."),
        ("organize.preview.body.claim.other", "{2} chests will be claimed for {3}."),
        ("organize.preview.body.no-room.one", "1 stack has no room."),
        ("organize.preview.body.no-room.other", "{4} stacks have no room."),
        ("organize.preview.body.skipped.one", "1 chest skipped (in use by {6})."),
        ("organize.preview.body.skipped.other", "{5} chests skipped (in use by {6})."),
        ("message.organize.summary", "Organized: {0} stacks moved, {1} skipped ({2}), {3} no room"),
        ("message.deposit.summary", "Put away: {0} stacks moved, {1} no room"),
        ("message.organize.cancelled", "Organize cancelled"),
        ("message.organize.stopped", "Organize stopped: {0}. Nothing was lost."),
        ("message.craft.materials-moved", "Materials moved, try again"),
        ("message.base.not-in-base", "Not in a base"),
        ("message.base.in-progress", "Organize in progress by {0}"),
        ("message.category.set", "Chest category: {0}"),
        ("message.favourite.on", "Favourite: {0}"),
        ("message.favourite.off", "No longer favourite: {0}"),
        ("network.version.mismatch", "Don's QOL version mismatch: host {0}, you {1}"),
        // Contract section 10 (S-E004-009): preview composer and end-of-run summary keys, appended in table order.
        ("organize.preview.line", "{0}: {1}"),
        ("organize.preview.body.nothing", "Nothing to move."),
        ("organize.preview.body.skipped-reason.one", "1 chest skipped ({6})."),
        ("organize.preview.body.skipped-reason.other", "{5} chests skipped ({6})."),
        ("organize.skip-reason.in-use", "in use by {0}"),
        ("organize.skip-reason.in-use-unknown", "in use"),
        ("organize.skip-reason.keep", "Keep"),
        ("organize.skip-reason.ineligible", "not eligible"),
        ("organize.skip-reason.invalid-contents", "unclear contents"),
        ("organize.names-join", ", "),
        ("message.organize.summary-moved.one", "Organized 1 stack"),
        ("message.organize.summary-moved.other", "Organized {0} stacks"),
        ("message.organize.summary-moved-none", "Nothing moved"),
        ("message.organize.summary-skipped.one", "1 chest skipped ({1})"),
        ("message.organize.summary-skipped.other", "{0} chests skipped ({1})"),
        ("message.organize.summary-no-room.one", "1 stack had no room"),
        ("message.organize.summary-no-room.other", "{0} stacks had no room"),
        ("message.organize.summary-stopped", "stopped early: {0}"),
        ("message.organize.summary-stopped-no-reason", "stopped early"),
        ("message.organize.summary-join", "; "),
        ("player.unknown", "another player"),
        // Contract chest-category-contracts.md section 5 (S-E004-003): the refusal notice and its reason fragments, appended in table order.
        ("message.category.refused", "Chest category not changed: {0}"),
        ("category.refusal.denied", "no access to this chest"),
        ("category.refusal.in-use", "in use by {0}"),
        ("category.refusal.timeout", "no answer from the chest owner"),
        ("category.refusal.unavailable", "the chest is not available"),
        // Contract version-rpc-contracts.md 2.3 (S-E006-002): the host notice for a joining player without the mod.
        ("network.version.missing", "Don's QOL version mismatch: host {0}, joining player has no Don's QOL"),
        // Contract category-button-contracts.md section 5 (S-E004-004): the Category button label, appended in table order.
        ("chest.category.button", "{0}"),
        // Contract organize-runner-contracts.md section 6 (S-E004-011): the summary passthrough and the stop-reason fragments, appended in table order.
        ("message.organize.run-summary", "{0}"),
        ("organize.stop-reason.reservation-refused", "a chest refused the reservation"),
        ("organize.stop-reason.reservation-timed-out", "no answer from a chest owner"),
        ("organize.stop-reason.reservation-lost", "a chest changed owner"),
        ("organize.stop-reason.chest-unavailable", "a chest is no longer available"),
        ("organize.stop-reason.chest-changed", "a chest changed during the run"),
        ("organize.stop-reason.destination-full", "chest full"),
        ("organize.stop-reason.rollback-failed", "items could not be put back, check the log"),
        ("organize.stop-reason.aborted", "cancelled"),
        ("organize.stop-reason.lock-lost", "the base lock was lost"),
        // Edited by S-E004-015 (ADR-GWIN-143): the two Deposit keys of deposit-button contract 5, appended in that order.
        ("message.deposit.cancelled", "Put away cancelled"),
        ("message.deposit.summary-stopped", "Put away: {0} stacks moved, {1} no room; stopped early: {2}"),
        // Edited by S-E008-001 (ADR-GWIN-153): the craft and build success line of contract 4.1, appended last (84 keys).
        ("message.craft.used-base", "Used materials from base chests"),
        // Edited by S-E010-002 (organize-console-command 5): the five console keys, appended in this order (89 keys).
        ("console.organize.started", "Organize started"),
        ("console.organize.preview", "Organize preview open, close the console to confirm"),
        ("console.organize.no-player", "Not in a world"),
        ("console.basestorage.usage", "Usage: dons-qol organize"),
        ("console.basestorage.description", "organize - start an Organize for the base you are in"),
        ("organize.preview.body.rules.one", "1 chest will get a category."),
        ("organize.preview.body.rules.other", "{0} chests will get a category."),
        ("organize.preview.body.reason.full", "This base is full. Free up space or add a chest."),
        ("organize.preview.body.reason.category", "Some categories ran out of space. Add a chest or give one that category."),
        ("tooltip.category", "Category: {0}"),
        ("menu.button", "Don's QOL"),
        ("menu.close", "Close"),
        ("menu.header", "{0} stations, {1} chests, {2}% full"),
        ("menu.row", "{0}%  {1}m {2}"),
        ("menu.in-use", "in use"),
        ("menu.no-access", "no access"),
        ("menu.take-all", "Take all"),
        ("menu.place-stacks", "Place stacks"),
        ("menu.open", "Open"),
        ("menu.highlight", "Highlight"),
        ("menu.share", "Share"),
        ("feed.hover", "Feed from base"),
        ("feed.kept", "Kept fed from the base"),
        ("chest.sort", "Sort"),
        ("chest.name", "Name"),
        ("chest.name.topic", "Name this chest"),
        ("message.chest.sorted", "Sorted: {0} stacks"),
        ("message.chest.sort-busy", "Chest in use, not sorted"),
        ("menu.gamepad.hint", "Right stick: focus   A: select   B: close"),
        ("menu.search.topic", "Search the base"),
        ("feed.status.fuel-only", "Fuel {0} of {1}"),
        ("feed.header.item", "Item"),
        ("feed.header.fed", "Keep fed"),
        ("feed.header.keep", "Leave in base"),
        ("feed.status", "Queued {0}/{1}   Fuel {2}/{3}"),
        ("feed.status.no-fuel", "Queued {0}/{1}"),
        ("feed.in-base", "{0} in base"),
        ("feed.on", "On"),
        ("feed.off", "Off"),
        ("feed.less", "-"),
        ("feed.more", "+"),
        ("menu.mark", "Mark"),
        ("menu.search", "Search the base"),
        ("menu.search.header", "{0} items, {1} in all, in {2}"),
        ("menu.hit", "{0} x{1}"),
        ("menu.hit.more", "{0} +{1} more"),
        ("tooltip.favourite", "Favourite: Put away and Place stacks leave this"),
        // Edited by S-QR-002 (ADR-GWIN-176)
        
        ("menu.restock", "Restock"),
        ("message.restock.summary", "{0}"),
        ("message.restock.moved.one.one", "Restocked 1 stack, 1 item"),
        ("message.restock.moved.one.other", "Restocked 1 stack, {1} items"),
        ("message.restock.moved.other.one", "Restocked {0} stacks, 1 item"),
        ("message.restock.moved.other.other", "Restocked {0} stacks, {1} items"),
        ("message.restock.nothing", "Nothing to restock"),
        ("message.restock.nothing-moved", "Nothing restocked"),
        ("message.restock.left-behind.one", "1 item left behind"),
        ("message.restock.left-behind.other", "{0} items left behind"),
        ("message.restock.stopped", "stopped early"),
        ("message.restock.capture-failed", "Restock failed: could not read your inventory"),
    ];

    /// <summary>The number of StringKeys constants: 31 from contract 3.2, 16 from contract section 10 (S-E004-009), 1 from S-E005-002 (PlayerUnknown), 5 from S-E004-003 (category refusals), 1 from S-E006-002 (NetworkVersionMissing), 1 from S-E004-004 (ChestCategoryButton) and 10 from S-E004-011 (run summary and stop reasons).</summary>
    // Edited by S-E004-015 (ADR-GWIN-143): 65 plus MessageDepositCancelled and MessageDepositSummaryStopped (deposit-button contract 5).
    // Edited by S-E008-001 (ADR-GWIN-153): 68, with MessageCraftUsedBase (contract 4.1).
    // Edited by S-E010-002 (organize-console-command 5): 68 plus the five console keys, 73.
    // Edited by S-A-003: 73 plus the rules key and the two no-room reasons, 76.
    public const int StringKeyCount = 296; // Edited by the base move and Put away selected: 129 plus 25 plus 5

    /// <summary>Contract 3.2 "Const" column (31 constants), contract section 10 (16 more) and S-E005-002 section 2.6 (PlayerUnknown): the StringKeys constants and the value each one holds.</summary>
    public static readonly (string Name, string Value)[] StringKeyValues =
    [
        ("HoverChestInBase", "hover.chest.in-base"),
        ("HoverChestNotInBase", "hover.chest.not-in-base"),
        ("HoverChestCategory", "hover.chest.category"),
        ("HoverChestCycleHint", "hover.chest.cycle-hint"),
        ("HoverStationBaseChests", "hover.station.base-chests"),
        ("StationActionsOrganizeButton", "station.actions.organize-button"),
        ("StationActionsDepositButton", "station.actions.deposit-button"),
        ("StationActionsStatusLine", "station.actions.status-line"),
        ("StationActionsStatusLineFill", "station.actions.status-line-fill"),
        ("StationActionsDisabledInProgress", "station.actions.disabled-in-progress"),
        ("StationActionsDisabledNoChests", "station.actions.disabled-no-chests"),
        ("ChestCategoryAuto", "chest.category.auto"),
        ("ChestCategoryKeep", "chest.category.keep"),
        ("ChestCategoryTooltip", "chest.category.tooltip"),
        ("OrganizePreviewHeader", "organize.preview.header"),
        ("DepositPreviewHeader", "deposit.preview.header"),
        ("OrganizePreviewBodyMove", "organize.preview.body.move"),
        ("OrganizePreviewBodyClaim", "organize.preview.body.claim"),
        ("OrganizePreviewBodyNoRoom", "organize.preview.body.no-room"),
        ("OrganizePreviewBodySkipped", "organize.preview.body.skipped"),
        ("MessageOrganizeSummary", "message.organize.summary"),
        ("MessageDepositSummary", "message.deposit.summary"),
        ("MessageOrganizeCancelled", "message.organize.cancelled"),
        ("MessageOrganizeStopped", "message.organize.stopped"),
        ("MessageCraftMaterialsMoved", "message.craft.materials-moved"),
        ("MessageBaseNotInBase", "message.base.not-in-base"),
        ("MessageBaseInProgress", "message.base.in-progress"),
        ("MessageCategorySet", "message.category.set"),
        ("MessageFavouriteOn", "message.favourite.on"),
        ("MessageFavouriteOff", "message.favourite.off"),
        ("NetworkVersionMismatch", "network.version.mismatch"),
        ("OrganizePreviewLine", "organize.preview.line"),
        ("OrganizePreviewBodyNothing", "organize.preview.body.nothing"),
        ("OrganizePreviewBodyRules", "organize.preview.body.rules"),
        ("OrganizePreviewBodyReasonFull", "organize.preview.body.reason.full"),
        ("OrganizePreviewBodyReasonCategory", "organize.preview.body.reason.category"),
        ("TooltipCategory", "tooltip.category"),
        ("MenuButton", "menu.button"),
        ("MenuClose", "menu.close"),
        ("MenuHeader", "menu.header"),
        ("MenuRow", "menu.row"),
        ("MenuInUse", "menu.in-use"),
        ("MenuNoAccess", "menu.no-access"),
        ("MenuTakeAll", "menu.take-all"),
        ("MenuPlaceStacks", "menu.place-stacks"),
        ("MenuOpen", "menu.open"),
        ("MenuHighlight", "menu.highlight"),
        ("MenuShare", "menu.share"),
        ("FeedHover", "feed.hover"),
        ("FeedKept", "feed.kept"),
        ("ChestSort", "chest.sort"),
        ("ChestName", "chest.name"),
        ("ChestNameTopic", "chest.name.topic"),
        ("MessageChestSorted", "message.chest.sorted"),
        ("MessageChestSortBusy", "message.chest.sort-busy"),
        ("MenuGamepadHint", "menu.gamepad.hint"),
        ("MenuSearchTopic", "menu.search.topic"),
        ("FeedStatusFuelOnly", "feed.status.fuel-only"),
        ("FeedHeaderItem", "feed.header.item"),
        ("FeedHeaderFed", "feed.header.fed"),
        ("FeedHeaderKeep", "feed.header.keep"),
        ("FeedStatus", "feed.status"),
        ("FeedStatusNoFuel", "feed.status.no-fuel"),
        ("FeedInBase", "feed.in-base"),
        ("FeedOn", "feed.on"),
        ("FeedOff", "feed.off"),
        ("FeedLess", "feed.less"),
        ("FeedMore", "feed.more"),
        ("MenuMark", "menu.mark"),
        ("MenuSearch", "menu.search"),
        ("MenuSearchHeader", "menu.search.header"),
        ("MenuHit", "menu.hit"),
        ("MenuHitMore", "menu.hit.more"),
        ("TooltipFavourite", "tooltip.favourite"),
        ("MenuSearchHeaderOne", "menu.search.header.single"),
        ("MenuSearchChests", "menu.search.chests"),
        ("MenuSearchNone", "menu.search.none"),
        ("MenuSearchNoneHeader", "menu.search.none.header"),
        ("MenuSearchClear", "menu.search.clear"),
        ("MenuDistance", "menu.distance"),
        ("OrganizePreviewBodySpill", "organize.preview.body.spill"),
        ("OrganizePreviewBodySkippedReason", "organize.preview.body.skipped-reason"),
        ("OrganizeSkipReasonInUse", "organize.skip-reason.in-use"),
        ("OrganizeSkipReasonInUseUnknown", "organize.skip-reason.in-use-unknown"),
        ("OrganizeSkipReasonKeep", "organize.skip-reason.keep"),
        ("OrganizeSkipReasonIneligible", "organize.skip-reason.ineligible"),
        ("OrganizeSkipReasonInvalidContents", "organize.skip-reason.invalid-contents"),
        ("OrganizeNamesJoin", "organize.names-join"),
        ("MessageOrganizeSummaryMoved", "message.organize.summary-moved"),
        ("MessageOrganizeSummaryMovedNone", "message.organize.summary-moved-none"),
        ("MessageOrganizeSummarySkipped", "message.organize.summary-skipped"),
        ("MessageOrganizeSummaryNoRoom", "message.organize.summary-no-room"),
        ("MessageOrganizeSummaryStopped", "message.organize.summary-stopped"),
        ("MessageOrganizeSummaryStoppedNoReason", "message.organize.summary-stopped-no-reason"),
        ("MessageOrganizeSummaryJoin", "message.organize.summary-join"),
        ("PlayerUnknown", "player.unknown"),
        ("MessageCategoryRefused", "message.category.refused"),
        ("CategoryRefusalDenied", "category.refusal.denied"),
        ("CategoryRefusalInUse", "category.refusal.in-use"),
        ("CategoryRefusalTimeout", "category.refusal.timeout"),
        ("CategoryRefusalUnavailable", "category.refusal.unavailable"),
        ("NetworkVersionMissing", "network.version.missing"),
        ("ChestCategoryButton", "chest.category.button"),
        ("MessageOrganizeRunSummary", "message.organize.run-summary"),
        ("OrganizeStopReasonReservationRefused", "organize.stop-reason.reservation-refused"),
        ("OrganizeStopReasonReservationTimedOut", "organize.stop-reason.reservation-timed-out"),
        ("OrganizeStopReasonReservationLost", "organize.stop-reason.reservation-lost"),
        ("OrganizeStopReasonChestUnavailable", "organize.stop-reason.chest-unavailable"),
        ("OrganizeStopReasonChestChanged", "organize.stop-reason.chest-changed"),
        ("OrganizeStopReasonDestinationFull", "organize.stop-reason.destination-full"),
        ("OrganizeStopReasonRollbackFailed", "organize.stop-reason.rollback-failed"),
        ("OrganizeStopReasonAborted", "organize.stop-reason.aborted"),
        ("OrganizeStopReasonLockLost", "organize.stop-reason.lock-lost"),
        // Edited by S-E004-015 (ADR-GWIN-143): the two Deposit constants, in the order of deposit-button contract 5.
        ("MessageDepositCancelled", "message.deposit.cancelled"),
        ("MessageDepositSummaryStopped", "message.deposit.summary-stopped"),
        // Edited by S-E008-001 (ADR-GWIN-153): the craft and build success key (contract 4.1), last.
        ("MessageCraftUsedBase", "message.craft.used-base"),
        // Edited by S-E010-002 (organize-console-command 5): the five console constants, in the order of the keys.
        ("ConsoleOrganizeStarted", "console.organize.started"),
        ("ConsoleOrganizePreview", "console.organize.preview"),
        ("ConsoleOrganizeNoPlayer", "console.organize.no-player"),
        ("ConsoleBasestorageUsage", "console.basestorage.usage"),
        ("ConsoleBasestorageDescription", "console.basestorage.description"),
        // Edited by S-QR-002 (ADR-GWIN-176)
        
        ("MenuRestock", "menu.restock"),
        ("MessageRestockSummary", "message.restock.summary"),
        ("MessageRestockMoved", "message.restock.moved"),
        ("MessageRestockNothing", "message.restock.nothing"),
        ("MessageRestockNothingMoved", "message.restock.nothing-moved"),
        ("MessageRestockLeftBehind", "message.restock.left-behind"),
        ("MessageRestockStopped", "message.restock.stopped"),
        ("MessageRestockCaptureFailed", "message.restock.capture-failed"),
        ("MoveNameTopic", "move.name.topic"),
        ("MoveNamed", "move.named"),
        ("MoveNameCleared", "move.name.cleared"),
        ("MovePreviewHeader", "move.preview.header"),
        ("MovePreviewMoved", "move.preview.moved"),
        ("MovePreviewStayed", "move.preview.stayed"),
        ("MovePreviewNoRoom", "move.preview.no-room"),
        ("MovePreviewSkipped", "move.preview.skipped"),
        ("MoveDoneMoved", "move.done.moved"),
        ("MoveDoneStayed", "move.done.stayed"),
        ("MoveDoneNoRoom", "move.done.no-room"),
        ("MoveDoneSkipped", "move.done.skipped"),
        ("MoveNothing", "move.nothing"),
        ("MoveStillWaiting", "move.still-waiting"),
        ("MoveRefusedBusy", "move.refused.busy"),
        ("MoveRefusedNoSuchBase", "move.refused.no-such-base"),
        ("MoveRefusedPlayerAtBase", "move.refused.player-at-base"),
        ("MoveRefusedSameBase", "move.refused.same-base"),
        ("MoveRefusedChanged", "move.refused.changed"),
        ("MoveRefusedTimedOut", "move.refused.timed-out"),
        ("MoveRefusedFailed", "move.refused.failed"),
        ("MoveLine", "move.line"),
        ("MenuPutAwaySelected", "menu.put-away-selected"),
        ("MessageItemSelected", "message.item.selected"),
        ("MessageItemDeselected", "message.item.deselected"),
        ("MessageNothingSelected", "message.nothing-selected"),
        ("MessageSelectMode", "message.select-mode"),
        ("MenuSelectItems", "menu.select-items"),
        ("MenuPutAwayCountOne", "menu.put-away-count.one"),
        ("MenuPutAwayCountOther", "menu.put-away-count.other"),
        ("MenuCancel", "menu.cancel"),
        ("MessageQuickPutAwayEquipped", "message.quick-put-away.equipped"),
        ("CharacterSlotHelmet", "character.slot.helmet"),
        ("CharacterSlotChest", "character.slot.chest"),
        ("CharacterSlotLegs", "character.slot.legs"),
        ("CharacterSlotCape", "character.slot.cape"),
        ("CharacterSlotUtility", "character.slot.utility"),
        ("CharacterSlotTrinket", "character.slot.trinket"),
        ("CharacterSlotWeapon", "character.slot.weapon"),
        ("CharacterSlotShield", "character.slot.shield"),
        ("CharacterSlotAmmo", "character.slot.ammo"),
        ("MessageWornNoRoom", "message.worn.no-room"),
        ("MessageWornWrongSlot", "message.worn.wrong-slot"),
        ("CompendiumButton", "compendium.button"),
        ("CompendiumTabItems", "compendium.tab.items"),
        ("CompendiumTabCreatures", "compendium.tab.creatures"),
        ("CompendiumTabBiomes", "compendium.tab.biomes"),
        ("CompendiumTabPieces", "compendium.tab.pieces"),
        ("CompendiumSearch", "compendium.search"),
        ("CompendiumBack", "compendium.back"),
        ("CompendiumClose", "compendium.close"),
        ("CompendiumRow", "compendium.row"),
        ("CompendiumEmpty", "compendium.empty"),
        ("CompendiumPick", "compendium.pick"),
        ("CompendiumFailed", "compendium.failed"),
        ("CompendiumSectionStats", "compendium.section.stats"),
        ("CompendiumSectionCrafting", "compendium.section.crafting"),
        ("CompendiumSectionUsedIn", "compendium.section.used-in"),
        ("CompendiumSectionDroppedBy", "compendium.section.dropped-by"),
        ("CompendiumSectionGathered", "compendium.section.gathered"),
        ("CompendiumSectionDrops", "compendium.section.drops"),
        ("CompendiumSectionAppears", "compendium.section.appears"),
        ("CompendiumSectionDamage", "compendium.section.damage"),
        ("CompendiumSectionCreatures", "compendium.section.creatures"),
        ("CompendiumSectionResources", "compendium.section.resources"),
        ("CompendiumSectionCost", "compendium.section.cost"),
        ("CompendiumStatWeight", "compendium.stat.weight"),
        ("CompendiumStatStack", "compendium.stat.stack"),
        ("CompendiumStatQuality", "compendium.stat.quality"),
        ("CompendiumStatValue", "compendium.stat.value"),
        ("CompendiumStatNoPortal", "compendium.stat.no-portal"),
        ("CompendiumStatDurability", "compendium.stat.durability"),
        ("CompendiumStatArmor", "compendium.stat.armor"),
        ("CompendiumStatArmorLevels", "compendium.stat.armor-levels"),
        ("CompendiumStatBlock", "compendium.stat.block"),
        ("CompendiumStatMovement", "compendium.stat.movement"),
        ("CompendiumStatDamage", "compendium.stat.damage"),
        ("CompendiumStatFoodHealth", "compendium.stat.food-health"),
        ("CompendiumStatFoodStamina", "compendium.stat.food-stamina"),
        ("CompendiumStatFoodEitr", "compendium.stat.food-eitr"),
        ("CompendiumStatFoodRegen", "compendium.stat.food-regen"),
        ("CompendiumStatFoodTime", "compendium.stat.food-time"),
        ("CompendiumStatSet", "compendium.stat.set"),
        ("CompendiumStatBoss", "compendium.stat.boss"),
        ("CompendiumStatHealth", "compendium.stat.health"),
        ("CompendiumStatStrongest", "compendium.stat.strongest"),
        ("CompendiumStatComfort", "compendium.stat.comfort"),
        ("CompendiumStatStation", "compendium.stat.station"),
        ("CompendiumRecipeByHand", "compendium.recipe.by-hand"),
        ("CompendiumRecipeStation", "compendium.recipe.station"),
        ("CompendiumRecipeStationLevel", "compendium.recipe.station-level"),
        ("CompendiumIngredient", "compendium.ingredient"),
        ("CompendiumIngredientLevels", "compendium.ingredient-levels"),
        ("CompendiumDrop", "compendium.drop"),
        ("CompendiumGathered", "compendium.gathered"),
        ("CompendiumResponse", "compendium.response"),
        ("CompendiumTimeAny", "compendium.time.any"),
        ("CompendiumTimeDay", "compendium.time.day"),
        ("CompendiumTimeNight", "compendium.time.night"),
        ("CompendiumCategoryWeapon", "compendium.category.weapon"),
        ("CompendiumCategoryArmor", "compendium.category.armor"),
        ("CompendiumCategoryShield", "compendium.category.shield"),
        ("CompendiumCategoryTool", "compendium.category.tool"),
        ("CompendiumCategoryAmmo", "compendium.category.ammo"),
        ("CompendiumCategoryFood", "compendium.category.food"),
        ("CompendiumCategoryPotion", "compendium.category.potion"),
        ("CompendiumCategoryTrophy", "compendium.category.trophy"),
        ("CompendiumCategoryMaterial", "compendium.category.material"),
        ("CompendiumCategoryMisc", "compendium.category.misc"),
        ("CompendiumDamageTrue", "compendium.damage.true"),
        ("CompendiumDamageBlunt", "compendium.damage.blunt"),
        ("CompendiumDamageSlash", "compendium.damage.slash"),
        ("CompendiumDamagePierce", "compendium.damage.pierce"),
        ("CompendiumDamageChop", "compendium.damage.chop"),
        ("CompendiumDamagePickaxe", "compendium.damage.pickaxe"),
        ("CompendiumDamageFire", "compendium.damage.fire"),
        ("CompendiumDamageFrost", "compendium.damage.frost"),
        ("CompendiumDamageLightning", "compendium.damage.lightning"),
        ("CompendiumDamagePoison", "compendium.damage.poison"),
        ("CompendiumDamageSpirit", "compendium.damage.spirit"),
        ("CompendiumResponseVeryWeak", "compendium.response.very-weak"),
        ("CompendiumResponseWeak", "compendium.response.weak"),
        ("CompendiumResponseSlightlyWeak", "compendium.response.slightly-weak"),
        ("CompendiumResponseSlightlyResistant", "compendium.response.slightly-resistant"),
        ("CompendiumResponseResistant", "compendium.response.resistant"),
        ("CompendiumResponseVeryResistant", "compendium.response.very-resistant"),
        ("CompendiumResponseImmune", "compendium.response.immune"),
        ("CompendiumResponseNormal", "compendium.response.normal"),
        ("CompendiumBiomeMeadows", "compendium.biome.meadows"),
        ("CompendiumBiomeBlackForest", "compendium.biome.black-forest"),
        ("CompendiumBiomeSwamp", "compendium.biome.swamp"),
        ("CompendiumBiomeMountain", "compendium.biome.mountain"),
        ("CompendiumBiomePlains", "compendium.biome.plains"),
        ("CompendiumBiomeMistlands", "compendium.biome.mistlands"),
        ("CompendiumBiomeAshlands", "compendium.biome.ashlands"),
        ("CompendiumBiomeDeepNorth", "compendium.biome.deep-north"),
        ("CompendiumBiomeOcean", "compendium.biome.ocean"),
        ("CompendiumBlurbMeadows", "compendium.blurb.meadows"),
        ("CompendiumBlurbBlackForest", "compendium.blurb.black-forest"),
        ("CompendiumBlurbSwamp", "compendium.blurb.swamp"),
        ("CompendiumBlurbMountain", "compendium.blurb.mountain"),
        ("CompendiumBlurbPlains", "compendium.blurb.plains"),
        ("CompendiumBlurbMistlands", "compendium.blurb.mistlands"),
        ("CompendiumBlurbAshlands", "compendium.blurb.ashlands"),
        ("CompendiumBlurbDeepNorth", "compendium.blurb.deep-north"),
        ("CompendiumBlurbOcean", "compendium.blurb.ocean"),
        ("CompendiumUndiscoveredItem", "compendium.undiscovered.item"),
        ("CompendiumUndiscoveredStation", "compendium.undiscovered.station"),
        ("MenuAllBases", "menu.all-bases"),
        ("MenuHeadingBase", "menu.heading.base"),
        ("MenuHeadingChest", "menu.heading.chest"),
        ("MenuRowEmpty", "menu.row.empty"),
        ("MenuRowMore", "menu.row.more"),
        ("BasesRefresh", "bases.refresh"),
        ("BasesRename", "bases.rename"),
        ("BasesFrom", "bases.from"),
        ("BasesTo", "bases.to"),
        ("BasesLoading", "bases.loading"),
        ("BasesNone", "bases.none"),
        ("BasesPickSource", "bases.pick-source"),
        ("BasesMovingFrom", "bases.moving-from"),
        ("BasesFallbackName", "bases.fallback-name"),
        ("BasesUnknownBiome", "bases.unknown-biome"),
        ("BasesHere", "bases.here"),
        ("BasesDistance", "bases.distance"),
        ("BasesInfo", "bases.info"),
        ("BasesRenameDenied", "bases.rename-denied"),
    ];

    /// <summary>The plural base keys that have no text of their own (every base except station.actions.status-line).</summary>
    public static readonly string[] PluralBaseKeysWithoutText =
    [
        "hover.chest.in-base",
        "hover.station.base-chests",
        "organize.preview.body.move",
        "organize.preview.body.claim",
        "organize.preview.body.no-room",
        "organize.preview.body.skipped",
        "organize.preview.body.skipped-reason",
        "message.organize.summary-moved",
        "message.organize.summary-skipped",
        "message.organize.summary-no-room",
    ];

    /// <summary>Contract 3.2 rows as theory data: key and English text.</summary>
    public static TheoryData<string, string> EnglishRows()
    {
        var data = new TheoryData<string, string>();
        foreach ((string key, string text) in EnglishTable)
        {
            data.Add(key, text);
        }

        return data;
    }

    /// <summary>Contract 2.3 and 3.2: plural keys formatted with count as the only argument: key, count, expected.</summary>
    public static TheoryData<string, int, string> PluralRows() => new()
    {
        { "hover.chest.in-base", 1, "[In base: 1 station]" },
        { "hover.chest.in-base", 6, "[In base: 6 stations]" },
        { "hover.chest.in-base", 0, "[In base: 0 stations]" },
        { "hover.station.base-chests", 41, "[Base: 41 chests]" },
    };

    /// <summary>Contract 2.3 S8 status-line rows: count of stations, count of chests, expected text.</summary>
    public static TheoryData<int, int, string> StatusLineRows() => new()
    {
        { 1, 1, "Base: <color=orange>1</color> station, <color=orange>1</color> chest" },
        { 1, 41, "Base: <color=orange>1</color> station, <color=orange>41</color> chests" },
        { 6, 1, "Base: <color=orange>6</color> stations, <color=orange>1</color> chest" },
        { 6, 41, "Base: <color=orange>6</color> stations, <color=orange>41</color> chests" },
    };

    /// <summary>Contract 3.2 move clauses under S8: stacks count, chests count, expected text.</summary>
    public static TheoryData<int, int, string> MoveClauseRows() => new()
    {
        { 1, 1, "1 stack to move across 1 chest." },
        { 1, 5, "1 stack to move across 5 chests." },
        { 37, 1, "37 stacks to move across 1 chest." },
        { 37, 5, "37 stacks to move across 5 chests." },
    };

    /// <summary>Contract 2.3 preview example: the shared 7-element args array {37,5,2,"Ores",4,1,"Erik"}.</summary>
    public static object?[] PreviewArgs() => new object?[] { 37, 5, 2, "Ores", 4, 1, "Erik" };

    /// <summary>Contract 2.3 preview example: the four clause texts for <see cref="PreviewArgs"/>, in order move, claim, no-room, skipped.</summary>
    public static readonly string[] PreviewClauseTexts =
    [
        "37 stacks to move across 5 chests.",
        "2 chests will be claimed for Ores.",
        "4 stacks have no room.",
        "1 chest skipped (in use by Erik).",
    ];

    /// <summary>Contract 5.3 chest rows: stationCount, categoryLabel, cycleKeyLabel, expected line.</summary>
    public static TheoryData<int, string?, string?, string> ChestLines() => new()
    {
        { 1, null, null, "[In base: 1 station]" },
        { 6, null, null, "[In base: 6 stations]" },
        { 0, null, null, "[Not in a base]" },
        { 3, "Keep", null, "[In base: 3 stations] [Keep]" },
        { 2, "Auto", null, "[In base: 2 stations] [Auto]" },
        { 0, "Ores", null, "[Not in a base] [Ores]" },
        { 3, "Ores", "G", "[In base: 3 stations] [Ores] [<color=yellow><b>G</b></color>] Category" },
        { 1, null, "G", "[In base: 1 station] [<color=yellow><b>G</b></color>] Category" },
        { 2, "", "  ", "[In base: 2 stations]" },
        { 2, "Ore\r\nMetal", "Key\nX", "[In base: 2 stations] [Ore Metal] [<color=yellow><b>Key X</b></color>] Category" },
    };

    /// <summary>Contract 5.3 station rows: chestCount, expected line.</summary>
    public static TheoryData<int, string> StationLines() => new()
    {
        { 41, "[Base: 41 chests]" },
        { 1, "[Base: 1 chest]" },
        { 0, "[Base: 0 chests]" },
    };

    /// <summary>Contract 5.3 AC-2 matrix values for categoryLabel.</summary>
    public static readonly string?[] MatrixCategoryLabels = [null, "", " ", "Auto", "Keep", "Ores", "Ore\r\nMetal", "\n"];

    /// <summary>Contract 5.3 AC-2 matrix values for cycleKeyLabel.</summary>
    public static readonly string?[] MatrixCycleKeyLabels = [null, "", "G", "Key\nX"];

    /// <summary>Contract 5.3 AC-2 matrix values for stationCount.</summary>
    public static readonly int[] MatrixStationCounts = [0, 1, 2, 6, 1000];

    /// <summary>Contract 5.3 AC-2 matrix values for Station(chestCount).</summary>
    public static readonly int[] MatrixChestCounts = [0, 1, 2, 41, 1000];

    /// <summary>Contract 4 rows: input text and expected single-line output, plus the extra cases from the same section.</summary>
    public static TheoryData<string, string> SingleLineRows() => new()
    {
        { "Ores", "Ores" },
        { "Ore\r\nMetal", "Ore Metal" },
        { "a\n\nb\rc", "a b c" },
        { "Erik the Red", "Erik the Red" },
        { "\n", " " },
        { "a\u0085b", "a b" },
        { "a  b", "a  b" },
        { "x\r\n y", "x  y" },
        { "\nA\n", " A " },
        { "<b>G</b>", "<b>G</b>" },
        { "", "" },
    };

    /// <summary>Contract 2.3 fixed AC texts and formatting examples, verbatim.</summary>
    public const string InProgressTemplate = "Organize in progress by {0}";

    /// <summary>Contract 2.3: network.version.mismatch template.</summary>
    public const string VersionMismatchTemplate = "Don's QOL version mismatch: host {0}, you {1}";

    /// <summary>Contract 2.3: network.version.mismatch with "0.1.0" and "0.2.0".</summary>
    public const string VersionMismatchText = "Don's QOL version mismatch: host 0.1.0, you 0.2.0";

    /// <summary>Contract 2.3: message.organize.summary with 37, 2, "in use", 4.</summary>
    public const string OrganizeSummaryText = "Organized: 37 stacks moved, 2 skipped (in use), 4 no room";

    /// <summary>Contract 2.3: message.base.in-progress with "Erik".</summary>
    public const string InProgressErikText = "Organize in progress by Erik";

    /// <summary>Contract 2.3: station.actions.status-line-fill with 1.5 (invariant culture).</summary>
    public const string StatusLineFillText = ", <color=orange>1.5%</color> full";

    /// <summary>Contract 2.3: message.craft.materials-moved, with or without extra args.</summary>
    public const string MaterialsMovedText = "Materials moved, try again";

    /// <summary>Contract 2.3: the status-line text of spec AC-1 once rich-text tags are stripped.</summary>
    public const string StatusLineStrippedText = "Base: 6 stations, 41 chests";
}
