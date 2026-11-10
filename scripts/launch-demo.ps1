# ==============================================================================
# MyVirtualCommunity AI Hackathon Edition — Evaluator Demonstration Launcher
# Safely starts the backend World Core & launches the Unity Standalone client.
# ==============================================================================
[CmdletBinding()]
param(
    [int]$Port = 3000,
    [string]$HostAddress = "127.0.0.1",
    [int]$StartupTimeoutSec = 20,
    [switch]$BackendOnly,
    [switch]$ForceBuild,
    [int]$DurationSec = 0,
    [string]$CustomExePath = ""
)

$ErrorActionPreference = "Stop"

function Stop-ProcessTree([int]$parentPid) {
    if ($parentPid -le 0) { return }
    try {
        $children = Get-CimInstance Win32_Process -Filter "ParentProcessId = $parentPid" -ErrorAction SilentlyContinue
        foreach ($child in $children) {
            Stop-ProcessTree -parentPid $child.ProcessId
            try {
                $cProc = Get-Process -Id $child.ProcessId -ErrorAction SilentlyContinue
                if ($cProc -and -not $cProc.HasExited) {
                    $cProc.Kill()
                    $cProc.WaitForExit(1500)
                }
            } catch { }
        }
    } catch {
        # Ignore errors querying CIM
    }
    try {
        $pProc = Get-Process -Id $parentPid -ErrorAction SilentlyContinue
        if ($pProc -and -not $pProc.HasExited) {
            $pProc.Kill()
            $pProc.WaitForExit(1500)
        }
    } catch { }
}

# 1. Resolve paths relative to script location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) {
    $scriptDir = $PSScriptRoot
}
$repoRoot = (Resolve-Path "$scriptDir\..").Path

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " MyVirtualCommunity: Evaluator Demonstration Launcher" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Repository Root: $repoRoot" -ForegroundColor Gray

# 2. Verify Node.js prerequisite
Write-Host "`n[1/5] Verifying Node.js environment..." -ForegroundColor Cyan
try {
    $nodeVersionOutput = & node -v 2>&1
    if ($LASTEXITCODE -ne 0 -or -not $nodeVersionOutput) {
        throw "node -v returned non-zero exit code"
    }
    Write-Host "  Found Node.js: $nodeVersionOutput" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Node.js is not found on PATH or failed to execute." -ForegroundColor Red
    Write-Host "  Prerequisite: Node.js v20.18.0 or higher is required." -ForegroundColor Yellow
    Write-Host "  Download from: https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# 3. Verify repository dependencies
Write-Host "`n[2/5] Verifying dependencies and backend files..." -ForegroundColor Cyan
$nodeModulesDir = Join-Path $repoRoot "node_modules"
$tsxCli = Join-Path $nodeModulesDir "tsx\dist\cli.mjs"
$serverIndex = Join-Path $repoRoot "src\server\index.ts"

if (-not (Test-Path $serverIndex)) {
    Write-Host "  ERROR: Server entrypoint not found at $serverIndex" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $nodeModulesDir) -or -not (Test-Path $tsxCli)) {
    Write-Host "  WARNING: node_modules or tsx compiler not found." -ForegroundColor Yellow
    Write-Host "  Attempting to install dependencies (npm install)..." -ForegroundColor Yellow
    Push-Location $repoRoot
    try {
        & npm install
        if ($LASTEXITCODE -ne 0) {
            throw "npm install exited with code $LASTEXITCODE"
        }
    } finally {
        Pop-Location
    }
}

if (-not (Test-Path $tsxCli)) {
    Write-Host "  ERROR: tsx runner not found in $tsxCli even after npm install." -ForegroundColor Red
    Write-Host "  Please run 'npm install' manually from the repository root." -ForegroundColor Yellow
    exit 1
}
Write-Host "  Dependencies and backend files verified." -ForegroundColor Green

# 4. Verify Windows Standalone Client Build Freshness (Safety: must succeed before starting backend!)
$targetExe = $null
if (-not $BackendOnly) {
    Write-Host "`n[3/6] Verifying Windows Standalone client build freshness..." -ForegroundColor Cyan
    if (-not [string]::IsNullOrWhiteSpace($CustomExePath)) {
        if (-not (Test-Path $CustomExePath)) {
            Write-Host "  ERROR: Custom executable path not found: $CustomExePath" -ForegroundColor Red
            exit 1
        }
        $targetExe = (Resolve-Path $CustomExePath).Path
        Write-Host "  Using Custom Executable: $targetExe (Build freshness check bypassed for custom path)" -ForegroundColor Yellow
    } else {
        $buildScript = Join-Path $scriptDir "build-standalone.ps1"
        $buildArgs = @()
        if ($ForceBuild) { $buildArgs += "-Force" }

        & powershell -ExecutionPolicy Bypass -File $buildScript @buildArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Host "`n  FATAL: Standalone client build failed or could not be verified." -ForegroundColor Red
            Write-Host "  The launcher will NOT start the backend or execute an unverified executable." -ForegroundColor Red
            exit 1
        }

        $canonicalExe = Join-Path $repoRoot "unity-client\Builds\Windows\MyVirtualCommunity.exe"
        if (-not (Test-Path $canonicalExe)) {
            Write-Host "`n  FATAL: Canonical executable not found at $canonicalExe." -ForegroundColor Red
            exit 1
        }
        $targetExe = $canonicalExe
    }
}

# 5. Check port availability & pre-existing backend check
Write-Host "`n[4/6] Checking port $Port availability..." -ForegroundColor Cyan
$existingBackendPid = $null
$reusingExistingBackend = $false

# Give lingering TIME_WAIT / CLOSE_WAIT sockets or a terminating process up to 3 seconds to clear
$tcpConnections = $null
$portCheckStart = Get-Date
while ((Get-Date) - $portCheckStart -lt [TimeSpan]::FromSeconds(3)) {
    $listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if (-not $listening) {
        # Check if non-listening sockets (e.g. TIME_WAIT) exist
        $nonListen = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
        if (-not $nonListen) {
            break
        }
        Start-Sleep -Milliseconds 400
        continue
    }
    $tcpConnections = $listening
    break
}

if (-not $tcpConnections) {
    # Final check for an active listener
    $tcpConnections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
}

if ($tcpConnections) {
    $occupyingPid = $tcpConnections[0].OwningProcess
    Write-Host "  Port $Port is currently in use by process PID: $occupyingPid." -ForegroundColor Yellow
    
    # If the process is already dead or terminating, wait briefly for port release
    $procCheck = Get-Process -Id $occupyingPid -ErrorAction SilentlyContinue
    if (-not $procCheck) {
        Start-Sleep -Milliseconds 800
    }

    # Probe health to see if it belongs to MyVirtualCommunity
    $probedHealth = $false
    $healthAttempts = 0
    while ($healthAttempts -lt 3 -and -not $probedHealth) {
        $healthAttempts++
        try {
            $existingHealth = Invoke-RestMethod -Uri "http://${HostAddress}:${Port}/health" -Method Get -TimeoutSec 2 -ErrorAction Stop
            if ($existingHealth -and $existingHealth.service -eq "MyVirtualCommunity AI Hackathon Assistant") {
                Write-Host "  Detected existing MyVirtualCommunity backend running on port $Port!" -ForegroundColor Green
                Write-Host "  Service: $($existingHealth.service)" -ForegroundColor Gray
                Write-Host "  World Core: $($existingHealth.worldCore.mode) | Meta: $($existingHealth.metaProvider)" -ForegroundColor Gray
                $reusingExistingBackend = $true
                $existingBackendPid = $occupyingPid
                $probedHealth = $true
                break
            } else {
                Write-Host "  ERROR: Port $Port is occupied by an external service (PID: $occupyingPid)." -ForegroundColor Red
                Write-Host "  The launcher will NOT terminate unrelated processes on your machine." -ForegroundColor Red
                Write-Host "  Please stop the process using port $Port or set another port, e.g.: .\scripts\launch-demo.ps1 -Port 3005" -ForegroundColor Yellow
                exit 1
            }
        } catch {
            # Check if listener has vanished (port just cleared)
            $stillListening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
            if (-not $stillListening) {
                Write-Host "  Port $Port was freed." -ForegroundColor Green
                break
            }
            Start-Sleep -Milliseconds 600
        }
    }

    if (-not $reusingExistingBackend -and (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)) {
        Write-Host "  ERROR: Port $Port is occupied by an unresponsive or external service (PID: $occupyingPid)." -ForegroundColor Red
        Write-Host "  The launcher will NOT terminate unrelated processes on your machine." -ForegroundColor Red
        Write-Host "  Please free port $Port and try again." -ForegroundColor Yellow
        exit 1
    }
}

# 5. Start Backend Process (if not reusing)
$backendProcess = $null
$startedBackend = $false

try {
    if (-not $reusingExistingBackend) {
        Write-Host "  Starting local backend server on port $Port..." -ForegroundColor Cyan
        
        # Windows PowerShell 5.1 and PowerShell 7.x compatible environment propagation
        $origPort = $env:PORT
        $origHost = $env:HOST
        try {
            $env:PORT = "$Port"
            $env:HOST = "$HostAddress"

            $backendProcess = Start-Process node `
                -ArgumentList "`"$tsxCli`"", "`"$serverIndex`"" `
                -WorkingDirectory $repoRoot `
                -PassThru `
                -NoNewWindow
        } finally {
            if ($null -ne $origPort) { $env:PORT = $origPort } else { Remove-Item env:PORT -ErrorAction SilentlyContinue }
            if ($null -ne $origHost) { $env:HOST = $origHost } else { Remove-Item env:HOST -ErrorAction SilentlyContinue }
        }

        if (-not $backendProcess -or $backendProcess.HasExited) {
            Write-Host "  ERROR: Failed to spawn backend process." -ForegroundColor Red
            exit 1
        }
        $startedBackend = $true
        Write-Host "  Backend spawned with PID: $($backendProcess.Id)" -ForegroundColor Green

        # 6. Poll /health until ready
        Write-Host "  Probing backend health at http://${HostAddress}:${Port}/health..." -ForegroundColor Gray
        $startTime = Get-Date
        $ready = $false
        $healthResponse = $null

        while ((Get-Date) - $startTime -lt [TimeSpan]::FromSeconds($StartupTimeoutSec)) {
            if ($backendProcess.HasExited) {
                Write-Host "  ERROR: Backend process exited prematurely with code $($backendProcess.ExitCode)." -ForegroundColor Red
                exit 1
            }
            try {
                $healthResponse = Invoke-RestMethod -Uri "http://${HostAddress}:${Port}/health" -Method Get -TimeoutSec 1 -ErrorAction Stop
                if ($healthResponse -and $healthResponse.status -eq "ok" -and $healthResponse.service -eq "MyVirtualCommunity AI Hackathon Assistant") {
                    $ready = $true
                    break
                }
            } catch {
                Start-Sleep -Milliseconds 500
            }
        }

        if (-not $ready) {
            Write-Host "  ERROR: Backend health check timed out after $StartupTimeoutSec seconds." -ForegroundColor Red
            exit 1
        }

        Write-Host "  Backend is READY!" -ForegroundColor Green
        Write-Host "    Service:        $($healthResponse.service)" -ForegroundColor Gray
        Write-Host "    Meta Provider:  $($healthResponse.metaProvider) (Model: $($healthResponse.model))" -ForegroundColor Gray
        Write-Host "    World Core:     $($healthResponse.worldCore.mode) (Connected: $($healthResponse.worldCore.connected))" -ForegroundColor Gray
    }

    if ($BackendOnly) {
        Write-Host "`n[-BackendOnly specified] Backend is running. Press Ctrl+C to stop." -ForegroundColor Yellow
        $backendOnlyStart = Get-Date
        while ($true) {
            if ($startedBackend -and $backendProcess.HasExited) {
                Write-Host "Backend process terminated." -ForegroundColor Yellow
                break
            }
            if ($DurationSec -gt 0 -and ((Get-Date) - $backendOnlyStart).TotalSeconds -ge $DurationSec) {
                Write-Host "Duration ($DurationSec s) reached in -BackendOnly mode. Exiting..." -ForegroundColor Gray
                break
            }
            Start-Sleep -Seconds 1
        }
        return
    }

    # 7. Launch Validated Unity Client
    if (-not $targetExe -or -not (Test-Path $targetExe)) {
        Write-Host "  ERROR: No validated Standalone executable available to launch." -ForegroundColor Red
        exit 1
    }

    $exeDir = Split-Path -Parent $targetExe
    Write-Host "`n[6/6] Launching Validated Unity 6 Client..." -ForegroundColor Cyan
    Write-Host "  Executing:         $targetExe" -ForegroundColor Gray
    Write-Host "  Working Directory: $exeDir" -ForegroundColor Gray

    $unityProcess = Start-Process -FilePath $targetExe -WorkingDirectory $exeDir -PassThru

    if (-not $unityProcess -or $unityProcess.HasExited) {
        Write-Host "  ERROR: Unity client failed to start or exited immediately." -ForegroundColor Red
        exit 1
    }

    Write-Host "`n=================================================================" -ForegroundColor Green
    Write-Host " Unity Client is RUNNING (PID: $($unityProcess.Id))" -ForegroundColor Green
    Write-Host " Explore the virtual corridor, interact with NPCs, and test citizen reporting." -ForegroundColor Gray
    Write-Host " The launcher will automatically stop the backend when you exit the game." -ForegroundColor Gray
    Write-Host "=================================================================" -ForegroundColor Green

    # 9. Wait for Unity client exit
    if ($DurationSec -gt 0) {
        Write-Host "Monitoring client for bounded smoke test duration ($DurationSec seconds)..." -ForegroundColor Yellow
        $exited = $unityProcess.WaitForExit($DurationSec * 1000)
        if (-not $exited) {
            Write-Host "Duration ($DurationSec s) reached. Stopping Unity client..." -ForegroundColor Yellow
            Stop-Process -Id $unityProcess.Id -Force -ErrorAction SilentlyContinue
            $unityProcess.WaitForExit(5000)
        }
    } else {
        $unityProcess.WaitForExit()
    }
    Write-Host "`nUnity Client exited with code $($unityProcess.ExitCode)." -ForegroundColor Yellow
}
finally {
    # 10. Strict process safety cleanup: stop ONLY the backend process this script started
    if ($startedBackend -and $backendProcess) {
        Write-Host "`nCleaning up local backend process tree (PID: $($backendProcess.Id))..." -ForegroundColor Cyan
        try {
            # Note: Do not gate Stop-ProcessTree behind -not $backendProcess.HasExited because
            # tsx wrapper might have terminated while the actual server child is still active!
            Stop-ProcessTree -parentPid $backendProcess.Id
            
            # Wait briefly for OS socket teardown on $Port so immediate re-runs find a clean port
            $drainWait = Get-Date
            while ((Get-Date) - $drainWait -lt [TimeSpan]::FromSeconds(3)) {
                $listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
                if (-not $listening) { break }
                Start-Sleep -Milliseconds 200
            }
            Write-Host "  Backend process tree stopped and port $Port released cleanly." -ForegroundColor Green
        } catch {
            Write-Host "  Notice: Backend process was already terminated." -ForegroundColor Gray
        }
    } elseif ($reusingExistingBackend) {
        Write-Host "`nLeaving pre-existing backend process running (PID: $existingBackendPid)." -ForegroundColor Gray
    }
    Write-Host "Session finished safely." -ForegroundColor Cyan
}
