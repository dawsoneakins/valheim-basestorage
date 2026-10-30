# BaseStorage (working title): Valheim mod spec and acceptance criteria, v1

BaseStorage is a BepInEx mod for Valheim with two features:

1. **Craft from the base.** Every chest in your base counts as if it were in your inventory when you craft, upgrade or build.
2. **Organize the base.** One button at any crafting station (workbench, forge and so on) sorts the whole base so that like items live together. Each chest belongs to a category (metals, wood, food and so on), and an Organize moves every item into a chest of its category.

The mod targets a small group that plays on a game hosted by one of the players (peer-hosted, "Start server" from the host's game). Every player in the group runs the same mod version.

This document is the source of truth for the program. Every story's acceptance criteria must trace to a numbered AC below or to the autonomy grant in section 9.

## 1. Principles

1. **Items are never lost or duplicated.** This outranks every other requirement. Each item stack moved by the mod is accounted for: the total count of every item type across the player inventory and all touched chests is identical before and after any mod operation. On any failure, the operation stops and leaves items where they are. The mod never drops items on the ground and never deletes them.
2. **Vanilla behaviour wins when in doubt.** When the mod cannot be sure a chest is safe to touch (another player has it open, ownership cannot be obtained, access is denied), it skips that chest and says so. It does not guess.
3. **Feels like part of the game.** Same UI style, same hover-text conventions, short MessageHud notices, no debug spam in normal play.
4. **Uninstall is harmless.** Removing the mod leaves every chest and item intact. Any data the mod stores on chests is ignored by vanilla.
5. **Fail closed in patches.** An exception inside any Harmony patch is caught and logged. The game then falls back to vanilla behaviour for that call. A mod bug must never crash the game or block crafting.

## 2. Users and core jobs

- Primary: the host and 1-5 friends sharing one world and one or more bases.
- Jobs: (a) craft and build without first walking to chests to fetch materials; (b) come back from a trip and put everything away in one action; (c) find things because each kind of item has a known home; (d) never wonder whether the mod ate their silver.

## 3. Target stack (mandatory)

Solution `BaseStorage.sln` at the repo root:

```
src/BaseStorage.Core/          netstandard2.0. Pure logic, NO references to Unity, Valheim or BepInEx.
src/BaseStorage.Plugin/        net462. BepInEx plugin: Harmony patches, adapters to game types, UI, RPC.
tests/BaseStorage.Core.Tests/  net10.0, xunit. All logic tests run here.
tests/BaseStorage.PatchTargets.Tests/  net10.0, xunit. Patch-target contract tests (AC-1.4).
docs/                          this spec, ADRs, PLAYTEST.md, README for players.
package/                       Thunderstore package sources (manifest.json, icon.png, README.md).
```

- **Core vs Plugin split (mandatory).** Every decision the mod makes lives in Core behind small interfaces, so it can be unit tested without the game running. Plugin code is a thin adapter: it reads game state into Core snapshots, calls Core, and applies the results. Decisions here means which chests are in the base, item counts, which chest an item goes to, the move plan, consumption order, conflict handling and protocol state. If a Plugin method holds more than trivial branching, move that logic to Core.
- **Plugin references:**
  - BepInEx 5 (`BepInEx.Core` 5.4.21 from `https://nuget.bepinex.dev/v3/index.json`), which brings HarmonyX.
  - `Microsoft.NETFramework.ReferenceAssemblies`.
  - `BepInEx.AssemblyPublicizer.MSBuild`, with `Publicize="true"` on `assembly_valheim`.
  - Game assemblies by HintPath from `$(ValheimDir)\valheim_Data\Managed\`, all with `Private=false`: `assembly_valheim.dll`, `assembly_utils.dll`, `assembly_guiutils.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`, `UnityEngine.PhysicsModule.dll` and `Unity.TextMeshPro.dll` as needed.
- **`ValheimDir` resolution.** `Directory.Build.props` defaults it to `C:\Program Files (x86)\Steam\steamapps\common\Valheim`. The `VALHEIM_DIR` environment variable overrides it, and so does a gitignored `Directory.Build.user.props`.
- **Never commit game assemblies** or any file from the game folder.
- **No runtime dependencies** beyond BepInEx, HarmonyX and what the game ships. No Jötunn, no ServerSync: config sync and version checks are implemented in the mod (E6). Anything else needs an ADR in `docs/adr/`.
- **Language and style.** C# `LangVersion` latest, nullable enabled in Core, `TreatWarningsAsErrors` in Release. An `.editorconfig` must keep `dotnet format --verify-no-changes` clean.
- **Plugin identity.** GUID `dawso.valheim.basestorage`, name `BaseStorage`, SemVer version starting at `0.1.0`. A single `Version.props` (or equivalent) is the only place the version is written, and the plugin attribute, Thunderstore manifest and network version check all read from it.

## 4. Gate commands (mandatory: the pipeline gates call these)

These run from the repo root and must exit 0 on a clean checkout once the foundation story lands:

```
dotnet build BaseStorage.sln -c Release --nologo -warnaserror   (typecheck gate)
dotnet test  BaseStorage.sln -c Release --nologo                (test gate)
dotnet format BaseStorage.sln --verify-no-changes               (lint gate)
```

Building must never write into the game folder. An opt-in MSBuild target, `-p:DeployToGame=true`, copies `BaseStorage.dll` to `$(ValheimDir)\BepInEx\plugins\BaseStorage\`, and only if that BepInEx folder already exists. Gates never pass it.

## 5. Epics and acceptance criteria

IDs are stable. Reference them from ROADMAP stories as "AC-x.y".

### E1 Foundations

- **AC-1.1 Solution scaffold.** The layout in section 3 exists, and all three gate commands in section 4 exit 0. Core has zero references to Unity, Valheim or BepInEx assemblies; a test asserts this by inspecting Core's referenced assemblies.
- **AC-1.2 Plugin boots.** `BaseStorage.Plugin` builds to `BaseStorage.dll` with a `[BepInPlugin]` entry point. On load it applies all Harmony patches through one `Harmony` instance, logs `BaseStorage <version> loaded` through the BepInEx logger, and reports any patch that fails to apply by name rather than throwing.
- **AC-1.3 Patch safety wrapper.** A shared helper wraps every patch body. An exception is logged once per patch per session, with the stack trace at Debug level, and vanilla behaviour continues. The helper's Core-testable part, a per-key rate-limited error reporter, has unit tests.
- **AC-1.4 Patch-target contract test.** `BaseStorage.PatchTargets.Tests` loads `assembly_valheim.dll` from `$(ValheimDir)` with `System.Reflection.MetadataLoadContext` (read-only, game not running). It asserts that every type, method overload, field and RPC name the plugin depends on exists with the expected signature. The plugin's patch list is the single source for these targets, either by reflection over the attributes or by a shared constants file. When `ValheimDir` is missing, the tests are skipped with a clear message rather than failing. This is the early warning for game updates.
- **AC-1.5 Config.** All user settings are BepInEx `ConfigEntry` values in `BepInEx/config/dawso.valheim.basestorage.cfg`, grouped into sections with descriptions. Section 7 lists the settings and defaults. Settings marked "host-synced" are overridden by the host's values in multiplayer (AC-6.2).
- **AC-1.6 Logging.** Info-level logs only for load, config sync and one summary line per Organize or Deposit. Per-move detail goes to Debug level. A `DebugLogging` setting turns Debug on.

### E2 Base resolution: what counts as "the base"

- **AC-2.1 Station graph.** The base is built from crafting stations, meaning any `CraftingStation` with a build range greater than 0: workbench, forge, black forge, galdr table, artisan table and modded stations that use the vanilla class. Two stations are connected when either one is inside the other's build range, using the same distance rule vanilla uses for its build-range check (record that rule in an ADR). A base is the connected group, found by flood fill, of stations reachable from a starting point. Core implements this as a pure function over station positions and ranges, with tests for: a single station, a chain of 5, two separate bases that do not merge, a chain broken in the middle, overlapping ranges, and stations at different heights.
- **AC-2.2 Base membership.** A container belongs to the base when it is within the build range of at least one station in the base. A container in range of two separate bases belongs to both, and each base's operations may use it.
- **AC-2.3 Eligible containers.** Only player-built, stationary `Container` pieces are eligible. Excluded by default:
  - carts and ships (`ExcludeVehicles`, default true);
  - tombstones;
  - any container the local player cannot open under vanilla rules (private chests owned by others, chests inside a ward the player has no access to);
  - chests flagged **Keep** (AC-4.6).
- **AC-2.4 Starting point.**
  - Crafting or upgrading at a station uses that station's base.
  - Building with the hammer uses the base whose station range contains the player.
  - Organize or Deposit started from a crafting station's panel (AC-4.10) uses that station's base.
  - Organize or Deposit started by an optional hotkey (unbound by default) uses the base containing the player.
  - If the player is in no base, the mod does nothing and shows "Not in a base".
- **AC-2.5 Range cap and cache.**
  - **Size cap.** `MaxBaseStations` (default 64) limits the flood fill. When it is hit, the mod logs once and uses the stations found so far.
  - **Cache.** The station graph and membership are cached per base and rebuilt at most once per `BaseCacheSeconds` (default 2). They are also rebuilt immediately when a station or container is placed or removed nearby.
  - **Container tracking.** The plugin keeps its own registry of loaded containers, added on Awake and removed on destroy, because vanilla has no global container list. A Core test proves the cache is invalidated on these events.
- **AC-2.6 Inspect.** Hovering a chest shows `[In base: N stations]` or `[Not in a base]`, plus its category (AC-4.3) or `[Keep]`. Hovering a station shows the number of chests in its base.

### E3 Craft and build from the base

- **AC-3.1 Availability.**
  - For crafting, upgrading and building, the mod changes the "have the materials" check (`Player.HaveRequirements` and the overloads used for pieces and recipes) and the first-missing-item lookup.
  - Available amount = player inventory + every eligible container in the base, minus containers currently open by another player (AC-5.3).
  - Core computes availability from snapshots and is unit tested, including items whose quality affects the requirement.
- **AC-3.2 Display.** In the crafting panel and the build-piece requirement tooltip, each requirement shows the combined amount. When chests contribute, it uses the format `have/need` with the chest part noted, for example `12 (+40) / 20`. The colour rules match vanilla: red only when the combined amount is short.
- **AC-3.3 Consumption order.** When a craft, upgrade or build is confirmed, the mod takes materials from the player inventory first. It then takes the rest from base chests, nearest chest to the player first, and within a chest the smallest stacks first, so chests empty out rather than fragment. Core produces the consumption plan; tests cover the order, multiple requirement types, and exact-boundary amounts.
- **AC-3.4 Atomic consumption.** Before taking anything, the mod reserves every chest in the plan through the ownership protocol (AC-5.2).
  - If any chest cannot be reserved within `ReserveTimeoutMs` (default 1500), or its contents changed since the plan was made, nothing is consumed. The craft or build is cancelled and the player sees "Materials moved, try again".
  - The item is created or the piece placed only after consumption fully succeeds.
  - Tests prove that a failed reservation consumes nothing, and that the conservation invariant from principle 1 holds.
- **AC-3.5 Upgrades and multi-craft.** Upgrading an item, and any multi-craft amount vanilla offers, use the same availability and consumption path.
- **AC-3.6 Toggle.** `CraftFromBase` (default true) disables E3 entirely. When it is off, behaviour is exactly vanilla.

### E4 Organize the base by category

- **AC-4.1 Category catalog.**
  - **Defaults.** Shipped categories: Metals & Ores, Wood, Stone & Minerals, Hides & Leather, Fibres & Cloth, Raw Food, Cooked Food, Meads & Potions, Seeds & Saplings, Monster Drops, Trophies, Gear (weapons, armour, tools, shields), Ammo, Misc.
  - **Mapping.** Items map to categories by prefab name through `BepInEx/config/BaseStorage.categories.txt`. The file has one line per category (`Metals & Ores: CopperOre, Copper, TinOre, Tin, ...`); blank lines and lines starting with `#` are ignored.
  - **First run.** If the file is missing, the mod writes a complete default file covering every vanilla item that exists in the current game build.
  - **Fallback.** Unmapped items fall back by the game's item type: food consumables go to Cooked Food, trophies to Trophies, weapons, armour, tools and shields to Gear, ammo to Ammo, and everything else to Misc.
  - **Testing.** Core parses the file and resolves categories. Tests cover parsing errors (reported with line numbers, the bad line skipped), duplicates (first one wins, with a warning) and fallbacks.
- **AC-4.2 Category on the chest.** Each chest's assigned category is stored on the chest's networked data under the key `basestorage.cat`, so every player sees the same assignment and it survives restarts. Unassigned chests have no key.
- **AC-4.3 Manual assignment.** The open-chest panel shows a **Category** button labelled with the chest's current value. Left-click cycles forward and right-click cycles back through: Auto (unassigned), then each category, then Keep. Hover text on the chest shows the current value. An optional `CycleCategoryKey` (unbound by default) does the same while hovering a chest. Changing an assignment is a networked write to that chest that goes through the ownership protocol.
- **AC-4.4 Auto-assignment.** At the start of an Organize, each unassigned chest is resolved in order:
  1. If one category has at least `AutoAssignThresholdPct` (default 50%) of the chest's stacks, the chest is assigned that category. Ties go to the category listed first in the catalog.
  2. Otherwise a chest that holds items stays unassigned. It is a source only and is emptied into category chests.
  3. Empty unassigned chests form an overflow pool. They are claimed one at a time, nearest the category's existing chests first, when a category runs out of space.
  Core implements this; tests cover majority, tie, empty and mixed chests.
- **AC-4.5 Organize plan.** Core turns a base snapshot (chests, contents, sizes, assignments, Keep flags) into a move plan:
  1. Every stack not in a chest of its own category moves to one.
  2. Partial stacks merge into existing partial stacks first.
  3. When all of a category's chests are full, the next overflow chest is claimed for it.
  4. If no space remains anywhere, items stay where they are and the plan reports them as "no room".
  5. After moves, each category chest is compacted: stacks merged, then ordered by item name.
  Planning a 60-chest base with 32 slots each takes under 50 ms in the Core test suite. A property-based or randomized test checks, over at least 1,000 random bases, that the plan conserves every item type exactly, never exceeds stack sizes or chest capacity, and never touches Keep chests.
- **AC-4.6 Keep chests.** A chest set to Keep is never a source or a destination for Organize or Deposit. It still counts for craft-from-base unless `KeepChestsCraftable` is false (default true).
- **AC-4.7 Preview and confirm.**
  - Organize opens a summary panel before anything moves, for example:
    > Organize base: 214 stacks to move across 23 chests. 2 chests will be claimed for Metals & Ores. 4 stacks have no room. 1 chest skipped (in use by Erik).
  - The player confirms or cancels. `OrganizeSkipPreview` (default false) skips the panel.
- **AC-4.8 Execution.**
  - The plan runs chest by chest through the ownership protocol (AC-5.2). Each move adds to the destination first, confirms it succeeded, and only then removes from the source. A failed move stops the remaining plan.
  - At the end, one MessageHud line summarizes what moved, what was skipped and why, and what had no room.
  - Execution is spread over frames, with at most `MovesPerFrame` (default 20), so the game does not hitch.
  - Execution tests use a fake container layer in Core, with injected faults (reservation refused, chest changed mid-run, destination full) and the conservation invariant asserted after every case.
- **AC-4.9 Deposit (put away inventory).** The **Put away inventory** button on a crafting station's panel (AC-4.10), or the optional `DepositKey` (unbound by default), moves items from the player inventory into the base's category chests using the same planner. It never touches:
  - the hotbar (row 1);
  - equipped items;
  - items flagged favourite by the mod;
  - anything listed in the `DepositNeverItems` setting (default: food you are carrying, meads, arrows and bolts, the hammer, hoe, cultivator, and the active fishing rod and bait).
  A preview is optional (`DepositPreview`, default false). Mod favourites toggle with Alt+click on an inventory slot and are stored in the player profile's custom data.
- **AC-4.10 Station is the entry point.**
  - **Buttons.** When the player uses any crafting station that belongs to a base, the crafting panel shows two buttons in vanilla style: **Organize base** and **Put away inventory**. They sit next to the existing craft controls and do not cover recipe list, requirements or craft button at any supported resolution (1280x720 to 3840x2160, UI scale 0.8 to 1.2).
  - **Base.** Both act on that station's base (AC-2.4). Near the buttons, a short status line shows the base size: `Base: 6 stations, 41 chests`.
  - **Disabled state.** Both buttons are disabled, with a tooltip explaining why, when an Organize is already running in this base (AC-5.4) or the base has no eligible chests.
  - **Settings.** `StationButtons` (default true, local) hides them. The optional `OrganizeKey` hotkey is unbound by default. Hotkeys are a convenience, not the primary path.
  - **Testing.** The button layout logic (position, enabled state, tooltip text) is computed in Core from panel and base state, and unit tested.

### E5 Multiplayer safety (peer-hosted)

- **AC-5.1 Ownership model, documented.** An ADR describes the Valheim rules the mod relies on, verified against the current `assembly_valheim`:
  - networked-object ownership;
  - how `Container` requests access (`RPC_RequestOpen` / `RPC_OpenRespons`, `RPC_RequestStack` / `RPC_StackResponse`, `ZNetView.ClaimOwnership`);
  - where inventory contents are serialized.
  The protocol in AC-5.2 is designed from that ADR.
- **AC-5.2 Reservation protocol.**
  - **Reserving.** Before writing to any chest it does not own, the mod obtains ownership using the same request/response handshake vanilla uses to open a chest. It marks the chest in use for the duration and releases it afterwards.
  - **Scope.** Reservations are per chest and short-lived. If the requester disconnects, they expire after `ReserveTimeoutMs`.
  - **Testing.** Core models the protocol as a state machine (requesting, granted, writing, releasing, failed). Tests cover grant, deny, timeout, owner disconnect mid-operation, and the chest becoming in use by another player mid-operation.
- **AC-5.3 In-use chests.** A chest another player currently has open is excluded from craft availability, consumption, Organize and Deposit. Organize lists it as skipped by player name. The mod never forces a chest closed.
- **AC-5.4 One Organize per base.** At most one Organize or Deposit runs per base at a time. A lock key is stored on the lowest-ID station in the base, with holder and expiry, and expires after `OrganizeLockSeconds` (default 30) if the holder vanishes. A second player trying to start one sees "Organize in progress by <name>".
- **AC-5.5 Conservation under concurrency.** A Core simulation test runs two to three simulated peers performing interleaved crafts, Organizes and Deposits against a shared fake world for at least 500 random schedules, then asserts the conservation invariant.

### E6 Version and config agreement

- **AC-6.1 Version check.** On connect, peers exchange the mod's version over a registered RPC.
  - The host refuses a client with a different version or without the mod, using vanilla's connection-error path. Both sides then see "BaseStorage version mismatch: host X, you Y".
  - `RequireSameVersion` (default true, host-synced) controls this. Core holds the comparison logic and its tests.
- **AC-6.2 Host-synced config.** After the version check, the host sends its host-synced settings and its category file. Clients use them for the session without overwriting their local `.cfg`. A client's local values return when they play single-player or host themselves. Payload serialization lives in Core and is round-trip tested.
- **AC-6.3 Single-player parity.** In single-player, or when the local player is the host, everything works without any network handshake.

### E7 Packaging, docs and playtest

- **AC-7.1 Thunderstore package.** An opt-in target or script (`dotnet build -c Release -p:PackageThunderstore=true`) produces `dist/BaseStorage-<version>.zip` containing `manifest.json` (name, version, description, `BepInEx-BepInExPack_Valheim` dependency), `icon.png` (256×256), `README.md`, `CHANGELOG.md` and `plugins/BaseStorage.dll`. This lets friends install it from r2modman by importing the zip.
- **AC-7.2 Player README.** `package/README.md` covers:
  - what the mod does: set chest categories from the chest panel, then press Organize base or Put away inventory at any workbench;
  - installation for host and friends (all players need the same version);
  - every config setting;
  - the categories file format;
  - known limits (section 8);
  - how to uninstall.
- **AC-7.3 Playtest checklist.** `docs/PLAYTEST.md` is a numbered manual test script, one section per AC from E2-E6, with exact steps and expected results. It covers solo, and host plus one friend. It is the acceptance step a human runs in the real game, because the pipeline cannot run Valheim. Each story that adds in-game behaviour adds or updates its checklist entries.
- **AC-7.4 Developer README.** The root `README.md` covers:
  - prerequisites: the .NET 10 SDK, the game installed, BepInExPack_Valheim installed for in-game testing;
  - how to set `VALHEIM_DIR`;
  - build, test and deploy-to-game commands;
  - how to read the BepInEx log;
  - how to re-run AC-1.4 after a game update.

### E8 UX polish

- **AC-8.1 Feedback.** Every user action ends in one short MessageHud line. Text is localizable through a single strings table, English only in v1.
- **AC-8.2 Optional hotkeys.** All hotkeys are BepInEx `KeyboardShortcut` settings, unbound by default. Gamepad: the station and chest-panel buttons are clickable with the gamepad cursor where vanilla allows; no dedicated gamepad binding in v1, recorded in the known limits.
- **AC-8.3 Hover text stays short.** The mod adds at most one line to chest and station hover text.

### E9 Quality bars that apply to every story

- **AC-9.1** Every Core change ships with unit tests. Coverage of Core stays at or above 85% line coverage (report via coverlet; the gate does not have to enforce it, but the verify phase checks it).
- **AC-9.2** No Plugin code allocates per frame in Update loops beyond what is needed. Base resolution and planning never run every frame (AC-2.5).
- **AC-9.3** Each story that touches in-game behaviour updates `docs/PLAYTEST.md` and, if user-facing, `package/README.md` and `CHANGELOG.md`.
- **AC-9.4** Each story that adds a patch target adds it to the AC-1.4 contract list.

## 6. Non-functional requirements

- **Performance:** Organize of a 60-chest base completes in under 10 seconds of game time on the host. No frame takes over 8 ms of mod work.
- **Compatibility:** the current Steam build of Valheim installed on the build machine, and BepInExPack_Valheim 5.4.23xx. Other popular inventory mods are not a v1 requirement. Any modded chest that inherits vanilla `Container` should just work.
- **Data:** the only persistent data is `basestorage.*` keys on chest and station networked data, plus mod favourites in the player profile's custom data.

## 7. Settings (BepInEx config)

| Section | Key | Default | Host-synced |
|---|---|---|---|
| General | CraftFromBase | true | yes |
| General | ExcludeVehicles | true | yes |
| General | KeepChestsCraftable | true | yes |
| General | MaxBaseStations | 64 | yes |
| General | BaseCacheSeconds | 2 | no |
| General | RequireSameVersion | true | yes |
| Organize | AutoAssignThresholdPct | 50 | yes |
| Organize | OrganizeSkipPreview | false | no |
| Organize | DepositPreview | false | no |
| Organize | DepositNeverItems | (list in AC-4.9) | no |
| Organize | MovesPerFrame | 20 | no |
| Organize | OrganizeLockSeconds | 30 | yes |
| Network | ReserveTimeoutMs | 1500 | yes |
| General | StationButtons | true | no |
| Keys | CycleCategoryKey | (unbound) | no |
| Keys | OrganizeKey | (unbound) | no |
| Keys | DepositKey | (unbound) | no |
| Debug | DebugLogging | false | no |

## 8. Out of scope for v1 (known limits)

- Console and crossplay players (they cannot run mods).
- Dedicated servers. It may work, but v1 is verified only on peer-hosted games. Dedicated-server support is a possible follow-up.
- Feeding smelters, kilns or fires from chests (planned for v2, section 11).
- Carts and ships as storage (excluded by default).
- Item search across the base (planned for v2, section 11), chest renaming, and visual chest labels.
- Gamepad-specific bindings.
- A dedicated buildable "sorting table" piece (v1 uses the existing crafting stations).
- Compatibility shims for other storage or inventory mods.

## 9. Autonomy grant to pipemason

After all must-have ACs in E1-E7 are delivered, the program may add stories it judges useful from this list, each marked `origin: pipemason-proposed` in ROADMAP.md and summarised in `docs/ADDED-BY-PIPEMASON.md`:
- a per-chest Sort button;
- restocking food and meads from the base;
- a short coloured category label on chest hover;
- a statistics line ("Base: 41 chests, 78% full");
- a console command `basestorage organize`;
- export and import of category assignments.

Guardrails:
- Never weaken principle 1 (conservation).
- No new runtime dependencies.
- No writes to the game folder outside the opt-in deploy target.
- No feature that requires the server to run something clients don't.
- Prefer the simpler behaviour and record an ADR instead of escalating, except where data or items could be lost.

## 10. Build-machine constraints (read before planning verification)

- Windows 11, .NET SDK 10.0.113 on PATH. Valheim is installed at `C:\Program Files (x86)\Steam\steamapps\common\Valheim`. That folder is read-only for the pipeline except through the opt-in deploy target, and BepInEx is NOT installed there yet. No git remote, so do not push.
- The game cannot be launched or driven by the pipeline. Verification is unit tests, the patch-target contract test (AC-1.4), a warning-free Release build, and `dotnet format`. In-game behaviour is verified later by a human with `docs/PLAYTEST.md`. This is why AC-1.4, AC-4.5, AC-4.8 and AC-5.5 carry so much weight.
- A smoke build was verified on 2026-10-25: a net462 class library referencing `BepInEx.Core` 5.4.21 from the BepInEx NuGet feed, `Microsoft.NETFramework.ReferenceAssemblies` 1.0.3, `BepInEx.AssemblyPublicizer.MSBuild` 0.4.2 and the game's `assembly_valheim.dll` with Publicize builds cleanly. A Harmony patch on `Player.HaveRequirements(Piece.Requirement[], bool, int, int)` compiles against the installed build.
- The repo uses LF line endings (`.gitattributes`). Normalise CRLF in any test that matches text, and use `Path.Combine` in filesystem code.
- Write nothing outside the repo except the OS temp directory.

## 11. Planned for v2 (do not build in v1, but do not paint v1 into a corner)

These are confirmed for the next version. v1 stories must not implement them, and must not make them harder: base resolution, the container registry, availability, the reservation protocol and the planner should be reusable for them without restructuring.

- **Feed stations from the base.** Smelters, kilns, blast furnaces, charcoal kilns, spinning wheels, windmills, the eitr refinery, and optionally fires and torches pull fuel and ore from base chests of the matching category. They use the same reservation protocol and conservation rule as Organize, with per-station on/off and a minimum keep amount per item.
- **Search the base.** A search box on the station panel finds an item across all base chests, lists the chests and counts, and highlights the matching chests in the world for a few seconds. It optionally takes items straight into the player inventory through the reservation protocol.

## 12. v0.2

`docs/SPEC-v0.2.md` extends this spec. It replaces AC-4.4 rule 2 (mixed chests get a category), extends AC-4.5 (planner retry passes) and AC-4.10 (the Base Storage menu is a second entry point).
