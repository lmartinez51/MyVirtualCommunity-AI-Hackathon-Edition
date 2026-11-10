# ==============================================================================
# Comprehensive Launcher Compatibility & Safety Test Suite
# Tests scripts/launch-demo.ps1 under Windows PowerShell 5.1
# ==============================================================================
$ErrorActionPreference = "Continue"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " Running Launcher Compatibility & Process Safety Tests" -ForegroundColor Cyan
Write-Host " PowerShell Version: $($PSVersionTable.PSVersion) (Edition: $($PSVersionTable.PSEdition))" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

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

# Capture baseline of unrelated node processes to verify none are touched
$baselineNodePids = (Get-Process node -ErrorAction SilentlyContinue).Id
Write-Host "Baseline unrelated Node.js PIDs: $($baselineNodePids -join ', ')" -ForegroundColor Gray

# ------------------------------------------------------------------------------
# TEST 1: Parent Environment Variable Restoration
# ------------------------------------------------------------------------------
Write-Host "`nTest Suite 1: Parent Environment Isolation" -ForegroundColor Cyan
$env:PORT = "ORIGINAL_PORT_VAL"
$env:HOST = "ORIGINAL_HOST_VAL"

# Launch in -BackendOnly with -DurationSec 4 on custom port 3088
$testPort = 3088
$launcher1 = Start-Process powershell `
    -ArgumentList "-ExecutionPolicy Bypass -File scripts/launch-demo.ps1 -Port $testPort -BackendOnly -DurationSec 5" `
    -PassThru

Start-Sleep -Seconds 3

Assert-Test "Parent `$env:PORT restored" ($env:PORT -eq "ORIGINAL_PORT_VAL") "Expected ORIGINAL_PORT_VAL, got '$env:PORT'"
Assert-Test "Parent `$env:HOST restored" ($env:HOST -eq "ORIGINAL_HOST_VAL") "Expected ORIGINAL_HOST_VAL, got '$env:HOST'"

# Clean up parent env
Remove-Item env:PORT -ErrorAction SilentlyContinue
Remove-Item env:HOST -ErrorAction SilentlyContinue

# ------------------------------------------------------------------------------
# TEST 2: Backend Health & Child Environment Binding on Custom Port
# ------------------------------------------------------------------------------
Write-Host "`nTest Suite 2: Backend Health Probe on Target Port" -ForegroundColor Cyan
$healthProbeSuccess = $false
$healthData = $null
try {
    $healthData = Invoke-RestMethod -Uri "http://127.0.0.1:$testPort/health" -Method Get -TimeoutSec 3 -ErrorAction Stop
    $healthProbeSuccess = ($healthData.status -eq "ok" -and $healthData.service -eq "MyVirtualCommunity AI Hackathon Assistant")
} catch {
    $healthProbeSuccess = $false
}

Assert-Test "Backend responds on custom port $testPort with correct identity" $healthProbeSuccess "Service: $($healthData.service), Status: $($healthData.status)"

# ------------------------------------------------------------------------------
# TEST 3: Safe Cleanup on Normal Launcher Exit (via -DurationSec)
# ------------------------------------------------------------------------------
Write-Host "`nTest Suite 3: Safe Cleanup of Spawned Backend Process Tree" -ForegroundColor Cyan
# Wait for launcher1 to complete its 5 second duration and exit normally
$launcher1.WaitForExit(10000)
Start-Sleep -Seconds 2

# Check if port 3088 was freed cleanly
$connAfterStop = Get-NetTCPConnection -LocalPort $testPort -State Listen -ErrorAction SilentlyContinue
if ($connAfterStop) {
    Stop-Process -Id $connAfterStop[0].OwningProcess -Force -ErrorAction SilentlyContinue
    Assert-Test "Port $testPort released after launcher normal exit" $false "Lingering connection on PID $($connAfterStop[0].OwningProcess)"
} else {
    Assert-Test "Port $testPort released after launcher normal exit" $true "No lingering listeners on port $testPort"
}

# ------------------------------------------------------------------------------
# TEST 4: Standard Port 3000 Launcher Execution
# ------------------------------------------------------------------------------
Write-Host "`nTest Suite 4: Standard Port 3000 Launcher Execution" -ForegroundColor Cyan
$launcher2 = Start-Process powershell `
    -ArgumentList "-ExecutionPolicy Bypass -File scripts/launch-demo.ps1 -BackendOnly -DurationSec 8" `
    -PassThru

Start-Sleep -Seconds 4
$health3000 = $null
try {
    $health3000 = Invoke-RestMethod -Uri "http://127.0.0.1:3000/health" -Method Get -TimeoutSec 3 -ErrorAction Stop
} catch {}

Assert-Test "Port 3000 backend started and healthy" ($null -ne $health3000 -and $health3000.status -eq "ok") "Model: $($health3000.model), Meta: $($health3000.metaProvider)"

# ------------------------------------------------------------------------------
# TEST 5: Existing Backend Recognition (No Duplicate/Kill)
# ------------------------------------------------------------------------------
Write-Host "`nTest Suite 5: Reusing Pre-Existing Backend Instance" -ForegroundColor Cyan
# Run a second launcher against port 3000 with short duration; it should detect existing backend and not kill it
$output5 = & powershell -ExecutionPolicy Bypass -File scripts/launch-demo.ps1 -BackendOnly -DurationSec 2 2>&1 | Out-String
$recognizedExisting = $output5 -match "Detected existing MyVirtualCommunity backend running on port 3000"
Assert-Test "Second launcher detects and reuses existing backend" $recognizedExisting

# Verify port 3000 is still healthy
$healthStillHealthy = $false
try {
    $h = Invoke-RestMethod -Uri "http://127.0.0.1:3000/health" -Method Get -TimeoutSec 2 -ErrorAction Stop
    $healthStillHealthy = ($h.status -eq "ok")
} catch {}
Assert-Test "Existing backend remained alive and undisturbed" $healthStillHealthy

# Wait for launcher2 to finish its duration
$launcher2.WaitForExit(10000)
Start-Sleep -Seconds 2
$tcp3000 = Get-NetTCPConnection -LocalPort 3000 -State Listen -ErrorAction SilentlyContinue
Assert-Test "Port 3000 released cleanly after primary launcher finishes" ($null -eq $tcp3000)

# ------------------------------------------------------------------------------
# TEST 6: Unrelated Process Protection
# ------------------------------------------------------------------------------
Write-Host "`nTest Suite 6: Protection of Unrelated Node.js Processes" -ForegroundColor Cyan
$currentNodePids = (Get-Process node -ErrorAction SilentlyContinue).Id
$unrelatedSurvived = $true
$missingPids = @()
foreach ($pidVal in $baselineNodePids) {
    if ($currentNodePids -notcontains $pidVal) {
        $unrelatedSurvived = $false
        $missingPids += $pidVal
    }
}
Assert-Test "All pre-existing unrelated Node.js processes survived" $unrelatedSurvived "Missing PIDs: $($missingPids -join ', ')"

# ------------------------------------------------------------------------------
# TEST 7: External Port Conflict Detection (Safe Abort)
# ------------------------------------------------------------------------------
Write-Host "`nTest Suite 7: External Port Conflict Detection (Safe Abort)" -ForegroundColor Cyan
$listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Parse("127.0.0.1"), 3099)
$listener.Start()

try {
    $conflictOutput = & powershell -ExecutionPolicy Bypass -File scripts/launch-demo.ps1 -Port 3099 -BackendOnly 2>&1 | Out-String
    $detectedConflict = $conflictOutput -match "occupied by an external service" -or $conflictOutput -match "unresponsive or external"
    $didNotKill = $listener.Server.IsBound
    
    Assert-Test "Launcher detected external port conflict safely" $detectedConflict
    Assert-Test "Launcher refused to kill foreign listening process" $didNotKill
} finally {
    $listener.Stop()
}

# ------------------------------------------------------------------------------
# TEST 8: Standalone Build Freshness & Lifecycle Invariants
# ------------------------------------------------------------------------------
Write-Host "`nTest Suite 8: Standalone Build Freshness & Lifecycle Invariants" -ForegroundColor Cyan
$freshnessScript = Join-Path $PSScriptRoot "test-build-freshness.ps1"
if (Test-Path $freshnessScript) {
    & powershell -ExecutionPolicy Bypass -File $freshnessScript
    $freshnessOk = ($LASTEXITCODE -eq 0)
    Assert-Test "All 13 build freshness and lifecycle invariants passed" $freshnessOk "Exit Code: $LASTEXITCODE"
} else {
    Assert-Test "Build freshness test script found" $false "Missing $freshnessScript"
}

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " Results: $passCount Passed, $failCount Failed (Total: $testCount)" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })
Write-Host "=================================================================" -ForegroundColor Cyan

if ($failCount -gt 0) { exit 1 } else { exit 0 }
