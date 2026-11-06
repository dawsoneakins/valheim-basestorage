# Don's QOL spec: move a base's items to another base

Status: built on `feat/base-move`; every Core rule is tested, and the game-side code has not yet run in the game. Run section 8 first.

`docs/SPEC.md`, `docs/SPEC-v0.2.md`, `docs/SPEC-qol.md` and the shipped code are current behaviour; this document changes them only where a section below says so. Principle 1 still holds: items are never lost or duplicated, and on any doubt the mod skips the chest. Core holds every decision and Plugin is a thin adapter. Rules R1 to R6, R8 and R9 apply unchanged. R7 applies except for the one exception in section 3.

## 1. What the player gets

A player registers a base under a name. From a station in another base, or from the menu, they choose **Move items to...**, pick a registered base, read a preview and confirm. The items in the source base's chests move into the destination base's chests by category. It ends with one line, for example `Moved 214 stacks to Swamp Keep; 12 stacks stayed (portal rules); 3 stacks had no room`.

## 2. The portal rule

The rule mirrors `Inventory.IsTeleportable` item by item:

1. An item with `m_shared.m_toolTier >= 1000` never moves. The game blocks these from portals whatever the world setting is.
2. Otherwise, with the `TeleportAll` world key on, the item moves.
3. Otherwise the item moves only when `m_shared.m_teleportable` is true.

An item that stays is left in its source chest, and the preview and summary count it and name its types. The rule reads the world key; there is no mod setting. `TeleportWorld.m_allowAllItems` is a per-portal flag and is deliberately ignored, because a move does not go through a portal.

## 3. How a move runs

Any player can start a move: the host, a friend, or a single-player. The host is the only machine that writes chest data for a move, whoever started it, so a move has one authority and no step where two machines each hold half of it.

The existing reservation protocol keeps working for chests the requester or host has loaded. It holds those chests still and marks them in use. It cannot reach a far base, because its handshake goes through a loaded `Container` and nobody has one there. Writing far chests directly on the host is the R7 exception.

1. **Ask.** The requester sends the host `BaseStorage_MoveRequest` with the source base and destination name. The host, the only machine holding the far base's records, answers with the plan summary. The bases list reaches a friend the same way.
2. **Find.** Registered bases are stored on a station under `basestorage.base`. The host resolves both bases from world records, using the same membership rules as a loaded base: `GetAllZDOsWithPrefabIterative` per station and container prefab, then the graph and range rules in Core. This search is slow, so it runs across frames and never from Update (quality audit rule).
3. **Read.** A chest's contents are the `ZDOVars.s_items` byte array. Decode them into a temporary `Inventory` sized from the prefab's `m_width` and `m_height`.
4. **Check.** Refuse the whole move when a player other than the requester or host has either base in their active area, and name that player. Skip any chest that is a `Keep` chest, a vehicle, private, warded against the requester, or in use. Skip any chest that fails the decode check in section 4. The host checks privacy and wards for the requester's player id, not its own.
5. **Plan.** Core plans the moves from plain snapshots: all source stacks that pass the portal rule, placed with the Put Away placement (merge, empty slots, claimed overflow). A stack with no room stays where it is.
6. **Preview.** The requester sees the plan and confirms. No chest is held during the preview.
7. **Hold.** Requester and host each reserve the involved chests they have loaded, through the existing protocol, with a long hold timeout of its own. The usual `ReserveTimeoutMs` (1.5 seconds) lets go of a chest even while it is held, which would open the duplicate path below. The host skips chests the requester already holds. A chest the player has open cancels the move, because a change in its window would save over the host's write. The requester then sends `BaseStorage_MoveConfirm` with the id and `DataRevision` of every chest it holds.
8. **Apply.** The host waits until its copy of each chest has reached that revision, up to `ReserveTimeoutMs`. It re-plans and compares with the previewed plan. Any difference cancels the move and asks for a retry. If they match, it writes every changed chest in one frame on the main thread, destination and source together. The host's world never holds a half-finished move.
9. **Settle.** The host sends `BaseStorage_MoveDone` with the written revision per chest. The requester keeps its holds until its own copy of each chest has reached those revisions, then releases them and shows the summary.

Step 9 is required. `RPC_ZDOData` applies incoming data only when its `DataRevision` is higher than the local one, and it compares nothing else. A requester that changes a held chest's record before the host's write arrives can tie or beat that revision, keep the old contents and later send them back, which duplicates items. A held chest therefore gets no write from the requester until the host's write has arrived. S-BM-004 confirms from the game's code whether `Container.SetInUse` raises the revision; it is treated as if it does until then.

If the requester disconnects after the host writes, the world is already correct: its copy is discarded when it leaves. If the host crashes before saving, the world reloads from the last save with no move applied.

## 4. Safety rules

- **Decode check.** `Inventory.Load` silently drops an item it cannot add, such as one from a removed mod. Before a chest takes part, the item count in its saved header must match what the decode produced. Any difference skips that chest, and it is never rewritten. Items move as clones of the decoded item, so durability, crafter and custom data travel with them.
- **Conservation.** Before the write, per item identity: source total plus destination total must equal the same totals after the plan. A mismatch writes nothing.
- **Same base.** If a chest or station belongs to both, the move is refused: Organize is the tool for that. Bases that are merely close are fine, because each machine holds what it has loaded.
- **One at a time.** The host runs one move at a time across the world. An Organize running at either base makes the holds fail or the re-plan differ, so the move cancels with nothing written; the Organize lock itself is not taken, because it needs a loaded station.
- **Far station range.** A far station's range comes from its prefab, because extensions need the loaded object. A far base can only come out smaller than it looks, so a chest can be left out but never wrongly included.
- **Failure.** Any exception before the write leaves everything untouched and is logged at Error (R3).
- **Settle timeout.** If the host's write has not arrived within the timeout, the requester keeps the chests marked in use, says so, and logs at Error. It never releases early, because that is the duplicate path in section 3. Leaving the game clears the holds safely.

## 5. Who can run it

Friends, hosts and single-players all use the same flow. In single-player and for the host, the network steps run locally. Two limits remain, each with a clear message:

- A third player standing at either base refuses the move, naming that player, because their game owns those chests and the host cannot hold them still.
- A host and a friend can stand at either base, including the source and the destination at once, because each holds its own loaded chests.

## 6. Stories

**S-BM-001 Portal rule.** Core rule from section 2, with a test per branch. The Plugin reads the two item fields and the world key.

**S-BM-002 Move planner.** Core planner over snapshots, reusing the Put Away placement. Fuzz test for conservation. Stacks that stay, have no room or sit in skipped chests are all reported.

**S-BM-003 Named bases.** Set, rename and clear a name, stored under `basestorage.base` on every loaded station of the base, so losing one station keeps the name. A station built later has no name until the base is named again. Two bases with the same name resolve to the one with the lowest station id.

**S-BM-004 Unloaded chest reader and writer.** Plugin adapter for section 3 steps 2, 3 and the write in step 8, with the decode check. Patch targets are added to `PatchTargetNames.cs`.

**S-BM-005 Move protocol.** Core state machine for section 3 steps 1 and 6 to 9 on both sides, and the three `BaseStorage_Move*` RPCs. Tests cover every timeout, a disconnect at each step, a changed plan, and a late host write. A requester that releases before the settle step must fail a test.

**S-BM-006 Move in the game.** Button, destination list, preview, summary, strings table. Playtest steps below.

**S-BM-007 Docs.** Player README, settings table if a setting is added, Known limits, CHANGELOG.

## 7. Unverified until playtested

Read from the game's code: chests save to `ZDOVars.s_items`; `ZDO.Set` raises the data revision and has no owner check; `Container.Load` reloads on a revision change unless the chest is in use; the host releases ownership of far records (`ReleaseNearbyZDOS`); `ZDOPeer.ShouldSend` does not exclude a record's owner, so a host's write is sent to the friend who owns that chest; `RPC_ZDOData` applies it only with a higher `DataRevision`. These assumptions cannot be settled from the code and need the steps in section 8:

- A-1. The host holds the records of every far chest in a peer-hosted world.
- A-2. A write to a far chest survives a save, a world reload and walking to that base.
- A-3. A write to a far chest with no owner reaches a friend who later visits.
- A-4. The temporary inventory round trip keeps all item data, including items with custom data from other mods.
- A-5. A chest with an unknown item is detected rather than silently cleaned.
- A-6. A friend holding a source chest receives the host's write, its open game shows the new contents after release, and the old contents never come back.
- A-7. A friend's game does not take a chest's record back from the host, or send older data, during a move (`ReleaseNearbyZDOS` runs on its own timer).

## 8. Proof-of-concept playtest (operator, on a copy of a test world)

Agents do not launch the game or read saves. Back up the world first. Everything below starts from the base menu: **Name base** names the base you stand in, and **Move items** lists the other named bases.

1. Single-player: base A and base B far apart, one marked chest each. Stand at B and use **Name base** to name it, then go to A. Put wood, a crafted sword with a crafter name and a stack of copper in A's chest. Teleport-all off.
2. Run the move from A to B. Expected: wood and sword leave, copper stays, and the summary names copper. (A-1)
3. Save, quit to menu, reload, go to B. Expected: wood and sword are there with the crafter name and durability intact, and nothing is duplicated in A. (A-2, A-4)
4. Turn the `TeleportAll` world modifier on and repeat. Expected: copper moves.
5. Host moves from A to far base B with a friend elsewhere, then the friend walks to B. Expected: the friend sees the new contents. (A-3)
6. Friend stands at A with the host far away and moves from A to B. Expected: preview and summary appear for the friend, and A's chests update without reopening the area. Count every item at both bases: the totals match the totals before the move. (A-1, A-6)
7. Straight after step 6, the friend reopens A's chests, moves an item in each and walks away and back. Host saves and reloads. Expected: the moved items have not returned to A. (A-6, A-7)
8. Friend stands at A and the host stands at B, then the friend starts a move. Expected: it succeeds, and B's chests update for the host.
9. Three players: a third player stands at B, and a friend starts a move. Expected: refused, naming the third player, and nothing changes.
10. Friend confirms a move and quits the game straight away. Host checks both bases, saves and reloads. Expected: totals match, with no item lost or duplicated.
11. Host quits without saving straight after a move, then reloads. Expected: the world is back to the last save, with the items at A.
12. Put a modded item in a chest, remove that mod, run the move. Expected: the chest is skipped and reported, and the item is back after reinstalling the mod. (A-5)
13. Open a source chest, then start a move through the hotkey-free path: open the base menu with the chest still open and confirm. Expected: `Move cancelled: a chest is in use, try again`, and nothing moved.
14. Put a ward owned by another player over base B, with you not on its list. Expected: B's warded chests are skipped and counted, and nothing lands in them.
15. Open the inventory with the base menu open. Expected: the weight and armor numbers are visible, and the menu does not cover the crafting panel at your normal resolution.
16. Open the base menu at a low resolution and at your normal one. Expected: all twelve buttons are visible and none overlap, and the base list opens under **Move items**.

## 9. Out of scope

Dedicated servers, carts and ships, moving the player's own inventory, and a cost or crafted item for moving.

## 10. Other playtest steps on this branch

Put away selected:

1. Stand in a base and press **Put away selected** in the base menu. Expected: a line asks you to click items.
2. Click two inventory items, one of them on the hotbar. Expected: each shows a blue marker and says `Selected`, and neither is picked up. Click one again. Expected: `No longer selected`, and its marker goes.
3. Select it again and press **Put away selected** again. Expected: only the selected stacks go to the chests by category, and the markers go.
4. Hold two identical swords, equip one, select both and put away. Expected: only the unequipped sword moves.
5. Press the button twice with nothing selected. Expected: `Nothing selected, so nothing was put away`.
6. After step 5, click an item. Expected: normal vanilla behaviour, because select mode is off.
7. Press **Put away selected**. Expected: it reads **Click items to select**, and **Cancel** replaces **Highlight**. Select two stacks. Expected: both slots are washed blue and the button reads **Put away 2 stacks**.
8. Press **Cancel**. Expected: the blue goes, the label and **Highlight** return, and clicks pick items up again. Select again, then close the inventory and reopen it. Expected: nothing is selected.
9. Shift + right-click a stack in a base. Expected: it goes to the chests with no popup, even with `DepositPreview` on, and one summary line appears. Do it to an equipped item. Expected: `Equipped items are not put away`, and nothing moves. Do it outside a base. Expected: `Not in a base`, and nothing moves.
10. Right-click without Shift. Expected: normal use and equip. Shift + right-click a stack in an open chest. Expected: normal vanilla behaviour.

## 11. Base management (first called All bases): changes to the sections above

This replaces **Name base** and **Move items** in the base menu, and supersedes S-BM-003 and the Find step's lookup by name.

- **Every base is listed.** The host finds them from world records (`BaseDirectory`): linked stations form one base, and a station with no player-built chest in range isn't one. The window opens from the base menu, wherever the player stands.
- **Names are optional.** An unnamed base shows a made-up name, numbered per biome by station id. It isn't saved. A saved name still lives on every station under `basestorage.base`, and a station added later joins its base's name automatically, because the name of any station in a base counts.
- **Any two bases.** The request names both bases by a station id. The player needn't stand in either, and a move between two far bases has no chests to hold, so it goes straight to the host's write.
- **Renaming from anywhere.** A rename goes to the host, which writes every station's record. A ward denying the player over any station of the base refuses it.
- **The list's numbers.** Used slots come from each chest's saved header, without decoding items, so opening the list stays cheap. Far stations' range still leaves out extensions, so a far base's chest count can read low.

New unverified assumptions:

- A-8. A rename of a base the host hasn't loaded survives a save and reload, and a friend who walks there sees it.
- A-9. A move between two far bases, with the player in neither, survives a save and reload with totals matching.

Playtest:

1. Open **Base management** from the base menu, away from any base. Expected: every base with a chest is listed, nearest first, and a lone workbench isn't.
2. Rename a far base, save, quit and reload. Expected: the name stays. (A-8)
3. Stand in neither base. Press **From** on A and **To** on B, and confirm. Expected: the preview names both bases and the items move. Save, reload and count both bases. Expected: totals match. (A-9)
4. Stand in A with the list open. Expected: A says `You are here` and starts as the source.
5. Open the base menu. Expected: groups headed **This base** and **Open chest**, **Close** in the top corner, and chest rows with item icons, `+N` for extra kinds, a thin fill bar and `Empty` on empty chests.
6. Search the base menu for an item. Expected: results still show the found item and its count.
7. As a friend, open the list, rename a base and move between two far bases. Expected: all work, and the host sees the same list.
