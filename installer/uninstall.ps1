# BaseStorage uninstaller. Removes the plugin, and asks before removing BepInEx, because other mods may need it.
# It leaves the config files, and it never touches save files. Vanilla ignores the keys BaseStorage left on chests.
param(
    [string]$GameDir,
    [string]$SteamRoot,
    [switch]$NoPrompt,
    [switch]$RemoveBepInEx,
    [switch]$IgnoreRunningGame
)

$ErrorActionPreference = 'Stop'

# Join-Path throws on a drive that is not there, and Steam keeps listing libraries on drives that are gone.
function Get-ChildPath([string]$parent, [string]$child) {
    return $parent.TrimEnd('\') + '\' + $child
}

function Test-GameDir([string]$dir) {
    return $dir -and (Test-Path -LiteralPath (Get-ChildPath $dir 'valheim.exe'))
}

function Find-GameDir {
    $roots = @()
    if ($SteamRoot) {
        $roots = @($SteamRoot)
    } else {
        try {
            $key = Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -ErrorAction Stop
            if ($key.SteamPath) { $roots += ($key.SteamPath -replace '/', '\') }
        } catch { }
        $roots += 'C:\Program Files (x86)\Steam'
    }

    foreach ($root in $roots) {
        $candidates = @((Get-ChildPath $root 'steamapps\common\Valheim'))
        $vdf = Get-ChildPath $root 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $vdf) {
            foreach ($match in [regex]::Matches((Get-Content -LiteralPath $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $candidates += (Get-ChildPath ($match.Groups[1].Value -replace '\\\\', '\') 'steamapps\common\Valheim')
            }
        }

        foreach ($candidate in $candidates) {
            if (Test-GameDir $candidate) { return $candidate }
        }
    }

    return $null
}

if (-not $IgnoreRunningGame -and (Get-Process -Name 'valheim' -ErrorAction SilentlyContinue)) {
    Write-Host 'Valheim is running. Close it and run this again.' -ForegroundColor Red
    exit 1
}

$dir = if ($GameDir) { $GameDir } else { Find-GameDir }
if (-not (Test-GameDir $dir)) {
    if ($NoPrompt) {
        Write-Host 'Could not find Valheim.' -ForegroundColor Red
        exit 1
    }

    $dir = (Read-Host 'Could not find Valheim. Paste the folder that holds valheim.exe').Trim('" ')
    if (-not (Test-GameDir $dir)) {
        Write-Host "No valheim.exe in '$dir'." -ForegroundColor Red
        exit 1
    }
}

$plugin = Join-Path $dir 'BepInEx\plugins\BaseStorage'
if (Test-Path -LiteralPath $plugin) {
    Remove-Item -LiteralPath $plugin -Recurse -Force
    Write-Host "Don's QOL removed."
} else {
    Write-Host "Don's QOL was not installed."
}

$remove = $RemoveBepInEx
if (-not $remove -and -not $NoPrompt) {
    $remove = (Read-Host 'Also remove BepInEx? Other mods stop working if you do. (y/N)') -match '^[yY]'
}

if ($remove) {
    foreach ($name in 'BepInEx\core', 'BepInEx\patchers', 'doorstop_libs', 'doorstop_config.ini', 'winhttp.dll', '.doorstop_version', 'start_game_bepinex.sh', 'start_server_bepinex.sh') {
        $path = Join-Path $dir $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Recurse -Force }
    }

    Write-Host 'BepInEx removed. Your config files are still in BepInEx\config.'
}

Write-Host 'Done.' -ForegroundColor Green
