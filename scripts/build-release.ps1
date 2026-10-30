# Builds the friend-ready zip: dist/BaseStorage-<version>-installer.zip, holding the installer, the uninstaller and the plugin DLLs.
# The dev-only test-bed plugin lives in another repo and is never included.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$version = ([xml](Get-Content -LiteralPath (Join-Path $repo 'Version.props') -Raw)).Project.PropertyGroup.Version
Push-Location $repo
try {
    dotnet build BaseStorage.sln -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'build failed' }
} finally {
    Pop-Location
}

$stage = Join-Path ([IO.Path]::GetTempPath()) ("basestorage-release-" + [Guid]::NewGuid().ToString('N'))
$plugin = Join-Path $stage 'plugin'
New-Item -ItemType Directory -Force -Path $plugin | Out-Null
$built = Join-Path $repo 'src\BaseStorage.Plugin\bin\Release\net462'
Copy-Item -LiteralPath (Join-Path $built 'BaseStorage.dll'), (Join-Path $built 'BaseStorage.Core.dll') -Destination $plugin
Copy-Item -Path (Join-Path $repo 'installer\*') -Destination $stage

$dist = Join-Path $repo 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$zip = Join-Path $dist "BaseStorage-$version-installer.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
Remove-Item -LiteralPath $stage -Recurse -Force
Write-Host "Wrote $zip"

# The one-file installer: it downloads BepInEx and the mod zip below, and checks both against pinned hashes.
$modZip = Join-Path $dist "BaseStorage-$version-mod.zip"
if (Test-Path -LiteralPath $modZip) { Remove-Item -LiteralPath $modZip -Force }
Compress-Archive -LiteralPath (Join-Path $built 'BaseStorage.dll'), (Join-Path $built 'BaseStorage.Core.dll') -DestinationPath $modZip
$hash = (Get-FileHash -LiteralPath $modZip -Algorithm SHA256).Hash
$url = "https://github.com/dawsoneakins/valheim-basestorage/releases/download/v$version/BaseStorage-$version-mod.zip"
$script = (Get-Content -LiteralPath (Join-Path $repo 'installer\install.ps1') -Raw).Replace('__MOD_URL__', $url).Replace('__MOD_HASH__', $hash)
$head = @(
    '@echo off',
    'set "BS_SELF=%~f0"',
    'powershell -NoProfile -ExecutionPolicy Bypass -Command "$t=[IO.File]::ReadAllText($env:BS_SELF); & ([scriptblock]::Create($t.Substring($t.IndexOf(''#BASESTORAGE''+''-SCRIPT''))))"',
    'pause',
    'exit /b',
    '#BASESTORAGE-SCRIPT'
) -join "`r`n"
$setup = Join-Path $dist 'BaseStorage-Setup.bat'
[IO.File]::WriteAllText($setup, $head + "`r`n" + ($script -replace "`r?`n", "`r`n"))
Write-Host "Wrote $modZip"
Write-Host "Wrote $setup"
