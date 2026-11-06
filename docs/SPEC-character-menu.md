# Don's QOL spec: character menu

Status: built on `feat/character-menu`; the Core rules are tested, and the game-side code has not yet run in the game. Run section 6 first.

`docs/SPEC.md`, `docs/SPEC-v0.2.md`, `docs/SPEC-qol.md` and the shipped code are current behaviour; this document changes them only where a section below says so. Principle 1 still holds: items are never lost or duplicated.

## 1. What the player gets

A panel below the inventory with a live 3D view of the character between its slots.

- **Worn slots:** helmet, chest, legs, cape, utility and trinket. Equipping one of these, by any route, moves it out of the inventory grid and into its slot, which frees that grid space.
- **Hand slots:** weapon, shield and ammo only show what is equipped. Those items stay in the inventory or hotbar, so the number keys keep working.
- **Putting gear on:** right-click it, use its hotbar key, or pick it up and click its slot.
- **Taking gear off:** click its slot. It goes to the first free grid space. With no free space it stays in its slot, unequipped, shown grey, and a line says so. It returns to the grid when space opens.
- **3D view:** shows current gear, hair and colours, on a dark recess with a soft firelight glow at the feet. Drag it to turn the character.
- **Look:** the panel uses the game's wood, like every panel the mod builds (`PanelStyle`). Empty slots show a darkened item outline, and a held piece of gear lights the slot it fits. Hand slots sit behind a divider with a fainter background, because they only mirror what is equipped.
- **Chests:** the game puts the chest panel in the same place, so the character panel hides while a chest is open. The grave window is a chest, so it hides there too.

## 2. How worn items are stored

A worn item stays in the player's `Inventory` with `m_gridPos` set to row 4, column 0 to 5. The inventory stays 8x4. Read from the game's code:

- `Inventory.Load` adds items with the position check skipped, so row 4 survives saving and loading.
- `InventoryGrid.UpdateGui` skips an item whose element is null, and `GetElement` returns null past the grid, so row 4 is not drawn.
- `FindEmptySlot` searches only inside the grid, so nothing lands in row 4.
- `Humanoid.EquipItem` needs `m_inventory.ContainsItem`, which is why the items cannot live in a second inventory.
- `GetBoundItems` reads row 0 only, so the hotbar is unaffected.
- `Player.SetupVisEquipment` fills any `VisEquipment` from the player's gear, which is what the 3D view uses.

`Humanoid.DropInvalidItems` drops anything outside the inventory, and `Player.SetInventorySize` calls it on every spawn with saved rows and when a trader sells a row. So it gets a prefix and finalizer that count the worn row as inside for exactly as long as it runs. Anything else out of place still drops.

Two counts assume every item is in the grid, so they are patched to count only items inside it: `Inventory.GetEmptySlots` and `Inventory.HaveEmptySlot`. Without this, worn gear would use up space it no longer occupies.

`PlayerInventoryCapture` rejected any stack outside the grid, which would have stopped every Put Away. It now accepts the worn row. Equipped stacks are excluded from Put Away as before.

## 3. Equip and unequip

`Humanoid.EquipItem` gets a postfix for `Player` and worn types only: the item takes its slot. Anything already in that slot takes the new item's old position, so a full inventory can still swap gear. `Humanoid.UnequipItem` gets a postfix: an item in the worn row goes to a free grid space, or stays when there is none. `Player.EquipInventoryItems` runs through `EquipItem` on load, so gear already equipped when the mod is installed moves into its slot on its own.

## 4. Death and graves

`CreateTombStone` unequips gear, so worn items go back to the grid where there is room. A full inventory can leave up to 6 in the worn row. `MoveInventoryToGrave` copies positions, so those reach the grave at row 4.

A grave would hide them, so graves are given a fifth row, in `Container.Awake` for a reloaded grave and after `MoveInventoryToGrave` for a new one. `TakeAll` needs nothing new: `MoveAll` retries an item that fails at its old position with `AddItem(item)`, and `EasyFitInInventory` reads the patched `GetEmptySlots`.

## 5. Known limits

- **Removing the mod:** worn items stay worn and saved, because the game keeps them on load, but vanilla draws no slot for them and they can't be taken off. Take gear off before uninstalling. Reinstalling the mod also brings them back.
- **Hotbar:** armor or a utility item on the hotbar leaves it when equipped, and its key no longer toggles it.
- **Extra inventory rows:** the game has an `invrows` value a trader purchase can raise. With five or more rows the worn row would overlap the grid. Nothing handles that yet.
- **Gamepad:** a gamepad can equip from the grid, but taking gear off and turning the view need a pointer.
- **Other inventory mods:** a mod that resizes or rearranges the player inventory may conflict.

## 6. Unverified until playtested

- C-1. Worn items survive save, quit, reload and a host change.
- C-2. The element prefab works outside a grid: icon, durability bar, tooltip and clicks.
- C-3. The panel fits below the inventory and doesn't cover the QOL menu.
- C-4. The grave window shows a fifth row, and a reloaded grave keeps it.
- C-5. The stripped character copy shows gear, plays its idle pose and leaves nothing behind in the world: no sounds, no second player on the map, no error spam in the log.
- C-6. The 3D view stays lit at night and in a cave, with its backdrop showing and no white wash.
- C-7. Opening the inventory doesn't noticeably drop the frame rate.

Playtest, on a copy of a test character and world. Agents do not launch the game or read saves.

1. Open the inventory. Expected: the panel is below it, showing your character and nine labelled slots, and the weight, armor and QOL menu are all visible. (C-3, C-5)
2. Right-click a helmet. Expected: it leaves the grid, appears in Helmet, and appears on the 3D character. The weight and armor numbers are unchanged. (C-2)
3. Pick up a chest piece and click the Legs slot. Expected: a line says it doesn't go there, and it stays held. Click the Chest slot. Expected: it's worn.
4. Equip one item of each worn type, then fill every grid space. Expected: all 32 spaces can be filled.
5. With the grid full, click a worn item's slot. Expected: it stays, grey, and a line says there's no room. Drop one grid item, and it returns to the grid.
6. With the grid full, equip a different helmet from the grid. Expected: the two swap places, and nothing is lost.
7. Hover and use a weapon, shield and arrows. Expected: their slots show them, the items stay in the grid, and number keys still swap them.
8. Run Put Away at a base. Expected: it works, and worn gear stays on.
9. Save, quit to menu and reload. Count your items. Expected: everything is still worn, with the same durability. (C-1)
10. Open a chest. Expected: the panel hides, then returns when you close the chest.
11. Drag across the 3D view. Expected: the character turns. Check it at night and in a cave. (C-6)
12. Die with some gear on and a part-full inventory. Fetch the grave. Expected: every item comes back.
13. Die wearing all six with a full grid, so 38 items reach the grave. Walk away so the grave unloads, come back and open it. Expected: all 38 are visible in the grave and can be taken. (C-4)
14. Play with a friend who also has the mod. Expected: each sees the other's gear normally, and there is no second copy of anyone on the map. (C-5)
15. Check the BepInEx log and frame rate after a few minutes. (C-5, C-7)
16. Empty each slot. Expected: a dark outline of that kind of item, and the slot's name in its tooltip. Pick up a chest piece. Expected: the Chest slot lights up within a moment, and goes dark when you put the piece down.
17. Compare the panel with the inventory above it. Expected: the same wood, matching widths, and buttons under the view rather than over it.
