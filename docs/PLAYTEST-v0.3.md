# BaseStorage v0.3 playtest

## Menu: open chest row

Use a base with more chests than fit in the BaseStorage menu's list.

1. Open the menu, scroll the list to the bottom and click a chest at the bottom. Expected: the chest opens, its row turns gold and shows `Close`, and the list does not jump.
2. Scroll to the top, close the inventory, walk to a chest whose row is far down the list and open it by hand, then open the menu. Expected: the list scrolls so that chest's gold row is near the middle.
3. With the menu open and that chest open, scroll away from its row and wait 5 seconds. Expected: the list stays where you scrolled.
4. Close the inventory, open the same chest by hand again. Expected: the list scrolls back to its row.
5. Move the mouse off the gold row. Expected: the row stays gold and its text is readable.

## Installer: Steam library on a missing drive

1. On a PC whose `libraryfolders.vdf` lists a library on a drive that is not connected, run the setup bat. Expected: no `Join-Path` error, and it finds Valheim or asks for the folder.