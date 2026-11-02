# BaseStorage playtest checklist

## Setup

## Load check

Confirms BepInEx loads the plugin. `<version>` is the `Version` value in `Version.props`.

1. Install BepInExPack_Valheim into the game folder.
2. Run `dotnet build BaseStorage.sln -c Release -p:DeployToGame=true`.
3. Check that `BaseStorage.dll` and `BaseStorage.Core.dll` are in `<ValheimDir>\BepInEx\plugins\BaseStorage\`.
4. Start Valheim and wait for the main menu.
5. Open `<ValheimDir>\BepInEx\LogOutput.log` and search for `BaseStorage <version> loaded`.

Expected: exactly one line whose message is `BaseStorage <version> loaded`, for example `[Info   :BaseStorage] BaseStorage 0.1.0 loaded`.
No `BaseStorage` line contains
`failed to apply` or `failed to load`. If one does, record the patch name it gives.

6. Open `<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg`.

Expected: the file has 5 sections, `[General]`, `[Organize]`, `[Network]`, `[Keys]` and `[Debug]`, and 19 settings in total. `AutoAssignMixedChests = true` is listed under `[Organize]`.
Under `[Keys]`, `CycleCategoryKey`, `OrganizeKey` and `DepositKey` are all empty (unbound).

## Solo

## Host plus one friend

## Debug logging

BaseStorage writes one Info line at load (`BaseStorage <version> loaded`) and, per Organize or Deposit, one summary line. Everything else (per-move lines, patch applied lines, stack traces, UI fallback notes) is Debug and is off by default.

1. In `BepInEx/config/BepInEx.cfg`, add `Debug` to `LogLevels` under `[Logging.Disk]` (and `[Logging.Console]` for the console). BepInEx filters Debug out by default, so the mod's Debug lines never reach `LogOutput.log` otherwise.
2. Open the in-game ConfigurationManager (F1), find the `[Debug]` section and set `DebugLogging` to true. The change applies without restarting the game.
3. Trigger the action you are testing. Lines tagged `[Debug  :BaseStorage]` appear in `LogOutput.log`.
4. Set `DebugLogging` back to false. No new Debug lines are written.

Editing the `.cfg` file by hand while the game runs needs a config reload before it takes effect.
## Category file

BaseStorage writes `BepInEx\config\BaseStorage.categories.txt` once per game start, at the main menu, when the file is missing. Set `DebugLogging = true` to see the Debug line in the scenarios below.

1. Fresh install, file absent. Start the game and reach the main menu. The file exists, starts with `# BaseStorage categories file (BaseStorage.categories.txt)`, has 14 category lines in the default order, and the log shows at Info `Wrote default category file '...\BepInEx\config\BaseStorage.categories.txt' with <n> item(s)`.
2. File exists with player edits (an edited file). After a full game start the content and last-write time are unchanged. With `DebugLogging = true` the Debug log shows `Category file '...' already exists; left unchanged`.
3. File exists and is a 0-byte file. It is still 0 bytes after the start.
4. `BepInEx\config` is read-only and the file is absent. The log shows a Warning `Could not write default category file '...': System.UnauthorizedAccessException: ...`. No file is created, and the game reaches the main menu and plays normally.
5. File deleted while the game runs. It is not recreated in that process. It is recreated on the next start.

## Favourites

Mod favourites are per character. They are stored under the `basestorage.fav` key of the character's custom data and are saved with the character by the game's normal save; the mod never forces a save. A favourite slot shows a small gold star (the design-system id `inventory.slot.favourite-marker`) in the slot's bottom-right corner, on top of the item icon.

1. Load a character, press Tab to open the inventory, hold **Alt** and left-click an occupied slot (for example Wood). Expected: a star appears in the slot's bottom-right corner, the top-left message reads `Favourite: <item name>` (for example `Favourite: Wood`), and the item stays in the slot (it is not picked up).
2. Hold Alt and left-click the same slot again. Expected: the marker disappears, the message reads `No longer favourite: <item name>`, and the item stays in the slot.
3. Left-click a slot without Alt: vanilla pickup. Shift+click: vanilla split dialog. Ctrl+click: vanilla quick move. Expected: no marker change and no message in all three cases.
4. Hold Alt and left-click an empty slot. Expected: nothing happens: no marker, no message, and no `BaseStorage` error in `<ValheimDir>\BepInEx\LogOutput.log`.
5. Open a chest, hold Alt and left-click a slot in the chest's grid. Expected: vanilla behaviour for that chest slot, no marker and no message.
6. Pick an item up onto the cursor, hold Alt and left-click another inventory slot. Expected: the item is placed or swapped as in vanilla, and no favourite marker changes.
7. Mark three slots, one of them holding a stack count above 1 (for example Stone x 25). Check that the marker covers neither the item icon nor the stack count, at 1280x720 with UI scale 0.8 and at the highest available resolution (3840x2160 if possible) with UI scale 1.2. Note which corner vanilla uses for the equipped and queued indicators; if it is the top-left corner, record it as a design-system follow-up. Optional, with UnityExplorer: each marked slot element has exactly one child named `BaseStorage.inventory.slot.favourite-marker`.
8. With two slots marked, log out to the main menu (the character is saved), then load the same character again. Expected: the same two slots show the marker.
9. With a slot marked, log out and close the game. Remove `BaseStorage.dll` and `BaseStorage.Core.dll` from `<ValheimDir>\BepInEx\plugins\BaseStorage\`, start the game and load the same character. Expected: the character loads with no error in `LogOutput.log`, the inventory is intact and no marker is shown. The leftover `basestorage.fav` entry is ignored by vanilla.
10. Reinstall the mod and load the character. Expected: the slot marked before the uninstall shows the marker again.

## Acceptance script (AC-2.1 to AC-6.3)

This section has one numbered section per acceptance criterion from AC-2.1 to AC-6.3, in the order of the spec. Each has a Solo run and a Host plus one friend run, except AC-5.1 and AC-5.5, which cannot be seen in the game and are desk checks.
A step that says "Run the Solo steps of" a level-2 heading of this file means: run every numbered step of that subsection there, in order, and check every `Expected:` line. The same goes for "Run the Host plus one friend steps of" a heading. The Solo subsection of `## Config sync (AC-6.2, AC-6.3)` is named Solo and hosting.
The list of sections is checked by `PlaytestCoverageTests`. Start every run on a new world with `DebugLogging = true` (see `## Debug logging`) unless a step says otherwise.

### 1. AC-2.1 - Station graph

A base is the group of crafting stations that reach each other. Two stations are connected when either one is inside the other's build range. The distance is horizontal (height is ignored) and strictly less than the range, as in vanilla. A workbench has a build range of 20 m. The base summary line is `Base at station <id>: <S> station(s), ...` as described in `## Base membership (AC-2.3, AC-2.5, AC-5.3)`.

#### Solo

1. Place a workbench A on empty ground. Walk 15 m away and place a workbench B. Walk another 15 m in the same direction, so the new spot is 30 m from A, and place a workbench C.

Expected: the summary after A reads `1 station(s)`, after B `2 station(s)`, and after C `3 station(s)`. C is outside the range of A but inside the range of B, so the chain A, B, C is one base.

2. Walk 100 m away from the chain and place a workbench D.

Expected: `Base at station <id>: 1 station(s)`. D does not merge with the chain: the two bases stay separate.

3. Remove B with the hammer. Place a workbench E 10 m from A, then a workbench F 10 m from C, on the side away from A.

Expected: the summary after E reads `2 station(s)` (A and E) and the summary after F reads `2 station(s)` (C and F). The chain was broken in the middle by removing B, so A and C are no longer in one base.

4. Place a workbench G on a hill or a raised platform 12 m above the ground, 15 m horizontally from A.

Expected: the summary reads `3 station(s)` (A, E and G). The height difference does not matter, only the horizontal distance does.

5. Place a workbench H 25 m horizontally from every other station of that base, then place a forge 15 m from H.

Expected: the summary after H reads `1 station(s)` (25 m is outside the 20 m range) and the summary after the forge reads `2 station(s)`: the forge counts as a crafting station.

6. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and no new `Base at station` line appears while you stand still.

#### Host plus one friend

The host runs the world with `DebugLogging = true` and reads the host `LogOutput.log`. Only the host needs the mod for these steps. The friend joins and stands next to the host.

1. The host places a workbench A. The friend walks 15 m away and places a workbench B.

Expected: on the host, `Registry: station added (2 loaded)` and `Base at station <id>: 2 station(s), ...`. A station placed by the friend joins the base of the host.

2. The friend walks 100 m away from both stations and places a workbench C.

Expected: on the host, `Base at station <id>: 1 station(s), ...`. The friend's lone station is a separate base.

3. The host removes B with the hammer and checks the log.

Expected: `Registry: station removed (2 loaded)`, and no `BaseStorage` line contains `failed and was skipped`.

Also covered by: the station graph tests in `tests/BaseStorage.Core.Tests/Bases` (`StationGraphCasesTests`, `BuildRangeRuleTests`).

### 2. AC-2.2 - Base membership

A container belongs to a base when it is inside the build range of at least one station of that base, and it belongs to every base that reaches it.

#### Solo

1. Place a workbench A on empty ground. Place a wooden chest X 17 m from A and a wooden chest Y 5 m from A, in a straight line. Then walk along that line to a spot 35 m from A (so X is 18 m from it) and place a workbench B there. The two stations are 35 m apart, so they are two separate bases.

Expected: after B, the log shows `Registry: station added (2 loaded)` and `Base at station <id>: 1 station(s), 1 container(s), 1 eligible; excluded 0 tombstone, 0 vehicle, 0 not player-built, 0 access denied; 0 in use by another`. Chest X is inside the range of both stations, so the base of B counts it as well as the base of A.

2. Wait 10 seconds. Hover A and then B.

Expected: A shows `[Base: 2 chests]` (X and Y) and B shows `[Base: 1 chest]` (X). The chest X in range of two bases belongs to both.

3. Place a wooden chest Z 60 m from both stations and hover it.

Expected: `[Not in a base]`. Z is outside the range of every station.

4. Remove A with the hammer and wait 10 seconds. Hover X and Y.

Expected: X shows `[In base: 1 station]` (the base of B) and Y shows `[Not in a base]`.

5. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and no new `Registry:` or `Base at station` line appears while you stand still.

#### Host plus one friend

The host runs the world with `DebugLogging = true`. Only the host needs the mod. The host places a workbench A, and the friend stands next to A.

1. The friend builds a wooden chest 10 m from A. The host waits 10 seconds and hovers A and the chest.

Expected: A shows `[Base: 1 chest]` and the chest shows `[In base: 1 station]`. A chest built by the friend belongs to the base of the host.

2. The friend builds a wooden chest 30 m from A, outside the range of every station. The host hovers that chest.

Expected: `[Not in a base]`.

3. The host walks 35 m from A, in the direction opposite to the 30 m chest, and places a workbench B. The friend builds a wooden chest 17 m from A and 18 m from B, on the line between the two. The host waits 10 seconds and hovers A and then B.

Expected: A shows `[Base: 2 chests]` (the 10 m chest and the new chest) and B shows `[Base: 1 chest]` (the new chest). The chest in range of both bases belongs to both.

Also covered by: `MembershipFindMembersTests` and `ResolverStationStartTests` in `tests/BaseStorage.Core.Tests/Bases`.

### 3. AC-2.3 - Eligible containers

#### Solo

1. Run the Solo steps of `## Base membership (AC-2.3, AC-2.5, AC-5.3)`.

Expected: every Expected line there holds: a cart or ship is excluded while `ExcludeVehicles` is true and eligible when it is false, a tombstone is excluded, and the wooden chests are eligible.

2. Run the Solo steps of `## Station buttons (AC-2.4, AC-4.7, AC-4.10)`.

Expected: every Expected line there holds, including the step where all chests are set to Keep: a Keep chest does not count as an eligible chest for Organize.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Base membership (AC-2.3, AC-2.5, AC-5.3)`.

Expected: every Expected line there holds: a Private chest of the friend and a chest inside the friend's ward are counted as access denied, and a chest the friend has open is counted as in use by another.

### 4. AC-2.4 - Starting point

#### Solo

1. Run the Solo steps of `## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)`.

Expected: every Expected line there holds: the hotkey away from every base shows `Not in a base`, and inside a base it organizes the base you stand in.

2. Run the Solo steps of `## Station buttons (AC-2.4, AC-4.7, AC-4.10)`.

Expected: every Expected line there holds: the Organize base button uses the base of the station you use, whatever your position.

3. Build two workbenches 15 m apart, so they are one base. Put the Wood a recipe needs in a chest in range of the first workbench only (more than 20 m from the second). Craft the recipe at the second workbench.

Expected: the recipe is craftable and the Wood is taken from that chest, because crafting uses the base of the workbench and the second workbench is in the same base. See the Craft from base section below for the full craft steps.

4. Equip the hammer and stand in range of the second workbench only. Select a wooden piece whose Wood is only in that chest, and place it.

Expected: the piece is buildable and the Wood is taken from the chest, because building uses the base whose station range contains you. Standing outside every station range, the same piece is not buildable.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)`.

Expected: every Expected line there holds.

2. Run the Host plus one friend steps of `## Station buttons (AC-2.4, AC-4.7, AC-4.10)`.

Expected: every Expected line there holds.

### 5. AC-2.5 - Range cap and cache

#### Solo

1. Run the Solo steps of `## Base membership (AC-2.3, AC-2.5, AC-5.3)`.

Expected: every Expected line there holds: the registry tracks chests and stations as they load and unload, the counts return to the same values after a walk away and back, and nothing runs while you stand still.

2. Run the Solo steps of `## Hover inspect (AC-2.6, AC-8.3)`.

Expected: every Expected line there holds: a changed count shows within the cache time (10 seconds by default).

#### Host plus one friend

1. Run the Host plus one friend steps of `## Base membership (AC-2.3, AC-2.5, AC-5.3)`.

Expected: every Expected line there holds.

Also covered by: `StationGraphCapTests` (the `MaxBaseStations` cap) and `BaseCacheInvalidationTests` in `tests/BaseStorage.Core.Tests/Bases`.

### 6. AC-2.6 - Inspect

#### Solo

1. Run the Solo steps of `## Hover inspect (AC-2.6, AC-8.3)`.

Expected: every Expected line there holds: a chest shows `[In base: N stations]` or `[Not in a base]` plus its category or `[Keep]`, and a station shows `[Base: N chests]`.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Hover inspect (AC-2.6, AC-8.3)`.

Expected: every Expected line there holds.

### 7. AC-3.1 - Availability

The available amount of a material is the inventory plus every eligible chest of the base, minus any chest another player has open. See the Craft from base and Build from base sections below for the full steps.

#### Solo

1. Stand at a workbench inside a base. Choose a recipe that needs Wood, keep part of the Wood in the inventory and the rest in a chest of the base, and open the crafting panel.

Expected: the recipe is craftable, although the inventory alone is not enough.

2. Move the Wood the recipe needs into a chest outside every base and look at the panel again.

Expected: the recipe is not craftable, as in vanilla.

3. Move the Wood back into the chest of the base. Equip the hammer and select a wooden piece whose Wood is only in that chest.

Expected: the piece is buildable (its icon is not red). Standing outside every base, the same piece is not buildable.

4. Put all but one unit of the needed Wood in the base chests and the inventory together.

Expected: the recipe is not craftable and nothing is taken: one short is still short.

#### Host plus one friend

1. The friend stands at a workbench in the host's base, with part of the Wood in a chest of the host's base. The friend opens the crafting panel.

Expected: the recipe is craftable for the friend.

2. The host opens that chest and keeps it open. The friend looks at the panel again.

Expected: within 2 seconds the chest is no longer counted, so the recipe is not craftable when that chest was needed. The host's chest window stays open.

3. The host closes the chest.

Expected: within 2 seconds the recipe is craftable for the friend again.

### 8. AC-3.2 - Display

When chests contribute, a requirement reads `<inventory> (+<chests>) / <need>`, for example `52/20`. See the Requirement display section below for the full steps.

#### Solo

1. Stand at a workbench inside a base. Choose a recipe that needs Wood, keep fewer Wood in the inventory than it needs, put more in a chest of the base, and select the recipe in the crafting panel.

Expected: the Wood line reads `<inventory> (+<chests>) / <need>` with the real counts, for example `52/20`, in white and not blinking, and the recipe is craftable.

2. Take Wood out of the chest until the inventory plus the chest is less than the need.

Expected: the line still shows the combined form and blinks red and white like vanilla, and the recipe is not craftable. Red appears only when the combined amount is short.

3. Move all Wood out of the base chests, into a chest outside every base.

Expected: the line shows only the needed amount, exactly as vanilla.

4. Inside the base, equip the hammer and hover a piece whose Wood is partly in the inventory and partly in a base chest.

Expected: the Wood line of the tooltip reads `<inventory> (+<chests>) / <need>`, white when the combined amount covers the need and blinking red when it is short.

#### Host plus one friend

1. The friend opens the crafting panel at a workbench in the host's base, with Wood in a base chest.

Expected: the friend's Wood line shows `(+<chests>)` equal to the Wood in the base chests.

2. The host puts 10 Wood into a base chest while the friend watches the panel.

Expected: within 2 seconds the friend's `(+<chests>)` grows by 10, and nothing in any inventory or chest changes because of the display.

### 9. AC-3.3 - Consumption order

Materials are taken from the inventory first, then from the base chests, nearest chest to the player first, and inside a chest the smallest stack first. Pick a recipe that needs 20 Wood at its first quality at a workbench. If the game offers only a different amount, scale the stack sizes below so that the order is the same.

#### Solo

1. Build a workbench with two wooden chests in range: chest A 3 m from the workbench and chest B 12 m from it. Put a stack of 30 Wood and a stack of 8 Wood in chest A, and a stack of 12 Wood in chest B. Keep exactly 6 Wood in the inventory. Stand at the workbench, select the recipe and press Craft.

Expected: the item appears. The inventory holds 0 Wood, chest A holds one stack of 24 Wood (the 8 stack was emptied first, then 6 came from the 30 stack), and chest B still holds 12 Wood. The log shows `Craft <token>: started, <takes> take(s) from 1 chest(s)` and `Craft <token>: Created`.

2. Choose a recipe that needs two kinds of material (for example Wood and Stone). Put part of each kind in the inventory and the rest in chest A and chest B, with the smaller stack in the farther chest. Craft it at the workbench.

Expected: each kind follows the same rule: the inventory is used first, then the nearest chest, then the farther one, and the smallest stack of a chest goes first.

3. Put exactly the missing amount of Wood in chest A, in one stack, and keep the rest of the recipe in the inventory. Craft it.

Expected: the item appears and chest A is empty (the stack is gone). The amount is exact: nothing is left over and nothing is missing.

4. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and no new `Craft` or `Reserve` line appears while you stand still.

#### Host plus one friend

1. The host sets up the chests, the stacks and the recipe of Solo step 1 at a workbench, with the friend next to it. The friend keeps exactly 6 Wood in the inventory and crafts.

Expected: the same inventory and chest contents as in Solo step 1. The friend's log shows `Reserve <id> [craft]: route SendRequest` for a chest the host owns and `Craft <token>: Created`.

2. The host crafts the same recipe again with the chests refilled as in step 1.

Expected: the same order. The host's log shows `route LocalOwner` for the chests the host owns.

### 10. AC-3.4 - Atomic consumption

Either the whole recipe is paid and the item appears, or nothing is consumed and the notice `Materials moved, try again` appears. See the Craft from base and Build from base sections below for the full steps.

#### Solo

1. Stand at a workbench inside a base with part of the Wood a recipe needs in the inventory and the rest in a chest of the base. Craft the recipe.

Expected: the item appears in the inventory, the Wood drops by exactly the recipe amount (inventory first, then the chest), and no chest window opens. The log shows `Craft <token>: started, <takes> take(s) from <chests> chest(s)` and `Craft <token>: Created`.

2. Equip the hammer, stand in range of the workbench with 4 Wood in the inventory and 20 Wood in a chest of the base, and place the wooden chest piece (10 Wood).

Expected: the chest is placed, the Wood in the inventory drops to 0 and the Wood in the base chest drops to 14. The log shows `Build <token>: started, <takes> take(s) from <chests> chest(s)` and `Build <token>: Placed`.

3. Make the Wood short by one unit in total (inventory plus base chests) and try to craft or place the piece.

Expected: the piece stays red and the recipe is not craftable. Nothing is taken from the inventory or from any chest.

#### Host plus one friend

1. The friend sets `ReserveTimeoutMs = 20000` in `[Network]` and restarts, so the window is wide. The friend stands at a workbench of the host's base and starts a craft that needs a chest of the host. Before the craft completes, the host takes the needed Wood out of that chest.

Expected: the friend sees `Materials moved, try again`, the friend's log shows `Craft <token>: Failed (<Outcome> at <id>)`, and the friend gets no item. The friend's inventory and every chest hold exactly what they held before, apart from the Wood the host took. Restore `ReserveTimeoutMs` to `1500`.

2. The friend crafts the same recipe again with the Wood in place.

Expected: the item appears for the friend and the chest loses exactly the planned amount. The friend's log shows `Reserve <id> [craft]: route SendRequest`, `Reserve <id> [craft]: open response granted consumed, chest UI not shown` and `Craft <token>: Created`.

3. The friend places a wooden chest piece from base Wood with the same setup as step 1, and the host takes the Wood out before the piece appears.

Expected: the friend sees `Materials moved, try again`, no piece is placed, and nothing is lost.

Also covered by: the consumption and conservation tests in `tests/BaseStorage.Core.Tests/Consumption` and `tests/BaseStorage.Core.Tests/Crafting`.

### 11. AC-3.5 - Upgrades and multi-craft

Upgrading an item and crafting several at once (hold Left Shift while the crafting panel is open) use the same availability and consumption path as a single craft. See the Upgrade and multi-craft section below for the full steps.

#### Solo

1. At a workbench inside a base, with an item in the inventory that the workbench can upgrade, keep fewer of one upgrade material in the inventory than the upgrade needs and the rest in a chest of the base. Select the item in the upgrade list and press Craft.

Expected: the material line shows `<inventory> (+<chests>) / <need>` with the upgrade amount in white, the item's quality rises by one in the same slot, and the material drops by exactly the need (inventory first, then the nearest chest).

2. Select Wood arrows (`ArrowWood`), keep fewer than 40 Wood in the inventory and the rest in a base chest, and hold Left Shift.

Expected: the Wood line reads `<inventory> (+<chests>) / 40` and Feathers reads `/ 10`. Pressing Craft gives five crafts' worth of arrows at once, and Wood drops by exactly 40 and Feathers by exactly 10.

3. Make the inventory plus the base chests hold 39 Wood.

Expected: with Left Shift held the Wood line blinks red and Craft cannot be pressed. Released, the line shows `/ 8` in white and a single craft takes exactly 8 Wood.

#### Host plus one friend

1. The friend upgrades at a workbench in the host's base with part of the upgrade material in a chest of the host.

Expected: the item's quality rises by one for the friend and the chest loses exactly the planned amount. The friend's log shows `Reserve <id> [craft]: route SendRequest` and `Craft <token>: Created`.

2. The friend crafts Wood arrows several at once (Left Shift) with the Wood split between the inventory and that chest.

Expected: Wood and Feathers drop by exactly 40 and 10 in total.

### 12. AC-3.6 - Toggle

#### Solo

1. Run the Solo steps of `## Craft from base off (AC-3.6)`.

Expected: every Expected line there holds: with `CraftFromBase = false` the chests are not counted and crafting and building are exactly vanilla.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Craft from base off (AC-3.6)`.

Expected: every Expected line there holds: the host's value of `CraftFromBase` applies to the friend for the session.

### 13. AC-4.1 - Category catalog

The categories come from `<ValheimDir>\BepInEx\config\BaseStorage.categories.txt`. The shipped default has 14 categories, in this order: Metals & Ores, Wood, Stone & Minerals, Hides & Leather, Fibres & Cloth, Raw Food, Cooked Food, Meads & Potions, Seeds & Saplings, Monster Drops, Trophies, Gear, Ammo and Misc.

#### Solo

1. Close the game, delete `<ValheimDir>\BepInEx\config\BaseStorage.categories.txt`, start the game and wait for the main menu. Open the file.

Expected: the file exists, starts with `# BaseStorage categories file (BaseStorage.categories.txt)`, has 14 category lines in the order above, and the log shows `Wrote default category file '...\BepInEx\config\BaseStorage.categories.txt' with <n> item(s)`.

2. Close the game, edit the file: add a comment line starting with `#` and a blank line anywhere, and remove `Coal` from the Wood line. Start a world, build a workbench with a wooden chest M, a wooden chest W and an empty wooden chest, and set the empty chest to Misc with the Category button. Put 1 Coal in chest M and 20 Wood in chest W, and set chest W to Wood. Press Organize base at the workbench and confirm.

Expected: the comment and the blank line change nothing. The Wood stays in chest W. The Coal, which no line of the file maps any more, falls back by item type (not food, not a trophy, not gear, not ammo) to Misc and ends up in the chest set to Misc.

3. Open a chest and cycle the Category button through the whole list.

Expected: the label shows Auto, then the 14 categories in the order of the file, then Keep, then Auto again.

#### Host plus one friend

The friend has a different `BaseStorage.categories.txt` than the host (for example the friend deletes the Wood line). Only the host needs the file for the steps below; the friend needs the mod.

1. The friend joins the host's world and opens a chest of the base.

Expected: the friend's Category button cycles through the categories of the host's file for the session, including Wood. The friend's own file is not changed. Run the Host plus one friend steps of `## Config sync (AC-6.2, AC-6.3)` for the log lines.

Also covered by: the category file tests in `tests/BaseStorage.Core.Tests/Categories` (`CategoryFileErrorTests` for parse errors with line numbers, `CategoryFileDuplicateTests` for duplicates, `CategoryResolveTests` and `FallbackCategories` for fallbacks).

### 14. AC-4.2 - Category on the chest

The category is stored on the chest under the key `basestorage.cat`, so every player sees it and it survives a restart. A chest that is set to Auto has no key.

#### Solo

1. Place a wooden chest, open it and set the category to Wood with the Category button. Close it and hover it.

Expected: the hover line ends with `[Wood]`.

2. Leave a second wooden chest alone and hover it.

Expected: the hover line has no category in brackets: an unassigned chest has no key.

3. Quit to the main menu, load the same world and hover both chests.

Expected: the first chest shows `[Wood]` and the second shows no category. The assignment survived the restart.

4. Open the first chest and cycle the category back to Auto. Hover it.

Expected: the hover line has no category in brackets.

5. Set the first chest to Wood again, quit the game, remove `BaseStorage.dll` and `BaseStorage.Core.dll` from `<ValheimDir>\BepInEx\plugins\BaseStorage\`, start the game, load the world and open the chest.

Expected: the chest opens as in vanilla, its items are intact, and the log has no error. Reinstall the mod and load the world: the chest shows `[Wood]` again, because vanilla ignores the leftover key.

#### Host plus one friend

1. The host sets a chest to Wood. The friend hovers it.

Expected: the friend sees `[Wood]` on the hover line within a second.

2. The friend leaves the game and rejoins, and the host quits the world and loads it again. Both hover the chest.

Expected: both see `[Wood]` again.

Also covered by: `ChestCategoryCodecTests` in `tests/BaseStorage.Core.Tests/ChestCategories` and `docs/adr/0001-basestorage-keys-ignored-by-vanilla.md`.

### 15. AC-4.3 - Manual assignment

#### Solo

1. Run the Solo steps of `## Category assignment (AC-4.3, AC-8.2)`.

Expected: every Expected line there holds: left-click cycles forward and right-click cycles back through Auto, the categories and Keep, the hover text shows the value, and the optional `CycleCategoryKey` does the same while hovering a chest.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Category assignment (AC-4.3, AC-8.2)`.

Expected: every Expected line there holds: the change is a write through the reservation protocol, so a chest another player has open is refused with `Chest category not changed: in use by <name>`.

### 16. AC-4.4 - Auto-assignment

At the start of an Organize, an unassigned chest takes the category that holds at least `AutoAssignThresholdPct` (default 50) of its stacks. A tie goes to the category listed first. A chest with no majority is then handled by the `AutoAssignMixedChests` setting (`[Organize]`, default `true`, host-synced): when it is `true`, Organize gives that chest a category that is short of space, and when it is `false` the chest stays unassigned and is only a source, as in 0.1.0. Use Wood, Stone and Flint: Wood comes before Stone & Minerals in the catalog, and Stone and Flint are both in Stone & Minerals.

#### Solo

1. Build a workbench with four wooden chests in range, all set to Auto. Put 4 stacks of Wood and 1 stack of Stone in chest A. Put 2 stacks of Wood and 2 stacks of Stone in chest B. Put 1 stack each of Wood, Stone and Arrow (any ammo) in chest C. Leave chest D empty. Press Organize base at the workbench and click yes.

Expected: after the run, hover each chest. Chest A shows `[Wood]` (4 of 5 stacks, 80 percent). Chest B shows `[Wood]` (a tie at 50 percent goes to the category listed first, Wood). Chest C is not assigned by the threshold (each category has 33 percent), but `AutoAssignMixedChests` is `true`, so Stone is short of space and chest C keeps its Stone and takes the Stone of chests A and B, while its Wood and Arrow leave to category chests. Chest D shows no category unless it was claimed as an overflow chest.

2. Set `AutoAssignMixedChests` to `false` in `[Organize]` of `<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg`, restart, refill chests A, B and C as in step 1 and run Organize again.

Expected: chest C is emptied as in 0.1.0: it stays unassigned, is only a source, and all its items move out to category chests. Set `AutoAssignMixedChests` back to `true`.

3. Set `AutoAssignThresholdPct` to 80 in `[Organize]` of `<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg`, restart, set chests A and B to Auto again, refill them as in step 1 and run Organize again.

Expected: chest A still shows `[Wood]` (80 percent reaches the threshold) and chest B shows no category (50 percent is below it). Set the value back to 50.

4. Fill a Wood chest completely with Wood, put one more Wood stack in another chest, keep one empty chest, and run Organize.

Expected: the empty chest is claimed for Wood and the extra stack ends up there. An empty chest is used only when the category chests run out of space.

5. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and no new `BaseStorage` line appears while you stand still.

#### Host plus one friend

1. The host sets up chests A and C as in Solo step 1 and the friend stands next to the workbench. The friend presses Organize base on the workbench panel and clicks yes.

Expected: the host hovers A and C. A shows `[Wood]` and C is handled as in Solo step 1 with `AutoAssignMixedChests` at its default `true`. The assignment is visible to both players.

2. The friend has chest A open while the host runs Organize.

Expected: the friend's chest is skipped (`1 chest skipped (in use by <FriendName>)` in the preview) and its category is not changed by the run.

3. The host sets `AutoAssignMixedChests` to `false` and restarts, the friend keeps `true`, and the friend rejoins. The friend presses Organize base on the workbench panel with chests A, B and C filled as in Solo step 1, and clicks yes.

Expected: the host's value wins because `AutoAssignMixedChests` is host-synced: chest C is emptied as in 0.1.0 (Solo step 2), although the friend's own file says `true`.

Also covered by: the tests in `tests/BaseStorage.Core.Tests/Categories/AutoAssign` (`AutoAssignMajorityTests`, `AutoAssignTieTests`, `AutoAssignThresholdTests`, `AutoAssignOverflowTests`).

### 17. AC-4.5 - Organize plan

#### Solo

1. Build a workbench with three wooden chests in range. Put 20 Wood and 10 Stone in chest A, 5 Flint and 30 Wood in chest B, and leave chest C empty. Set chest B to Wood and chest C to Stone & Minerals. Press Organize base and read the preview, then click yes.

Expected: the preview counts the stacks that are not in a chest of their own category. After the run the Wood is in chest B and the Stone and the Flint are in chest C. Nothing is lost: the totals are 50 Wood, 10 Stone and 5 Flint.

2. Put a stack of 10 Wood and a stack of 15 Wood in two different chests, both set to Wood, and run Organize.

Expected: the partial stacks are merged, so the Wood chests hold fewer stacks than before and the total Wood is unchanged.

3. Look at chest C after step 1.

Expected: the stacks are merged and ordered by item name, so Flint is before Stone.

4. Fill a Wood chest completely with Wood, put one more Wood stack in a chest that is not a Wood chest, remove every free chest, and run Organize.

Expected: the summary ends with `1 stack had no room` and the stack stays where it was.

5. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and no new `BaseStorage` line appears while you stand still.

#### Host plus one friend

1. The host and the friend each count the Wood, the Stone and the Flint in the base chests and in their inventories, and write the totals down. The friend runs Organize base with a misplaced Wood stack and a misplaced Stone stack in the base, and clicks yes.

Expected: the friend's summary line reads `Organized <N> stacks`. The host counts again: the totals are the same as before the run, and every stack is in a chest of its own category.

Also covered by: `OrganizeFuzzTests` (at least 1,000 random bases, conservation of every item type, no stack above its size, no Keep chest touched), `OrganizeTimingTests` (60 chests of 32 slots under 50 ms) and `OrganizeNoRoomTests` in `tests/BaseStorage.Core.Tests/Organize`.

### 18. AC-4.6 - Keep chests

A chest set to Keep is never a source or a destination for Organize or Deposit. It still counts for craft-from-base unless `KeepChestsCraftable` is false (default true).

#### Solo

1. Build a workbench with two wooden chests in range. Put 30 Wood in chest K and set it to Keep. Put 20 Wood in chest W and set it to Wood. Put 5 Stone in chest W as well. Press Organize base and click yes.

Expected: chest K still holds exactly 30 Wood, and chest W still holds its Wood. The Stone moves to a Stone & Minerals chest if there is one, otherwise it stays. Nothing moves into or out of chest K.

2. Put a Stone stack in your inventory (not on the hotbar and not a favourite) and press Put away inventory on the workbench panel.

Expected: the Stone does not go into chest K, even when K is the only chest left that could take it.

3. Pick a recipe that needs Wood and move the Wood it needs into chest K. With `KeepChestsCraftable` true, craft it at the workbench.

Expected: the recipe is craftable and the Wood is taken from chest K.

4. Set `KeepChestsCraftable = false` under `[General]` in `<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg`, restart, and open the crafting panel with the Wood only in chest K. Set it back to `true` afterwards.

Expected: the chest is not counted and the recipe is not craftable.

5. Run the Solo steps of `## Station buttons (AC-2.4, AC-4.7, AC-4.10)`.

Expected: every Expected line there holds, including the step where every chest of the base is set to Keep and the button is disabled with `No chests in this base`.

#### Host plus one friend

1. The friend sets a chest to Keep. The host puts a misplaced stack in it and runs Organize base.

Expected: the host sees that the friend's chest is untouched, and the stack stays where the host put it.

2. The friend crafts a recipe whose Wood is only in that chest, with `KeepChestsCraftable` true on the host.

Expected: the friend can craft, and the Wood is taken from the Keep chest.

Also covered by: `EligibilityRuleTests` and `EligibilityEvaluateTests` in `tests/BaseStorage.Core.Tests/Bases`, and the Keep cases of `OrganizeFuzzTests`.

### 19. AC-4.7 - Preview and confirm

#### Solo

1. Run the Solo steps of `## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)`.

Expected: every Expected line there holds: the preview opens before anything moves, the no button cancels without moving anything, yes runs the plan, and `OrganizeSkipPreview = true` skips the preview.

2. Run the Solo steps of `## Station buttons (AC-2.4, AC-4.7, AC-4.10)`.

Expected: every Expected line there holds: the button opens the same preview.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)`.

Expected: every Expected line there holds: a chest in use by another player is named in the preview.

### 20. AC-4.8 - Execution

#### Solo

1. Run the Solo steps of `## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)`.

Expected: every Expected line there holds: the moves go chest by chest through the reservation handshake, spread over frames by `MovesPerFrame`, and one summary line ends the run.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)`.

Expected: every Expected line there holds.

Also covered by: the execution tests with injected faults in `tests/BaseStorage.Core.Tests/Organize/Execution`.

### 21. AC-4.9 - Deposit (put away inventory)

#### Solo

1. Run the Solo steps of `## Put away inventory (AC-2.4, AC-4.8, AC-4.9, AC-4.10, AC-5.4)`.

Expected: every Expected line there holds: items move from the inventory to the category chests, and the hotbar, equipped items, favourites and the `DepositNeverItems` list are not touched.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Put away inventory (AC-2.4, AC-4.8, AC-4.9, AC-4.10, AC-5.4)`.

Expected: every Expected line there holds.

Also covered by: the deposit tests in `tests/BaseStorage.Core.Tests/Deposit` and `tests/BaseStorage.Core.Tests/Organize/Deposit`.

### 22. AC-4.10 - Station is the entry point

#### Solo

1. Run the Solo steps of `## Station buttons (AC-2.4, AC-4.7, AC-4.10)`.

Expected: every Expected line there holds: the buttons and the `Base: N stations, M chests, P% full` line appear next to the craft controls, are disabled with a tooltip when they cannot run, and `StationButtons = false` hides them.

2. Run the Solo steps of `## Put away inventory (AC-2.4, AC-4.8, AC-4.9, AC-4.10, AC-5.4)`.

Expected: every Expected line there holds.

3. Place a lone workbench and three wood chests in its range, all on Auto, and leave the chests empty. Use the workbench. Then put 1 stack of Wood in one chest and use the workbench again; then a second stack in another chest; then fill all 30 slots of the three chests.

Expected: the line reads `Base: 1 station, 3 chests, 0% full`, then `Base: 1 station, 3 chests, 3% full` (1 of 30 slots), then `Base: 1 station, 3 chests, 7% full` (2 of 30 slots, rounded to the nearest whole percent), then `Base: 1 station, 3 chests, 100% full`. Each change shows within 3 seconds. Setting one full chest to Keep gives `Base: 1 station, 2 chests, 100% full`, because a Keep chest counts neither as a chest nor as slots.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Station buttons (AC-2.4, AC-4.7, AC-4.10)`.

Expected: every Expected line there holds.

2. Run the Host plus one friend steps of `## Put away inventory (AC-2.4, AC-4.8, AC-4.9, AC-4.10, AC-5.4)`.

Expected: every Expected line there holds.

3. The host builds the Solo step 3 base with 1 stack of Wood in one chest. The friend uses the same workbench.

Expected: the friend's line reads `Base: 1 station, 3 chests, 3% full`, the same as the host's.

### 23. AC-5.1 - Ownership model, documented

Not observable in game: the ownership model is the content of an architecture decision record, not a behaviour of the running game.

#### Solo and host plus one friend

1. Open `docs/adr/0002-container-ownership-and-access-model.md`.

Expected: the file exists and its status is Accepted.

2. Read the Context section against the three rules of the acceptance criterion.

Expected: the ADR describes networked-object ownership (`ZNetView.ClaimOwnership` and `ZDO.SetOwner`), how a `Container` requests access (`RPC_RequestOpen` and `RPC_OpenResponse`, `RPC_RequestStack` and `RPC_StackResponse`), and where the inventory contents are serialized (the `ZDOVars.s_items` blob of the chest).

3. Read the Decision section.

Expected: the reservation protocol of AC-5.2 follows from it: it reuses the open handshake, suppresses the vanilla chest window on the requester, and lists the pre-checks, the timeout and the release.

### 24. AC-5.2 - Reservation protocol

#### Solo

1. Run the Solo steps of `## Chest reservation (AC-5.2, AC-5.3, AC-6.3)`.

Expected: every Expected line there holds: the chest is reserved through the open handshake, the chest window never opens for the requester, and the reservation is released.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Chest reservation (AC-5.2, AC-5.3, AC-6.3)`.

Expected: every Expected line there holds: grant, denial, timeout and a requester who leaves are all handled, and a reservation does not outlive its requester.

Also covered by: the reservation state machine tests in `tests/BaseStorage.Core.Tests/Reservations`.

### 25. AC-5.3 - In-use chests

#### Solo

1. Run the Solo steps of `## Chest reservation (AC-5.2, AC-5.3, AC-6.3)`.

Expected: every Expected line there holds.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Chest reservation (AC-5.2, AC-5.3, AC-6.3)`.

Expected: every Expected line there holds: a chest the friend has open is reported as in use by the friend and the host never closes it.

2. Run the Host plus one friend steps of `## Base membership (AC-2.3, AC-2.5, AC-5.3)`.

Expected: every Expected line there holds: the chest the friend keeps open is counted as in use by another.

### 26. AC-5.4 - One Organize per base

#### Solo

1. Run the Solo steps of `## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)`.

Expected: every Expected line there holds: the lock is released when the run ends.

2. Run the Solo steps of `## Put away inventory (AC-2.4, AC-4.8, AC-4.9, AC-4.10, AC-5.4)`.

Expected: every Expected line there holds.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)`.

Expected: every Expected line there holds: a second player sees `Organize in progress by <name>`, and a lock left by a player who quit expires after `OrganizeLockSeconds`.

2. Run the Host plus one friend steps of `## Put away inventory (AC-2.4, AC-4.8, AC-4.9, AC-4.10, AC-5.4)`.

Expected: every Expected line there holds.

### 27. AC-5.5 - Conservation under concurrency

Not observable in game: a concurrent schedule of crafts, Organizes and Deposits cannot be repeated by hand, so it is checked by the Core simulation tests.

#### Solo and host plus one friend

1. Run `dotnet test tests/BaseStorage.Core.Tests -c Release --filter FullyQualifiedName~BaseStorage.Core.Tests.Simulation` from the repository root.

Expected: the run reports `Failed: 0`. The output of each shard logs the fixed seed (`simulation seed=20261028`) and the schedule range it ran.

2. Open `tests/BaseStorage.Core.Tests/Simulation/SimulationTests.cs` and read the totals the shards log.

Expected: the corpus holds at least 500 random schedules with two to three simulated peers, and the shards together cover all of them. After every schedule the conservation invariant holds: every item type is still present in the same total amount.

### 28. AC-6.1 - Version check

On every new connection both sides send their mod version once. With `RequireSameVersion = true` (the default) the host refuses a friend with a different version or without the mod, through the vanilla "Incompatible version" screen. See the Version handshake section below for the full steps.

#### Solo

1. Start a single-player world, play for a minute, and check `<ValheimDir>\BepInEx\LogOutput.log`.

Expected: no line contains `Version handshake`. Single-player has no peers, so nothing is sent or refused.

#### Host plus one friend

The host starts the world with "Start server" and `RequireSameVersion = true`. The friend has the same BaseStorage build unless a step says otherwise.

1. The friend joins the host's world with the same version.

Expected: the friend joins normally. The host log gains `Version handshake: peer <id> accepted, host <version>, client <version>` with both versions equal.

2. The friend installs a build with a different version (for example a package built with `-p:Version=0.9.9`) and joins.

Expected: the friend does not enter the world and sees the vanilla "Incompatible version" error with one extra line, `BaseStorage version mismatch: host <hostVersion>, you <friendVersion>`. The host log gains `Version handshake: peer <id> refused, versions differ, host <hostVersion>, client <friendVersion>`.

3. The friend deletes `BepInEx\plugins\BaseStorage`, keeps BepInEx, and joins.

Expected: the friend does not enter the world and sees the vanilla "Incompatible version" error about 5 seconds after connecting. The host log gains `Version handshake: peer <id> refused, no BaseStorage payload, host <hostVersion>`.

4. The host sets `RequireSameVersion = false` in the ConfigurationManager (F1), and the friend without the mod joins again. Set the value back to `true` afterwards.

Expected: the friend joins and plays vanilla, and the host log gains `Version handshake: peer <id> accepted, host <hostVersion>, client none`.

### 29. AC-6.2 - Host-synced config

#### Solo

1. Run the Solo and hosting steps of `## Config sync (AC-6.2, AC-6.3)`.

Expected: every Expected line there holds: nothing is sent or awaited while alone or hosting, and the local file is unchanged.

#### Host plus one friend

1. Run the Host plus one friend steps of `## Config sync (AC-6.2, AC-6.3)`.

Expected: every Expected line there holds: the friend uses the host's settings and category file for the session, and the friend's local file and category file are never written.

### 30. AC-6.3 - Single-player parity

#### Solo

1. Start a single-player world, play for a minute, and open and close a chest. Check `<ValheimDir>\BepInEx\LogOutput.log`.

Expected: no `Version handshake` line appears. Nothing is sent or awaited, because single-player has no peers.

2. Run the Solo steps of `## Chest reservation (AC-5.2, AC-5.3, AC-6.3)`.

Expected: every Expected line there holds: the route is `LocalOwner` and no request is sent.

3. Run the Solo and hosting steps of `## Config sync (AC-6.2, AC-6.3)`.

Expected: every Expected line there holds.

#### Host plus one friend

1. Run the Solo and hosting steps of `## Config sync (AC-6.2, AC-6.3)`, where the friend hosts a world and plays alone.

Expected: every Expected line there holds: the host sends and awaits nothing while alone, and uses its local values.

2. The host builds a chest and stands next to it with the friend more than 100 m away. The host crafts, organizes and puts away inventory.

Expected: everything works for the host without a network handshake: the log shows `route LocalOwner` for the chests the host owns and no `Version handshake` line for the host's own actions.

## Base membership (AC-2.3, AC-2.5, AC-5.3)

Checks that BaseStorage tracks the loaded containers and crafting stations, and that carts, ships, tombstones and private chests are counted the way the base rules say. This section needs Debug logging: set it up as in the `## Debug logging` section first. Every expected result is a `[Debug  :BaseStorage]` line in `<ValheimDir>\BepInEx\LogOutput.log`.

Two kinds of line are used. `<n>` is the number of objects of that kind that are loaded after the change, and `<id>` is `<UserId>:<Id>` of the station.

- Registry change: `Registry: <container|station> <added|removed> (<n> loaded)`
- Base summary: `Base at station <id>: <S> station(s), <C> container(s), <E> eligible; excluded <T> tombstone, <V> vehicle, <P> not player-built, <A> access denied; <U> in use by another`. When the station is not in a base the line is `Base at station <id>: not in a base`.

Read the counts from the base summary line that is written when you place a station. Summary lines written while a zone is still loading can show partial counts: ignore them. `<S>` is the number of workbenches placed so far in the test world, so it grows by one with each workbench you place. Start from a new world, in an area with no other crafting station, with `DebugLogging = true`. A workbench has a build range of 20 m, so "in range" means within 20 m of the workbench.

### Solo

1. Place a workbench on empty ground.

Expected: `Registry: station added (1 loaded)`, then `Base at station <id>: 1 station(s), 0 container(s), 0 eligible; excluded 0 tombstone, 0 vehicle, 0 not player-built, 0 access denied; 0 in use by another`.

2. Place two wooden chests in range of the workbench, then place a second workbench in range of the first.

Expected: two lines `Registry: container added`, ending with `(2 loaded)`. Then `Registry: station added (2 loaded)` and `Base at station <id>: 2 station(s), 2 container(s), 2 eligible; excluded 0 tombstone, 0 vehicle, 0 not player-built, 0 access denied; 0 in use by another`.

3. Remove one of the chests with the hammer.

Expected: `Registry: container removed (1 loaded)`. No other `Registry:` line is written.

4. Make sure `ExcludeVehicles` is true (the default). Build a cart in range of the workbench, then place a third workbench in range.

Expected: `Registry: container added (2 loaded)` for the cart, `Registry: station added (3 loaded)` and `Base at station <id>: 3 station(s), 2 container(s), 1 eligible; excluded 0 tombstone, 1 vehicle, 0 not player-built, 0 access denied; 0 in use by another`.

5. Set `ExcludeVehicles` to false in the ConfigurationManager (F1), then place a fourth workbench in range.

Expected: `Registry: station added (4 loaded)` and `Base at station <id>: 4 station(s), 2 container(s), 2 eligible; excluded 0 tombstone, 0 vehicle, 0 not player-built, 0 access denied; 0 in use by another`. The cart is now eligible, so eligible is one higher than in step 4.

6. Set `ExcludeVehicles` back to true. Remove the cart with the hammer. Moor a ship in range of the workbench: build a karve (the raft has no chest, so it adds no container), then place a fifth workbench in range.

Expected: `Registry: container removed (1 loaded)` for the cart, then `Registry: container added (2 loaded)` for the ship storage, then `Registry: station added (5 loaded)` and `Base at station <id>: 5 station(s), 2 container(s), 1 eligible; excluded 0 tombstone, 1 vehicle, 0 not player-built, 0 access denied; 0 in use by another`.

7. Die in range of the workbench (for example by jumping from a height, or with the console command `suicide` if the console is enabled), respawn, walk back to the workbench and place a sixth workbench in range.

Expected: `Registry: container added (3 loaded)` when the tombstone appears, then `Registry: station added (6 loaded)` and `Base at station <id>: 6 station(s), 3 container(s), 1 eligible; excluded 1 tombstone, 1 vehicle, 0 not player-built, 0 access denied; 0 in use by another`.

8. Write down the `(<n> loaded)` values of the last container line and the last station line. Walk far enough away that the base zone unloads (more than 250 m in a straight line), wait 30 seconds, then walk back until the base is visible again.

Expected: a run of `Registry: container removed` and `Registry: station removed` lines while you walk away, then the same number of `added` lines on the way back. After the return the last `(<n> loaded)` values of each kind equal the values you wrote down, with no duplicate counts (3 containers and 6 stations). Ignore the base summary lines written while the zone loads.

9. Place a campfire next to the workbench.

Expected: no new `Registry:` line and no new `Base at station` line. A campfire is neither a container nor a crafting station.

10. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while you stand still for 30 seconds no new `Registry:` or `Base at station` line appears (nothing runs every frame).

### Host plus one friend

The host runs the world with `DebugLogging = true` and reads the host `LogOutput.log`. Use a new world with one workbench placed by the host, no chests and no other station. The friend joins and stands next to the host's workbench. Only the host needs the mod for these steps.

1. The friend builds a chest in range of the workbench and sets it to Private. The host then places a second workbench in range.

Expected: on the host, `Registry: container added (1 loaded)` and `Base at station <id>: 2 station(s), 1 container(s), 0 eligible; excluded 0 tombstone, 0 vehicle, 0 not player-built, 1 access denied; 0 in use by another`.

2. The friend sets the chest to Public. The host places a third workbench in range.

Expected: `Base at station <id>: 3 station(s), 1 container(s), 1 eligible; excluded 0 tombstone, 0 vehicle, 0 not player-built, 0 access denied; 0 in use by another`. The chest is eligible again.

3. The friend places a ward next to the chest and does not add the host to its permitted list. The host places a fourth workbench outside the ward radius, still within 20 m of the chest.

Expected: `Base at station <id>: 4 station(s), 1 container(s), 0 eligible; excluded 0 tombstone, 0 vehicle, 0 not player-built, 1 access denied; 0 in use by another`. The Public chest counts as access denied because the ward does not permit the host.

4. The friend removes the ward, opens the chest and keeps it open. The host places a fifth workbench in range.

Expected: `Registry: station added (5 loaded)` and `Base at station <id>: 5 station(s), 1 container(s), 1 eligible; excluded 0 tombstone, 0 vehicle, 0 not player-built, 0 access denied; 1 in use by another`.

5. The friend closes the chest. Check the host log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while you stand still for 30 seconds no new `Registry:` or `Base at station` line appears (nothing runs every frame).

## Hover inspect (AC-2.6, AC-8.3)

Checks the line that BaseStorage adds to the hover text of a chest and of a crafting station. Point the crosshair at the object and read the hover text. BaseStorage adds exactly one line at the end of the vanilla hover text; the vanilla lines stay as they are. Debug logging is not needed.

The line has these forms:

- Chest in a base: `[In base: 1 station]` or `[In base: <N> stations]`.
- Chest in no base: `[Not in a base]`.
- Chest with a category: the same line followed by the category in brackets, for example `[In base: 2 stations] [Ores]`, or `[Keep]` when the chest is set to Keep. A chest set to Auto shows no category.
- Crafting station in a base: `[Base: 1 chest]` or `[Base: <N> chests]`. A station that is in no base shows no added line. Carts, ships and tombstones are not counted as chests.

The line comes from the cached base, so after you place or remove a chest or station it can take up to the cache time (`BaseCacheSeconds`, 10 seconds by default) to change unless the placing or removing forces a refresh. Wait 10 seconds before you read a changed count. Start from a new world in an area with no other crafting station.

### Solo

1. Place a wooden chest on empty ground, with no crafting station near it. Hover the chest.

Expected: the vanilla chest text, then one added line `[Not in a base]`.

2. Place a workbench within 20 m of the chest. Hover the chest.

Expected: the chest shows `[In base: 1 station]`.

3. Place a second workbench within 20 m of the first. Hover the chest.

Expected: the chest shows `[In base: 2 stations]`.

4. Hover a workbench.

Expected: the workbench shows `[Base: 1 chest]`.

5. Place a second wooden chest in range of the workbenches and build a cart in range as well. Wait 10 seconds, then hover a workbench.

Expected: the workbench shows `[Base: 2 chests]`, because a cart is not a chest: carts, ships and tombstones are never counted, whatever `ExcludeVehicles` is set to.

6. Remove both wooden chests and the cart with the hammer, wait 10 seconds, then hover a workbench.

Expected: the workbench shows `[Base: 0 chests]`.

7. Category display (story S-E004-004, see `## Category assignment`): set a chest in a base to the category Ores with the Category button and hover it.

Expected: `[In base: 2 stations] [Ores]`, still one added line.

8. Likewise (story S-E004-004): set a chest to Keep and hover it.

Expected: the chest line ends with `[Keep]`, for example `[In base: 2 stations] [Keep]`.

9. Check the whole log in `<ValheimDir>\BepInEx\LogOutput.log`, then keep the crosshair on a chest for 30 seconds.

Expected: no `BaseStorage` line contains `failed and was skipped`, no new `BaseStorage` line is written during the 30 seconds of hovering, and every hover shows exactly one added line.

### Host plus one friend

The host runs the world and the friend joins and stands next to the host. Only the host needs the mod for these steps. The host places one workbench and one wooden chest in range of it.

1. The host hovers the chest and then the workbench.

Expected: the chest shows `[In base: 1 station]` and the workbench shows `[Base: 1 chest]` on the host.

2. The friend builds a second chest in range of the workbench. The host waits 10 seconds and hovers the workbench.

Expected: the workbench shows `[Base: 2 chests]` on the host.

3. The host walks more than 30 m from every station and hovers a chest placed there by the friend.

Expected: that chest shows `[Not in a base]`.

4. The host checks the log in `<ValheimDir>\BepInEx\LogOutput.log` and keeps the crosshair on a chest for 30 seconds.

Expected: no `BaseStorage` line contains `failed and was skipped`, nothing new is logged during the 30 seconds, and every hover shows exactly one added line.

## Category assignment (AC-4.3, AC-8.2)

Checks the Category button of the open-chest panel and the optional cycle hotkey. The button is a copy of the vanilla Take All button, named `BaseStorage.chest.category.cycle-button`, on the same row as Take All and Stack All, labelled with the chest's current value. The cycle is Auto, then every category of `BepInEx\config\BaseStorage.categories.txt` in file order, then Keep, and back to Auto. Every change is a write to the chest's networked data through the chest reservation handshake, so it shows one top-left message `Chest category: <value>` when it applies and one centre message `Chest category not changed: <reason>` when it does not. Debug logging is not needed. The hover line is the one from the `## Hover inspect` section: the category appears in brackets after the base part, and Auto shows no category.

### Solo

1. Place a wooden chest, press E to open it and look at the container panel.

Expected: a third button sits on the Take All / Stack All row, same size and style as Take All, labelled `Auto`. It does not overlap the two vanilla buttons or the chest name.

2. Left-click the Category button once.

Expected: the label changes to the first category of the categories file (`Metals & Ores` with the shipped file) and the top-left message reads `Chest category: Metals & Ores`. The chest's items are not moved.

3. Left-click the Category button again until the label reads `Keep`, then once more.

Expected: the label walks every category of the file in order, then `Keep`, then back to `Auto`; each click shows one `Chest category: <value>` message.

4. Right-click the Category button once.

Expected: the label reads `Keep` (one step back from Auto) and the message reads `Chest category: Keep`.

5. Close the chest and hover it.

Expected: the hover text has exactly one added line, ending in `[Keep]`, for example `[Not in a base] [Keep]`.

6. Open the chest again and press Escape to close it.

Expected: the label reads `Keep` when the panel opens (the value is read from the chest, not remembered by the button), and the button disappears with the container panel.

7. Open the inventory with Tab and no chest.

Expected: no Category button is shown.

8. Set `CycleCategoryKey = G` under `[Keys]` in `<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg` (or through the ConfigurationManager, F1), close every menu, hover the chest and press G.

Expected: the message reads `Chest category: Auto` (one step forward from Keep) and the hover line loses `[Keep]`. Pressing G without hovering a chest, or while a menu, the chat or the console is open, does nothing.

9. Open the chat with Enter, type `g`, close the chat.

Expected: no category message and no change: the hotkey is ignored while typing.

10. Set `CycleCategoryKey` back to empty.

Expected: pressing G on a hovered chest does nothing.

11. Check `<ValheimDir>\BepInEx\LogOutput.log`.

Expected: no `BaseStorage` line contains `failed and was skipped`.

### Host plus one friend

1. The host opens a chest the host placed and left-clicks the Category button.

Expected: the label and the message read `Metals & Ores` for the host; the friend hovers the chest and sees `[Metals & Ores]` on the hover line within a second.

2. The friend opens the same chest and left-clicks the Category button.

Expected: the friend's label changes to the next category and shows `Chest category: <value>`; the host hovers the chest and sees the new value.

3. The host opens the chest and keeps it open; the friend hovers it and presses the bound `CycleCategoryKey`.

Expected: the friend sees `Chest category not changed: in use by <host name>` and the hover line keeps the old value; the host's panel is unaffected.

4. The friend opens a chest the host placed; the host walks far away; the friend left-clicks the Category button.

Expected: the button greys out until the write finishes, the friend sees either `Chest category: <value>` or `Chest category not changed: no answer from the chest owner`, and no items move.

## Gamepad cursor (AC-8.2, AC-7.3)

Checks that the station and chest-panel buttons can be reached with the vanilla gamepad cursor. There is no dedicated gamepad binding in v1; all hotkeys are unbound by default, so a controller player uses the on-screen buttons only. This is recorded in the known limits of `package/README.md` as "No gamepad bindings".

### Solo

1. Connect a gamepad. Open `<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg` and check that every hotkey under the `[Keys]` section is unbound (no key is set on any line). Start the game and load a world.

Expected: no keyboard or mouse hotkey is bound; every hotkey line under `[Keys]` is empty or `None`, and there is no dedicated gamepad binding in v1.

2. Stand in a base at a workbench and open the crafting panel. Push the left stick to move the vanilla gamepad cursor onto the **Organize base** button, then press the vanilla confirm button (A).

Expected: the Organize preview opens, as in Station buttons.

3. Close the preview. With the gamepad cursor, move onto the **Put away inventory** button and press the confirm button (A).

Expected: the Put away preview opens (or the run starts if `DepositPreview` is off), as in Put away inventory.

4. Open a chest in a base. Move the gamepad cursor onto the **Category** button and press the confirm button (A).

Expected: the Category label advances to the next value and `Chest category: <value>` shows.

## Organize console command (AC-2.4, AC-5.4)

Checks the in-game console command. Start Valheim with the `-console` launch option and open the console with F5. The command is `basestorage organize`. It organizes the base you stand in, with the same preview, lock and summary as the Organize base button. It uses the base and the misplaced Wood, Stone and Flint of `## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)`. `<YourName>`, `<HostName>` and `<FriendName>` are the character names.

### Solo

1. Open the console and type `help`.

Expected: the list contains `basestorage` with `organize - start an Organize for the base you are in`.

2. Type `basestorage` alone, then `basestorage sort`.

Expected: each prints `Usage: basestorage organize` in the console, and nothing else happens.

3. Stand outside a base (more than 30 m from any crafting station) and type `basestorage organize`.

Expected: the console prints `Not in a base`, the centre of the screen shows `Not in a base`, no popup opens and nothing moves.

4. Stand in the base next to the workbench and type `basestorage organize`, then press F5 to close the console.

Expected: the console prints `Organize preview open, close the console to confirm`. After F5 closes the console, the vanilla popup titled `Organize base` is open.

5. Click yes.

Expected: the items move and the top-left reads `Organized <N> stacks`.

6. Misplace a stack, run the command, then click no.

Expected: the top-left reads `Organize cancelled` and nothing moves.

7. Set `OrganizeSkipPreview = true` and run the command. Set it back to `false` afterwards.

Expected: the console prints `Organize started`, no popup opens and the top-left reads `Organized 1 stack`.

8. Set every chest to Keep and run the command. Set the chests back to Auto afterwards.

Expected: the console and the centre of the screen show `No chests in this base`.

9. Run the command, leave the popup open, reopen the console and run it again. Then close the console and click no.

Expected: the console and the centre of the screen show `Organize in progress by <YourName>`.

10. Type `BaseStorage ORGANIZE`.

Expected: it behaves as step 4.

### Host plus one friend

1. The host clicks Organize base on the workbench and leaves the preview open. The friend, inside the same base, runs `basestorage organize`. The host then clicks no.

Expected: the friend's console and the centre of the screen show `Organize in progress by <HostName>`, no popup opens for the friend and nothing moves.

2. The friend runs the command again in the base and confirms the preview.

Expected: the friend's console prints `Organize preview open, close the console to confirm`, the items move, the friend sees the summary and the host sees the chests change.

3. The friend stands outside a base and runs the command.

Expected: the friend's console and the centre of the screen show `Not in a base`.

4. Set `MovesPerFrame` = 1 with many misplaced stacks. While the friend's run moves items, the host runs the command.

Expected: the host's console and the centre of the screen show `Organize in progress by <FriendName>`.

## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)

Checks the first end-to-end Organize: the hotkey starts it for the base you stand in, the preview asks for a confirmation, the moves are spread over frames through the chest reservation handshake, one summary line ends the run, and the base lock refuses a second player. Set `DebugLogging = true` (see `## Debug logging`) so the `Reserve` lines of the run are visible; the summary itself is an Info line. The lines used are:

- Summary (Info): `[Info   :BaseStorage] Organized <N> stacks; <skips>; <M> stacks had no room`, or `Nothing moved` when nothing had to move, with `stopped early: <reason>` when the run did not finish
- Reservation detail (Debug): `Reserve <id> [organize]: ...` as in the `## Chest reservation` section

Before starting, bind the hotkey: in `<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg` under `[Keys]` set `OrganizeKey = G` (any unused key). `OrganizeKey` is unbound by default; `OrganizeSkipPreview = false`, `MovesPerFrame` = 20 and `OrganizeLockSeconds = 30` are the defaults under `[Organize]`. The category of a chest comes from the default categories (the category file is not read yet), so use Wood, Stone and Flint in the steps below.

### Solo

1. Stand in an open field more than 30 m from any crafting station and press the hotkey.

Expected: the centre of the screen shows `Not in a base`. Nothing else happens and no `Reserve` line is written.

2. Place a workbench, then three wooden chests in range of it. Put 20 Wood and 10 Stone into chest A, 5 Flint and 30 Wood into chest B, and leave chest C empty. Hover chest A and set its category to Wood with the Category button when that story has landed; otherwise leave every chest on Auto (the chest with the most Wood is auto-assigned Wood at the start of the run). Stand next to the workbench and press the hotkey.

Expected: a vanilla yes/no popup titled `Organize base` opens with a body like `2 stacks to move across 1 chest. 1 chest will be claimed for Stone.` No item has moved yet. The chest windows are closed.

3. Click the no button.

Expected: the popup closes and the top-left message reads `Organize cancelled`. The chests are unchanged, and `<ValheimDir>\BepInEx\LogOutput.log` has no `Organized` line.

4. Press the hotkey again and click the yes button.

Expected: the popup closes, the Wood ends up in the Wood chest, the Stone and Flint in a chest claimed for Stone, and within a second the top-left message reads `Organized <N> stacks` (for example `Organized 2 stacks`). The log shows exactly one `[Info   :BaseStorage] Organized <N> stacks` line and, as Debug, `Reserve <id> [organize]: route LocalOwner` and the `Granted -> Writing` and `Releasing -> Completed` transitions for every chest that was written. No `stopped early` text appears.

5. Fill a Wood chest completely with Wood, put one more Wood stack into another chest, remove every other free chest, and press the hotkey, then yes.

Expected: the summary ends with `1 stack had no room`, and the Wood stack stays where it was.

6. Set `OrganizeSkipPreview = true` in the ConfigurationManager (F1), move a Wood stack into the Stone chest, and press the hotkey.

Expected: no popup; the Wood moves back at once and the `Organized 1 stack` message appears. Set `OrganizeSkipPreview` back to `false`.

7. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, exactly one `Organized` Info line per run you started, and while you stand still no new `BaseStorage` line appears (an idle tick does nothing).

8. Set `MovesPerFrame` = 1 in the ConfigurationManager (F1), fill the base with at least 10 misplaced stacks, and press the hotkey, then yes. Watch the chests while the run moves items. Afterwards read `<ValheimDir>\BepInEx\LogOutput.log` and check the lowest-id workbench of the base. Set `MovesPerFrame` back to 20.

Expected: the stacks move one per frame over several frames instead of all at once, the top-left message reads `Organized <N> stacks`, and the lock is released when the run ends: pressing the hotkey again at once is accepted and shows the preview instead of `Organize in progress by <YourName>`.

### Host plus one friend

The host runs the world with `DebugLogging = true` and `OrganizeKey = G`, and reads the host `LogOutput.log`. The friend needs the mod for steps 3 and 4, with the same `OrganizeKey`. `<FriendName>` is the friend's character name as shown in the player list.

1. The friend opens a chest in the base and keeps it open. The host presses the hotkey.

Expected: the popup body contains `1 chest skipped (in use by <FriendName>)`. After yes, the friend's chest is untouched, the friend's chest window stays open, and the summary contains `1 chest skipped (in use by <FriendName>)`.

2. The friend closes the chest and stays next to it, so the friend owns it. The host puts a misplaced stack into that chest and presses the hotkey, then yes.

Expected: the log shows `Reserve <id> [organize]: route SendRequest` and `open response granted consumed, chest UI not shown` for that chest, the stack moves, no chest window opens on the host, and the summary line ends the run.

3. The host sets `MovesPerFrame` = 1 and fills the base with 40 misplaced stacks, then presses the hotkey and yes. While the host's run is moving items, the friend presses the friend's hotkey inside the same base.

Expected: the friend sees `Organize in progress by <HostName>` in the centre of the screen and nothing moves for the friend. The host's run finishes with its summary. The friend presses the hotkey again after the host's summary and gets the preview.

4. The friend starts a run and, while it moves items, the host presses the hotkey.

Expected: the host sees `Organize in progress by <FriendName>`. The host checks the lowest-id workbench of the base after the friend's summary: a new run by the host is accepted (the lock was released).

5. The friend starts a run and quits to the desktop within the first second. The host waits 35 seconds and presses the hotkey.

Expected: the host's preview opens (the abandoned lock expired after `OrganizeLockSeconds`). Pressing yes runs the Organize normally.

6. The host checks the log, then stands still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, every run has exactly one `Organized` Info line, and while standing still no new `BaseStorage` line appears.

The lock lost while a run is moving items (the run aborts with `stopped early: the base lock was lost`), a chest that changes under the run (`stopped early: a chest changed during the run`) and a full destination (`stopped early: chest full`) are covered by unit tests, not by manual steps. `No chests in this base` appears when the base has no usable chest.

## Station buttons (AC-2.4, AC-4.7, AC-4.10)

Checks the Organize base button on the crafting panel. Using a crafting station that belongs to a base shows an **Organize base** button and a `Base: N stations, M chests, P% full` line next to the craft controls. The button organizes that station's base, whatever the player's position, and it opens the same preview as the hotkey. Two settings apply: `StationButtons` (`[General]`, default `true`, local to each player) shows or hides the button and the line, and `OrganizeSkipPreview` (`[Organize]`, default `false`) skips the preview. The Put away inventory button is not part of this section. With `DebugLogging = true` (see `## Debug logging`) the lines go to `<ValheimDir>\BepInEx\LogOutput.log`: `[station.actions.organize-button] Organize <Outcome>` for every click and `[station.actions.row] hidden: no room next to the craft controls` once per session when the controls do not fit.

### Solo

1. Stand away from every crafting station and open the inventory with Tab.

Expected: the inventory shows no Organize base button and no `Base:` line.

2. Place a lone workbench with no chest in range and use it. Hover the Organize base button.

Expected: the button appears next to the craft controls without covering the recipe list, the requirements or the craft button. It is disabled, the tooltip reads `No chests in this base`, and the line reads `Base: 1 station, 0 chests, 0% full`.

3. In the base of the Organize section (a workbench and three chests with misplaced Wood, Stone and Flint), use the workbench.

Expected: the button is enabled with no tooltip, and the line reads `Base: 1 station, 3 chests, <P>% full`.

4. Click Organize base.

Expected: the `Organize base` yes/no popup opens, nothing has moved, and the log shows `[station.actions.organize-button] Organize Started`.

5. Click no in the popup.

Expected: the top-left message reads `Organize cancelled`, and every chest holds exactly what it held before.

6. Click Organize base again, then click yes.

Expected: the items move, the top-left message reads `Organized <N> stacks`, and within a second of the summary the button is enabled again.

7. Build a forge in range of the workbench and use the forge.

Expected: the line reads `Base: 2 stations, 3 chests, <P>% full`, and Organize base from the forge organizes the same chests.

8. Set `OrganizeSkipPreview = true` in the ConfigurationManager (F1), misplace one stack and click Organize base. Set it back to `false` afterwards.

Expected: no popup opens, and the stack moves at once.

9. Set `StationButtons = false` in the ConfigurationManager (F1) while using the workbench, then set it back to `true`.

Expected: within a second of the first change the button and the line disappear, and after the second change they come back.

10. Set every chest of the base to Keep with the Category button, look at the panel, then set the chests back to Auto.

Expected: the button is disabled with the tooltip `No chests in this base` and the line reads `Base: 1 station, 0 chests, 0% full`.

11. Use the workbench at 1280x720 with UI scale 0.8, then 1.2, then at 1920x1080 and (if the monitor allows) 3840x2160 with UI scale 1.0 and 1.2.

Expected: the button and the line stay on screen and never cover the recipe list, the requirements or the craft button. When they are hidden instead, the log has `[station.actions.row] hidden: no room next to the craft controls`.

12. Check the whole log, then keep the station panel open for 30 seconds without clicking anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while the panel stays open for 30 seconds with nothing clicked, no new `BaseStorage` line appears.

### Host plus one friend

Both players use the same build. The host runs the world with `DebugLogging = true`. `<HostName>` and `<FriendName>` are the character names as shown in the player list.

1. The host sets `MovesPerFrame = 1`, misplaces 40 stacks and starts Organize base from the workbench, then clicks yes. Meanwhile the friend uses a station of the same base.

Expected: within a second the friend's button is disabled and hovering it shows `Organize in progress by <HostName>`. After the host's summary the friend's button is enabled again within a second.

2. The friend clicks Organize base at a station of the host's base and confirms.

Expected: the run moves the chests of the host's base, and the host's button at the workbench shows `Organize in progress by <FriendName>` until the friend's summary.

3. The friend keeps a station of the host's base open. The host starts a long run as in step 1, and the friend clicks Organize base within the first second, before the friend's button turns disabled.

Expected: the friend sees `Organize in progress by <HostName>` in the centre of the screen and nothing moves for the friend.

4. The friend sets `StationButtons = false` in the ConfigurationManager (F1), then sets it back to `true`.

Expected: while it is `false` the friend sees neither the button nor the line while the host still sees both (the setting is local), and after the change back the friend sees both again.

5. Check the log on both peers, then keep a station panel open for 30 seconds without clicking anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while the panel stays open for 30 seconds with nothing clicked, no new `BaseStorage` line appears on either peer.

The placement over every resolution and scale corner, the disabled-reason precedence and the hidden states are covered by the Core `StationPanelLayout` tests, and the base facts, the geometry and the refresh memo by the station-actions unit tests, not by manual steps.

## Put away inventory (AC-2.4, AC-4.8, AC-4.9, AC-4.10, AC-5.4)

Checks the Put away inventory button and the optional hotkey. Using a crafting station that belongs to a base shows a **Put away inventory** button to the right of **Organize base**, and it puts your inventory into the chests of that station's base by their categories. The optional `DepositKey` (`[Keys]`, unbound by default) does the same for the base you stand in. `DepositPreview` (`[Organize]`, default `false`) shows a yes/no preview first. The hotbar (top row), equipped items, favourites (Alt+click a slot) and the items in `DepositNeverItems` (default `@mead, @ammo, Hammer, Hoe, Cultivator, FishingRod, @bait`) never move. Every Put away ends with one top-left line such as `Put away: 4 stacks moved, 2 no room`. With `DebugLogging = true` (see `## Debug logging`) the line `[station.actions.deposit-button] Deposit <Outcome>` appears in `<ValheimDir>\BepInEx\LogOutput.log` for every click.

### Solo

1. In the base of the Organize section, put Wood 30 in the hotbar slot 1, equip an axe, carry 10 Cooked meat, Alt+click a stack of Resin to make it a favourite, and carry 40 Stone and 20 Flint in rows 2-4. Use the workbench.

Expected: the Put away inventory button sits to the right of Organize base, and both buttons are enabled.

2. Click Put away inventory.

Expected: no popup opens, the Stone and Flint go to their chests within a second, the top-left message reads `Put away: 2 stacks moved, 0 no room`, the hotbar Wood, the axe, the Cooked meat and the favourite Resin are still in the inventory, and the log has `[station.actions.deposit-button] Deposit Started`.

3. Set `DepositPreview = true` in the ConfigurationManager (F1), pick up 10 Stone and click Put away inventory, then click no.

Expected: the `Put away inventory` yes/no popup opens with nothing moved, and after no the top-left message reads `Put away cancelled` and nothing has moved.

4. Click Put away inventory again, then click yes, then set `DepositPreview` back to `false`.

Expected: the Stone moves and the `Put away: 1 stacks moved, 0 no room` summary appears in the top-left.

5. Set every chest of the base to Keep with the Category button, look at the panel, then set the chests back to Auto.

Expected: both buttons are disabled with the tooltip `No chests in this base`.

6. Bind `DepositKey = G` in the ConfigurationManager (F1), walk inside the base away from every station, pick up 10 Stone, close the inventory and press G.

Expected: the Stone goes into its chest and the summary line appears in the top-left.

7. Walk out of every base and press G.

Expected: the centre of the screen reads `Not in a base` and nothing moves.

8. Walk back into the base, open the inventory with Tab and press G.

Expected: nothing happens (the hotkey is ignored while a menu is open).

9. Set `MovesPerFrame = 1` in the ConfigurationManager (F1), carry 30 single stacks of mixed materials, click Put away inventory and, while it runs, look at both buttons.

Expected: both buttons are disabled with `Organize in progress by <YourName>` until the summary appears.

10. Set `StationButtons = false` in the ConfigurationManager (F1), then set it back to `true` and set `DepositKey` back to empty.

Expected: while it is `false` both buttons and the `Base:` line disappear, and after the change back they return.

11. Check the whole log, then keep the station panel open for 30 seconds without clicking anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while the panel stays open for 30 seconds with nothing clicked, no new `BaseStorage` line appears.

### Host plus one friend

Both players use the same build. The host runs the world with `DebugLogging = true`. `<HostName>` and `<FriendName>` are the character names as shown in the player list.

1. The host sets `MovesPerFrame = 1`, carries 30 stacks and clicks Put away inventory at the workbench. Meanwhile the friend uses a station of the same base.

Expected: within a second the friend's two buttons are disabled with `Organize in progress by <HostName>`, and they are enabled again within a second of the host's summary.

2. While the host's Put away runs, the friend presses their bound `DepositKey` inside the base, or clicks Put away inventory within the first second.

Expected: the friend sees `Organize in progress by <HostName>` in the centre and nothing moves from the friend's inventory.

3. The friend clicks Put away inventory at a station of the host's base.

Expected: the friend's items go into the host's chests, and the host's chests show them when opened.

4. The friend opens one of the base chests and keeps it open while the host clicks Put away inventory.

Expected: no item goes into the chest the friend holds open (it is skipped as in use), whatever has no other room stays in the host's inventory and is counted as no room in the summary, and nothing is lost.

5. Check the log on both peers, then keep a station panel open for 30 seconds without clicking anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while the panel stays open for 30 seconds with nothing clicked, no new `BaseStorage` line appears on either peer.

The exclusions (hotbar, equipped items, favourites and the never list) and the conservation of items are covered by the Core Deposit planner and execution tests, the run flow and the summaries by the coordinator tests, and the button placement, the `StationButtons` hiding and the disabled state by the Core `StationPanelLayout` tests, not by manual steps.

## Chest reservation (AC-5.2, AC-5.3, AC-6.3)

Checks that BaseStorage reserves a chest through the vanilla open handshake, keeps the chest window closed for the requester, reports a chest another player has open by that player's name, and releases every reservation. This section needs Debug logging: set it up as in the `## Debug logging` section first. Every expected result is a `[Debug  :BaseStorage]` line in `<ValheimDir>\BepInEx\LogOutput.log`.

The reservation probe: with `DebugLogging = true`, holding Left Shift and pressing E on a chest (the alt interact) runs a reservation probe instead of opening the chest. The probe reserves the chest, holds it for half of `ReserveTimeoutMs`, writes nothing to it and releases it. With `DebugLogging = false` Shift+E behaves as in vanilla and opens the chest.

Before starting the game, set `ReserveTimeoutMs = 20000` under `[Network]` in `<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg`. The value is read once at game start, so the probe holds a chest for 10 seconds. Restore `1500` at the end of this section.

`<id>` is `<UserId>:<Id>` of the chest and `<operation>` is the operation label, which is `probe` in every step below. The lines used are:

- Route: `Reserve <id> [<operation>]: route <Route>`
- Transition: `Reserve <id> [<operation>]: <From> -> <To>`, followed by ` (<Failure>)` when `<To>` is `Failed`
- In use: `Reserve <id> [<operation>]: in use by <holderLabel>`
- Response consumed: `Reserve <id> [<operation>]: open response <granted|refused> consumed, chest UI not shown`
- Late response consumed: `Reserve <id> [late]: open response <granted|refused> consumed, chest UI not shown`

### Solo

1. Start a world with `DebugLogging = true`. Build a wooden chest, stand next to it, hold Left Shift and press E on it.

Expected: `Reserve <id> [probe]: route LocalOwner`, then `Reserve <id> [probe]: Requesting -> Granted` and `Reserve <id> [probe]: Granted -> Writing`, and about 10 seconds later `Reserve <id> [probe]: Writing -> Releasing` and `Reserve <id> [probe]: Releasing -> Completed`. The chest window never opens. There is no `open response` line, because no request is sent.

2. After `Releasing -> Completed`, press E on the same chest.

Expected: the chest window opens normally and no new `Reserve` line is written (a real open is never consumed). Close the window.

3. Set `DebugLogging = false` in the ConfigurationManager (F1). Hold Left Shift and press E on the chest.

Expected: the chest opens as in vanilla and no `Reserve` line is written. Close the window and set `DebugLogging` back to `true`.

4. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while you stand still for 30 seconds no new `Reserve` line appears (an idle tick does nothing).

### Host plus one friend

The host runs the world with `DebugLogging = true` and `ReserveTimeoutMs = 20000`, and reads the host `LogOutput.log`. Only the host needs the mod unless a step says otherwise; the host runs the probe (Left Shift and E). `<FriendName>` is the friend's character name as shown in the player list.

1. The friend opens a chest the host built and keeps it open. The host probes that chest.

Expected: `Reserve <id> [probe]: route SkipInUse`, `Reserve <id> [probe]: in use by <FriendName>` and `Reserve <id> [probe]: Requesting -> Failed (InUseByOther)`. The friend's chest window stays open and the friend can still move items.

2. The friend closes the chest and stays next to it, so the friend owns it. The host probes it.

Expected: `Reserve <id> [probe]: route SendRequest`, `Reserve <id> [probe]: open response granted consumed, chest UI not shown`, `Reserve <id> [probe]: Requesting -> Granted`, `Reserve <id> [probe]: Granted -> Writing`, and about 10 seconds later `Writing -> Releasing` and `Reserve <id> [probe]: Releasing -> Completed`. The host sees no chest window and no "in use" message.

3. Repeat step 2. While the host holds the chest (within the 10 seconds after `Requesting -> Granted`), the friend presses E on that chest.

Expected: the friend sees the vanilla in-use refusal and no chest window. After `Releasing -> Completed` on the host, the friend presses E again and the chest opens.

4. The friend builds a chest and sets it to Private. The host probes it.

Expected: `Reserve <id> [probe]: route SkipAccessDenied` and `Reserve <id> [probe]: Requesting -> Failed (Denied)`. There is no `open response` line, because no request is sent.

5. The host quits to the desktop, sets `ReserveTimeoutMs = 1`, starts the game and rejoins. The friend stands next to a chest the friend built. The host probes that chest.

Expected: `Reserve <id> [probe]: route SendRequest`, `Reserve <id> [probe]: Requesting -> Failed (Timeout)`, then within 3 seconds `Reserve <id> [late]: open response granted consumed, chest UI not shown`. The host sees no chest window. The late window is twice `ReserveTimeoutMs`, so with a value of `1` it is 2 milliseconds: if the chest window opens instead, the friend's answer arrived after the window closed. Record which of the two happened. Restore `ReserveTimeoutMs = 20000` and restart the game.

6. The host builds a chest and stands next to it, with the friend more than 100 m away. The host probes it.

Expected: `Reserve <id> [probe]: route LocalOwner` and the same transition lines as Solo step 1. There is no `open response` line (no network request).

7. Install the mod on the friend as well, with the friend's `ReserveTimeoutMs = 20000` and `DebugLogging = true`. The friend probes a chest the host built, then quits the game within 5 seconds. About 5 seconds after the friend has left, the host presses E on that chest.

Expected: the chest opens for the host. A reservation does not outlive its requester.

8. Check the host log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while you stand still for 30 seconds no new `Reserve` line appears (an idle tick does nothing).

The owner refusing after the host's local checks passed (the Deny race) and ownership lost while writing are covered by unit tests, not by manual steps.

## Version handshake (AC-6.1, AC-6.3)

On every new connection both sides register the `BaseStorage_Version` RPC and send their mod version once. The host checks each joining client: a client with a different version, or without the mod (no payload within 5 seconds), is refused through the vanilla connection-error path (the vanilla `Error` RPC with the incompatible-version code, then a disconnect 1 second later), so the friend sees the vanilla "Incompatible version" screen. A friend who has the mod also checks the host's version and leaves on their own with the same error. The host's `RequireSameVersion` setting (`[General]`, default `true`) turns the refusal off; with `false` everyone is accepted. Every outcome writes one `[Info   :BaseStorage]` line to `<ValheimDir>\BepInEx\LogOutput.log` that starts with `Version handshake: peer <id>`. The mod version is the `Version.props` value shown in the load line (`BaseStorage <version> loaded`).

### Solo

1. Start a single-player world and play for a minute. Open a chest, close it, and check `<ValheimDir>\BepInEx\LogOutput.log`.

Expected: no line contains `Version handshake`. Nothing is sent and nothing is refused: single-player has no peers.

2. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and no `Version handshake` line appears.

### Host plus one friend

The host starts the world with "Start server" and `RequireSameVersion = true`, and reads the host `LogOutput.log`. The friend installs the same BaseStorage build (the same `BaseStorage <version> loaded` line in both logs) unless a step says otherwise. `<id>` is a small number that counts the connections of the session.

1. Matched: the friend joins the host's world with the same version.

Expected: the friend joins normally and plays. The host log gains `Version handshake: peer <id> accepted, host <version>, client <version>` with both versions equal, and the friend log gains the same `accepted` line for the host as its peer. Neither side sees a MessageHud notice.

2. Mismatched: the friend installs a build with a different version (for example a package built with `-p:Version=0.9.9`) and joins.

Expected: the friend does not enter the world. The friend sees the vanilla "Incompatible version" connection error, with one extra line below it reading `BaseStorage version mismatch: host <hostVersion>, you <friendVersion>`. The host sees the same `BaseStorage version mismatch: host <hostVersion>, you <friendVersion>` text as a centre notice, and the host log gains `Version handshake: peer <id> refused, versions differ, host <hostVersion>, client <friendVersion>` followed about 1 second later by `Version handshake: peer <id> disconnected after refusal`. The friend log gains `Version handshake: peer <id> is the host and versions differ, leaving, host <hostVersion>, client <friendVersion>`.

3. Missing: the friend removes BaseStorage (delete `BepInEx\plugins\BaseStorage`), keeps BepInEx, and joins.

Expected: the friend does not enter the world and sees the vanilla "Incompatible version" connection error about 5 seconds after connecting (there is no BaseStorage line on the friend's screen, because the friend has no mod). The host sees the centre notice `BaseStorage version mismatch: host <hostVersion>, joining player has no BaseStorage`, and the host log gains `Version handshake: peer <id> refused, no BaseStorage payload, host <hostVersion>` and, about 1 second later, `Version handshake: peer <id> disconnected after refusal`.

4. Lenient host: the host sets `RequireSameVersion = false` in the ConfigurationManager (F1), and the friend without the mod joins again.

Expected: the friend joins and plays vanilla. The host log gains `Version handshake: peer <id> accepted, host <hostVersion>, client none` about 5 seconds after the connection, and no notice is shown. Set `RequireSameVersion` back to `true`.

5. The friend reinstalls the matching build and joins once more, then both check their logs and stand still for 30 seconds without touching anything.

Expected: the friend joins and both logs gain an `accepted` line. No `BaseStorage` line contains `failed and was skipped`, and no new `Version handshake` line appears while standing still.

A host with a different version than a friend who has `RequireSameVersion = false` locally still refuses the friend, because the host decides; the friend's own check with its local setting only adds the client-side leave. Both decisions are covered by unit tests, not by manual steps.

## Craft from base (AC-2.4, AC-3.1, AC-3.4)

Crafting at a station inside a base uses the materials of the base chests as well as the inventory. The materials are consumed all at once, before the item is made: either the whole recipe is paid and the item appears in the inventory, or nothing is consumed and the notice `Materials moved, try again` appears. The setting `CraftFromBase` (`[General]`, default `true`) turns the feature off.

Turn on `DebugLogging` first, as in `## Debug logging`. Every expected log result below is a `[Debug  :BaseStorage]` line in `<ValheimDir>\BepInEx\LogOutput.log`. The craft lines read:

- `Craft <token>: started, <takes> take(s) from <chests> chest(s)` when a craft that needs chest items starts.
- `Craft <token>: <End>` when it ends without a failure or a return, for example `Craft <token>: Created`.
- `Craft <token>: Failed (<Outcome>)`, with ` at <id>` after the outcome when a chest is named, when the consumption failed and nothing was taken.
- `Craft <token>: Returned, <stacks> stack(s) put back` when the game declined the craft after the take and every stack was put back.
- `Craft: vanilla (<Reason>)` when the craft is left to vanilla, for example `Craft: vanilla (NotInBase)`.

The reservation lines of `## Chest reservation (AC-5.2, AC-5.3, AC-6.3)` appear with the label `[craft]` between them.

Build pieces (the Hammer) are covered by the section Build from base below.

### Solo

1. Stand at a Workbench inside a base, with part of the Wood a chosen recipe needs in the inventory and the rest in a chest of the base. Open the crafting panel, select the recipe and press Craft.

Expected: the recipe shows as craftable and the item appears in the inventory. The Wood in the inventory and in the chest drops by exactly the recipe amount, the inventory first and then the nearest chest, and no chest window opens. The log shows `Craft <token>: started, <takes> take(s) from <chests> chest(s)`, `Reserve <id> [craft]: route LocalOwner` and `Craft <token>: Created`.

2. Put every material of a recipe into the inventory and craft it at the same Workbench.

Expected: crafting behaves as vanilla and the log has no `Craft` line.

3. Move the Wood a recipe needs into a chest outside every base, or stand at a Workbench outside a base, and open the crafting panel.

Expected: the recipe is not craftable, as in vanilla, because the chest is outside the base.

4. Set `CraftFromBase = false` in the ConfigurationManager (F1) and open the crafting panel at the Workbench of step 1 with the Wood in the chest again.

Expected: the chest materials are not counted and crafting is vanilla. Set `CraftFromBase` back to `true`.

5. Equip the Hammer and select a piece whose Wood is only in a base chest.
Expected: the piece is buildable; placing it is covered by the section Build from base below.

6. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while you stand still for 30 seconds no new `Craft` or `Reserve` line appears.

### Host plus one friend

Both players run the same build. The friend crafts unless a step says otherwise. Both read their own `LogOutput.log`.

1. The friend stands at a Workbench in the host's base, with part of the materials in a chest the host last used (owned by the host), and crafts the recipe.

Expected: the friend's log shows `Reserve <id> [craft]: route SendRequest`, `Reserve <id> [craft]: open response granted consumed, chest UI not shown` and `Craft <token>: Created`. The item appears in the friend's inventory and the chest loses exactly the planned amount. The host sees no change other than the chest contents.

2. The host keeps that chest open while the friend crafts the same recipe.

Expected: the open chest is not counted (`route SkipInUse`, or the recipe is not craftable when the open chest is needed). Nothing is taken from it and the host's window stays open.

3. The friend sets `ReserveTimeoutMs = 20000` in `[Network]` and restarts, so the window is wide. The friend starts a craft that needs the chest, and the host takes the needed Wood out of that chest before the craft completes.

Expected: the friend sees `Materials moved, try again`, the friend's log shows `Craft <token>: Failed (<Outcome> at <id>)`, and the friend gets no item. Both the friend's inventory and every chest hold exactly what they held before the craft, apart from the Wood the host took. Restore `ReserveTimeoutMs` to `1500`.

4. The host crafts at the Workbench with part of the materials in a chest the friend owns.

Expected: the same as step 1 with the roles swapped: `route SendRequest`, `open response granted consumed, chest UI not shown` and `Craft <token>: Created` in the host's log.

5. On both peers, check the log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while standing still for 30 seconds no new `Craft` or `Reserve` line appears.

The commit fallback (the game declining after the take, `Returned`), the inventory-full pre-flight, the timeout and the exception paths are covered by unit tests, not by manual steps.

## Build from base (AC-2.4, AC-3.1, AC-3.4)

Building with the Hammer (or any build tool) while standing inside a base uses the materials of that base's chests as well as the inventory. The materials are taken all at once, before the piece appears: either the whole piece is paid and the piece is placed, or nothing is consumed, no piece is placed and the notice `Materials moved, try again` appears. The base is the one whose station build range contains the player. The setting `CraftFromBase` (`[General]`, default `true`) turns the feature off.

Turn on `DebugLogging` first, as in `## Debug logging`. Every expected log result below is a `[Debug  :BaseStorage]` line in `<ValheimDir>\BepInEx\LogOutput.log`. The build lines read:

- `Build <token>: started, <takes> take(s) from <chests> chest(s)` when a placement that needs chest items starts.
- `Build <token>: <End>` when it ends without a failure or a return, for example `Build <token>: Placed`, `Build <token>: Abandoned` or `Build <token>: PlacementRefused`.
- `Build <token>: Failed (<Outcome>)`, with ` at <id>` after the outcome when a chest is named, when the consumption failed and nothing was taken.
- `Build <token>: Returned, <stacks> stack(s) put back` when the game declined the placement after the take and every stack was put back.
- `Build: vanilla (<Reason>)` when the placement is left to vanilla, for example `Build: vanilla (NotInBase)`.
- `Build: placement suppressed, inventory does not cover the piece` when the placement guard stops a piece the inventory alone does not pay for.

The reservation lines of `## Chest reservation (AC-5.2, AC-5.3, AC-6.3)` appear with the label `[craft]` between them.

### Solo

1. Stand in Workbench range inside a base with 4 Wood in the inventory and 20 Wood in a chest of the base. Equip the Hammer, select the wooden chest piece (10 Wood) and place it.

Expected: the build menu shows the piece as buildable (the icon is not red) and placing it places the chest. The Wood in the inventory drops to 0 and the Wood in the base chest drops to 14, and no chest window opens. The log shows `Build <token>: started, <takes> take(s) from <chests> chest(s)`, `Reserve <id> [craft]: route LocalOwner` and `Build <token>: Placed`.

2. Make the Wood the piece needs short by one unit in total (inventory plus base chests), then select the piece and try to place it.

Expected: the piece stays red and placing shows the vanilla missing-requirement message. Nothing is taken, even though the base is only one short.

3. In a base of two connected Workbenches, stand in range of the second Workbench only, with the Wood in a chest in range of the first Workbench only. Select the piece and place it.

Expected: the piece is buildable and placing takes from that chest, because the base containing the player is used.

4. Put every material of the piece into the inventory and place it.

Expected: building behaves as vanilla and the log has no `Build` line.

5. Stand outside every base, with the Wood only in a base chest, and select the piece.

Expected: the piece is not buildable, as in vanilla, no `Materials moved, try again` appears, and the log has no `Build` line.

6. Leave the Wood the piece needs only in a cart inside the base (`ExcludeVehicles = true`), then only in a chest set to Keep (`KeepChestsCraftable = false`).

Expected: in both cases the piece is not buildable.

7. With chest Wood needed, aim at an invalid spot (for example blocked by the player) and click to place.

Expected: the vanilla placement reason appears, nothing is taken, and the log shows `Build <token>: PlacementRefused`.

8. Set `CraftFromBase = false` in the ConfigurationManager (F1) and select the piece with the Wood only in the base chest again.

Expected: the chest materials are not counted and building is vanilla. Set `CraftFromBase` back to `true`.

9. Check the whole log, then stand still for 30 seconds with the Hammer equipped without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while you stand still for 30 seconds no new `Build` or `Reserve` line appears.

### Host plus one friend

Both players run the same build. The friend builds unless a step says otherwise. Both read their own `LogOutput.log`.

1. The friend stands in the host's base, with part of the Wood in a chest the host last used (owned by the host), and builds a wooden chest with the Hammer.

Expected: the friend's log shows `Reserve <id> [craft]: route SendRequest`, `Reserve <id> [craft]: open response granted consumed, chest UI not shown` and `Build <token>: Placed`. The piece appears for both players and the chest loses exactly the planned amount.

2. The host keeps that chest open while the friend selects the same piece.

Expected: the open chest is not counted, so the piece is not buildable when the open chest is needed. Nothing is taken from it and the host's window stays open.

3. The friend sets `ReserveTimeoutMs = 20000` in `[Network]` and restarts, so the window is wide. The friend places a piece that needs the chest, and the host takes the needed Wood out of that chest before the piece appears.

Expected: the friend sees `Materials moved, try again`, the friend's log shows `Build <token>: Failed (<Outcome> at <id>)`, and no piece is placed. The friend's inventory and every chest hold exactly what they held before, apart from the Wood the host took. Restore `ReserveTimeoutMs` to `1500`.

4. The host builds from a chest the friend owns.

Expected: the same as step 1 with the roles swapped: `route SendRequest`, `open response granted consumed, chest UI not shown` and `Build <token>: Placed` in the host's log.

5. On both peers, check the log, then stand still for 30 seconds with the Hammer equipped without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while standing still for 30 seconds no new `Build` or `Reserve` line appears.

The commit fallback (the game declining the placement after the take, `Returned`), the placement guard (`placement suppressed`), the pre-flight while waiting, abandonment, the timeout and the exceptions are covered by unit tests, not by manual steps.
## Requirement display (AC-3.2)

The crafting panel and the build tooltip of the Hammer show, for each requirement whose material is also in base chests, `<inventory> (+<chests>) / <need>`, for example `52/20`. When no chest contributes, the requirement shows exactly what vanilla shows: only the needed amount, for example `20`. The amount blinks red and white as in vanilla only when the inventory plus the chests is less than the need. The display follows `CraftFromBase` (`[General]`, default `true`). The display writes no log line, so the steps check the screen.

Placing a piece is covered by `## Build from base (AC-2.4, AC-3.1, AC-3.4)`, as step 5 of `## Craft from base (AC-2.4, AC-3.1, AC-3.4)` says.

### Solo

1. Stand at a Workbench inside a base. Choose a recipe that needs Wood, keep fewer Wood in the inventory than it needs, put more in a chest of the base, and select the recipe in the crafting panel (surplus case).

Expected: the Wood line reads `<inventory> (+<chests>) / <need>` with the real counts, for example `52/20`, in white, not blinking and fully readable, and the recipe is craftable.

2. Take Wood out of the chest until the inventory plus the chest is less than the need (short case).

Expected: the line still shows the combined form with the new chest count and blinks red and white like vanilla, and the recipe is not craftable.

3. Make the inventory plus the chest exactly the need (exact case).

Expected: the line is white, not blinking, and the recipe is craftable.

4. Move all Wood out of the base chests, for example into a chest outside every base (no chest case).

Expected: the line shows only the needed amount, exactly as vanilla, white when the inventory covers it and blinking otherwise.

5. Put the Wood back in the base chest, set `CraftFromBase = false` in the ConfigurationManager (F1) and look at the same recipe. Set it back to `true`, then stand at a Workbench outside every base (vanilla cases).

Expected: with the setting off the line shows the vanilla text, and at the Workbench outside every base it shows the vanilla text too.

6. Inside the base, equip the Hammer and hover a piece whose Wood is partly in the inventory and partly in a base chest (build tooltip).

Expected: the Wood line of the tooltip reads `<inventory> (+<chests>) / <need>`, white when the combined amount covers the need and blinking red when it is short. Placing still counts only the inventory. Outside every base the tooltip is vanilla.

7. Keep the crafting panel open for 30 seconds without touching anything, then check the log.

Expected: the numbers stay steady (only the vanilla red blink changes), no `BaseStorage` line contains `failed and was skipped`, and no new `BaseStorage` line appears while the panel is open.

### Host plus one friend

Both players run the same build. The friend looks unless a step says otherwise.

1. The friend opens the crafting panel at a Workbench in the host's base, with Wood in a base chest the host last used.

Expected: the friend's Wood line shows `(+<chests>)` equal to the Wood in the base chests.

2. The host opens that chest and keeps it open, then closes it.

Expected: within 2 seconds the friend's `(+<chests>)` no longer counts the open chest (the line returns to the vanilla text when it was the only chest with Wood, and blinks red when the combined amount is now short). After the host closes it, it is counted again within 2 seconds.

3. The host puts 10 Wood into a base chest while the friend watches the panel.

Expected: within 2 seconds the friend's `(+<chests>)` grows by 10, and nothing in any inventory or chest changes because of the display.

4. The friend equips the Hammer in the host's base.

Expected: the build tooltip shows the same combined form as solo step 6.

5. On both peers, keep the crafting panel open for 30 seconds without touching anything, then check the log.

Expected: the numbers stay steady (only the vanilla red blink changes), no `BaseStorage` line contains `failed and was skipped`, and no new `BaseStorage` line appears while the panel is open.

The short, exact, surplus and no-chest cases, the culture rule and the fall-back-to-vanilla paths are covered by unit tests, not by manual steps. Upgrade and multi-craft amounts are covered by their own section when they are added.

## Upgrade and multi-craft from base (AC-3.5)

Upgrading an item and crafting several at once at a station inside a base use the same path as `## Craft from base (AC-2.4, AC-3.1, AC-3.4)`: the materials come from the inventory first and then the base chests, and are taken all at once before the item is made or upgraded. An upgrade needs the amounts for the quality it upgrades to, shown in the panel as `<inventory> (+<chests>) / <need>`. Crafting several at once is vanilla's: hold Left Shift (the `AltPlace` key) while the crafting panel is open, and the needs become the per-craft amounts times the number crafted (5 by default); an upgrade is never crafted several at once. The setting `CraftFromBase` (`[General]`, default `true`) turns all of it off. When a chest cannot be reserved or its contents changed, nothing is taken and the notice `Materials moved, try again` is shown. The log lines are the `Craft` and `Reserve <id> [craft]` lines of the Craft from base section; no new line exists. Upgrades at upgrader stations stay vanilla.

Turn on `DebugLogging` first, as in `## Debug logging`.

### Solo

1. At a Workbench inside a base, with an item in the inventory that the Workbench can upgrade, hold fewer of one upgrade material in the inventory than the upgrade needs and the rest in a chest of the base. Select the item in the panel's upgrade list and press Craft.

Expected: the material's line shows `<inventory> (+<chests>) / <need>` with the upgrade amount (not the craft amount) in white and the upgrade is available. The item's quality rises by one in the same slot, the material drops by exactly `<need>` (inventory first, then the nearest chest), no chest window opens, and the log shows `Craft <token>: started, <takes> take(s) from <chests> chest(s)`, `Reserve <id> [craft]: route LocalOwner` and `Craft <token>: Created`.

2. Arrange the inventory plus the base chests to hold one less of a material than the next upgrade needs.

Expected: the line blinks red, the upgrade cannot be pressed and nothing is taken.

3. Put that one back so the inventory plus the chests hold exactly the need, and upgrade.

Expected: the line is white, the quality rises by one, and afterwards neither the inventory nor any base chest holds that material.

4. Select Wood arrows (`ArrowWood`, Wood 8 and Feathers 2 per craft), keep fewer than 40 Wood in the inventory and the rest in a base chest, and hold Left Shift.

Expected: the Wood line reads `<inventory> (+<chests>) / 40` and Feathers `/ 10`, both white. Pressing Craft while holding Left Shift gives five crafts' worth of arrows at once, Wood drops by exactly 40 and Feathers by exactly 10 (inventory first), and the log shows `Craft <token>: started, ...` and `Craft <token>: Created`.

5. Make the inventory plus the base chests hold 39 Wood.

Expected: with Left Shift held the Wood line blinks red and Craft cannot be pressed; released, the line shows `/ 8` in white and a single craft works and takes exactly 8 Wood.

6. Set `CraftFromBase = false` in the ConfigurationManager (F1) and repeat the setups of steps 1 and 4.

Expected: both count only the inventory, as vanilla. Set `CraftFromBase` back to `true`.

7. Check the whole log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while standing still for 30 seconds no new `Craft` or `Reserve` line appears.

### Host plus one friend

Both players run the same build. The friend acts unless a step says otherwise.

1. The friend upgrades at a Workbench in the host's base with part of the upgrade material in a chest the host last used (owned by the host).

Expected: the friend's log shows `Reserve <id> [craft]: route SendRequest`, `Reserve <id> [craft]: open response granted consumed, chest UI not shown` and `Craft <token>: Created`; the item's quality rises by one and the chest loses exactly the planned amount.

2. The friend crafts Wood arrows several at once (Left Shift) with the Wood split between the inventory and that chest.

Expected: the same three lines, and Wood and Feathers drop by exactly 40 and 10 in total.

3. The friend sets `ReserveTimeoutMs = 20000` in `[Network]` and restarts, starts an upgrade that needs the host's chest, and the host takes the needed material out of that chest before it completes.

Expected: the friend sees `Materials moved, try again`, the log shows `Craft <token>: Failed (<Outcome> at <id>)`, the item keeps its quality, and the friend's inventory and every chest hold exactly what they held before, apart from what the host took. Restore `ReserveTimeoutMs` to `1500`.

4. The host crafts several at once from a chest the friend owns.

Expected: the same as step 2 with the roles swapped.

5. On both peers, check the log, then stand still for 30 seconds without touching anything.

Expected: no `BaseStorage` line contains `failed and was skipped`, and while standing still for 30 seconds no new `Craft` or `Reserve` line appears.

The exact boundary for every target quality, the level-4 amount rule, the multiplier for amounts 1 to 7 in both the availability check and the plan, the pre-flight (upgrade item gone) and the abandonment paths are covered by unit tests, not by manual steps.

## Craft from base off (AC-3.6)

The setting `CraftFromBase` (under `[General]`, default `true`) is the switch for everything in this guide that reads base chests. Set to `false`, crafting, building, the requirement display, upgrading and crafting several at once are exactly vanilla: no base chest is read or reserved, so no `Reserve <id> [craft]` line and no `Craft` Debug line appears. A change in the ConfigurationManager (F1) applies at the next craft, build or requirement display without a restart. A craft or build that has already started finishes as it began. The setting is host-synced: the host's value applies to a friend for the session, see `## Config sync (AC-6.2, AC-6.3)`. A host change during a session reaches a connected friend when the friend joins again.

Turn on `DebugLogging` first, as in `## Debug logging`.

### Solo

1. Setup: `DebugLogging = true`, `CraftFromBase = true`; a base with a Workbench and one chest holding 20 `Wood`; no Wood in the inventory. Open the crafting panel at the Workbench and select a recipe that needs Wood.

Expected: the requirement shows `20/<need>`.

2. Without leaving the game, set `CraftFromBase = false` in F1, close F1 and select the same recipe again.

Expected: the requirement shows only `<need>` and blinks red as vanilla, the recipe cannot be crafted, and the log gets no new `Reserve` or `Craft` line.

3. Take enough Wood into the inventory and craft the recipe.

Expected: the Wood is taken from the inventory only, the chest still holds 20 `Wood`, and there is no `Reserve <id> [craft]` line.

4. Put the inventory Wood back in the chest, equip the Hammer and select a piece that needs Wood.

Expected: the build tooltip shows only `<need>` as vanilla, the piece cannot be placed (vanilla's message), and the chest still holds 20 `Wood`.

5. With the materials only in the chest, try an upgrade and, holding Left Shift, a multi-craft.

Expected: both count only the inventory, as vanilla.

6. Set `CraftFromBase = true` in F1 without a restart and open the Workbench panel again.

Expected: `20/<need>` returns and crafting takes the Wood from the chest.

7. Stand still for 30 seconds.

Expected: no `BaseStorage` line contains `failed and was skipped`.

### Host plus one friend

1. Setup: the host sets `CraftFromBase = false`, builds the base of Solo step 1 and starts with "Start server"; the friend keeps `CraftFromBase = true`, sets `DebugLogging = true` and joins.

Expected: the friend log has `Config sync: CraftFromBase = false (host)`.

2. The friend opens the Workbench panel in the base with the Wood only in the chest.

Expected: vanilla display, the recipe cannot be crafted, no `Reserve` line in the friend log.

3. The friend selects the Wood piece with the Hammer.

Expected: vanilla tooltip, no piece placed, the chest unchanged.

4. The friend sets `CraftFromBase = true` in its own F1.

Expected: still vanilla, because the host's value applies for the session.

5. The host sets `CraftFromBase = true` in F1 while the friend stays connected, and opens the Workbench panel.

Expected: the host's panel shows `20/<need>` at once; the friend's panel stays vanilla.

6. The friend quits to the main menu and joins again.

Expected: `Config sync: CraftFromBase = true (host)` in the friend log and `20/<need>` on the friend's panel.

7. Both stand still for 30 seconds.

Expected: no `BaseStorage` line contains `failed and was skipped`.

That every craft-from-base patch checks `CraftFromBase` before anything else is proven by an automated test, not by manual steps.

## Feedback messages (AC-8.1, AC-7.3)

Every action ends in exactly one short MessageHud line whose text comes from the strings table (English only). For each row, do the trigger and check that exactly one line appears, with this wording, in this place; numbers and names depend on your base. "top left" and "centre" are BaseStorage lines; "game" means the game's own line and no BaseStorage line; "none" means no line at all.

| Outcome | Action | How to trigger | Expected message | Where |
|---------|--------|----------------|------------------|-------|
| `CraftCreated` | Craft | Solo: at a Workbench in a base, keep the Wood for a recipe only in a base chest and craft it. Then craft five more quickly; the line must not pile up. | `Used materials from base chests` | top left |
| `CraftReturned` | Craft | Unit test only: craft-from-base S10 (vanilla declines at commit). | `Materials moved, try again` | centre |
| `CraftFailed` | Craft | Host plus one friend: as Craft from base, Host plus one friend, step 3 (the host takes the Wood during the wide reservation window). | `Materials moved, try again` | centre |
| `CraftPreFlightFailed` | Craft | Unit test only: craft-from-base S7 (pre-flight at start). | `Materials moved, try again` | centre |
| `CraftUnavailable` | Craft | Unit test only: craft-from-base S5 (no plan at craft time). | `Materials moved, try again` | centre |
| `CraftAbandoned` | Craft | Unit test only: craft-from-base S8 (panel closed while waiting). | no message (Superseded) | none |
| `BuildPlaced` | Build | Solo: in a base, keep the Wood only in a base chest and place ten wood floor pieces quickly with the Hammer; one line, not ten stacked. | `Used materials from base chests` | top left |
| `BuildReturned` | Build | Unit test only: build-from-base T12 (vanilla declines after the take). | the game's own message; no BaseStorage message | game |
| `BuildFailed` | Build | Host plus one friend: as Build from base, Host plus one friend, step 3. | `Materials moved, try again` | centre |
| `BuildPreFlightFailed` | Build | Unit test only: build-from-base T11 (pre-flight while waiting). | `Materials moved, try again` | centre |
| `BuildUnavailable` | Build | Unit test only: build-from-base T5 (no plan at place time). | `Materials moved, try again` | centre |
| `BuildAbandoned` | Build | Unit test only: build-from-base T8 (place mode left while waiting). | no message (Superseded) | none |
| `BuildPlacementRefused` | Build | Solo: in a base, with the Wood only in a base chest, try to place a piece where the ghost is red (overlapping another piece). | the game's own message; no BaseStorage message | game |
| `CategoryApplied` | CategoryChange | Solo: open a base chest and click the Category button until it shows `Metals & Ores`. | `Chest category: Metals & Ores` | top left |
| `CategoryDenied` | CategoryChange | Host plus one friend: the friend binds `CycleCategoryKey`, hovers a chest inside a ward the friend is not permitted on, and presses the key. | `Chest category not changed: no access to this chest` | centre |
| `CategoryInUse` | CategoryChange | Host plus one friend: the host (Erik) keeps a chest open; the friend hovers it and presses `CycleCategoryKey`. | `Chest category not changed: in use by Erik` | centre |
| `CategoryTimeout` | CategoryChange | Unit test only: chest-category-contracts CC9 (the owner never answers). | `Chest category not changed: no answer from the chest owner` | centre |
| `CategoryUnavailable` | CategoryChange | Unit test only: chest-category-contracts, no reservation route (`Begin` returns null). | `Chest category not changed: the chest is not available` | centre |
| `CategoryUnchanged` | CategoryChange | Unit test only: FA4 (the chest already holds the requested value). | `Chest category: Keep` | top left |
| `CategoryAlreadyPending` | CategoryChange | Host plus one friend: the friend presses `CycleCategoryKey` twice quickly on a host-owned chest; only the first request's line appears. | no message (Coalesced) | none |
| `CategoryChestGone` | CategoryChange | Unit test only: FS6 source fact (null chest or invalid view). | no message (TargetGone) | none |
| `OrganizeCompleted` | Organize | Solo: at a station in a base with stacks to move, click Organize base and confirm. | `Organized 37 stacks; 4 stacks had no room` | top left |
| `OrganizeStopped` | Organize | Unit test only: organize-runner section 8 row 9 (destination full). | `Organized 12 stacks; stopped early: chest full` | top left |
| `OrganizeCancelled` | Organize | Solo: click Organize base, then No in the preview. | `Organize cancelled` | top left |
| `OrganizeNotInBase` | Organize | Solo: bind `OrganizeKey`, stand outside every base and press it. | `Not in a base` | centre |
| `OrganizeNoChests` | Organize | Solo: bind `OrganizeKey`, stand in a base whose chests are all Keep, and press it. | `No chests in this base` | centre |
| `OrganizeAlreadyActive` | Organize | Unit test only: organize-runner section 8 row 8 (Start while a run is active). | `Organize in progress by Erik` | centre |
| `OrganizeLockRefused` | Organize | Host plus one friend: Astrid (the friend) opens an Organize preview; the host binds `OrganizeKey`, stands in the same base and presses it. | `Organize in progress by Astrid` | centre |
| `OrganizeConfirmRefused` | Organize | Unit test only: organize-runner section 8 row 13 (Confirm with the lock taken meanwhile). | `Organize in progress by Astrid` | centre |
| `OrganizeNoPlayer` | Organize | Unit test only: organize-runner row 2 (no player). | no message (NoHud) | none |
| `OrganizeWorldGone` | Organize | Unit test only: organize-runner row 11 (world gone). | no message (NoHud) | none |
| `DepositCompleted` | Deposit | Solo: at a station in a base, with stacks in the inventory, click Put away inventory (confirm if the preview shows). | `Put away: 4 stacks moved, 2 no room` | top left |
| `DepositStopped` | Deposit | Unit test only: GW-U-1911 (destination full). | `Put away: 0 stacks moved, 0 no room; stopped early: chest full` | top left |
| `DepositCancelled` | Deposit | Solo: with `DepositPreview = true`, click Put away inventory, then No. | `Put away cancelled` | top left |
| `DepositNotInBase` | Deposit | Solo: bind `DepositKey`, stand outside every base and press it. | `Not in a base` | centre |
| `DepositNoChests` | Deposit | Solo: bind `DepositKey`, stand in a base whose chests are all Keep, and press it. | `No chests in this base` | centre |
| `DepositAlreadyActive` | Deposit | Unit test only: GW-U-1913 (already active). | `Organize in progress by Erik` | centre |
| `DepositLockRefused` | Deposit | Host plus one friend: Astrid opens an Organize preview; the host binds `DepositKey`, stands in the same base and presses it. | `Organize in progress by Astrid` | centre |
| `DepositConfirmRefused` | Deposit | Unit test only: GW-U-1915. | `Organize in progress by Astrid` | centre |
| `DepositNoPlayer` | Deposit | Unit test only: GW-U-1901, GW-U-1902. | no message (NoHud) | none |
| `DepositWorldGone` | Deposit | Unit test only: GW-U-1916. | no message (NoHud) | none |
| `FavouriteAdded` | Favourite | Solo: hold Alt and left-click a Wood stack in the inventory. | `Favourite: Wood` | top left |
| `FavouriteRemoved` | Favourite | Solo: hold Alt and left-click the same Wood stack again. | `No longer favourite: Wood` | top left |


## Config sync (AC-6.2, AC-6.3)

Once the version handshake accepts a friend, the host sends its host-synced settings and its category file to that friend over the peer RPC `BaseStorage_ConfigSync`. The nine host-synced settings are `CraftFromBase`, `ExcludeVehicles`, `KeepChestsCraftable`, `MaxBaseStations`, `RequireSameVersion`, `AutoAssignThresholdPct`, `OrganizeLockSeconds`, `ReserveTimeoutMs` and `AutoAssignMixedChests`. The category file is `<ValheimDir>\BepInEx\config\BaseStorage.categories.txt`. The friend uses the host values and the host category text for the session only. The friend's local file `<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg` is never written, and neither is the friend's own category file.

A friend that applies the payload writes exactly one Info line, `Config sync: applied host settings from peer <id>, 9 settings, <n> categories`. The per-setting lines `Config sync: <Key> = <value> (host)` appear only when the friend has `DebugLogging = true`. The ConfigurationManager (F1) on the friend shows the friend's own file values, not the host's, by design. Single-player and hosting send and await nothing.

### Host plus one friend

The host starts the world with "Start server". Both read their `LogOutput.log`. `<id>` is a small number that counts the connections of the session.

1. Setup: the host sets `CraftFromBase = false` and `MaxBaseStations = 3` and adds a line `Playtest: Wood` to its category file. The friend keeps `CraftFromBase = true` and `MaxBaseStations = 64`, sets `DebugLogging = true`, has no `Playtest` line in its category file, waits at the main menu, and records `Get-FileHash "<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg"`.

Expected: both sides are at the main menu with the settings above, and the friend has noted the hash.

2. The friend joins the host's world.

Expected: the friend log has the `Version handshake: peer <id> accepted` line, then exactly one `Config sync: applied host settings` Info line whose category count equals the host file's category count, and the Debug lines `Config sync: CraftFromBase = false (host)` and `Config sync: MaxBaseStations = 3 (host)`. The host log has no `Config sync: applied` line.

3. The friend opens F1.

Expected: the friend's own values (`CraftFromBase = true`, `MaxBaseStations = 64`) are shown, by design.

4. The friend quits to the main menu and runs `Get-FileHash "<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg"` again.

Expected: the same hash as in step 1, the Debug line `Config sync: host settings cleared, local settings apply`, and the friend's category file has no `Playtest` line.

5. Both stand still for 30 seconds.

Expected: no `BaseStorage` line contains `failed and was skipped`, and no new `Config sync` line appears.

### Solo and hosting

The friend keeps `DebugLogging = true` and starts from the main menu.

1. The friend starts a single-player world.

Expected: no `Config sync:` line except the Debug line about a missing category file when that file is missing, no `Version handshake` line, and no `(host)` line.

2. The friend hosts a world with "Start server" and plays alone for a minute.

Expected: the same as step 1: nothing is sent or awaited while alone.

3. The original host joins the friend's hosted world.

Expected: the friend (now the host) log has no `Config sync: applied` line, and the joining player's log has one `Config sync: applied` line with `Config sync: CraftFromBase = true (host)` and `Config sync: MaxBaseStations = 64 (host)` (with `DebugLogging = true` on the joining player). This shows that a host uses and sends its local values.

4. The friend runs `Get-FileHash "<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg"` again.

Expected: the hash from the Host plus one friend setup step.

5. Both stand still for 30 seconds.

Expected: no `BaseStorage` line contains `failed and was skipped`.

A malformed payload, an oversize payload and a payload that arrives out-of-order are covered by unit tests, not by manual steps.
