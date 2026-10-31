# BaseStorage v0.4 playtest

Use a throwaway world first. Solo needs one player; the multiplayer section needs two on the same version.

## Feed panel

Build a smelter in a base, with copper ore, tin ore and coal in the base's chests.

1. Look at the smelter's ore input. Expected: the hover text has a new line, `[Shift + E] Feed from base` (or your bound keys).
2. Press Shift + E. Expected: the inventory opens with a panel listing Coal first and then every ore the smelter accepts, each with an icon and `N in base`. Nothing goes into the smelter yet.
3. Check the counts against the chests. Expected: they match, and they update within a second or two when you move items.
4. Press Esc. Expected: the panel closes with the inventory. Pressing E alone on the input still adds ore as normal.
5. Repeat on the fuel input and on the kiln, blast furnace, windmill, spinning wheel and eitr refinery. Expected: the panel lists only what each station accepts, and a station with no fuel shows no fuel line.

## Keep fed

1. Turn Coal and Copper ore on. Expected: within a few seconds the queue reaches its limit and the fuel fills, the status line updates, and the chests lose exactly what the smelter gained.
2. Let the smelter run. Expected: it stays topped up with no input from you.
3. Turn Tin ore on as well. Expected: copper keeps going in first, and tin follows when the copper runs out.
4. Turn everything off. Expected: feeding stops and the items already inside keep processing.
5. Set `Leave in base` to 50 on Coal with 60 coal in the chests, and turn Coal on. Expected: 10 goes in, and 50 stays in the chests.
6. Break the smelter while it is being fed. Expected: the items inside drop as in vanilla, and nothing is lost or duplicated.
7. Leave the area and come back, then restart the game. Expected: the choices are still there.

## Busy chests

1. Open a chest that holds the ore while the smelter has room. Expected: no error. The smelter takes from another chest, or waits, and the chest window behaves normally.
2. Run Organize base while a station is being fed. Expected: it finishes or stops with its normal message. Check that no items went missing.

## Multiplayer

1. Both players stand near one smelter, and one turns on Coal and Copper ore. Expected: the other player sees the same choices within a few seconds.
2. Watch the queue for a few minutes. Expected: it never goes above the limit, which would mean two players were feeding it.
3. A player who does not own the station changes a choice. Expected: the change sticks.
4. Fill a private chest of one player's. Expected: the other player's panel does not count it unless they have access.
