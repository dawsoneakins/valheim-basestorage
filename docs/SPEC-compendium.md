# Don's QOL spec: compendium

Status: built on `feat/character-menu`; the Core rules are tested, and the game-side code has not yet run in the game. Run section 6 first.

## 1. What the player gets

A spoiler-free, searchable, cross-linked wiki inside the inventory screen, opened with the **Compendium** button in the character menu. Tabs: Items, Creatures, Biomes and Building. Search matches title, group and description, and needs every word.

- **Item:** stats, recipes, what it's used in, what drops it and where it's gathered.
- **Creature:** boss flag, base health, strongest attack, damage responses, drops and the biomes it spawns in, by day or night.
- **Biome:** a short description, creatures and resources.
- **Building piece:** comfort, station needed and cost.

## 2. Spoilers

A page exists only once the player has discovered its subject, so it can't be searched for or linked to before then.

| Subject | Discovered when |
|---|---|
| Item | It's in the game's known materials or known recipes, so it's been picked up or can be crafted |
| Building piece | It's in the game's known recipes, so it shows in the build menu |
| Station | It's in the game's known stations |
| Biome | It's in the game's known biomes, or the mod saw the player there |
| Creature | The mod saw it within 30 metres of the player, or the player knows its trophy |

The game keeps no list of creatures met, so the mod saves its own on the character under `basestorage.seen`. Its first sightings start when the mod is first running; the trophy rule covers older characters.

Pages name nothing undiscovered. Lists such as **Used in**, **Dropped by**, **Drops**, **Appears in**, **Gathered from** and a biome's creatures and resources just leave undiscovered things out. A recipe or a piece's cost must stay complete to be useful, so an undiscovered ingredient reads `Undiscovered item x10` with no link, and an undiscovered station reads `an undiscovered station`. Sources appear only with biomes the player has visited.

Places are left out, because the game doesn't record which ones a player has visited. Biome descriptions describe only the land.

## 3. Where the facts come from

Facts are read at runtime from `ObjectDB`, `ZNetScene`, `SpawnSystem` and `ZoneSystem`, never written by hand or copied from a website, so they follow game updates and include other mods' additions. The only hand-written facts are the one-line biome descriptions in the strings table. Item, creature and piece descriptions are the game's own text, in the player's language; the mod's labels are in English.

Core turns plain records into pages (`CompendiumBuilder`) and searches them (`CompendiumBook`). A link is made only when its target page exists. Plugin only reads the game (`GameCompendiumSource`) and draws (`CompendiumWindow`).

The game's data is read once per world. The book is rebuilt from it on every open, with what the player knows then.

## 4. The window

The window is a child of the inventory screen, so the game already blocks movement and shows the cursor while it's open. Closing the inventory closes it. While the search box has focus, the inventory can't close, the same as the base menu's search. The character panel and its 3D view stop while the window is open.

The list reuses a screen's worth of rows while scrolling, so thousands of entries cost no more than a few dozen.

## 5. Known limits

- A creature within 30 metres counts as seen even behind a wall or underground, which can reveal it a little early.
- A page shows a creature's full stats and weaknesses from the first sighting, and an item's whole recipe list once the item is known. Only things undiscovered are hidden, not details of things found.
- Knowledge belongs to the character, as the game's does. A new character starts with an empty compendium.
- Loot in dungeon chests isn't listed: place contents load only when the world builds them, so they can't be read here.
- A gathering source's name comes from its prefab name, so some read roughly. A source the world places only inside locations isn't listed, because it has no biome to check.
- Creature figures are base values, before star levels and world modifiers.
- It needs a pointer, and it can't open while a chest is open, because the button is in the character menu.
- The mod's own labels and biome descriptions are English.

## 6. Unverified until playtested

- D-1. Every spawn list is reachable through the `SpawnSystem` prefab or a loaded instance, so creatures show their biomes.
- D-2. Vegetation prefabs lead to their drops, so biomes list their resources.
- D-3. The first open is quick enough to feel instant.
- D-4. Rows, scrolling, search typing and links work and look right.
- D-5. An existing character's known items, pieces, stations and biomes all show on the first open.
- D-6. Sightings are saved with the character and survive a reload.

Playtest. Agents do not launch the game or read saves.

1. On a brand-new character, open the inventory and press **Compendium**. Expected: it opens within a moment, showing only what you've met so far, with the Meadows the only biome. Search `iron` and `surtling`. Expected: nothing. (D-3)
2. Pick up a new item, close the compendium and reopen it. Expected: the item now has a page.
3. Stand near a creature for a couple of seconds, then reopen. Expected: it's under Creatures. Save, quit, reload and reopen. Expected: it's still there. (D-6)
4. Open a recipe with an ingredient you haven't found. Expected: `Undiscovered item x...` with no link, and no name anywhere.
5. Open a late-game character. Expected: its known items, pieces and biomes are all there, including creatures whose trophies it holds. (D-5)
6. Scroll the whole item list. Expected: smooth, with no gaps or repeated rows. (D-4)
7. Open Wood. Expected: stats, and **Used in** and **Gathered from** sections. Click a linked item, then **Back**. (D-2, D-4)
8. Search `bronze` on a character who knows it, typing letters such as `e` and `i`. Expected: the list narrows, and the inventory stays open. (D-4)
9. Open a creature you've met. Expected: health, drops you've found, damage taken and biomes you've visited. Open a biome. Expected: only creatures and resources you know. (D-1, D-2)
10. Open Building, then the workbench. Expected: its cost, with links.
11. Press **Close**. Expected: the inventory and character panel return.
12. Leave to the main menu, join another world and open it. Expected: it works, with the same character's knowledge.
13. With another item mod installed, find one of its items in play. Expected: it gets a page.
14. Check the BepInEx log and frame rate after a few minutes. Expected: no errors from the mod and no stutter once a second. (D-3)
