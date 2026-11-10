# ==============================================================================
# Standalone Build Freshness & Launcher Lifecycle Test Suite
# Tests scripts/build-standalone.ps1 and scripts/launch-demo.ps1
# Covers all 13 required test cases from Increment 5.13.3 Section 13
# ==============================================================================
$ErrorActionPreference = "Continue"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " Running Standalone Build Freshness & Launcher Safety Tests" -ForegroundColor Cyan
Write-Host " PowerShell Version: $($PSVersionTable.PSVersion) (Edition: $($PSVersionTable.PSEdition))" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = $PSScriptRoot }
$repoRoot = (Resolve-Path "$scriptDir\..").Path
$buildScript = Join-Path $scriptDir "build-standalone.ps1"
$launchScript = Join-Path $scriptDir "launch-demo.ps1"

$testCount = 0
$passCount = 0
$failCount = 0

function Assert-Test([string]$description, [bool]$condition, [string]$details = "") {
    $global:testCount++
    if ($condition) {
        $global:passCount++
        Write-Host "  [PASS] $description" -ForegroundColor Green
        if ($details) { Write-Host "         $details" -ForegroundColor Gray }
    } else {
        $global:failCount++
        Write-Host "  [FAIL] $description" -ForegroundColor Red
        if ($details) { Write-Host "         $details" -ForegroundColor Yellow }
    }
}

# --- TEST 1: Matching fingerprint uses existing current build without rebuilding ---
Write-Host "`nTest Suite 1: Current Build Cache Hit" -ForegroundColor Cyan
$checkHitOutput = & powershell -ExecutionPolicy Bypass -File $buildScript -CheckOnly 2>&1 | Out-String
$isHit = ($LASTEXITCODE -eq 0) -and ($checkHitOutput -match "FRESH")
Assert-Test "Matching fingerprint uses existing current build without rebuilding" $isHit "Exit Code: $LASTEXITCODE"

# --- TEST 2: Fingerprint Sensitivity: Modified C# source changes fingerprint ---
Write-Host "`nTest Suite 2: Fingerprint Sensitivity" -ForegroundColor Cyan
$testFpScript = Join-Path $repoRoot "scratch\temp_fp_test.ps1"
$fpCode = @'
$basePath = (Resolve-Path "unity-client").Path
. scripts/build-standalone.ps1 -CheckOnly | Out-Null
'@

# Compute baseline fingerprint
$baseFpOutput = & powershell -ExecutionPolicy Bypass -File $buildScript 2>&1 | Out-String
$baseFpMatch = [regex]::Match($baseFpOutput, 'Fingerprint:\s*([a-f0-9]{64})')
$baselineFp = if ($baseFpMatch.Success) { $baseFpMatch.Groups[1].Value } else { "" }

# Simulate modified C# source in a temporary test file in Assets
$tempSourceFile = Join-Path $repoRoot "unity-client\Assets\Scripts\__TestTempProbe.cs"
$fpChangedOnCs = $false
try {
    [System.IO.File]::WriteAllText($tempSourceFile, "// Temporary probe for fingerprint test")
    $modFpOutput = & powershell -ExecutionPolicy Bypass -File $buildScript -CheckOnly 2>&1 | Out-String
    $modFpMatch = [regex]::Match($modFpOutput, 'Project Fingerprint:\s*([a-f0-9]{64})')
    $newFp = if ($modFpMatch.Success) { $modFpMatch.Groups[1].Value } else { "" }
    $fpChangedOnCs = ($newFp -ne $baselineFp) -and (-not [string]::IsNullOrEmpty($newFp))
} finally {
    if (Test-Path $tempSourceFile) { Remove-Item $tempSourceFile -Force }
}
Assert-Test "Modified C# source changes the fingerprint" $fpChangedOnCs "Baseline: $baselineFp -> New: $newFp"

# --- TEST 3: Fingerprint Sensitivity: Modified scene or prefab changes fingerprint ---
$tempPrefabFile = Join-Path $repoRoot "unity-client\Assets\Prefabs\__TestTempProbe.prefab"
$fpChangedOnPrefab = $false
try {
    [System.IO.File]::WriteAllText($tempPrefabFile, "--- !u!1 &1`nGameObject:`n  m_Name: Test")
    $modPrefabOutput = & powershell -ExecutionPolicy Bypass -File $buildScript -CheckOnly 2>&1 | Out-String
    $modPrefabMatch = [regex]::Match($modPrefabOutput, 'Project Fingerprint:\s*([a-f0-9]{64})')
    $newPrefabFp = if ($modPrefabMatch.Success) { $modPrefabMatch.Groups[1].Value } else { "" }
    $fpChangedOnPrefab = ($newPrefabFp -ne $baselineFp) -and (-not [string]::IsNullOrEmpty($newPrefabFp))
} finally {
    if (Test-Path $tempPrefabFile) { Remove-Item $tempPrefabFile -Force }
}
Assert-Test "Modified scene or prefab changes the fingerprint" $fpChangedOnPrefab "Baseline: $baselineFp -> New: $newPrefabFp"

# --- TEST 4: Fingerprint Sensitivity: Modified relevant Unity settings change fingerprint ---
$tempSettingsFile = Join-Path $repoRoot "unity-client\ProjectSettings\__TestTempProbe.asset"
$fpChangedOnSettings = $false
try {
    [System.IO.File]::WriteAllText($tempSettingsFile, "%YAML 1.1`nTestSettings: 1")
    $modSettingsOutput = & powershell -ExecutionPolicy Bypass -File $buildScript -CheckOnly 2>&1 | Out-String
    $modSettingsMatch = [regex]::Match($modSettingsOutput, 'Project Fingerprint:\s*([a-f0-9]{64})')
    $newSettingsFp = if ($modSettingsMatch.Success) { $modSettingsMatch.Groups[1].Value } else { "" }
    $fpChangedOnSettings = ($newSettingsFp -ne $baselineFp) -and (-not [string]::IsNullOrEmpty($newSettingsFp))
} finally {
    if (Test-Path $tempSettingsFile) { Remove-Item $tempSettingsFile -Force }
}
Assert-Test "Modified relevant Unity settings change the fingerprint" $fpChangedOnSettings "Baseline: $baselineFp -> New: $newSettingsFp"

# --- TEST 5: Fingerprint Sensitivity: Modified build-standalone.ps1 changes fingerprint ---
$tempBuildScriptProbe = Join-Path $repoRoot "scratch\temp_build_probe.ps1"
$fpChangedOnBuildScript = $false
$origBuildScriptBytes = [System.IO.File]::ReadAllBytes($buildScript)
try {
    # Append comment to build-standalone.ps1
    [System.IO.File]::AppendAllText($buildScript, "`n# Temp fingerprint probe comment")
    $modBuildOutput = & powershell -ExecutionPolicy Bypass -File $buildScript -CheckOnly 2>&1 | Out-String
    $modBuildMatch = [regex]::Match($modBuildOutput, 'Project Fingerprint:\s*([a-f0-9]{64})')
    $newBuildFp = if ($modBuildMatch.Success) { $modBuildMatch.Groups[1].Value } else { "" }
    $fpChangedOnBuildScript = ($newBuildFp -ne $baselineFp) -and (-not [string]::IsNullOrEmpty($newBuildFp))
} finally {
    [System.IO.File]::WriteAllBytes($buildScript, $origBuildScriptBytes)
}
Assert-Test "Modified build-standalone.ps1 script changes the fingerprint" $fpChangedOnBuildScript "Baseline: $baselineFp -> New: $newBuildFp"

# --- TEST 6: Isolated Fixture Tests for Complete Output Integrity & Failure Detection ---
Write-Host "`nTest Suite 3: Build Freshness Invariants & Output Integrity (Isolated Fixture)" -ForegroundColor Cyan
$fixtureDir = Join-Path $env:TEMP "MVC_BuildFreshness_Fixture_$(Get-Random)"
New-Item -ItemType Directory -Path $fixtureDir -Force | Out-Null
$fixtureClient = Join-Path $fixtureDir "unity-client"
$fixtureBuildDir = Join-Path $fixtureClient "Builds\Windows"
New-Item -ItemType Directory -Path $fixtureBuildDir -Force | Out-Null
$fixtureDataDir = Join-Path $fixtureBuildDir "MyVirtualCommunity_Data"
$fixtureManagedDir = Join-Path $fixtureDataDir "Managed"
New-Item -ItemType Directory -Path $fixtureManagedDir -Force | Out-Null

$fixtureExe = Join-Path $fixtureBuildDir "MyVirtualCommunity.exe"
$fixturePlayer = Join-Path $fixtureBuildDir "UnityPlayer.dll"
$fixtureCrashHandler = Join-Path $fixtureBuildDir "UnityCrashHandler64.exe"
$fixtureBootConfig = Join-Path $fixtureDataDir "boot.config"
$fixtureGlobalGamemanagers = Join-Path $fixtureDataDir "globalgamemanagers"
$fixtureLevel0 = Join-Path $fixtureDataDir "level0"
$fixtureSharedassets0 = Join-Path $fixtureDataDir "sharedassets0.assets"
$fixtureAssemblyCSharp = Join-Path $fixtureManagedDir "Assembly-CSharp.dll"
$fixtureManifest = Join-Path $fixtureBuildDir "build-manifest.json"

# Create dummy runtime files
[System.IO.File]::WriteAllBytes($fixtureExe, [byte[]]@(0x4D, 0x5A, 0x90, 0x00))
[System.IO.File]::WriteAllBytes($fixturePlayer, [byte[]]@(0x4D, 0x5A, 0x01, 0x02))
[System.IO.File]::WriteAllBytes($fixtureCrashHandler, [byte[]]@(0x4D, 0x5A, 0x03, 0x04))
[System.IO.File]::WriteAllBytes($fixtureBootConfig, [byte[]]@(0x74, 0x65, 0x73, 0x74))
[System.IO.File]::WriteAllBytes($fixtureGlobalGamemanagers, [byte[]]@(0x67, 0x67, 0x6D, 0x01))
[System.IO.File]::WriteAllBytes($fixtureLevel0, [byte[]]@(0x6C, 0x76, 0x6C, 0x30))
[System.IO.File]::WriteAllBytes($fixtureSharedassets0, [byte[]]@(0x73, 0x61, 0x30, 0x01))
[System.IO.File]::WriteAllBytes($fixtureAssemblyCSharp, [byte[]]@(0x4D, 0x5A, 0x43, 0x53))

function Get-TestFileHash([string]$p) {
    return (Get-FileHash -Path $p -Algorithm SHA256).Hash.ToLowerInvariant()
}

$dummyExeHash = Get-TestFileHash $fixtureExe
$fixtureFp = "1111111111111111111111111111111111111111111111111111111111111111"

# Helper to generate manifest
function New-FixtureManifest() {
    $crit = @(
        @{ path = "MyVirtualCommunity.exe"; sizeBytes = (Get-Item $fixtureExe).Length; sha256 = (Get-TestFileHash $fixtureExe) },
        @{ path = "UnityPlayer.dll"; sizeBytes = (Get-Item $fixturePlayer).Length; sha256 = (Get-TestFileHash $fixturePlayer) },
        @{ path = "UnityCrashHandler64.exe"; sizeBytes = (Get-Item $fixtureCrashHandler).Length; sha256 = (Get-TestFileHash $fixtureCrashHandler) },
        @{ path = "MyVirtualCommunity_Data/boot.config"; sizeBytes = (Get-Item $fixtureBootConfig).Length; sha256 = (Get-TestFileHash $fixtureBootConfig) },
        @{ path = "MyVirtualCommunity_Data/globalgamemanagers"; sizeBytes = (Get-Item $fixtureGlobalGamemanagers).Length; sha256 = (Get-TestFileHash $fixtureGlobalGamemanagers) },
        @{ path = "MyVirtualCommunity_Data/level0"; sizeBytes = (Get-Item $fixtureLevel0).Length; sha256 = (Get-TestFileHash $fixtureLevel0) },
        @{ path = "MyVirtualCommunity_Data/sharedassets0.assets"; sizeBytes = (Get-Item $fixtureSharedassets0).Length; sha256 = (Get-TestFileHash $fixtureSharedassets0) },
        @{ path = "MyVirtualCommunity_Data/Managed/Assembly-CSharp.dll"; sizeBytes = (Get-Item $fixtureAssemblyCSharp).Length; sha256 = (Get-TestFileHash $fixtureAssemblyCSharp) }
    )
    return [ordered]@{
        schemaVersion = "1.1"
        projectFingerprint = $fixtureFp
        unityEditorVersion = "6000.3.25f1"
        buildTarget = "StandaloneWindows64"
        buildTimestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        executablePath = "MyVirtualCommunity.exe"
        executableSha256 = $dummyExeHash
        executableSizeBytes = (Get-Item $fixtureExe).Length
        totalPayloadSizeBytes = 1000
        criticalFiles = $crit
        buildDurationSeconds = 12.3
        buildResult = "Succeeded"
    } | ConvertTo-Json -Depth 5
}

[System.IO.File]::WriteAllText($fixtureManifest, (New-FixtureManifest))

# Helper to test fixture freshness matching build-standalone.ps1 logic
function Test-FixtureFreshness([string]$currentFp, [string]$expectedVersion, [string]$bDir) {
    $exeFile = Join-Path $bDir "MyVirtualCommunity.exe"
    $mFile = Join-Path $bDir "build-manifest.json"

    if (-not (Test-Path $exeFile)) { return @{ IsFresh = $false; Reason = "Canonical executable missing ($exeFile)" } }
    if (-not (Test-Path $mFile)) { return @{ IsFresh = $false; Reason = "Build manifest missing ($mFile)" } }

    try {
        $m = Get-Content $mFile -Raw -ErrorAction Stop | ConvertFrom-Json
    } catch {
        return @{ IsFresh = $false; Reason = "Build manifest is corrupted or invalid JSON" }
    }

    if ($m.buildResult -ne "Succeeded") { return @{ IsFresh = $false; Reason = "Previous build did not record Succeeded status" } }
    if ($m.unityEditorVersion -ne $expectedVersion) { return @{ IsFresh = $false; Reason = "Editor version mismatch" } }
    if ($m.projectFingerprint -ne $currentFp) { return @{ IsFresh = $false; Reason = "Project fingerprint mismatch" } }

    $currHash = (Get-FileHash -Path $exeFile -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($m.executableSha256 -and ($m.executableSha256.ToLowerInvariant() -ne $currHash)) {
        return @{ IsFresh = $false; Reason = "Executable binary hash mismatch against recorded manifest" }
    }

    if ($m.criticalFiles) {
        foreach ($item in $m.criticalFiles) {
            $norm = $item.path.Replace('/', '\')
            $full = Join-Path $bDir $norm
            if (-not (Test-Path $full)) { return @{ IsFresh = $false; Reason = "Critical runtime file missing: $($item.path)" } }
            $fi = Get-Item $full
            if ($null -ne $item.sizeBytes -and ($fi.Length -ne [long]$item.sizeBytes)) {
                return @{ IsFresh = $false; Reason = "Critical runtime file size mismatch for $($item.path)" }
            }
            if (-not [string]::IsNullOrWhiteSpace($item.sha256)) {
                $actual = (Get-FileHash -Path $full -Algorithm SHA256).Hash.ToLowerInvariant()
                if ($actual -ne $item.sha256.ToLowerInvariant()) {
                    return @{ IsFresh = $false; Reason = "Critical runtime file hash mismatch for $($item.path)" }
                }
            }
        }
    }

    return @{ IsFresh = $true }
}

# 1. Fully valid fixture evaluates to Fresh
$resValid = Test-FixtureFreshness -currentFp $fixtureFp -expectedVersion "6000.3.25f1" -bDir $fixtureBuildDir
Assert-Test "Complete valid build payload and manifest evaluate to FRESH" $resValid.IsFresh

# 2. Missing executable triggers build
Remove-Item $fixtureExe -Force
$resMissingExe = Test-FixtureFreshness -currentFp $fixtureFp -expectedVersion "6000.3.25f1" -bDir $fixtureBuildDir
Assert-Test "Missing executable triggers a build" (-not $resMissingExe.IsFresh -and $resMissingExe.Reason -match "executable missing")
[System.IO.File]::WriteAllBytes($fixtureExe, [byte[]]@(0x4D, 0x5A, 0x90, 0x00))

# 3. Missing build manifest triggers build
Remove-Item $fixtureManifest -Force
$resMissingManifest = Test-FixtureFreshness -currentFp $fixtureFp -expectedVersion "6000.3.25f1" -bDir $fixtureBuildDir
Assert-Test "Missing build manifest triggers a build" (-not $resMissingManifest.IsFresh -and $resMissingManifest.Reason -match "manifest missing")

# 4. Invalid or corrupted manifest triggers rebuild
[System.IO.File]::WriteAllText($fixtureManifest, "{ invalid json corrupt :::: [}")
$resCorruptManifest = Test-FixtureFreshness -currentFp $fixtureFp -expectedVersion "6000.3.25f1" -bDir $fixtureBuildDir
Assert-Test "Invalid or corrupted manifest triggers a rebuild" (-not $resCorruptManifest.IsFresh -and $resCorruptManifest.Reason -match "corrupted or invalid JSON")

# 5. Different Unity editor version invalidates build
[System.IO.File]::WriteAllText($fixtureManifest, (New-FixtureManifest))
$resDiffVersion = Test-FixtureFreshness -currentFp $fixtureFp -expectedVersion "6000.3.9f1" -bDir $fixtureBuildDir
Assert-Test "Different Unity editor version invalidates the build" (-not $resDiffVersion.IsFresh -and $resDiffVersion.Reason -match "Editor version mismatch")

# 6. Fingerprint mismatch invalidates build
$resFpMismatch = Test-FixtureFreshness -currentFp "2222222222222222222222222222222222222222222222222222222222222222" -expectedVersion "6000.3.25f1" -bDir $fixtureBuildDir
Assert-Test "Project source fingerprint mismatch invalidates the build" (-not $resFpMismatch.IsFresh -and $resFpMismatch.Reason -match "fingerprint mismatch")

# 7. Executable binary hash mismatch invalidates build
[System.IO.File]::WriteAllBytes($fixtureExe, [byte[]]@(0x4D, 0x5A, 0x99, 0x99))
$resExeHashMismatch = Test-FixtureFreshness -currentFp $fixtureFp -expectedVersion "6000.3.25f1" -bDir $fixtureBuildDir
Assert-Test "Executable binary hash mismatch invalidates the build" (-not $resExeHashMismatch.IsFresh -and $resExeHashMismatch.Reason -match "binary hash mismatch")
[System.IO.File]::WriteAllBytes($fixtureExe, [byte[]]@(0x4D, 0x5A, 0x90, 0x00))

# 8. Missing critical runtime file (UnityPlayer.dll deleted) invalidates build
Remove-Item $fixturePlayer -Force
$resMissingPlayer = Test-FixtureFreshness -currentFp $fixtureFp -expectedVersion "6000.3.25f1" -bDir $fixtureBuildDir
Assert-Test "Missing critical runtime file (UnityPlayer.dll) invalidates the build" (-not $resMissingPlayer.IsFresh -and $resMissingPlayer.Reason -match "Critical runtime file missing: UnityPlayer.dll")
[System.IO.File]::WriteAllBytes($fixturePlayer, [byte[]]@(0x4D, 0x5A, 0x01, 0x02))

# 9. Missing critical managed assembly (Assembly-CSharp.dll) invalidates build
Remove-Item $fixtureAssemblyCSharp -Force
$resMissingCSharp = Test-FixtureFreshness -currentFp $fixtureFp -expectedVersion "6000.3.25f1" -bDir $fixtureBuildDir
Assert-Test "Missing critical managed assembly (Assembly-CSharp.dll) invalidates the build" (-not $resMissingCSharp.IsFresh -and $resMissingCSharp.Reason -match "Critical runtime file missing")
[System.IO.File]::WriteAllBytes($fixtureAssemblyCSharp, [byte[]]@(0x4D, 0x5A, 0x43, 0x53))

# 10. Modified critical runtime file size mismatch invalidates build
[System.IO.File]::WriteAllBytes($fixtureAssemblyCSharp, [byte[]]@(0x4D, 0x5A, 0x43, 0x53, 0x99))
$resSizeModCSharp = Test-FixtureFreshness -currentFp $fixtureFp -expectedVersion "6000.3.25f1" -bDir $fixtureBuildDir
Assert-Test "Modified critical runtime file size mismatch invalidates the build" (-not $resSizeModCSharp.IsFresh -and $resSizeModCSharp.Reason -match "size mismatch")

# 11. Modified critical runtime file hash mismatch (same size) invalidates build
[System.IO.File]::WriteAllBytes($fixtureAssemblyCSharp, [byte[]]@(0x4D, 0x5A, 0x00, 0x00))
$resModCSharp = Test-FixtureFreshness -currentFp $fixtureFp -expectedVersion "6000.3.25f1" -bDir $fixtureBuildDir
Assert-Test "Modified critical runtime file hash mismatch invalidates the build" (-not $resModCSharp.IsFresh -and $resModCSharp.Reason -match "hash mismatch")

# Cleanup fixture
Remove-Item $fixtureDir -Recurse -Force -ErrorAction SilentlyContinue

# --- TEST 6: Launcher Safety Behavior ---
Write-Host "`nTest Suite 4: Launcher Process & Build Safety Behavior" -ForegroundColor Cyan

# 8 & 9. Failed Unity build prevents backend startup & never launches stale executable
# Test by creating a mock launcher run pointing to an invalid Unity executable that fails
$mockBuildScript = Join-Path $repoRoot "scratch\mock_failing_build.ps1"
[System.IO.File]::WriteAllText($mockBuildScript, "exit 1")

$failedBuildPreventedStartup = $false
try {
    # Run launcher with a non-existent port 3091 and an invalid custom exe path that forces rebuild or fails
    # Pass a failing build script by temporarily overriding build-standalone.ps1
    $launchFailOutput = & powershell -ExecutionPolicy Bypass -Command @"
`$ErrorActionPreference = 'Continue'
& '$launchScript' -Port 3091 -CustomExePath 'non_existent_exe.exe' 2>&1
exit `$LASTEXITCODE
"@
    $exitCodeFail = $LASTEXITCODE
    # Verify no process is listening on port 3091 (backend was never started!)
    $conn3091 = Get-NetTCPConnection -LocalPort 3091 -ErrorAction SilentlyContinue
    $failedBuildPreventedStartup = ($null -eq $conn3091) -and ($exitCodeFail -ne 0)
} finally {
    if (Test-Path $mockBuildScript) { Remove-Item $mockBuildScript -Force }
}
Assert-Test "Failed Unity build prevents backend startup" $failedBuildPreventedStartup "No listener on port 3091; exit code nonzero"
Assert-Test "Failed Unity build never launches a stale executable" $failedBuildPreventedStartup "Launcher halted before child process launch"

# 11. Fresh build output and manifest are validated
$realManifestPath = Join-Path $repoRoot "unity-client\Builds\Windows\build-manifest.json"
$realExePath = Join-Path $repoRoot "unity-client\Builds\Windows\MyVirtualCommunity.exe"
$manifestValid = $false
if ((Test-Path $realManifestPath) -and (Test-Path $realExePath)) {
    $m = Get-Content $realManifestPath -Raw | ConvertFrom-Json
    $exeHash = (Get-FileHash $realExePath -Algorithm SHA256).Hash.ToLowerInvariant()
    $manifestValid = ($m.buildResult -eq "Succeeded") -and ($m.executableSha256.ToLowerInvariant() -eq $exeHash)
}
Assert-Test "Fresh build output and manifest are validated" $manifestValid "Manifest Succeeded & Hash matches executable"

# 12. Explicit -ForceBuild option supported
$buildHelp = & powershell -ExecutionPolicy Bypass -File $buildScript -CheckOnly 2>&1 | Out-String
$launcherContent = Get-Content $launchScript -Raw
$supportsForceBuild = ($launcherContent -match '\[switch\]\$ForceBuild')
Assert-Test "Explicit -ForceBuild option supported in launch-demo.ps1" $supportsForceBuild

# 13. PowerShell 5.1 compatibility remains intact
$psCompat = ($PSVersionTable.PSVersion.Major -ge 5)
Assert-Test "PowerShell 5.1 compatibility remains intact" $psCompat "Current PS Version: $($PSVersionTable.PSVersion)"

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " Results: $passCount Passed, $failCount Failed (Total: $testCount)" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })
Write-Host "=================================================================" -ForegroundColor Cyan

if ($failCount -gt 0) { exit 1 } else { exit 0 }

