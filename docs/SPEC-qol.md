# Don's QOL spec: restock, more feeding, sort a chest, chest names, gamepad

The mod is named Don's QOL and its code keeps the BaseStorage name. `docs/SPEC.md`, `docs/SPEC-v0.2.md` and the shipped code on main are current behaviour; this document changes them only where a section below says so. Principle 1 still holds: items are never lost or duplicated, and on any doubt the mod skips the chest. Core holds every decision and Plugin is a thin adapter.

## 1. Rules for every story

R1 to R5 of `docs/SPEC-v0.2.md` section 1 apply unchanged. They come from bugs that passed every test and failed in the real game. Also:

- **R6. Check every game API against the installed game before using it.** Do not trust memory of Valheim's API. Use the operator-approved read-only tool from Bash: `"C:/Users/dawso/.pipemason/tools/game-api/bin/game-api.exe" types|members|il|callers ...`, with `--dll <name>` for other assemblies. It reads owners, signatures and compiled behaviour without running the game. Do not write your own IL loader or load .NET code from PowerShell. Names, signatures and overloads from `assembly_valheim` go through `src/Shared/PatchTargetNames.cs` and the patch-target contract tests. `assembly_utils`, `assembly_guiutils` and Unity APIs, such as `ZInput` and `KeyCode`, are outside that resolver: the Release build guards them and they are not listed. Valheim's own spellings stand, for example `InUpradeTab`.
- **R7. Reuse what exists.** Chest moves go through the reservation protocol. New panels and buttons reuse `VanillaButtonFactory` and the current menu. New text goes in the string table. New settings follow the existing pattern and the player README's settings table.
- **R8. Keep saved data.** Do not rename the plugin GUID, the `basestorage.*` keys, the `BaseStorage_*` RPCs, the DLLs, the install folder or the console command. New data uses a new `basestorage.*` key that vanilla ignores.
- **R9. A shared pin is a reason to look, not a reason to delete.** When a count or surface test changes because of this work, update it to the new true value. Do not weaken it.

## 2. Operator rules

Implementation only: no live game, e2e or manual tests, and no pr_review. Agents never launch Valheim, never write to its folder and never read saves. The operator tests by hand from `docs/PLAYTEST-v0.4.3.md`. Gates are `dotnet build -c Release -warnaserror`, `dotnet test` and `dotnet format --verify-no-changes`. Local only: nothing is pushed, released or deployed.

## 3. Stories

Plan exactly these stories, in this dependency order. Split one only if it cannot be done in one story, and keep its id as a prefix.

### Epic R: restock from base

**S-QR-001 Restock planner.** A Core planner tops up what the player carries from the base chests. It covers stacks on the hotbar and item types marked as favourites, and it fills each to its maximum stack size. It never adds a new item type to the inventory. It takes from the fullest-matching stack in the nearest chest first, ties to snapshot order. Keep chests are used only when `KeepChestsCraftable` is true. A full inventory, or a chest in use, leaves the rest and reports the count. Conservation is proved with a fuzz test.

**S-QR-002 Restock in the game.** A **Restock** button next to Put away inventory in the menu, and an unbound `RestockKey` hotkey. It runs through the reservation protocol and shares the one-per-base lock. It ends with one line such as `Restocked 3 stacks, 42 items`, or `Nothing to restock`. Add a `RestockFromKeep` choice only if `KeepChestsCraftable` cannot serve; otherwise add no setting.

### Epic F: feed more stations

**S-QF-001 Feeding model.** Extend `Core/Feeding` so a station can be a fuel-only station, a single-input station or a several-input station, without changing how smelter-class stations behave. Existing saved `basestorage.feed` values keep working. The planner never overfills a station and never takes the last item a chest rule protects.

**S-QF-002 Fires and torches.** Campfires, hearths, bonfires, standing torches and other `Fireplace` pieces can keep fuel topped up from the base with the fuel the piece itself accepts. The player opens the same feed panel with the same Shift+use gesture. The hover text shows it is kept fed.

**S-QF-003 Fermenters and cooking stations.** Fermenters are filled from chests holding mead bases, and cooking stations with raw food the station accepts. The player picks which items in the feed panel. The mod only puts items in; it does not collect finished items, so nothing burns unattended beyond what the player chose. A cooking station with an empty slot takes one item per slot.

### Epic S: sort a chest

**S-QS-001 Chest sort order.** Core takes one chest's items and returns the merged, ordered layout: same items stack together, then catalog category order, then item name. Item data survives, as in S-A-004 of `docs/SPEC-v0.2.md`. Stacks that differ in quality, durability or crafter stay separate.

**S-QS-002 Sort button.** A **Sort** button on the chest panel beside the Category button. It works only on the open chest, moves nothing between chests and holds the chest through the reservation protocol. A Keep chest can be sorted. It ends with one line such as `Sorted: 14 stacks`.

### Epic L: chest names

**S-QL-001 Chest names.** A player can name a chest, up to 32 characters, through the vanilla text input, from a **Name** button on the chest panel. The name is stored on the chest under `basestorage.name` and sanitised on read. An empty name clears it. The menu list shows the name, and search matches it. Hover text shows it as well as the category.

**S-QL-002 Labels in the world.** Chests with a name or a category can show a floating label above them, within 6 metres of the player and only inside a base. Add `ChestLabels` (client, default true) and `ChestLabelDistance` (client, default 6). Labels must add no per-frame allocation for chests out of range, and must not show through the UI. Reuse the game's own world-text path if it has one that fits.

### Epic G: gamepad support

**S-QG-001 Navigation model.** Core holds a focus model for the menu: move between tabs or buttons, list rows and the search box; wrap and clamp rules; what confirm and cancel do on each. It is fully unit tested and needs no game types.

**S-QG-002 Menu on a gamepad.** The menu opens from the inventory with a gamepad button that does not clash with vanilla, chosen by reading vanilla's bindings. The stick or D-pad moves focus with a visible highlight. Confirm opens a chest or presses a button. Cancel closes. The button hints follow the game's own gamepad hint style. Keyboard and mouse behaviour does not change. `ZInput` is used for input, and the mod must work with the game's gamepad layout setting.

**S-QG-003 Chest and feed panels on a gamepad.** The Category, Sort and Name buttons, the feed panel and the Organize and Put away previews can all be used with a gamepad. Text entry uses the game's own on-screen keyboard path. The category cycle hotkeys stay keyboard only.

### Epic Z: release notes

**S-QZ-001 Docs and version.** Set `Version.props` to 0.4.3. Add `docs/RELEASE-NOTES-v0.4.3.md` and `docs/PLAYTEST-v0.4.3.md`, with numbered steps for every game-only path, including the emblem icon and the bukeberries fix already on main. Update the player README: features, settings table and Known limits, which no longer list what this program adds. Remaining limits stay listed. Do not build or publish a release.

## 4. Out of scope

Dedicated servers, a sorting table piece, carts and ships as storage, and any change to Organize's placement rules.
