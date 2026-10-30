# ADR 0002: Container ownership and access model

## Status

Accepted

## Context

BaseStorage must reserve a chest that another player may own or have open, write into it, and release it, without opening the vanilla inventory UI on the requester. This ADR records how vanilla Valheim decides who owns a networked chest, who may open it, and how "in use" is observed.

Verified against `assembly_valheim.dll` from Steam build 25909357 (SHA-256 `94998885b103d6e5f5480123eaee0a093e82af2ddeeee37536c15bf555f24f3a`).

Every claim below is labelled either (checked by the contract test) or (inspected only; the contract test cannot check this). Names are checked by the contract test in `tests/BaseStorage.PatchTargets.Tests` against the installed assembly. Behaviour (branch logic, order of calls) was read from the IL and is not checked.

1. Ownership. Every networked object is a ZDO reached through `ZNetView.GetZDO`. `ZNetView.IsOwner` is true when the view is valid and `ZDO.IsOwner` holds. `ZDO.GetOwner` returns the owner's session id (a long) and `ZDO.SetOwner` sets it. The local session id is `ZDOMan.GetSessionID`, and `ZNet.GetUID` is the value the owner compares against. The method names and signatures are (checked by the contract test); the behaviour is (inspected only; the contract test cannot check this).
2. `ZNetView.ClaimOwnership`. If the caller is not the owner, it calls `ZDO.SetOwner` with the local session id. The claim is unilateral: it makes no in-use check and no access check. Vanilla calls it only from the take-all response, after a grant, and never on the open or stack path. (inspected only; the contract test cannot check this)
3. RPC registration. `Container.Awake` registers `RPC_RequestOpen`, `RPC_OpenResponse`, `RPC_RequestStack`, `RPC_StackResponse`, `RPC_RequestTakeAll`, `RPC_TakeAllResponse` and `RPC_Discovered` on the chest's `ZNetView`, each wire string equal to its handler name. The misspelling of the open response does not exist in the game; the handler is `RPC_OpenResponse(long uid, bool granted)`, next to `RPC_RequestOpen(long uid, long playerID)`. The four handler methods exist (checked by the contract test); the wire strings and the registration are (inspected only; the contract test cannot check this).
4. Open handshake. `Container.Interact` returns false on hold. If `Container.m_checkGuardStone` is set and `PrivateArea.CheckAccess` fails, it returns without sending. If `Container.CheckAccess` fails for the local player id, it shows a message and does not send. Otherwise it invokes `RPC_RequestOpen` routed to the ZDO owner. A non-owner that receives the request returns without any response. The owner answers `RPC_OpenResponse(false)` when the chest (or its wagon) is in use by someone other than the requester, or when `Container.CheckAccess` fails. Otherwise it calls `ZDOMan.ForceSendZDO`, then `ZDO.SetOwner` with the requester, then answers `RPC_OpenResponse(true)`. On the requester, a grant calls `InventoryGui.Show` for the chest, and a refusal shows an "in use" message for both "in use" and "not yours". (inspected only; the contract test cannot check this)
5. Stack handshake. `RPC_RequestStack` applies the same gates and the same `ZDOMan.ForceSendZDO` then `ZDO.SetOwner` grant. On grant, `RPC_StackResponse` stacks the chest into the local player's inventory, which moves items. Take-all follows the same pattern and adds `ZNetView.ClaimOwnership`. (inspected only; the contract test cannot check this)
6. Where inventory contents are serialized. `Container.Save` writes `Inventory.Save` output into the ZDO as a byte array under `ZDOVars.s_items`, and records `ZDO.get_DataRevision`. `Container.OnContainerChanged` is wired to the inventory change event and saves only when the chest is not loading and `ZNetView.IsOwner` is true, so a non-owner's edits are never written. `Container.Load` returns early when the data revision is unchanged or the chest is in use; otherwise it reads `ZDO.GetByteArray` for `ZDOVars.s_items`. `Container.CheckForChanges` runs repeatedly, calling `Container.Load` then `Container.UpdateUseVisual`. The names and signatures are (checked by the contract test); the behaviour is (inspected only; the contract test cannot check this).
7. In-use state. `Container.m_inUse` is a local flag and `Container.IsInUse` returns it. `Container.SetInUse` does nothing unless the caller is the owner. `Container.UpdateUseVisual` runs on every peer, and only the owner writes `ZDOVars.s_inUse` as 1 or 0; every peer reads it with `ZDO.GetInt`. The vanilla UI keeps the flag set from `InventoryGui.UpdateContainer` every frame while it owns the open chest, and `InventoryGui.Hide` and `InventoryGui.CloseContainer` clear it. The authoritative answer is the owner's local flag, enforced in the request handlers. The remotely observable answer is the ZDO int `InUse` equal to 1. Because opening transfers ownership, the player with a chest open is its owner. To name that player, match `ZNet.GetPlayerList` entries whose `m_characterID` user id (`ZDOID.get_UserID`) equals `ZDO.GetOwner`, and read `m_name`. `ZNet.GetPeer` is not usable on clients, which only know the server peer. `Vagon.InUse` is true when the cart's container is in use or the cart is attached. (inspected only; the contract test cannot check this)
8. Access rules. `Container.CheckAccess` switches on the `PrivacySetting` stored in `Container.m_privacy`. `Private` allows only the piece creator (`Piece.GetCreator` equals the player id). `Group` always denies in this build. `Public` always allows. The enum literals and the `CheckAccess` signature are (checked by the contract test); the branch behaviour is (inspected only; the contract test cannot check this). A ward is a `PrivateArea`: the static `PrivateArea.CheckAccess(point, radius, flash, wardCheck)` is true when no enabled ward containing the point denies local access, and local access means the player is the ward creator or is permitted. The ward check runs only on the requester, in `Container.Interact`, and only when `Container.m_checkGuardStone` is set. The owner's `RPC_RequestOpen` checks privacy but not wards. (inspected only; the contract test cannot check this)
9. Disconnect and release. On the server only (`ZNet.IsServer`), `ZDOMan.ReleaseZDOS` runs about every 2 seconds and calls `ZDOMan.ReleaseNearbyZDOS` for the server and each connected peer. A persistent ZDO whose owner is not in a peer's active area (`ZDOMan.IsInPeerActiveArea`; false for a disconnected owner, since `ZNet.GetPeer` is then null) is given to that nearby peer with `ZDO.SetOwner`. A peer's own ZDO outside its active area is set to owner 0. `ZDOMan.RemovePeer` handles the disconnect itself. After a disconnect the server therefore hands the chest to a nearby peer within about 2 seconds, and the new owner's `Container.UpdateUseVisual` rewrites `InUse` from its own flag (false), so a stale flag clears itself. Ownership also moves with no disconnect when the owner walks away. (inspected only; the contract test cannot check this)

**Relied-on game names.**

- `Container.RPC_RequestOpen`
- `Container.RPC_OpenResponse`
- `Container.RPC_RequestStack`
- `Container.RPC_StackResponse`
- `Container.Interact`
- `Container.CheckAccess`
- `Container.IsInUse`
- `Container.SetInUse`
- `Container.UpdateUseVisual`
- `Container.CheckForChanges`
- `Container.OnContainerChanged`
- `Container.Save`
- `Container.Load`
- `Container.m_nview`
- `Container.m_inventory`
- `Container.m_inUse`
- `Container.m_privacy`
- `Container.m_checkGuardStone`
- `Container.m_piece`
- `Container.m_wagon`
- `ZNetView.IsValid`
- `ZNetView.IsOwner`
- `ZNetView.ClaimOwnership`
- `ZNetView.GetZDO`
- `ZNetView.InvokeRPC`
- `ZDO.IsOwner`
- `ZDO.GetOwner`
- `ZDO.SetOwner`
- `ZDO.get_DataRevision`
- `ZDO.GetInt`
- `ZDO.GetByteArray`
- `ZDO.Set`
- `ZDO.m_uid`
- `ZDOID.get_UserID`
- `ZDOVars.s_inUse`
- `ZDOVars.s_items`
- `ZDOMan.GetSessionID`
- `ZDOMan.ForceSendZDO`
- `ZDOMan.RemovePeer`
- `ZDOMan.ReleaseZDOS`
- `ZDOMan.ReleaseNearbyZDOS`
- `ZDOMan.IsInPeerActiveArea`
- `ZNet.GetUID`
- `ZNet.GetPeer`
- `ZNet.IsServer`
- `ZNet.GetPlayerList`
- `PrivateArea.CheckAccess`
- `Piece.GetCreator`
- `Vagon.InUse`
- `Inventory.Save`
- `InventoryGui.Show`
- `InventoryGui.Hide`
- `InventoryGui.CloseContainer`
- `InventoryGui.UpdateContainer`
- `InventoryGui.IsContainerOpen`
- `InventoryGui.m_currentContainer`
- `Game.GetPlayerProfile`
- `PlayerProfile.GetPlayerID`

**Inspected only.** RPC wire strings and `ZNetView.Register` (its generic overloads cannot be pinned by the contract test), the branch logic of facts 2 to 9, the 2 second release cadence, and the fact that release runs only on the server.

## Decision

Reserve a chest by reusing the vanilla open handshake, `RPC_RequestOpen` then `RPC_OpenResponse`, and suppress the vanilla UI on the requester.

Do not use the following:

- The stack or take-all handshakes, because their grant path moves items.
- A bare `ZNetView.ClaimOwnership`, because it bypasses the owner's in-use and access gates and races the current owner.
- A new mod RPC. The vanilla owner-side gates are the access rule, and they work when the owner runs the mod in a different state.

Requester pre-checks, with no network traffic:

1. `ZNetView.IsValid` is true.
2. When `Container.m_checkGuardStone` is set, `PrivateArea.CheckAccess(pos, 0, false, false)`. Passing `flash: false` deliberately differs from `Container.Interact` so background scans never flash wards.
3. `Container.CheckAccess` for `Game.GetPlayerProfile().GetPlayerID()`.
4. The in-use observation below.

A failure maps to an access-denied or in-use-by-other result and no request is sent.

Request: invoke `RPC_RequestOpen` with the player id through the owner-routed `ZNetView.InvokeRPC` overload and mark the reservation Pending. Core times Pending with its clock and `ReserveTimeoutMs` (default 1500), so a timeout is always bounded.

No vanilla UI: a later story adds a Harmony prefix on `Container.RPC_OpenResponse`. While a BaseStorage reservation for that chest is Pending, or inside the late-response window, the prefix consumes the response and skips the original, so `InventoryGui.Show` is never called and no "in use" message appears. Otherwise vanilla runs untouched.

Grant: `granted == true` alone is not enough. The owner calls `ZDOMan.ForceSendZDO` before `ZDO.SetOwner`, and ZDO sync is separate from the routed RPC, so the reservation stays Pending until `ZNetView.IsOwner` is true. Then it calls `Container.SetInUse(true)`, which publishes `InUse = 1` so other requesters are refused, and the status becomes Granted. If ownership never arrives before the timeout, the timeout applies. A `granted == false` response is Denied; vanilla does not distinguish "in use" from "not yours".

Write: through `Container.m_inventory`. The change event reaches `Container.OnContainerChanged`, which calls `Container.Save` because the requester is now owner, and the `ZDOVars.s_items` blob syncs to peers.

Release: `Container.SetInUse(false)`. Ownership is not handed back, which matches vanilla open-then-close; the server's release pass rebalances it.

Self-open: if `InventoryGui.m_currentContainer` is the chest, the local player already owns it with it in use. The mod may write but never calls `SetInUse` on it. The owner-side in-use gate is skipped when the requester is the owner, so a self-request is always granted when access passes, and this local check is required.

Lost: while Pending or Granted, the reservation is Lost if the `ZNetView` becomes invalid; while Granted, it is also Lost if `ZNetView.IsOwner` turns false.

Timeout: a Pending reservation older than `ReserveTimeoutMs` is released by Core and nothing is consumed. This covers a request that reached a non-owner (no response is ever sent), the owner disconnecting mid-handshake, and a grant whose ownership never arrives.

Late responses: after a timeout the chest stays in a late-response set for twice `ReserveTimeoutMs`. A late `RPC_OpenResponse` inside that window is swallowed. A late grant leaves the requester as owner without in-use set, the same end state as a vanilla open-then-close. The player's own `Container.Interact` on that chest clears the entry, so a real open is never swallowed.

Disconnect:

- If the holder disconnects, nothing runs on its side. The server reassigns ownership within about 2 seconds and the new owner clears `InUse`.
- Other modded peers treat `InUse == 1` as in use by another only when the owner is not the local session and appears in `ZNet.GetPlayerList`. An absent holder never blocks them.
- If the owner disconnects while Pending there is no response, so the timeout applies, and a retry goes to the new owner.
- If the owner disconnects while Granted, ownership is lost, the status becomes Lost, and Core stops the plan.

In-use observation: a chest is in use by another when `ZDO.GetInt` of `ZDOVars.s_inUse` is 1, the owner differs from `ZDOMan.GetSessionID`, and the owner is present in `ZNet.GetPlayerList`. The holder's name is the `m_name` of the entry whose character id user id equals the owner. The mod never calls `InventoryGui.Hide` or `InventoryGui.CloseContainer` for another player's chest and never forces a chest closed.

## Consequences

- Each reservation state (requesting, granted, writing, releasing, failed) maps to one verified game call, so the later reservation story is a translation of this Decision.
- Reservation is not transactional across chests. ZDO sync is per ZDO, so a holder disconnecting mid-plan can leave a half-applied plan. The add-first-then-remove rule and the conservation invariant remain the planner's job.
- The roughly 2 second server release cadence is vanilla behaviour outside the mod's control. The `ReserveTimeoutMs` expiry is met by the remaining peers' own request timeouts and the player-list presence rule, not by changing vanilla ownership.
- A game update that renames or re-signs a relied-on name fails the contract test. Behaviour drift (branch logic, call order, release cadence) is not caught and needs a re-inspection after game updates.
- Ruled out: the stack and take-all handshakes (side effects), a bare `ZNetView.ClaimOwnership` (no gates, races), a custom RPC registered with `ZNetView.Register` (duplicates vanilla gates and cannot be contract-checked), and closing other players' UIs.
