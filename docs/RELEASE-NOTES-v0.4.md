# BaseStorage v0.4 release notes

## [0.4.0] - 2026-10-31

### Added

- Feed stations from base chests. Shift + use on the ore or fuel input of a smelter, kiln, blast furnace, windmill, spinning wheel or eitr refinery opens a panel with every item it accepts and how many the base holds. Turn on `Keep fed` for an item and the station is topped up from the chests. `Leave in base` sets how many stay behind. The choices are saved on the station and shared with everyone.

### Changed

- Crafting requirements with items in base chests show the total you have against what you need, such as `26/50`, because the longer form cut off the needed amount.

### Fixed

- Place stacks moved favourite items. It now leaves them where they are.
- A favourite slot no longer shows its star while the slot is empty.

Stations are only fed while a player is nearby, because the game only runs the parts of the world a player has loaded.
