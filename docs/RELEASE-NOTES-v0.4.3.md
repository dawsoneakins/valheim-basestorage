# Don's QOL v0.4.3 release notes

## [0.4.3] - 2026-11-02

BaseStorage is now called Don's QOL. Your settings, chest categories and favourites carry over, because the files keep their old names. Everyone playing together needs the same version, so update as a group.

### Added

- **Restock** tops up your hotbar stacks and favourite items from the base chests, with a button in the menu and an optional `RestockKey`.
- **More feeding.** Shift + use now works on fires, torches, fermenters, cooking stations and ovens. Fires and ovens keep their fuel topped up, fermenters take a mead base when empty, and cooking stations fill free slots while their fire is lit. Finished food and mead are not collected, so food left on a fire can burn.
- **Sort** tidies the open chest: identical stacks merge, then it orders by category and name. Different quality, durability or crafter stay separate.
- **Chest names.** Name a chest from its panel. Names show on hover and in the menu, and search finds them.
- **World labels.** Named and categorised chests show a floating label nearby, inside a base. Turn it off with `ChestLabels`, and set the range with `ChestLabelDistance`.
- **Gamepad control.** Press the right stick in the inventory to use the base menu, an open chest's buttons and the feed panel.

### Changed

- The mod has a new name and a new emblem.

### Fixed

- Put away inventory now puts bukeberries away.
- Station feeding respects Keep chests: they are used only when `KeepChestsCraftable` is on.
- Restock cannot duplicate items when a stack would land somewhere other than its target. This never shipped, it was caught before release.
