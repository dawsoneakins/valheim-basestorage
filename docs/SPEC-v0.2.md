# BaseStorage v0.2 spec: smarter sorting, the BaseStorage menu, and a one-click installer

v0.1 (`docs/SPEC.md`, ACs AC-1.1 to AC-9.4) is shipped behaviour. This document changes it only where a section below says so. Everything else in `docs/SPEC.md` still holds, especially Principle 1: items are never lost or duplicated, and on any doubt the mod skips the chest. Spec section 3 (Core holds every decision, Plugin is a thin adapter) still holds.

## 1. What the first real playtest taught us (2026-10-30)

The v0.1 program finished with every test green, and the mod still did not work in the game. Four bugs reached the first playtest, all fixed on main before this program:

| Bug | Why the tests missed it |
|---|---|
| A C# tuple in the plugin needs `System.ValueTuple.dll`, which Valheim does not ship. Every button failed with a TypeLoadException. | Tests run on net10.0, where tuples are always available. |
| `TimeSpan.MinValue` in the hotkeys overflowed on every frame. | The Plugin cannot be loaded by tests. |
| The station buttons were silently hidden on every normal screen. | Fixtures used a 180-wide craft button; the real one is about 336 wide and sits in the screen corner. |
| A one-chest snapshot used a memo that only works during a full rebuild, so every Organize and Put away crashed on its first chest. The crash was swallowed and reported as `cancelled`. | The fake container layer in Core tests never reaches the Plugin snapshot code. |

Rules for every story in this program:

- **R1. No new assembly dependencies in the plugin.** `PluginRuntimeSafetyTests` must stay green. No tuples, and nothing else that needs a DLL Valheim lacks.
- **R2. Do not assume the game's UI geometry.** Measure rectangles at runtime. Fixtures use real values: on a 2000 x 1120 screen the craft button is about (1533, 241, 336, 60) in canvas units, the recipe list about (1326, 231, 188, 552), and the crafting panel sits in the right corner.
- **R3. Never swallow an exception silently.** Every catch that ends an operation logs the exception at Error, or says in a comment why it cannot.
- **R4. Keep Plugin code thin.** Anything with a decision in it goes in Core with tests. A Plugin path that only runs in the game gets a source-scan or metadata test where one is practical, and a numbered step in `docs/PLAYTEST.md`.
- **R5. Game-bound code must not use a helper that only works in one call context** without checking that context first.

## 2. Operator rules

Implementation only: no live game, e2e or manual tests, and no pr_review. The operator tests by hand from `docs/PLAYTEST.md`. Gates are build with `-warnaserror`, `dotnet test`, and `dotnet format --verify-no-changes`. Nothing here may write to a player's save files, and the test-bed plugin at `projects/valheim-basestorage-testbed` is not part of this repo or of any package.

## 3. Stories

Plan exactly these stories, in this dependency order. Split one only if it cannot be done in one story, and keep its id as a prefix.

### Epic A: smarter sorting

Measured baseline, 60 chests of 24 slots, about 40 percent assigned at random: at 70 percent full, Organize moved 380 stacks and 499 had no room; at near full, 264 moved and 782 had no room. A mixed chest under the 50 percent threshold stays a source forever, and the planner makes a single pass.

**S-A-001 Mixed chests get a category (replaces AC-4.4 rule 2).** After the existing threshold step, Core assigns the remaining mixed, unassigned chests. Demand per category is the number of stacks of that category across the usable chests. Capacity is the slot count of the chests already holding that category. Repeat: take the category with the largest shortfall, tie to catalog order; give it the unassigned mixed chest holding the most stacks of that category, tie to snapshot order; add its slots to capacity. Stop when no category is short or no chest remains. Keep chests and manually assigned chests are never changed. Empty chests stay in the overflow pool. A new `AutoAssignMixedChests` host-synced setting, default true, restores the v0.1 rule when false. The result is deterministic. Update the tests that pinned rule 2 only as far as this section requires, and keep them passing with the setting false.

**S-A-002 The planner retries until nothing more can move (extends AC-4.5).** Repeat placement passes over the stacks still misplaced until a pass places nothing. A later pass may use a slot an earlier pass freed. Moves stay in execution order, so each move's destination has room when it runs; `MoveBatch` must not break that. No-room entries reflect only the final pass. The fuzz test still proves conservation, capacity and Keep over at least 1,000 random bases, now including bases over 90 percent full. Target: on the near-full baseline, fewer than 100 stacks have no room when any free slots exist. Report the measured figure in the story.

**S-A-003 Rules are saved and explained.** A category given by S-A-001 is written to the chest through the existing reservation path when the run succeeds, so hover text and the menu show it. A chest that could not be written is skipped and logged, and the run continues. The preview says how many chests will get a rule, for example `12 chests will get a category`. If a base still cannot sort, the preview says why in plain words, for example `This base is full. Free up space or add a chest.`, rather than only a no-room count.

**S-A-004 A moved item stays the same item.** Prove that a move keeps durability, quality, crafter name and id, variant, world level and custom data, including the rollback path. Fix `CloneForMove` and the deposit equivalent if any of these is dropped.

### Epic B: item category in the tooltip

**S-B-001 Category line on item tooltips.** Every item tooltip gets one line, `Category: <name>`, from the same catalog Organize uses, so the line always says where Organize sends the item. Items with no listed prefab show the category they fall back to. Add a `ShowCategoryTooltip` client setting, default true. Do not touch vanilla tooltip text otherwise, and do not allocate per frame.

### Epic C: the BaseStorage menu

The character panel under the player's name has a row of icons (compendium, skills and so on). The menu adds one more, labelled **BaseStorage**.

**S-C-001 The BaseStorage button and empty panel.** Clone a vanilla button in that row, opening a panel in the vanilla style that closes with Esc and with the inventory, and never covers the crafting panel's own controls. It works at 1280x720 scale 0.8 and at 3840x2160 scale 1.2. Outside a base the panel says `Not in a base`. Only the base the player stands in is shown; no remote bases, no cached data.

**S-C-002 Chest list.** A scrolling list of every chest in the current base: name or position label, category or Auto or Keep, percent full, and a distance hint. Sorted by category, then distance. Filterable by category. Handles 150 chests without a frame hitch: build rows once and reuse them. It refreshes when the base changes, and it is read-only in this story.

**S-C-003 View a chest.** Selecting a row shows that chest's contents as a read-only grid with vanilla icons, stack counts and tooltips. It reads a snapshot and takes no reservation. It updates within 2 seconds of a change. A chest another player has open is marked in use.

**S-C-004 Set a chest's rule from the menu.** The list has the same cycle as the chest's Category button (Auto, categories, Keep), through the existing `ChestCategoryWriter` and reservation path. A refusal shows the same `Chest category not changed: in use by <name>` message.

**S-C-005 Organize and Put away in the menu.** Both buttons, the same preview and confirm, the same lock, and the same disabled reasons as the station buttons. They use the base the player stands in, the rule AC-2.4 already gives the hotkeys. This extends AC-4.10: the station stays an entry point, and it is no longer the only one. The station buttons stay, as a shortcut, unless `StationButtons` is false.

**S-C-006 Status line.** The status line under the station buttons never appears in the game, and the buttons sit outside the panel. Find out why, fix it or remove it, and show the same facts in the menu header (stations, chests, percent full, Organize holder).

**S-C-007 Take items from a chest in the menu.** After S-C-003, the chest grid lets the player move a stack to their inventory, through the same reservation and add-then-remove path as Organize. All of Principle 1 applies: refused, lost or changed means nothing moves. It works only for the current base, and honours private chests, wards and access rules.

### Epic D: release

**S-D-001 One-click installer.** A release zip with `Install BaseStorage.bat`, a PowerShell script and the plugin DLLs. Double-clicking finds Valheim through Steam's `libraryfolders.vdf`, asks for the folder only if it cannot; installs BepInExPack_Valheim if missing, at a pinned version with a pinned SHA-256, from Thunderstore, and refuses on a mismatch; copies the plugin to `BepInEx\plugins\BaseStorage\`; prints what it did. It backs up any file it replaces, is safe to run twice, needs no admin rights unless the folder requires them, never touches save files, refuses while Valheim is running, and works with no PowerShell execution-policy change. Tests run against a fake Valheim folder and an injected downloader, with no network and no real game folder.

**S-D-002 Uninstaller.** `Uninstall BaseStorage.bat` removes the plugin, and asks before removing BepInEx. It leaves the config and the leftover chest keys, which vanilla ignores.

**S-D-003 v0.2.0 docs.** `Version.props` 0.2.0, CHANGELOG, player README, and new `docs/PLAYTEST.md` sections for every story, each with numbered steps and `Expected:` lines. Include a Put away inventory section for a throwaway character. Update `docs/SPEC.md` where this spec replaces it.

## 4. Out of scope

Viewing a base the player is not in, station feeding (spec section 11), base item search, and any change to crafting from the base.
