# BaseStorage v0.4.3 playtest

Checks for Restock. Run these in a game with BaseStorage installed. `docs/PLAYTEST.md` holds the earlier checks and is not repeated here.

## Restock

Setup: a base with a crafting station, at least two chests in its build range (one with Wood, one with Resin and Stone), and a character carrying Wood in the hotbar (top row), Resin in the hotbar and Stone below the hotbar.

1. Open the base menu next to a crafting station. Check that a Restock button sits on the first row beside Put away inventory, and that it opens no popup of its own.
2. At the normal panel width, check that Organize base, Put away inventory and Restock share the first row, and that Mark and Close sit on the second row. Check that no button overlaps another or the search box.
3. Click Restock with the player standing in the base. Check that the hotbar Wood and Resin stacks fill from the chests, that the Stone stack is unchanged, and that one top-left line appears, for example `Restocked 2 stacks, 60 items`.
4. Check that the chests lost exactly the items the player gained, and that no new stack appeared in the player inventory (U1).
5. Fill the Wood chest so that one stack is nearly full, then click Restock when the player's Wood stack has room for only part of it. Check the line names the items left behind, for example `Restocked 1 stack, 20 items; 40 items left behind` (U2).
6. Check that a stack marked as a favourite (Alt+click a slot) outside the hotbar is also topped up, and that the Stone stack stays alone (U3: the hotbar is the top row).
7. Bind `RestockKey` in the config under `[Keys]` (for example `R + LeftShift`) and restart. Stand in the base and press the key. Check that Restock runs as in step 3.
8. Unbind `RestockKey` (empty). Press the old key. Check that nothing happens.
9. With `RestockKey` bound, open the chat box, the console or the search box in the base menu and press the key. Check that Restock does not start while any text box is open. The key also does nothing with the map open, in a cutscene or while dead.
10. Narrow the panel to its smallest width, 380. Check that Restock wraps to the second row, that no button overlaps another or the search box, and that the list rows stay readable.
11. Start Organize base, and while it is previewing or running, click Restock. Check the message `Organize in progress by <your name>` and that nothing moves.
12. With a second player, start Organize base as the second player. While it runs, click Restock as the first player. Check the message `Organize in progress by <second player name>` and that nothing moves.
13. Walk out of the base, away from every crafting station, and click Restock. Check the message `Not in a base` and that nothing moves.
14. Carry only full hotbar stacks, or stacks that no chest has. Click Restock. Check the line `Nothing to restock`.
15. Put the only matching chest in use by a second player (open it there). Click Restock. Check the line `Nothing restocked; 30 items left behind`, or one that reports the items left behind.
16. Carry two Wood stacks that differ in crafter or world level from the chest stack. Check that only the chest stack with the same identity moves, and that the others count as left behind (U4: identity reads the same from a chest item and an inventory item after a move). A stack that differs in crafter or world level and sits earlier in the inventory list can make the game fill that stack instead of the target, so Restock refuses the add; check that this also shows in the left-behind count and that no item is lost or copied.
17. Carry two Wood stacks of the same identity (same crafter, quality and world level), both with room, and put Wood in a chest. Click Restock. Check that the chest lost exactly what the player gained, that only the first stack in the inventory list was topped up, and that a stack Restock refused shows in the left-behind count.
18. Check the log (`BepInEx/LogOutput.log`). Check that each Restock wrote one Info line with the same text as the top-left line, and no Error line unless a run stopped early (`Restock stopped: ...`).

## Rename and icon

19. Open the inventory. Check the Don's QOL emblem under your name, in full colour, with no gold tint or raven shadow. Hover it. Check the tooltip says Don's QOL.

## Bukeberries

20. Carry bukeberries with no chest rule stopping them. Click Put away inventory. Check they are put away.

## Feeding

21. Shift + use a campfire in a base. Check the panel lists its fuel and the status reads `Fuel x of y`. Turn feeding on with Wood in a chest. Check the fire tops up within a few seconds and hover shows `Kept fed from the base`.
22. Do the same on a standing torch with its own fuel. Check a dungeon torch, which burns forever, shows no hint.
23. Shift + use an empty, roofed fermenter. Turn on a mead base. Check one goes in and the chest loses exactly one. Check it never feeds while fermenting, when exposed or when ready.
24. Shift + use a cooking station over a lit fire. Turn on raw meat. Check each free slot takes one and the chest loses that many. Put the fire out and check nothing more goes on.
25. Shift + use an oven. Check it takes fuel as well as food.
26. Set a chest holding the fuel to Keep and `KeepChestsCraftable = false`. Check that chest is not used.
27. Check Shift + use no longer does what it did on a fire before. Use without Shift must behave as vanilla.

## Sort a chest

28. Open a messy chest. Check Sort and Name sit under the Category button, on screen, and do not cover anything. Click Sort. Check `Sorted: n stacks`, identical stacks merged, and order by category then name.
29. Check item counts match before and after. Sort a chest holding two swords of different durability and two stacks by different crafters. Check they stay separate and keep their data.
30. Sort a Keep chest. Check it sorts. Have a second player open a chest and try. Check `Chest in use, not sorted`.

## Chest names and labels

31. Click Name. Type a name. Check it shows on hover, in the base menu, and is found by search.
32. Clear the name. Type a name with `<color=red>` in it. Check no rich text applies, for you or a second player.
33. Stand within 6 metres of named and categorised chests in a base. Check the labels float above them, disappear when you walk away, leave a base, open a menu or hide the HUD.
34. Set `ChestLabels = false`. Check the labels go.

## Gamepad

35. With a gamepad, open the inventory and press the right stick. Check the base menu opens and the hint shows. Check the D-pad and stick move the gold highlight across the buttons, the search box and the list, and that the inventory grid and recipe list stay still.
36. Press A on Organize, Put away and Restock. Check the Yes and No preview works on the gamepad. Press A on a row and on its rule cell. Check the chest opens, and the rule cycles.
37. Press A on the search box. Check the game's text entry appears and the search runs.
38. Open a chest and press the right stick. Check the focus moves to Category, Sort and Name, and that each works. Check the on-screen keyboard appears for Name on a Steam Deck.
39. Open a feed panel on a gamepad. Check on and off and the keep amounts work. Check B closes everything.
40. Change the gamepad layout setting and repeat step 35. Check keyboard and mouse still behave exactly as before.

## Unverified assumptions

These are the game behaviours the Core tests cannot prove. Steps 4, 5, 6, 16 and 17 above check them.

- U1: `Inventory.AddItem(item, pos)` does not fill the stack at `pos`. For a stackable item it fills the first stack in inventory list order that matches name, quality and world level and has room, and uses `pos` only when no such stack remains. Restock therefore adds only when the target is that first stack, and otherwise refuses and counts the items as left behind.
- U2: When the stack cannot take everything, the game merges part and returns false. Restock clamps the count to the room in the target stack, so it does not reach this.
- U3: The hotbar is inventory row 0 (`y == 0`). Confirmed from the game code: `Player.UseHotbarItem` reads row 0.
- U4: The identity fields (durability, crafter, variant, world level, custom data) read the same from a chest item and from an inventory item of the same stack after a move. Confirmed from the game code: `Clone` copies the identity fields, a merge writes only `m_stack`, and Save and Load share one path.
- U5: The Sort and Name buttons fit under the Category button on every chest size. Step 28 checks this.
- U6: The right stick press is free in the inventory on every gamepad layout. The game's inventory code does not read it, and steps 35 and 40 check it.
- U7: A chest stays held through the reservation while it is open for Sort, as it does for a category change. Step 30 checks this.
- U8: The text entry works on top of the open inventory. Steps 31 and 37 check this.
