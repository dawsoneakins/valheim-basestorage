# BaseStorage v0.2 playtest checklist

These steps cover what v0.2 adds. `docs/PLAYTEST.md` still covers v0.1. Use a throwaway character and world.

## v0.2 sorting and tooltips

### Mixed chests and saved rules (S-A-001, S-A-003)

1. On a new world, place a workbench and three chests in range. Put Wood, Stone and Bread in each chest, in different amounts, and leave every chest on Auto.
2. Press Organize base and read the preview.

Expected: the preview says how many chests will get a category. Click yes.

3. Hover each chest.

Expected: each shows a category in brackets, chosen by Organize. Run `bstest check` if the test plugin is installed: nothing is lost.

4. Fill every slot in all three chests with items of the wrong category and press Organize base.

Expected: if no slot is free, the preview says `This base is full. Free up space or add a chest.`

### Moved items stay the same item (S-A-004)

1. Put two identical swords in a chest, one badly worn and one nearly new, and leave a chest of another category beside it.
2. Press Organize base, then yes, and open the destination chest.

Expected: both swords keep their own durability, and the worn one has not become new or the new one worn. Repeat with an item you crafted: the crafter name stays.

### Your category file drives sorting (S-B-001)

1. Edit `BepInEx\config\BaseStorage.categories.txt` to move Wood into a category of your own, then restart the game.
2. Hover Wood in your inventory.

Expected: the tooltip ends with `Category: <your category>`.

3. Set a chest to that category, put Wood in another chest, and run Organize base.

Expected: the Wood goes to that chest.

4. Set `ShowCategoryTooltip` to false.

Expected: tooltips have no category line.

## The Base Storage menu (S-C-001 to S-C-007)

1. Open a workbench.

Expected: a chest icon sits at the right of the row under your character's name, the same size as the other icons, with the row evenly spaced. Hovering it says Base Storage. No Organize base or Put away inventory buttons appear under the workbench.

2. Click the icon outside every base.

Expected: the panel opens beside the chest window area and says `Not in a base`. Esc closes it with the inventory.

3. Stand in a base and open it.

Expected: a header with stations, chests and percent full, then one row per chest with its fill, distance and rule. It covers neither the crafting panel nor the chest window.

4. Click a chest row, including one far across the base.

Expected: the normal chest window opens on that chest, its row lights up and its label reads Close. Every slot shows, and you can drag, split and move items in and out. Walking away does not close it. Clicking the row again closes the chest.

5. Close the inventory, then walk away from a chest you opened normally.

Expected: the chest closes at the usual distance.

6. Click the rule on a row, and the Category button in the chest window.

Expected: a dropdown lists Auto, each category and Keep, and picking one sets it. Right-clicking the chest button still steps back one value.

7. Have another player open a chest, and try a private chest of theirs.

Expected: the row says `in use` or `no access`, and a private chest does not open.

8. With no chest open, look at **Take all** and **Place stacks** under Organize base. Then open a chest far from you and click each.

Expected: both are greyed out with no chest open. With one open, they act on that chest, the same as the buttons in the chest window. With a full inventory, Take all moves what fits and nothing is lost.

9. Open a chest from the menu, click **Highlight**, and close the inventory.

Expected: that chest glows for about 30 seconds, and you can walk to it. Clicking Highlight again on the same chest stops the glow early.

10. With a friend in the world, open a chest from the menu and click **Share**.

Expected: a map ping appears at the chest for both of you. The friend, with the mod and near enough for that chest to be loaded, sees it glow for about 30 seconds. Solo, your chest glows and you see the ping.

11. Click **Organize base** and **Put away inventory** in the menu.

Expected: the same preview and summary as before. Check the totals with `bstest check`.

## Put away inventory (still untested in the game)

Use a throwaway character, not your real one.

1. Fill the inventory with mixed items, keeping some on the hotbar and marking one favourite with Alt and left-click.
2. Stand in a base with chests that have categories, run `bstest snap`, click **Put away inventory**, then run `bstest check`.

Expected: `ITEMS OK`. Hotbar items, equipped items and favourites stay with you.

## Installer

1. On a copy of the game, or a friend's PC, with no BepInEx, extract the zip and double-click `Install BaseStorage.bat`.

Expected: it finds Valheim, says BepInEx was installed, and the game starts with `BaseStorage 0.2.0 loaded` in `BepInEx\LogOutput.log`.

2. Run it again.

Expected: it says BepInEx is already installed and keeps a backup of the replaced plugin files.

3. Run `Uninstall BaseStorage.bat` and answer no.

Expected: the plugin is gone, BepInEx stays, and saves are untouched.
