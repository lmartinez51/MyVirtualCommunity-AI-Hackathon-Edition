# ==============================================================================
# Increment 5.23.2 — Simulated World Clock Helper for Valeria Lifecycle Acceptance
# Drives POST /api/world/clock on a running local MyVirtualCommunity backend
# WITHOUT altering any authoritative business schedule or NPC routine definition.
# ==============================================================================
[CmdletBinding()]
param(
    [ValidateSet("Closed", "Opening", "MorningRoaming", "CalibrationStart", "CalibrationEnd", "PreClosing", "ClosingBoundary", "AcceleratedDay", "Reset", "Status")]
    [string]$Preset = "Status",
    [string]$BaseUrl = "http://127.0.0.1:3000",
    [double]$TimeScale = 1.0,
    [int]$AcceleratedDurationSec = 90
)

$ErrorActionPreference = "Stop"

# Reference date: Wednesday 2026-11-04 in America/Ciudad_Juarez (MST = UTC-7)
$presetTimestamps = @{
    "Closed"           = "2026-11-04T15:59:40.000Z" # 08:59:40 MST (20s before 09:00 opening; shutter closed, Valeria inside)
    "Opening"          = "2026-11-04T15:59:55.000Z" # 08:59:55 MST (5s before 09:00 opening transition)
    "MorningRoaming"   = "2026-11-04T17:10:00.000Z" # 10:10:00 MST (normal exterior/showroom roaming)
    "CalibrationStart" = "2026-11-04T19:59:45.000Z" # 12:59:45 MST (15s before 13:00 calibration walk)
    "CalibrationEnd"   = "2026-11-04T20:44:45.000Z" # 13:44:45 MST (15s before 13:45 calibration return)
    "PreClosing"       = "2026-11-04T23:56:50.000Z" # 16:56:50 MST (10s before 16:57 pre-closing return)
    "ClosingBoundary"  = "2026-11-04T23:59:50.000Z" # 16:59:50 MST (10s before 17:00 shutter close)
}

function Show-ValeriaStatus([string]$Url) {
    $clk = Invoke-RestMethod -Uri "$Url/api/world/clock" -Method Get
    $mov = (Invoke-RestMethod -Uri "$Url/api/world/npcs/valeria-optica-attendant/movement" -Method Get).movement
    $biz = (Invoke-RestMethod -Uri "$Url/api/businesses/optical:optica-fashion/status" -Method Get).status
    Write-Host ("[{0}] scale={1} | lifecycle={2} | shutterClosed={3} | bizStatus={4} | wp={5} | pos=({6:F2},{7:F2},{8:F2}) | state={9} | speed={10:F2}" -f `
        $mov.localCivilTime, $clk.clock.timeScale, $mov.businessLifecycleState, $mov.shutterClosedPermitted, `
        $biz.status, $mov.currentWaypointId, $mov.position.x, $mov.position.y, $mov.position.z, `
        $mov.movementState, $mov.currentSpeedMps) -ForegroundColor Cyan
}

if ($Preset -eq "Status") {
    Show-ValeriaStatus -Url $BaseUrl
    exit 0
}

if ($Preset -eq "Reset") {
    $body = @{ reset = $true } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/api/world/clock" -Method Post -ContentType "application/json" -Body $body | Out-Null
    Write-Host "World Clock reset to live real-time." -ForegroundColor Green
    Show-ValeriaStatus -Url $BaseUrl
    exit 0
}

if ($Preset -eq "AcceleratedDay") {
    # Runs an accelerated full business day (08:55 MST -> 17:15 MST = 8h20m = 500 min)
    # Default scale = 333x if TimeScale was left at 1.0 -> completes full day in ~90 seconds
    $scale = if ($TimeScale -le 1.0) { 333.0 } else { $TimeScale }
    Write-Host "Starting accelerated simulated business day (08:55 MST -> 17:15 MST) at ${scale}x for ${AcceleratedDurationSec}s..." -ForegroundColor Yellow
    $body = @{ simulatedTime = "2026-11-04T15:55:00.000Z"; timeScale = $scale } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/api/world/clock" -Method Post -ContentType "application/json" -Body $body | Out-Null
    $endAt = (Get-Date).AddSeconds($AcceleratedDurationSec)
    while ((Get-Date) -lt $endAt) {
        Show-ValeriaStatus -Url $BaseUrl
        Start-Sleep -Seconds 2
    }
    Write-Host "Accelerated day observation finished. Run with '-Preset Reset' to restore live World Clock." -ForegroundColor Green
    exit 0
}

$targetIso = $presetTimestamps[$Preset]
$payload = @{ simulatedTime = $targetIso; timeScale = $TimeScale } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/api/world/clock" -Method Post -ContentType "application/json" -Body $payload | Out-Null
Write-Host "Simulated World Clock set to preset '$Preset' ($targetIso, timeScale=${TimeScale}x)." -ForegroundColor Green
Show-ValeriaStatus -Url $BaseUrl
