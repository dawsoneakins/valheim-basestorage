# BaseStorage v0.4.1 playtest

## Favourites

1. Load a character that had stars in 0.4.0. Expected: the items in those slots still have stars, and every stack of those items has one too.
2. Open the menu and press **Mark**. Expected: the button turns gold. Left-click an item. Expected: it gets a star instead of being picked up, and the top-left message says so.
3. Left-click it again. Expected: the star goes, and the item stays in place.
4. Press **Mark** again and left-click an item. Expected: normal pickup.
5. Close the inventory with **Mark** on, then open it again. Expected: **Mark** is off.
6. Hold Alt and left-click an item with the menu closed. Expected: it still toggles the star.
7. Star Wood, then move the stack to another slot. Expected: the star moves with it, and the old slot has none.
8. Split a starred stack. Expected: both stacks have stars.
9. Put all of a starred item in a chest and take it back. Expected: the star returns.
10. Hover a starred item, including one in a chest. Expected: the tooltip has a `Favourite: Put away and Place stacks leave this` line.
11. With two stacks of a starred item, press Place stacks on a chest that holds some. Expected: both stay with you.
12. Press F6. Expected: starred items stay with you.

## Search the base

1. Open the menu. Expected: a `Search the base` box sits above the chest list, and all the buttons fit.
2. Click the box and type a word containing `e` and a Tab, such as `copper`. Expected: the inventory stays open, and every letter appears.
3. Expected: the list narrows to chests holding it, most first, each showing a line such as `Copper ore x212`, and the header reads `N found in M chests`.
4. Search for something with several matches, such as `ore`. Expected: rows show the largest match and `+N more`.
5. Search for something you do not have. Expected: an empty list and `0 found in 0 chests`, with no error.
6. Click a result. Expected: the chest opens in the normal window.
7. Take items out, then look at the search results. Expected: the count updates within a second or two.
8. Press **Highlight** with a search typed and no chest open. Expected: every matching chest glows for 30 seconds.
9. Press **Highlight** with no search typed and a chest open. Expected: it toggles the glow on that chest, as before.
10. Press Esc while typing. Expected: the box loses focus. Press it again. Expected: the inventory closes.
11. Clear the box. Expected: the full list and normal header return.
12. Close the menu and open it again. Expected: the box is empty.
