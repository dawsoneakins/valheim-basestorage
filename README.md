# Don's QOL

![Don's QOL](package/icon.png)

Don's QOL, formerly BaseStorage, is a BepInEx 5 mod for Valheim: chests near a crafting station form a base, crafting and building draw from every chest in that base, and one button organizes items into chests by category. The full specification, acceptance criteria and settings table live in [docs/SPEC.md](docs/SPEC.md); this file is for people building the mod. Only the code, the settings file (`dawso.valheim.basestorage.cfg`), the categories file and the saved chest data keep the BaseStorage name, so updating keeps your settings and chest categories.

## Install

On Windows, download `Dons-QOL-Setup.bat` from the latest release (https://github.com/dawsoneakins/valheim-dons-qol/releases/latest) and double-click it. It finds Valheim, installs BepInEx and the mod, checks both downloads, and never touches your saves. Close Valheim first.

To update, download the latest `Dons-QOL-Setup.bat` and run it the same way. It replaces the plugin and keeps your settings, your favourites and your saves. Everyone playing together needs the same version, so update as a group.

## Repository layout

```
BaseStorage.sln                        the solution the gates build
Directory.Build.props                  ValheimDir resolution, LangVersion, TreatWarningsAsErrors (Release)
Version.props                          the only place the mod version is written
src/BaseStorage.Core/                  netstandard2.0. Pure logic, no references to Unity, Valheim or BepInEx.
src/BaseStorage.Plugin/                net462. BepInEx plugin: Harmony patches, adapters to game types, UI, RPC.
src/Shared/PatchTargetNames.cs         game type, method and field names the plugin depends on (linked into Plugin and PatchTargets.Tests)
tests/BaseStorage.Core.Tests/          net10.0, xunit. All logic tests and the repo source-scan tests.
tests/BaseStorage.PatchTargets.Tests/  net10.0, xunit. Patch-target contract tests (AC-1.4).
docs/                                  SPEC.md, PLAYTEST.md (manual checklist), adr/ (design decisions)
package/                               Thunderstore package sources (manifest.json, icon.png, the player README.md)
scripts/smoke-valheim.ps1              headless load check against the local game; never run it while Valheim is open
CHANGELOG.md                           player-facing changes, copied into the Thunderstore zip
```

## Prerequisites

- The .NET 10 SDK (`dotnet --version` prints 10.x). Every project builds with it, including the net462 plugin.
- Valheim installed. The Plugin project references `assembly_valheim.dll` and the Unity assemblies from `<ValheimDir>\valheim_Data\Managed\` and fails with `BS1001: Valheim not found` when they are missing. Game assemblies are never committed.
- BepInExPack_Valheim installed into the game folder, only for in-game testing. The build, test and format gates do not need it.

## Setting VALHEIM_DIR

`Directory.Build.props` resolves the MSBuild property `ValheimDir` in this order (highest first):

1. A gitignored `Directory.Build.user.props` next to the solution. It is imported last, so its value wins over the environment variable:

   ```xml
   <Project>
     <PropertyGroup>
       <ValheimDir>D:\Games\Steam\steamapps\common\Valheim</ValheimDir>
     </PropertyGroup>
   </Project>
   ```

2. The `VALHEIM_DIR` environment variable, used when `ValheimDir` is not set yet. PowerShell: `$env:VALHEIM_DIR = 'D:\Games\Steam\steamapps\common\Valheim'`, or set it permanently in the Windows environment settings.
3. The default `C:\Program Files (x86)\Steam\steamapps\common\Valheim`.

A `-p:ValheimDir=<dir>` argument on the command line overrides all three for that one command. The patch-target tests read `ValheimDir` at build time, so rebuild after changing it.

## Build, test and format

The three gate commands run from the repo root and must exit 0 (spec section 4):

```
dotnet build BaseStorage.sln -c Release --nologo -warnaserror
dotnet test  BaseStorage.sln -c Release --nologo
dotnet format BaseStorage.sln --verify-no-changes
```

Release treats warnings as errors and every public member needs an XML doc comment. `dotnet format BaseStorage.sln` (without `--verify-no-changes`) fixes formatting. Files use LF line endings (`.gitattributes`). Building never writes into the game folder. The test gate includes the repo source-scan tests under `tests/BaseStorage.Core.Tests/Repo/`, which enforce the rules in this section; read a failing one as the rulebook.

## Core coverage (AC-9.1)

Core line coverage must stay at or above 85%. Measure it with coverlet from the repo root:

```
dotnet test tests/BaseStorage.Core.Tests -c Release --nologo --collect:"XPlat Code Coverage" --results-directory TestResults/coverage
```

The report is `TestResults/coverage/<run-guid>/coverage.cobertura.xml` (a new `<run-guid>` folder per run). Read the `line-rate` attribute of `<package name="BaseStorage.Core"`; multiply by 100 for the percentage. The gates do not enforce this number; the verify phase checks it.

## Patch-target contract tests (AC-1.4)

`tests/BaseStorage.PatchTargets.Tests` opens `assembly_valheim.dll` from `ValheimDir` read-only (`MetadataLoadContext`, the game is not running) and checks that every type, method overload, field and RPC name in `src/Shared/PatchTargetNames.cs` exists with the expected shape, and that the Harmony attributes in the built plugin agree with those names. Run only this project with:

```
dotnet test tests/BaseStorage.PatchTargets.Tests -c Release --nologo
```

When the game is not installed the game-backed tests are skipped, not failed, with `BST2002: Skipped: ValheimDir '<dir>' does not exist.` or `BST2003` naming the missing `assembly_valheim.dll`; `BST2004` means the plugin has not been built yet (run the build gate first).

After a game update:

1. Run the build gate, so the plugin compiles against the new `assembly_valheim.dll`.
2. Run the command above. A failure names the missing member and the constant that declares it, for example `BST2011: Patch target method 'Container.RPC_RequestOpen' (PatchTargetNames.Container.RpcRequestOpen) not found in '...'`; `BST2012` lists the overloads the new build declares.
3. Fix the constant in `src/Shared/PatchTargetNames.cs` and the patch that uses it, re-run the tests, and record the change in an ADR under `docs/adr/` (spec AC-9.4: every patch target is in the contract list).

## Deploy to the game

```
dotnet build BaseStorage.sln -c Release --nologo -p:DeployToGame=true
```

The opt-in `DeployToGame` target copies `Dons-QOL.dll` and `Dons-QOL.Core.dll` to `<ValheimDir>\BepInEx\plugins\Dons-QOL\`, and only when `<ValheimDir>\BepInEx` already exists (otherwise it logs `BS1002: DeployToGame skipped`). The gates never pass this property. Do not deploy while the game is running; BepInEx loads plugins at start. The manual checklist for what to test in game is [docs/PLAYTEST.md](docs/PLAYTEST.md).

## Thunderstore package

```
dotnet build BaseStorage.sln -c Release --nologo -p:PackageThunderstore=true
```

The opt-in `PackageThunderstore` target writes `dist/Dons-QOL-<version>.zip` (`dist/` is gitignored; `-p:BaseStorageDistDir=<dir>` moves it) and logs `BS1004: PackageThunderstore wrote '<path>'`. The zip holds exactly six entries: `manifest.json` (version from `Version.props`, dependency on `denikson-BepInExPack_Valheim`), `icon.png`, `README.md` (the player README from `package/`), `CHANGELOG.md`, `plugins/Dons-QOL.dll` and `plugins/Dons-QOL.Core.dll`. Friends install it by importing the zip into r2modman. Plain builds and the gates never produce it.

## Reading the BepInEx log

BepInEx writes `<ValheimDir>\BepInEx\LogOutput.log`, overwritten on every game start. The mod's lines are tagged `[Info   :BaseStorage]`, `[Warning:BaseStorage]` or `[Debug  :BaseStorage]`. At Info it writes only:

- `Dons-QOL <version> loaded` once at start (the load check in `docs/PLAYTEST.md`; a `failed to apply` or `failed to load` line names a patch that did not apply);
- `Wrote default category file '...\BepInEx\config\BaseStorage.categories.txt' with <n> item(s)` when the categories file is created;
- config sync lines in multiplayer and one summary line per Organize or Deposit.

Per-move detail, patch applied lines and stack traces are Debug and need two switches: add `Debug` to `LogLevels` under `[Logging.Disk]` in `BepInEx\config\BepInEx.cfg` (BepInEx filters Debug out otherwise), and set `DebugLogging = true` in the `[Debug]` section of `BepInEx\config\dawso.valheim.basestorage.cfg` or through the in-game ConfigurationManager (F1). See "Debug logging" in [docs/PLAYTEST.md](docs/PLAYTEST.md).

## More

- [docs/SPEC.md](docs/SPEC.md): the specification and acceptance criteria.
- [docs/PLAYTEST.md](docs/PLAYTEST.md): the manual in-game checklist, one section per feature.
- [docs/adr/](docs/adr/): design decisions that affect behaviour or data.
- [CHANGELOG.md](CHANGELOG.md): player-facing changes under `[Unreleased]` until a release.
- [package/README.md](package/README.md): the player README shipped in the Thunderstore zip.
