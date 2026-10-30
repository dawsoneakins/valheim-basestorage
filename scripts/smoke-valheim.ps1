# Runtime smoke check for BaseStorage: build the plugin, deploy it into the local
# Valheim + BepInEx install, start the game headless and confirm BepInEx loads it.
# Exit 0 = loaded cleanly, 1 = failed (reason printed), 2 = toolchain missing.
# Used as commands.game-windows.run in .pipeline/config.yml.
param(
  [int]$TimeoutSec = 180
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$valheimDir = if ($env:VALHEIM_DIR) { $env:VALHEIM_DIR } else { 'C:\Program Files (x86)\Steam\steamapps\common\Valheim' }
$exe = Join-Path $valheimDir 'valheim.exe'
$bepCore = Join-Path $valheimDir 'BepInEx\core\BepInEx.dll'
$log = Join-Path $valheimDir 'BepInEx\LogOutput.log'
$pluginDir = Join-Path $valheimDir 'BepInEx\plugins\BaseStorage'

function Fail([int]$code, [string]$msg) { Write-Host "SMOKE FAIL: $msg"; exit $code }

if (-not (Test-Path $exe)) { Fail 2 "valheim.exe not found at $valheimDir (set VALHEIM_DIR)" }
if (-not (Test-Path $bepCore)) { Fail 2 "BepInEx is not installed in $valheimDir (install BepInExPack_Valheim)" }
if (Get-Process -Name valheim -ErrorAction SilentlyContinue) { Fail 1 'Valheim is already running; close it and re-run (the smoke never touches a game you are playing)' }

Write-Host 'Building BaseStorage.Plugin (Release)...'
& dotnet build (Join-Path $repo 'src\BaseStorage.Plugin\BaseStorage.Plugin.csproj') -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { Fail 1 'plugin build failed' }
$dll = Get-ChildItem -Path (Join-Path $repo 'src\BaseStorage.Plugin\bin\Release') -Recurse -Filter BaseStorage.dll | Select-Object -First 1
if (-not $dll) { Fail 1 'BaseStorage.dll not found after build' }

New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Copy-Item -Force $dll.FullName $pluginDir
$coreDll = Join-Path $dll.DirectoryName 'BaseStorage.Core.dll'
if (Test-Path $coreDll) { Copy-Item -Force $coreDll $pluginDir }
Write-Host "Deployed $($dll.FullName) -> $pluginDir"

# A plugin with no [BepInPlugin] class yet (early stories) can only be checked for
# loading without errors; once the attribute exists, BepInEx must report loading it.
$hasPlugin = [IO.File]::ReadAllText($dll.FullName, [Text.Encoding]::ASCII).Contains('BepInPlugin')

if (Test-Path $log) { Remove-Item -Force $log }
$env:SteamAppId = '892970'
$proc = Start-Process -FilePath $exe -WorkingDirectory $valheimDir -ArgumentList '-batchmode', '-nographics' -PassThru -WindowStyle Hidden
Write-Host "Started valheim.exe pid $($proc.Id) headless; waiting up to $TimeoutSec s for the BepInEx chainloader"

$deadline = (Get-Date).AddSeconds($TimeoutSec)
$done = $false
try {
  while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    if ($proc.HasExited) { break }
    try {
      if ((Test-Path $log) -and (Select-String -Path $log -SimpleMatch 'Chainloader startup complete' -Quiet)) { $done = $true; break }
    } catch {
      # The game may hold the log open; retry on the next poll.
    }
  }
} finally {
  if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
}

if (-not (Test-Path $log)) { Fail 1 "BepInEx wrote no log (exit code $($proc.ExitCode)); doorstop may not be loading" }
$text = Get-Content -Raw $log
if (-not $done) { Fail 1 "chainloader did not complete within $TimeoutSec s. Log tail:`n$(($text -split "`n" | Select-Object -Last 15) -join "`n")" }

$errors = $text -split "`n" | Where-Object { $_ -match '\[(Error|Fatal)' -and $_ -match 'BaseStorage' }
if ($errors) { Fail 1 "BaseStorage errors in BepInEx log:`n$($errors -join "`n")" }
if ($hasPlugin -and $text -notmatch 'Loading \[BaseStorage') { Fail 1 'BaseStorage.dll has a [BepInPlugin] but BepInEx did not load it' }

if ($hasPlugin) { Write-Host 'SMOKE PASS: BepInEx loaded BaseStorage with no errors' }
else { Write-Host 'SMOKE PASS: game started with BepInEx and BaseStorage.dll deployed; no [BepInPlugin] entry point yet, so there was nothing to load' }
exit 0
