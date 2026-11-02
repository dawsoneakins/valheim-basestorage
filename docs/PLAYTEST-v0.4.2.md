# BaseStorage v0.4.2 playtest

## Upgrades from base chests

1. Put a piece of armour in your inventory, not worn, and put the upgrade materials in a base chest with none on you. At a workbench, open the Upgrade tab, pick the armour and press Craft. Expected: the bar fills, `Used materials from base chests` shows, the quality goes up by one, and the chest loses the materials.
2. Repeat with the armour worn. Expected: it upgrades the same way. The game takes an upgraded item off you, so put it back on.
3. Repeat with a weapon and with a tool. Expected: both upgrade.
4. Split the materials between your inventory and a chest. Expected: it upgrades, and your inventory is used first.
5. Start an upgrade and switch to the Craft tab before the bar fills. Expected: nothing is taken and nothing is upgraded.
6. Craft a new item from chest materials on the Craft tab. Expected: it works as before.

## Organize

1. Set up a base with every chest given a category, no chest for Trophies, and a stack of trophies in a Wood chest with room in others. Press Organize base. Expected: the trophies move to the Monster Drops or Gear chest, whichever exists, and the preview says how many stacks go to the nearest related chest.
2. Give a chest the Misc category and repeat. Expected: the trophies go to the Misc chest.
3. Press Organize base again. Expected: `Nothing to move`, or only the stacks that have since gained room.
4. Add a chest and set it to Trophies, then press Organize base. Expected: the trophies move into it.
5. Leave one empty chest, with one category holding a single stack and another holding many, neither with a chest. Press Organize base. Expected: the empty chest goes to the category with many stacks.
6. Fill every chest and press Organize base. Expected: the summary still reports stacks with no room, and nothing is lost.
7. Set `AutoAssignMixedChests = false` and press Organize base on a base like step 1. Expected: stacks with no chest of their own stay where they are.

## Put away inventory

1. Carry food in the backpack rows, not starred. Press Put away inventory. Expected: it goes to its food chest.
2. Star the food and press again with more of it in the backpack. Expected: it stays with you.
3. Keep food on the hotbar with more of the same in the backpack. Expected: both stay with you.
4. Carry meads, arrows and bait. Expected: they stay with you, as before.
5. Carry an item whose category has no chest. Expected: it goes to a Misc chest, or else the nearest related chest.

## Search the base

1. Search `copper`. Expected: chests holding Copper come before chests holding only Copper ore, each row shows the item icon, the gold matched letters and the distance on the right, and the header reads like `Copper x40 in 2 chests`.
2. Search `ore copper`. Expected: Copper ore is found.
3. With an empty Raw Food chest and a Cooked Food chest set up, search `food`. Expected: both are listed, the word gold in their category labels, the header counts them, and food stored elsewhere follows. Press **Highlight**. Expected: they all glow.
4. Search something that one chest in another player's use holds. Expected: the row shows the item and `(in use)`.
5. Search `zzz`. Expected: the header reads `No matches`, and the list says nothing matches `zzz`.
6. Click into the box and type. Expected: a dark field with a magnifying glass, the text clear of the left edge and centred, a gold caret at the same height as the text, and an edge that turns bright gold, going back when you click away. Type past the end of the box. Expected: the text scrolls inside the box and stays clear of the Clear button.
7. Type something and press **Clear**. Expected: the box empties, keeps the cursor, and the full list returns.
8. Look at rows and the Clear button at a narrow window or a low resolution. Expected: nothing overlaps.
9. Close the search and look at the normal list. Expected: rows look as they did, with no icon and no gap.
