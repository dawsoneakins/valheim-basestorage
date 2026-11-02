# BaseStorage v0.4.2 release notes

## [0.4.2] - 2026-11-01

### Fixed

- Upgrades now work with materials from base chests, for armour, weapons and tools alike. They were cancelled silently while the chests were being reserved, so nothing was taken and nothing was upgraded. Only upgrades with every material already in the inventory worked.

### Changed

- Organize sorts as much as it can. A stack with no room in its own category now goes to a Misc chest, or else to the chest whose category sits closest to its own in the categories file, so raw food ends up beside cooked food. It only moves if that is better than where it is, and the next Organize takes it home once there is room. Stacks only stay put when nothing better has room.
- When there are too few empty chests to go round, the categories that need the most room get them, instead of whichever Organize reached first.
- Put away inventory uses the same nearest related chest when an item has no chest of its own.
- A chest that takes in a stack from another category keeps its own category from then on.
- Put away inventory now puts food away. Star food as a favourite, or keep it on the hotbar, to keep it on you. The `@food` entry is gone from `DepositNeverItems`; an old config that still lists it is harmless.
- `AutoAssignMixedChests = false` turns off the new sorting along with the mixed-chest step.

### Search

- Results are ranked: an exact name, then the start of a name, then the start of a word, then any part of a name.
- Several words match in any order, so `ore copper` finds Copper ore.
- Items are also found by category, and so are chests: `food` lists every Raw Food and Cooked Food chest, even empty ones, then any food sitting elsewhere. Keep chests are found by `keep`.
- Results show the item's icon, the matched letters in gold and the chest's distance. A chest in use still shows what it holds.
- The search box lights up while you type and has a **Clear** button.
- The header reads `Copper ore x212 in 3 chests`, or `5 items, 412 in all, in 7 chests`, and a search that finds nothing says so in the list.
