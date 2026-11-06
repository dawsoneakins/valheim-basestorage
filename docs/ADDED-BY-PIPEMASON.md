# Added by Pipemason

These features were proposed by the Pipemason program after the must-have acceptance criteria were delivered (spec section 9).
Each is marked `origin: pipemason-proposed` in .pipeline/ROADMAP.md, and package/README.md and docs/PLAYTEST.md describe each one.

## Base fill statistics (S-E010-001)

The status line next to Organize base and Put away inventory on a crafting station panel now ends with how full the base is: `Base: 6 stations, 41 chests, 64% full`. The `Base: N stations, M chests, P% full` line counts the occupied slots (slots holding at least one item) over all slots of the base's eligible chests, the same chests counted in M, so Keep chests are left out. It is rounded to the nearest whole percent, and an exact half rounds up. A base with no eligible chest shows `0% full`. Core computes the value and unit tests cover it. There is no new setting: `StationButtons` hides the line as before.

## Organize console command (S-E010-002)

The in-game console command `dons-qol organize` (Valheim started with `-console`, F5) starts an Organize for the base containing the player through the same runner, one-Organize-per-base lock and preview as the Organize base button. Outside a base it prints `Not in a base`. While another player holds the base's lock it prints `Organize in progress by <name>`. Any other argument prints `Usage: dons-qol organize`. The command is registered by a postfix on `Terminal.InitTerminal`, which is listed in the patch-target contract test. Core chooses the printed line and unit tests cover it.
