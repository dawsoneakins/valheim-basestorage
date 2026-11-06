# Don's QOL installer. Runs under Windows PowerShell 5.1, so friends need nothing extra installed.
# It never touches save files, and it backs up any plugin file it replaces.
param(
    [string]$GameDir,
    [string]$SteamRoot,
    [string]$PackZip,
    [string]$ExpectedHash = 'BCE631497976A93977CEB08E166712E6C31D15244956F89F17DF092A9B62E29F',
    [string]$PackUrl = 'https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/5.4.2351/',
    [string]$ModZip,
    [string]$ModUrl = '__MOD_URL__',
    [string]$ModHash = '__MOD_HASH__',
    [switch]$NoPrompt,
    [switch]$IgnoreRunningGame
)

$ErrorActionPreference = 'Stop'
# Empty when the script runs from inside Dons-QOL-Setup.bat, which has no folder of its own to read.
$pluginSource = if ($PSScriptRoot) { Join-Path $PSScriptRoot 'plugin' } else { '' }
$modTemp = $null

function Stop-Install([string]$message) {
    Write-Host ''
    Write-Host "Install stopped: $message" -ForegroundColor Red
    Write-Host 'Nothing else was changed.'
    exit 1
}

# Join-Path throws on a drive that is not there, and Steam keeps listing libraries on drives that are gone.
function Get-ChildPath([string]$parent, [string]$child) {
    return $parent.TrimEnd('\') + '\' + $child
}

function Test-GameDir([string]$dir) {
    return $dir -and (Test-Path -LiteralPath (Get-ChildPath $dir 'valheim.exe'))
}

function Find-SteamRoots {
    if ($SteamRoot) { return @($SteamRoot) }
    $roots = @()
    try {
        $key = Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -ErrorAction Stop
        if ($key.SteamPath) { $roots += ($key.SteamPath -replace '/', '\') }
    } catch { }
    $default = 'C:\Program Files (x86)\Steam'
    if ($roots -notcontains $default) { $roots += $default }
    return $roots
}

function Find-GameDir {
    foreach ($root in (Find-SteamRoots)) {
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

# .NET, not Get-FileHash: that cmdlet is missing when PowerShell starts with another version's module path.
function Get-Sha256([string]$path) {
    $sha = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($path)
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($stream))) -replace '-', ''
    } finally {
        $stream.Dispose()
        $sha.Dispose()
    }
}

function Expand-Pack([string]$zipPath, [string]$target) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $prefix = 'BepInExPack_Valheim/'
    $root = [IO.Path]::GetFullPath($target).TrimEnd('\') + '\'
    $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        foreach ($entry in $zip.Entries) {
            if (-not $entry.FullName.StartsWith($prefix) -or $entry.FullName.EndsWith('/')) { continue }
            $relative = $entry.FullName.Substring($prefix.Length) -replace '/', '\'
            $destination = [IO.Path]::GetFullPath((Join-Path $target $relative))
            if (-not $destination.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
                Stop-Install "the BepInEx package has an unsafe path: $($entry.FullName)"
            }

            New-Item -ItemType Directory -Force -Path (Split-Path $destination) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination, $true)
        }
    } finally {
        $zip.Dispose()
    }
}

function Get-File([string]$url, [string]$label) {
    Write-Host "Downloading $label..."
    $path = Join-Path ([IO.Path]::GetTempPath()) ('basestorage-' + [Guid]::NewGuid().ToString('N') + '.zip')
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    (New-Object Net.WebClient).DownloadFile($url, $path)
    return $path
}

# Fetches the mod and checks it before anything in the game folder changes.
function Get-Mod {
    $zipPath = $ModZip
    $temp = $null
    if (-not $zipPath) {
        if ($ModUrl -like '__*') { Stop-Install 'the plugin folder is missing. Extract the whole zip first, then run this again.' }
        $temp = Get-File $ModUrl "Don's QOL"
        $zipPath = $temp
    }

    try {
        $hash = Get-Sha256 $zipPath
        if ($hash -ne $ModHash) { Stop-Install "the Don's QOL download does not match the expected file ($hash)." }
        $folder = Join-Path ([IO.Path]::GetTempPath()) ('basestorage-mod-' + [Guid]::NewGuid().ToString('N'))
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $folder)
        return $folder
    } finally {
        if ($temp -and (Test-Path -LiteralPath $temp)) { Remove-Item -LiteralPath $temp -Force }
    }
}

function Install-BepInEx([string]$dir) {
    $zipPath = $PackZip
    $temp = $null
    if (-not $zipPath) {
        $temp = Get-File $PackUrl 'BepInEx'
        $zipPath = $temp
    }

    try {
        $hash = Get-Sha256 $zipPath
        if ($hash -ne $ExpectedHash) {
            Stop-Install "the BepInEx download does not match the expected file ($hash)."
        }

        Expand-Pack $zipPath $dir
        Write-Host 'BepInEx installed.'
    } finally {
        if ($temp -and (Test-Path -LiteralPath $temp)) { Remove-Item -LiteralPath $temp -Force }
    }
}

if (-not $IgnoreRunningGame -and (Get-Process -Name 'valheim' -ErrorAction SilentlyContinue)) {
    Stop-Install 'Valheim is running. Close it and run this again.'
}

if (-not $pluginSource -or -not (Test-Path -LiteralPath (Join-Path $pluginSource 'Dons-QOL.dll'))) {
    $modTemp = Get-Mod
    $pluginSource = $modTemp
}

$dir = if ($GameDir) { $GameDir } else { Find-GameDir }
if (-not (Test-GameDir $dir)) {
    if ($NoPrompt) { Stop-Install 'could not find Valheim.' }
    $dir = (Read-Host 'Could not find Valheim. Paste the folder that holds valheim.exe').Trim('" ')
    if (-not (Test-GameDir $dir)) { Stop-Install "no valheim.exe in '$dir'." }
}

Write-Host "Valheim found at $dir"

try {
    if (Test-Path -LiteralPath (Join-Path $dir 'BepInEx\core\BepInEx.dll')) {
        Write-Host 'BepInEx is already installed.'
    } else {
        Install-BepInEx $dir
    }

    $target = Join-Path $dir 'BepInEx\plugins\Dons-QOL'
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    $files = Get-ChildItem -LiteralPath $pluginSource -File
    $backup = $null
    # Before the rename the plugin lived in plugins\BaseStorage. BepInEx would load both and see the same GUID twice, so back that folder up and remove it.
    $oldTarget = Join-Path $dir 'BepInEx\plugins\BaseStorage'
    if (Test-Path -LiteralPath $oldTarget) {
        $backup = Join-Path $dir ('BepInEx\Dons-QOL-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
        New-Item -ItemType Directory -Force -Path $backup | Out-Null
        Copy-Item -LiteralPath $oldTarget -Destination (Join-Path $backup 'BaseStorage') -Recurse -Force
        Remove-Item -LiteralPath $oldTarget -Recurse -Force
        Write-Host "Removed the old plugin folder $oldTarget (the rename to Dons-QOL)."
    }

    foreach ($file in $files) {
        $existing = Join-Path $target $file.Name
        if (Test-Path -LiteralPath $existing) {
            if (-not $backup) {
                $backup = Join-Path $dir ('BepInEx\Dons-QOL-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
                New-Item -ItemType Directory -Force -Path $backup | Out-Null
            }

            Copy-Item -LiteralPath $existing -Destination $backup -Force
        }

        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
} catch [UnauthorizedAccessException] {
    Stop-Install 'Windows refused access to the game folder. Right-click the installer and choose Run as administrator.'
}

if ($modTemp -and (Test-Path -LiteralPath $modTemp)) { Remove-Item -LiteralPath $modTemp -Recurse -Force }

Write-Host ''
Write-Host "Don's QOL is installed. Start Valheim as usual." -ForegroundColor Green
if ($backup) { Write-Host "Your previous plugin files are in $backup" }
