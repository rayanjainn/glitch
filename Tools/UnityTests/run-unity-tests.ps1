<#
.SYNOPSIS
  Runs the project's Unity EditMode and/or PlayMode tests headless (Windows).
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File Tools/UnityTests/run-unity-tests.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
.NOTES
  Close the project in the Unity Editor first: a project can only be open in one Unity instance.
  Results: TestResults/<Platform>.xml (NUnit XML) and TestResults/<Platform>.log.
  Exit code: 0 = all passed, non-zero = failures or Unity error (see the .log).
#>
param(
    [Parameter(Mandatory = $true)][string]$UnityPath,
    [ValidateSet('EditMode', 'PlayMode', 'All')][string]$Platform = 'All'
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $UnityPath)) { throw "Unity executable not found: $UnityPath" }

$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$out = Join-Path $project 'TestResults'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$platforms = if ($Platform -eq 'All') { @('EditMode', 'PlayMode') } else { @($Platform) }
$worst = 0
foreach ($p in $platforms) {
    Write-Host "Running $p tests..."
    $arguments = @('-batchmode', '-projectPath', "`"$project`"", '-runTests', '-testPlatform', $p,
                   '-testResults', "`"$out\$p.xml`"", '-logFile', "`"$out\$p.log`"")
    $proc = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    Write-Host "$p exit code: $($proc.ExitCode)  (results: $out\$p.xml, log: $out\$p.log)"
    if ($proc.ExitCode -ne 0) { $worst = $proc.ExitCode }
}
exit $worst
