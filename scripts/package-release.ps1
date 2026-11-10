# ==============================================================================
# MyVirtualCommunity AI Hackathon Edition — Release Packaging Script
# Packages the Windows Standalone Unity Client into Builds/Release/
# ==============================================================================
[CmdletBinding()]
param(
    [switch]$Force,
    [string]$SourceDir = "",
    [string]$OutputDir = "",
    [string]$ArchiveName = "MyVirtualCommunity-Windows-x64.zip"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) {
    $scriptDir = $PSScriptRoot
}
$repoRoot = (Resolve-Path "$scriptDir\..").Path

if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    $SourceDir = Join-Path $repoRoot "unity-client\Builds\Windows"
}
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "Builds\Release"
}

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " MyVirtualCommunity: Packaging Windows Standalone Release" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Resolve paths
if (-not (Test-Path $SourceDir)) {
    Write-Error "Source build directory not found: $SourceDir`nPlease ensure the Unity Standalone build is present."
    exit 1
}
$sourcePath = (Resolve-Path -Path $SourceDir).Path

# Verify build freshness using canonical build-standalone script
$buildScript = Join-Path $scriptDir "build-standalone.ps1"
if (Test-Path $buildScript) {
    Write-Host "Validating build freshness against current Unity sources..." -ForegroundColor Cyan
    & powershell -ExecutionPolicy Bypass -File $buildScript -CheckOnly
    if ($LASTEXITCODE -ne 0 -and -not $Force) {
        Write-Error "The build in $sourcePath is STALE or unverified against current project sources.`nRun .\scripts\build-standalone.ps1 to rebuild before packaging, or use -Force to bypass."
        exit 1
    }
}

# Verify essential files exist
$exePath = Join-Path $sourcePath "MyVirtualCommunity.exe"
$playerDll = Join-Path $sourcePath "UnityPlayer.dll"
$dataDir = Join-Path $sourcePath "MyVirtualCommunity_Data"

if (-not (Test-Path $exePath) -or -not (Test-Path $playerDll) -or -not (Test-Path $dataDir)) {
    Write-Error "Incomplete Unity build in $sourcePath.`nMissing MyVirtualCommunity.exe, UnityPlayer.dll, or MyVirtualCommunity_Data/."
    exit 1
}

# 2. Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}
$resolvedOutput = (Resolve-Path -Path $OutputDir).Path
$targetZip = Join-Path $resolvedOutput $ArchiveName

if ((Test-Path $targetZip) -and -not $Force) {
    Write-Warning "Archive already exists: $targetZip"
    Write-Host "Use -Force to overwrite the existing release archive." -ForegroundColor Yellow
    $existingHash = Get-FileHash -Path $targetZip -Algorithm SHA256
    $existingItem = Get-Item -Path $targetZip
    Write-Host "Existing Archive Size: $([math]::Round($existingItem.Length / 1MB, 2)) MB ($($existingItem.Length) bytes)"
    Write-Host "Existing Archive SHA-256: $($existingHash.Hash)" -ForegroundColor Green
    exit 0
}

if (Test-Path $targetZip) {
    Write-Host "Overwriting existing archive (-Force specified)..." -ForegroundColor Yellow
    Remove-Item -Path $targetZip -Force
}

# 3. Create a clean staging folder to exclude logs and Burst debug info
$stagingDir = Join-Path $env:TEMP "MVC_Release_Staging_$(Get-Random)"
if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir | Out-Null

try {
    Write-Host "Staging release payload from: $sourcePath" -ForegroundColor Gray
    
    # Items to include
    $includedItems = @(
        "MyVirtualCommunity.exe",
        "UnityPlayer.dll",
        "UnityCrashHandler64.exe",
        "D3D12",
        "MonoBleedingEdge",
        "MyVirtualCommunity_Data"
    )

    $uncompressedBytes = 0
    foreach ($item in $includedItems) {
        $srcItemPath = Join-Path $sourcePath $item
        if (Test-Path $srcItemPath) {
            $destItemPath = Join-Path $stagingDir $item
            Copy-Item -Path $srcItemPath -Destination $destItemPath -Recurse -Force
            if (Test-Path -PathType Container $srcItemPath) {
                $subFiles = Get-ChildItem -Path $srcItemPath -Recurse -File
                foreach ($sf in $subFiles) {
                    $uncompressedBytes += $sf.Length
                }
            } else {
                $f = Get-Item -Path $srcItemPath
                $uncompressedBytes += $f.Length
            }
        } else {
            Write-Warning "Item not found in build: $item (omitting)"
        }
    }

    Write-Host "Total uncompressed payload size: $([math]::Round($uncompressedBytes / 1MB, 2)) MB ($uncompressedBytes bytes)" -ForegroundColor Gray

    # 4. Create ZIP archive using Compress-Archive
    Write-Host "Compressing archive to: $targetZip ..." -ForegroundColor Cyan
    Compress-Archive -Path "$stagingDir\*" -DestinationPath $targetZip -CompressionLevel Optimal

    if (-not (Test-Path $targetZip)) {
        Write-Error "Failed to produce release archive at $targetZip"
        exit 1
    }

    $zipItem = Get-Item -Path $targetZip
    $zipBytes = $zipItem.Length
    $hash = Get-FileHash -Path $targetZip -Algorithm SHA256

    # 5. Verify archive integrity
    Write-Host "Verifying archive integrity..." -ForegroundColor Gray
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($targetZip)
    $entryCount = $zip.Entries.Count
    $hasExe = $false
    foreach ($entry in $zip.Entries) {
        if ($entry.Name -eq "MyVirtualCommunity.exe") {
            $hasExe = $true
            break
        }
    }
    $zip.Dispose()

    if (-not $hasExe) {
        Write-Error "Verification failed: MyVirtualCommunity.exe not found in generated zip archive!"
        exit 1
    }

    Write-Host ""
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host " Package Created and Verified Successfully!" -ForegroundColor Green
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host " Archive Path:        $targetZip"
    Write-Host " Total Files:         $entryCount"
    Write-Host " Uncompressed Size:   $([math]::Round($uncompressedBytes / 1MB, 2)) MB ($uncompressedBytes bytes)"
    Write-Host " Compressed Size:     $([math]::Round($zipBytes / 1MB, 2)) MB ($zipBytes bytes)"
    Write-Host " SHA-256 Checksum:    $($hash.Hash)" -ForegroundColor Yellow
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Extraction Instructions for Evaluators:"
    Write-Host "  1. Extract $ArchiveName to a local directory (e.g. unity-client/Builds/Windows/ or Builds/Release/MyVirtualCommunity-Windows-x64/)"
    Write-Host "  2. Ensure MyVirtualCommunity.exe, UnityPlayer.dll, and MyVirtualCommunity_Data/ are present"
    Write-Host "  3. Execute .\scripts\launch-demo.ps1 to start the backend and game seamlessly"
    Write-Host ""
}
finally {
    if (Test-Path $stagingDir) {
        Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
