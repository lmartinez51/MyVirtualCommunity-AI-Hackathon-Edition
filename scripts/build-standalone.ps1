# ==============================================================================
# MyVirtualCommunity AI Hackathon Edition — Windows Standalone Unity Builder
# Deterministic build freshness verification & automated Unity batchmode build.
# ==============================================================================
[CmdletBinding()]
param(
    [switch]$Force,
    [switch]$CheckOnly,
    [string]$CustomUnityExe = "",
    [string]$LogFile = ""
)

$ErrorActionPreference = "Stop"

# 1. Resolve repository paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = $PSScriptRoot }
$repoRoot = (Resolve-Path "$scriptDir\..").Path
$unityProjectDir = Join-Path $repoRoot "unity-client"
$canonicalBuildDir = Join-Path $unityProjectDir "Builds\Windows"
$canonicalExePath = Join-Path $canonicalBuildDir "MyVirtualCommunity.exe"
$manifestPath = Join-Path $canonicalBuildDir "build-manifest.json"

if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $scratchDir = Join-Path $repoRoot "scratch"
    if (-not (Test-Path $scratchDir)) {
        New-Item -ItemType Directory -Path $scratchDir -Force | Out-Null
    }
    $LogFile = Join-Path $scratchDir "unity_build.log"
}

# 2. Determine expected Unity Editor version from ProjectVersion.txt
$versionFile = Join-Path $unityProjectDir "ProjectSettings\ProjectVersion.txt"
$expectedVersion = "6000.3.25f1" # Fallback default
if (Test-Path $versionFile) {
    $versionContent = Get-Content $versionFile -Raw
    if ($versionContent -match 'm_EditorVersion:\s*([^\s\r\n]+)') {
        $expectedVersion = $matches[1]
    }
}

# 3. Resolve Unity Editor Executable
function Resolve-UnityEditor([string]$overridePath, [string]$version) {
    if (-not [string]::IsNullOrWhiteSpace($overridePath) -and (Test-Path $overridePath)) {
        return (Resolve-Path $overridePath).Path
    }

    # Candidate 1: Standard Unity Hub install directory
    $hubEditor = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (Test-Path $hubEditor) {
        return $hubEditor
    }

    # Candidate 2: Local user Hub editor directory
    $userHubEditor = Join-Path $env:LOCALAPPDATA "Programs\Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (Test-Path $userHubEditor) {
        return $userHubEditor
    }

    # Candidate 3: Unity on PATH
    $pathCmd = Get-Command "Unity.exe" -ErrorAction SilentlyContinue
    if ($pathCmd -and (Test-Path $pathCmd.Source)) {
        return $pathCmd.Source
    }

    return $null
}

$unityExe = Resolve-UnityEditor -overridePath $CustomUnityExe -version $expectedVersion

# 4. Compute Deterministic Unity Project Fingerprint
# Hashes all inputs: Assets/, ProjectSettings/, Packages/manifest.json, Packages/packages-lock.json, and scripts/build-standalone.ps1
function Get-UnityProjectFingerprint([string]$projectDir, [string]$repoRootDir = "") {
    $basePath = (Resolve-Path $projectDir).Path
    if ([string]::IsNullOrWhiteSpace($repoRootDir)) {
        $repoRootDir = (Resolve-Path "$basePath\..").Path
    } else {
        $repoRootDir = (Resolve-Path $repoRootDir).Path
    }

    $allFiles = @()

    $unityInputs = @(
        (Join-Path $basePath "Assets"),
        (Join-Path $basePath "ProjectSettings"),
        (Join-Path $basePath "Packages\manifest.json")
    )
    $lockFile = Join-Path $basePath "Packages\packages-lock.json"
    if (Test-Path $lockFile) {
        $unityInputs += $lockFile
    }

    foreach ($sp in $unityInputs) {
        if (Test-Path -PathType Container $sp) {
            $files = Get-ChildItem -Path $sp -Recurse -File -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -notmatch '[\\/](Library|Temp|Logs|Obj|Builds)[\\/]' }
            if ($files) { $allFiles += $files }
        } elseif (Test-Path -PathType Leaf $sp) {
            $allFiles += (Get-Item $sp)
        }
    }

    $buildScriptPath = Join-Path $repoRootDir "scripts\build-standalone.ps1"
    if (Test-Path $buildScriptPath) {
        $allFiles += (Get-Item $buildScriptPath)
    }

    # Sort files deterministically by relative path with forward slashes
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $memStream = New-Object System.IO.MemoryStream
    $streamWriter = New-Object System.IO.StreamWriter($memStream)

    $getRelPath = {
        param($file)
        if ($file.FullName.StartsWith($basePath, [System.StringComparison]::OrdinalIgnoreCase)) {
            return "unity-client/" + $file.FullName.Substring($basePath.Length).TrimStart('\','/').Replace('\', '/').ToLowerInvariant()
        } elseif ($file.FullName.StartsWith($repoRootDir, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $file.FullName.Substring($repoRootDir.Length).TrimStart('\','/').Replace('\', '/').ToLowerInvariant()
        } else {
            return $file.Name.ToLowerInvariant()
        }
    }

    $sortedFiles = $allFiles | Sort-Object { & $getRelPath $_ }

    foreach ($file in $sortedFiles) {
        $relPath = & $getRelPath $file
        $fileBytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $fileHash = $sha256.ComputeHash($fileBytes)
        $fileHashHex = [BitConverter]::ToString($fileHash).Replace('-', '').ToLowerInvariant()
        $streamWriter.WriteLine($relPath + ":" + $fileHashHex)
    }

    $streamWriter.Flush()
    $memStream.Position = 0
    $compositeHash = $sha256.ComputeHash($memStream.ToArray())
    $fingerprintHex = [BitConverter]::ToString($compositeHash).Replace('-', '').ToLowerInvariant()

    $streamWriter.Dispose()
    $memStream.Dispose()
    $sha256.Dispose()

    return $fingerprintHex
}

# 5. Critical Runtime Files Definition
$CriticalRuntimeFiles = @(
    "MyVirtualCommunity.exe",
    "UnityPlayer.dll",
    "UnityCrashHandler64.exe",
    "MyVirtualCommunity_Data/boot.config",
    "MyVirtualCommunity_Data/globalgamemanagers",
    "MyVirtualCommunity_Data/level0",
    "MyVirtualCommunity_Data/sharedassets0.assets",
    "MyVirtualCommunity_Data/Managed/Assembly-CSharp.dll"
)

# 6. Inspect Existing Build Manifest & Validate Full Output Integrity
function Test-BuildFreshness([string]$currentFingerprint, [string]$expectedUnityVersion, [string]$targetBuildDir = $canonicalBuildDir) {
    $exeFile = Join-Path $targetBuildDir "MyVirtualCommunity.exe"
    $manifestFile = Join-Path $targetBuildDir "build-manifest.json"

    if (-not (Test-Path $exeFile)) {
        return @{ IsFresh = $false; Reason = "Canonical executable missing ($exeFile)" }
    }
    if (-not (Test-Path $manifestFile)) {
        return @{ IsFresh = $false; Reason = "Build manifest missing ($manifestFile)" }
    }

    try {
        $manifestJson = Get-Content $manifestFile -Raw -ErrorAction Stop | ConvertFrom-Json
    } catch {
        return @{ IsFresh = $false; Reason = "Build manifest is corrupted or invalid JSON" }
    }

    if ($manifestJson.buildResult -ne "Succeeded") {
        return @{ IsFresh = $false; Reason = "Previous build did not record Succeeded status" }
    }
    if ($manifestJson.unityEditorVersion -ne $expectedUnityVersion) {
        return @{ IsFresh = $false; Reason = "Build was created with different Unity version ($($manifestJson.unityEditorVersion) vs expected $expectedUnityVersion)" }
    }
    if ($manifestJson.projectFingerprint -ne $currentFingerprint) {
        return @{ IsFresh = $false; Reason = "Project fingerprint mismatch (current: $currentFingerprint, manifest: $($manifestJson.projectFingerprint))" }
    }

    # Verify primary executable integrity
    $currentExeHash = (Get-FileHash -Path $exeFile -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($manifestJson.executableSha256 -and ($manifestJson.executableSha256.ToLowerInvariant() -ne $currentExeHash)) {
        return @{ IsFresh = $false; Reason = "Executable binary hash mismatch against recorded manifest" }
    }

    # Verify critical runtime files
    if ($manifestJson.criticalFiles) {
        foreach ($item in $manifestJson.criticalFiles) {
            $normPath = $item.path.Replace('/', '\')
            $itemFullPath = Join-Path $targetBuildDir $normPath
            if (-not (Test-Path $itemFullPath)) {
                return @{ IsFresh = $false; Reason = "Critical runtime file missing: $($item.path)" }
            }
            $itemFile = Get-Item $itemFullPath
            if ($null -ne $item.sizeBytes -and ($itemFile.Length -ne [long]$item.sizeBytes)) {
                return @{ IsFresh = $false; Reason = "Critical runtime file size mismatch for $($item.path) ($($itemFile.Length) vs $($item.sizeBytes))" }
            }
            if (-not [string]::IsNullOrWhiteSpace($item.sha256)) {
                $actualHash = (Get-FileHash -Path $itemFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
                if ($actualHash -ne $item.sha256.ToLowerInvariant()) {
                    return @{ IsFresh = $false; Reason = "Critical runtime file hash mismatch for $($item.path)" }
                }
            }
        }
    } else {
        # Fallback check for critical files existence
        foreach ($cf in $CriticalRuntimeFiles) {
            $normPath = $cf.Replace('/', '\')
            $cfPath = Join-Path $targetBuildDir $normPath
            if (-not (Test-Path $cfPath)) {
                return @{ IsFresh = $false; Reason = "Essential runtime file missing: $cf" }
            }
        }
    }

    return @{
        IsFresh = $true
        Manifest = $manifestJson
        Fingerprint = $currentFingerprint
    }
}

# --- Execution Entrypoint ---
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " MyVirtualCommunity: Standalone Build Freshness Checker" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Unity Project:     $unityProjectDir" -ForegroundColor Gray
Write-Host "Expected Version:  $expectedVersion" -ForegroundColor Gray
Write-Host "Canonical Output:  $canonicalExePath" -ForegroundColor Gray

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$currentFingerprint = Get-UnityProjectFingerprint -projectDir $unityProjectDir
$sw.Stop()
Write-Host "Project Fingerprint: $currentFingerprint (computed in $($sw.ElapsedMilliseconds) ms)" -ForegroundColor Gray

$freshness = Test-BuildFreshness -currentFingerprint $currentFingerprint -expectedUnityVersion $expectedVersion

if ($CheckOnly) {
    if ($freshness.IsFresh -and -not $Force) {
        Write-Host "Build Status: FRESH (Up-to-date with current sources)" -ForegroundColor Green
        exit 0
    } else {
        $reason = if ($Force) { "-Force specified" } else { $freshness.Reason }
        Write-Host "Build Status: STALE or REBUILD REQUIRED ($reason)" -ForegroundColor Yellow
        exit 1
    }
}

if ($freshness.IsFresh -and -not $Force) {
    Write-Host "`nStandalone build is FRESH and up-to-date!" -ForegroundColor Green
    Write-Host "  Executable: $canonicalExePath" -ForegroundColor Gray
    Write-Host "  Fingerprint: $($freshness.Manifest.projectFingerprint)" -ForegroundColor Gray
    Write-Host "  Built:       $($freshness.Manifest.buildTimestamp)" -ForegroundColor Gray
    exit 0
}

# --- Build Required ---
$buildReason = if ($Force) { "Explicit -Force specified" } else { $freshness.Reason }
Write-Host "`nRebuilding Windows Standalone Client... ($buildReason)" -ForegroundColor Yellow

if (-not $unityExe) {
    Write-Host "  ERROR: Unity Editor $expectedVersion was not found on this system." -ForegroundColor Red
    Write-Host "  Required Editor Path: C:\Program Files\Unity\Hub\Editor\$expectedVersion\Editor\Unity.exe" -ForegroundColor Yellow
    Write-Host "  Cannot build Standalone client automatically." -ForegroundColor Red
    exit 1
}

Write-Host "  Using Unity Editor: $unityExe" -ForegroundColor Cyan
Write-Host "  Build Log:          $LogFile" -ForegroundColor Cyan

# Create destination directory if needed
if (-not (Test-Path $canonicalBuildDir)) {
    New-Item -ItemType Directory -Path $canonicalBuildDir -Force | Out-Null
}

# Invalidate or remove stale manifest before build so failures never leave a false success
if (Test-Path $manifestPath) {
    Remove-Item -Path $manifestPath -Force -ErrorAction SilentlyContinue
}

$buildStart = Get-Date
$buildMethod = "MyVirtualCommunity.Editor.WindowsStandaloneBuilder.BuildWindowsExecutable"
$batchArgs = "-batchmode -quit -projectPath `"$unityProjectDir`" -executeMethod $buildMethod -logFile `"$LogFile`""

Write-Host "  Launching Unity Batchmode Builder..." -ForegroundColor Gray

$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = $unityExe
$processInfo.Arguments = $batchArgs
$processInfo.UseShellExecute = $false
$processInfo.CreateNoWindow = $true

$process = [System.Diagnostics.Process]::Start($processInfo)
$process.WaitForExit()
$exitCode = $process.ExitCode
$buildDuration = (Get-Date) - $buildStart

Write-Host "  Unity process exited with code $exitCode (Duration: $([math]::Round($buildDuration.TotalSeconds, 1))s)." -ForegroundColor Gray

# Validate output
if ($exitCode -ne 0 -or -not (Test-Path $canonicalExePath)) {
    Write-Host "`n  ERROR: Standalone build failed!" -ForegroundColor Red
    Write-Host "  Exit Code: $exitCode" -ForegroundColor Red
    Write-Host "  Inspect build log: $LogFile" -ForegroundColor Yellow
    exit 1
}

# Calculate final executable SHA-256
$exeHash = (Get-FileHash -Path $canonicalExePath -Algorithm SHA256).Hash.ToLowerInvariant()
$exeItem = Get-Item $canonicalExePath

# Gather critical runtime files inventory
$criticalFilesInventory = @()
foreach ($rel in $CriticalRuntimeFiles) {
    $normRel = $rel.Replace('/', '\')
    $itemPath = Join-Path $canonicalBuildDir $normRel
    if (Test-Path $itemPath) {
        $fi = Get-Item $itemPath
        $h = (Get-FileHash -Path $itemPath -Algorithm SHA256).Hash.ToLowerInvariant()
        $criticalFilesInventory += @{
            path = $rel
            sizeBytes = $fi.Length
            sha256 = $h
        }
    } else {
        Write-Host "  WARNING: Critical file not found after build: $rel" -ForegroundColor Yellow
    }
}

# Calculate total payload size (excluding temporary logs or burst debug info)
$payloadDirs = @("MonoBleedingEdge", "MyVirtualCommunity_Data", "D3D12")
$totalPayloadBytes = 0
foreach ($dirName in $payloadDirs) {
    $pDir = Join-Path $canonicalBuildDir $dirName
    if (Test-Path $pDir) {
        $dirSize = (Get-ChildItem -Path $pDir -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
        if ($dirSize) { $totalPayloadBytes += $dirSize }
    }
}
$topFiles = @("MyVirtualCommunity.exe", "UnityPlayer.dll", "UnityCrashHandler64.exe")
foreach ($tf in $topFiles) {
    $tfPath = Join-Path $canonicalBuildDir $tf
    if (Test-Path $tfPath) {
        $totalPayloadBytes += (Get-Item $tfPath).Length
    }
}

# Re-read fingerprint to ensure post-build consistency
$postBuildFingerprint = Get-UnityProjectFingerprint -projectDir $unityProjectDir -repoRootDir $repoRoot

# Create and write build manifest
$manifestData = [ordered]@{
    schemaVersion = "1.1"
    projectFingerprint = $postBuildFingerprint
    unityEditorVersion = $expectedVersion
    buildTarget = "StandaloneWindows64"
    buildTimestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    executablePath = "MyVirtualCommunity.exe"
    executableSha256 = $exeHash
    executableSizeBytes = $exeItem.Length
    totalPayloadSizeBytes = $totalPayloadBytes
    criticalFiles = $criticalFilesInventory
    buildDurationSeconds = [math]::Round($buildDuration.TotalSeconds, 1)
    buildResult = "Succeeded"
}

$manifestJsonStr = $manifestData | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText($manifestPath, $manifestJsonStr)

Write-Host "`n=================================================================" -ForegroundColor Green
Write-Host " Windows Standalone Build COMPLETED & VALIDATED!" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
Write-Host " Executable:       $canonicalExePath"
Write-Host " Size:             $([math]::Round($exeItem.Length / 1MB, 2)) MB ($($exeItem.Length) bytes)"
Write-Host " SHA-256:          $exeHash" -ForegroundColor Cyan
Write-Host " Fingerprint:      $postBuildFingerprint" -ForegroundColor Cyan
Write-Host " Manifest Written: $manifestPath" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green

exit 0
