# Don's QOL

Formerly BaseStorage. Files, folders and the settings file keep the old name, so updating keeps your settings and chest categories.

Craft and build from your base chests, give chests categories, and organize the base or put away your inventory from any workbench.


Want the easy route? Get the installer zip, extract it, and double-click `Install BaseStorage.bat`. It finds Valheim, sets up BepInEx and installs the mod. It never touches your saves.

## What it does

- **Craft and build from your chests.** Crafting, upgrading, crafting several at once and building at a station use the materials in the chests of your base. The requirement lists show the chest amounts. A base is the connected crafting stations and the chests near them.
- **Chest categories.** Each chest gets a Category button on its panel, so you decide what belongs where.
- **Organize base.** A button on the station panel sorts the items in the whole base into the chests by their categories.
- **Put away inventory.** A second station button moves your inventory into the base chests by their categories.
- **See your base.** Hover a chest to see `[In base: <N> stations]` or `[Not in a base]`, plus its category or `[Keep]`; hover a station to see `[Base: <N> chests]`.

Every BaseStorage action ends with one short message in the top-left corner.

## Installation

**Host**

1. Install BepInExPack_Valheim.
2. Install BaseStorage with r2modman or the Thunderstore Mod Manager (import `BaseStorage-<version>.zip` as a local mod), or copy `BaseStorage.dll` and `BaseStorage.Core.dll` into `BepInEx/plugins/BaseStorage/`.
3. Start the game once. This creates `BepInEx/config/dawso.valheim.basestorage.cfg` and `BepInEx/config/BaseStorage.categories.txt`.

**Friends**

1. Follow the same install steps as the host.
2. When you join, the host's host-synced settings and category file are used for the session. Your own `.cfg` file is not overwritten.
3. Single-player works with no host.

Everyone in a multiplayer game needs the same BaseStorage version. With `RequireSameVersion` on (default `true`, host-synced), a player whose version differs from the host's is refused with `Don's QOL version mismatch: host X, you Y`.

## Configuration

Settings live in `BepInEx/config/dawso.valheim.basestorage.cfg`. `DebugLogging` (under `[Debug]`, default false) turns on detailed Debug lines. To see them in `LogOutput.log`, also add `Debug` to `LogLevels` in `BepInEx.cfg`.

`CraftFromBase` (under `[General]`, default `true`) lets crafting, upgrading, crafting several at once and building at a station inside a base use the materials in the base chests, and shows the chest amounts in the requirement lists. Set it to `false` to turn all of that off: crafting and building are exactly vanilla and no chest is read or reserved. It is host-synced: in multiplayer the host's value is used, and a friend gets it when joining. A change takes effect at the next craft or build without a restart.

Host-synced settings take the host's value in multiplayer; the others are local to each player. Hotkeys are BepInEx keyboard shortcut text (for example `F7`) and are unbound by default.

| Setting | Section | Default | Host-synced | What it does |
| --- | --- | --- | --- | --- |
| `CraftFromBase` | `[General]` | `true` | yes | Craft and build using materials from chests in your base. |
| `ExcludeVehicles` | `[General]` | `true` | yes | Ignore containers on carts and ships when collecting the base's chests. |
| `KeepChestsCraftable` | `[General]` | `true` | yes | Allow crafting to use materials from chests marked Keep. |
| `MaxBaseStations` | `[General]` | `64` | yes | Maximum number of crafting stations joined into one base. |
| `BaseCacheSeconds` | `[General]` | `2` | no | Seconds a resolved base is reused before it is resolved again. |
| `RequireSameVersion` | `[General]` | `true` | yes | Refuse players whose BaseStorage version differs from the host's. |
| `AutoAssignThresholdPct` | `[Organize]` | `50` | yes | Percent of a chest's stacks that must share a category before Organize assigns that category. |
| `AutoAssignMixedChests` | `[Organize]` | `true` | yes | Let Organize give mixed chests below the threshold a category that is short of space. Set to false to keep them as unassigned sources, as in 0.1.0. |
| `OrganizeSkipPreview` | `[Organize]` | `false` | no | Start Organize without showing the preview first. |
| `DepositPreview` | `[Organize]` | `false` | no | Show a preview before Put away inventory moves anything. |
| `DepositNeverItems` | `[Organize]` | `@mead,@ammo,Hammer,Hoe,Cultivator,FishingRod,@bait` | no | Items Put away inventory never moves. Comma-separated prefab names or tokens: @mead, @ammo (arrows and bolts), @bait. Star an item as a favourite to keep it on you. |
| `MovesPerFrame` | `[Organize]` | `20` | no | Maximum item moves per frame while Organize or Put away inventory runs. |
| `OrganizeLockSeconds` | `[Organize]` | `30` | yes | Seconds before an abandoned Organize lock on a base expires. |
| `ReserveTimeoutMs` | `[Network]` | `1500` | yes | Milliseconds to wait for a chest reservation before giving up. |
| `StationButtons` | `[General]` | `true` | no | Show the Organize base and Put away inventory buttons on crafting station panels. |
| `ShowCategoryTooltip` | `[General]` | `true` | no | Show each item's BaseStorage category on its tooltip. |
| `CycleCategoryKey` | `[Keys]` | unbound | no | Hotkey that cycles the category of the chest you are looking at. |
| `OrganizeKey` | `[Keys]` | unbound | no | Hotkey that starts Organize base for the base that contains the player. |
| `DepositKey` | `[Keys]` | unbound | no | Hotkey that starts Put away inventory at the nearest crafting station. |
| `RestockKey` | `[Keys]` | unbound | no | Hotkey that starts Restock for the base that contains the player. |
| `ChestLabels` | `[General]` | `true` | no | Show chest names and categories as labels in the world, inside a base. |
| `ChestLabelDistance` | `[General]` | `6` | no | How close, in metres, a chest must be for its label to show. |
| `DebugLogging` | `[Debug]` | `false` | no | Write per-move detail to the log at Debug level. |

## Categories file

The categories live in `BepInEx/config/BaseStorage.categories.txt`. BaseStorage writes it on first run and never overwrites it, so your edits are kept. Delete it to get the defaults back. In multiplayer the host's file is used.

The format is one line per category, `Category Name: PrefabA, PrefabB, ...`. Lines starting with `#` and blank lines are ignored. A prefab belongs to the first category that lists it; later duplicates are ignored and logged. `Auto` and `Keep` are reserved and cannot be category names. The order of the file is the order the Category button cycles through. Items not listed fall back by type: food to Cooked Food, trophies to Trophies, weapons, armour, tools and shields to Gear, ammo to Ammo, everything else to Misc.

```text
# Name: prefab, prefab
Wood: Wood, FineWood, RoundLog
Stone: Stone, Flint
```

## Chest categories

Open a chest. A **Category** button sits next to Take All and Stack All, labelled with the chest's current value. Left-click it to go to the next value and Right-click it to go back: `Auto` (no assignment), then every category of the categories file in order, then `Keep` (never organized into or out of). The change is saved on the chest itself, so every player sees it and it survives a restart; a message such as `Chest category: Wood` confirms it, and `Chest category not changed: in use by Erik` tells you why it did not apply.

Optional hotkey: set `CycleCategoryKey` under `[Keys]` in the config file (unbound by default). Pressing it while looking at a chest, with no menu open, moves that chest to the next value. The chest's hover text shows the current category, or `[Keep]`, on the mod's one added line.

## Station buttons

Use any crafting station in a base and an **Organize base** button appears next to the craft controls, with a status line such as `Base: 6 stations, 41 chests, 64% full`. The last part, `P% full`, is how many slots of the base's eligible chests hold an item, rounded to the nearest whole percent; Keep chests are not counted. The button organizes that station's whole base and first shows a preview: Yes runs it and No moves nothing. Set the `OrganizeSkipPreview` setting (`OrganizeSkipPreview = true`) under `[Organize]` to skip the preview.

The button is disabled, with a tooltip saying why, while an Organize runs in the base (`Organize in progress by <name>`) or when the base has no usable chest (`No chests in this base`). Set the `StationButtons` setting (`StationButtons = false`) under `[General]` (local to each player, default `true`) to hide the button and the line. With a gamepad, the button is reached with the cursor and has no button binding of its own.

**Put away inventory**, next to Organize base, moves your inventory into the station's base chests by their categories. It never moves the hotbar (top row), equipped items, favourites (Alt+click a slot) or anything in `DepositNeverItems` (default: meads, arrows and bolts, the hammer, hoe, cultivator, fishing rod and bait). Set `DepositPreview = true` under `[Organize]` to show a preview first (default off). The optional `DepositKey` under `[Keys]` (unbound by default) puts away into the base you stand in. It ends with one line such as `Put away: 4 stacks moved, 2 no room`, and it shares the disabled state and the one-per-base lock with Organize base.

**Restock**, next to Put away inventory, tops up the stacks you carry from the base chests. It only fills the stacks in your hotbar (top row) and your favourites (Alt+click a slot), one stack at a time, and it only takes from a chest the items that really arrived. Set `RestockKey` under `[Keys]` (unbound by default) to restock the base you stand in. It ends with one line such as `Restocked 2 stacks, 60 items`, and it shares the one-per-base lock with Organize base.

**Sort** and **Name** sit under the Category button of an open chest. Sort merges identical stacks and orders the chest by category, then name. It works only on that chest, keeps item data, and stacks that differ in quality, durability or crafter stay separate. Name gives the chest a name of up to 32 characters; an empty name clears it. Names show in the base menu and on hover, and search finds them.

Named and categorised chests show a floating label within `ChestLabelDistance` metres, inside a base. Set `ChestLabels = false` to turn these off.

Shift + use also opens the feed panel on fires, torches, fermenters and cooking stations. Fires and ovens keep fuel topped up, fermenters take a mead base when empty, and cooking stations fill their free slots while their fire is lit.

With a gamepad, press the right stick in the inventory to open the base menu, or to take the focus on an open chest's buttons. The stick or D-pad moves the gold highlight, A selects and B closes.

## Console command

Start Valheim with the `-console` launch option, press F5 and type `basestorage organize` to organize the base you stand in. It uses the same preview (closing the console shows it), the same one-Organize-per-base lock and the same summary as the Organize base button. Outside a base it prints `Not in a base`. While another player organizes the base it prints `Organize in progress by <name>`. `basestorage` alone prints `Usage: basestorage organize`. This is the in-game console, not a game console.

## Known limits

- Console and crossplay players are not supported.
- Dedicated servers are not supported; it is verified only on peer-hosted games.
- Stations are only filled: finished food and mead are not collected, so food left on a cooking station can burn.
- Carts and ships are not used as storage (excluded by default, `ExcludeVehicles`).
- There is no dedicated sorting table piece.
- Compatibility with other storage or inventory mods is not guaranteed.
- The gamepad control is new and has not been tried on every layout. The category dropdown needs a pointer, so a gamepad cycles categories instead, and the hotkeys stay keyboard only.

## Uninstall

Remove the mod in the mod manager, or delete `BepInEx/plugins/BaseStorage/`. You can also delete `BepInEx/config/dawso.valheim.basestorage.cfg` and `BepInEx/config/BaseStorage.categories.txt`. Items stay in your chests. The data BaseStorage stores on chests and stations, and your favourites in the player profile, is ignored by vanilla, so worlds and characters still load. In a group, everyone removes it together, or everyone stays on the same version.
