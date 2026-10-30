# BaseStorage release notes

## [0.2.0] - 2026-10-30

### Added

- Base Storage menu, opened from an icon under your name: every chest in the base you stand in with its fill, distance and rule. Clicking one opens the normal chest window from anywhere in the base. Organize base and Put away inventory now live here.
- `Category: <name>` line on item tooltips, from the same list Organize uses, with the `ShowCategoryTooltip` setting.
- One-click installer and uninstaller. The installer finds Valheim through Steam and sets up BepInEx.

### Changed

- Organize retries until nothing more can move, so a stack with no room can use a slot freed later in the same run.
- A dropdown sets a chest's rule, from the chest window or the menu.
- Categories Organize gives mixed chests are saved to the chest, and the preview says how many, and why any stacks are left over.

### Fixed

- The mod did not work in the game: buttons failed to load, hotkeys threw on every frame, the station buttons were hidden on normal screens, and every Organize and Put away stopped on its first chest.
- Moving an item could copy the durability or crafter of a similar stack. A move now copies the exact stack it checked.
- Organize ignored `BaseStorage.categories.txt`. It now uses the same list as the Category button.
