$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$unityProjectDir = Join-Path $repoRoot "unity-client"
$scratchDir = Join-Path $repoRoot "scratch"
if (-not (Test-Path $scratchDir)) {
    New-Item -ItemType Directory -Path $scratchDir -Force | Out-Null
}
$logFile = Join-Path $scratchDir "unity_verification_516.log"
$unityExe = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"

$method = "MyVirtualCommunity.Editor.Increment5160VisualVerificationSuite.RunVerification"
$batchArgs = "-batchmode -quit -projectPath `"$unityProjectDir`" -executeMethod $method -logFile `"$logFile`""

Write-Host "Launching Unity batchmode verification: $method" -ForegroundColor Cyan
Write-Host "Log file: $logFile" -ForegroundColor Gray

$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = $unityExe
$processInfo.Arguments = $batchArgs
$processInfo.UseShellExecute = $false
$processInfo.CreateNoWindow = $true

$process = [System.Diagnostics.Process]::Start($processInfo)
$process.WaitForExit()

$exitCode = $process.ExitCode
Write-Host "Unity process exited with code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })

if (Test-Path $logFile) {
    Get-Content $logFile -Tail 40
}

exit $exitCode

