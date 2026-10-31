# BaseStorage v0.3 release notes

## [0.3.0] - 2026-10-31

### Changed

- The BaseStorage menu now scrolls to the open chest's row and lights it clearly, whether the chest was opened from the list or in the world. A row already in view stays where it is.

### Fixed

- The installer and uninstaller no longer stop with a `Join-Path` error when Steam still lists a library on a drive that is missing.
- The favourite marker never showed in the game, because it was copied from a switched-off image. It is now a gold star in the slot's bottom-right corner, on top of the item icon.
- Menu and station buttons no longer show the Craft button's `Missing requirement` hover text.
- Organize base, Put away inventory and Close in the menu no longer look greyed out.
